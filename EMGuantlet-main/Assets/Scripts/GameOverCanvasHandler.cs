using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameOverCanvasHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI jewelsValueText;
    [SerializeField] private TextMeshProUGUI keysValueText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;

    [SerializeField] private UnityEngine.UI.Button backButton;

    /// <summary>
    /// Inicializa la pantalla mostrando las estadísticas finales de la partida.
    /// </summary>
    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.LocalPlayerController != null)
        {
            if (GameManager.Instance.LocalPlayerController.CurrentHealth > 0)
            {
                Canvas rootCanvas = GetComponentInParent<Canvas>();
                if (rootCanvas != null)
                {
                    rootCanvas.enabled = false;
                }
                else
                {
                    gameObject.SetActive(false);
                }
                return;
            }
        }

        displayGameStats();
        checkHostStatus();
    }

    private void checkHostStatus()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsIds.Count > 1)
            {
                if (backButton != null)
                {
                    backButton.interactable = false;
                    TextMeshProUGUI btnText = backButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = "ESPERANDO A LOS DEMÁS...";
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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsIds.Count > 1)
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