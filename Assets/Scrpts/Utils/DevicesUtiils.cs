using UnityEngine;
using UnityEngine.InputSystem;

public static class DevicesUtils
{
    public static int CountDevices()
    {
        int count = 0;
        foreach (var device in InputSystem.devices)
        {   
            if (device is not Mouse)
                count++;
        }
        return count;
    }
}
