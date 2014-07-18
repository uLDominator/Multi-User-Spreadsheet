//
// server.cpp
// ~~~~~~~~~~
//
// Copyright (c) 2003-2012 Christopher M. Kohlhoff (chris at kohlhoff dot com)
//
// Distributed under the Boost Software License, Version 1.0. (See accompanying
// file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)
//

#include <fstream>
#include <sstream>
#include <iostream>
#include <string>
#include <boost/bind.hpp>
#include <boost/thread.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/enable_shared_from_this.hpp>
#include <boost/asio.hpp>
#include <boost/tokenizer.hpp>
#include <set>
#include <map>
#include <stack>
#include <boost/property_tree/ptree.hpp>
#include <boost/property_tree/xml_parser.hpp>
#include <boost/foreach.hpp>
#include <boost/signals2.hpp>
#include <boost/signals2/connection.hpp>

using boost::asio::ip::tcp;
/*
*	The TCP_connection class represents a TCP connection from a client.  
*	
*/
class tcp_connection
  : public boost::enable_shared_from_this<tcp_connection>
{
public:
  typedef boost::shared_ptr<tcp_connection> pointer;

  /*
  *	The create method creates a pointer to a TCP_Connection.  It takes an io_service reference
  *	to the socket for the connection.  The connection will destroy it self when it is out of scope.
  */
  static pointer create(boost::asio::io_service& io_service)
  {
    return pointer(new tcp_connection(io_service));
  }

  /*
  *	The socket method returns the socket for the connection.  The socket can be used
  * to send and receive messages.
  */
  tcp::socket& socket()
  {
    return socket_;
  }


private:
  /*
  *	This method must be called through the create method.
  * The TCP_conneciton constructor takes the socket for the connection
  *
  */
  tcp_connection(boost::asio::io_service& io_service)
    : socket_(io_service)
  {
  }
  // The socket is used for network communication to and from the connection
  tcp::socket socket_;
};



	/*
	* The spreadsheet session represents a spreadsheet session on the server.
	* Once a connection is created on the server,and the client joins a session, then
	* a sessoin is created on the server.  When a client makes a change to the spreadsheet,
	* the session will verify the change is valid, and then send the changes to every one else.
	* The session verifies the the client is updating a correct version.  The spreadsheet session
	* will take mutliple clients, updates from each clients, it will allow clients to do an update
	* as well as save.  The session is saved when a client sends the save command or when there are 
	* zero clients on the session.
	*
	*/
class spreadsheet_session
{
	class tcp_server; //foward declare tcp_server class

public:	
	typedef boost::signals2::signal<void ()>  signal_t;
	
	
	/*
	* The connect method sends an event to the server class to inform it to
	* delete the session. 
	*/
	boost::signals2::connection connect(const signal_t::slot_type &subscriber)
    {
        return m_sig.connect(subscriber);
    }

	/* Assumes user has already been authenticated. Assumes there are no duplicate 
	* spreadsheet_sessions with the same filename open. Once the file conneciton is complete
	*/
	spreadsheet_session(std::string file, std::string xml_file, tcp_connection::pointer user)
	{
		std::cout << "-----Starting new Spreadsheet Session: " << file << "-----" << std::endl;

		//Initialize member variables
		this->filename = file;
		this->xml_name = xml_file;
		this->ss_version = 0;
		this->user_count = 0;

		//Attempt to open the filename
		open_file(xml_file);
		save_ss();

		//Add user to list
		add_user(user);
	}
	
	~spreadsheet_session()
	{
		std::cout << "Destroying SS Session: " << this->filename << std::endl;
	}

	/* Adds user to the list of connected users and sends the spreadsheets
	* data to the user.
	*/
	void add_user(tcp_connection::pointer connection)
	{
		std::cout << "Adding user to SS Session: " << this->filename << std::endl;

		//Add to the list and increment count
		this->mtx_.lock();
		this->connected_users.insert(connection);
		this->user_count++;	
		this->mtx_.unlock();
		//Send spreadsheet data to connection
		send_XML(connection);
	}

private:	
	//Member variables
	//the set of connection holds all the connected clients to the session
	std::set<tcp_connection::pointer> connected_users;
	//the map holds all cells that been changed since change
	//the key is the spreadsheet cell and it maps to the contents of the cell
	std::map<std::string, std::string> used_cells;
	//the stack holds all the changes to the cell
	//the stack holds a pair where the first time in the pair is the cell 
	//the second item in the pair is the cell contents
	std::stack< std::pair <std::string, std::string> > changes;
	//the file name is the spreadsheet file name for the session
	std::string filename;
	//the xml_name is the xml file the session saves too
	std::string xml_name;
	//the current version of the update
	int ss_version;
	//the user_count is the total of clients connected to the session
	int user_count;
	//this is used to send an event to the server
	signal_t    m_sig;
    std::string m_text;
	
