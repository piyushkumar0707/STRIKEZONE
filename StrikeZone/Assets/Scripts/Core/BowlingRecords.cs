using UnityEngine;

namespace StrikeZone.Core
{
    /// <summary>Small, device-local records that persist between game launches.</summary>
    public static class BowlingRecords
    {
        private const string ClassicBestKey = "StrikeZone.Record.ClassicBest";
        private const string ChallengeBestKey = "StrikeZone.Record.ChallengeBest";
        private const string PlayerOneWinsKey = "StrikeZone.Record.PlayerOneWins";
        private const string PlayerTwoWinsKey = "StrikeZone.Record.PlayerTwoWins";
        private const string DrawsKey = "StrikeZone.Record.Draws";

        public static int ClassicBestScore => PlayerPrefs.GetInt(ClassicBestKey, 0);
        public static int ChallengeBestScore => PlayerPrefs.GetInt(ChallengeBestKey, 0);
        public static int PlayerOneWins => PlayerPrefs.GetInt(PlayerOneWinsKey, 0);
        public static int PlayerTwoWins => PlayerPrefs.GetInt(PlayerTwoWinsKey, 0);
        public static int Draws => PlayerPrefs.GetInt(DrawsKey, 0);

        public static bool RecordClassic(int score)
        {
            return UpdateHighScore(ClassicBestKey, score);
        }

        public static bool RecordChallenge(int score)
        {
            return UpdateHighScore(ChallengeBestKey, score);
        }

        public static string RecordLocalMatch(int playerOneScore, int playerTwoScore)
        {
            if (playerOneScore == playerTwoScore)
            {
                PlayerPrefs.SetInt(DrawsKey, PlayerPrefs.GetInt(DrawsKey) + 1);
                PlayerPrefs.Save();
                return "MATCH DRAW RECORDED";
            }

            var playerOneWon = playerOneScore > playerTwoScore;
            var winsKey = playerOneWon ? PlayerOneWinsKey : PlayerTwoWinsKey;
            PlayerPrefs.SetInt(winsKey, PlayerPrefs.GetInt(winsKey) + 1);
            PlayerPrefs.Save();
            return playerOneWon ? "PLAYER 1 WIN RECORDED" : "PLAYER 2 WIN RECORDED";
        }

        public static string GetLocalMatchRecord()
        {
            return $"LOCAL MATCH RECORD  P1 {PlayerPrefs.GetInt(PlayerOneWinsKey)}  •  " +
                   $"P2 {PlayerPrefs.GetInt(PlayerTwoWinsKey)}  •  DRAWS {PlayerPrefs.GetInt(DrawsKey)}";
        }

        private static bool UpdateHighScore(string key, int score)
        {
            var previousBest = PlayerPrefs.GetInt(key, 0);
            if (score <= previousBest) return false;

            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
            return true;
        }
    }
}
