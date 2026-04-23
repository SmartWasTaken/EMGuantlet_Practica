using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : CharController
{
    protected int damageToEnemy;
    protected float attackCooldown;

    private PlayerControls controls;

    public bool IsAttacking { get; private set; } = false;
    public int DamageToEnemy => damageToEnemy;

    public NetworkVariable<int> netCharacterIndex = new NetworkVariable<int>(-1);

    public NetworkVariable<bool> netIsAttacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public static System.Collections.Generic.List<PlayerController> ActivePlayers = new System.Collections.Generic.List<PlayerController>();

    /// <summary>
    /// Inicializa controles de entrada y registra el jugador local en el gestor global.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ActivePlayers.Add(this);
        netIsAttacking.OnValueChanged += OnNetworkAttackChanged;

        if (IsOwner)
        {
            controls = new PlayerControls();
            controls.Player.Move.performed += ctx => movement = ctx.ReadValue<Vector2>();
            controls.Player.Move.canceled += _ => movement = Vector2.zero;
            controls.Player.Attack.performed += onAttack;
            controls.Enable();

            UniqueEntity uniqueEntity = GetComponent<UniqueEntity>();
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterLocalPlayer(this, uniqueEntity);
        }
        netCharacterIndex.OnValueChanged += OnCharacterIndexChanged;

        if (netCharacterIndex.Value != -1)
        {
            LoadStatsFromNetwork(netCharacterIndex.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        ActivePlayers.Remove(this);
        netIsAttacking.OnValueChanged -= OnNetworkAttackChanged;

        netCharacterIndex.OnValueChanged -= OnCharacterIndexChanged;
        if (IsOwner && controls != null)
        {
            controls.Player.Attack.performed -= onAttack;
            controls.Disable();
        }
        base.OnNetworkDespawn();
    }

    private void OnNetworkAttackChanged(bool previousValue, bool newValue)
    {
        if (!IsOwner && newValue == true)
        {
            animator.SetTrigger("Attack");
        }
    }

    private void OnCharacterIndexChanged(int previousValue, int newValue)
    {
        if (newValue != -1)
        {
            LoadStatsFromNetwork(newValue);
        }
    }

    private void LoadStatsFromNetwork(int index)
    {
        if (GameManager.Instance != null && index >= 0 && index < GameManager.Instance.allCharacters.Length)
        {
            ApplyCharacterStats(GameManager.Instance.allCharacters[index]);
        }
    }

    /// <summary>
    /// Inicializa estado del jugador y notifica los valores iniciales al HUD.
    /// </summary>
    //protected override void Start()
    //{
    //    base.Start();
    //
    //    // Dispara eventos iniciales para actualizar el HUD
    //    GameEvents.HealthChanged(health);
    //    GameEvents.KeysChanged();
    //    GameEvents.DiamondsChanged();
    //
    //    IsAttacking = false;
    //}

    /// <summary>
    /// Actualiza animación, orientación y estado de vida en cada frame.
    /// </summary>
    protected override void Update()
    {
        if (IsOwner)
        {
            netMovement.Value = movement;
            checkDeath();
        }

        Vector2 currentMove = netMovement.Value;

        animator.SetFloat("speed", currentMove.sqrMagnitude);

        if (currentMove.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(currentMove.y, currentMove.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    /// <summary>
    /// Activa el mapa de controles y suscribe la acción de ataque.
    /// </summary>
    //private void OnEnable()
    //{
    //    controls.Enable();
    //    controls.Player.Attack.performed += onAttack;
    //}
    //
    ///// <summary>
    ///// Desuscribe la acción de ataque y desactiva el mapa de controles.
    ///// </summary>
    //private void OnDisable()
    //{
    //    controls.Player.Attack.performed -= onAttack;
    //    controls.Disable();
    //}

    /// <summary>
    /// Gestiona la muerte del jugador y lanza el flujo de fin de partida.
    /// </summary>
    public override void Die()
    {
        base.Die();

        // Dispara evento de muerte
        GameEvents.PlayerDied();

        GameManager.Instance?.TriggerGameOver();

    }

    /// <summary>
    /// Aplica daño al jugador y notifica el cambio de salud al HUD.
    /// </summary>
    public override void TakeDamage(int amount, Vector2 knockbackDir)
    {
        base.TakeDamage(amount, knockbackDir);

        if (IsServer && IsOwner)
        {
            GameEvents.HealthChanged(health);
        }
    }

    /// <summary>
    /// Aplica las estadísticas que el Servidor ha ordenado.
    /// </summary>
    public void ApplyCharacterStats(PlayerStats newStats)
    {
        if (newStats == null) return;

        stats = newStats;

        PlayerStats pStats = stats as PlayerStats;
        if (pStats != null && pStats.animatorController != null)
        {
            animator.runtimeAnimatorController = pStats.animatorController;
        }

        LoadStats();
        Debug.Log($"[PlayerController] Red aplicó personaje: {newStats.characterName}");

        if (IsOwner)
        {
            if (GameManager.Instance != null && pStats != null)
            {
                GameManager.Instance.SelectedCharacterStats = pStats;
            }

            StartCoroutine(WaitForDataAndInitializeHUD(initialHealth));
        }
    }

    //barrera condicional
    private System.Collections.IEnumerator WaitForDataAndInitializeHUD(int currentHealth)
    {
        HeadUpDisplayController hud = null;

        while (hud == null)
        {
            hud = FindFirstObjectByType<HeadUpDisplayController>();
            yield return null;
        }

        if (NetworkManager.Singleton != null)
        {
            ulong myId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            while (GameManager.Instance != null && !GameManager.Instance.HasPlayerName(myId))
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.1f);

        hud.InitializeHUD();
        GameEvents.HealthChanged(currentHealth);
        GameEvents.KeysChanged();
        GameEvents.DiamondsChanged();
    }

    /// <summary>
    /// Carga estadísticas del personaje seleccionado y aplica valores de combate y movimiento.
    /// </summary>
    protected override void LoadStats()
    {

        base.LoadStats();

        PlayerStats playerStats = stats as PlayerStats;

        if (playerStats != null)
        {
            moveSpeed *= playerStats.speedBonus;

            damageToEnemy = playerStats.attackDamage;
            attackCooldown = playerStats.attackCooldown;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No tiene PlayerStats asignado. Usando valores por defecto.");
            damageToEnemy = 50;
            attackCooldown = 0.5f;
            moveSpeed *= 1.25f;
        }
    }

    /// <summary>
    /// Verifica si la salud ha llegado a cero y ejecuta la muerte una sola vez.
    /// </summary>
    private void checkDeath()
    {
        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// Inicia la animación de ataque y programa su final según el cooldown.
    /// </summary>
    private void onAttack(InputAction.CallbackContext context)
    {
        animator.SetTrigger("Attack");
        IsAttacking = true;

        if (IsOwner) netIsAttacking.Value = true;

        Invoke(nameof(endAttack), attackCooldown);
    }

    /// <summary>
    /// Finaliza el estado de ataque del jugador.
    /// </summary>
    private void endAttack()
    {
        IsAttacking = false;

        if (IsOwner) netIsAttacking.Value = false;
    }

    [ClientRpc]
    public void ApplyStatsClientRpc(int characterIndex)
    {
        if (GameManager.Instance != null && GameManager.Instance.allCharacters.Length > characterIndex)
        {
            ApplyCharacterStats(GameManager.Instance.allCharacters[characterIndex]);
        }
    }

    protected override void UpdateHealthUI()
    {
        if (IsOwner)
        {
            GameEvents.HealthChanged(health);
        }
    }

    protected override void CheckDeathFromClient()
    {
        if (IsOwner)
        {
            checkDeath();
        }
    }
}