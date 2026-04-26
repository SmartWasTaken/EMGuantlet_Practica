using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    private TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;

    [SerializeField] private bool showFPS = true;

    private void Awake()
    {
        fpsText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (!showFPS)
        {
            fpsText.text = "";
            return;
        }

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = string.Format("{0:0.} FPS", fps);
    }

    public void SetShowFPS(bool value)
    {
        showFPS = value;
        if (!value) fpsText.text = "";
    }
}