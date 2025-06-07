using UnityEngine;

namespace CarControllerwithShooting
{
    public class CollactableScript : MonoBehaviour
    {
        public CollactableType collactableType;
        public int Amount;
        public AudioClip audioClip;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Car"))
            {
                if (collactableType == CollactableType.Gasoline)
                {
                    Gasoline.Instance.Add_Gassoline(Amount, audioClip);
                }
                else if (collactableType == CollactableType.AmmoMachinegun)
                {
                    GunController.Instance.Add_Ammo_MachineGun(Amount, audioClip);
                }
                else if (collactableType == CollactableType.AmmoMissile)
                {
                    GunController.Instance.Add_Ammo_Missile(Amount, audioClip);
                }
                Destroy(gameObject);
            }
        }
    }

    public enum CollactableType
    {
        Gasoline,
        AmmoMachinegun,
        AmmoMissile
    }
}