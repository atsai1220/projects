﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boggle
{
    class BoggleState
    {
        private static string _boggleDB;
        public static string boggleDB
        {
            get
            {
                if (_boggleDB == null)
                {
                    _boggleDB = ConfigurationManager.ConnectionStrings["BoggleDB"].ConnectionString;
                }

                return _boggleDB;
            }
        }

        /// <summary>
        /// Instance of bogglestate
        /// </summary>
        private static BoggleState boggleState;

        /// <summary>
        /// Singleton pattern for bogglestate
        /// </summary>
        /// <returns></returns>
        public static BoggleState getBoggleState()
        {
            if (boggleState == null)
            {
                boggleState = new BoggleState();
            }
            return boggleState;
        }

        /// <summary>
        /// Creates a new game
        /// </summary>
        /// <param name="player1Token">User token of player 1</param>
        /// <param name="player1TimeLimit">Time limit requested by player 1</param>
        public string AddGame(string player1Token, int player1TimeLimit, SqlConnection conn, SqlTransaction trans)
        {
            using (SqlCommand command = new SqlCommand("INSERT INTO Games(Player1, Player2, Board, TimeLimit, StartTime) VALUES(@UserToken1, @UserToken2, @Board, @TimeLimit, @StartTime); SELECT SCOPE_IDENTITY() AS GameID",
                    conn,
                    trans))
            {
                command.Parameters.AddWithValue("@UserToken1", player1Token);
                command.Parameters.AddWithValue("@UserToken2", DBNull.Value);
                command.Parameters.AddWithValue("@Board", "");
                command.Parameters.AddWithValue("@TimeLimit", player1TimeLimit);
                command.Parameters.AddWithValue("@StartTime", DBNull.Value);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    return reader["GameID"].ToString();
                }
            }
        }

        /// <summary>
        /// Adds a word to a specific game for a specific player
        /// and sets score of player in table
        /// </summary>
        /// <param name="gameId">Id of game</param>
        /// <param name="userToken">User token of player in action</param>
        /// <param name="word">Word being played</param>
        /// <param name="score">Score acquired</param>
        public void AddWord(string gameId, string userToken, string word, int score, SqlConnection conn, SqlTransaction trans)
        {
            string script = "INSERT INTO Words(Word, GameID, Player, Score) VALUES (@Word, @GameID, @Player, @Score)";

            using (SqlCommand command = new SqlCommand(script, conn, trans))
            {
                command.Parameters.AddWithValue("@Word", word);
                command.Parameters.AddWithValue("@GameID", gameId);
                command.Parameters.AddWithValue("@Player", userToken);
                command.Parameters.AddWithValue("@Score", score);
                if (command.ExecuteNonQuery() != 1)
                {
                    throw new ArgumentException();
                }
            }
        }

        /// <summary>
        /// Removes a game from the list.
        /// </summary>
        /// <param name="gameId">The game to remove</param>
        public void CancelGame(string gameId, SqlConnection conn, SqlTransaction trans)
        {
            using (SqlCommand command = new SqlCommand("DELETE FROM Games WHERE GameID = @GameID",
                conn,
                trans))
            {
                command.Parameters.AddWithValue("@GameID", gameId);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Creates a user
        /// </summary>
        /// <param name="nickname">Nickname of player</param>
        /// <param name="userToken">User token of player</param>
        public void CreateUser(string nickname, string userToken, SqlConnection conn, SqlTransaction trans)
        {
            using (SqlCommand command = new SqlCommand("insert into Users(UserID, Nickname) values(@UserID, @Nickname)",
                    conn,
                    trans))
            {
                command.Parameters.AddWithValue("@UserID", userToken);
                command.Parameters.AddWithValue("@Nickname", nickname.Trim());

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns the board for the game.
        /// </summary>
        /// <param name="gameId">The gameId</param>
        /// <returns></returns>
        public string GetBoard(string gameId, SqlConnection conn, SqlTransaction trans)
        {
            string script = "SELECT Board FROM Games WHERE GameId = @GameId";

            using (SqlCommand command = new SqlCommand(script, conn, trans))
            {
                command.Parameters.AddWithValue("@GameId", gameId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    return reader.GetString(0);
                }
            }
        }

        /// <summary>
        /// Returns the nickname given the userToken
        /// </summary>
        /// <param name="userToken">User token of player</param>
        /// <returns></returns>
        public string GetNickname(string userToken, SqlConnection conn, SqlTransaction trans)
        {
            string script = "SELECT Nickname FROM Users WHERE UserId = @UserId";
            using (SqlCommand command = new SqlCommand(script, conn, trans))
            {
                command.Parameters.AddWithValue("@UserId", userToken);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    return reader.GetString(0);
                }
            }
        }

        /// <summary>
        /// Get the user tokens of both players
        /// </summary>
        /// <param name="gameId">The game id</param>
        /// <param name="player1Id">The user token of player 1</param>
        /// <param name="player2Id">The user token of player 2</param>
        public void GetPlayers(string gameId, out string player1Id, out string player2Id, SqlConnection conn, SqlTransaction trans)
        {
            string script = "SELECT Player1, Player2 FROM Games WHERE GameId = @GameId";
            // Column 0 is player 1
            using (SqlCommand command = new SqlCommand(script, conn, trans))
            {
                command.Parameters.AddWithValue("@GameId", gameId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        player1Id = reader.GetString(0);

                        if (reader.IsDBNull(1))
                        {
                            player2Id = "";
                        }
                        else
                        {
                            player2Id = reader.GetString(1);
                        }
                    }
                    else
                    {
                        throw new Exception("This will happen if gameId does not exist. This should not happen if service is correct");
                    }
                }
            }
        }

        /// <summary>
        /// Returns the score of a player
        /// </summary>
        /// <param name="gameId">Id of game</param>
        /// <param name="userToken">User token of player</param>
        /// <returns></returns>
        public int GetScore(string gameId, string userToken, SqlConnection conn, SqlTransaction trans)
        {
            string script = "SELECT SUM(Score) FROM Words WHERE GameID = @GameID AND Player = @UserId";
            using (SqlCommand command = new SqlCommand(script, conn, trans))
            {
                command.Parameters.AddWithValue("@GameID", gameId);
                command.Parameters.AddWithValue("@UserId", userToken);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    // If there are no words sum is null
                    if(reader.IsDBNull(0))
                    {
                        return 0;
                    }

                    return reader.GetInt32(0);
                }
            }
        }

        /// <summary>
        /// Returns time information from the game.
        /// </summary>
        /// <param name="gameId">The id of the game queried</param>
        /// <param name="timeLimit">The time limit of the game</param>
        /// <param name="startTime">The time that the game started</param>
        public void GetTime(string gameId, out int timeLimit, out long startTime, SqlConnection conn, SqlTransaction trans)
        {
            using (SqlCommand command = new SqlCommand("SELECT TimeLimit, StartTime FROM Games WHERE GameID = @GameID",
                conn,
                trans))
            {
                command.Parameters.AddWithValue("@GameID", gameId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    timeLimit = (int)reader["TimeLimit"];

                    DateTime time = (DateTime)reader["StartTime"];

                    startTime = time.Ticks;
                }
            }
        }

        /// <summary>
        /// Returns a List of strings that contains the played words of a player
        /// </summary>
        /// <param name="gameId">Id of game</param>
        /// <param name="userToken">Token of player</param>
        /// <returns></returns>
        public List<WordPair> GetWords(string gameId, string userToken, SqlConnection conn, SqlTransaction trans)
        {
            string script = "SELECT Word, Score FROM Words WHERE GameID = @GameID AND Player = @UserId";

            using (SqlCommand command = new SqlCommand(script, conn, trans))
            {
                command.Parameters.AddWithValue("@GameID", gameId);
                command.Parameters.AddWithValue("@UserId", userToken);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    List<WordPair> list = new List<WordPair>();

                    while (reader.Read())
                    {
                        WordPair pair = new WordPair();
                        pair.Word = (string)reader["Word"];
                        pair.Score = (int)reader["Score"];
                        list.Add(pair);
                    }

                    return list;
                }
            }
        }

        /// <summary>
        /// Starts a specific game
        /// </summary>
        /// <param name="gameId">Id of game</param>
        /// <param name="player2Token">Player 2 user token</param>
        /// <param name="player2TimeLimit">Player 2's specified time limit</param>
        /// <param name="startTime">System start time of game</param>
        /// <param name="board">String that rep the board</param>
        public void StartGame(string gameId, string player2Token, int player2TimeLimit, long startTime, string board, SqlConnection conn, SqlTransaction trans)
        {
            using (SqlCommand command = new SqlCommand("UPDATE Games SET Player2 = @Player2, TimeLimit = (TimeLimit + @Player2TimeLimit) / 2, StartTime = @StartTime, Board = @Board WHERE GameId = @GameID",
                    conn,
                    trans))
            {
                command.Parameters.AddWithValue("@GameID", gameId);
                command.Parameters.AddWithValue("@Player2", player2Token);
                command.Parameters.AddWithValue("@Player2TimeLimit", player2TimeLimit);
                command.Parameters.AddWithValue("@StartTime", new DateTime(startTime));
                command.Parameters.AddWithValue("@Board", board);

                command.ExecuteNonQuery();
            }
        }

        public bool GameExists(string gameId, SqlConnection conn, SqlTransaction trans)
        {
            using (SqlCommand command = new SqlCommand("SELECT GameID FROM Games WHERE GameID = @GameID",
                    conn,
                    trans))
            {
                command.Parameters.AddWithValue("@GameID", gameId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public bool PlayerExists(string userToken, SqlConnection conn, SqlTransaction trans)
        {
            using (SqlCommand command = new SqlCommand("SELECT UserID FROM Users WHERE UserID = @UserToken",
                       conn,
                       trans))
            {
                command.Parameters.AddWithValue("@UserToken", userToken);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public string GetLastGameId(SqlConnection conn, SqlTransaction trans)
        {
            using (SqlCommand command = new SqlCommand("SELECT MAX(GameId) FROM Games",
                conn,
                trans))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetValue(0).ToString();
                    }
                    else
                    {
                        throw new Exception("Sql query returned no results");
                    }
                }
            }
        }
    }
}
