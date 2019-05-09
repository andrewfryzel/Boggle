//Andrew Fryzel and Jared Nay

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace BoggleService.Models
{

   
    /// <summary>
    /// Identifies a user. A user has a UserToken and a Nickname
    /// </summary>
    public class User
    {
        public String UserToken { get; set; }
        public String Nickname { get; set; }
    }

    /// <summary>
    /// Identifies the status of a game. 
    /// </summary>
    [DataContract]
    public class Status
    {
        [DataMember(EmitDefaultValue = false)]
        public String GameState { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Board { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLimit { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLeft { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Player Player1 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Player Player2 { get; set; }
    }

    /// <summary>
    /// A player has a Nickname and has a Score
    /// </summary>
    [DataContract]
    public class Player
    {
        public String UserToken { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Nickname { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<WordAndScore> WordsPlayed { get; set; }
    }

    /// <summary>
    /// The Boggle Game game
    /// </summary>
    [DataContract()]
    public class Game
    {
        [DataMember]
        public String GameID { get; set; }

        public String GameState { get; set; }

        public String Player1Token { get; set; }

        public int Player1Score { get; set; }

        public String Player2Token { get; set; }

        public int Player2Score { get; set; }

        public String Board { get; set; }

        public int TimeLimit { get; set; }

        public int TimeLeft { get; set; }

        [DataMember]
        public bool IsPending { get; set; }

        public bool IsActive { get; set; }

        public List<WordAndScore> WordsPlayed1 { get; set; }

        public List<WordAndScore> WordsPlayed2 { get; set; }

        public SortedSet<String> AllWords;
    }

    /// <summary>
    /// A game request (used with the PostJoinGame method)
    /// </summary>
    public class GameRequest
    {
        public String UserToken { get; set; }
        public int TimeLimit { get; set; }
    }

    /// <summary>
    /// A played has a UserToken and a Word
    /// </summary>
    public class PlayedWord
    {
        public String UserToken { get; set; }
        public String Word { get; set; }
    }

    /// <summary>
    /// A WordandScore has exactly that, a Word and a Score
    /// </summary>
    public class WordAndScore
    {
        public String Word { get; set; }
        public int Score { get; set; }
    }
}