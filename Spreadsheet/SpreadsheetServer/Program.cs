using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SpreadsheetUtilities;

namespace SpreadsheetServer
{
    /// <summary>
    /// Server that waits for clients to connect to use spreadsheets.
    /// </summary>
    public class SpreadsheetServer
    {
        private const int Port = 2000;  // Required port number
        private readonly TcpListener _server; // Underlying TCP server that handles requests

        /// <summary>
        /// Start the Spreadsheet Server
        /// </summary>
        public static void Main(string[] args)
        {
            // Run the Spreadsheet Server. If any errors are thrown indicate it on the console.
            try
            {
                new SpreadsheetServer();
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Creates a Spreadsheet Server that listens for connections on the provided port.
        /// </summary>
        public SpreadsheetServer()
        {
            _server = new TcpListener(IPAddress.Any, Port);
            _server.Start();
            _server.BeginAcceptSocket(ConnectionReceived, null);
        }

        /// <summary>
        /// Closes the server connection.
        /// </summary>
        public void CloseSpreadsheetServer()
        {
            if (_server != null) 
                _server.Stop();
        }

        /// <summary>
        /// Handles connection requests.
        /// </summary>
        private void ConnectionReceived(IAsyncResult ar)
        {
            try
            {
                Socket socket = _server.EndAcceptSocket(ar);
                var s = new StringSocket(socket, Encoding.Default);
                s.BeginReceive(UserReceived, s);

                //look for other users
                _server.BeginAcceptSocket(ConnectionReceived, null);
            }
            catch
            {
                // Don't shut down the server from client error
            }
        }

        /// <summary>
        /// Receives the name of the client and connects them to the given spreadsheet object.
        /// </summary>
        private void UserReceived(String line, Exception e, object p)
        {
            try
            {
                var ss = p as StringSocket;
                if (ss == null)
                    return;

                //lock (_users)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        ss.BeginReceive(UserReceived, ss);
                        return;
                    }
                    line = line.Trim().ToUpper();

                    // Check if command was valid
                    if (!line.StartsWith("CONNECT "))
                    {
                        ss.BeginSend("IGNORING " + line + "\n", (ee, pp) => { }, null);
                        ss.BeginReceive(UserReceived, ss);
                    }
                }
            }
            catch
            {
                // Don't shut down the server from client error
            }
        }
    }


}