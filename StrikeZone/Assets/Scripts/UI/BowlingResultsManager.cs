using StrikeZone.Core;
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
            var text = FindText("FinalScore");
            if (text != null) text.text = snapshot == null ? "NO GAME DATA" : $"FINAL SCORE\n{snapshot.TotalSoFar} / 300";
            var stats = FindText("GameStats");
            if (stats != null && snapshot != null)
            {
                var strikes = 0; var spares = 0; var opens = 0;
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
                stats.text = $"STRIKES   {strikes}     SPARES   {spares}     OPENS   {opens}     BEST FRAME   {bestFrame}";

                foreach (var scoreboard in FindObjectsByType<ScoreboardUI>())
                    scoreboard.Render(snapshot, 0);
            }
            Bind("PlayAgainButton", () => { BowlingSession.Clear(); SceneManager.LoadScene("BowlingGame"); });
            Bind("ResultsMenuButton", () => SceneManager.LoadScene("BowlingMenu"));
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
