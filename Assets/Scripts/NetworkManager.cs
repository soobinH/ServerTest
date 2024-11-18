using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    private Socket socket;
    private bool isServer;
    private Thread receiveThread;
    private bool isRunning = true;
    private bool isInitialized = false;
    private List<Socket> clientSockets = new List<Socket>();
    private object clientLock = new object();

    [SerializeField] private int serverPort = 8888;
    [SerializeField] private string serverIP = "127.0.0.1";

    public bool IsServer => isServer;
    public bool IsConnected => isInitialized && socket != null && socket.Connected;

    [Serializable]
    public struct PlayerState
    {
        public float posX;
        public float posY;
        public string playerId;
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void StartServer()
    {
        try
        {
            isServer = true;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, serverPort));
            socket.Listen(5);

            receiveThread = new Thread(ServerReceive);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            isInitialized = true;
            Debug.Log($"Server started on port {serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start server: {e.Message}");
        }
    }

    public void StartClient()
    {
        try
        {
            isServer = false;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(serverIP, serverPort);

            receiveThread = new Thread(ClientReceive);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            isInitialized = true;
            Debug.Log($"Connected to server at {serverIP}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
        }
    }

    private void ServerReceive()
    {
        while (isRunning)
        {
            try
            {
                Socket clientSocket = socket.Accept();
                lock (clientLock)
                {
                    clientSockets.Add(clientSocket);
                }
                Thread clientThread = new Thread(() => HandleClient(clientSocket));
                clientThread.IsBackground = true;
                clientThread.Start();
                Debug.Log("New client connected");
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError($"Server receive error: {e.Message}");
                break;
            }
        }
    }

    private void HandleClient(Socket clientSocket)
    {
        byte[] buffer = new byte[1024];
        while (isRunning)
        {
            try
            {
                int received = clientSocket.Receive(buffer);
                if (received > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, received);
                    BroadcastToClients(message, clientSocket);
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError($"Client handling error: {e.Message}");
                break;
            }
        }

        lock (clientLock)
        {
            clientSockets.Remove(clientSocket);
        }
    }

    private void ClientReceive()
    {
        byte[] buffer = new byte[1024];
        while (isRunning)
        {
            try
            {
                int received = socket.Receive(buffer);
                if (received > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, received);
                    ProcessReceivedMessage(message);
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError($"Client receive error: {e.Message}");
                break;
            }
        }
    }

    private void ProcessReceivedMessage(string message)
    {
        try
        {
            PlayerState state = JsonUtility.FromJson<PlayerState>(message);
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                UpdatePlayerPosition(state);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Message processing error: {e.Message}");
        }
    }

    public void SendPlayerState(PlayerState state)
    {
        if (!isInitialized || socket == null)
        {
            Debug.LogWarning("Cannot send player state: Network not initialized");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(state);
            byte[] data = Encoding.UTF8.GetBytes(json);
            if (isServer)
            {
                BroadcastToClients(json, null);
            }
            else
            {
                socket.Send(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send player state: {e.Message}");
        }
    }

    private void BroadcastToClients(string message, Socket sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (clientLock)
        {
            foreach (Socket client in clientSockets)
            {
                if (client != sender && client.Connected)
                {
                    try
                    {
                        client.Send(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to broadcast to client: {e.Message}");
                    }
                }
            }
        }
    }

    private void UpdatePlayerPosition(PlayerState state)
    {
        GameObject player = GameObject.Find(state.playerId);
        if (player == null)
        {
            // 플레이어가 없으면 생성
            CreateNetworkPlayer(state);
        }
        else
        {
            // 기존 플레이어 위치 업데이트
            player.transform.position = new Vector3(state.posX, state.posY, 0);
        }
    }

    private void CreateNetworkPlayer(PlayerState state)
    {
        // 이미 존재하는 플레이어인지 확인
        GameObject existingPlayer = GameObject.Find(state.playerId);
        if (existingPlayer != null)
        {
            return;
        }

        // 네트워크 플레이어용 큐브 생성
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cube);
        player.name = state.playerId;

        // 로컬 플레이어와 구분하기 위해 다른 색상 지정
        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.red;
        player.GetComponent<Renderer>().material = material;

        player.transform.position = new Vector3(state.posX, state.posY, 0);
    }

    void OnDestroy()
    {
        isRunning = false;
        if (socket != null)
        {
            try
            {
                socket.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error closing socket: {e.Message}");
            }
        }

        lock (clientLock)
        {
            foreach (Socket clientSocket in clientSockets)
            {
                try
                {
                    clientSocket.Close();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error closing client socket: {e.Message}");
                }
            }
            clientSockets.Clear();
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            try
            {
                receiveThread.Join(1000);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error stopping thread: {e.Message}");
            }
        }
    }
}