	//Lock object
	boost::mutex mtx_;
	
	/*
	*	The send call back verifies there was not an error.
	* 	If the client has disconnected then it removes the client from the session
	*/

	void send_callback(const boost::system::error_code& /*error*/ error_code,
	size_t /*bytes_transferred*/, tcp_connection::pointer connection) 
	{
		std::cout << "Finished sending message in SS Session: " << this->filename << std::endl;
		if(error_code)
		{
				std::cout << "Error occured while sending message in SS Session:" << this->filename << std::endl;
				//remove connection from list and decrement count
				this->mtx_.lock();
				this->connected_users.erase(connection);
				this->user_count--;
				int temp_user_count = this->user_count;
				int temp_change_sizes = this->changes.size();
				this->mtx_.unlock();	
				//If no users exist, delete the session
				if(temp_user_count == 0)
				{
					if(temp_change_sizes != 0)
						save_ss();
					//TODO
					//std::cout << "about to m_sig" << std::endl;
					//return m_sig();
				}
						
				return;
		}
		
	}

	/*
	* Attempt to open the given xml file the spreadsheet is saved on. 
	*  If a file does not exist, it creates a the xml file
	*/
	void open_file(std::string f)
	{
		std::cout << "Opening file in SS Session: " << this->filename << std::endl;
		 
		using boost::property_tree::ptree;
		ptree pt;

		//Open the file
		read_xml(f, pt);
		try
		{
			//Iterate over the <cell> </cell> modules
			BOOST_FOREACH(ptree::value_type &v, pt.get_child("spreadsheet"))
			{
				const ptree& child = v.second;
				//Get name and value from module
				//std::string name = child.get<std::string>("name");
				//std::string value = child.get<std::string>("contents");

				std::string name = child.get("name", "");
				std::string value = child.get("contents", "");

				if(name != "" && value != "")
				{
					//Insert into list
					this->used_cells.insert(std::pair<std::string, std::string>(name, value) );
				}
			}
			
		}
		catch(std::exception& e)	{std::cout << "Error occured while opening file in SS Session: " << this->filename << std::endl; }
	}	
	/*
	*	The message_received method receives the messages sent from the client to the server
	*	The session expects the client to send the following message:
	*	
	*	When a client attempts to make a change to a cell
	*	CHANGE
	*	Name:name 
	*	Version:version 
	*	Cell:cell 
	*	Length:length 
	*	content 
	*	
	*	When the client reqeuests an update
	*	UNDO 
	*	Name:name 
	*	Version:version 
	*
	*	When the client request to save the spreadsheet
	*	SAVE 
	*	Name:name
	*
	*	When the client leaves the session
	*	LEAVE 
	*	Name:name 
	*
	*/
	void message_received(tcp_connection::pointer connection, char* buffer, const boost::system::error_code& error_code)
	{
		std::cout << "Received a message in SS Session: " << this->filename << std::endl;
		
		if(error_code)
		{
			std::cout << "Error occured while receiving a message in SS Session: " << this->filename  << std::endl;
			//remove connection from list and decrement count
			this->mtx_.lock();
			this->connected_users.erase(connection);
			this->user_count--;
			int temp_user_count = this->user_count;
			int temp_change_sizes = this->changes.size();
			this->mtx_.unlock();		
			//If no users exist, delete the session
			if(temp_user_count == 0)
			{
				if(temp_change_sizes != 0)
					save_ss();
				//TODO
				//std::cout << "about to m_sig" << std::endl;
				//return m_sig();
			}
			
			return;
		}
		
		
		
		//Turn chars into string
		std::istringstream  in(buffer);

		//String to represent read string
		std::string line;

		//Get first line to tokenize
		getline(in, line);
		
		std::cout << "\nReceived message:\n" << line << std::endl;

		if(line == "CHANGE")
		{
			std::cout << "In CHANGE command" << std::endl;
			//Get name
			getline(in,line);
			std::string file_name = line.substr(5, line.length()-5);
			std::cout << "Name: " << file_name << std::endl;
			
			//Get version
			getline(in,line);
			std::string temp = line.substr(8, line.length()-8);
			int version = std::atoi( temp.c_str() );
			std::cout << "Version: " << version << std::endl;
			
			//Get cell
			getline(in,line);
			std::string cellname = line.substr(5, line.length()-5);
			std::cout << "Cell: " << cellname << std::endl;

			//Get length
			getline(in, line);
			std::string length = line.substr(7, line.length()-7);
			std::cout << "Length: " << length << std::endl;

			//Get content
			getline(in,line);
			std::string content = line;
			std::cout << "Content: " << content << std::endl;

			//Validate version #
			mtx_.lock();
			int temp_version = this->ss_version;
			mtx_.unlock();
			if(version == temp_version)
			{
				std::cout << "Version numbers match" << std::endl;

				//Create iterator and attempt to find key
				std::map<std::string, std::string>::iterator it;
				this->mtx_.lock();
				it = this->used_cells.find(cellname);
				this->mtx_.unlock();

				std::string previousContents = "";
				//If the key is in the table, reassign its value
				if(it != this->used_cells.end())
				{
					//Store previous contents
					previousContents = this->used_cells[cellname];
					this->used_cells[cellname] = content;
				}
				//The key isn't in the table, insert it
				else
				{
					this->mtx_.lock();
					this->used_cells.insert(std::pair<std::string, std::string>(cellname, content));
					this->mtx_.unlock();
				}

				//Push changes onto stack and increment version #
				this->mtx_.lock();
				this->changes.push( std::pair<std::string, std::string>(cellname, previousContents));
				this->ss_version++;
				int temp_version = ss_version;
				this->mtx_.unlock();

				//sendUpdate to all connections except this one
				send_update(connection, cellname, length, content);

				std::ostringstream version_number;
				version_number << temp_version;

				//send CHANGE OK command to connection
				std::string message = "CHANGE OK\nName:";
					message.append(file_name+"\nVersion:");
					message.append(version_number.str()+"\n");

				send_message(connection, message);
			}
			else
			{	
				mtx_.lock();
				std::ostringstream version_number;
				version_number << this->ss_version;
				mtx_.unlock();
				//send CHANGE WAIT to connection
				std::string message = "CHANGE WAIT\nName:";
					message.append(file_name+"\nVersion:");
					message.append(version_number.str()+"\n");

				send_message(connection, message);
			}
			
		}
		else if(line == "UNDO")
		{
			std::cout << "In UNDO command" << std::endl;
			//Get name
			getline(in,line);
			std::string file_name = line.substr(5, line.length()-5);
			
			//Get version
			getline(in,line);
			std::string temp = line.substr(8, line.length()-8);
			int version = std::atoi( temp.c_str() );
			mtx_.lock();
			int temp_version = this->ss_version;
			mtx_.unlock();
			//if invalid version #
			if(version != temp_version)
			{
				std::ostringstream version_number;
				version_number << temp_version;

				//send UNDO WAIT command
				std::string message = "UNDO WAIT\nName:";
					message.append(file_name+"\nVersion:");
					message.append(version_number.str()+"\n");

				send_message(connection, message);
			}
			//check changes size
			else if(changes.size() == 0)
			{
				std::ostringstream version_number;
				version_number << temp_version;

				//send UNDO END command
				std::string message = "UNDO END\nName:";
					message.append(file_name+"\nVersion:");
					message.append(version_number.str()+"\n");

				send_message(connection, message);
			}
			else
			{	
				//retreive last cell changed and its previous value
				this->mtx_.lock();
				std::pair<std::string, std::string> temp = this->changes.top();
				this->changes.pop();
				this->mtx_.unlock();
				
				std::string cellname = temp.first;
				std::string contents = temp.second;
				
				//revert change in used_cells
				if(contents == "")
				{
					std::map<std::string, std::string>::iterator it;
					this->mtx_.lock();
					it = this->used_cells.find(cellname);
					this->used_cells.erase(it);
					this->mtx_.unlock();
				}
				else
				{
					this->mtx_.lock();
					this->used_cells[cellname] = contents;
					this->mtx_.unlock();
				}

				//increment version number
				this->mtx_.lock();
				this->ss_version++;
				int temp_version = this->ss_version;
				this->mtx_.unlock();
				
				std::ostringstream int_to_str;
				int_to_str << contents.length();
				
				//broadcast to all connections
				send_update(connection, cellname, int_to_str.str(),contents);

				std::ostringstream version_number;
				version_number << temp_version;

				std::ostringstream contents_length;
				contents_length << contents.length();

				//send UNDO ok to this connection
				std::string message = "UNDO OK\nName:";
					message.append(file_name+"\nVersion:");
					message.append(version_number.str()+"\nCell:");
					message.append(cellname+"\nLength:");
					message.append(contents_length.str()+"\n");
					message.append(contents+"\n");

				send_message(connection, message);
			}
		}
		else if(line == "SAVE")
		{
			std::cout << "In SAVE command" << std::endl;
			//Get name
			getline(in,line);
			std::string file_name = line.substr(5, line.length()-5);

			//merge unsaved changes with last saved SS
			save_ss();

			//send SAVE OK command to connection
			std::string message = "SAVE OK\nName:";
				message.append(file_name+"\n");

			send_message(connection, message);
		}
		else if(line == "LEAVE")
		{
			std::cout << "In LEAVE command" << std::endl;
			//Get name
			getline(in,line);
			std::string file_name = line.substr(5, line.length()-5);

			//remove connection from list and decrement count
			this->mtx_.lock();
			this->connected_users.erase(connection);
			this->user_count--;
			int temp_user_count = this->user_count;
			int temp_change_sizes = this->changes.size();
			this->mtx_.unlock();
			
			//If no users exist, delete the session
			if(temp_user_count == 0)
			{
				if(temp_change_sizes != 0)
					save_ss();
				//TODO
				//std::cout << "about to m_sig" << std::endl;
				//return m_sig();
			}
		}
		else
		{
			std::cout << "In ERROR command" << std::endl;
			//send ERROR command
			std::string message = "ERROR\n";

			send_message(connection, message);
		}
		
		delete buffer;
	}


	
	/* Saves the spreadsheet with the current data.
	*/
	void save_ss()
	{
		std::cout << "In ss session save_ss for file: " << this->filename << std::endl;
		
		//save the SS by merging popped elements from changes stack
		using boost::property_tree::ptree;
		ptree pt;
		//read through used_cells adding each to the property tree
		std::map<std::string, std::string>::iterator it;
		//Lock 
		this->mtx_.lock();
		
		std::cout << "Number of unsaved changes: " << this->changes.size() << std::endl;
		
		//Determine if we need to save
		it = this->used_cells.begin();
		if(it == this->used_cells.end())
		{
			ptree & node = pt.add("spreadsheet", NULL);
		}
		//Save the ss
		else
			for(it; it != this->used_cells.end(); it++)
			{
				std::string temp_cellname = it->first;
				std::string temp_contents = it->second;

				ptree & node = pt.add("spreadsheet.cell","");

				node.put("name", temp_cellname);
				node.put("contents", temp_contents);
		

				//pt.put("spreadsheet.cell"+temp_cellname, temp_contents);
			}


		//Write to file
		boost::property_tree::xml_writer_settings<char> settings('\t', 1);
		write_xml(this->xml_name, pt, std::locale(), settings);

		//Empty changes stack
		while(!this->changes.empty())
			this->changes.pop();
		
		//Unlock
		this->mtx_.unlock();
	}
	
