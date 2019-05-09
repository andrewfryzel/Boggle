// Authors Jared Nay and Andrew Fryzel
// CS 3500 University of Utah
// April 15, 2019

using Boggle;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Http;




namespace MyBoggleService
{
    /// <summary>
    /// Values conroller takes Http requests and sends them to the Database Access(DBA) class. The DBA should NOT
    /// have null values sent to it. 
    /// </summary>
    public class ValuesController
    {
        // A class that will allow access to the database to keep SQL and C# separated.
        private static DatabaseAccess dba;
        private static readonly ISet<string> dictionary;

        /// <summary>
        /// Creates a new value controller that makes a DatabaseAccess instance, and initializes the dictionary  as
        /// a static string.
        /// </summary>
        static ValuesController()
        {
            dba = new DatabaseAccess();
            dictionary = new HashSet<string>();
            using (StreamReader words = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "/dictionary.txt"))
            {
                string word;
                while ((word = words.ReadLine()) != null)
                {
                    dictionary.Add(word.ToUpper());
                }
            }
        }

        /// <summary>
        /// Create a new user.
        /// </summary>
        /// If Nickname is null, or when trimmed is empty or longer than 50 characters, responds with status 403 (Forbidden).
        /// Otherwise, creates a new user with a unique user token and the trimmed Nickname.
        /// The returned user token should be used to identify the user in subsequent requests. Responds with status 200 (Ok).
        /// <param name="Nickname"></param>
        /// <returns></returns>
        //[Route("BoggleService/users")]
        //[FromBody]
        public Token PostRegister(UserInfo Nickname)
        {
            Token token = new Token();
            // Check to make sure Nickname is valid.
            if (Nickname == null || Nickname.Nickname.Trim().Length == 0 || Nickname.Nickname.Trim().Length > 50)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            token.UserToken = dba.AddUser(Nickname.Nickname.Trim());
            return token;
        }


        /// <summary>
        /// Join a game.
        /// </summary>
        /// If UserToken is invalid, TimeLimit less than 5, or TimeLimit greater than 120, responds with status 403 (Forbidden).
        /// Otherwise, if UserToken is already a player in the pending game, responds with status 409 (Conflict).
        /// Otherwise, if there are no players in the pending game, adds UserToken as the first player of the pending game, and the 
        /// TimeLimit as the pending game's requested time limit. 
        /// Returns an object as illustrated below containing the pending game's game ID.Responds with status 200 (Ok).
        /// Otherwise, adds UserToken as the second player.The pending game becomes active and a new pending game with no players is created.
        /// The active game's time limit is the integer average of the time limits requested by the two players. Returns an object as illustrated below 
        /// containing the new active game's game ID(which should be the same as the old pending game's game ID). Responds with status 200 (Ok).
        /// <param name="user"></param>
        /// <returns></returns>
        // [HttpPost]
        //[Route("BoggleService/games")]
        public Game PostJoinGame(GameRequest user)
        {
            // Null check and TimeLimit validation and initilization of those values to local variables if they are valid.
            bool UserNullCheck = user.UserToken == null || user.TimeLimit < 5 || user.TimeLimit > 120;
            if (UserNullCheck)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }
            String userToken = user.UserToken.Trim();
            int timeLimit = user.TimeLimit;

