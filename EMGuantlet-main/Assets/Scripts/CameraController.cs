using UnityEngine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    private Transform target;
    public PlayerController SpectatedPlayer { get; private set; }
    private int currentSpectatorIndex = 0;
    public bool IsSpectating { get; private set; } = false;

    private PlayerController lastSpectatedPlayer;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            target = GameManager.Instance.LocalPlayerTransform;
            if (target != null) SpectatedPlayer = target.GetComponent<PlayerController>();

            GameEvents.OnLocalPlayerRegistered += handlePlayerRegistered;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameEvents.OnLocalPlayerRegistered -= handlePlayerRegistered;

        StopSpectating();
    }

    private void LateUpdate()
    {
        if (target == null || isTargetDead())
        {
            cycleSpectatorTarget(1);
        }

        if (SpectatedPlayer != null && !isTargetDead())
        {
            target = SpectatedPlayer.transform;
        }

        if (target != null)
            transform.position = target.position + offset;

        if (SpectatedPlayer != lastSpectatedPlayer)
        {
            UpdateSpectatorCounts();
        }
    }

    public void StopSpectating()
    {
        if (IsSpectating)
        {
            if (lastSpectatedPlayer != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                lastSpectatedPlayer.ChangeSpectatorCountServerRpc(-1);
            }
            SpectatedPlayer = null;
            lastSpectatedPlayer = null;
            IsSpectating = false;
        }
    }

    private void UpdateSpectatorCounts()
    {
        if (IsSpectating)
        {
            if (lastSpectatedPlayer != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                lastSpectatedPlayer.ChangeSpectatorCountServerRpc(-1);
            }
            if (SpectatedPlayer != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                SpectatedPlayer.ChangeSpectatorCountServerRpc(1);
            }
        }
        lastSpectatedPlayer = SpectatedPlayer;
    }

    private bool isTargetDead()
    {
        PlayerController pc = target != null ? target.GetComponent<PlayerController>() : null;
        return pc != null && pc.CurrentHealth <= 0;
    }

    private void findAliveTeammate()
    {
        foreach (PlayerController p in PlayerController.ActivePlayers)
        {
            if (p != null && p.CurrentHealth > 0)
            {
                target = p.transform;
                SpectatedPlayer = p;
                IsSpectating = true;
                return;
            }
        }
    }

    public void cycleSpectatorTarget(int direction)
    {
        var alivePlayers = new System.Collections.Generic.List<PlayerController>();
        foreach (var p in PlayerController.ActivePlayers)
        {
            if (p != null && p.CurrentHealth > 0)
                alivePlayers.Add(p);
        }

        if (alivePlayers.Count == 0)
        {
            target = null;
            SpectatedPlayer = null;
            IsSpectating = false;
            return;
        }

        currentSpectatorIndex += direction;

        if (currentSpectatorIndex >= alivePlayers.Count) currentSpectatorIndex = 0;
        if (currentSpectatorIndex < 0) currentSpectatorIndex = alivePlayers.Count - 1;

        SpectatedPlayer = alivePlayers[currentSpectatorIndex];
        target = SpectatedPlayer.transform;
        IsSpectating = true;
    }

    private void handlePlayerRegistered(PlayerController player)
    {
        target = player != null ? player.transform : null;
        SpectatedPlayer = player;
    }
}