	/* 
	*	Relay UPDATE command to all connections BESIDES the one given in the parameter.
	*/
	void send_update(tcp_connection::pointer connection, std::string cell_name, std::string length, std::string cell_data)
	{
		std::cout << "Creating UPDATE command for users in SS Session: " << this->filename << std::endl;

		//Lock
		this->mtx_.lock();
		
		std::set<tcp_connection::pointer>::iterator it;
		std::set<tcp_connection::pointer>::iterator begin = this->connected_users.begin();
		std::set<tcp_connection::pointer>::iterator end = this->connected_users.end();
		std::ostringstream version_number;
				version_number << this->ss_version;
				
		//Unlock
		this->mtx_.unlock();
		
		//Loop through all connections
		for(it = begin; it != end; it++)
		{
			//If not the connection
			if(*it == connection)
				continue;

			//Send update information
			std::string message = "UPDATE\nName:";
				message.append(this->filename+"\nVersion:");
				message.append(version_number.str()+"\nCell:");
				message.append(cell_name+"\nLength:");
				message.append(length+"\n");
				message.append(cell_data+"\n");
			
			send_message(*it, message);
		}


	}
	
	/*
	*	Sends the xml file to the client when the client joins the session
	*	The xml head is sent first on a line and the rest of the xml content is sent on the following line
	*/
	void send_XML(tcp_connection::pointer connection)
	{
		std::cout << "Creating XML document in SS Session: " << this->filename << std::endl;

		//Get the string version of the xml data
		std::string xmldata = get_current_state();
		
		mtx_.lock();
		std::ostringstream version;
				version << this->ss_version;
		mtx_.unlock();
		std::ostringstream length;
				length << xmldata.length();

		//Send JOIN OK command
		std::string message = "JOIN OK\nName:";
			message.append(this->filename+"\nVersion:");
			message.append(version.str()+"\nLength:");
			message.append(length.str()+"\n");
			message.append(xmldata+"\n");

		send_message(connection, message);
	}
	
