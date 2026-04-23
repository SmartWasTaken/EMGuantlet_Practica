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

    /// <summary>
    /// Dejamos el Awake vacío porque en multijugador debemos esperar 
    /// a que la red nos confirme qué personaje somos antes de encender el HUD.
    /// </summary>
    private void Awake()
    {
        // Se ha movido la inicialización a InitializeHUD()
    }

    private void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();
    }

    /// <summary>
    /// Suscribe los eventos de actualización del HUD al habilitar el componente.
    /// </summary>
    private void OnEnable()
    {
        GameEvents.OnHealthChanged += UpdateHearts;
        GameEvents.OnKeysChanged += UpdateKeys;
        GameEvents.OnDiamondsChanged += UpdateDiamonds;
    }

    /// <summary>
    /// Desuscribe los eventos de actualización del HUD al deshabilitar el componente.
    /// </summary>
    private void OnDisable()
    {
        GameEvents.OnHealthChanged -= UpdateHearts;
        GameEvents.OnKeysChanged -= UpdateKeys;
        GameEvents.OnDiamondsChanged -= UpdateDiamonds;
    }

    private void Update()
    {
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

        if (GameManager.Instance != null)
        {
            ulong id = spectated.OwnerClientId;
            int characterIndex = GameManager.Instance.GetPlayerSelection(id);
            if (characterIndex >= 0 && characterIndex < GameManager.Instance.allCharacters.Length)
            {
                string charName = GameManager.Instance.allCharacters[characterIndex].characterName.ToLowerInvariant();
                if (charName.Contains("yellow")) activeBlock = findBlockBySlot(HudSlot.Yellow);
                else if (charName.Contains("red")) activeBlock = findBlockBySlot(HudSlot.Red);
                else if (charName.Contains("purple")) activeBlock = findBlockBySlot(HudSlot.Purple);
                else if (charName.Contains("green")) activeBlock = findBlockBySlot(HudSlot.Green);

                refreshBlockVisibility();
                if (activeBlock != null && activeBlock.textPlayerName != null)
                {
                    activeBlock.textPlayerName.text = GameManager.Instance.GetPlayerName(id);
                }
            }
        }
    }

    public void EnableSpectatorMode()
    {
        isSpectating = true;
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

    /// <summary>
    /// Método que será llamado por el Jugador cuando ya tenga sus datos de red cargados.
    /// </summary>
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

    /// <summary>
    /// Actualiza los dígitos de vida del bloque de HUD activo.
    /// </summary>
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

    /// <summary>
    /// Actualiza el dígito de llaves del bloque de HUD activo.
    /// </summary>
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

    /// <summary>
    /// Actualiza los dígitos de diamantes del bloque de HUD activo.
    /// </summary>
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

    /// <summary>
    /// Determina el bloque HUD activo en función del nombre del personaje seleccionado.
    /// </summary>
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

    /// <summary>
    /// Busca y devuelve el bloque de HUD asociado al slot indicado.
    /// </summary>
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

    /// <summary>
    /// Devuelve el sprite correspondiente al dígito solicitado.
    /// </summary>
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
}