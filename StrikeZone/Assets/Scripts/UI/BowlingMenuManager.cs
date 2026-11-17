using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace StrikeZone.UI
{
    public sealed class BowlingMenuManager : MonoBehaviour
    {
        private void Start()
        {
            Bind("PlayButton", () => SceneManager.LoadScene("BowlingGame"));
            Bind("InstructionsButton", () => SetVisible("InstructionsPanel", true));
            Bind("CloseInstructionsButton", () => SetVisible("InstructionsPanel", false));
            Bind("SettingsButton", () => SetVisible("SettingsPanel", true));
            Bind("CloseSettingsButton", () => SetVisible("SettingsPanel", false));
            Bind("ExitButton", () => Application.Quit());

            var slider = FindSlider("VolumeSlider");
            if (slider != null)
            {
                slider.SetValueWithoutNotify(PlayerPrefs.GetFloat("StrikeZone.MasterVolume", 1f));
                slider.onValueChanged.AddListener(SetMasterVolume);
                SetMasterVolume(slider.value);
            }
        }

        private void Bind(string buttonName, UnityEngine.Events.UnityAction action)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
                if (button.name == buttonName) button.onClick.AddListener(action);
        }

        private void SetVisible(string objectName, bool visible)
        {
            var objects = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var item in objects)
                if (item.name == objectName) item.gameObject.SetActive(visible);
        }

        private Slider FindSlider(string objectName)
        {
            foreach (var slider in FindObjectsByType<Slider>(FindObjectsInactive.Include))
                if (slider.name == objectName) return slider;
            return null;
        }

        private void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("StrikeZone.MasterVolume", AudioListener.volume);
            PlayerPrefs.Save();
            foreach (var label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
                if (label.name == "VolumeValue") label.text = $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
        }
    }
}