	/*
	*	The get_current_method get's the current state of the session.  It puts it in the xml format
	*	to prepare to send to the user.  The xml format is return in a string.  The string contains the
	*	xml header is on the first line and the remaining of the xml format is on the  next line
	*/
	std::string get_current_state()
	{
		std::cout << "Creating current SS data for SS Session: " << filename << std::endl;

		std::ostringstream ss;

		using boost::property_tree::ptree;
		ptree pt;

		//read through used_cells adding each to the property tree
		std::map<std::string, std::string>::iterator it;

		//Lock
		this->mtx_.lock();

		//Populate property tree
		it = this->used_cells.begin();
		if(it == this->used_cells.end())
		{
			ptree & node = pt.add("spreadsheet", NULL);
		}
		else
			for(it; it != this->used_cells.end(); it++)
			{
				std::string temp_cellname = it->first;
				std::string temp_contents = it->second;

				ptree & node = pt.add("spreadsheet.cell","");

				node.put("name", temp_cellname);
				node.put("contents", temp_contents);
			}

		//Unlock
		this->mtx_.unlock();	

		//Write xml to stringstream
		write_xml(ss, pt);

		return ss.str();
	}
	
	/*
	*	The send message sends the messages from the session to the client.
	* 	The following messages are to be expected from the session:
	*	
	*	When the session was succesfully saved:
	* 	SAVE SP OK
	*	Name:name
	*
	*	If the request to save the session failed:
	*	SAVE SP FAIL
	*	Name:name
	*	message
	*
	*	To communicate a committed change to other clients, the server should send
	*	UPDATE
	*	Name:name
	*	Version:version
	*	Cell:cell
	*	Length:length
	*	content of the change
	*
	*	If the update request succeeded, the server should respond with
	*	UNDO SP OK 
	*	Name:name 
	*	Version:version 
	*	Cell:cell 
	*	Length:length 
	*	content 
	*
	*	If there are no unsaved changes, the server should respond with
	*	UNDO SP END
	*	Name:name
	*	Version:version
	*	
	*	If the client’s version is out of date, the server should respond with 
	*	When u
	*	UNDO SP FAIL LF
	*	Name:name LF
	*	message LF
	*		
	*/
	void send_message(tcp_connection::pointer connection, std::string message)
	{
		std::cout << "In ss session send_message for file: " << this->filename << std::endl;

		std::cout << "\nSending message:\n" << message << std::endl;

		//Send message to socket
		boost::asio::async_write(connection->socket(), boost::asio::buffer(message),
					boost::bind(&spreadsheet_session::send_callback,
					this,
					boost::asio::placeholders::error,
					boost::asio::placeholders::bytes_transferred,
					connection));

		char* ss_RecieveBuffer = new char[256];

		//Set the socket to start receiving
		connection->socket().async_receive(boost::asio::buffer(ss_RecieveBuffer, 255),
			boost::bind(&spreadsheet_session::message_received, //Callback
			this, //the spreadsheet session
			connection, //the current connection
			ss_RecieveBuffer, //the buffer
			boost::asio::placeholders::error)); //any errors
	}
};
	

