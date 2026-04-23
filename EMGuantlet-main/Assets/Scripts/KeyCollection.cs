using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(UniqueEntity))]
public class KeyCollection : NetworkBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private UniqueEntity uniqueEntity;

    public string EntityId => uniqueEntity?.EntityId ?? "UNKNOWN";
    public EntityType EntityType => uniqueEntity?.Type ?? EntityType.Pickup_Key;

    /// <summary>
    /// Inicializa la referencia de entidad única y valida el tipo configurado.
    /// </summary>
    private void Awake()
    {
        uniqueEntity = GetComponent<UniqueEntity>();

        if (uniqueEntity != null && uniqueEntity.Type != EntityType.Pickup_Key)
        {
            Debug.LogWarning($"[KeyCollection] {gameObject.name} tiene tipo {uniqueEntity.Type} en lugar de Pickup_Key");
        }
    }

    /// <summary>
    /// Detecta la colisión con el jugador e intenta recoger la llave.
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

        if (GameManager.Instance.TryAddKey(player.EntityId, EntityId))
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