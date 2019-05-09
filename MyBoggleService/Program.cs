
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CustomNetworking;

// FROM HIS EXAMPLES
//https://github.com/UofU-CS3500-S19/examples/blob/e097c9a7e6a368cca9ce4c4c703441860e4452ee/Sockets/ChatServer1/SimpleChatServer.cs
namespace MyBoggleService
{
    class Program
    {
        private StringSocketListener server;
        private static ValuesController values;

        static void Main(string[] args)
        {
            new Program();
            Console.ReadLine();
        }


        /// <summary>
        /// Creates a SimpleChatServer that listens for connection requests on port 4000.
        /// </summary>
        public Program()
        {

            values = new ValuesController();


            // A TcpListener listens for incoming connection requests
            server = new StringSocketListener(60000, Encoding.UTF8);

            // Start the TcpListener
            server.Start();

            // Ask the server to call ConnectionRequested at some point in the future when 
            // a connection request arrives.  It could be a very long time until this happens.
            // The waiting and the calling will happen on another thread.  BeginAcceptSocket 
            // returns immediately, and the constructor returns to Main.
            server.BeginAcceptStringSocket(ConnectionRequested, null);
        }

        /// <summary>
        /// This is the callback method that is passed to BeginAcceptSocket.  It is called
        /// when a connection request has arrived at the server.
        /// </summary>
        private void ConnectionRequested(StringSocket socket, object payload)
        {
            // We obtain the socket corresonding to the connection request.  Notice that we
            // are passing back the IAsyncResult object.
            StringSocketListener s = (StringSocketListener)payload;
          

            // We ask the server to listen for another connection request.  As before, this
            // will happen on another thread.
            server.BeginAcceptStringSocket(ConnectionRequested, null);

            // We create a new ClientConnection, which will take care of communicating with
            // the remote client.
            new ClientConnection(socket);
        }
    }
}
