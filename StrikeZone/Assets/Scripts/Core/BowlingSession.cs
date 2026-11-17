using StrikeZone.Logic;

namespace StrikeZone.Core
{
    /// <summary>Holds the finished game's score while Unity loads the results scene.</summary>
    public static class BowlingSession
    {
        public const int ChallengeStrikeTarget = 3;

        public static ScoreSnapshot FinalSnapshot { get; private set; }
        public static bool IsStrikeChallenge { get; private set; }
        public static bool IsTwoPlayer { get; private set; }
        public static int ChallengeStrikes { get; private set; }
        public static ScoreSnapshot SecondPlayerFinalSnapshot { get; private set; }
        public static float BestShotQuality { get; private set; }
        public static string BestShotRank { get; private set; } = "—";
        public static bool ChallengeComplete => IsStrikeChallenge && ChallengeStrikes >= ChallengeStrikeTarget;

        public static void BeginGame(bool strikeChallenge)
        {
            IsStrikeChallenge = strikeChallenge;
            IsTwoPlayer = false;
            RestartCurrentGame();
        }

        public static void BeginTwoPlayerGame()
        {
            IsStrikeChallenge = false;
            IsTwoPlayer = true;
            RestartCurrentGame();
        }

        public static void RestartCurrentGame()
        {
            FinalSnapshot = null;
            SecondPlayerFinalSnapshot = null;
            ChallengeStrikes = 0;
            BestShotQuality = 0f;
            BestShotRank = "—";
        }

        public static void RegisterStrike()
        {
            if (IsStrikeChallenge)
            {
                ChallengeStrikes++;
            }
        }

        public static void RecordShot(float quality, string rank)
        {
            if (BestShotRank == "—" || quality > BestShotQuality)
            {
                BestShotQuality = quality;
                BestShotRank = rank;
            }
        }

        public static void Capture(ScoreSnapshot snapshot, ScoreSnapshot secondPlayerSnapshot = null)
        {
            FinalSnapshot = snapshot;
            SecondPlayerFinalSnapshot = secondPlayerSnapshot;
        }

        public static void Clear()
        {
            FinalSnapshot = null;
            IsStrikeChallenge = false;
            IsTwoPlayer = false;
            ChallengeStrikes = 0;
            SecondPlayerFinalSnapshot = null;
            BestShotQuality = 0f;
            BestShotRank = "—";
        }
    }
}
