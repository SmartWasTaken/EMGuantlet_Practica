using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerLabelController : NetworkBehaviour
{
    private TextMeshPro textMesh;
    private PlayerController parentController;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        parentController = GetComponentInParent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (parentController != null && parentController.IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateNameLabel();
    }

    private void Start()
    {
        if (parentController != null && !parentController.IsOwner)
        {
            UpdateNameLabel();
        }
    }

    private void UpdateNameLabel()
    {
        if (parentController == null || GameManager.Instance == null) return;

        ulong ownerId = parentController.OwnerClientId;
        string playerName = GameManager.Instance.GetPlayerName(ownerId);

        if (textMesh != null)
        {
            textMesh.text = playerName;
        }
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}