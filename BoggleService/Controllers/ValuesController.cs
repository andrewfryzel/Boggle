//Andrew Fryzel and Jared Nay

using Boggle;
using BoggleService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Http;

namespace BoggleService.Controllers
{

    public class ValuesController : ApiController
    {
        /// <summary>
        /// Create a new user.
        /// 
        /// If Nickname is null, or when trimmed is empty or longer than 50 characters, responds with status 403 (Forbidden).
        /// Otherwise, creates a new user with a unique user token and the trimmed Nickname.
        /// The returned user token should be used to identify the user in subsequent requests. Responds with status 200 (Ok).
        /// </summary>
        /// <param name="Nickname"></param>
        /// <returns></returns>
        [Route("BoggleService/users")]
        public String PostRegister([FromBody] String Nickname)
        {
            lock (sync)
            {
                if (Nickname == null || Nickname.Trim().Length == 0 || Nickname.Trim().Length > 50)
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }
                else
                {
                    User newUser = new User();
                    string userID = Guid.NewGuid().ToString();
                    newUser.Nickname = Nickname;
                    newUser.UserToken = userID;
                    users.Add(userID, newUser);
                    return userID;
                }
            }
        }

        /// <summary>
        /// Join a game.
        /// If UserToken is invalid, TimeLimit less than 5, or TimeLimit greater than 120, responds with status 403 (Forbidden).
        /// Otherwise, if UserToken is already a player in the pending game, responds with status 409 (Conflict).
        /// Otherwise, if there are no players in the pending game, adds UserToken as the first player of the pending game, and the 
        /// TimeLimit as the pending game's requested time limit. 
        /// Returns an object as illustrated below containing the pending game's game ID.Responds with status 200 (Ok).
        /// Otherwise, adds UserToken as the second player.The pending game becomes active and a new pending game with no players is created.
        /// The active game's time limit is the integer average of the time limits requested by the two players. Returns an object as illustrated below 
        /// containing the new active game's game ID(which should be the same as the old pending game's game ID). Responds with status 200 (Ok).
        /// </summary>
        /// <param name="UserToken"></param>
        /// <param name="TimeLimit"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("BoggleService/games")]
        public Game PostJoinGame(GameRequest user)
        {
            lock (sync)
            {
                Game tempGame;
                String userToken = user.UserToken;
                int timeLimit = user.TimeLimit;

                //Make or get the pending game.
                if (pendingGameToken == null)
                    tempGame = MakePendingGame();
                else
                    games.TryGetValue(pendingGameToken, out tempGame);

                //Check to see if UserToken and TimeLimit are valid
                bool userTokenInvalid = (!users.ContainsKey(userToken));
                bool timeLimitInvalid = ((timeLimit < 5) || (timeLimit > 120));
                if (userTokenInvalid || timeLimitInvalid)
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }

                //If the player is already in the pending game it is a conflict.
                bool playerAlreadyPending = (userToken.Equals(tempGame.Player1Token));
                if (playerAlreadyPending)
                {
                    throw new HttpResponseException(HttpStatusCode.Conflict);
                }

                //If there is no player 1, place as player 1.
                if (tempGame.Player1Token == null && tempGame.Player2Token == null)
                {
                    tempGame.Player1Token = userToken;
                    tempGame.TimeLimit = timeLimit;
                }
                //Else it is player 2.
                else
                {
                    tempGame.Player2Token = userToken;
                    tempGame.IsPending = false;
                    tempGame.IsActive = true;
                    pendingGameToken = null;

                    //Get the time limit and start game.
                    int finalTime = (tempGame.TimeLimit + timeLimit) / 2;
                    tempGame.TimeLimit = finalTime;
                    tempGame.TimeLeft = finalTime;
                    tempGame.GameState = "active";
                }
                return tempGame;
            }
        }

        /// <summary>
        /// Play a word in a game.
        ///
        /// If Word is null or empty or longer than 30 characters when trimmed, or if gameID or UserToken is invalid, 
        /// or if UserToken is not a player in the game identified by gameID, responds with response code 403 (Forbidden).
        /// Otherwise, if the game state is anything other than "active", responds with response code 409 (Conflict).
        /// Otherwise, records the trimmed Word as being played by UserToken in the game identified by gameID.Returns the score for Word in the 
        /// context of the game (e.g. if Word has been played before the score is zero). 
        /// Responds with status 200 (OK). Note: The word is not case sensitive.
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="UserToken"></param>
        /// <param name="Word"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("BoggleService/games/{gameID}")]
        public int PlayWord(String gameID, PlayedWord Word)
        {
            String userToken = Word.UserToken;
            //Check to make sure the gameID, UserToken, and Word are all valid.
            bool invalidWord = ((Word.Word == null) || (Word.Word.Trim().Length == 0) || (Word.Word.Trim().Length > 30
                || userToken == null) || !games.ContainsKey(gameID) || gameID == null
                || !users.ContainsKey(userToken));

            if (invalidWord)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            games.TryGetValue(gameID, out Game tempGame);

            //Check to see if the player is currently in this game.
            bool playerNotInGame = !userToken.Equals(tempGame.Player1Token) && !userToken.Equals(tempGame.Player2Token);

            if (playerNotInGame)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            //Check to see if the game is active.
            bool isActiveGame = tempGame.GameState.Equals("active");
            if (!isActiveGame)
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            //Calculate the score.
            String trimmedWord = Word.Word.Trim().ToUpper();
            int score = CalculateScore(tempGame, trimmedWord);

            //Make a new word and score object.
            WordAndScore was = new WordAndScore();
            was.Word = trimmedWord;
            was.Score = score;

            //Pick appropriate score.
            if (userToken.Equals(tempGame.Player1Token))
            {
                tempGame.WordsPlayed1.Add(was);
                tempGame.Player1Score = tempGame.Player1Score + score;
            }
            else
            {
                tempGame.WordsPlayed2.Add(was);
                tempGame.Player2Score = tempGame.Player2Score + score;
            }
            tempGame.AllWords.Add(trimmedWord);
            return score;
        }

        /// <summary>
        /// Cancel a pending request to join a game.
        ///
        /// If UserToken is invalid or is not a player in the pending game, responds with status 403 (Forbidden).
        /// Otherwise, removes UserToken from the pending game and responds with status 204 (NoContent).
        /// </summary>
        /// <param name="UserToken"></param>
        [HttpPut]
        [Route("BoggleService/games")]
        public void Cancel([FromBody]String UserToken)
        {
            lock (sync)
            {
                //Make sure it's a valid user.
                bool invalidUserToken = !(users.ContainsKey(UserToken) || UserToken == null);
                if (invalidUserToken)
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }

                //Get the game to cancel.
                games.TryGetValue(pendingGameToken, out Game pendingGame);

                //If the player is not in the pending game, Forbidden Exception is thrown.
                bool playerNotPending = !(UserToken.Equals(pendingGame.Player1Token))
                    && !(UserToken.Equals(pendingGame.Player2Token));
                if (playerNotPending)
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }

                pendingGame.Player1Token = null;
            }
        }

        /// <summary>
        /// Get game status information.
        ///
        /// If gameID is invalid, responds with status 403 (Forbidden).
        /// Othrewise, returns information about the game named by gameID as illustrated below.Note that the information returned depends on whether 
        /// brief is true or false as well as on the state of the game.
        /// Responds with status code 200 (OK). Note: The Board and Words are not case sensitive.
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="brief"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("BoggleService/games/{gameID}/{brief}")]
        public Status GetGameStatus(String gameID, bool brief)
        {
            //Check if the gameID is valid
            if (gameID == null)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }
            Status status;

            //Setup for if the game is pending
            if (gameID.Equals(pendingGameToken))
            {
                status = new Status();
                status.GameState = "pending";
                return status;
            }

            if (!games.ContainsKey(gameID))
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            games.TryGetValue(gameID, out Game game);
            users.TryGetValue(game.Player1Token, out User user1);
            users.TryGetValue(game.Player2Token, out User user2);
            Player Player1 = new Player();
            Player Player2 = new Player();

            status = new Status();
            //Setup for if the game is active.
            if (game.GameState.Equals("active"))
            {
                Player1.Score = game.Player1Score;
                Player2.Score = game.Player2Score;
                status.GameState = game.GameState;
                status.TimeLeft = game.TimeLeft;

                if (brief)
                {
                    status.Player1 = Player1;
                    status.Player2 = Player2;
                }
                else
                {
                    status.Board = game.Board;
                    status.TimeLimit = game.TimeLimit;
                    Player1.Nickname = user1.Nickname;
                    Player2.Nickname = user2.Nickname;
                    status.Player1 = Player1;
                    status.Player2 = Player2;
                }

                if (game.TimeLeft > 0)
                {
                    game.TimeLeft--;
                }
                else
                {
                    game.GameState = "completed";
                }
            }

            //Setup for if the game is completed.
            else if (game.GameState.Equals("completed"))
            {
                Player1.Score = game.Player1Score;
                Player2.Score = game.Player2Score;
                status.GameState = game.GameState;

                if (brief)
                {
                    status.Player1 = Player1;
                    status.Player2 = Player2;
                }
                else
                {
                    status.Board = game.Board;
                    status.TimeLimit = game.TimeLimit;
                    Player1.Nickname = user1.Nickname;
                    Player2.Nickname = user2.Nickname;
                    Player1.WordsPlayed = game.WordsPlayed1;
                    Player2.WordsPlayed = game.WordsPlayed2;
                    status.Player1 = Player1;
                    status.Player2 = Player2;
                }
            }
            return status;
        }

        /// <summary>
        /// Calculates a scored based on word values
        /// </summary>
        /// <param name="tempGame"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        private int CalculateScore(Game tempGame, String word)
        {
            //Words less than 3 characters score 0.
            if (word.Length <= 2)
                return 0;

            //Initialize game, player, board, player word list, and bool for valid.
            BoggleBoard tempBoard = new BoggleBoard(tempGame.Board);
            bool isValidWord;

            //Find appropriate score.
            isValidWord = IsValidWord(tempGame, tempBoard, word);
            if (isValidWord)
            {
                if ((word.Length == 3) || (word.Length == 4))
                    return 1;
                else if (word.Length == 5)
                    return 2;
                else if (word.Length == 6)
                    return 3;
                else if (word.Length == 7)
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
        /// Creates a new pending game. 
        /// Sets game variables to starting instances
        /// </summary>
        /// <returns></returns>
        private Game MakePendingGame()
        {
            //Initialize game.
            String tempGameToken = Guid.NewGuid().ToString();
            Game tempGame = new Game();

            //Populate games properties and add it to the dictionary.
            tempGame.GameID = tempGameToken;
            tempGame.IsPending = true;
            tempGame.IsActive = false;
            tempGame.GameState = "pending";
            tempGame.Player1Score = 0;
            tempGame.Player2Score = 0;
            pendingGameToken = tempGameToken;

            BoggleBoard board = new BoggleBoard();
            tempGame.Board = board.ToString();
            tempGame.WordsPlayed1 = new List<WordAndScore>();
            tempGame.WordsPlayed2 = new List<WordAndScore>();
            tempGame.AllWords = new SortedSet<string>();
            games.Add(tempGameToken, tempGame);

            return tempGame;
        }

        /// <summary>
        /// Checks if a word is valid against the board and the dictionary
        /// </summary>
        /// <param name="tempGame"></param>
        /// <param name="board"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        private bool IsValidWord(Game tempGame, BoggleBoard board, String word)
        {
            String temp;
            if (word.Trim().Length == 0 || word == null)
            {
                return false;
            }

            if (!board.CanBeFormed(word))
                return false;

            foreach (var item in tempGame.AllWords)
            {
                if (item.Equals(word))
                    return false;
            }

            using (StreamReader reader = new System.IO.StreamReader(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt"))
            {
                while ((temp = reader.ReadLine()) != null)
                {
                    if (temp.Equals(word.Trim().ToUpper()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Data structure for storing all the players and games. 
        /// </summary>
        private readonly static Dictionary<String, User> users = new Dictionary<String, User>();
        private readonly static Dictionary<String, Game> games = new Dictionary<String, Game>();
        private static String pendingGameToken;
        private static readonly object sync = new object();
    }
}
