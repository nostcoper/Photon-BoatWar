using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New PowerUp", menuName = "PowerUp/Shooting")]
public class PowerUpShooting : PowerUpBase
{
    public int ammo;
    public GameObject projectilePrefab; 
    private int remainingAmmo;


    public override void Activate(GameObject player)
    {
        if (remainingAmmo == 0){    
            remainingAmmo = ammo;
        }
        Shoot(player.transform.Find("Fire Position"));
    }

    public void Shoot(Transform firePoint)
    {
        if (remainingAmmo > 0)
        {
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Bullet bullet = projectile.GetComponent<Bullet>();
            bullet.owner = firePoint.parent.gameObject;

            remainingAmmo--;
        }
    }

    public override bool IsFinished()
    {
        return remainingAmmo <= 0;
    }
}
