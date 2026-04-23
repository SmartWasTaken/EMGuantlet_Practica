using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Configuración")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float safetyDelay = 1.0f;

    private bool isFading = false;

    private void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        StartCoroutine(WaitForNetworkAndFade());
    }

    private void Update()
    {
        if (loadingText != null && !isFading)
        {
            int dots = Mathf.FloorToInt(Time.time * 3f) % 4;
            loadingText.text = "CARGANDO PARTIDA" + new string('.', dots);
        }
    }

    private IEnumerator WaitForNetworkAndFade()
    {
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        while (GameManager.Instance == null || GameManager.Instance.LocalPlayerController == null)
        {
            yield return null;
        }

        while (PlayerController.ActivePlayers.Count < NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            yield return null;
        }

        if (loadingText != null) loadingText.text = "¡LISTOS!";
        yield return new WaitForSeconds(safetyDelay);

        isFading = true;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - (elapsedTime / fadeDuration);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}