class tcp_server
{
public:
	/* Server constructor.
	 *
	 */	 
	tcp_server(boost::asio::io_service& io_service)
		: acceptor_(io_service, tcp::endpoint(tcp::v4(), 1984))
	{			
		//read file, add file
		std::ifstream  in("spreadsheet_files.txt");
		std::string line;

		if(in.fail())
		{
			std::cout <<"Error: Could not open spreadsheet_files.txt."<< std::endl;
			return;
		}
		std::cout << "Populating spreadsheet map." << std::endl;
		//While lines remain
		while (getline(in, line))
		{
			//the file format is in the following format:
			//blank line
			//filename
			//password
			//xmlfilename
			getline(in,line); //get filename
			std::string filename = line;
			
			getline(in, line); //get password
			std::string password = line;
			
			getline(in, line); //get xmlfilename
			std::string xml_filename = line;			
			
			//Create map entry for acquired info
			std::pair <std::string,std::string> temp (xml_filename, password);
			
			//Insert info into map
			files.insert(std::pair<std::string,std::pair<std::string, std::string> > 
				(filename, temp));
		}
		std::cout << "Done populating spreadsheet map." << std::endl;
		 
		//close file
		in.close();
		//Start accepting new connections
		start_accept();
	}

private:
	
	/*
	*
	*
	*/
	void start_accept()
	{
	//Create object for newly connected socket
	tcp_connection::pointer new_connection =
		tcp_connection::create(acceptor_.get_io_service());
	  
	std::cout << "Now accepting connections.\n" << std::endl;
	  
	//Accept the new socket connection
	acceptor_.async_accept(new_connection->socket(),
		boost::bind(&tcp_server::handle_accept, this, new_connection,
			boost::asio::placeholders::error));
	}

