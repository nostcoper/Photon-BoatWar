using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

public class GestorRed : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Configuración")]
    public NetworkPrefabRef prefabVehiculo;
    public NetworkPrefabRef[] prefabsAdicionales;
    public int maxIntentosFindCapturaEntrada = 3;

    [Header("Debug")]
    public bool mostrarLogsExtendidos = true;
    public bool inicioAutomatico = true;

    private NetworkRunner _runner;
    private int intentosFindCapturaEntrada = 0;
    private const string NOMBRE_SALA_PREDETERMINADA = "SalaArena";
    private CapturaEntrada inputHandler;
    private bool inputHandlerRegistrado = false;
    private bool ignoreInputsFromGestorRed = true; // Bandera para ignorar inputs

    void Start()
    {
        // Verificar si ya existe un Runner
        _runner = FindObjectOfType<NetworkRunner>();

        if (_runner == null)
        {
            Debug.Log("Creando nuevo NetworkRunner...");
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }
        else
        {
            Debug.Log("NetworkRunner existente encontrado");
            _runner.ProvideInput = true;
        }

        // Iniciar automáticamente
        if (inicioAutomatico)
        {
            IniciarJuego(GameMode.AutoHostOrClient);
        }
    }

    void Update()
    {
        // Verificar estado del runner durante el juego
        if (_runner != null && _runner.IsRunning && Time.frameCount % 300 == 0)
        {
            Debug.Log($"Estado NetworkRunner - IsRunning: {_runner.IsRunning}, IsConnectedToServer: {_runner.IsConnectedToServer}, " +
                      $"LocalPlayer: {_runner.LocalPlayer}, GameMode: {_runner.GameMode}, ProvideInput: {_runner.ProvideInput}");
        }
    }

    public async void IniciarJuego(GameMode modo, string nombreSala = null, bool esPrivado = false)
    {
        try
        {
            Debug.Log($"Iniciando juego en red... Modo: {modo}");

            // Si ya hay un runner activo, detenerlo
            if (_runner.IsRunning)
            {
                Debug.Log("Runner ya está ejecutándose, deteniéndolo...");
                await _runner.Shutdown();
                inputHandlerRegistrado = false; // Resetear la bandera de registro
            }

            // Asegurar que ProvideInput está activado
            _runner.ProvideInput = true;

            // Preparar la escena
            var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(sceneRef);

            // Usar el nombre de sala predeterminado si no se proporciona uno
            if (string.IsNullOrEmpty(nombreSala))
            {
                nombreSala = NOMBRE_SALA_PREDETERMINADA;
            }

            // Parámetros de inicio
            var startGameArgs = new StartGameArgs
            {
                GameMode = modo,
                SessionName = nombreSala,
                Scene = sceneInfo,
                SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>() ??
                              gameObject.AddComponent<NetworkSceneManagerDefault>(),
                SessionProperties = esPrivado ? new Dictionary<string, SessionProperty>() {
                    { "private", true }
                } : null
            };

            // Verificar si tenemos prefabs registrados
            if (!prefabVehiculo.IsValid)
            {
                Debug.LogError("CRÍTICO: El prefab del vehículo no está asignado o no es válido!");
            }

            // Registrar callbacks
            _runner.AddCallbacks(this);

            // Iniciar el juego
            Debug.Log("Iniciando juego...");
            var resultado = await _runner.StartGame(startGameArgs);

            if (resultado.Ok)
            {
                Debug.Log($"Juego en red iniciado correctamente. Sala: {nombreSala}, Modo: {_runner.GameMode}");
                Debug.Log($"LocalPlayer: {_runner.LocalPlayer}, IsServer: {_runner.IsServer}, IsClient: {_runner.IsClient}");

                // Verificar que ProvideInput sigue activado
                if (!_runner.ProvideInput)
                {
                    Debug.LogWarning("ProvideInput se desactivó después de StartGame. Reactivándolo...");
                    _runner.ProvideInput = true;
                }

                // Buscar y configurar el sistema de captura de entrada
                ConfigurarCapturaEntrada();

                // Verificar las asignaciones de prefabs
                VerificarPrefabs();
            }
            else
            {
                Debug.LogError($"Error al iniciar el juego: {resultado.ShutdownReason}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Excepción al iniciar el juego en red: {e.Message}\n{e.StackTrace}");
        }
    }

    private void VerificarPrefabs()
    {
        if (!prefabVehiculo.IsValid)
        {
            Debug.LogError("Prefab de vehículo no es válido. ¡Los jugadores no podrán spawnearse!");
        }
        else
        {
            Debug.Log($"Prefab de vehículo configurado correctamente: {prefabVehiculo}");
        }

        // Verificar prefabs adicionales
        if (prefabsAdicionales != null && prefabsAdicionales.Length > 0)
        {
            for (int i = 0; i < prefabsAdicionales.Length; i++)
            {
                if (prefabsAdicionales[i].IsValid)
                {
                    Debug.Log($"Prefab adicional {i} es válido");
                }
                else
                {
                    Debug.LogWarning($"Prefab adicional {i} no es válido");
                }
            }
        }
    }

    private void ConfigurarCapturaEntrada()
    {
        Debug.Log("Configurando sistema de captura de entradas...");

        // Buscar instancia existente
        inputHandler = FindObjectOfType<CapturaEntrada>();

        if (inputHandler != null)
        {
            Debug.Log("🎮 CapturaEntrada encontrado, configurando callbacks...");

            if (!inputHandlerRegistrado)
            {
                _runner.AddCallbacks(inputHandler);
                inputHandlerRegistrado = true;
                Debug.Log("CapturaEntrada registrado exitosamente como callback");
            }
            else
            {
                Debug.Log("CapturaEntrada ya estaba registrado como callback");
            }

            // Verificar si necesitamos verificar que el runner tiene ProvideInput activado
            if (!_runner.ProvideInput)
            {
                Debug.LogWarning("⚠️ NetworkRunner tiene ProvideInput=false. Activándolo...");
                _runner.ProvideInput = true;
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró CapturaEntrada. Creando nuevo componente...");

            // Verificar si ya intentamos demasiadas veces
            if (intentosFindCapturaEntrada >= maxIntentosFindCapturaEntrada)
            {
                Debug.LogError("❌ Error: No se pudo encontrar o crear CapturaEntrada después de varios intentos.");
                return;
            }

            // Añadir al objeto del GestorRed
            inputHandler = gameObject.AddComponent<CapturaEntrada>();
            if (inputHandler != null)
            {
                _runner.AddCallbacks(inputHandler);
                inputHandlerRegistrado = true;
                Debug.Log("🎮 Se agregó CapturaEntrada como componente nuevo");

                // Asegurar que el runner acepte inputs
                if (!_runner.ProvideInput)
                {
                    Debug.LogWarning("NetworkRunner tiene ProvideInput=false. Activándolo...");
                    _runner.ProvideInput = true;
                }
            }
            else
            {
                Debug.LogError("❌ Error al agregar el componente CapturaEntrada");
                intentosFindCapturaEntrada++;

                // Intentar nuevamente después de un pequeño retraso
                Invoke(nameof(ConfigurarCapturaEntrada), 0.5f);
            }
        }
    }

    private void SpawnVehiculoJugador(NetworkRunner runner, PlayerRef player)
    {
        try
        {
            // Generar posición aleatoria para el spawn
            Vector3 pos = new Vector3(
            Random.Range(-15f, 15f), // Más amplio para evitar colisiones
            1f,
            Random.Range(-15f, 15f)
            );

            // Rotación aleatoria para variar la dirección inicial
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

            Debug.Log($"Spawneando vehículo para {player} en posición {pos} con rotación {rot.eulerAngles}");

            // Spawnear el vehículo con autoridad de input explícita
            var carro = runner.Spawn(prefabVehiculo, pos, rot, player, (runner, obj) => {
                // Callback cuando el objeto se spawne, configurar autoridades
                Debug.Log($"Callback de spawn para {obj.Id}");

                // Verificar autoridades
                bool correctAuthority = obj.InputAuthority == player;
                Debug.Log($"Autoridad de Input correcta: {correctAuthority}, InputAuth={obj.InputAuthority}, Target={player}");
            });

            // Verificar si el spawn fue exitoso
            if (carro != null)
            {
                Debug.Log($"🚗 Spawn exitoso para {player} | InputAuth: {carro.InputAuthority} | StateAuth: {carro.StateAuthority}");

                // Verificar que la autoridad de entrada corresponda al jugador
                if (carro.InputAuthority != player)
                {
                    Debug.LogWarning($"⚠️ Discrepancia de autoridad para {player} - Asignando manualmente");

                    // Intento de corrección manual
                    runner.SetPlayerAlwaysInterested(player, carro, true);

                    // Verificar de nuevo después de la corrección
                    Debug.Log($"Después de corrección: InputAuth={carro.InputAuthority}, StateAuth={carro.StateAuthority}");
                }

                // Verificar componentes esenciales
                var controlVehiculo = carro.GetComponent<ControlVehiculo>();
                if (controlVehiculo == null)
                {
                    Debug.LogError($"❌ Error: El prefab del vehículo no tiene el componente ControlVehiculo!");
                }
                else
                {
                    Debug.Log($"ControlVehiculo encontrado con HasInputAuthority={controlVehiculo.HasInputAuthority}");
                }
            }
            else
            {
                Debug.LogError($"❌ Error al hacer spawn del vehículo para el jugador {player}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Excepción al spawnear vehículo: {e.Message}\n{e.StackTrace}");
        }
    }

    // Todos los métodos de INetworkRunnerCallbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Fusion] Player joined: {player}, LocalPlayer: {runner.LocalPlayer}, IsServer: {runner.IsServer}");

        if (runner.IsServer)
        {
            SpawnVehiculoJugador(runner, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Jugador {player} abandonó la sesión");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Simplemente ignorar este callback
        if (ignoreInputsFromGestorRed)
        {
            return; // No hacer nada en este callback para evitar interferencias
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // También ignoramos este callback
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("✅ Cliente conectado al servidor");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"❌ Conexión fallida: {reason}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"❌ Desconectado del servidor: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Network runner shutdown: {shutdownReason}");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"Lista de sesiones actualizada. Sesiones encontradas: {sessionList.Count}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    // Función para facilitar depuración - permite forzar la creación de un nuevo juego
    public void ReiniciarJuego()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
            inputHandlerRegistrado = false;
        }

        Invoke(nameof(IniciarJuego), 1f);
    }
}