using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

// Auf ein Objekt in der MENÜ-Szene legen (dort, wo auch der einzige NetworkManager liegt).
// Host: nur PC/Editor. Client: Browser (WebGL) und alle anderen.
public class MultiplayerSceneLoader : MonoBehaviour
{
    [Header("Szene")]
    public string gameSceneName = "Scene 1";

    [Header("Verbindung")]
    [Tooltip("Adresse des Hosts, zu der sich der Client verbindet")]
    public string serverAddress = "127.0.0.1";
    public ushort port = 7777;

    [Tooltip("Muss auf Host UND Client gleich sein. Für WebGL-Clients: an.")]
    public bool useWebSockets = true;

    // =========================================================
    // SINGLEPLAYER (funktioniert überall, auch WebGL)
    // =========================================================

    // Startet KEIN Netzwerk, sondern lädt die Szene ganz normal.
    // Dadurch läuft movement_player über den "kein NetworkObject"-Pfad
    // (InitLocalPlayer() direkt in Start()).
    public void StartSingleplayerAndLoadScene1()
    {
        Debug.Log("Singleplayer gestartet – lade " + gameSceneName + " (ohne Netzwerk)...");

        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    // =========================================================
    // HOST (nur PC / Editor)
    // =========================================================

    public void StartHostAndLoadScene1()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogError("Ein WebGL-Build kann nicht Host sein. Bitte als Client beitreten.");
        return;
#else
        var nm = NetworkManager.Singleton;

        if (nm == null)
        {
            Debug.LogError("NetworkManager wurde nicht gefunden!");
            return;
        }

        if (nm.IsListening)
        {
            Debug.LogWarning("Netzwerk läuft bereits.");
            return;
        }

        ConfigureTransport(listenOnAllInterfaces: true);

        if (!nm.StartHost())
        {
            Debug.LogError("Host konnte nicht gestartet werden!");
            return;
        }

        Debug.Log("Host gestartet – lade " + gameSceneName + "...");

        var status = nm.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
            Debug.LogError("Szene konnte nicht geladen werden: " + status +
                           " (Name in den Build Settings prüfen)");
#endif
    }

    // =========================================================
    // CLIENT (Browser und alle anderen)
    // =========================================================

    public void StartClientAndJoin()
    {
        var nm = NetworkManager.Singleton;

        if (nm == null)
        {
            Debug.LogError("NetworkManager wurde nicht gefunden!");
            return;
        }

        if (nm.IsListening)
        {
            Debug.LogWarning("Netzwerk läuft bereits.");
            return;
        }

        ConfigureTransport(listenOnAllInterfaces: false);

        if (!nm.StartClient())
        {
            Debug.LogError("Client konnte nicht gestartet werden!");
            return;
        }

        // Die Szene lädt der Server; "Enable Scene Management" im NetworkManager
        // sorgt dafür, dass der Client automatisch nachzieht.
        Debug.Log("Client verbindet zu " + serverAddress + ":" + port);
    }

    // Für ein UI-Eingabefeld (IP/Adresse des Hosts)
    public void SetServerAddress(string address)
    {
        serverAddress = address.Trim();
    }

    // =========================================================
    // TRANSPORT
    // =========================================================

    private void ConfigureTransport(bool listenOnAllInterfaces)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (utp == null)
        {
            Debug.LogWarning("Kein UnityTransport am NetworkManager gefunden.");
            return;
        }

        utp.UseWebSockets = useWebSockets;

        if (listenOnAllInterfaces)
            utp.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        else
            utp.SetConnectionData(serverAddress, port);
    }
}