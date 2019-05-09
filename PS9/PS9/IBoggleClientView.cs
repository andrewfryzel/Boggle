//Andrew Fryzel and Jared Nay

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS9
{
 
    public interface IBoggleClientView
    {
        event Action<String, String> RegisterEvent;

        event Action NewEvent;
        event Action HelpEvent;
        event Action CloseEvent;
        event Action CancelEvent;
        event Action ResetEvent;
        event Action<String> WordEvent;
        event Action<String> StartGameEvent;
        event Func<bool> RefreshGame;
        event Action GameStatus;




        void OpenNew();
        void DoClose();
        void DoHelp();
       
        void SetTime();
        void Board(string board);
        bool GameState { get; set; }
        double Timer { get; set; }
        bool UserRegistered { get; set; }
        void SetName(String name1, String name2);
        void SetScore(String p1, String p2, String p1Name, String p2Name);
        void EnableControls(bool state);
        void StopTime(bool state);
        void Clear();
        void WordsPlayed(dynamic p1, dynamic p2);
        void EnableWordsTextBox(bool state);
    }
}
