using UnityEngine;
using UnityEngine.UI;

namespace CarControllerwithShooting
{
    public class SettingsScript : MonoBehaviour
    {
        public Slider Slider_Mouse;
        public Dropdown DropDown_Quality;
        public Toggle Toggle_SoundFX;

        void Start()
        {
            AddQualityOptions();
            GetMouseSlider();
            GetMusicToggle();
        }

        public void GetMouseSlider()
        {
            Slider_Mouse.value = PlayerPrefs.GetFloat("MouseSensivity", 1);
            Slider_Mouse.onValueChanged.AddListener(delegate { ChangeMouse(Slider_Mouse); });
        }

        public void GetMusicToggle()
        {
            Toggle_SoundFX.isOn = (PlayerPrefs.GetInt("Music", 1) == 1 ? true : false);
            Toggle_SoundFX.onValueChanged.AddListener(delegate { ChangeMusic(Toggle_SoundFX); });
        }

        public void AddQualityOptions()
        {
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                DropDown_Quality.options.Add(new Dropdown.OptionData() { text = names[i] });
            }

            if (PlayerPrefs.GetInt("QualitySetting", -1) != -1)
            {
                QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("QualitySetting"), true);
                DropDown_Quality.value = PlayerPrefs.GetInt("QualitySetting", 0);
                DropDown_Quality.RefreshShownValue();
            }
            else
            {
                DropDown_Quality.value = QualitySettings.GetQualityLevel();
                DropDown_Quality.RefreshShownValue();
            }

            DropDown_Quality.onValueChanged.AddListener(delegate { SelectQualityLevel(DropDown_Quality); });
        }

        void SelectQualityLevel(Dropdown dropdown)
        {
            PlayerPrefs.SetInt("QualitySetting", dropdown.value);
            QualitySettings.SetQualityLevel(dropdown.value, true);
        }

        public void ChangeMusic(Toggle toggle)
        {
            PlayerPrefs.SetInt("Music", (toggle.isOn == true ? 1 : 0));
            if (toggle.isOn)
            {
                AudioListener.volume = 1;
            }
            else
            {
                AudioListener.volume = 0;
            }
        }

        public void ChangeMouse(Slider slider)
        {
            PlayerPrefs.SetFloat("MouseSensivity", slider.value);
        }
    }
}