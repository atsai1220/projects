﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows.Forms;
using System.Threading;

namespace BoggleClient
{
    class Controller
    {
        private IBoggleView boggleWindow;
        private Model boggleModel;

        /// <summary>
        /// Begin controlling boggleWindow
        /// </summary>
        public Controller()
        {
            boggleModel = new Model();
        }

        public Controller(IBoggleView _boggleWindow) : base()
        {
            boggleModel = new Model();

            boggleWindow = _boggleWindow;
            boggleWindow.registerPlayerEvent += registerPlayer;
            boggleWindow.joinGameEvent += (timeLimit) => joinGame(timeLimit);
            boggleWindow.joinCanceledEvent += cancelJoinRequest;
            boggleWindow.closeEvent += HandleCloseEvent;
            boggleWindow.helpEvent += HandleHelpEvent;

            // testInit();
        }

        /// <summary>
        /// TODO REmove this (is for testing)
        /// </summary>
        private void testInit()
        {
            registerPlayer("asdf");

            joinGame(120);
        }


        /// <summary>
        /// Create HttpClient to communicate with server
        /// </summary>
        /// <returns></returns>
        private static HttpClient CreateClient()
        {
            // TODO change Uri to update with input from user
            // Create a client whose base address is the GitHub server
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://bogglecs3500s16.azurewebsites.net");

            // Tell the server that the client will accept this particular type of response data
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");



            // There is more client configuration to do, depending on the request.
            return client;
        }



        /// <summary>
        /// Registers the player with the boggle server.
        /// Sets the id of the player in the model.
        /// 
        /// POST /BoggleService.svc/users
        /// {
        ///     "Nickname": "Joe"
        /// }
        /// </summary>
        /// <param name="nickName">Desired name of the player.</param>
        private async void registerPlayer(string nickName)
        {
            // TODO implement Player to get token
            using (HttpClient client = CreateClient())
            {
                dynamic player = new ExpandoObject();
                //player.Nickname = boggleModel.GetName();
                player.Nickname = nickName;

                // To send a POST request, we must include the serialized parameter object
                // in the body of the request.
                StringContent content = new StringContent(JsonConvert.SerializeObject(player), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("/BoggleService.svc/users", content);

                if (response.IsSuccessStatusCode)
                {
                    // TODO get and set user token to player
                    String result = response.Content.ReadAsStringAsync().Result;
                    dynamic serverResponse = JsonConvert.DeserializeObject(result);
                    Console.WriteLine(serverResponse);

                    boggleModel.UserToken = serverResponse.UserToken;
                }
                else
                {
                    // TODO change
                    handleMessagePopUpEvent("If Nickname is null, or is empty when trimmed, responds with status 403 (Forbidden).");
                }
            }
        }

        /// <summary>
        /// Handles a pop-up dialog with passed message
        /// </summary>
        /// <param name="_message"></param>
        private void handleMessagePopUpEvent(string _message)
        {
            boggleWindow.MessagePopUp(_message);
        }

        //private Timer gameCheckTimer;

        // Sam
        /// <summary>
        /// Joins a game or starts a new game.
        /// 
        /// State should go to waiting for game to start.
        /// </summary>
        private async void joinGame(int timeLimit)
        {
            using (HttpClient client = CreateClient())
            {
                dynamic data = new ExpandoObject();

                data.UserToken = boggleModel.UserToken;
                data.TimeLimit = timeLimit;

                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("/BoggleService.svc/games", content);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    dynamic responseObject = JsonConvert.DeserializeObject(result);

                    boggleModel.GameId = responseObject.GameID;

                    previousGameState = "pending";

                    await checkGameStatus();

                    /*

                    gameCheckTimer = new Timer();

                    gameCheckTimer.Tick += (a, b) => checkGameStatus();
                    
                    gameCheckTimer.Interval = 10000;

                    gameCheckTimer.Start();*/
                }
                else
                {
                    // TODO Display error message
                }
            }
        }

