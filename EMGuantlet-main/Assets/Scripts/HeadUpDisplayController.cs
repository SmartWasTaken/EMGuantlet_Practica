using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeadUpDisplayController : MonoBehaviour
{
    private enum HudSlot
    {
        Yellow,
        Red,
        Purple,
        Green
    }

    [System.Serializable]
    private class HudBlock
    {
        public HudSlot slot;
        public GameObject root;

        [Header("Player Info")]
        public TextMeshProUGUI textPlayerName;

        [Header("Hearts")]
        public Image imageHeartTens;
        public Image imageHeartUnits;

        [Header("Keys")]
        public Image imageKeyUnits;

        [Header("Diamonds")]
        public Image imageDiamondsHundreds;
        public Image imageDiamondTens;
        public Image imageDiamondUnits;
    }

    [Header("Bloques de HUD por personaje")]
    [SerializeField] private HudBlock[] hudBlocks;

    [Header("Single Player")]
    [SerializeField] private bool hideNonSelectedBlocks = true;

    [Header("Log de Partida")]
    [SerializeField] private GameObject logPanel;
    [SerializeField] private TextMeshProUGUI logText;
    private Coroutine clearLogRoutine;

    [Header("Indicador de Espectadores")]
    [SerializeField] private GameObject spectatorCountPanel;
    [SerializeField] private TextMeshProUGUI spectatorCountText;

    [Header("Sprites de cifras")]
    [SerializeField] private Sprite spriteZero;
    [SerializeField] private Sprite spriteOne;
    [SerializeField] private Sprite spriteTwo;
    [SerializeField] private Sprite spriteThree;
    [SerializeField] private Sprite spriteFour;
    [SerializeField] private Sprite spriteFive;
    [SerializeField] private Sprite spriteSix;
    [SerializeField] private Sprite spriteSeven;
    [SerializeField] private Sprite spriteEight;
    [SerializeField] private Sprite spriteNine;

    private HudBlock activeBlock;

    private bool isSpectating = false;
    private CameraController cameraController;

    private void Awake()
    {
    }

    private void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();

        if (logText != null) logText.text = "";
        if (logPanel != null) logPanel.SetActive(false);
        if (spectatorCountPanel != null) spectatorCountPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnHealthChanged += UpdateHearts;
        GameEvents.OnKeysChanged += UpdateKeys;
        GameEvents.OnDiamondsChanged += UpdateDiamonds;
        GameEvents.OnSpectatorTargetChanged += HandleSpectatorTargetChanged;

        GameManager.OnGameLogMessage += HandleGameLogMessage;
    }

    private void OnDisable()
    {
        GameEvents.OnHealthChanged -= UpdateHearts;
        GameEvents.OnKeysChanged -= UpdateKeys;
        GameEvents.OnDiamondsChanged -= UpdateDiamonds;
        GameEvents.OnSpectatorTargetChanged -= HandleSpectatorTargetChanged;

        GameManager.OnGameLogMessage -= HandleGameLogMessage;
    }

    private void Update()
    {
        if (!isSpectating && GameManager.Instance != null && GameManager.Instance.LocalPlayerController != null)
        {
            int viewers = GameManager.Instance.LocalPlayerController.netSpectatorsCount.Value;
            if (spectatorCountPanel != null)
            {
                bool shouldShow = viewers > 0;
                if (spectatorCountPanel.activeSelf != shouldShow) spectatorCountPanel.SetActive(shouldShow);

                if (shouldShow && spectatorCountText != null) spectatorCountText.text = viewers.ToString();
            }
        }

        if (!isSpectating || cameraController == null || cameraController.SpectatedPlayer == null) return;

        PlayerController spectated = cameraController.SpectatedPlayer;

        UpdateHearts(spectated.CurrentHealth);

        int keys = spectated.netKeys.Value;
        int units = keys % 10;
        Sprite unitsSprite = getSpriteForDigit(units);
        if (activeBlock != null && activeBlock.imageKeyUnits != null && activeBlock.imageKeyUnits.sprite != unitsSprite)
            activeBlock.imageKeyUnits.sprite = unitsSprite;

        int diamonds = spectated.netDiamonds.Value;
        int hundreds = diamonds / 100;
        int tens = (diamonds % 100) / 10;
        int dUnits = diamonds % 10;
        if (activeBlock != null)
        {
            activeBlock.imageDiamondsHundreds.sprite = getSpriteForDigit(hundreds);
            activeBlock.imageDiamondTens.sprite = getSpriteForDigit(tens);
            activeBlock.imageDiamondUnits.sprite = getSpriteForDigit(dUnits);
        }
    }

    private void HandleGameLogMessage(string msg)
    {
        if (logText == null) return;

        if (logPanel != null) logPanel.SetActive(true);
        logText.text = msg;

        if (clearLogRoutine != null) StopCoroutine(clearLogRoutine);
        clearLogRoutine = StartCoroutine(ClearLogAfterDelay(4f));
    }

    private System.Collections.IEnumerator ClearLogAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (logText != null) logText.text = "";
        if (logPanel != null) logPanel.SetActive(false);
    }

    public void EnableSpectatorMode()
    {
        isSpectating = true;
        if (spectatorCountPanel != null) spectatorCountPanel.SetActive(false);
    }

    public void DisableSpectatorMode()
    {
        isSpectating = false;
        if (hudBlocks != null)
        {
            foreach (var block in hudBlocks)
            {
                if (block != null && block.root != null)
                {
                    block.root.SetActive(false);
                }
            }
        }
    }

    public void InitializeHUD()
    {
        resolveActiveBlockFromSelectedCharacter();
        refreshBlockVisibility();

        if (activeBlock != null && activeBlock.textPlayerName != null && Unity.Netcode.NetworkManager.Singleton != null)
        {
            ulong myId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            activeBlock.textPlayerName.text = GameManager.Instance.GetPlayerName(myId);
        }
    }

    public void UpdateHearts(int hearts)
    {
        if (activeBlock == null) return;

        if (isSpectating && cameraController != null && cameraController.SpectatedPlayer != null)
        {
            hearts = cameraController.SpectatedPlayer.CurrentHealth;
        }

        if (hearts < 0) hearts = 0;

        int tens = hearts / 10;
        int units = hearts % 10;

        Sprite tensSprite = getSpriteForDigit(tens);
        Sprite unitsSprite = getSpriteForDigit(units);

        if (activeBlock.imageHeartTens != null && activeBlock.imageHeartTens.sprite != tensSprite)
            activeBlock.imageHeartTens.sprite = tensSprite;

        if (activeBlock.imageHeartUnits != null && activeBlock.imageHeartUnits.sprite != unitsSprite)
            activeBlock.imageHeartUnits.sprite = unitsSprite;
    }

    public void UpdateKeys()
    {
        if (isSpectating) return;

        if (activeBlock == null) return;

        int keys = GameManager.Instance != null ? GameManager.Instance.GetKeys() : 0;
        int units = keys % 10;
        Sprite unitsSprite = getSpriteForDigit(units);

        if (activeBlock.imageKeyUnits != null && activeBlock.imageKeyUnits.sprite != unitsSprite)
            activeBlock.imageKeyUnits.sprite = unitsSprite;
    }

    public void UpdateDiamonds()
    {
        if (isSpectating) return;

        if (activeBlock == null) return;

        int diamonds = GameManager.Instance != null ? GameManager.Instance.GetDiamonds() : 0;
        int hundreds = diamonds / 100;
        int tens = (diamonds % 100) / 10;
        int units = diamonds % 10;

        Sprite hundredsSprite = getSpriteForDigit(hundreds);
        Sprite tensSprite = getSpriteForDigit(tens);
        Sprite unitsSprite = getSpriteForDigit(units);

        if (activeBlock.imageDiamondsHundreds != null && activeBlock.imageDiamondsHundreds.sprite != hundredsSprite)
            activeBlock.imageDiamondsHundreds.sprite = hundredsSprite;

        if (activeBlock.imageDiamondTens != null && activeBlock.imageDiamondTens.sprite != tensSprite)
            activeBlock.imageDiamondTens.sprite = tensSprite;

        if (activeBlock.imageDiamondUnits != null && activeBlock.imageDiamondUnits.sprite != unitsSprite)
            activeBlock.imageDiamondUnits.sprite = unitsSprite;
    }

    private void resolveActiveBlockFromSelectedCharacter()
    {
        activeBlock = findBlockBySlot(HudSlot.Green);

        string characterName = GameManager.Instance?.SelectedCharacterStats?.characterName;
        if (string.IsNullOrEmpty(characterName)) return;

        string characterNameLowerCase = characterName.ToLowerInvariant();

        if (characterNameLowerCase.Contains("yellow")) activeBlock = findBlockBySlot(HudSlot.Yellow);
        else if (characterNameLowerCase.Contains("red")) activeBlock = findBlockBySlot(HudSlot.Red);
        else if (characterNameLowerCase.Contains("purple")) activeBlock = findBlockBySlot(HudSlot.Purple);
        else if (characterNameLowerCase.Contains("green")) activeBlock = findBlockBySlot(HudSlot.Green);
    }

    private void refreshBlockVisibility()
    {
        if (hudBlocks == null) return;

        for (int i = 0; i < hudBlocks.Length; i++)
        {
            HudBlock block = hudBlocks[i];
            if (block == null || block.root == null) continue;

            bool visible = !hideNonSelectedBlocks || block == activeBlock;
            block.root.SetActive(visible);
        }
    }

    private HudBlock findBlockBySlot(HudSlot slot)
    {
        if (hudBlocks == null) return null;

        for (int i = 0; i < hudBlocks.Length; i++)
        {
            if (hudBlocks[i] != null && hudBlocks[i].slot == slot)
                return hudBlocks[i];
        }

        return null;
    }

    private Sprite getSpriteForDigit(int digit)
    {
        return digit switch
        {
            0 => spriteZero,
            1 => spriteOne,
            2 => spriteTwo,
            3 => spriteThree,
            4 => spriteFour,
            5 => spriteFive,
            6 => spriteSix,
            7 => spriteSeven,
            8 => spriteEight,
            9 => spriteNine,
            _ => spriteZero
        };
    }

    private void HandleSpectatorTargetChanged(PlayerController newTarget)
    {
        if (newTarget == null || GameManager.Instance == null) return;

        ulong id = newTarget.OwnerClientId;
        int characterIndex = GameManager.Instance.GetPlayerSelection(id);
        if (characterIndex >= 0 && characterIndex < GameManager.Instance.allCharacters.Length)
        {
            string charName = GameManager.Instance.allCharacters[characterIndex].characterName.ToLowerInvariant();
            if (charName.Contains("yellow")) activeBlock = findBlockBySlot(HudSlot.Yellow);
            else if (charName.Contains("red")) activeBlock = findBlockBySlot(HudSlot.Red);
            else if (charName.Contains("purple")) activeBlock = findBlockBySlot(HudSlot.Purple);
            else if (charName.Contains("green")) activeBlock = findBlockBySlot(HudSlot.Green);

            refreshBlockVisibility();

            Invoke(nameof(ForceApplySpectatorName), 0.05f);
        }
    }

    private void ForceApplySpectatorName()
    {
        if (activeBlock != null && activeBlock.textPlayerName != null && cameraController != null && cameraController.SpectatedPlayer != null)
        {
            ulong id = cameraController.SpectatedPlayer.OwnerClientId;
            activeBlock.textPlayerName.text = GameManager.Instance.GetPlayerName(id);
        }
    }
}