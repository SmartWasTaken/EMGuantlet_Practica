using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // Necesario para inyectar la IP

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuButtonsHandler : MonoBehaviour
{
    [Header("Paneles UI")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject clientIpPanel;

    [Header("Campos de Texto")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField ipAddressInput;

    [Header("Botones")]
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonConnectClient;
    [SerializeField] private Button buttonCancelClient;
    [SerializeField] private Button buttonOptions;
    [SerializeField] private Button buttonExit;

    // Variable estática para llevar el nombre a la siguiente escena
    public static string LocalPlayerName { get; private set; } = "";

    private void Awake()
    {
        // Asignación limpia de listeners mediante código
        if (buttonHost != null) buttonHost.onClick.AddListener(OnHostButtonClicked);
        if (buttonClient != null) buttonClient.onClick.AddListener(OnClientButtonClicked);
        if (buttonConnectClient != null) buttonConnectClient.onClick.AddListener(OnConnectClientClicked);
        if (buttonCancelClient != null) buttonCancelClient.onClick.AddListener(OnCancelClientClicked);

        // Mantengo tus botones originales de opciones y salir
        if (buttonOptions != null) buttonOptions.onClick.AddListener(OnOptionsButtonClicked);
        if (buttonExit != null) buttonExit.onClick.AddListener(OnExitButtonClicked);
    }

    private void Start()
    {
        // Estado inicial de los paneles
        if (mainPanel != null) mainPanel.SetActive(true);
        if (clientIpPanel != null) clientIpPanel.SetActive(false);

        if (ipAddressInput != null) ipAddressInput.text = "127.0.0.1";
    }

    private void OnHostButtonClicked()
    {
        SavePlayerName();
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

        // Inyectamos la IP al componente Unity Transport
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(ipAddress, 7777);
        }

        NetworkManager.Singleton.StartClient();
    }

    private void OnCancelClientClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (clientIpPanel != null) clientIpPanel.SetActive(false);
    }

    private void SavePlayerName()
    {
        if (playerNameInput != null)
        {
            LocalPlayerName = playerNameInput.text.Trim();
        }
    }

    public void OnOptionsButtonClicked()
    {
        Debug.Log("Options button pressed");
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
}