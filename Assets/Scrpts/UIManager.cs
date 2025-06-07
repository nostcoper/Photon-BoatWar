using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    
    [Header("Elementos de UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI goalScoreText;

    public GameObject FinalPanel;
    public TextMeshProUGUI Finaltext;
    public Image crow;
    public BoatController controller;

    public Image powerUpIcon;
    public GameManager gameManager;



    void Start()
    {
        controller = GetComponent<BoatController>();
        goalScoreText.text = ConfigManager.Instance.WinPoint.ToString();
        gameManager =  GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {

        if (gameManager.gameEnded){
            FinalPanel.SetActive(true);
            if (controller.winner){
                Finaltext.text = "¡Ganaste!";
            }
        }

        scoreText.text = " " + controller.points;
        crow.enabled =  controller.winner;
    
    if(controller.CurrentPowerUp != null ){
        powerUpIcon.enabled = true;
        powerUpIcon.sprite = controller.CurrentPowerUp.powerUpIcon;
    }
    else
        {
            powerUpIcon.enabled = false; 
        }
    }

}
