﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Dynamic;
using static System.Net.HttpStatusCode;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading;

namespace Boggle
{
    /// <summary>
    /// Provides a way to start and stop the IIS web server from within the test
    /// cases.  If something prevents the test cases from stopping the web server,
    /// subsequent tests may not work properly until the stray process is killed
    /// manually.
    /// </summary>
    public static class IISAgent
    {
        // Reference to the running process
        private static Process process = null;

        /// <summary>
        /// Starts IIS
        /// </summary>
        public static void Start(string arguments)
        {
            if (process == null)
            {
                ProcessStartInfo info = new ProcessStartInfo(Properties.Resources.IIS_EXECUTABLE, arguments);
                info.WindowStyle = ProcessWindowStyle.Minimized;
                info.UseShellExecute = false;
                process = Process.Start(info);
            }
        }

        /// <summary>
        ///  Stops IIS
        /// </summary>
        public static void Stop()
        {
            if (process != null)
            {
                process.Kill();
            }
        }
    }
    [TestClass]
    public class BoggleTests
    {
        /// <summary>
        /// This is automatically run prior to all the tests to start the server
        /// </summary>
        [ClassInitialize()]
        public static void StartIIS(TestContext testContext)
        {
            //IISAgent.Start(@"/site:""BoggleService"" /apppool:""Clr4IntegratedAppPool"" /config:""..\..\..\.vs\config\applicationhost.config""");
        }

        /// <summary>
        /// This is automatically run when all tests have completed to stop the server
        /// </summary>
        [ClassCleanup()]
        public static void StopIIS()
        {
            //IISAgent.Stop();
        }

        private RestTestClient client = new RestTestClient("http://localhost:60000/BoggleService.svc");

        /// <summary>
        /// Tests CreateUser
        /// </summary>
        [TestMethod]
        public void CreateUserTest1()
        {
            dynamic player = new ExpandoObject();
            player.Nickname = "Andrew";
            Response r = client.DoPostAsync("users", player).Result;
            Assert.AreEqual(Created, r.Status);
        }