        private string previousGameState;

        private async Task checkGameStatus()
        {
            while (true)
            {
                using (HttpClient client = CreateClient())
                {
                    string url = String.Format("/BoggleService.svc/games/{0}", boggleModel.GameId);

                    //  Request the short version
                    if (previousGameState.Equals("active"))
                    {
                        url += "?yes";
                    }

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();

                        dynamic gameStatus = JsonConvert.DeserializeObject(result);

                        Console.WriteLine(gameStatus.GameState);

                        string gameState = gameStatus.GameState;

                        if (gameState.Equals("pending"))
                        {
                            // Wait
                        }
                        if (gameState.Equals("active"))
                        {
                            if (previousGameState.Equals("pending"))
                            {
                                HandleGameStarted(gameStatus);
                            }
                            else if (previousGameState.Equals("active"))
                            {
                                HandleGameStateUpdate(gameStatus);
                            }
                        }
                        if (gameState.Equals("completed"))
                        {
                            if (previousGameState.Equals("active"))
                            {
                                HandleGameStateUpdate(gameStatus);
                            }
                            if (previousGameState.Equals("completed"))
                            {
                                HandleGameEndEvent(gameStatus);

                                return;
                            }
                        }

                        previousGameState = gameState;
                    }
                    else
                    {
                        // TODO display error message.
                    }
                }

                Task wait = new Task(() => Thread.Sleep(1000));
                wait.Start();
                await wait;
            }
        }

        // Sam
        /// <summary>
        /// Cancel a pending request to join a game.
        /// </summary>
        private async void cancelJoinRequest()
        {
            using (HttpClient client = CreateClient())
            {
                dynamic data = new ExpandoObject();
                data.UserToken = boggleModel.UserToken;

                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PutAsync("/BoggleService.svc/games", content);

                if (response.IsSuccessStatusCode)
                {

                }
                else
                {
                    // TODO display error message
                }
            }
        }

        /// <summary>
        /// Handles Close event
        /// </summary>
        private void HandleCloseEvent()
        {
            boggleWindow.DoClose();
        }

        // ASm
        private void HandleAboutEvent()
        {

        }

        //Andrew
        private void HandleHelpEvent()
        {
            boggleWindow.MessagePopUp(
                @"How to:
1) Register your account with a nickname
2) Join a game with a specified time
3) Wait till another player joins
4) Start!

Rules:
Create strings that are legal words for points!
< 3 characters 0 pt
3 - 4 characters 1 pt
5 characters 2 pts
6 characters 3 pts
7 characters 5 pts
> 7 characters 11 pts

otherwise, -1 pt");
        }

        //Andrew
        /// <summary>
        /// dispay end game with list of words and score
        /// </summary>
        private void HandleGameEndEvent(dynamic gameStatus)
        {

        }

        // Sam
        /// <summary>
        /// populate cubes and set timer
        /// </summary>
        private void HandleGameStarted(dynamic gameStatus)
        {
            boggleWindow.BoardString = gameStatus.Board;

            boggleWindow.TimeRemaining = gameStatus.TimeLeft;

            boggleWindow.Nickname = gameStatus.Player1.Nickname;
            boggleWindow.Player1Score = gameStatus.Player1.Score;

            boggleWindow.Player2Nickname = gameStatus.Player2.Nickname;
            boggleWindow.Player2Score = gameStatus.Player2.Score;
        }

        private void HandleGameStateUpdate(dynamic gameStatus)
        {
            boggleWindow.TimeRemaining = gameStatus.TimeLeft;

            boggleWindow.Player1Score = gameStatus.Player1.Score;
            boggleWindow.Player2Score = gameStatus.Player2.Score;
        }

        // Andrew
        /// <summary>
        /// send to server for points
        /// </summary>
        private void HandleWordEnteredEvent()
        {

        }
    }
}
