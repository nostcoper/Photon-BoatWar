using UnityEngine;
using Fusion;

// Proyectil simplificado sin física compleja
public class ProyectilBasico : NetworkBehaviour
{
    [Header("Configuración del Proyectil")]
    public float velocidad = 20f;
    public float dano = 25f;
    public float tiempoVida = 5f;

    [Networked] public PlayerRef Propietario { get; set; }
    [Networked] public TickTimer TiempoDestruir { get; set; }

    private Collider proyectilCollider;
    private bool haImpactado = false;

    public override void Spawned()
    {
        proyectilCollider = GetComponent<Collider>();

        // Configurar tiempo de vida
        if (HasStateAuthority)
        {
            TiempoDestruir = TickTimer.CreateFromSeconds(Runner, tiempoVida);
        }

        Debug.Log($"🚀 Proyectil spawneado por {Propietario}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Destruir si se acabó el tiempo
        if (TiempoDestruir.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // Mover el proyectil hacia adelante (sin usar Rigidbody)
        transform.position += transform.forward * velocidad * Runner.DeltaTime;
    }

    public void Inicializar(PlayerRef propietario)
    {
        Propietario = propietario;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority || haImpactado) return;

        haImpactado = true;

        // Buscar si colisionó con un vehículo
        var vehiculo = other.GetComponentInParent<ControlVehiculo>();

        // No dañar al propietario
        if (vehiculo != null && vehiculo.Object.InputAuthority != Propietario)
        {
            // Aplicar daño
            vehiculo.RecibirDanoRpc(dano, Propietario);

            Debug.Log($"💥 Proyectil impactó a vehículo de {vehiculo.Object.InputAuthority}");
        }

        // Siempre destruir el proyectil al impactar con cualquier cosa
        Runner.Despawn(Object);
    }
}