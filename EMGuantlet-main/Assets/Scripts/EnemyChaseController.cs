using UnityEngine;

public class EnemyChaseController : EnemyController
{
    protected float chaseRange;
    protected float wanderChangeInterval;
    protected float wanderSpeedMin;
    protected float wanderSpeedMax;
    protected float idleChance;

    private Transform playerTransform;
    private Vector2 wanderDirection;
    private float wanderSpeed;
    private float wanderTimer;

    private float searchTimer = 0f;
    private const float SEARCH_INTERVAL = 0.25f;

    /// <summary>
    /// Inicializa la referencia al jugador y configura el estado inicial de vagabundeo.
    /// </summary>
    protected override void Start()
    {
        base.Start();

        if (GameManager.Instance != null)
        {
            playerTransform = GameManager.Instance.LocalPlayerTransform;
            GameEvents.OnLocalPlayerRegistered += onPlayerRegistered;
        }

        setNewWanderDirection();
    }

    /// <summary>
    /// Libera la suscripción al evento de registro del jugador al destruir el enemigo.
    /// </summary>
    private void OnDestroy()
    {
        GameEvents.OnLocalPlayerRegistered -= onPlayerRegistered;
    }

    /// <summary>
    /// Carga y aplica las estadísticas de persecución y vagabundeo del enemigo.
    /// </summary>
    protected override void LoadStats()
    {
        base.LoadStats();

        ChaseEnemyStats chaseStats = stats as ChaseEnemyStats;

        if (chaseStats != null)
        {
            chaseRange = chaseStats.chaseRange;
            wanderChangeInterval = chaseStats.wanderChangeInterval;
            wanderSpeedMin = chaseStats.wanderSpeedMin;
            wanderSpeedMax = chaseStats.wanderSpeedMax;
            idleChance = chaseStats.idleChance;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No tiene ChaseEnemyStats asignado. Usando valores por defecto.");
            chaseRange = 10f;
            wanderChangeInterval = 2f;
            wanderSpeedMin = 0.3f;
            wanderSpeedMax = 0.7f;
            idleChance = 0.2f;
        }
    }

    /// <summary>
    /// Decide si el enemigo persigue al jugador o se mueve de forma aleatoria.
    /// </summary>
    protected override void Move()
    {
        if (!IsServer) return;

        if (isKnockback)
            return;

        FindClosestPlayer();

        if (playerTransform == null)
        {
            wanderMovement();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > chaseRange)
            wanderMovement();
        else
            chasePlayer();
    }

    private void FindClosestPlayer()
    {
        searchTimer -= Time.fixedDeltaTime;
        if (searchTimer > 0f) return;

        searchTimer = SEARCH_INTERVAL;

        float closestDistance = float.MaxValue;
        Transform closest = null;

        foreach (PlayerController p in PlayerController.ActivePlayers)
        {
            if (p != null && p.CurrentHealth > 0)
            {
                float dist = Vector2.Distance(transform.position, p.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = p.transform;
                }
            }
        }
        playerTransform = closest;
    }

    /// <summary>
    /// Actualiza la referencia del jugador cuando se registra el jugador local.
    /// </summary>
    private void onPlayerRegistered(PlayerController player)
    {
        playerTransform = player != null ? player.transform : null;
    }

    /// <summary>
    /// Mueve al enemigo hacia el jugador y orienta su rotación en la dirección de avance.
    /// </summary>
    private void chasePlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        movement = direction;

        rb.linearVelocity = direction * moveSpeed;

        // Inyectamos a la red para animaciones
        netMovement.Value = direction;

        if (direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// Ejecuta el desplazamiento aleatorio del enemigo cuando está fuera del rango de persecución.
    /// </summary>
    private void wanderMovement()
    {
        wanderTimer -= Time.fixedDeltaTime;

        if (wanderTimer <= 0f)
            setNewWanderDirection();

        rb.linearVelocity = wanderDirection * wanderSpeed;

        netMovement.Value = wanderDirection;

        if (wanderDirection.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(wanderDirection.y, wanderDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// Genera una nueva dirección y velocidad para el movimiento aleatorio del enemigo.
    /// </summary>
    private void setNewWanderDirection()
    {
        if (Random.value < idleChance)
        {
            wanderDirection = Vector2.zero;
            wanderSpeed = 0f;
        }
        else
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            wanderDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            wanderSpeed = Random.Range(moveSpeed * wanderSpeedMin, moveSpeed * wanderSpeedMax);
        }

        wanderTimer = wanderChangeInterval;
    }
}