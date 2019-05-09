//Andrew Fryzel and Jared Nay

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

// RESOURCES
// https://github.com/UofU-CS3500-S19/examples/blob/master/ToDoListClient/ToDoListClient/Controller3.cs
// http://ice.eng.utah.edu/api.html

namespace PS9
{
    /// <summary>
    /// The Controller class for the MVC BoggleClient game
    /// </summary>
    class Controller
    {
        private IBoggleClientView view;
        private string domain;
        private string user;
        private HttpClient client;
        private string GameID;
        private bool active = false;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Timer offTimer;
        private CancellationTokenSource tokenSource; //https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource?view=netframework-4.7.2

        /// <summary>
        /// The Controller constructor
        /// </summary>      
        public Controller(IBoggleClientView window)
        {
            this.view = window;
            user = "0";
            window.CloseEvent += HandleClose;
            window.NewEvent += HandleNew;
            window.HelpEvent += HandleHelp;
            window.RegisterEvent += MakeUser;
            window.StartGameEvent += JoinGame;
            window.WordEvent += PlayWord;
            window.RefreshGame += GameStatus;
            window.ResetEvent += Cancel;
            window.CancelEvent += Cancel;

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += TimerPlay;
            offTimer = new System.Windows.Forms.Timer();
            offTimer.Interval = 1000;
            offTimer.Tick += TimerOff;
            offTimer.Start();
        }

        /// <summary>
        /// Close the window
        /// </summary>
        private void HandleClose()
        {
            view.DoClose();
        }

        /// <summary>
        /// Opens a new window
        /// </summary>
        private void HandleNew()
        {
            view.OpenNew();
        }
        /// <summary>
        /// Displays the help message
        /// </summary>
        private void HandleHelp()
        {
            view.DoHelp();
        }


        /// <summary>
        /// Uses the BoggleGame API to create and Post a user and domain
        /// </summary>
        /// <param name="name"></param>
        /// <param name="domain"></param>
        private async void MakeUser(String name, string domain)
        {
            //from his REST examples
            try
            {
                this.domain = domain;
                using (client = CreateClient(this.domain))
                {
                    user = name;
                    //dynamic user = new ExpandoObject();
                    //user.Nickname = name;

                    tokenSource = new CancellationTokenSource();
                    view.EnableControls(false);
                    StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                    //StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("users", content, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        String result = await response.Content.ReadAsStringAsync();
                        user = (String)JsonConvert.DeserializeObject(result);
                    }
                    else
                    {
                        MessageBox.Show("Error Encountered! " + response.StatusCode + "\nPlease Restart The Game And Try Again!");
                    }
                }
            }
            catch (TaskCanceledException)
            {

            }

            //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/try-catch-finally
            finally
            {
                // change this probably
                view.EnableControls(true);
            }
        }

