using UnityEngine;

namespace CarControllerwithShooting
{
    public class RadarItem : MonoBehaviour
    {
        public RadarTargetType TargetType;

        void Start()
        {
            if (RadarSystem.Instance != null)
                RadarSystem.Instance.AddTarget(this);
        }
    }
}