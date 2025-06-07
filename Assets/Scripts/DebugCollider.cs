using UnityEngine;

public class DebugCollider : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Suelo: Colisión con {collision.gameObject.name}, velocidad: {collision.relativeVelocity.magnitude}");
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.frameCount % 100 == 0) // Reducir spam de logs
        {
            Debug.Log($"Suelo: Contacto continuo con {collision.gameObject.name}");
        }
    }
}