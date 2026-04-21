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

    [Header("Nombres sobre los botones (¡NUEVO!)")]
    [SerializeField] private TextMeshProUGUI[] colorNameLabels;

    [Header("Botones de Color")]
    [SerializeField] private Button[] colorButtons;
    private string[] hexColors = { "#FFFF00", "#FF0000", "#A020F0", "#00FF00" };

    [Header("Panel Host")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private TMP_Dropdown mapsDropdown;

    [SerializeField] private ScrollRect logScrollRect;

    private NetworkVariable<long> ownerYellow = new NetworkVariable<long>(-1);
    private NetworkVariable<long> ownerRed = new NetworkVariable<long>(-1);
    private NetworkVariable<long> ownerPurple = new NetworkVariable<long>(-1);
    private NetworkVariable<long> ownerGreen = new NetworkVariable<long>(-1);

    private NetworkVariable<long>[] colorOwners;
    private List<string> rawMessages = new List<string>();

    private void Awake()
    {
        Instance = this;
        colorOwners = new NetworkVariable<long>[] { ownerYellow, ownerRed, ownerPurple, ownerGreen };
    }

    public override void OnNetworkSpawn()
    {
        hostPanel.SetActive(IsHost);
        if (deselectText != null) deselectText.SetActive(false);

        GameManager.Instance.RegisterPlayerServerRpc(MainMenuButtonsHandler.LocalPlayerName);

        if (IsHost)
            AddLogMessageClientRpc("Servidor creado por " + MainMenuButtonsHandler.LocalPlayerName, -1);

        if (IsHost && mapsDropdown != null && GameManager.Instance.availableMaps != null)
        {
            mapsDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var map in GameManager.Instance.availableMaps)
            {
                options.Add(new TMP_Dropdown.OptionData(map.mapName));
            }
            mapsDropdown.AddOptions(options);
        }

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

    public void RefreshLobbyUI()
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
            long currentOwnerId = colorOwners[i].Value;

            if (colorNameLabels != null && i < colorNameLabels.Length && colorNameLabels[i] != null)
            {
                if (currentOwnerId != -1)
                {
                    colorNameLabels[i].text = GameManager.Instance.GetPlayerName((ulong)currentOwnerId);
                }
                else
                {
                    colorNameLabels[i].text = "";
                }
            }

            if (currentOwnerId == (long)myId)
            {
                mySelection = i;
            }
        }

        if (deselectText != null) deselectText.SetActive(mySelection != -1);

        if (colorButtons == null) return;

        for (int i = 0; i < colorButtons.Length; i++)
        {
            if (i >= colorOwners.Length) break;
            if (colorButtons[i] == null) continue;

            bool isTakenByAnyone = colorOwners[i].Value != -1;
            bool isTakenByMe = colorOwners[i].Value == (long)myId;

            colorButtons[i].interactable = !isTakenByAnyone || isTakenByMe;

            ColorBlock cb = colorButtons[i].colors;
            if (isTakenByMe)
            {
                cb.normalColor = Color.gray;
                cb.highlightedColor = Color.gray;
            }
            else
            {
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            }
            colorButtons[i].colors = cb;
        }
    }

    private void UpdateLogDisplay()
    {
        logText.text = "";
        foreach (string msg in rawMessages)
        {
            logText.text += msg + "\n";
        }

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();

        if (logScrollRect != null)
        {
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void OnColorButtonClicked(int index)
    {
        SelectCharacterServerRpc(index);
    }

    public void OnDeselectClicked()
    {
        DeselectServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void SelectCharacterServerRpc(int index, RpcParams rpcParams = default)
    {
        if (index < 0 || index >= colorOwners.Length) return;

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

    [Rpc(SendTo.Server)]
    public void DeselectServerRpc(RpcParams rpcParams = default)
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

    private string GetColorName(int i) => i == 0 ? "Amarillo" : i == 1 ? "Rojo" : i == 2 ? "Morado" : "Verde";

    public void OnStartGameClicked()
    {
        if (!IsHost) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (GameManager.Instance.GetPlayerSelection(clientId) == -1)
            {
                AddLogMessageClientRpc("<color=#FF0000>¡No se puede empezar! Todos deben elegir color.</color>", -1);
                return;
            }
        }

        if (mapsDropdown != null && GameManager.Instance.availableMaps != null)
        {
            GameManager.Instance.networkedMapIndex.Value = mapsDropdown.value;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.PlaygroundLevel, LoadSceneMode.Single);
    }
}