using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Fusion;

public class SetupMenuManager : MonoBehaviour
{
    [Header("Scenes Buttons")]
    public Button HostMode;
    public Button ClientMode;

    void Start()
    {
        HostMode.onClick.AddListener(() => {
            ConfigManager.Instance.Mode = GameMode.Host;
            SceneManager.LoadScene("Demo");
        });

        ClientMode.onClick.AddListener(() => {
            ConfigManager.Instance.Mode = GameMode.Client;
            SceneManager.LoadScene("Demo");
        });
    }

    void Update()
    {
    }
}
