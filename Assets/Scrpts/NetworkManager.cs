using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    // Essential callbacks that need implementation
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        
        // Capture basic input - modify as needed for your game
        if (Input.GetKey(KeyCode.W))
            data.buttons |= InputButtons.Forward;
        if (Input.GetKey(KeyCode.S))
            data.buttons |= InputButtons.Backward;
        if (Input.GetKey(KeyCode.A))
            data.buttons |= InputButtons.Left;
        if (Input.GetKey(KeyCode.D))
            data.buttons |= InputButtons.Right;
        if (Input.GetKey(KeyCode.Space))
            data.buttons |= InputButtons.Jump;
        
        // Set mouse/camera input
        data.direction = Camera.main.transform.forward;
        
        input.Set(data);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} joined the game");
        
        // Spawn player object if this is the local player
        if (player == runner.LocalPlayer)
        {
            // Example: Spawn player at origin
            // runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} left the game");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("Scene load started");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("Scene load completed");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Network runner shutdown: {shutdownReason}");
    }

    // Optional callbacks - can be left empty for basic functionality
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connection failed: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // Accept all connection requests by default
        request.Accept();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // Handle custom authentication if needed
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("Host migration occurred");
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // Handle missing input if needed
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Handle object entering Area of Interest
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Handle object exiting Area of Interest
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // Handle reliable data transfer progress
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // Handle reliable data received
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // Handle session list updates
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        // Handle user simulation messages
    }

    private async void Start()
    {
        // Create or find the NetworkRunner
        _runner = FindAnyObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.Log("Creating new NetworkRunner...");
            _runner = gameObject.AddComponent<NetworkRunner>();
        }

        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        // Add the SceneManager to handle scene loading
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(sceneRef);
        Debug.Log("Starting game with StartGame...");

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "SalaPrueba",
            Scene = sceneInfo,
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            Debug.Log("✅ StartGame completed successfully.");
        }
        else
        {
            Debug.LogError($"❌ StartGame failed: {result.ShutdownReason}");
        }
    }

    private void Update()
    {
        if (_runner == null)
        {
            Debug.Log("Runner not created yet");
        }
        else if (!_runner.IsRunning)
        {
            Debug.Log("Runner created but not running yet");
        }
        else if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Runner OK] IsRunning: {_runner.IsRunning}, Connected: {_runner.IsConnectedToServer}, " +
                      $"LocalPlayer: {_runner.LocalPlayer}, Mode: {_runner.GameMode}, Input: {_runner.ProvideInput}");
        }
    }
}

// Input data structure for network input

public struct NetworkInputData : INetworkInput
{
    public InputButtons buttons;
    public Vector3 direction;
}

[System.Flags]
public enum InputButtons
{
    None = 0,
    Forward = 1 << 0,
    Backward = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Jump = 1 << 4,
}