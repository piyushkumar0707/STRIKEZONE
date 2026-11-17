using StrikeZone.Core;
using StrikeZone.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StrikeZone.UI
{
    public sealed class BowlingResultsManager : MonoBehaviour
    {
        private void Start()
        {
            var snapshot = BowlingSession.FinalSnapshot;
            var secondSnapshot = BowlingSession.SecondPlayerFinalSnapshot;
            var isNewRecord = false;
            var localMatchRecord = string.Empty;
            if (snapshot != null)
            {
                if (BowlingSession.IsTwoPlayer && secondSnapshot != null)
                    localMatchRecord = BowlingRecords.RecordLocalMatch(snapshot.TotalSoFar, secondSnapshot.TotalSoFar);
                else if (BowlingSession.IsStrikeChallenge)
                    isNewRecord = BowlingRecords.RecordChallenge(snapshot.TotalSoFar);
                else
                    isNewRecord = BowlingRecords.RecordClassic(snapshot.TotalSoFar);
            }

            var title = FindText("ResultsTitle");
            if (title != null)
            {
                if (BowlingSession.IsTwoPlayer) title.text = "2 PLAYER MATCH RESULTS";
                else if (BowlingSession.IsStrikeChallenge) title.text = "STRIKE CHALLENGE RESULTS";
            }
            var text = FindText("FinalScore");
            if (text != null)
            {
                if (BowlingSession.IsTwoPlayer && snapshot != null && secondSnapshot != null)
                {
                    var outcome = snapshot.TotalSoFar == secondSnapshot.TotalSoFar ? "DRAW" :
                        snapshot.TotalSoFar > secondSnapshot.TotalSoFar ? "PLAYER 1 WINS" : "PLAYER 2 WINS";
                    text.text = $"PLAYER 1  {snapshot.TotalSoFar}   —   PLAYER 2  {secondSnapshot.TotalSoFar}\n{outcome}";
                    text.fontSize = 34f;
                    text.GetComponent<RectTransform>().sizeDelta = new Vector2(1300f, 150f);
                }
                else
                {
                    text.text = snapshot == null ? "NO GAME DATA" : $"FINAL SCORE\n{snapshot.TotalSoFar} / 300";
                }
            }

            var stats = FindText("GameStats");
            if (stats != null && snapshot != null)
            {
                if (BowlingSession.IsTwoPlayer && secondSnapshot != null)
                {
                    stats.text = $"P1  {FormatPlayerStats(snapshot)}\nP2  {FormatPlayerStats(secondSnapshot)}\n{localMatchRecord}  |  {BowlingRecords.GetLocalMatchRecord()}";
                    stats.color = new Color(.92f, .96f, 1f);
                }
                else
                {
                    var baseStats = FormatPlayerStats(snapshot);
                    var bestShot = $"BEST SHOT RANK {BowlingSession.BestShotRank}";
                    if (BowlingSession.IsStrikeChallenge)
                    {
                        var outcome = BowlingSession.ChallengeComplete ? "COMPLETE" : "NOT MET";
                        var strikesTowardGoal = Mathf.Min(BowlingSession.ChallengeStrikes, BowlingSession.ChallengeStrikeTarget);
                        var recordLabel = isNewRecord ? "NEW CHALLENGE BEST!" : $"CHALLENGE BEST {PlayerPrefs.GetInt("StrikeZone.Record.ChallengeBest")}";
                        stats.text = $"CHALLENGE {outcome}  •  {strikesTowardGoal}/{BowlingSession.ChallengeStrikeTarget} STRIKES\n{baseStats}   |   {bestShot}\n{recordLabel}";
                        stats.color = BowlingSession.ChallengeComplete ? new Color(.35f, 1f, .65f) : new Color(1f, .65f, .35f);
                    }
                    else
                    {
                        var recordLabel = isNewRecord ? "NEW PERSONAL BEST!" : $"PERSONAL BEST {PlayerPrefs.GetInt("StrikeZone.Record.ClassicBest")}";
                        stats.text = $"{baseStats}\n{bestShot}  |  {recordLabel}";
                    }
                }

                var statsRect = stats.GetComponent<RectTransform>();
                statsRect.anchoredPosition = new Vector2(0f, BowlingSession.IsTwoPlayer ? 54f : 40f);
                statsRect.sizeDelta = new Vector2(1500f, BowlingSession.IsTwoPlayer ? 112f : 96f);
                stats.fontSize = BowlingSession.IsTwoPlayer ? 18f : 20f;

                var scoreboards = FindObjectsByType<ScoreboardUI>(FindObjectsInactive.Include);
                var firstBoard = FindBoard(scoreboards, "ResultsScoreboard");
                if (firstBoard != null)
                {
                    firstBoard.Render(snapshot, 0);
                    firstBoard.GetComponent<RectTransform>().anchoredPosition = BowlingSession.IsTwoPlayer
                        ? new Vector2(0f, -100f)
                        : new Vector2(0f, -120f);
                }

                if (BowlingSession.IsTwoPlayer)
                {
                    var secondBoard = FindBoard(scoreboards, "ResultsScoreboardPlayer2");
                    if (secondBoard == null && firstBoard != null)
                    {
                        var secondBoardObject = Instantiate(firstBoard.gameObject, firstBoard.transform.parent, false);
                        secondBoardObject.name = "ResultsScoreboardPlayer2";
                        secondBoard = secondBoardObject.GetComponent<ScoreboardUI>();
                    }

                    secondBoard?.Render(secondSnapshot, 0);
                    if (secondBoard != null)
                        secondBoard.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -255f);

                    SetButtonPosition("PlayAgainButton", new Vector2(0f, -380f));
                    SetButtonPosition("ResultsMenuButton", new Vector2(0f, -470f));
                }
            }
            Bind("PlayAgainButton", () => { BowlingSession.RestartCurrentGame(); SceneManager.LoadScene("BowlingGame"); });
            Bind("ResultsMenuButton", () => SceneManager.LoadScene("BowlingMenu"));
        }

        private static string FormatPlayerStats(ScoreSnapshot snapshot)
        {
            var strikes = 0;
            var spares = 0;
            var opens = 0;
            var bestFrame = 0;
            var previousCumulative = 0;
            foreach (var frame in snapshot.Frames)
            {
                if (frame.IsStrike) strikes++;
                else if (frame.IsSpare) spares++;
                else if (frame.Roll1.HasValue && frame.Roll2.HasValue) opens++;

                if (frame.Cumulative.HasValue)
                {
                    bestFrame = Mathf.Max(bestFrame, frame.Cumulative.Value - previousCumulative);
                    previousCumulative = frame.Cumulative.Value;
                }
            }

            return $"{snapshot.TotalSoFar} PTS   STRIKES {strikes}   SPARES {spares}   OPENS {opens}   BEST FRAME {bestFrame}";
        }

        private static ScoreboardUI FindBoard(ScoreboardUI[] scoreboards, string objectName)
        {
            foreach (var scoreboard in scoreboards)
                if (scoreboard.name == objectName) return scoreboard;
            return null;
        }

        private void SetButtonPosition(string objectName, Vector2 position)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button.name == objectName)
                    button.GetComponent<RectTransform>().anchoredPosition = position;
            }
        }

        private TMP_Text FindText(string objectName)
        {
            foreach (var text in FindObjectsByType<TMP_Text>())
                if (text.name == objectName) return text;
            return null;
        }

        private void Bind(string objectName, UnityEngine.Events.UnityAction action)
        {
            foreach (var button in FindObjectsByType<Button>())
                if (button.name == objectName) button.onClick.AddListener(action);
        }
    }
}
