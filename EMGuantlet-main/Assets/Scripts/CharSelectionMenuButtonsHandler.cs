using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CharSelectionMenuButtonsHandler : NetworkBehaviour
{
    public static CharSelectionMenuButtonsHandler Instance { get; private set; }

    [Header("UI Log")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private GameObject deselectText;

    [Header("Botones de Color")]
    [SerializeField] private Button[] colorButtons; // 0:Verde, 1:Morado, 2:Rojo, 3:Amarillo
    private string[] hexColors = { "#00FF00", "#A020F0", "#FF0000", "#FFFF00" };

    [Header("Panel Host")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private TMP_Dropdown mapsDropdown;

    private NetworkVariable<long> ownerGreen = new NetworkVariable<long>(-1);
    private NetworkVariable<long> ownerPurple = new NetworkVariable<long>(-1);
    private NetworkVariable<long> ownerRed = new NetworkVariable<long>(-1);
    private NetworkVariable<long> ownerYellow = new NetworkVariable<long>(-1);

    private NetworkVariable<long>[] colorOwners;
    private List<string> rawMessages = new List<string>();

    private void Awake()
    {
        Instance = this;
        colorOwners = new NetworkVariable<long>[] { ownerGreen, ownerPurple, ownerRed, ownerYellow };
    }

    public override void OnNetworkSpawn()
    {
        hostPanel.SetActive(IsHost);
        deselectText.SetActive(false);

        GameManager.Instance.RegisterPlayerServerRpc(MainMenuButtonsHandler.LocalPlayerName);

        if (IsHost)
            AddLogMessageClientRpc("Servidor creado por " + MainMenuButtonsHandler.LocalPlayerName, -1);

        foreach (var ownerVar in colorOwners)
        {
            ownerVar.OnValueChanged += OnColorOwnerChanged;
        }

        RefreshLobbyUiLocal();
    }

    public override void OnNetworkDespawn()
    {
        foreach (var ownerVar in colorOwners)
        {
            ownerVar.OnValueChanged -= OnColorOwnerChanged;
        }
    }

    public void OnBackButtonClicked()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    private void OnColorOwnerChanged(long prev, long current)
    {
        RefreshLobbyUiLocal();
    }

    [Rpc(SendTo.Everyone)]
    public void AddLogMessageClientRpc(string message, int colorIndex)
    {
        rawMessages.Add(message);
        UpdateLogDisplay();
    }

    public void RefreshLobbyUi()
    {
        RefreshLobbyUiLocal();
    }

    private void RefreshLobbyUiLocal()
    {
        UpdateLogDisplay();

        ulong myId = NetworkManager.Singleton.LocalClientId;
        int mySelection = -1;

        for (int i = 0; i < colorOwners.Length; i++)
        {
            if (colorOwners[i].Value == (long)myId)
            {
                mySelection = i;
                break;
            }
        }

        if (deselectText != null)
        {
            deselectText.SetActive(mySelection != -1);
        }

        for (int i = 0; i < colorButtons.Length; i++)
        {
            bool isTakenByAnyone = colorOwners[i].Value != -1;
            bool isTakenByMe = colorOwners[i].Value == (long)myId;
            colorButtons[i].interactable = !isTakenByAnyone || isTakenByMe;
        }
    }

    private void UpdateLogDisplay()
    {
        logText.text = "";
        foreach (string msg in rawMessages)
        {
            logText.text += msg + "\n";
        }
    }

    public void OnColorButtonClicked(int index)
    {
        SelectCharacterServerRpc(index);
    }

    [Rpc(SendTo.Server)]
    private void SelectCharacterServerRpc(int index, RpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;

        if (colorOwners[index].Value != -1 && colorOwners[index].Value != (long)id) return;

        int oldSelection = -1;
        for (int i = 0; i < colorOwners.Length; i++)
        {
            if (colorOwners[i].Value == (long)id)
            {
                oldSelection = i;
                colorOwners[i].Value = -1;
            }
        }

        colorOwners[index].Value = (long)id;
        GameManager.Instance.StorePlayerSelection(id, index);

        string name = GameManager.Instance.GetPlayerName(id);
        string colorName = GetColorName(index);

        if (oldSelection != index)
        {
            AddLogMessageClientRpc($"<color={hexColors[index]}>{name}</color> ha elegido el color {colorName}", index);
        }
    }

    public void OnDeselectClicked()
    {
        DeselectServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void DeselectServerRpc(RpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;

        for (int i = 0; i < colorOwners.Length; i++)
        {
            if (colorOwners[i].Value == (long)id)
            {
                colorOwners[i].Value = -1;
                GameManager.Instance.RemovePlayerSelection(id);

                string name = GameManager.Instance.GetPlayerName(id);
                AddLogMessageClientRpc($"{name} ha soltado su color.", -1);
                break;
            }
        }
    }

    private string GetColorName(int i) => i == 0 ? "Verde" : i == 1 ? "Morado" : i == 2 ? "Rojo" : "Amarillo";

    public void OnStartGameClicked()
    {
        if (IsHost)
            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.PlaygroundLevel, LoadSceneMode.Single);
    }
}