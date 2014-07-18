// Written by Joe Zachary for CS 3500, November 2012

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace SpreadsheetUtilities
{
    /// <summary> 
    /// A StringSocket is a wrapper around a Socket.  It provides methods that
    /// asynchronously read lines of text (strings terminated by newlines) and 
    /// write strings. (As opposed to Sockets, which read and write raw bytes.)  
    ///
    /// StringSockets are thread safe.  This means that two or more threads may
    /// invoke methods on a shared StringSocket without restriction.  The
    /// StringSocket takes care of the synchonization.
    /// 
    /// Each StringSocket contains a Socket object that is provided by the client.  
    /// A StringSocket will work properly only if the client refains from calling
    /// the contained Socket's read and write methods.
    /// 
    /// If we have an open Socket s, we can create a StringSocket by doing
    /// 
    ///    StringSocket ss = new StringSocket(s, new UTF8Encoding());
    /// 
    /// We can write a string to the StringSocket by doing
    /// 
    ///    ss.BeginSend("Hello world", callback, payload);
    ///    
    /// where callback is a SendCallback (see below) and payload is an arbitrary object.
    /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
    /// successfully written the string to the underlying Socket, or failed in the 
    /// attempt, it invokes the callback.  The parameters to the callback are a
    /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
    /// the Exception that caused the send attempt to fail.
    /// 
    /// We can read a string from the StringSocket by doing
    /// 
    ///     ss.BeginReceive(callback, payload)
    ///     
    /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
    /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
    /// string of text terminated by a newline character from the underlying Socket, or
    /// failed in the attempt, it invokes the callback.  The parameters to the callback are
    /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
    /// string or the Exception will be non-null, but nor both.  If the string is non-null, 
    /// it is the requested string (with the newline removed).  If the Exception is non-null, 
    /// it is the Exception that caused the send attempt to fail.
    /// </summary>

    public class StringSocket
    {
        // These delegates describe the callbacks that are used for sending and receiving strings.
        public delegate void SendCallback(Exception e, object payload);
        public delegate void ReceiveCallback(String s, Exception e, object payload);

        // Contains information about a single send request
        private struct SendRequest
        {
            public string Text { get; set; }
            public SendCallback Callback { get; set; }
            public object Payload { get; set; }
        }

        // Contains information about a single receive request
        private struct ReceiveRequest
        {
            public ReceiveCallback Callback { get; set; }
            public object Payload { get; set; }
        }

        // Underlying socket
        private readonly Socket _socket;

        // Information about strings that are waiting to be sent
        private readonly Queue<SendRequest> _sendRequests;

        // Information about strings that are waiting to be received
        private readonly Queue<ReceiveRequest> _receiveRequests;

        // Encoding to convert bytes to strings
        private readonly Encoding _encoding;

        // Array used to send bytes to the underlying socket
        private byte[] _sendBytes;

        // Number of bytes sent to the underlying socket during the current send attempt
        private int _sendCount;

        // Array used to receive bytes from the underlying socket
        private readonly byte[] _receiveBytes;

        // Incomplete received line
        private String _incompleteLine;

        // Complete lines of text received but not yet send to callbacks
        private readonly Queue<string> _receivedLines;


        /// <summary>
        /// Creates a LineSocket from a regular Socket, which should already be connected.  
        /// The read and write methods of the regular Socket must not be called after the
        /// LineSocket is created.  Otherwise, the LineSocket will not behave properly.  
        /// The encoding to use to convert between raw bytes and strings is also provided.
        /// </summary>
        public StringSocket(Socket s, Encoding e)
        {
            _socket = s;
            _encoding = e;
            _sendRequests = new Queue<SendRequest>();
            _receiveRequests = new Queue<ReceiveRequest>();
            _incompleteLine = "";
            _receiveBytes = new byte[1024];
            _receivedLines = new Queue<string>();
        }

        /// <summary>
        /// Closes the StringSocket
        /// </summary>
        public void Close()
        {
            if (_socket == null) 
                return;

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        /// <summary>
        /// We can write a string to a StringSocket ss by doing
        /// 
        ///    ss.BeginSend("Hello world", callback, payload);
        ///    
        /// where callback is a SendCallback (see below) and payload is an arbitrary object.
        /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
        /// successfully written the string to the underlying Socket, or failed in the 
        /// attempt, it invokes the callback.  The parameters to the callback are a
        /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
        /// the Exception that caused the send attempt to fail. 
        /// 
        /// This method is non-blocking.  This means that it does not wait until the string
        /// has been sent before returning.  Instead, it arranges for the string to be sent
        /// and then returns.  When the send is completed (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginSend
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginSend must take care of synchronization instead.  On a given StringSocket, each
        /// string arriving via a BeginSend method call must be sent (in its entirety) before
        /// a later arriving string can be sent.
        /// </summary>
        public void BeginSend(String s, SendCallback callback, object payload)
        {
            // Add the request to the queue and launch the sending process if the queue
            // was previously empty.  Otherwise, the sending process is in process and
            // this new entry will be dealt with.
            lock (_sendRequests)
            {
                _sendRequests.Enqueue(new SendRequest { Text = s, Callback = callback, Payload = payload });
                if (_sendRequests.Count == 1)
                {
                    ProcessSendQueue();
                }
            }
        }

        /// <summary>
        /// This should be called only after a lock on sendRequests has been acquired.
        /// It pings back and forth with the BytesSent callback to send out all the strings in
        /// the queue.  This method gets the string at the front of the queue and attempts
        /// to send it.  BytesSent takes care of making sure all of the bytes are actually sent
        /// before calling this method again to send the next string. 
        /// </summary>
        private void ProcessSendQueue()
        {
            while (_sendRequests.Count > 0)
            {
                _sendBytes = _encoding.GetBytes(_sendRequests.First().Text);
                try
                {
                    _socket.BeginSend(_sendBytes, _sendCount = 0, _sendBytes.Length, SocketFlags.None, BytesSent, null);
                    break;
                }
                catch (Exception e)
                {
                    SendRequest req = _sendRequests.Dequeue();
                    ThreadPool.QueueUserWorkItem(x => req.Callback(e, req.Payload));
                }
            }
        }

        /// <summary>
        /// This method is the callback used when bytes are being sent.  It makes sure that all of
        /// the bytes have been sent, then calls the appropriate callback and calls ProcessSendQueue.
        /// </summary>
        private void BytesSent(IAsyncResult ar)
        {
            try
            {
                // Compute how many bytes have been sent so far
                _sendCount += _socket.EndSend(ar);
            }
            catch (Exception e)
            {
                SendRequest req = _sendRequests.Dequeue();
                ThreadPool.QueueUserWorkItem(x => req.Callback(e, req.Payload));
                ProcessSendQueue();
                return;
            }

            // If all the bytes were sent, remove the request from the queue, notify the
            // callback, and process the next entry in the send queue.
            if (_sendCount == _sendBytes.Length)
            {
                lock (_sendRequests)
                {
                    SendRequest req = _sendRequests.Dequeue();
                    ThreadPool.QueueUserWorkItem(x => req.Callback(null, req.Payload));
                    ProcessSendQueue();
                }
            }

            // If all the bytes weren't sent, send the rest.
            else
            {
                try
                {
                    _socket.BeginSend(_sendBytes, _sendCount, _sendBytes.Length - _sendCount, SocketFlags.None, BytesSent, null);
                }

                catch (Exception e)
                {
                    SendRequest req = _sendRequests.Dequeue();
                    ThreadPool.QueueUserWorkItem(x => req.Callback(e, req.Payload));
                    ProcessSendQueue();
                }
            }

        }


        /// <summary>
        /// We can read a string from the StringSocket by doing
        /// 
        ///     ss.BeginReceive(callback, payload)
        ///     
        /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
        /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
        /// string of text terminated by a newline character from the underlying Socket, or
        /// failed in the attempt, it invokes the callback.  The parameters to the callback are
        /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
        /// string or the Exception will be null, or possibly boh.  If the string is non-null, 
        /// it is the requested string (with the newline removed).  If the Exception is non-null, 
        /// it is the Exception that caused the send attempt to fail.  If both are null, this
        /// indicates that the sending end of the remote socket has been shut down.
        /// 
        /// This method is non-blocking.  This means that it does not wait until a line of text
        /// has been received before returning.  Instead, it arranges for a line to be received
        /// and then returns.  When the line is actually received (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginReceive
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginReceive must take care of synchronization instead.  On a given StringSocket, each
        /// arriving line of text must be passed to callbacks in the order in which the corresponding
        /// BeginReceive call arrived.
        /// 
        /// Note that it is possible for there to be incoming bytes arriving at the underlying Socket
        /// even when there are no pending callbacks.  StringSocket implementations should refrain
        /// from buffering an unbounded number of incoming bytes beyond what is required to service
        /// the pending callbacks.
        /// </summary>
        public void BeginReceive(ReceiveCallback callback, object payload)
        {
            // Add the request to the queue, then start the receiving process if the queue
            // was previously empty.
            lock (_receiveRequests)
            {
                _receiveRequests.Enqueue(new ReceiveRequest { Callback = callback, Payload = payload });
                if (_receiveRequests.Count == 1)
                {
                    ProcessReceiveQueue();
                }
            }
        }

        /// <summary>
        /// This  tries to fill requests with text on hand and then, if there are still
        /// requests remaining, requests more data from the underlying socket.
        /// </summary>
        private void ProcessReceiveQueue()
        {
            lock (_receiveRequests)
            {
                // For each complete line of text, invoke the corresponding callback.
                while (_receivedLines.Any() && _receiveRequests.Any())
                {
                    String line = _receivedLines.Dequeue();
                    ReceiveRequest req = _receiveRequests.Dequeue();
                    ThreadPool.QueueUserWorkItem(x => req.Callback(line, null, req.Payload));
                }

                // If any unserviced requests remain, request more bytes.
                while (_receiveRequests.Count > 0)
                {
                    try
                    {
                        _socket.BeginReceive(_receiveBytes, 0, _receiveBytes.Length, SocketFlags.None, BytesReceived, null);
                        break;
                    }
                    catch (Exception e)
                    {
                        ReceiveRequest req = _receiveRequests.Dequeue();
                        ThreadPool.QueueUserWorkItem(x => req.Callback(null, e, req.Payload));
                        _incompleteLine = "";
                    }
                }
            }
        }

        /// <summary>
        /// This private method is the callback for the receive attempts.
        /// </summary>
        private void BytesReceived(IAsyncResult ar)
        {
            // Get the number of bytes received.  
            int count;
            try
            {
                count = _socket.EndReceive(ar);
            }
            catch (Exception e)
            {
                ReceiveRequest req = _receiveRequests.Dequeue();
                ThreadPool.QueueUserWorkItem(x => req.Callback(null, e, req.Payload));
                ProcessReceiveQueue();
                _incompleteLine = "";
                return;
            }

            // If no bytes were received, this means that the remote socket has
            // shut down.  Send a null to the callback to signal this.
            if (count == 0)
            {
                _receivedLines.Enqueue(null);
                ProcessReceiveQueue();
            }

            // If bytes were received, save them.
            else
            {
                _incompleteLine += _encoding.GetString(_receiveBytes, 0, count);

                // Extract all complete lines of text and put into the ReceivedLines queue
                int lineEnd, lineStart = 0;
                while ((lineEnd = _incompleteLine.IndexOf('\n', lineStart)) >= 0)
                {
                    _receivedLines.Enqueue(_incompleteLine.Substring(lineStart, lineEnd - lineStart));
                    lineStart = lineEnd + 1;
                }
                _incompleteLine = _incompleteLine.Substring(lineStart);

                // Try to fill requests with the new data
                ProcessReceiveQueue();
            }
        }
    }
}