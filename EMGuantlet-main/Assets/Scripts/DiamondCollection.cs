using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(UniqueEntity))]
public class DiamondCollection : NetworkBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private UniqueEntity uniqueEntity;

    public string EntityId => uniqueEntity?.EntityId ?? "UNKNOWN";
    public EntityType EntityType => uniqueEntity?.Type ?? EntityType.Pickup_Diamond;

    /// <summary>
    /// Inicializa la referencia de entidad única y valida el tipo configurado.
    /// </summary>
    private void Awake()
    {
        uniqueEntity = GetComponent<UniqueEntity>();

        if (uniqueEntity != null && uniqueEntity.Type != EntityType.Pickup_Diamond)
        {
            Debug.LogWarning($"[DiamondCollection] {gameObject.name} tiene tipo {uniqueEntity.Type} en lugar de Pickup_Diamond");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(SpawnAnimation());
    }

    private IEnumerator SpawnAnimation()
    {
        Transform visualTransform = transform;

        SpriteRenderer spr = GetComponentInChildren<SpriteRenderer>();
        if (spr != null && spr.transform != transform)
        {
            visualTransform = spr.transform;
        }

        Vector3 originalScale = visualTransform.localScale;
        Vector3 originalPosition = visualTransform.localPosition;

        visualTransform.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            float scaleValue = Mathf.Sin(percent * Mathf.PI);
            visualTransform.localScale = originalScale * (percent + (scaleValue * 0.3f));

            float heightOffset = Mathf.Sin(percent * Mathf.PI) * 0.5f;
            visualTransform.localPosition = originalPosition + new Vector3(0, heightOffset, 0);

            yield return null;
        }

        visualTransform.localScale = originalScale;
        visualTransform.localPosition = originalPosition;
    }

    /// <summary>
    /// Detecta la colisión con el jugador e intenta recoger el diamante.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (player.IsOwner)
        {
            Collider2D col = GetComponent<Collider2D>();
            SpriteRenderer spr = GetComponentInChildren<SpriteRenderer>();
            if (col != null) col.enabled = false;
            if (spr != null) spr.enabled = false;
        }

        if (!IsServer) return;

        if (GameManager.Instance == null) return;

        if (GameManager.Instance.TryAddDiamond(player.EntityId, EntityId))
        {
            Debug.Log($"[{EntityType}:{EntityId}] collected by [Player:{player.EntityId}]");

            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}