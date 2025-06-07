using UnityEngine;

namespace CarControllerwithShooting
{
    public class MissileScript : MonoBehaviour
    {
        public GameObject explosionPrefab;
        public int DamagePower = 25;
        public Transform particle_following;
        public bool isEnemyMissile = false;

        private void OnCollisionEnter(Collision collision)
        {
            if (isEnemyMissile)
            {

                if (collision.collider.CompareTag("Car"))
                {
                    CarController.Instance.GetDamage(DamagePower);
                }
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                if (particle_following != null)
                {
                    particle_following.parent = null;
                }

                Destroy(gameObject);
            }
            else
            {
                if (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Enemy") || collision.collider.CompareTag("Natural"))
                {
                    Vector3 explosionPos = transform.position;
                    Collider[] colliders = Physics.OverlapSphere(explosionPos, 5);
                    foreach (Collider hit in colliders)
                    {
                        if (hit.CompareTag("Enemy"))
                        {
                            hit.GetComponent<EnemyAI>().GetDamage(DamagePower);
                        }
                        else if (hit.CompareTag("Natural"))
                        {
                            hit.GetComponent<NaturalAI>().GetDamage(DamagePower);
                        }
                    }
                }
                else if (collision.collider.CompareTag("Collapsable"))
                {
                    Vector3 explosionPos = transform.position;
                    Collider[] colliders = Physics.OverlapSphere(explosionPos, 10);
                    foreach (Collider hit in colliders)
                    {
                        if(hit.CompareTag("Collapsable"))
                        {
                            Rigidbody rb = hit.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                rb.isKinematic = false;
                                rb.useGravity = true;
                                rb.AddExplosionForce(100, explosionPos, 10, 3.0F);
                                Destroy(hit.gameObject, 10);
                            }
                        }
                    }
                }
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                if (particle_following != null)
                {
                    particle_following.parent = null;
                }
                Destroy(gameObject);
            }
        }
    }
}
