using UnityEngine;
using Fusion;

public class ConfigManager : Singleton<ConfigManager>
{
    [SerializeField] private GameMode _mode = GameMode.AutoHostOrClient;

    public GameMode Mode
    {
        get => _mode;
        set => _mode = value;
    }
}