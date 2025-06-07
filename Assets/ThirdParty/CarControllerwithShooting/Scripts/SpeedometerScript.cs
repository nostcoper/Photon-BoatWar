using UnityEngine;

namespace CarControllerwithShooting
{
    public class SpeedometerScript : MonoBehaviour
    {
        public float minRotation = 45f;
        public float maxRotation = -225f;

        void FixedUpdate()
        {
            float rotation = Mathf.Lerp(minRotation, maxRotation, CarController.Instance.CurrentSpeed / CarController.Instance.MaxSpeed);
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        }
    }
}
