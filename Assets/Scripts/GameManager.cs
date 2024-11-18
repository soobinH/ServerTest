using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private GameObject playerPrefab;
    private bool isServer = false;

    void Start()
    {
        // 커맨드 라인 인자를 확인하여 서버/클라이언트 구분
        string[] args = System.Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            if (arg == "-server")
            {
                isServer = true;
                break;
            }
        }

        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();

        if (networkManager != null)
        {
            if (isServer)
            {
                networkManager.StartServer();
            }
            else
            {
                networkManager.StartClient();
            }
            SpawnLocalPlayer();
        }
        else
        {
            Debug.LogError("NetworkManager not found!");
        }
    }

    void SpawnLocalPlayer()
    {
        Vector3 randomPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
        GameObject player = Instantiate(playerPrefab, randomPosition, Quaternion.identity);
        PlayerController controller = player.AddComponent<PlayerController>();
    }
}