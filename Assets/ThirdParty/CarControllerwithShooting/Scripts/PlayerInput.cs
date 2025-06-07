using UnityEngine;

namespace CarControllerwithShooting
{
    public class PlayerInput : MonoBehaviour
    {
        private CarController _carController;

        private void Awake()
        {
            _carController = GetComponent<CarController>();
        }

        private void FixedUpdate()
        {
            _carController.Move();
        }
    }
}