	/* 
	 *
	 *
	 */
	void handle_accept(tcp_connection::pointer new_connection, const boost::system::error_code& error)
	{
		//Debugging information
		std::cout << "Processing new connection." << std::endl;

		if(error)
		{
			std::cout << "Error encountered in handle_accept." << std::endl;
			std::cout << "Exitting." << std::endl;
			return;
		}

		//If we dont have an error
		if (!error)
		{
			char* m_RecieveBuffer = new char[256];

			//Start the received connection
			new_connection->socket().async_receive(boost::asio::buffer(m_RecieveBuffer, 255),
				boost::bind(&tcp_server::server_handle_read, 
				this,
				new_connection,
				m_RecieveBuffer, 
				boost::asio::placeholders::error)); 
		}
		
		std::cout << "Finished processing connection." << std::endl;

		//Begin accepting sockets again
		start_accept();
	}
	
	/*
	 *
	 *
	 */
	void server_handle_read(tcp_connection::pointer connection, char* buffer, const boost::system::error_code& error_code)
	{
		std::cout << "Processing received data." << std::endl;		
		std::cout << "\nThe server received:\n" << buffer << std::endl;
		
		
		
		
		if(error_code)
		{
			std::cout << "Error encountered in handle_read." << std::endl;	
			std::cout << "Exitting." << std::endl;
			
			return;
		}
		
		if(!error_code)
		{
			//parse message out
			//debug test the message received		
			std::string str(buffer);
			std::istringstream is( buffer );

			std::string line;
			
			getline(is, line);						
			
			if(line == "CREATE")
			{
				std::cout << "Processing CREATE command." << std::endl;
				create_received(connection, str);
	
				
				char* m_RecieveBuffer = new char[256];

				//Start the received connection
				connection->socket().async_receive(boost::asio::buffer(m_RecieveBuffer, 255),
						boost::bind(&tcp_server::server_handle_read, 
						this,
						connection,
						m_RecieveBuffer, 
						boost::asio::placeholders::error)); 	
	
	

			}
			else if(line == "JOIN")
			{
				std::cout << "Processing JOIN command." << std::endl;
				join_received(connection, str);		
			}		
			else
			{
				std::cout << "Error: Unexpected message encountered." << std::endl;
				//if they don't send join or create, server sends ERROR
				std::string message = "ERROR\n";
				send_message(connection, message);
				
				
				char* m_RecieveBuffer = new char[256];

				//Start the received connection
				connection->socket().async_receive(boost::asio::buffer(m_RecieveBuffer, 255),
						boost::bind(&tcp_server::server_handle_read, 
						this,
						connection,
						m_RecieveBuffer, 
						boost::asio::placeholders::error)); 	


			}
		}

		
		

		delete buffer;
	}
	
