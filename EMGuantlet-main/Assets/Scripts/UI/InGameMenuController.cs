using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class InGameMenuController : MonoBehaviour
{
    [Header("Paneles UI")]
    [SerializeField] private GameObject menuPanel;

    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("FPS Settings")]
    [SerializeField] private Toggle fpsToggle;
    [SerializeField] private FPSCounter fpsCounter;

    private bool isMenuOpen = false;

    private void Start()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
            float savedMusicVol = PlayerPrefs.GetFloat("MusicVolumePref", 0.75f);
            musicSlider.value = savedMusicVol;
            SetMusicVolume(savedMusicVol);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            float savedSFXVol = PlayerPrefs.GetFloat("SFXVolumePref", 0.75f);
            sfxSlider.value = savedSFXVol;
            SetSFXVolume(savedSFXVol);
        }

        if (fpsToggle != null)
        {
            fpsToggle.onValueChanged.AddListener(ToggleFPS);
            bool savedFPSState = PlayerPrefs.GetInt("ShowFPSPref", 1) == 1;
            fpsToggle.isOn = savedFPSState;
            ToggleFPS(savedFPSState);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (menuPanel != null)
        {
            menuPanel.SetActive(isMenuOpen);
        }
    }

    public void CloseMenu()
    {
        if (isMenuOpen) ToggleMenu();
    }

    private void SetMusicVolume(float value)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat("MusicVolume", db);
            PlayerPrefs.SetFloat("MusicVolumePref", value);
        }
    }

    private void SetSFXVolume(float value)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat("SFXVolume", db);
            PlayerPrefs.SetFloat("SFXVolumePref", value);
        }
    }

    private void ToggleFPS(bool isOn)
    {
        if (fpsCounter != null)
        {
            fpsCounter.SetShowFPS(isOn);
        }
        PlayerPrefs.SetInt("ShowFPSPref", isOn ? 1 : 0);
    }
}