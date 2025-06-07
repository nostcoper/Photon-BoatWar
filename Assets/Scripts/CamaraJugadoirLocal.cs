using UnityEngine;
using Fusion;

public class CamaraJugadorLocal : MonoBehaviour
{
    private Camera camara;

    void Start()
    {
        camara = GetComponentInChildren<Camera>(true); // Incluye cámaras desactivadas

        if (camara == null)
        {
            Debug.LogWarning("📷 No se encontró cámara como hijo del objeto");
            return;
        }

        StartCoroutine(AsignarCamaraJugadorLocal());
    }

    private System.Collections.IEnumerator AsignarCamaraJugadorLocal()
    {
        int intentos = 0;
        while (intentos < 10)
        {
            var vehiculos = FindObjectsOfType<ControlVehiculo>();
            foreach (var vehiculo in vehiculos)
            {
                if (vehiculo.HasInputAuthority)
                {
                    // Solo activar esta cámara si este objeto pertenece al mismo jugador
                    if (vehiculo.transform.IsChildOf(transform) || transform.IsChildOf(vehiculo.transform))
                    {
                        camara.gameObject.SetActive(true);
                        Debug.Log("🎥 Cámara activada para jugador local");
                    }
                    else
                    {
                        camara.gameObject.SetActive(false);
                    }

                    yield break; // Salir del coroutine una vez hecho
                }
            }

            intentos++;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogWarning("No se pudo encontrar el vehículo local después de varios intentos");
    }
}
