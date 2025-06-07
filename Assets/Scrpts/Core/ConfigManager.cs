using UnityEngine;

public class ConfigManager : Singleton<ConfigManager>
{
    [SerializeField] private int playerNumber = 2;
    [SerializeField] private int limitPlayerNumber = 2;
    [SerializeField] private int winPoint = 3;

    public const int MINPLAYERS = 2;
    public const int MAXPLAYERS = 4;
    public const int MINPOINTS = 3;
    public const int MAXPOINTS = 15;

    public int PlayerNumber 
    { 
        get => playerNumber; 
        set => playerNumber = Mathf.Clamp(value, MINPLAYERS, MAXPLAYERS); 
    }

    public int LimitPlayerNumber 
    { 
        get => limitPlayerNumber; 
        set => limitPlayerNumber = Mathf.Clamp(value, MINPLAYERS, MAXPLAYERS); 
    }

    public int WinPoint 
    { 
        get => winPoint; 
        set => winPoint = Mathf.Clamp(value, MINPOINTS, MAXPOINTS); 
    }
}

