using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager hookedManager;

    private void OnEnable()
    {
        StartCoroutine(WaitForServerAndSpawn());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (hookedManager != null)
        {
            hookedManager.OnClientConnectedCallback -= SpawnPlayer;
            hookedManager.OnServerStarted -= OnServerStartedHandler;
        }
    }

    private IEnumerator WaitForServerAndSpawn()
    {
        Debug.LogWarning("[Spawner] Warte auf NetworkManager.Singleton ...");

        // Warten, bis der NetworkManager überhaupt existiert
        // (kann je nach Script Execution Order verzögert sein).
        float timeout = 10f;
        float elapsed = 0f;

        while (NetworkManager.Singleton == null && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        var nm = NetworkManager.Singleton;

        if (nm == null)
        {
            Debug.LogError("[Spawner] NetworkManager.Singleton wurde nie gefunden! Spawner kann nicht arbeiten.");
            yield break;
        }

        Debug.LogWarning("[Spawner] NetworkManager gefunden. Warte auf Server-Start ...");

        // Warten, bis dieser Prozess tatsächlich Server/Host ist.
        elapsed = 0f;

        while (!nm.IsServer && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!nm.IsServer)
        {
            // Dieser Prozess ist ein reiner Client -> kein Spawnen nötig,
            // der Host/Server kümmert sich darum.
            Debug.LogWarning("[Spawner] Kein Server (vermutlich reiner Client) -> Spawner bleibt inaktiv.");
            yield break;
        }

        Debug.LogWarning("[Spawner] Server aktiv! Hänge Events ein und spawne vorhandene Clients.");

        hookedManager = nm;
        nm.OnClientConnectedCallback -= SpawnPlayer;
        nm.OnClientConnectedCallback += SpawnPlayer;
        nm.OnServerStarted -= OnServerStartedHandler;
        nm.OnServerStarted += OnServerStartedHandler;

        foreach (ulong id in nm.ConnectedClientsIds)
            SpawnPlayer(id);
    }

    private void OnServerStartedHandler()
    {
        Debug.LogWarning("[Spawner] OnServerStarted-Event erhalten (Nachzügler-Fall).");

        var nm = NetworkManager.Singleton;

        if (nm == null || !nm.IsServer)
            return;

        foreach (ulong id in nm.ConnectedClientsIds)
            SpawnPlayer(id);
    }

    private void SpawnPlayer(ulong clientId)
    {
        Debug.LogWarning($"[Spawner] SpawnPlayer() aufgerufen für ClientId={clientId}");

        var nm = NetworkManager.Singleton;

        if (nm == null || !nm.IsServer)
        {
            Debug.LogWarning("[Spawner] Abbruch: kein Server.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[Spawner] Player Prefab ist nicht gesetzt!");
            return;
        }

        if (nm.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
        {
            Debug.LogWarning($"[Spawner] ClientId={clientId} hat bereits ein PlayerObject -> Abbruch.");
            return;
        }

        if (playerPrefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("[Spawner] Dem Player Prefab fehlt die NetworkObject-Komponente!");
            return;
        }

        Vector3 pos = transform.position;
        Quaternion rot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform sp = spawnPoints[(int)(clientId % (ulong)spawnPoints.Length)];
            pos = sp.position;
            rot = sp.rotation;
        }

        // Kleiner Versatz pro Client, damit Spieler nicht ineinander stecken
        pos += new Vector3(clientId * 1.5f, 3f, 0f); // +3 in der Höhe, damit niemand im Terrain startet

        GameObject p = Instantiate(playerPrefab, pos, rot);
        p.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        Debug.LogWarning($"[Spawner] Spieler für Client {clientId} erfolgreich gespawnt.");
    }
}