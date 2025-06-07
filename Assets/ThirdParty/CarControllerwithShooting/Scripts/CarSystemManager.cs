using UnityEngine;

namespace CarControllerwithShooting
{
    public class CarSystemManager : MonoBehaviour
    {
        public ControllerType controllerType;
        public CameraType cameraType;
        public bool ShowRadar = true;

        public GameObject cameraFPS;
        public GameObject cameraTPS;
        public static CarSystemManager Instance;

        public bool isWeaponsActive = true;

        public void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (controllerType == ControllerType.KeyboardMouse)
            {
                GameCanvas.Instance.Configure_For_PCConsole();
                Cursor.visible = false;
                GameCanvas.Instance.button_HandBrake.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
            }
            else if (controllerType == ControllerType.Mobile)
            {
                GameCanvas.Instance.Configure_For_Mobile();
            }

            if (cameraType == CameraType.Interior_FPS)
            {
                cameraFPS.SetActive(true);
                cameraTPS.SetActive(false);
            }
            else if (cameraType == CameraType.Outdoor_TPS)
            {
                cameraFPS.SetActive(false);
                cameraTPS.SetActive(true);
            }
            if(!isWeaponsActive)
            {
                GunController.Instance.DeactivateWeapons();
            }
        }

        public Transform GetCamera()
        {
            if (cameraType == CameraType.Interior_FPS)
            {
                return cameraFPS.transform;
            }
            else
            {
                return cameraTPS.transform;
            }
        }
    }

    public enum ControllerType
    {
        KeyboardMouse,
        Mobile
    }

    public enum CameraType
    {
        Interior_FPS,
        Outdoor_TPS
    }
}