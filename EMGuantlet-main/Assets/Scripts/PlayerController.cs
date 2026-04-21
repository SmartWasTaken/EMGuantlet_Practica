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

        // Si este clon es mío, enciendo MI mando y aviso a MI cámara
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
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && controls != null)
        {
            controls.Player.Attack.performed -= onAttack;
            controls.Disable();
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
        if (!IsOwner) return; //Solo muevo yo MI personaje, no el de mi compañero

        animator.SetFloat("speed", movement.sqrMagnitude);

        if (movement.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        checkDeath();
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

        // Dispara evento de cambio de salud
        GameEvents.HealthChanged(health);
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
    }

    /// <summary>
    /// Carga estadísticas del personaje seleccionado y aplica valores de combate y movimiento.
    /// </summary>
    protected override void LoadStats()
    {
        //// ✅ PRIMERO: Intenta cargar desde GameManager (personaje seleccionado)
        //if (GameManager.Instance != null && GameManager.Instance.SelectedCharacterStats != null)
        //{
        //    stats = GameManager.Instance.SelectedCharacterStats;
        //    Debug.Log($"[PlayerController] Cargando personaje seleccionado: {stats.characterName}");
        //}
        //
        //// Si no hay personaje seleccionado, usa el asignado en el prefab (fallback)
        //if (stats == null)
        //{
        //    Debug.LogWarning("[PlayerController] No hay personaje seleccionado, usando stats por defecto del prefab");
        //}

        base.LoadStats();

        // ✅ Haz casting del campo heredado
        PlayerStats playerStats = stats as PlayerStats;

        if (playerStats != null)
        {
            // Aplica el bonus de velocidad del jugador
            moveSpeed *= playerStats.speedBonus;
            
            // Carga stats específicas del jugador
            damageToEnemy = playerStats.attackDamage;
            attackCooldown = playerStats.attackCooldown;
        }
        else
        {
            // Valores por defecto si no hay PlayerStats
            Debug.LogWarning($"[{gameObject.name}] No tiene PlayerStats asignado. Usando valores por defecto.");
            damageToEnemy = 50;
            attackCooldown = 0.5f;
            moveSpeed *= 1.25f; // Bonus por defecto
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
        Invoke(nameof(endAttack), attackCooldown);
    }

    /// <summary>
    /// Finaliza el estado de ataque del jugador.
    /// </summary>
    private void endAttack()
    {
        IsAttacking = false;
    }

    [ClientRpc]
    public void ApplyStatsClientRpc(int characterIndex)
    {
        if (GameManager.Instance != null && GameManager.Instance.allCharacters.Length > characterIndex)
        {
            ApplyCharacterStats(GameManager.Instance.allCharacters[characterIndex]);
        }
    }
}
