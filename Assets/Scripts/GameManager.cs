using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private bool isServer = false;

    void Start()
    {
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

            // 로컬 플레이어 생성
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
        player.AddComponent<PlayerController>();
    }
}