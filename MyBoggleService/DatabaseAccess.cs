// Authors Jared Nay and Andrew Fryzel
// CS 3500 University of Utah
// April 15, 2019

using Boggle;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.IO;

namespace MyBoggleService
{
    /// <summary>
    /// A class to control access to the Boggle Database. This class is desgined to communicate from a controller to the DB.
    /// Null values should NOT be sent to this class, and should be handled prior to calling any methods.
    /// </summary>
    public class DatabaseAccess
    {
        // Connection string and a pendingGameID
        private static string BoggleDB;
        private static string pendingGameID;


        string dbFolder;

        /// <summary>
        /// Constructor that saves the connection string in the Web.config file.
        /// </summary>
        public DatabaseAccess()
        {
            dbFolder = System.IO.Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            BoggleDB = String.Format(@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = {0}\BoggleDB.mdf; Integrated Security = True", dbFolder);

        }


        //**********************************************Methods for accessing Users********************************************************


        /// <summary>
        /// Opens the Sql connection and registers a user to the database.
        /// </summary>
        /// <param name="Nickname"></param>
        /// <returns></returns>
        public String AddUser(String Nickname)
        {
            // Create a unique UserID.
            String userID = Guid.NewGuid().ToString();

            // Set the connection with the Database.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                // Connection is opened
                conn.Open();

                // Open a transaction for the connection.
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Enter the sequel commands.
                    using (SqlCommand command = new SqlCommand("insert into Users (UserToken, Nickname) values(@UserToken, @Nickname)",
                        conn, trans))
                    {
                        // Set the placeholder values.
                        command.Parameters.AddWithValue("@UserToken", userID);
                        command.Parameters.AddWithValue("@Nickname", Nickname.Trim());

                        // Check query was successful, commit the transaction, then return.
                        if (command.ExecuteNonQuery() != 1)
                        {
                            throw new Exception("Query failed unexpectedly");
                        }
                        trans.Commit();
                        return userID;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a String of the players Nickname given a usertoken. If the user is not register, it throws an exception.
        /// </summary>
        /// <param name="UserToken"></param>
        /// <returns></returns>
        public String GetNickname(String UserToken)
        {
            // Open the connection.
            String Nickname = "";
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to get the Nickname.
                    using (SqlCommand command = new SqlCommand("select Nickname from Users where UserToken = @UserToken", conn, trans))
                    {
                        command.Parameters.AddWithValue("@UserToken", UserToken);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Do a null check. 
                            reader.Read();
                            int ord = reader.GetOrdinal("Nickname");
                            if (reader.IsDBNull(ord))
                            {
                                throw new Exception("No nickname found");
                            }
                            Nickname = (String)reader["Nickname"];
                        }
                    }
                }
            }
            return Nickname;
        }

