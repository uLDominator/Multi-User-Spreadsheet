using System;
using System.Text;
using System.Net.Sockets;
using SpreadsheetUtilities;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpreadsheetModel
{
    /// <summary>
    /// This class is the model for a spreadsheet object. 
    /// It handles connections to the server and parses out the commands.
    /// </summary>
    public class SpreadsheetClientModel
    {
        // The socket used to communicate with the server.  
        // If no connection has been made yet, this is null.
        private StringSocket _socket;

        // The TCP Client
        private TcpClient _client;
        // Register for this event to be notified when a line of text arrives.
        public event Action<ArrayList> IncomingLineEvent;
        // used to keep track of received commands and lines
        ArrayList lines = new ArrayList();
        //global string used for storing received xml
        String globalXML = "";
        object lockObj = new object();

       /// <summary>
       /// Default constructor for client model
       /// </summary>
        public SpreadsheetClientModel()
        { 
            _socket = null;
        }

        /// <summary>
        /// Connects to the server at the given hostname. Port is 1984.
        /// </summary>
        public void Connect(string hostname)
        {
            try
            {
                if (_socket != null)
                    return;
                //instantiate client on port 1984
                _client = new TcpClient(hostname, 1984);
                _socket = new StringSocket(_client.Client, UTF8Encoding.Default);
                //start receiving
                _socket.BeginReceive(LineReceived, null);
            }
            catch (Exception e)
            {
                throw new Exception("Error connecting to SpreadsheetClientModel" + e);
            }
        }

        /// <summary>
        /// Handles an arriving line of text and parses it out to send to the event handler.
        /// </summary>
        private void LineReceived(String s, Exception e, object p)
        {
            lock (lockObj)
            {
                //start receiving again
                _socket.BeginReceive(LineReceived, null);

                //string that preserves newline character and whitespace. Used to keep track of the expected and actual length of xml documents
                String str = "";
                bool goodToGo = false;

                if (s == null)
                    return;

                // list used for storing return values and sending them to the event listener
                ArrayList a = new ArrayList();

                str = s;
                // Trim any whitespace off of ends of string (string that doesn't preserve newlines or whitespace).
                s = s.Trim();

                //if s is a command, then clear the current arraylist of lines
                if (isCommand(s))
                { lines.Clear(); }

                // add current line to list of lines
                lines.Add(s);

                // if the first line is a command and the appropriate number of lines is acquired, then execute that command and clear the arraylist
                if (isCommand((String)lines[0]))
                {
                    // these are the checks for specific tags
                    if (lines[0].Equals("CREATE OK") && lines.Count == 3)
                    { a = doCreate(lines, true); goodToGo = true; }

                    if (lines[0].Equals("CREATE FAIL") && lines.Count == 3)
                    { a = doCreate(lines, false); goodToGo = true; }

                    //dont forget to change goodToGo
                    if (lines[0].Equals("JOIN OK") && lines.Count == 5)
                    {
                        int length;
                        Match match = Regex.Match(lines[3].ToString(), @"Length:([ !-~]*)", RegexOptions.None);

                        // remove last element since it is just the header...
                        lines.RemoveAt(lines.Count - 1);

                        //parse length of xml from 3rd received line...
                        int.TryParse(match.Groups[1].Value, out length);
                        // as long as the xml string hasn't been comletely built, add the current line to it
                        if (globalXML.Length < length)
                        {
                            globalXML = globalXML + str;
                        }

                        if (globalXML.Length >= length - 1)
                        {

                            //add xml document
                            lines.Add(globalXML);
                            //clear string so that it can be reused
                            globalXML = String.Empty;
                            // parse and pass to arraylist
                            a = doJoin(lines, true); goodToGo = true;
                        }
                    }

                    if (lines[0].Equals("JOIN FAIL") && lines.Count == 3)
                    { a = doJoin(lines, false); goodToGo = true; }

                    if (lines[0].Equals("CHANGE OK") && lines.Count == 3)
                    { a = doChange(lines, "OK"); goodToGo = true; }

                    if (lines[0].Equals("CHANGE FAIL") && lines.Count == 3)
                    { a = doChange(lines, "FAIL"); goodToGo = true; }

                    if (lines[0].Equals("CHANGE WAIT") && lines.Count == 3)
                    { a = doChange(lines, "WAIT"); goodToGo = true; }

                    if (lines[0].Equals("UNDO OK") && lines.Count == 6)
                    { a = doUndo(lines, "OK"); goodToGo = true; }

                    if (lines[0].Equals("UNDO END") && lines.Count == 3)
                    { a = doUndo(lines, "END"); goodToGo = true; }

                    if (lines[0].Equals("UNDO FAIL") && lines.Count == 4)
                    { a = doUndo(lines, "FAIL"); goodToGo = true; }

                    if (lines[0].Equals("UNDO WAIT") && lines.Count == 3)
                    { a = doUndo(lines, "WAIT"); goodToGo = true; }

                    if (lines[0].Equals("UPDATE") && lines.Count == 6)
                    { a = doUpdate(lines); goodToGo = true; }

                    if (lines[0].Equals("SAVE OK") && lines.Count == 2)
                    { a = doSave(lines, true); goodToGo = true; }

                    if (lines[0].Equals("SAVE FAIL") && lines.Count == 3)
                    { a = doSave(lines, false); goodToGo = true; }

                    if (lines[0].Equals("ERROR") && lines.Count == 1)
                    {
                        a = new ArrayList();
                        a.Add("ERROR");
                        goodToGo = true;
                    }
                    //if there is a valid event and a command waiting to be parsed, then pass it to the event handler
                    if (IncomingLineEvent != null && goodToGo)
                    {
                        //lines are cleared to start the process over
                        goodToGo = false;
                        IncomingLineEvent(a);
                        lines.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        public void Disconnect()
        {
            if (_socket != null)
                _socket.Close();

            if (_client != null)
                _client.Close();
        }


      
        /// <summary>
        /// Checks to see whether input line is a command.
        /// </summary>
        /// <param name="line"> Represents a current line of text</param>
        /// <returns></returns>
        bool isCommand(String line)
        {
            Match match = Regex.Match(line, @"^((CREATE)|(JOIN)|(CHANGE)|(UNDO)|(UPDATE)|(SAVE)|(ERROR))",RegexOptions.None);
            return match.Success;
        }



        #region Command Methods

        /// <summary>
        /// Sends a CREATE message to the server.
        /// </summary>
        public void Create(string name, string password)
        {
            if (_socket == null || name == null || password == null) 
                return;

            _socket.BeginSend("CREATE\nName:" + name + "\nPassword:"+password + "\n", (e, p) => { }, null);
        }

        /// <summary>
        /// Sends a JOIN message to the server.
        /// </summary>
        public void Join(string name, string password)
        {
            if (_socket == null || name == null || password == null)
                return;

            _socket.BeginSend("JOIN\nName:" + name + "\nPassword:" + password + "\n", (e, p) => { }, null);
        }

        /// <summary>
        /// Send CHANGE request to server
        /// </summary>
        public void Change(string name, int version, string cell, int length, string content)
        {
            lock (lockObj)
            {
                if (_socket == null || name == null || cell == null || content == null)
                    return;

                _socket.BeginSend("CHANGE\nName:" + name + "\nVersion:" + version + "\nCell:" + cell + "\nLength:" + length + "\n"
                    + content + "\n", (e, p) => { }, null);
            }
        }

        /// <summary>
        /// Send UNDO request to server 
        /// </summary>
        public void Undo(string name, int version)
        {
            if (_socket == null || name == null)
                return;

            _socket.BeginSend("UNDO\nName:"+name + "\nVersion:" + version + "\n", (e, p) => { }, null);
        }

        /// <summary>
        /// Send SAVE request to server 
        /// </summary>
        public void Save(string name)
        {
            if (_socket == null || name == null)
                return;

            _socket.BeginSend("SAVE\nName:"+name + "\n", (e, p) => { }, null);
        }

        /// <summary>
        /// Send LEAVE request to server
        /// </summary>
        public void Leave(string name)
        {
            if (_socket == null || name == null)
                return;

            _socket.BeginSend("LEAVE\nName:"+name+"\n", (e, p) => { }, null);
        }

        #endregion
        
        #region Receive Data Methods


        // These methods deal with information once it is received from the server.
 

        /// <summary>
        /// This method deals with a CREATE command
        /// This method also parses off any newline characters.
        /// a[0] = CREATE
        /// a[1] = True/False
        /// a[2] = name
        /// a[3] = either "password" or "some error message" depending on whether CREATE OK or CREATE FAIL was received
        /// </summary>
        private ArrayList doCreate(ArrayList lines, bool ok)
        {
            ArrayList a = new ArrayList();
            a.Add("CREATE");
            a.Add(ok.ToString());

            //adds name
            Match match = Regex.Match(lines[1].ToString(), @"Name:([ !-~]*)", RegexOptions.None);
            a.Add(match.Groups[1].Value);

            //if CREATE OK was received, then add the password
            if (ok)
            {
                match = Regex.Match(lines[2].ToString(), @"Password:([ !-~]*)", RegexOptions.None);
                a.Add(match.Groups[1].Value);
            }

            // else, add the error message
            else
            {
                match = Regex.Match(lines[2].ToString(), @".*", RegexOptions.None);
                a.Add(match.ToString());
            }

            return a;
        }


        /// <summary>
        /// This method deals with a JOIN command
        /// This method also parses off any newline characters.
        /// a[0] = JOIN
        /// a[1] = True/False
        /// a[2] = name
        /// 
        /// if JOIN OK
        /// a[3] = version
        /// a[4] = length of XML document in characters
        /// a[5] = xml document
        /// 
        /// if JOIN FAIL
        /// a[3] = message
        /// </summary>
        private ArrayList doJoin(ArrayList lines, bool ok)
        {

            ArrayList a = new ArrayList();
            a.Add("JOIN");
            a.Add(ok.ToString());

            //adds name [a-zA-Z]+
            Match match = Regex.Match(lines[1].ToString(), @"Name:([ !-~]*)", RegexOptions.None);
            a.Add(match.Groups[1].Value);
            
            if (ok)
            {
                //adds version [0..9]+
                match = Regex.Match(lines[2].ToString(), @"Version:([ !-~]*)", RegexOptions.None);
                a.Add(match.Groups[1].Value);
                // adds the length [0..9]+
                match = Regex.Match(lines[3].ToString(), @"Length:([ !-~]*)", RegexOptions.None);
                a.Add(match.Groups[1].Value);
                // adds the xml spreadsheet
                match = Regex.Match(lines[4].ToString(), @".*", RegexOptions.None);
                a.Add(match.ToString());
            }

            else
            {
                a.Add(lines[2].ToString());
            }

         

            return a;
        }


        /// <summary>
        ///This method deals with a CHANGE command
        ///a[0] = CHANGE
        ///a[1] = OK/FAIL/WAIT
        ///a[2] = name
        ///
        ///ifCHANGE WAIT or CHANGE OK
        ///a[3] = version
        ///
        /// if CHANG FAIL
        /// a[3] = error message
        /// </summary>
        private ArrayList doChange(ArrayList lines, String state)
        {
            lock (lockObj)
            {
                ArrayList a = new ArrayList();
                a.Add("CHANGE");
                a.Add(state);

                //adds name
                Match match = Regex.Match(lines[1].ToString(), @"Name:([ !-~]*)", RegexOptions.None);
                a.Add(match.Groups[1].Value);


                if (state.Equals("FAIL"))
                {
                    //adds error message
                    match = Regex.Match(lines[2].ToString(), @".*", RegexOptions.None);
                    a.Add(match.ToString());
                }
                else
                {

                    //adds version
                    match = Regex.Match(lines[2].ToString(), @"Version:([ !-~]*)", RegexOptions.None);
                    a.Add(match.Groups[1].Value);
                }

                return a;
            }
        }

        /// <summary>
        ///This method deals with a UNDO command
        ///a[0] = UNDO
        ///a[1] = OK/END/FAIL/WAIT
        ///a[2] = name
        ///a[3] = version
        ///
        ///if
        ///UNDO OK
        ///a[4] = cell
        ///a[5] = length of content in characters
        ///a[6] = content
        ///
        ///else
        ///do nothing but return
        /// </summary>
        private ArrayList doUndo(ArrayList lines, String state)
        {
            ArrayList a = new ArrayList();
            a.Add("UNDO");
            a.Add(state);

            //adds name
            Match match = Regex.Match(lines[1].ToString(), @"Name:([ !-~]*)", RegexOptions.None);
            a.Add(match.Groups[1].Value);
            //adds version
            if (!state.Equals("FAIL"))
            {
                match = Regex.Match(lines[2].ToString(), @"Version:([ !-~]*)", RegexOptions.None);
                a.Add(match.Groups[1].Value);
            }

            if (state.Equals("OK"))
            {
                match = Regex.Match(lines[3].ToString(), @"Cell:([ !-~]*)", RegexOptions.None);
                a.Add(match.Groups[1].Value);

                match = Regex.Match(lines[4].ToString(), @"Length:([ !-~]*)", RegexOptions.None);
                a.Add(match.Groups[1].Value);
                //adds content
                match = Regex.Match(lines[5].ToString(), @".*", RegexOptions.None);
                a.Add(match.ToString());

            }
            if (state.Equals("FAIL"))
            {
                match = Regex.Match(lines[2].ToString(), @".*", RegexOptions.None);
                a.Add(match.ToString());
            }

            return a;
        }


        /// <summary>
        /// This method deals with a UPDATE command
        /// a[0] = UPDATE
        /// a[1] = name
        /// a[2] = version
        /// a[3] = cell
        /// a[4] = length of content is characters
        /// a[5] = content for cell
        /// </summary>
        private ArrayList doUpdate(ArrayList lines)
        {
            ArrayList a = new ArrayList();

            a.Add("UPDATE");
            //adds name
            Match match = Regex.Match(lines[1].ToString(), @"Name:([ !-~]*)", RegexOptions.None);
            a.Add(match.Groups[1].Value);
            //adds version
            match = Regex.Match(lines[2].ToString(), @"Version:([ !-~]*)", RegexOptions.None);
            a.Add(match.Groups[1].Value);
            //adds cell
            match = Regex.Match(lines[3].ToString(), @"Cell:([ !-~]*)", RegexOptions.None);
            a.Add(match.Groups[1].Value);
            //adds length
            match = Regex.Match(lines[4].ToString(), @"Length:([ !-~]*)", RegexOptions.None);
            a.Add(match.Groups[1].Value);
            //adds content
            match = Regex.Match(lines[5].ToString(), @".*", RegexOptions.None);
            a.Add(match.ToString());

            return a;
        }

        /// <summary>
        /// This method deals with the SAVE command
        /// a[0] = SAVE
        /// a[1] = True/False
        /// a[2] = name
        /// 
        /// if SAVE FAIL
        /// a[3] = message
        /// </summary>
        private ArrayList doSave(ArrayList lines, bool ok)
        {
            ArrayList a = new ArrayList();

            a.Add("SAVE");
            a.Add(ok.ToString());
            //adds name
            Match match = Regex.Match(lines[1].ToString(), @"Name:([ !-~]*)", RegexOptions.None);
            a.Add(match.Groups[1].Value);
            // if SAVE FAIL then add message to list
            if (!ok)
            {
                match = Regex.Match(lines[2].ToString(), @".*", RegexOptions.None);
                a.Add(match.ToString());
            }

            return a;
        }
        #endregion
    }
}
