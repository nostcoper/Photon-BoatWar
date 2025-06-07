using UnityEngine;

namespace CarControllerwithShooting
{
    public class NaturalAI : MonoBehaviour
    {
        public int Health = 100;
        private bool isExploded = false;
        public Collider MainCollider;

        public void GetDamage(int Damage)
        {
            Health = Health - Damage;
            if (Health <= 0 && !isExploded)
            {
                isExploded = true;
                foreach (var meshCollider in GetComponentsInChildren<MeshCollider>())
                {
                    meshCollider.enabled = true;
                }
                // Let's Explode
                foreach (var rigidbody in GetComponentsInChildren<Rigidbody>())
                {
                    rigidbody.useGravity = true;
                    rigidbody.isKinematic = false;
                    rigidbody.AddExplosionForce(Random.Range(4, 15), rigidbody.transform.position, Random.Range(4, 10), Random.Range(1, 2), ForceMode.Impulse);
                    rigidbody.AddRelativeTorque(new Vector3(Random.Range(-4, 4), Random.Range(-4, 4), Random.Range(-4, 4)), ForceMode.Impulse);
                    Destroy(rigidbody.gameObject, 10);
                }


                if (RadarSystem.Instance != null)
                    RadarSystem.Instance.RemoveTarget(gameObject);
                // Let's Destroy itself
                Destroy(gameObject, 6);
            }
        }
    }
}
