using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

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

    [Header("Exit Settings")]
    [SerializeField] private Button exitMenuButton;
    [SerializeField] private GameObject confirmExitPanel;
    [SerializeField] private Button confirmExitButton;
    [SerializeField] private Button cancelExitButton;
    [SerializeField] private TextMeshProUGUI confirmWarningText;

    private bool isMenuOpen = false;

    private void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (confirmExitPanel != null) confirmExitPanel.SetActive(false);

        if (exitMenuButton != null) exitMenuButton.onClick.AddListener(OnExitMenuClicked);
        if (confirmExitButton != null) confirmExitButton.onClick.AddListener(OnConfirmExitClicked);
        if (cancelExitButton != null) cancelExitButton.onClick.AddListener(OnCancelExitClicked);

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

        if (!isMenuOpen && confirmExitPanel != null)
        {
            confirmExitPanel.SetActive(false);
        }
    }

    public void CloseMenu()
    {
        if (isMenuOpen) ToggleMenu();
    }

    private void OnExitMenuClicked()
    {
        if (confirmExitPanel != null) confirmExitPanel.SetActive(true);

        if (confirmWarningText != null)
        {
            bool isHost = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer;

            if (isHost)
            {
                confirmWarningText.text = "¿Estás seguro de que quieres salir?\n\n<color=#FF5555>Eres el HOST. Si sales, todos los jugadores se desconectarán de la partida.</color>";
            }
            else
            {
                confirmWarningText.text = "¿Estás seguro de que quieres salir de la partida?";
            }
        }
    }

    private void OnCancelExitClicked()
    {
        if (confirmExitPanel != null) confirmExitPanel.SetActive(false);
    }

    private void OnConfirmExitClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DisconnectAndReturnToMenu();
        }
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