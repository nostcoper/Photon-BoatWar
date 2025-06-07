using UnityEngine;

public abstract class PowerUpBase : ScriptableObject
{
    public string Name;
    public Sprite powerUpIcon;
    public float probability; 
    public abstract void Activate(GameObject emisor);
    public abstract bool IsFinished();
}