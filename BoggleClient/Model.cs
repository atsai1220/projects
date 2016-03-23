﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoggleClient
{
    class Model
    {
        private string[,] board { get; set; }
        private string[,] cubes { get; set; }


        private int wordsPlayed { get; set; }
        private string domain { get; set; }
        private string gameToken { get; set; }

        private Player you;
        private Player opp;


        public Model()
        {
            string[,] board = new string[4, 4];
            string[,] cubes = new string[16, 6];
            wordsPlayed = 0;
        }

        public Model(string _domain) : base()
        {
            domain = _domain;
        }

        public string GetName()
        {
            return you.GetName();
        }
    }


    /// <summary>
    /// Player class.
    /// This will hold player information
    /// </summary>
    class Player
    {
        private string id { get; set; }
        private string nickname { get; set; }
        private int score { get; set; }

        public Player()
        {
            id = null;
            nickname = null;
            score = 0;
        }

        public Player(string _id, string _nickname)
        {
            if (_id != null && _nickname != null)
            {
                id = _id;
                nickname = _nickname;
            }
        }

        public string GetName()
        {
            return nickname;
        }
    }


}
