using UnityEngine;

using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;

//https://www.youtube.com/watch?v=YOaYQrN1oYQ

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider volumeSlider;

    //public Dropdown resolutionDropDown;
    public TMPro.TMP_Dropdown resolutionDropDown;

    Resolution[] resolutions;
    private const string VolumeKey = "MasterVolume";


    private void Start()
    {
        //audioMixer.SetFloat("Volume", PlayerPrefs.GetFloat("MasterVolume", 0f));

        resolutions = Screen.resolutions;

        resolutionDropDown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + ", " + resolutions[i].refreshRateRatio + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }



        resolutionDropDown.AddOptions(options);
        resolutionDropDown.value = currentResolutionIndex;
        resolutionDropDown.RefreshShownValue();
        Debug.Log(currentResolutionIndex);

        float saved = PlayerPrefs.GetFloat(VolumeKey, 0f);
        Debug.Log($"[Settings] Loaded volume: {saved} | slider range: {volumeSlider.minValue} to {volumeSlider.maxValue}");

        volumeSlider.SetValueWithoutNotify(saved);      
        
        audioMixer.SetFloat("Volume", saved);
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("Volume", volume);
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
        Debug.Log($"[Settings] SAVED {volume} | readback {PlayerPrefs.GetFloat(VolumeKey, -999f)}");
    }


    public void SetReslution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

   

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }
}
