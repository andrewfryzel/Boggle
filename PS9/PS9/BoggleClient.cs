//Andrew Fryzel and Jared Nay

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;


namespace PS9
{

    public partial class BoggleClient : Form, IBoggleClientView
    {
        /// <summary>
        /// The Boggle Client. Initializes and sets variables to their appropriate properties.
        /// </summary>
        public BoggleClient()
        {
            InitializeComponent();
            EnableControls(false);
            p1Final.Visible = false;
            p2Final.Visible = false;
            listBox2.Visible = false;
            listBox1.Visible = false;
            timeBox.Visible = false;
            timeLabel.Visible = false;
            startGame.Visible = false;
            p1Score.Visible = false;
            p2Score.Visible = false;

            Player1.Visible = false;
            Player2.Visible = false;
            textBox1.Visible = false;
            textBox1.Enabled = false;
            //p1Final.Visible = false;
            //p2Final.Visible = false;

            p1Words.Visible = false;
            p2Words.Visible = false;
        }

        public event Action NewEvent;
        public event Action HelpEvent;
        public event Action ResetEvent;
        public event Action CloseEvent;
        public event Action CancelEvent;
        public event Func<bool> RefreshGame;
        public event Action<String> WordEvent;
        public event Action<String> StartGameEvent;
        public event Action<String, String> RegisterEvent;
        public event Action GameStatus;

        private bool userRegistered = false;

        public double timer = 0;
        private bool gameState = false;
        /// <summary>
        /// Getter and Setter for our game timer. 
        /// </summary>
        public double Timer
        {
            get { return timer; }
            set { timer = value; }
        }
        /// <summary>
        /// Game state Getter and Setter. 
        /// </summary>
        public bool GameState
        {
            get { return gameState; }
            set { gameState = value; }
        }
        /// <summary>
        /// Getter and Setter
        /// </summary>
        public bool UserRegistered
        {
            get { return userRegistered; }
            set { userRegistered = value; }
        }

        //-----------------------------------Methods that handle Events-----------------------------------

        private void FileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Calls an event that will open a new Boggle Client
        /// </summary>
        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (NewEvent != null)
            {
                NewEvent();
            }
        }

        /// <summary>
        /// Creates a new window of the BoggleClient
        /// </summary>
        public void OpenNew()
        {
            BoggleClientApplicationContext.GetContext().RunNew();
        }

