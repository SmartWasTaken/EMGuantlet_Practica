using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class CharSelectionMenuButtonsHandler : NetworkBehaviour
{
    [Header("Character Stats Assets")]
    [SerializeField] private PlayerStats greenCharacterStats;
    [SerializeField] private PlayerStats purpleCharacterStats;
    [SerializeField] private PlayerStats redCharacterStats;
    [SerializeField] private PlayerStats yellowCharacterStats;

    /// <summary>
    /// Vuelve al menú principal desde la pantalla de selección de personaje.
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void OnGreenButtonClicked() { selectCharacterAndStartGame(greenCharacterStats, 0); }
    public void OnPurpleButtonClicked() { selectCharacterAndStartGame(purpleCharacterStats, 1); }
    public void OnRedButtonClicked() { selectCharacterAndStartGame(redCharacterStats, 2); }
    public void OnYellowButtonClicked() { selectCharacterAndStartGame(yellowCharacterStats, 3); }

    [ServerRpc(RequireOwnership = false)]
    private void SelectCharacterServerRpc(int index, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StorePlayerSelection(clientId, index);
        }
    }

    /// <summary>
    /// Valida la selección del personaje y delega el inicio de partida.
    /// </summary>
    private void selectCharacterAndStartGame(PlayerStats characterStats, int characterIndex)
    {
        if (characterStats == null) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectedCharacterStats = characterStats;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.StorePlayerSelection(NetworkManager.Singleton.LocalClientId, characterIndex);
            
            Debug.Log("Soy el Host. Iniciando el nivel para todos...");
            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.PlaygroundLevel, LoadSceneMode.Single);
        }
        else
        {
            // El Cliente sí usa el RPC para avisar al servidor
            SelectCharacterServerRpc(characterIndex);
            Debug.Log("Soy Cliente. Personaje elegido. Esperando a que el Host inicie la partida...");
        }
    }
}