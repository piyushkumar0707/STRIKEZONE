using StrikeZone.Logic;
using UnityEngine;

namespace StrikeZone.UI
{
    /// <summary>Renders ten frame cells and highlights the frame currently being played.</summary>
    public sealed class ScoreboardUI : MonoBehaviour
    {
        private FrameCellUI[] _frameCells;

        private void Awake()
        {
            CacheCells();
        }

        public void Render(ScoreSnapshot snapshot, int currentFrame)
        {
            if (snapshot == null)
            {
                return;
            }

            if (_frameCells == null || _frameCells.Length != 10)
            {
                CacheCells();
            }

            var frames = snapshot.Frames;
            for (var i = 0; i < _frameCells.Length; i++)
            {
                if (_frameCells[i] == null)
                {
                    continue;
                }

                _frameCells[i].SetFrame(frames[i]);
                _frameCells[i].SetHighlighted(!snapshot.IsGameComplete && i + 1 == currentFrame);
            }
        }

        private void CacheCells()
        {
            _frameCells = GetComponentsInChildren<FrameCellUI>(true);
            System.Array.Sort(_frameCells, (left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
        }
    }
}
