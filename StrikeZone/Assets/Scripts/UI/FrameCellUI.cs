using TMPro;
using StrikeZone.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace StrikeZone.UI
{
    /// <summary>Displays one frame's roll symbols and cumulative score.</summary>
    public sealed class FrameCellUI : MonoBehaviour
    {
        private TMP_Text _frameNumber;
        private TMP_Text _roll1;
        private TMP_Text _roll2;
        private TMP_Text _roll3;
        private TMP_Text _cumulative;
        private Outline _highlight;

        private void Awake()
        {
            _frameNumber = FindText("FrameNumber");
            _roll1 = FindText("Roll1");
            _roll2 = FindText("Roll2");
            _roll3 = FindText("Roll3");
            _cumulative = FindText("Cumulative");
            _highlight = GetComponent<Outline>();
        }

        public void SetFrame(FrameScore frame)
        {
            if (frame == null)
            {
                return;
            }

            if (_frameNumber == null)
            {
                CacheText();
            }

            _frameNumber.text = frame.FrameNumber.ToString();
            _roll1.text = FormatFirstRoll(frame);
            _roll2.text = FormatSecondRoll(frame);
            if (_roll3 != null)
            {
                _roll3.text = frame.FrameNumber == 10 ? FormatTenthBonus(frame) : string.Empty;
            }
            _cumulative.text = frame.Cumulative.HasValue ? frame.Cumulative.Value.ToString() : string.Empty;
        }

        public void SetHighlighted(bool isHighlighted)
        {
            if (_highlight != null)
            {
                _highlight.enabled = isHighlighted;
            }
        }

        private void CacheText()
        {
            _frameNumber = FindText("FrameNumber");
            _roll1 = FindText("Roll1");
            _roll2 = FindText("Roll2");
            _roll3 = FindText("Roll3");
            _cumulative = FindText("Cumulative");
            _highlight = GetComponent<Outline>();
        }

        private TMP_Text FindText(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static string FormatFirstRoll(FrameScore frame)
        {
            if (!frame.Roll1.HasValue)
            {
                return string.Empty;
            }

            if (frame.FrameNumber < 10 && frame.IsStrike)
            {
                return string.Empty;
            }

            if (frame.FrameNumber == 10 && frame.Roll1 == 10)
            {
                return "X";
            }

            return FormatPins(frame.Roll1.Value);
        }

        private static string FormatSecondRoll(FrameScore frame)
        {
            if (!frame.Roll2.HasValue)
            {
                return frame.FrameNumber < 10 && frame.IsStrike ? "X" : string.Empty;
            }

            if (frame.FrameNumber < 10 && frame.IsSpare)
            {
                return "/";
            }

            if (frame.FrameNumber == 10 && frame.Roll1 < 10 && frame.Roll1 + frame.Roll2 == 10)
            {
                return "/";
            }

            return frame.FrameNumber == 10 && frame.Roll1 == 10 && frame.Roll2 == 10
                ? "X"
                : FormatPins(frame.Roll2.Value);
        }

        private static string FormatTenthBonus(FrameScore frame)
        {
            if (!frame.Roll3.HasValue)
            {
                return string.Empty;
            }

            if (frame.Roll3 == 10)
            {
                return "X";
            }

            if (frame.Roll1 == 10 && frame.Roll2 < 10 && frame.Roll2 + frame.Roll3 == 10)
            {
                return "/";
            }

            if (frame.Roll1 < 10 && frame.Roll2.HasValue && frame.Roll1 + frame.Roll2 == 10)
            {
                return FormatPins(frame.Roll3.Value);
            }

            return FormatPins(frame.Roll3.Value);
        }

        private static string FormatPins(int pins)
        {
            return pins == 0 ? "-" : pins.ToString();
        }
    }
}
