using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionUI : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    private void Start()
    {
        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 1f;        
        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;

        bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1.0f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1.0f);

        bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        closeButton.onClick.AddListener(ClosePopup);
    }
    private void OnBgmSliderChanged(float value)
    {
        if (AudioManager.instance != null)  //오디오 매니저의 singleton 인스턴스를 통해서 볼륨 조절
        {
            AudioManager.instance.SetBGMVolume(value);
        }
        PlayerPrefs.SetFloat("BGMVolume",value);
    }
    private void OnSfxSliderChanged(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
        }
        PlayerPrefs.SetFloat("SFXVolume", value);
    }
    private void ClosePopup()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        AudioManager.instance.PlaySFX("shotgun_fire");
    }
}