	void create_received(tcp_connection::pointer connection, std::string received)
	{		
		//break up 
		std::istringstream is( received);		
		std::string line = "";
		getline(is, line);  //this removes the CREATE
		getline(is, line); //this get's NAME:name
		std::string filename = line.substr(5, line.length() - 5);  //get file name
		
		//get password
		getline(is, line); //this get's PASSWORD:password
		std::string password = line.substr(9, line.length() - 9);  //get password
		
		//make sure file doesn't already exist
		//check map
		std::map<std::string, std::pair<std::string,std::string> >::iterator it;
		 
		mtx_.lock();
		it = files.find(filename);
		mtx_.unlock();
		
		//if valid file already exist send error
		if(it != files.end())
		{
			std::string message = "CREATE FAIL\nName : " + filename + "\nfile already exists\n";
			
			send_message(connection, message);
		}
		//File doesn't exist
		else
		{
			//add xml
			mtx_.lock();
			int file_count = files.size() + 1;
			mtx_.unlock();

			std::ostringstream oss;
			oss << file_count;

			std::string xml_name = "xml" + oss.str() + ".xml";
			
			std::string data = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<spreadsheet>\r\n</spreadsheet>";
			std::fstream file;
			
			//ios::app open files to write and adds to end of file
			//ios::noreplace creates a new file if it does not exist
			
			//convert xml_name to char*
			char *a=new char[xml_name.size()+1];
			a[xml_name.size()]=0;
			memcpy(a,xml_name.c_str(),xml_name.size());
			file.open (a, std::fstream::in | std::fstream::out | std::fstream::app);

			file << data;
			file.close();

			// add to end of spreadsheet.txt
			file.open("spreadsheet_files.txt", std::fstream::in | std::fstream::out | std::			fstream::app);
			
			data = "\n" + filename + "\n" + password + "\n" + xml_name + "\n";
			
			file << data;
			file.close();
			
			//add to map
			std::pair <std::string,std::string> temp (xml_name, password);
			
			mtx_.lock();
			files.insert(std::pair<std::string,std::pair<std::string, std::string> > 
				(filename, temp));
			mtx_.unlock();	
				
			//send message saying it was created
			std::string message = "CREATE OK\nName:" + filename + "\nPassword:" + password + "\n";
			send_message(connection,message);
		}
	}
	
	void join_received(tcp_connection::pointer connection, std::string received)
	{
		//break up 
		std::istringstream is( received);		
		std::string line = "";
		getline(is, line);  //this removes the join
		getline(is, line); //this get's NAME:name
		std::string filename = line.substr(5, line.length() - 5);  //get file name

		//get password
		getline(is, line); //this get's PASSWORD:password
		std::string password = line.substr(9, line.length() - 9);  //get password
		
		//two things can make join fail..password, file does not exist		
		//try to join where file does not exist
		std::string message;
		
		//Lock
		mtx_.lock();
		std::map<std::string, std::pair<std::string,std::string> >::iterator it;
		it = files.find(filename);
		//Unlock
		mtx_.unlock();
		
		//check to see if file exists
		if(it == files.end())
		{
		
			file_not_exist(connection, filename);
			return;
						

		}
		
		std::pair<std::string,std::string> temp = it->second;		
		std::string xml_file = temp.first;
		std::string saved_password = temp.second;
		
		//invalid password
		if(saved_password != password)
		{

			invalid_password(connection, filename);
			
			return;
		}
		
		//check to see if session is running
		std::map<std::string, spreadsheet_session::spreadsheet_session*>::iterator session_it;
		std::map<std::string, spreadsheet_session::spreadsheet_session*>::iterator session_end;
		
		mtx_.lock();
		session_it  = sessions.find(xml_file);
		session_end = sessions.end();		
		mtx_.unlock();
		
		//if(1)
		if(session_it != session_end )
		{
			//get session and add connection
			session_it->second->add_user(connection);
		}
		else //create new session
		{
		    mtx_.lock();
			boost::shared_ptr<boost::thread> thread(new boost::thread(	
							boost::bind(&tcp_server::create_thread, 
							this,
							filename,
							xml_file,
							connection)));

			mtx_.unlock();
		}
		
	}
	void file_not_exist(tcp_connection::pointer connection, std::string filename)
	{
		//file does not exist
		std::string	message = "JOIN FAIL\nName:" + filename + "\nFile does not exist.\n";
			send_message(connection, message);
			
			
		char* m_RecieveBuffer = new char[256];

		//Start the received connection
		connection->socket().async_receive(boost::asio::buffer(m_RecieveBuffer, 255),
				boost::bind(&tcp_server::server_handle_read, 
				this,
				connection,
				m_RecieveBuffer, 
				boost::asio::placeholders::error)); 	
	}
	