        /// <summary>
        /// Creates a new pending game and adds the player to it.
        /// </summary>
        /// <param name="userToken"></param>
        /// <param name="timeLimit"></param>
        /// <returns></returns>
        public Game AddPlayer1(String userToken, int timeLimit)
        {
            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to insert the game into DB
                    using (SqlCommand command = new SqlCommand("insert into Games (Player1, TimeLimit) values(@Player1, @TimeLimit)",
                        conn, trans))
                    {
                        command.Parameters.AddWithValue("@Player1", userToken);
                        command.Parameters.AddWithValue("@TimeLimit", timeLimit);

                        // Check query was successful, commit the transaction, then return
                        if (command.ExecuteNonQuery() != 1)
                        {
                            throw new Exception("Query failed unexpectedly");
                        }
                    }

                    // Command for getting the GameID
                    using (SqlCommand command = new SqlCommand("select GameID from Games where Player1 = @Player1",
                        conn, trans))
                    {
                        command.Parameters.AddWithValue("@Player1", userToken);

                        // Make the new game, and initialize it.
                        String gameToken = command.ExecuteScalar().ToString();
                        pendingGameID = gameToken;
                        Game game = new Game();
                        game.GameID = gameToken;
                        game.IsPending = true;

                        trans.Commit();
                        return game;
                    }
                }
            }
        }

        /// <summary>
        /// Adds player to the pending game and starts it.
        /// </summary>
        /// <param name="userToken"></param>
        /// <param name="timeLimit"></param>
        /// <returns></returns>
        public Game AddPlayer2(String userToken, int timeLimit)
        {
            // Total time limit for the game and starting the SQL connection.
            int finalTime;
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Get the time limit from player1 to be totaled.
                    using (SqlCommand command = new SqlCommand("select TimeLimit from Games where GameID = @GameID", conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameID", pendingGameID);
                        finalTime = (int)command.ExecuteScalar();
                    }

                    finalTime = (timeLimit + finalTime) / 2;

                    // Update the rest of the game in the DB.
                    using (SqlCommand command = new SqlCommand("update Games set Player2 = @Player2, Board = @Board, TimeLimit = @TimeLimit," +
                        " StartTime = @StartTime where GameID = @GameID ", conn, trans))
                    {
                        // Make the boggle board and set up all the parameters for query.
                        BoggleBoard board = new BoggleBoard();
                        DateTime dateTime = DateTime.Now;
                        command.Parameters.AddWithValue("@Player2", userToken);
                        command.Parameters.AddWithValue("@Board", board.ToString());
                        command.Parameters.AddWithValue("@TimeLimit", finalTime);
                        command.Parameters.AddWithValue("@StartTime", dateTime);
                        command.Parameters.AddWithValue("@GameID", pendingGameID);

                        // Make the game to return.
                        Game game = new Game();
                        game.GameID = pendingGameID;
                        game.IsPending = false;
                        pendingGameID = null;

                        // Check query success, commit, and return.
                        if (command.ExecuteNonQuery() != 1)
                        {
                            throw new Exception("Query failed unexpectedly");
                        }
                        trans.Commit();
                        return game;
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the user is registered to the database.
        /// </summary>
        /// <param name="UserToken"></param>
        /// <returns></returns>
        public bool IsValidUser(String UserToken)
        {
            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to check the DB for UserToken.
                    using (SqlCommand command = new SqlCommand("select UserToken from Users where UserToken = @UserToken", conn, trans))
                    {
                        command.Parameters.AddWithValue("@UserToken", UserToken);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // If it has no rows, it is not in the DB
                            if (reader.HasRows)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a player is in the requesting game.
        /// </summary>
        /// <param name="GameID"></param>
        /// <param name="UserToken"></param>
        /// <returns></returns>
        public bool IsPlayerInGame(String GameID, String UserToken)
        {
            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to pull both players from a game.
                    using (SqlCommand command = new SqlCommand("select Player1, Player2 from Games where GameID = @GameID",
                        conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameID", GameID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // If the game doesn't come back with anything, the player can't be in it.
                            if (!reader.HasRows)
                            {
                                return false;
                            }

                            // Next check to see if it's Player1.
                            reader.Read();
                            if (UserToken.Equals(reader["Player1"]))
                            {
                                return true;
                            }

                            // Next check to see if it is Player2.
                            int ord = reader.GetOrdinal("Player2");
                            if (!reader.IsDBNull(ord))
                            {
                                if (UserToken.Equals(reader["Player2"]))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see if the Player is in the pending game.
        /// </summary>
        /// <param name="UserToken"></param>
        /// <returns></returns>
        public bool IsPlayerPending(String UserToken)
        {
            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Select Player1 in the pending game.
                    using (SqlCommand command = new SqlCommand("select Player1 from Games where Player2 is null", conn, trans))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // A quick null check, if there no rows, there is no pending game. The player 
                            // can't be in a non existent game.
                            if (!reader.HasRows)
                            {
                                return false;
                            }
                        }

                        // the actual check to see if player is already pending.
                        String player1 = command.ExecuteScalar().ToString();
                        if (UserToken.Equals(player1))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        //**********************************************Methods for accessing Games********************************************************


        /// <summary>
        /// Cancels a player out of the pending game, and deletes it from the database.
        /// </summary>
        /// <param name="UserToken"></param>
        public void CancelGame(String UserToken)
        {
            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to delete the pending game from the DB.
                    using (SqlCommand command = new SqlCommand("delete from Games where Player1 = @Player1", conn, trans))
                    {
                        command.Parameters.AddWithValue("@Player1", UserToken);

                        // Check query success, commit, and return.
                        if (command.ExecuteNonQuery() != 1)
                        {
                            throw new Exception("Query failed unexpectedly");
                        }
                        pendingGameID = null;
                        trans.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the GameID is registered to the database.
        /// </summary>
        /// <param name="GameToken"></param>
        /// <returns></returns>
        public bool IsGameValid(String GameToken)
        {
            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to pull out the game ID.
                    using (SqlCommand command = new SqlCommand("select GameID from Games where GameID = @GameID", conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameID", GameToken);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // If we query a GameID from the DB, it is valid.
                            if (reader.HasRows)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see if there is currently a pending game in the database.
        /// </summary>
        /// <returns></returns>
        public bool DoesPendingGameExists()
        {
            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to select the pending game out of the DB
                    using (SqlCommand command = new SqlCommand("select Player1 from Games where Player2 is null", conn, trans))
                    {
                        // If it has a row there is a pending game.
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            return reader.HasRows;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the time left in a game.
        /// </summary>
        /// <param name="GameID"></param>
        /// <returns></returns>
        public int CalculateTimeLeft(String GameID)
        {
            // Declare locals here so they can be used throughout, then start the connection.
            int timeLimit;
            int timeRemaining;
            DateTime startTime;
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to get the TimeLimit and StartTime
                    using (SqlCommand command = new SqlCommand("select TimeLimit, StartTime from Games where GameID = @GameID",
                        conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameID", GameID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            reader.Read();
                            timeLimit = (int)reader["TimeLimit"];
                            startTime = (DateTime)reader["StartTime"];
                        }

                        // Subtract the two DateTimes (time stamps) and store it in a TimeSpan. Round the result and then 
                        // subtract from the time limit.
                        TimeSpan ts = (DateTime.Now.Subtract(startTime));
                        double round = Math.Round(ts.TotalSeconds);
                        timeRemaining = timeLimit - (int)round;

                        // If the result is < 0 the game is completed, so just return a 0.
                        if (timeRemaining == 0)
                        {
                            return 1;
                        }
                        else if (timeRemaining < 0)
                        {
                            return 0;
                        }
                        return timeRemaining;
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the game is active.
        /// </summary>
        /// <param name="GameID"></param>
        /// <returns></returns>
        public String GetGameState(String GameID)
        {
            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to get the StartTime.
                    using (SqlCommand command = new SqlCommand("select StartTime from Games where GameID = @GameID", conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameID", GameID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // If StartTime is null, the game is pending.
                            reader.Read();
                            int ord = reader.GetOrdinal("StartTime");
                            if (reader.IsDBNull(ord))
                            {
                                return "pending";
                            }

                            // If time left <= 0 the game is completed.
                            if (CalculateTimeLeft(GameID) == 0)
                            {
                                return "completed";
                            }
                        }
                    }
                }
            }
            return "active";
        }

        /// <summary>
        /// Gets the BoggleBoard for the corresponding GameID.
        /// </summary>
        /// <param name="GameID"></param>
        /// <returns></returns>
        public String GetBoggleBoard(String GameID)
        {
            // String to hold the board, and open up the connection.
            String Board;
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to get the Boggle Board
                    using (SqlCommand command = new SqlCommand("select Board from Games where GameID = @GameID", conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameId", GameID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // First make sure the Board value isn't null.
                            reader.Read();
                            int ord = reader.GetOrdinal("Board");
                            if (reader.IsDBNull(ord))
                            {
                                throw new Exception("Null value for board");
                            }

                            // Add to local and return;
                            Board = (String)reader["Board"];
                            return Board;
                        }
                    }
                }
            }
        }


        //**********************************************Methods for manipulating words****************************************************

        /// <summary>
        /// Returns a dictionary of all words and scores of a player.
        /// </summary>
        /// <param name="GameID"></param>
        /// <param name="UserToken"></param>
        /// <returns></returns>
        public List<WordAndScore> GetWordsPlayed(String GameID, String UserToken)
        {
            // Make the list and start connection.
            List<WordAndScore> PlayedWords = new List<WordAndScore>();
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to get the word and score.
                    using (SqlCommand command = new SqlCommand("select Word, Score from Words where GameID = @GameID and Player = @Player",
                         conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameID", GameID);
                        command.Parameters.AddWithValue("@Player", UserToken);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Add values to the list.
                            while (reader.Read())
                            {
                                WordAndScore was = new WordAndScore();
                                was.Word = (string)reader["Word"];
                                was.Score = (int)reader["Score"];
                                PlayedWords.Add(was);
                            }
                        }
                    }
                }
            }
            return PlayedWords;
        }

        /// <summary>
        /// Adds a Word, it's score, the player, and the GameID into the words table.
        /// </summary>
        /// <param name="GameID"></param>
        /// <param name="UserToken"></param>
        /// <param name="Word"></param>
        /// <param name="Score"></param>
        public void AddWord(String GameID, String UserToken, String Word, int Score)
        {
            // Start the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to make a new word.
                    using (SqlCommand command = new SqlCommand("insert into Words (Word, GameID, Player, Score) values (@Word, @GameID, @Player, @Score)",
                        conn, trans))
                    {
                        command.Parameters.AddWithValue("@Word", Word);
                        command.Parameters.AddWithValue("@GameID", GameID);
                        command.Parameters.AddWithValue("@Player", UserToken);
                        command.Parameters.AddWithValue("@Score", Score);

                        // Check query success and commit.
                        if (command.ExecuteNonQuery() != 1)
                        {
                            throw new Exception("Unexpected error adding word");
                        }
                        trans.Commit();
                    }
                }
            }
        }


        //**********************************************Methods for manipulating game status****************************************************


        /// <summary>
        /// Returns a basic status setup with Player1, Player2, and the TimeLimit.
        /// </summary>
        /// <param name="GameID"></param>
        /// <returns></returns>
        public Status GetStatus(String GameID)
        {
            // Need Players, user tokens, and time limit locals.
            Status status = new Status();
            Player Player1 = new Player();
            Player Player2 = new Player();
            String UserToken1;
            String UserToken2;
            int TimeLimit;

            // Open the connection.
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    // Command to get Player1, Player2, and TimeLimit
                    using (SqlCommand command = new SqlCommand("select Player1, Player2, TimeLimit from Games where GameID = @GameID",
                        conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameID", GameID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Add the data.
                            reader.Read();
                            UserToken1 = (String)reader["Player1"];
                            UserToken2 = (String)reader["player2"];
                            TimeLimit = (int)reader["TimeLimit"];
                        }
                    }
                }
            }

            // Add Tokens to the Players
            Player1.UserToken = UserToken1;
            Player2.UserToken = UserToken2;

            // Add values to the status, then return it.
            status.Player1 = Player1;
            status.Player2 = Player2;
            status.TimeLimit = TimeLimit;
            return status;
        }
    }
}