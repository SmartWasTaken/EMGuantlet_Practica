using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(UniqueEntity))]
public class KeyCollection : NetworkBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private UniqueEntity uniqueEntity;

    private bool canBeCollected = false;
    private bool hasBeenCollectedServer = false;

    private Vector3 targetScale;

    public string EntityId => uniqueEntity?.EntityId ?? "UNKNOWN";
    public EntityType EntityType => uniqueEntity?.Type ?? EntityType.Pickup_Key;

    private void Awake()
    {
        uniqueEntity = GetComponent<UniqueEntity>();

        if (uniqueEntity != null && uniqueEntity.Type != EntityType.Pickup_Key)
        {
            Debug.LogWarning($"[KeyCollection] {gameObject.name} tiene tipo {uniqueEntity.Type} en lugar de Pickup_Key");
        }

        SpriteRenderer spr = GetComponentInChildren<SpriteRenderer>();
        targetScale = (spr != null && spr.transform != transform) ? spr.transform.localScale : transform.localScale;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        canBeCollected = false;
        hasBeenCollectedServer = false;
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

        Vector3 originalPosition = visualTransform.localPosition;

        visualTransform.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            float scaleValue = Mathf.Sin(percent * Mathf.PI);
            visualTransform.localScale = targetScale * (percent + (scaleValue * 0.3f));

            float heightOffset = Mathf.Sin(percent * Mathf.PI) * 0.5f;
            visualTransform.localPosition = originalPosition + new Vector3(0, heightOffset, 0);

            yield return null;
        }

        visualTransform.localScale = targetScale;
        visualTransform.localPosition = originalPosition;

        canBeCollected = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryCollect(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryCollect(collision.gameObject);
    }

    private void TryCollect(GameObject other)
    {
        if (!canBeCollected) return;

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

        if (hasBeenCollectedServer) return;

        if (GameManager.Instance == null) return;

        if (GameManager.Instance.TryAddKey(player.EntityId, EntityId))
        {
            hasBeenCollectedServer = true;
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