        /// <summary>
        /// Uses the BoggleGame API to Join a game through Post
        /// </summary>
        /// <param name="time"></param>
        private async void JoinGame(string time)
        {
            try
            {
                using (client = CreateClient(this.domain))
                {
                    dynamic obj = new ExpandoObject();
                    obj.UserToken = user;
                    obj.TimeLimit = time;

                    view.EnableControls(false);
                    tokenSource = new CancellationTokenSource();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("games", content, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        String result = response.Content.ReadAsStringAsync().Result;
                        dynamic orgs = JsonConvert.DeserializeObject(result);
                        this.GameID = orgs.GameID;
                    }
                    else
                    {
                        MessageBox.Show("Error Encountered! " + response.StatusCode + "\nPlease Restart The Game And Try Again!");
                    }
                }
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {

            }
        }


        // Get Request
        /// <summary>
        /// Uses the BoggleGame API to Get game status information
        /// </summary>
        /// <returns></returns>
        private bool GameStatus()
        {
            try
            {
                HttpResponseMessage response;

                using (client = CreateClient(this.domain))
                {
                    tokenSource = new CancellationTokenSource();
                    String url = String.Format("games/{0}/{1}", this.GameID, false);
                    response = client.GetAsync(url).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        String result = response.Content.ReadAsStringAsync().Result;
                        dynamic obj = JsonConvert.DeserializeObject(result);

                        if (obj.GameState == "active")
                        {
                            active = true;
                            view.GameState = true;
                            view.Board((String)obj.Board);
                            view.GameState = true;
                            view.EnableWordsTextBox(true);
                            view.SetName((String)obj.Player1.Nickname, (String)obj.Player2.Nickname);
                            view.Timer = obj.TimeLeft;
                            view.SetTime();
                            view.SetScore((string)obj.Player1.Score, (string)obj.Player2.Score, (String)obj.Player1.Nickname, (String)obj.Player2.Nickname);
                            return true;
                        }
                        else
                        {
                            PlayedWords(obj);
                            return false;
                        }
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }



        /// <summary>
        /// Use the BoggleGame API to Play (Put) a Word. 
        /// </summary>
        /// <param name="word"></param>
        private async void PlayWord(string word)
        {
            try
            {
                using (client = CreateClient(this.domain))
                {
                    dynamic obj = new ExpandoObject();
                    obj.UserToken = user;
                    obj.Word = word;
                    //REMOVE
                    view.GameState = true;
                    tokenSource = new CancellationTokenSource();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
                    string url = String.Format("games/{0}/", this.GameID);


                    HttpResponseMessage response = await client.PutAsync(url, content, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        String result = await response.Content.ReadAsStringAsync();
                        dynamic obj2 = JsonConvert.DeserializeObject(result);
                    }
                }
            }
            catch
            {

            }
            finally
            {

            }
        }

        /// <summary>
        /// Timer that is used during gameplay (while the game state is active)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerPlay(object sender, EventArgs e)
        {
            if (active)
            {
                GameStatus();
            }
            else
            {
                offTimer.Start();
                timer.Stop();
            }
        }

        /// <summary>
        /// Timer that is used outside of game play (while the game state is inactive)
        /// </summary>
        private void TimerOff(object sender, EventArgs e)
        {
            if (user != null)
            {
                if (GameID != null)
                {
                    if (GameStatus())
                    {
                        offTimer.Stop();
                        timer.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Used with the other API methods to Create a Boggle Game client using the URL
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        private static HttpClient CreateClient(String domain)//Dont actually use this domain but need to? 
        {
            HttpClient client = new HttpClient();

            client.BaseAddress = new Uri("http://ice.eng.utah.edu/BoggleService/"); // use domain with this but idk how
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            return client;
        }

        /// <summary>
        /// Used to cancel a current request
        /// </summary>
        public void Cancel()
        {
            if (!view.GameState)
            {
                tokenSource.Cancel();
                if (!view.UserRegistered)
                {
                    view.EnableControls(true);
                    view.StopTime(false);
                }
                else
                {
                    CancelRequest();
                    view.StopTime(true);
                }
            }
            else
            {
                view.Clear();
                GameID = null;
                view.StopTime(true);
                active = false;
                view.EnableControls(true);

            }

        }

        /// <summary>
        /// The words that have been played
        /// </summary>
        /// <param name="obj"></param>
        public void PlayedWords(dynamic obj)
        {
            if (obj != null)
            {
                view.WordsPlayed(obj.Player1.WordsPlayed, obj.Player2.WordsPlayed);
            }
            Cancel();
        }

        /// <summary>
        /// Used with the Cancel() method. Calls the API to cancel current action
        /// </summary>
        private async void CancelRequest()
        {
            try
            {

                using (client = CreateClient(this.domain))
                {

                    StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                    string url = String.Format("games");
                    HttpResponseMessage response = await client.PutAsync(url, content);
                }
            }
            catch
            {

            }
        }

    }
}