using Unity.Netcode;
using UnityEngine;
using System.Collections;

public abstract class EnemyController : CharController
{
    protected int damageToPlayer;
    protected GameObject[] dropPrefabs;

    protected bool isSpawning = false;
    protected Vector3 targetScale;

    /// <summary>
    /// Inicializa la configuración base del enemigo heredada del controlador de personaje.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        targetScale = transform.localScale;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;
        transform.localScale = Vector3.zero;

        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float scaleMultiplier = Mathf.Sin(percent * Mathf.PI * 0.5f);

            transform.localScale = targetScale * scaleMultiplier;
            yield return null;
        }

        transform.localScale = targetScale;
        isSpawning = false;
    }

    /// <summary>
    /// Gestiona la interacción continua con el jugador para atacar o recibir daño.
    /// </summary>
    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsServer) return;

        if (isSpawning) return;

        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null) return;

        if (player.IsAttacking || player.netIsAttacking.Value)
        {
            TakeDamage(player.DamageToEnemy, (transform.position - player.transform.position).normalized);
            checkDeath();
        }
        else
        {
            Vector2 knockbackDir = (player.transform.position - transform.position).normalized;
            player.TakeDamage(damageToPlayer, knockbackDir);
        }
    }

    /// <summary>
    /// Carga estadísticas de combate y referencias de drops desde EnemyStats.
    /// </summary>
    protected override void LoadStats()
    {
        base.LoadStats();

        EnemyStats enemyStats = stats as EnemyStats;

        if (enemyStats != null)
        {
            moveSpeed *= enemyStats.speedPenalty;
            damageToPlayer = enemyStats.attackDamage;
            dropPrefabs = enemyStats.dropPrefabs;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No tiene EnemyStats asignado. Usando valores por defecto.");
            damageToPlayer = 1;
            moveSpeed *= 0.75f;
        }
    }

    /// <summary>
    /// Marca y procesa la muerte del enemigo cuando su vida llega a cero.
    /// </summary>
    public override void Die()
    {
        base.Die();

        if (IsServer && GameManager.Instance != null)
            GameManager.Instance.AddEnemyKill();

        if (IsServer) spawnDrops();
    }

    /// <summary>
    /// Verifica si el enemigo debe morir y programa su destrucción en escena.
    /// </summary>
    protected void checkDeath()
    {
        if (health <= 0)
        {
            Die();
            StartCoroutine(DespawnAfterAnimation());
        }
    }

    protected override void CheckDeathFromClient()
    {
        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DespawnAfterAnimation()
    {
        yield return new WaitForSeconds(1.2f);

        var netObj = GetComponent<Unity.Netcode.NetworkObject>();
        if (netObj != null && netObj.IsSpawned && IsServer)
            netObj.Despawn();
        else if (netObj == null)
            Destroy(gameObject);
    }

    /// <summary>
    /// Genera los drops del enemigo usando la configuración activa del mapa.
    /// </summary>
    protected virtual void spawnDrops()
    {
        if (!IsServer) return;

        if (dropPrefabs == null || dropPrefabs.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No tiene dropPrefabs configurados.");
            return;
        }

        EnemyDropConfig dropCfg = getDropConfig();

        if (dropCfg == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No hay MapConfig activo. No se spawnean drops.");
            return;
        }

        int dropCount = Random.Range(dropCfg.minDiamondDrops, dropCfg.maxDiamondDrops + 1);
        if (dropCount <= 0) return;

        float angleStep = 360f / dropCount;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < dropCount; i++)
        {
            GameObject dropPrefab = dropPrefabs[0];

            if (dropPrefabs.Length > 1 && i == 0 && Random.value < dropCfg.keyDropChance)
                dropPrefab = dropPrefabs[1];

            if (dropPrefab != null)
            {
                float angle = startAngle + i * angleStep;
                Vector3 dropPosition = transform.position +
                    new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * 0.5f;

                GameObject drop = Instantiate(dropPrefab, dropPosition, Quaternion.identity);
                UniqueEntity uniqueEntity = drop.GetComponent<UniqueEntity>();
                if (uniqueEntity != null) uniqueEntity.RegenerateIdOnSpawn();

                NetworkObject netObj = drop.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn(true);
                }
                else
                {
                    Debug.LogError($"[spawnDrops] El prefab {dropPrefab.name} no tiene NetworkObject.");
                }
            }
        }
    }

    /// <summary>
    /// Obtiene la configuración de drops del mapa según el tipo de enemigo.
    /// </summary>
    protected virtual EnemyDropConfig getDropConfig()
    {
        MapConfig mapCfg = GameManager.Instance?.SelectedMapConfig;
        if (mapCfg == null) return null;

        if (stats is ChaseEnemyStats)
            return mapCfg.dragonDropConfig;

        if (stats is LemniscateEnemyStats)
            return mapCfg.goatDropConfig;

        return null;
    }
}