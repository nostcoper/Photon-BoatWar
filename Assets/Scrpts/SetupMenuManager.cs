using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SetupMenuManager : MonoBehaviour
{
    [Header("Players Selector")]
    public TMP_Text playersText;
    public Button playersLeftArrow;
    public Button playersRightArrow;

    [Header("Points Selector")]
    public TMP_Text pointsText;
    public Button pointsLeftArrow;
    public Button pointsRightArrow;

    [Header("Scenes Buttons")]
    public Button GameScene;
    public Button MenuScene;

    void Start()
    {
        playersLeftArrow.onClick.AddListener(() => ChangePlayerNumber(-1));
        playersRightArrow.onClick.AddListener(() => ChangePlayerNumber(1));

        pointsLeftArrow.onClick.AddListener(() => ChangePoints(-1));
        pointsRightArrow.onClick.AddListener(() => ChangePoints(1));

        GameScene.onClick.AddListener(() => SceneManager.LoadScene("Movement"));
        MenuScene.onClick.AddListener(() =>  Application.Quit());

        UpdateDisplay(playersText, ConfigManager.Instance.PlayerNumber);
        UpdateDisplay(pointsText, ConfigManager.Instance.WinPoint);
    }

    void Update()
    {
        ConfigManager.Instance.LimitPlayerNumber = DevicesUtils.CountDevices() + 1;
        ConfigManager.Instance.PlayerNumber = Mathf.Clamp(ConfigManager.Instance.PlayerNumber, ConfigManager.MINPLAYERS, ConfigManager.Instance.LimitPlayerNumber);
        UpdateDisplay(playersText, ConfigManager.Instance.PlayerNumber);
    }

    void ChangePlayerNumber(int change)
    {
        ConfigManager.Instance.PlayerNumber = Mathf.Clamp(ConfigManager.Instance.PlayerNumber + change, ConfigManager.MINPLAYERS, ConfigManager.Instance.LimitPlayerNumber);
        UpdateDisplay(playersText, ConfigManager.Instance.PlayerNumber);
    }

    void ChangePoints(int change)
    {
        ConfigManager.Instance.WinPoint = Mathf.Clamp(ConfigManager.Instance.WinPoint + change, ConfigManager.MINPOINTS, ConfigManager.MAXPOINTS);
        UpdateDisplay(pointsText, ConfigManager.Instance.WinPoint);
    }

    void UpdateDisplay(TMP_Text text, int value)
    {
        text.text = value.ToString();
    }
}