            // Separate check after null check.
            if (!dba.IsValidUser(userToken))
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            // Add the player to the correct player.
            if (dba.DoesPendingGameExists())
            {
                if (dba.IsPlayerPending(userToken))
                {
                    throw new HttpResponseException(HttpStatusCode.Conflict);
                }

                // Returns an active game.
                return dba.AddPlayer2(userToken, timeLimit);
            }
            else
            {
                // Returns a pending game.
                return dba.AddPlayer1(userToken, timeLimit);
            }
        }

        /// <summary>
        /// Play a word in a game.
        /// </summary>
        /// If Word is null or empty or longer than 30 characters when trimmed, or if gameID or UserToken is invalid, 
        /// or if UserToken is not a player in the game identified by gameID, responds with response code 403 (Forbidden).
        /// Otherwise, if the game state is anything other than "active", responds with response code 409 (Conflict).
        /// Otherwise, records the trimmed Word as being played by UserToken in the game identified by gameID.Returns the score for Word in the 
        /// context of the game (e.g. if Word has been played before the score is zero). 
        /// Responds with status 200 (OK). Note: The word is not case sensitive.
        /// <param name="gameID"></param>
        /// <param name="Word"></param>
        /// <returns></returns>
        // [HttpPut]
        // [Route("BoggleService/games/{gameID}")]
        public int PlayWord(String gameID, PlayedWord Word)
        {
            // First a null check then local variables to hold strings.
            if (Word.Word == null || gameID == null || Word.UserToken == null)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }
            String word = Word.Word.Trim().ToUpper();
            String userToken = Word.UserToken;

            // Next the gammeID, word, and user token need to be checked for validity.
            bool IsWordInvalid = (word == "" || word.Length > 30);
            bool IsGameIDInvalid = (!(dba.IsGameValid(gameID)));
            bool IsUserTokenInvalid = (!(dba.IsValidUser(userToken)) || !(dba.IsPlayerInGame(gameID, userToken)));
            if (IsWordInvalid || IsGameIDInvalid || IsUserTokenInvalid)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            // If the game is not active, send a conflict status code.
            String GameState = dba.GetGameState(gameID);
            if (!(GameState.Equals("active")))
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            // Get the score of the word, and see if it has been played already. If it hasn't, add it. Then return
            // the score.
            int score = CalculateScore(gameID, userToken, word);
            dba.AddWord(gameID, userToken, word, score);

            //Scores ans = new Scores();
            //ans.Score = score;
            return score;
        }

        /// <summary>
        /// Cancel a pending request to join a game.
        /// </summary>
        /// If UserToken is invalid or is not a player in the pending game, responds with status 403 (Forbidden).
        /// Otherwise, removes UserToken from the pending game and responds with status 204 (NoContent).
        /// <param name="UserToken"></param>
        // [HttpPut]
        // [Route("BoggleService/games")]
        //[FromBody]
        public void Cancel(String UserToken)
        {
            // Start with  null check.
            if (UserToken == null)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            // If the player isn't registered or pending already, throw forbidden http code.
            bool isValidUser = dba.IsValidUser(UserToken);
            bool isPlayerPending = dba.IsPlayerPending(UserToken);
            if (!isValidUser || !isPlayerPending)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            dba.CancelGame(UserToken);
        }

        /// <summary>
        /// Get game status information.
        /// </summary>
        /// If gameID is invalid, responds with status 403 (Forbidden).
        /// Othrewise, returns information about the game named by gameID as illustrated below.Note that the information returned depends on whether 
        /// brief is true or false as well as on the state of the game.
        /// Responds with status code 200 (OK). Note: The Board and Words are not case sensitive.
        /// <param name="gameID"></param>
        /// <param name="brief"></param>
        /// <returns></returns>
        // [HttpGet]
        // [Route("BoggleService/games/{gameID}/{brief}")]
        public Status GetGameStatus(String gameID, bool brief)
        {
            // State, scores, and wordsplayed is needed basically everywhere.
            Status status;
            String state;
            int Player1Score = 0;
            int Player2Score = 0;
            List<WordAndScore> PlayedWords1;
            List<WordAndScore> PlayedWords2;

            // Null check for sure.
            if (gameID == null || !dba.IsGameValid(gameID))
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            // If it's pending only need to update the state.
            state = dba.GetGameState(gameID);
            if (state.Equals("pending"))
            {
                status = new Status();
                status.GameState = state;
                return status;
            }

            // Start making the status.
            status = dba.GetStatus(gameID);
            status.GameState = state;
            PlayedWords1 = dba.GetWordsPlayed(gameID, status.Player1.UserToken);
            PlayedWords2 = dba.GetWordsPlayed(gameID, status.Player2.UserToken);

            // Calculate the scores.
            foreach (WordAndScore was in PlayedWords1)
            {
                Player1Score = Player1Score + was.Score;
            }
            foreach (WordAndScore was in PlayedWords2)
            {
                Player2Score = Player2Score + was.Score;
            }
            status.Player1.Score = Player1Score;
            status.Player2.Score = Player2Score;

            // Brief statuses.
            if (brief)
            {
                // TimeLimit shouldn't be serialized, completed brief is already ready.
                status.TimeLimit = 0;
                if (state.Equals("active"))
                {
                    status.TimeLeft = dba.CalculateTimeLeft(gameID);
                }
            }
            else
            {
                // Shared values for both active and completed.
                status.Board = dba.GetBoggleBoard(gameID);
                status.Player1.Nickname = dba.GetNickname(status.Player1.UserToken);
                status.Player2.Nickname = dba.GetNickname(status.Player2.UserToken);

                // This time they are different for completed.
                if (state.Equals("active"))
                {
                    status.TimeLeft = dba.CalculateTimeLeft(gameID);
                }
                else
                {
                    status.TimeLeft = 0;
                    if (PlayedWords1.Count != 0)
                        status.Player1.WordsPlayed = PlayedWords1;
                    if (PlayedWords2.Count != 0)
                        status.Player2.WordsPlayed = PlayedWords2;
                }
            }
            return status;
        }

        /// <summary>
        /// Calculates the score of a word.
        /// </summary>
        /// <param name="GameID"></param>
        /// <param name="UserToken"></param>
        /// <param name="Word"></param>
        /// <returns></returns>
        private int CalculateScore(String GameID, String UserToken, String Word)
        {
            String Board = dba.GetBoggleBoard(GameID);

            //Words less than 3 characters score 0.
            if (Word.Length <= 2)
                return 0;


            // Check if the word can be played on the board.
            if (CanWordBePlayed(GameID, Word))
            {
                // Next check if the player has already played the word.
                List<WordAndScore> PlayedWords = dba.GetWordsPlayed(GameID, UserToken);
                foreach (WordAndScore was in PlayedWords)
                {
                    if (was.Word.Equals(Word))
                    {
                        return 0;
                    }
                }
                // If not choose the appropriate score.
                if ((Word.Length == 3) || (Word.Length == 4))
                    return 1;
                else if (Word.Length == 5)
                    return 2;
                else if (Word.Length == 6)
                    return 3;
                else if (Word.Length == 7)
                    return 5;
                else
                    return 11;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Checks to see if a word can be played in the game.
        /// </summary>
        /// <param name="GameID"></param>
        /// <param name="Word"></param>
        /// <returns></returns>
        private bool CanWordBePlayed(String GameID, String Word)
        {
            // First check if the word exists on the board.
            String tempString = dba.GetBoggleBoard(GameID);
            BoggleBoard Board = new BoggleBoard(tempString);

            // Next check the dictionary to see if it is a valid word.
            if (!dictionary.Contains(Word) || !Board.CanBeFormed(Word))
                return false;

            return true;
        }
    }
}
