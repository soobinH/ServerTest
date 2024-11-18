using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private GameObject playerPrefab;
    private bool isServer = false;

    void Start()
    {
        // Ŀ�ǵ� ���� ���ڸ� Ȯ���Ͽ� ����/Ŭ���̾�Ʈ ����
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