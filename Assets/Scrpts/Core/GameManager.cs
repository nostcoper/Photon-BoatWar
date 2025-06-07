using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CarControllerwithShooting;
using UnityEngine.InputSystem;
using UnityPlayerInput = UnityEngine.InputSystem.PlayerInput;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Configutation Game")]
    public int numberPlayer;
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Lista de materiales posibles")]
    public Material[] materialsAvailable;

    public int pointWin;

    [Header("Components")]
    public List<GameObject> playerList;
    public List<Camera> playerCameras = new List<Camera>();

    public bool gameEnded  { get; private set; }= false;

    private void OnEnable() {
        numberPlayer =  ConfigManager.Instance.PlayerNumber;    
        pointWin = ConfigManager.Instance.WinPoint;  
    }

    void Start()
    {
        for (int i = 0; i < numberPlayer; i++)
        {
            addPlayer(i);
        }
        AdjustCameraView(playerList.Count);
    }

    void addPlayer(int id)
    {
        UnityPlayerInput currentPlayer;
        
        if (id < 2)
        {
            string controlScheme = id == 0 ? "WASD" : "Arrows";
            string actionMap = controlScheme;

            currentPlayer = UnityPlayerInput.Instantiate(
                playerPrefab,
                controlScheme: controlScheme,
                pairWithDevice: Keyboard.current
            );
            
            currentPlayer.SwitchCurrentControlScheme(controlScheme, Keyboard.current);
            currentPlayer.SwitchCurrentActionMap(actionMap);
        }
        // Para el resto de jugadores usamos gamepads disponibles
        else
        {
            // Verificamos si hay suficientes gamepads disponibles
            if (Gamepad.all.Count >= id - 1)
            {
                currentPlayer = UnityPlayerInput.Instantiate(
                    playerPrefab,
                    controlScheme: "Gamepad",
                    pairWithDevice: Gamepad.all[id - 2]  // Restamos 2 porque los dos primeros usan teclado
                );
                
                currentPlayer.SwitchCurrentControlScheme("Gamepad", Gamepad.all[id - 2]);
                currentPlayer.SwitchCurrentActionMap("Gamepad");
            }
            else
            {
                Debug.LogError($"No hay suficientes gamepads conectados para el jugador {id + 1}");
                return;
            }
        }
        
        // Configuración común para todos los jugadores
        currentPlayer.transform.position = spawnPoints[playerList.Count].position;
        currentPlayer.transform.rotation = spawnPoints[playerList.Count].rotation;
        playerCameras.Add(currentPlayer.transform.Find("Camera").GetComponent<Camera>());
        currentPlayer.name = $"Player {playerList.Count + 1}";
 
        BoatController boatController = currentPlayer.GetComponent<BoatController>();
        SkinnedMeshRenderer boatMaterial = boatController.flagMesh.GetComponent<SkinnedMeshRenderer>();
        boatMaterial.material = materialsAvailable[id];

        playerList.Add(currentPlayer.gameObject);
    }
    

    private void AdjustCameraView(int playerCount)
    {
        switch (playerCount)
        {
            case 2:
                playerCameras[0].rect = new Rect(0f, 0f, 0.5f, 1);
                playerCameras[1].rect = new Rect(0.5f, 0, 0.5f, 1);
                break;
                
            case 3:
                playerCameras[0].rect = new Rect(0f, 0.5f, 0.5f, 0.5f); // Arriba izquierda
                playerCameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f); // Arriba derecha
                playerCameras[2].rect = new Rect(0f, 0f, 0.5f, 0.5f); // Abajo izquierda
                break;
            case 4:
                playerCameras[0].rect = new Rect(0f, 0.5f, 0.5f, 0.5f); // Arriba izquierda
                playerCameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f); // Arriba derecha
                playerCameras[2].rect = new Rect(0f, 0f, 0.5f, 0.5f); // Abajo izquierda
                playerCameras[3].rect = new Rect(0.5f, 0f, 0.5f, 0.5f); // Abajo derecha
                break;
        }
    } 

void Update()
{
    int highestPoints = -1;

    // Buscar el puntaje más alto
    foreach (GameObject player in playerList)
    {
        BoatController controller = player.GetComponent<BoatController>();
        if (controller != null && controller.points > highestPoints)
        {
            highestPoints = controller.points;
        }
    }

    // Marcar como ganadores a todos los jugadores con el puntaje más alto
    foreach (GameObject player in playerList)
    {
        BoatController controller = player.GetComponent<BoatController>();
        if (controller != null)
        {
            controller.winner = (controller.points == highestPoints);
        }
    }

    // Verificar si alguno de los ganadores alcanza el puntaje objetivo
    if (!gameEnded && highestPoints >= pointWin)
    {
        gameEnded = true;
        Debug.Log("Juego terminado. Jugadores con " + highestPoints + " puntos han ganado.");
        StartCoroutine(FinishGame());
    }
}

    private IEnumerator FinishGame()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("Menu Setup");
    }
}
