// Authors Jared Nay and Andrew Fryzel
// CS 3500 University of Utah
// April 22, 2019

using CustomNetworking;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace MyBoggleService
{
    /// <summary>
    /// Socket class
    /// Most taken from Joseph Zackary examples https://github.com/UofU-CS3500-S19/examples/blob/master/Sockets/ChatServer2/SimpleChatServer.cs
    /// </summary>
    class ClientConnection
    {
        // Incoming/outgoing is UTF8-encoded.  This is a multi-byte encoding.  The first 128 Unicode characters
        // (which corresponds to the old ASCII character set and contains the common keyboard characters) are
        // encoded into a single byte.  The rest of the Unicode characters can take from 2 to 4 bytes to encode.
        private static System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

        // The socket through which we communicate with the remote client

        private StringSocket socket;


        String temp;
        
        //Array of objects to store data
        //0 = request type (put,post,get,etc)
        //1 = gameid
        //2 = users / games
        //3 content length
        //4 request body
        private Object[] name = new Object[5];

        //Controller that can communicate with the database.
        private ValuesController values;


        /// <summary>
        /// Creates a ClientConnection from the socket, then begins communicating with it.
        /// </summary>
        public ClientConnection(StringSocket s)
        {
            //String stuff
            name[3] = 0;
            values = new ValuesController();
            this.socket = s;
            socket.BeginReceive(MessageReceived, socket);
        }

        /// <summary>
        /// Called when some data has been received.
        /// </summary>
        private void MessageReceived(String s, object payload)
        {
            //Console.WriteLine(name[0]);
            //Console.Write(s);
            Regex contentLength = new Regex(@"^Content-Length: (\d+)");

            // This is our main goal. Want to hit this so we can do stuff. 
            // When we hit /r we go into WhatIsIt
            if (s.Trim().Length == 0  && (int)name[3] > 0)
            {
                ((StringSocket)payload).BeginReceive(WhatIsIt, payload, (int)name[3]);
            }

            else if (s.Trim().Length == 0)
            {
                WhatIsIt(null, payload);
            }

            // on the second go around start doing more stuff
            else if (name[0] != null)
            {
                //Determine the content length
                Match m = contentLength.Match(s);

                if (m.Success)
                {
                    // the content length is stored in name[3]
                    name[3] = int.Parse(m.Groups[1].ToString());
                }

                //Think of these methods as recursion. Will call MessageReceived afer begin receive
                ((StringSocket)payload).BeginReceive(MessageReceived, payload);
            }

            // gets the post, put, get, delete request then calls MessageReceived
            else
            {
                name[0] = s;
                ((StringSocket)payload).BeginReceive(MessageReceived, payload);

            }
        }


        /// <summary>
        /// Helper method determining what type of request
        /// </summary>
        /// <param name="s"></param>
        private void WhatIsIt(String s, object payload)
        {
            Regex r;
            Match m;

            string regex = "(?:(/BoggleService/))";
            String[] split = name[0].ToString().Split('/');

            // HTTP request
            // PUT POST GET
            if (name[0].ToString()[0] == 'P')
            {
                if (name[0].ToString()[1] == 'U')
                {//put request

                    if (split[2].EndsWith("HTTP"))
                    {
                        PostCancel(s, payload);
                    }
                    else
                    {
                        //play word
                        String brief = split[3].Split(' ')[0];

                        PostPlayWord(s, brief, payload);
                    }

                }
                else if (name[0].ToString()[1] == 'O')
                {//post request
                    int temp = regex.Length;

                    if (split[2].StartsWith("users"))
                    {
                        name[0] = "POST";
                        name[2] = "users";
                        PostRegister(s, payload);
                    }

                    else if (split[2].StartsWith("games"))
                    {
                        name[0] = "POST";
                        name[2] = "games";
                        PostJoinGame(s, payload);
                    }
                }
            }
            else 
            {
                //GET
                bool brief;
                string gameID = split[3];
                if (split[4].StartsWith("False"))
                {
                    brief = false;
                }
                else
                {
                    brief = true;
                }

                PostGameStatus(gameID, brief, payload);
            }
        }

        /// <summary>
        /// Registers a user to the database
        /// </summary>
        /// <param name="temp"></param>
        private void PostRegister(String temp, object payload)
        {
            UserInfo user = new UserInfo();
            user.Nickname = JsonConvert.DeserializeObject<String>(temp);
            Token token = new Token();


            StringBuilder build = new StringBuilder();
            build.Append("HTTP/1.1 200 OK\r\n");
            try
            {
                token = values.PostRegister(user);
            }
            catch
            {
                build.Replace("HTTP/1.1 200 OK\r\n", "HTTP/1.1 403 Forbidden\r\n");
            }
            String str = JsonConvert.SerializeObject(token.UserToken);

            build.Append("Content-Type: application/json\r\n");
            build.Append("Content-Length: " + str.Length + "\r\n");
            build.Append("\r\n");
            build.Append(str);

            String send = build.ToString();

            ((StringSocket)payload).BeginSend(send, (x, y) => ((StringSocket)payload).Shutdown(SocketShutdown.Both), null);
        }

        /// <summary>
        /// Joins a player to the game.
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="payload"></param>
        private void PostJoinGame(String temp, object payload)
        {
            GameRequest gameRequest = new GameRequest();
            gameRequest = JsonConvert.DeserializeObject<GameRequest>(temp);
            Game game = new Game();


            StringBuilder build = new StringBuilder();
            build.Append("HTTP/1.1 200 OK\r\n");
            try
            {
                game = values.PostJoinGame(gameRequest);
            }
            catch (HttpResponseException e)
            {
                var message = e.Response;

                if (e.Response.ReasonPhrase.Equals("Forbidden"))
                {
                    build.Replace("HTTP/1.1 200 OK\r\n", "HTTP/1.1 403 Forbidden\r\n");
                }
                else if (e.Response.ReasonPhrase.Equals("Conflict"))
                {
                    build.Replace("HTTP/1.1 200 OK\r\n", "HTTP/1.1 409 Conflict\r\n");
                }


            }
            String str = JsonConvert.SerializeObject(game);

            build.Append("Content-Type: application/json\r\n");
            build.Append("Content-Length: " + str.Length + "\r\n");
            build.Append("\r\n");
            build.Append(str);

            String send = build.ToString();

            ((StringSocket)payload).BeginSend(send, (x, y) => ((StringSocket)payload).Shutdown(SocketShutdown.Both), null);
        }

        /// <summary>
        /// Plays a word in the game.
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="GameID"></param>
        /// <param name="payload"></param>
        private void PostPlayWord(String temp, String GameID, object payload)
        {
            PlayedWord word = new PlayedWord();
            word = JsonConvert.DeserializeObject<PlayedWord>(temp);
            int score = 0;


            StringBuilder build = new StringBuilder();
            build.Append("HTTP/1.1 200 OK\r\n");
            try
            {
                score = values.PlayWord(GameID, word);
            }
            catch (HttpResponseException e)
            {
                var message = e.Response;

                if (e.Response.ReasonPhrase.Equals("Forbidden"))
                {
                    build.Replace("HTTP/1.1 200 OK\r\n", "HTTP/1.1 403 Forbidden\r\n");
                }
                else if (e.Response.ReasonPhrase.Equals("Conflict"))
                {
                    build.Replace("HTTP/1.1 200 OK\r\n", "HTTP/1.1 409 Conflict\r\n");
                }


            }
            String str = "" + score;

            build.Append("Content-Type: application/json\r\n");
            build.Append("Content-Length: " + str.Length + "\r\n");
            build.Append("\r\n");
            build.Append(str);

            String send = build.ToString();

            ((StringSocket)payload).BeginSend(send, (x, y) => ((StringSocket)payload).Shutdown(SocketShutdown.Both), null);
        }

        /// <summary>
        /// Returns the status of the game.
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="brief"></param>
        /// <param name="payload"></param>
        private void PostGameStatus(string gameID, bool brief, object payload)
        {
            Status status = new Status();

            StringBuilder build = new StringBuilder();
            build.Append("HTTP/1.1 200 OK\r\n");

            try
            {
                status = values.GetGameStatus(gameID, brief);
            }
            catch (HttpResponseException e)
            {
                var message = e.Response;

                if (e.Response.ReasonPhrase.Equals("Forbidden"))
                {
                    build.Replace("HTTP/1.1 200 OK\r\n", "HTTP/1.1 403 Forbidden\r\n");
                }
            }

            String str = JsonConvert.SerializeObject(status);
            build.Append("Content-Type: application/json\r\n");
            build.Append("Content-Length: " + str.Length + "\r\n");
            build.Append("\r\n");
            build.Append(str);

            String send = build.ToString();

            ((StringSocket)payload).BeginSend(send, (x, y) => ((StringSocket)payload).Shutdown(SocketShutdown.Both), null);
        }

        /// <summary>
        /// Cancels a game
        /// </summary>
        /// <param name="UserRequest"></param>
        /// <param name="payload"></param>
        private void PostCancel(String UserRequest, object payload)
        {
            String userToken = JsonConvert.DeserializeObject<String>(UserRequest);

            StringBuilder build = new StringBuilder();
            build.Append("HTTP/1.1 204 NoContent\r\n");

            try
            {
                values.Cancel(userToken);
            }
            catch (HttpResponseException e)
            {
                var message = e.Response;

                if (e.Response.ReasonPhrase.Equals("Forbidden"))
                {
                    build.Replace("HTTP/1.1 204 NoContent\r\n", "HTTP/1.1 403 Forbidden\r\n");
                }
            }

            build.Append("Content-Type: application/json\r\n");
            build.Append("Content-Length: " + 0 + "\r\n");
            build.Append("\r\n");
            String send = build.ToString();
            ((StringSocket)payload).BeginSend(send, (x, y) => ((StringSocket)payload).Shutdown(SocketShutdown.Both), null);
        }
    }
}
