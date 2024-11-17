using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private NetworkManager networkManager;
    public string playerId;
    [SerializeField] private float moveSpeed = 5f;

    private float sendRate = 0.1f;
    private float nextSendTime;
    private Vector3 lastSentPosition;
    private bool isInitialized = false;

    void Start()
    {
        InitializeNetworking();
    }

    private void InitializeNetworking()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            playerId = System.Guid.NewGuid().ToString();
            gameObject.name = playerId;
            lastSentPosition = transform.position;
            isInitialized = true;
            SendPositionUpdate();
        }
        else
        {
            Debug.LogError("NetworkManager not found! Make sure it exists in the scene.");
        }
    }

    void Update()
    {
        if (!isInitialized)
        {
            InitializeNetworking();
            return;
        }

        // 플레이어 움직임 처리
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;

        // 주기적으로 위치 정보 전송
        if (Time.time >= nextSendTime)
        {
            if (Vector3.Distance(transform.position, lastSentPosition) > 0.001f)
            {
                SendPositionUpdate();
                lastSentPosition = transform.position;
                nextSendTime = Time.time + sendRate;
            }
        }
    }

    private void SendPositionUpdate()
    {
        if (networkManager != null)
        {
            NetworkManager.PlayerState state = new NetworkManager.PlayerState
            {
                posX = transform.position.x,
                posY = transform.position.y,
                playerId = playerId
            };
            networkManager.SendPlayerState(state);
        }
    }

    public float MoveSpeed
    {
        get { return moveSpeed; }
        set { moveSpeed = value; }
    }
}