        /// <summary>
        /// Tests CreateUser for empty Nickname
        /// </summary>
        [TestMethod]
        public void CreateuserTest2()
        {
            dynamic player = new ExpandoObject();
            player.Nickname = "";
            Response r = client.DoPostAsync("users", player).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Tests CreateUser for null Nickname
        /// </summary>
        [TestMethod]
        public void CreateuserTest3()
        {
            dynamic player = new ExpandoObject();
            player.Nickname = null;
            Response r = client.DoPostAsync("users", player).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Tests JoinGame
        /// </summary>
        [TestMethod]
        public void JoinGameTest1()
        {
            dynamic player = new ExpandoObject();
            player.Nickname = "Andrew";
            Response response = client.DoPostAsync("users", player).Result;

            string userToken = response.Data.UserToken;

            dynamic data = new ExpandoObject();
            data.UserToken = response.Data.UserToken;
            data.TimeLimit = 25;

            Response dataR = client.DoPostAsync("games", data).Result;
            Assert.AreEqual(Accepted, dataR.Status);

            CancelGameTest1((String)data.UserToken);
        }

        /// <summary>
        /// Test that requesting a bad time limit responds with Forbidden
        /// </summary>
        [TestMethod]
        public void JoinGameTest2()
        {
            string userToken = CreateUser("User");

            dynamic data = new ExpandoObject();
            data.UserToken = userToken;
            data.TimeLimit = 0;

            Response response = client.DoPostAsync("games", data).Result;
            Assert.AreEqual(Forbidden, response);

            data.TimeLimit = 121;
            response = client.DoPostAsync("games", data).Result;
            Assert.AreEqual(Forbidden, response);
        }

        /// <summary>
        /// Test that joining a game twice responds with conflict.
        /// </summary>
        [TestMethod]
        public void JoinGameTest3()
        {
            string userToken = CreateUser("User");

            dynamic data = new ExpandoObject();
            data.UserToken = userToken;
            data.TimeLimit = 60;

            client.DoPostAsync("games", data);
            Response response = client.DoPostAsync("games", data).Result;

            Assert.AreEqual(Conflict, response);
        }

        // tests cancel 
        [TestMethod]
        public void CancelGameTest1(string userToken)
        {
            dynamic data = new ExpandoObject();
            data.UserToken = "thisSHOULDNTwork";

            Response dataR = client.DoPutAsync(data, "games").Result;
            Assert.AreEqual(Forbidden, dataR);

            data.UserToken = userToken;
            dataR = client.DoPutAsync(data, "games").Result;
            Assert.AreEqual(OK, dataR);
        }

        /// <summary>
        /// Test player 2 joining a pending game
        /// </summary>
        [TestMethod]
        public void Player2JoinGame()
        {
            FillIn();

            // Player 1
            dynamic player1 = new ExpandoObject();
            player1.Nickname = "Andrew";
            Response response = client.DoPostAsync("users", player1).Result;
            string userToken1 = response.Data.UserToken;

            dynamic data1 = new ExpandoObject();
            data1.UserToken = userToken1;
            data1.TimeLimit = 5;

            Response data1R = client.DoPostAsync("games", data1).Result;
            Assert.AreEqual(Accepted, data1R.Status);

            // Player 2
            dynamic player2 = new ExpandoObject();
            player2.Nickname = "Sam";
            Response player2R = client.DoPostAsync("users", player2).Result;
            string userToken2 = player2R.Data.UserToken;

            dynamic data2 = new ExpandoObject();
            data2.UserToken = userToken2;
            data2.TimeLimit = 5;

            Response dataR = client.DoPostAsync("games", data2).Result;
            Assert.AreEqual(Created, dataR.Status);
        }

        /// <summary>
        /// 
        /// 
        /// </summary>
        [TestMethod]
        public void PlayerWordTest()
        {
            BoggleBoard board;

            string userToken1 = CreateUser("Andrew");
            string userToken2 = CreateUser("Sam");

            string gameId1 = JoinGame(userToken1, 5);
            string gameId2 = JoinGame(userToken2, 5);

            Assert.AreEqual(gameId1, gameId2);

            // If Word is null or empty when trimmed -> 403 (Forbidden)
            Response responseEmpty = client.DoPutAsync(CreateWord(userToken1, ""), "games/" + gameId1).Result;
            Assert.AreEqual(Forbidden, responseEmpty.Status);

            Response responseSpace = client.DoPutAsync(CreateWord(userToken1, "   "), "games/" + gameId1).Result;
            Assert.AreEqual(Forbidden, responseSpace.Status);

            // token empty -> 403 (Forbidden)
            Response responseTokenEmpty = client.DoPutAsync(CreateWord("", "test"), "games/" + gameId1).Result;
            Assert.AreEqual(Forbidden, responseTokenEmpty.Status);

            // token null -> 403 (Forbidden)
            string testToken = null;
            Response responseTokenNull = client.DoPutAsync(CreateWord(testToken, "test"), "games/" + gameId1).Result;
            Assert.AreEqual(Forbidden, responseTokenNull.Status);

            // gameId null -> 403 (Forbidden)
            //string nullId = null;
            //Response responseGameNull = client.DoPutAsync(CreateWord(userToken1, "test"), "games/" + nullId).Result;
            //Assert.AreEqual(Forbidden, responseGameNull.Status);

            // gameId empty -> 403 (Forbidden)
            //string emptyId = "";
            //Response responseGameEmpty = client.DoPutAsync(CreateWord(testToken, "test"), "games/" + emptyId).Result;
            //Assert.AreEqual(Forbidden, responseGameEmpty.Status);

            // gameid dne -> 403 (Forbidden)
            string dneId = "999";
            Response responseGameInvalid = client.DoPutAsync(CreateWord(testToken, "test"), "games/" + dneId).Result;
            Assert.AreEqual(Forbidden, responseGameInvalid.Status);

            Response responseIdEmpty = client.DoPutAsync(CreateWord(userToken1, "AAAA"), "games/" + "NO").Result;
            Assert.AreEqual(Forbidden, responseIdEmpty.Status);

            // Legal word and token and gameid
            // TODO legal word with score
            Response gameBrief = client.DoGetAsync("games/" + gameId1, "yes").Result;
            string boardString = gameBrief.Data.Board;
            board = new BoggleBoard(boardString);
            int legalWordScore = -2;

            string line;
            Response wordResult = new Response();
            System.IO.StreamReader file = new System.IO.StreamReader("dictionary.txt");


            while ((line = file.ReadLine()) != null)
            {
                if (board.CanBeFormed(line) && line.Length < 3)
                {
                    wordResult = client.DoPutAsync(CreateWord(userToken1, line), "games/" + gameId1).Result;

                    string legalWord = wordResult.Data.Score;
                    int.TryParse(legalWord, out legalWordScore);
                }

                // Found
                if (legalWordScore == 0)
                {
                    break;
                }
            }

            while ((line = file.ReadLine()) != null)
            {
                if (board.CanBeFormed(line) && line.Length >= 3 && line.Length < 5)
                {
                    wordResult = client.DoPutAsync(CreateWord(userToken1, line), "games/" + gameId1).Result;

                    string legalWord = wordResult.Data.Score;
                    int.TryParse(legalWord, out legalWordScore);

                    if (legalWordScore == 1)
                    {
                        break;
                    }
                }
            }

            while ((line = file.ReadLine()) != null)
            {
                if (board.CanBeFormed(line) && line.Length == 5)
                {
                    wordResult = client.DoPutAsync(CreateWord(userToken1, line), "games/" + gameId1).Result;

                    string legalWord = wordResult.Data.Score;
                    int.TryParse(legalWord, out legalWordScore);

                    if (legalWordScore == 2)
                    {
                        break;
                    }
                }
            }

            while ((line = file.ReadLine()) != null)
            {
                if (board.CanBeFormed(line) && line.Length == 6)
                {
                    wordResult = client.DoPutAsync(CreateWord(userToken1, line), "games/" + gameId1).Result;
                    wordResult = client.DoPutAsync(CreateWord(userToken2, line), "games/" + gameId1).Result;
                    string legalWord = wordResult.Data.Score;
                    int.TryParse(legalWord, out legalWordScore);

                    if (legalWordScore == 3)
                    {
                        break;
                    }
                }
            }

            while ((line = file.ReadLine()) != null)
            {
                if (board.CanBeFormed(line) && line.Length == 7)
                {
                    wordResult = client.DoPutAsync(CreateWord(userToken1, line), "games/" + gameId1).Result;

                    string legalWord = wordResult.Data.Score;
                    int.TryParse(legalWord, out legalWordScore);

                    if (legalWordScore == 5)
                    {
                        break;
                    }
                }
            }

            while ((line = file.ReadLine()) != null)
            {
                if (board.CanBeFormed(line) && line.Length > 7)
                {
                    wordResult = client.DoPutAsync(CreateWord(userToken1, line), "games/" + gameId1).Result;

                    string legalWord = wordResult.Data.Score;
                    int.TryParse(legalWord, out legalWordScore);

                    if (legalWordScore == 11)
                    {
                        break;
                    }
                }
            }
            Assert.AreEqual(OK, wordResult.Status);

            wordResult = client.DoPutAsync(CreateWord(userToken1, "ZZZZZZZZZ"), "games/" + gameId1).Result;

            //int temp;
            //Response responseLegal = client.DoPutAsync(CreateWord(userToken1, "AAAAA"), "games/" + gameId1).Result;
            //Assert.AreEqual(OK, responseLegal.Status);
            //string score = responseLegal.Data.Score;
            //int.TryParse(score, out temp);

            //Assert.AreEqual(temp.ToString(), score);

            // Wait 10 seconds
            System.Threading.Thread.Sleep(5000);
            // If game state is NOT active -> 409 (Conflict)
            Response responseInactive = client.DoPutAsync(CreateWord(userToken1, "AAAA"), "games/" + gameId1).Result;
            Assert.AreEqual(Conflict, responseInactive.Status);
        }

        [TestMethod]
        public void TestPending()
        {
            string userToken = CreateUser("Sam");
            string gameId = JoinGame(userToken, 120);

            Response response = client.DoGetAsync("games/" + gameId, new string[] { "false" }).Result;
            Assert.AreEqual("pending", (string)response.Data.GameState);
        }

        [TestMethod]
        public void TestActive()
        {
            FillIn();

            string player1 = CreateUser("Player1");
            string player2 = CreateUser("Player2");

            string gameId1 = JoinGame(player1, 5);
            string gameId2 = JoinGame(player2, 5);

            Assert.AreEqual(gameId1, gameId2);

            Response response = client.DoGetAsync("games/" + gameId1, new string[0]).Result;

            Assert.AreEqual("active", (string)response.Data.GameState);
            string board = response.Data.Board;

            response = client.DoGetAsync("games/" + gameId1 + "?Brief=yes").Result;

            Assert.AreEqual("active", (string)response.Data.GameState);
            try
            {
                board = response.Data.Board;
                Assert.Fail();
            }
            catch { }
        }

        [TestMethod]
        public void TestCompleted()
        {
            FillIn();

            string player1 = CreateUser("Player1");
            string player2 = CreateUser("Player2");

            string gameId1 = JoinGame(player1, 5);
            string gameId2 = JoinGame(player2, 5);

            Assert.AreEqual(gameId1, gameId2);

            Response response;
            do
            {
                response = client.DoGetAsync("games/" + gameId1 + "?Brief=yes", new string[0]).Result;
                // Don't spam server with requests.
                Thread.Sleep(1000);
            } while (response.Data.GameState != "completed");

            string board;
            try
            {
                board = response.Data.Board;
                Assert.Fail();
            }
            catch { }

            // Wait two seconds to make sure that time recieved is not less than zero.
            Thread.Sleep(2000);

            response = client.DoGetAsync("games/" + gameId1, new string[0]).Result;
            board = response.Data.Board;

            Assert.AreEqual(0, (int) response.Data.TimeLeft);
        }

        [TestMethod]
        public void TestInvalid()
        {
            Response response = client.DoGetAsync("games/1000000000", new string[0]).Result;

            Assert.AreEqual(Forbidden, response);
        }

        /// <summary>
        /// If there is a pending game with one player, fills it in.
        /// </summary>
        private void FillIn()
        {
            Response response;
            do
            {
                string player = CreateUser("Player");

                dynamic data = new ExpandoObject();
                data.UserToken = player;
                data.TimeLimit = 5;

                response = client.DoPostAsync("games", data).Result;

            } while (response.Status != Created);
        }

        /// <summary>
        /// Helper method to create a user.
        /// </summary>
        /// <param name="nickname">Nickname of the user.</param>
        /// <returns>The usertoken of the user.</returns>
        private string CreateUser(string nickname)
        {
            dynamic data = new ExpandoObject();
            data.Nickname = nickname;

            Response response = client.DoPostAsync("users", data).Result;

            return response.Data.UserToken;
        }

        /// <summary>
        /// Helper method to join a game
        /// </summary>
        /// <param name="userToken">User token to join</param>
        /// <returns>The game id</returns>
        private string JoinGame(string userToken, int timeLimit)
        {
            dynamic data = new ExpandoObject();
            data.UserToken = userToken;
            data.TimeLimit = timeLimit;

            Response response = client.DoPostAsync("games", data).Result;

            return response.Data.GameID;
        }

        private dynamic CreateWord(string userToken, string word)
        {
            dynamic data = new ExpandoObject();
            data.UserToken = userToken;
            data.Word = word;

            return data;
        }
    }
}