        /// <summary>
        /// Calls an event that will dispaly a Help Message
        /// </summary>
        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (HelpEvent != null)
            {
                HelpEvent();
            }
        }

        /// <summary>
        /// Displays the help message box
        /// </summary>
        public void DoHelp()
        {

            MessageBox.Show("1)Enter a Username and Domain. The domain is how you and a friend can play together. " +
                "\n\n2)Enter the same domain as them. Input a time to be used for the game lenght. " +
                "\n\n3)The two players times will be averaged together to determine the game length. " +
                "\n\n4)Try to match as many adjacent letters as possible to form words." +
                "\n\nThree- and four-letter words are worth one point, five-letter words are worth two points, " +
                "six-letter words are worth three points, seven-letter words are worth five points, and longer words are worth 11 points. " +
                "\n\nWords less than two letters or words containing numbers are worth zero points.");
        }
        /// <summary>
        /// Calls an event that will close a Boggle Client
        /// </summary>
        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CloseEvent != null)
            {
                CloseEvent();
            }
        }
        /// <summary>
        /// Closes the specified Boggle Client
        /// </summary>
        public void DoClose()
        {
            this.Close();
        }

        /// <summary>
        /// Events and actions when the Register button is clicked. 
        /// Sets a players username and desired domain.
        /// </summary>
        private void registerButton_Click_1(object sender, EventArgs e)
        {

            if (RegisterEvent != null)
            {
                RegisterEvent(registerBox1.Text.Trim(), domainBox.Text.Trim());
                registerButton.Enabled = false;
            }

            timeBox.Visible = true;
            timeLabel.Visible = true;
            startGame.Visible = true;
            domainBox.Visible = false;
            domainLabel.Visible = false;
            nick.Visible = false;
            registerBox1.Visible = false;
            registerButton.Visible = false;
            timeBox.Enabled = true;

        }

        /// <summary>
        /// Event that is created when the start game button is clicked
        /// </summary>
        private void startGame_Click(object sender, EventArgs e)
        {
            StartGame(true);
            if (StartGameEvent != null)
            {
                StartGameEvent(timeBox.Text);
                timeBox.Text = "";

            }
            timeLabel.Font = new Font("Serif", 10);

            timeBox.Visible = false;
            timeLabel.Visible = false;
            splitContainer1.Panel2.Show();
        }

        /// <summary>
        /// Disables/Enables the domainBox depending on if there is text in the register box (username)
        /// </summary>
        private void registerBox1_TextChanged(object sender, EventArgs e)
        {
            domainBox.Enabled = registerBox1.Text.Trim().Length > 0;
        }

        /// <summary>
        ///  Disables/Enables the registerButton depending on if there is text in the register box (username)
        /// </summary>
        private void domainBox_TextChanged_1(object sender, EventArgs e)
        {
            registerButton.Enabled = registerBox1.Text.Trim().Length > 0;
        }

        //-----------------------------------Methods-----------------------------------

        /// <summary>
        /// Initializes the game board and sets up how the boggle game will look
        /// </summary>
        public void Board(string board)
        {
            char[] arr = board.ToCharArray();

            label1.Text = arr[0].ToString();
            label2.Text = arr[1].ToString();
            label3.Text = arr[2].ToString();
            label4.Text = arr[3].ToString();
            label5.Text = arr[4].ToString();
            label6.Text = arr[5].ToString();
            label7.Text = arr[6].ToString();
            label8.Text = arr[7].ToString();
            label9.Text = arr[8].ToString();
            label10.Text = arr[9].ToString();
            label11.Text = arr[10].ToString();
            label12.Text = arr[11].ToString();
            label13.Text = arr[12].ToString();
            label14.Text = arr[13].ToString();
            label15.Text = arr[14].ToString();
            label16.Text = arr[15].ToString();
        }

        /// <summary>
        /// Checks if a word is a valid answer
        /// Used with the textBox1_KeyPress method
        /// </summary>

        private bool CheckWord(String word)
        {

            if (word.Length < 3)
            {
                return false;
            }

            // Check to ensure there aren't any numbers in string
            bool isDigit = word.Any(c => char.IsDigit(c));
            if (isDigit)
            {
                return false;
            }

            //if(the word is a valid word in relation to the given char blocks)
            else
                return true;
        }

        /// <summary>
        /// Sets objects to visible or not depending on if the StartGame button has been pressed
        /// </summary>
        /// <param name="flag"></param>
        public void StartGame(bool flag)
        {
            registerButton.Visible = false;
            registerBox1.Visible = false;
            domainBox.Visible = false;
            startGame.Visible = false;
            domainLabel.Visible = false;
            nick.Visible = false;
            Player1.Visible = true;
            Player2.Visible = true;
            p1Score.Visible = true;
            p2Score.Visible = true;
            textBox1.Visible = true;
        }

        /// <summary>
        /// Resets the game state and calls an event to do that
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Board();
            if (ResetEvent != null)
            {
                ResetEvent();
            }
            ResetCancelHelper();
        }

        /// <summary>
        /// Cancels the current action
        /// </summary>
        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (CancelEvent != null)
            {
                CancelEvent();
            }
            ResetCancelHelper();
        }

        /// <summary>
        /// A helper method for Resetting or Canceling a game state/ action
        /// </summary>
        public void ResetCancelHelper()
        {
            registerButton.Visible = true;
            registerBox1.Visible = true;
            domainBox.Visible = true;
            startGame.Visible = true;
            domainLabel.Visible = true;
            nick.Visible = true;
            registerButton.Enabled = true;
            Player1.Visible = false;
            Player2.Visible = false;
            p1Score.Visible = false;
            p2Score.Visible = false;
            startGame.Visible = false;
            timeLabel.Visible = false;
            textBox1.Visible = false;
            textBox1.Text = "";
            textBox1.Enabled = false;
            listBox2.Visible = false;
            listBox1.Visible = false;
            timeLabel.Font = new Font("Serif", 8);
            registerBox1.Text = "";
            domainBox.Text = "";
            p1Final.Visible = false;
            p2Final.Visible = false;
            p1Words.Text = "";
            p2Words.Text = "";
            p1Words.Visible = false;
            p2Words.Visible = false;
            timeLabel.Text = "Enter Desired Game Time";
        }
        /// <summary>
        /// Set the player name on the board to reflect what is stored on the server
        /// </summary>
        public void SetName(String name1, String name2)
        {
            Player1.Text = name1 + " Score";
            Player2.Text = name2 + " Score";
            p1Words.Text = name1 + " Words:"; ;
            p2Words.Text = name2 + " Words:";
        }

        /// <summary>
        /// Set the player score on the board to reflet what is stored on the server
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p1Name"></param>
        /// <param name="p2Name"></param>
        public void SetScore(String p1, String p2, String p1Name, String p2Name)
        {
            p1Score.Text = p1;
            p2Score.Text = p2;

            p1Final.Text = p1Name + " Final Score : " + p1;
            p2Final.Text = p2Name + " Final Score : " + p2;
        }

        /// <summary>
        /// The text box that a user will input a word into and press enter to play. 
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                WordEvent(textBox1.Text);
                e.Handled = true;
                //reset the text box
                textBox1.Text = "";
            }
        }


        /// <summary>
        /// Enables/disables the game controls depending on the game state
        /// </summary>
        public void EnableControls(bool state)
        {
            registerButton.Enabled = state && !UserRegistered;

            registerButton.Enabled = state;

            domainBox.Enabled = state && UserRegistered && registerBox1.Text.Trim().Length > 0; ;

            startGame.Enabled = state && UserRegistered && timeBox.Text.Trim().Length > 0;
        }

        /// <summary>
        /// Enables the word input text box
        /// </summary>
        public void EnableWordsTextBox(bool state)
        {
            textBox1.Enabled = state;
            ShowLetters();
        }

        /// <summary>
        /// Set the time remaining on the game board
        /// </summary>
        public void SetTime()
        {
            timeLabel.Visible = true;
            timeLabel.Text = "Time Remaining " + Timer.ToString();
            if (Timer.ToString().Equals("0"))
            {
                timeLabel.Text = "Time Remaining 0";
                EnableControls(false);
            }
        }

        /// <summary>
        /// Enables the start button if a time is input into the time text box.
        /// </summary>
        private void timeBox_TextChanged(object sender, EventArgs e)
        {
            startGame.Enabled = timeBox.Text.Trim().Length > 0;
        }

        /// <summary>
        /// Clears the board and resets certain properties
        /// </summary>
        public void Clear()
        {
            //The board has to be this length to prevent null instances when passing it into Board();
            String board = "                ";
            Board(board);

            p1Score.Text = "";
            p2Score.Text = "";

            Player1.Text = "";
            Player2.Text = "";
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        public void StopTime(bool state)
        {
            timeBox.Enabled = state;
            startGame.Enabled = state;
        }

        /// <summary>
        /// Gets and displays the words played by both players after the game is over
        /// </summary>
        public void WordsPlayed(dynamic p1, dynamic p2)
        {
            HideLetters();
            p1Final.Visible = true;
            p2Final.Visible = true;
            textBox1.Enabled = false;
            timeLabel.Visible = false;
            p1Words.Visible = true;
            p2Words.Visible = true;
            listBox1.Visible = true;
            listBox2.Visible = true;
            double finalP1 = 0;
            double finalP2 = 0;

            Dictionary<String, Double> p1List = new Dictionary<String, Double>();
            Dictionary<String, Double> p2List = new Dictionary<String, Double>();

            // Add the object values from the server to a dictionary
            foreach (dynamic item in p1)
            {
                p1List.Add((String)item.Word, (double)item.Score);
            }
            foreach (dynamic item in p2)
            {
                p2List.Add((String)item.Word, (double)item.Score);
            }

            // Use those values in the dictionary to display words played and their values
            foreach (var item in p1List)
            {
                listBox1.Items.Add("Word: " + item.Key + " Value: " + item.Value + "\n");
                finalP1 += item.Value;
            }
            foreach (var item in p2List)
            {
                listBox2.Items.Add("Word: " + item.Key + " Value: " + item.Value + "\n");
                finalP2 += item.Value;
            }
        }
        /// <summary>
        /// Hides the game board letters
        /// </summary>
        public void HideLetters()
        {
            label1.Visible = false;
            label2.Visible = false;
            label3.Visible = false;
            label4.Visible = false;
            label5.Visible = false;
            label6.Visible = false;
            label7.Visible = false;
            label8.Visible = false;
            label9.Visible = false;
            label10.Visible = false;
            label11.Visible = false;
            label12.Visible = false;
            label13.Visible = false;
            label14.Visible = false;
            label15.Visible = false;
            label16.Visible = false;
        }

        /// <summary>
        /// Shows the game board letters
        /// </summary>
        public void ShowLetters()
        {
            label1.Visible = true;
            label2.Visible = true;
            label3.Visible = true;
            label4.Visible = true;
            label5.Visible = true;
            label6.Visible = true;
            label7.Visible = true;
            label8.Visible = true;
            label9.Visible = true;
            label10.Visible = true;
            label11.Visible = true;
            label12.Visible = true;
            label13.Visible = true;
            label14.Visible = true;
            label15.Visible = true;
            label16.Visible = true;
        }
    }
}

