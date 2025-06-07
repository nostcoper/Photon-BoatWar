using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

public class CapturaEntrada : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Configuración de Controles")]
    public KeyCode teclaDisparo = KeyCode.Space;

    private Camera camaraJugador;
    private bool camaraInicializada = false;
    private Transform vehiculoLocalTransform;
    private bool inputsConfigurados = false;
    private NetworkRunner activeRunner;

    void Start()
    {
        // Buscar NetworkRunner activo al iniciar
        var runners = FindObjectsOfType<NetworkRunner>();
        if (runners.Length > 0)
        {
            Debug.Log($"Se encontraron {runners.Length} NetworkRunner(s). Usando el primero.");
            activeRunner = runners[0];
            activeRunner.AddCallbacks(this);
            inputsConfigurados = true;
        }
        else
        {
            Debug.LogError("No se encontró ningún NetworkRunner. Los inputs no funcionarán.");
        }
    }

    void Update()
    {
        // Verificar la cámara
        if (Camera.main != null && !camaraInicializada)
        {
            camaraJugador = Camera.main;
            camaraInicializada = true;
            Debug.Log("Cámara principal encontrada y asignada");
        }

        if (camaraJugador == null)
        {
            camaraJugador = Camera.main;
            camaraInicializada = camaraJugador != null;
        }

        // Buscar el vehículo local
        if (vehiculoLocalTransform == null)
        {
            var vehiculos = FindObjectsOfType<ControlVehiculo>();

            Debug.Log($"Buscando vehículo local entre {vehiculos.Length} vehículos");

            foreach (var v in vehiculos)
            {
                if (v.HasInputAuthority)
                {
                    vehiculoLocalTransform = v.transform;
                    Debug.Log($"Vehículo local encontrado: {v.gameObject.name} con ID {v.Object.Id}");
                    break;
                }
            }

            if (vehiculoLocalTransform == null && vehiculos.Length > 0)
            {
                Debug.LogWarning("No se encontró vehículo con InputAuthority. Es posible que haya problemas con las autoridades.");
            }
        }

        // Verificar runner
        if (!inputsConfigurados)
        {
            var runners = FindObjectsOfType<NetworkRunner>();
            if (runners.Length > 0)
            {
                Debug.Log("Configurando NetworkRunner que no se encontró en Start()");
                activeRunner = runners[0];
                activeRunner.AddCallbacks(this);
                inputsConfigurados = true;
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Verificar si es el runner activo
        if (runner != activeRunner)
        {
            Debug.LogWarning("OnInput llamado con un runner diferente al activo");
            activeRunner = runner; // Actualizar el runner activo
        }

        // Capturar inputs
        var datos = new DatosEntrada
        {
            Aceleracion = Input.GetAxis("Vertical"),
            Direccion = Input.GetAxis("Horizontal"),
            Disparar = Input.GetKey(teclaDisparo)
        };

        // Mostrar información detallada sobre los inputs
        if (datos.Aceleracion != 0 || datos.Direccion != 0 || datos.Disparar)
        {
            if (Time.frameCount % 60 == 0 || datos.Disparar)
            {
                Debug.Log($"Enviando input - Acel: {datos.Aceleracion:F2}, Dir: {datos.Direccion:F2}, Disparo: {datos.Disparar}");
            }
        }

        // Calcular dirección de disparo
        if (vehiculoLocalTransform != null)
        {
            datos.PuntoMira = vehiculoLocalTransform.forward;
        }
        else if (camaraJugador != null)
        {
            datos.PuntoMira = camaraJugador.transform.forward;
        }
        else
        {
            datos.PuntoMira = Vector3.forward;
        }

        // Enviar los datos al runner
        input.Set(datos);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Jugador {player} se unió al juego");

        // Verificar si somos el jugador local
        if (runner.LocalPlayer == player)
        {
            Debug.Log("¡Somos el jugador local! Configurando...");
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("🎮 Conectado al servidor exitosamente");
    }

    // Implementación de los callbacks requeridos por la interfaz
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Conexión fallida: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"Desconectado del servidor: {reason}");
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        Debug.LogWarning($"Inputs perdidos para el jugador {player}");
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Jugador {player} abandonó el juego");
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"NetworkRunner apagado: {shutdownReason}");
        inputsConfigurados = false;
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}