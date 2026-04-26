using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuButtonsHandler : MonoBehaviour
{
    [Header("Paneles UI Principales")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject clientIpPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Panel de Info (Rechazos al unirse)")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button buttonCloseInfo;

    [Header("Panel de Desconexión (Host se fue)")]
    [SerializeField] private GameObject disconnectPanel;
    [SerializeField] private TextMeshProUGUI disconnectText;
    [SerializeField] private Button buttonCloseDisconnect;

    [Header("Campos de Texto")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField ipAddressInput;

    [Header("Botones Menú Principal")]
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonOptions;
    [SerializeField] private Button buttonCredits;
    [SerializeField] private Button buttonExit;

    [Header("Botones Secundarios (Volver/Aceptar)")]
    [SerializeField] private Button buttonConnectClient;
    [SerializeField] private Button buttonCancelClient;
    [SerializeField] private Button buttonBackFromOptions;
    [SerializeField] private Button buttonBackFromCredits;

    [Header("Ajustes de Opciones")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle fpsToggle;

    public static string LocalPlayerName { get; private set; } = "";

    private void Awake()
    {
        if (buttonHost != null) buttonHost.onClick.AddListener(OnHostButtonClicked);
        if (buttonClient != null) buttonClient.onClick.AddListener(OnClientButtonClicked);
        if (buttonOptions != null) buttonOptions.onClick.AddListener(OnOptionsButtonClicked);
        if (buttonCredits != null) buttonCredits.onClick.AddListener(OnCreditsButtonClicked);
        if (buttonExit != null) buttonExit.onClick.AddListener(OnExitButtonClicked);

        if (buttonConnectClient != null) buttonConnectClient.onClick.AddListener(OnConnectClientClicked);
        if (buttonCancelClient != null) buttonCancelClient.onClick.AddListener(OnCancelClientClicked);
        if (buttonBackFromOptions != null) buttonBackFromOptions.onClick.AddListener(OnBackFromOptionsClicked);
        if (buttonBackFromCredits != null) buttonBackFromCredits.onClick.AddListener(OnBackFromCreditsClicked);

        if (buttonCloseInfo != null) buttonCloseInfo.onClick.AddListener(() => { if (infoPanel != null) infoPanel.SetActive(false); });

        if (buttonCloseDisconnect != null) buttonCloseDisconnect.onClick.AddListener(() => { if (disconnectPanel != null) disconnectPanel.SetActive(false); });
    }

    private void Start()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (clientIpPanel != null) clientIpPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (disconnectPanel != null) disconnectPanel.SetActive(false);

        if (ipAddressInput != null) ipAddressInput.text = "127.0.0.1";

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
        }

        if (!string.IsNullOrEmpty(GameManager.LastDisconnectReason))
        {
            if (disconnectPanel != null)
            {
                disconnectPanel.SetActive(true);
                if (disconnectText != null) disconnectText.text = GameManager.LastDisconnectReason;
            }
            else if (infoPanel != null)
            {
                infoPanel.SetActive(true);
                if (infoText != null) infoText.text = GameManager.LastDisconnectReason;
            }

            GameManager.LastDisconnectReason = "";
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientRejection;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientRejection;
        }
    }

    private void HandleClientRejection(ulong clientId)
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            string reason = NetworkManager.Singleton.DisconnectReason;

            if (string.IsNullOrEmpty(reason))
            {
                reason = "No se ha podido conectar al Host. Partida no encontrada o en curso.";
            }

            if (infoPanel != null) infoPanel.SetActive(true);
            if (infoText != null) infoText.text = reason;

            if (clientIpPanel != null) clientIpPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);

            NetworkManager.Singleton.Shutdown();
        }
    }

    private void OnHostButtonClicked()
    {
        SavePlayerName();

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
        }

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.CharSelection, LoadSceneMode.Single);
    }

    private void OnClientButtonClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (clientIpPanel != null) clientIpPanel.SetActive(true);
    }

    private void OnConnectClientClicked()
    {
        SavePlayerName();

        string ipAddress = ipAddressInput != null ? ipAddressInput.text : "127.0.0.1";
        if (string.IsNullOrEmpty(ipAddress)) ipAddress = "127.0.0.1";

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(ipAddress, 7777);
        }

        NetworkManager.Singleton.StartClient();
    }

    private void SavePlayerName()
    {
        if (playerNameInput != null)
        {
            LocalPlayerName = playerNameInput.text.Trim();
        }
    }

    private void OnCancelClientClicked()
    {
        if (clientIpPanel != null) clientIpPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void OnOptionsButtonClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void OnBackFromOptionsClicked()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void OnCreditsButtonClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void OnBackFromCreditsClicked()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void OnExitButtonClicked()
    {
        Debug.Log("Exit button pressed");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
        PlayerPrefs.SetInt("ShowFPSPref", isOn ? 1 : 0);
    }
}