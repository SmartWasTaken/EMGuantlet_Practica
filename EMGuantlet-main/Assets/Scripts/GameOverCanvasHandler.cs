using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;

public class GameOverCanvasHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI jewelsValueText;
    [SerializeField] private TextMeshProUGUI keysValueText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;

    [SerializeField] private Button backButton;

    [SerializeField] private Button spectateButton;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject spectatorUIPanel;
    [SerializeField] private TextMeshProUGUI spectatorNameText;

    private CameraController cameraController;

    /// <summary>
    /// Inicializa la pantalla mostrando las estadísticas finales de la partida.
    /// </summary>
    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.LocalPlayerController != null)
        {
            if (GameManager.Instance.LocalPlayerController.CurrentHealth > 0)
            {
                foreach (GameObject rootObj in gameObject.scene.GetRootGameObjects())
                {
                    rootObj.SetActive(false);
                }
                return;
            }
        }

        foreach (GameObject rootObj in gameObject.scene.GetRootGameObjects())
        {
            Camera rogueCam = rootObj.GetComponentInChildren<Camera>(true);
            if (rogueCam != null) rogueCam.gameObject.SetActive(false);

            AudioListener rogueAudio = rootObj.GetComponentInChildren<AudioListener>(true);
            if (rogueAudio != null) Destroy(rogueAudio);
        }

        cameraController = FindFirstObjectByType<CameraController>();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (spectatorUIPanel != null) spectatorUIPanel.SetActive(false);
        checkSpectateAvailability();

        displayGameStats();
        checkHostStatus();
    }

    private void Update()
    {
        if (spectatorUIPanel != null && spectatorUIPanel.activeSelf && cameraController != null)
        {
            if (cameraController.SpectatedPlayer != null)
            {
                ulong id = cameraController.SpectatedPlayer.OwnerClientId;
                string name = GameManager.Instance.GetPlayerName(id);
                if (spectatorNameText != null) spectatorNameText.text = $"OBSERVANDO A:\n{name.ToUpper()}";
            }
            else
            {
                if (spectatorNameText != null) spectatorNameText.text = "TODOS MUERTOS";
            }
        }

        checkHostStatus();
    }

    private void checkSpectateAvailability()
    {
        if (spectateButton == null) return;

        bool someoneAlive = false;
        foreach (var p in PlayerController.ActivePlayers)
        {
            if (p.CurrentHealth > 0) someoneAlive = true;
        }

        spectateButton.interactable = someoneAlive;
    }

    public void OnSpectateButtonClicked()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (spectatorUIPanel != null) spectatorUIPanel.SetActive(true);

        var mainHUD = FindFirstObjectByType<HeadUpDisplayController>();
        if (mainHUD != null)
        {
            mainHUD.EnableSpectatorMode();
        }

        if (cameraController != null)
        {
            cameraController.cycleSpectatorTarget(1);
        }
    }

    public void OnSpectateNextClicked()
    {
        if (cameraController != null)
        {
            cameraController.cycleSpectatorTarget(1);
        }
    }

    public void OnSpectatePreviousClicked()
    {
        if (cameraController != null)
        {
            cameraController.cycleSpectatorTarget(-1);
        }
    }

    public void OnStopSpectatingClicked()
    {
        if (spectatorUIPanel != null) spectatorUIPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        var mainHUD = FindFirstObjectByType<HeadUpDisplayController>();
        if (mainHUD != null)
        {
            mainHUD.DisableSpectatorMode();
        }
    }

    private void checkHostStatus()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsIds.Count > 1)
            {
                bool someoneAlive = false;
                foreach (var p in PlayerController.ActivePlayers)
                {
                    if (p != null && p.CurrentHealth > 0) someoneAlive = true;
                }

                if (backButton != null)
                {
                    // Si alguien sigue vivo, bloqueamos el botón. Si todos murieron, lo liberamos.
                    backButton.interactable = !someoneAlive;

                    TextMeshProUGUI btnText = backButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = someoneAlive ? "ESPERANDO A LOS DEMÁS..." : "VOLVER AL MENÚ";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Carga el menú principal al pulsar el botón de volver.
    /// </summary>
    public void OnBackButtonClicked()
    {
        bool someoneAlive = false;
        foreach (var p in PlayerController.ActivePlayers)
        {
            if (p != null && p.CurrentHealth > 0) someoneAlive = true;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsIds.Count > 1 && someoneAlive)
        {
            Debug.LogWarning("El Host no puede abandonar la partida mientras haya clientes vivos.");
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>
    /// Actualiza los textos del panel con diamantes, llaves y enemigos eliminados.
    /// </summary>
    private void displayGameStats()
    {
        if (GameManager.Instance == null) return;

        if (jewelsValueText != null)
            jewelsValueText.text = GameManager.Instance.GetDiamonds().ToString();

        if (keysValueText != null)
            keysValueText.text = GameManager.Instance.GetKeys().ToString();

        if (enemiesKilledText != null)
            enemiesKilledText.text = GameManager.Instance.EnemiesKilled.ToString();
    }
}