	void invalid_password(tcp_connection::pointer connection, std::string filename)
	{
			//file does not exist
		std::string message = "JOIN FAIL\nName:" + filename + "\nPassword is invalid.\n";
			send_message(connection, message);
			
			char* m_RecieveBuffer = new char[256];


			//Start the received connection
			connection->socket().async_receive(boost::asio::buffer(m_RecieveBuffer, 255),
					boost::bind(&tcp_server::server_handle_read, 
					this,
					connection,
					m_RecieveBuffer, 
					boost::asio::placeholders::error)); 	
	}
	
	void create_thread(std::string filename_, std::string xmlfile, tcp_connection::pointer connection_)
	{		
		spreadsheet_session::spreadsheet_session* temp_session;
		
		temp_session = new spreadsheet_session(filename_, xmlfile, connection_);
		
		//need lock
		mtx3_.lock();
		sessions.insert(std::pair<std::string, spreadsheet_session::spreadsheet_session*> (xmlfile, temp_session));
		mtx3_.unlock();		
		
		boost::signals2::connection  m_connection;
		m_connection = temp_session->connect(boost::bind(&tcp_server::close_session, this, xmlfile, m_connection));
		
		
	}
	
	void close_session(std::string xmlfile, boost::signals2::connection m_connection)
	{
		std::cout << "Closing the session." << std::endl;
		std::map<std::string,spreadsheet_session::spreadsheet_session*>::iterator it;
		
		
		it = sessions.find(xmlfile);
		delete it->second;	
		sessions.erase(it);
		m_connection.disconnect();
	}

	void send_message(tcp_connection::pointer connection, std::string message)
	{
		std::cout << "\nSending message:\n" << message << std::endl;

		//Send message to socket
		boost::asio::async_write(connection->socket(), boost::asio::buffer(message),
			boost::bind(&tcp_server::handle_write, 
			this,
			boost::asio::placeholders::error,
			boost::asio::placeholders::bytes_transferred, 
			connection));
			
			

	}

	void handle_write(const boost::system::error_code& /*error*/ e,
		size_t /*bytes_transferred*/, tcp_connection::pointer connection)
	{
		std::cout << "Finished sending message." << std::endl;
		
		if(e);
		{
			std::cout << "e: " << e << std::endl;
			std::cout << "Error occured while sending the message." << std::endl;
			return;
		}

	}


	boost::thread_group tgroup;
	//used for locks
	boost::mutex mtx_;
	boost::mutex mtx2_;
	boost::mutex mtx3_;
	//first string will be file name, the pair contains the xml and password
	std::map<std::string, std::pair<std::string,std::string> > files;
	//key will be file name, spreadsheet session is the running session
	//std::map<  std::pair< std::string, spreadsheet_session::spreadsheet_session> > sessions;
	std::map<std::string,spreadsheet_session::spreadsheet_session*> sessions;
	tcp::acceptor acceptor_;
};

/* Main entry for server. Starts the server listening on port 1984.
 * Reports any errors to the console.
 */
int main()
{
  try
  {
	//Debugging information
	std::cout << "CS3505 Final Project - Spring 2013" << std::endl;
	std::cout << "Created By: Zach Wilcox, Thomas Gonsor, Skyler Chase, Michael Quigley" << std::endl;
	std::cout << "-----Starting the Server-----" << std::endl;

	//Declare io_service object
    boost::asio::io_service io_service;

    tcp_server server(io_service);

	//Tell the io_service object to begin
    io_service.run();

  }
  catch (std::exception& e)
  {
	//Report any errors
    std::cerr << e.what() << std::endl;
  }

  return 0;
}  