using UnityEngine;
using Fusion;
using System.Collections;

public struct DatosEntrada : INetworkInput
{
    public float Aceleracion;
    public float Direccion;
    public bool Disparar;
    public Vector3 PuntoMira;
}

public class ControlVehiculo : NetworkBehaviour
{
    [Header("Configuración del Movimiento")]
    public float accelerateSpeed = 500f; // Velocidad de aceleración (equivale a velocidad)
    public float turnSpeed = 100f; // Velocidad de giro (equivale a rotacion)
    public float brakeForce = 0.1f; // Fuerza de frenado automático
    public float tiempoEntreLogMovimiento = 1f;

    [Header("Sistema de Vida")]
    [Networked] public float Vida { get; set; } = 100f;
    [Networked] public bool EstaMuerto { get; set; } = false;
    [Networked] public int Kills { get; set; } = 0;
    [Networked] public int Muertes { get; set; } = 0;

    [Header("Sistema de Disparo")]
    public NetworkPrefabRef prefabProyectil;
    public Transform puntoDisparo;
    public float tiempoEntreDisparo = 0.5f;
    [Networked] public TickTimer CooldownDisparo { get; set; }

    [Header("Efectos")]
    public GameObject efectoMuerte;
    public GameObject efectoDisparo;

    [Header("Configuración Física")]
    public float alturaFija = 0.5f;
    public LayerMask capasSuelo;
    public float fuerzaMovimiento = 100f;

    private Rigidbody rb;
    private NetworkTransform networkTransform;
    private Renderer vehicleRenderer;
    private float ultimoLogMovimiento;
    private Vector3 ultimaPosicion;
    private PlayerRef ultimoAtacante;
    private Collider vehicleCollider;
    private bool usoModoForzado = true;
    private bool inicializado = false;
    [Networked] private NetworkBool DisparoSolicitado { get; set; }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        networkTransform = GetComponent<NetworkTransform>();
        vehicleCollider = GetComponent<Collider>();
        vehicleRenderer = GetComponent<Renderer>();
        ultimaPosicion = transform.position;
        ultimoLogMovimiento = Time.time;

        Debug.Log($"[ControlVehiculo] Spawned. InputAuthority: {Object.InputAuthority}, HasInputAuthority: {HasInputAuthority}, StateAuthority: {Object.StateAuthority}, HasStateAuthority: {HasStateAuthority}");

        // Verificar y configurar NetworkTransform
        if (networkTransform == null)
        {
            Debug.LogWarning("NetworkTransform no encontrado. Añadiendo uno nuevo.");
            networkTransform = gameObject.AddComponent<NetworkTransform>();
        }

        // Configurar Rigidbody
        if (rb != null)
        {
            rb.isKinematic = false;
            // CORREGIDO: Permitir movimiento en Y para evitar problemas de física
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            rb.mass = 1f;
            rb.linearDamping = 2f; // Aumentado para control
            rb.angularDamping = 2f; // Aumentado para rotación controlada

            // CORREGIDO: Mantener gravedad activada
            rb.useGravity = false;
        }
        else
        {
            Debug.LogError("¡CRÍTICO! Rigidbody no encontrado en el vehículo. Agregando uno...");
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        // Configurar collider
        if (vehicleCollider != null)
        {
            vehicleCollider.enabled = true;
            if (vehicleCollider is BoxCollider boxCollider)
            {
                boxCollider.isTrigger = false;
                boxCollider.size = new Vector3(boxCollider.size.x, boxCollider.size.y + 0.1f, boxCollider.size.z);
            }
        }
        else
        {
            Debug.LogError("¡CRÍTICO! Collider no encontrado en el vehículo. Agregando uno...");
            BoxCollider newCollider = gameObject.AddComponent<BoxCollider>();
            newCollider.size = new Vector3(2f, 1f, 3f);
            vehicleCollider = newCollider;
        }

        // Configurar vida inicial
        if (HasStateAuthority)
        {
            Vida = 100f;
            EstaMuerto = false;
            Kills = 0;
            Muertes = 0;
        }

        // Configurar apariencia del vehículo
        if (vehicleRenderer != null)
            vehicleRenderer.material.color = HasInputAuthority ? Color.green : Color.gray;

        // Configurar cámara para el jugador local
        if (HasInputAuthority && Camera.main != null)
        {
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 5, -8);
            Camera.main.transform.localRotation = Quaternion.Euler(20, 0, 0);
        }

        // Verificar que hay un punto de disparo
        if (puntoDisparo == null)
        {
            GameObject puntoDisparoGO = new GameObject("PuntoDisparo");
            puntoDisparoGO.transform.SetParent(transform);
            puntoDisparoGO.transform.localPosition = new Vector3(0, 1f, 2f);
            puntoDisparo = puntoDisparoGO.transform;
        }

        // CORREGIDO: Configuración inicial simplificada
        if (HasStateAuthority)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Verificar las capas de colisión
        if (capasSuelo.value == 0)
        {
            Debug.LogWarning("No se han configurado capas para el suelo. Usando Default Layer.");
            capasSuelo = 1 << 0; // Layer Default
        }

        inicializado = true;

        // CORREGIDO: Configurar valores de movimiento aquí
        accelerateSpeed = 5000f;
        turnSpeed = 1000f;
        brakeForce = 0.1f;
    }

    public override void FixedUpdateNetwork()
    {
        if (!inicializado || EstaMuerto) return;

        // CORREGIDO: Simplificar el control de altura
        if (usoModoForzado && HasStateAuthority)
        {
            // Solo controlar altura si está muy lejos del suelo
            if (transform.position.y < alturaFija - 1f || transform.position.y > alturaFija + 2f)
            {
                Vector3 pos = transform.position;
                pos.y = alturaFija;
                transform.position = pos;
            }
        }

        // Obtener inputs
        if (GetInput<DatosEntrada>(out var input))
        {
            // CORREGIDO: Agregar debug para verificar inputs
            if (Mathf.Abs(input.Aceleracion) > 0.1f || Mathf.Abs(input.Direccion) > 0.1f)
            {
                Debug.Log($"[INPUT] Aceleración: {input.Aceleracion}, Dirección: {input.Direccion}");
            }

            // Sistema de disparo
            if (input.Disparar && CooldownDisparo.ExpiredOrNotRunning(Runner))
            {
                DisparoSolicitado = true;
                CooldownDisparo = TickTimer.CreateFromSeconds(Runner, tiempoEntreDisparo);
            }
            
            if (HasStateAuthority)
            {
                if (DisparoSolicitado)
                {
                    Vector3 dir = puntoDisparo != null ? puntoDisparo.forward : transform.forward;
                    Vector3 pos = puntoDisparo != null ? puntoDisparo.position : transform.position + transform.forward * 2f;
                    DispararProyectil(dir, pos);
                    DisparoSolicitado = false;
                }
                
                // CORREGIDO: Mover el vehículo siempre que haya input
                MoverVehiculo(input.Aceleracion, input.Direccion);
            }
        }

        // Verificar si el vehículo cayó del mundo
        if (transform.position.y < -10f && HasStateAuthority)
        {
            Debug.LogWarning($"Vehículo {Object.InputAuthority} cayó del mundo - Respawneando");
            transform.position = new Vector3(UnityEngine.Random.Range(-10f, 10f), alturaFija, UnityEngine.Random.Range(-10f, 10f));
            transform.rotation = Quaternion.identity;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            RecibirDanoRpc(25f, PlayerRef.None);
        }
    }

    // CORREGIDO: Método de movimiento simplificado y funcional
    private void MoverVehiculo(float aceleracion, float direccion)
    {
        if (rb == null) return;

        // ROTACIÓN (Torque en Y) - Simula el timón del barco
        if (Mathf.Abs(direccion) > 0.01f)
        {
            rb.AddTorque(0f, direccion * turnSpeed * Runner.DeltaTime, 0f);
            Debug.Log($"[ROTACIÓN] Aplicando torque: {direccion * turnSpeed * Runner.DeltaTime}");
        }

        // MOVIMIENTO HACIA ADELANTE/ATRÁS - Simula los motores del barco
        if (Mathf.Abs(aceleracion) > 0.01f)
        {
            rb.AddForce(transform.forward * aceleracion * accelerateSpeed * Runner.DeltaTime);
            Debug.Log($"[MOVIMIENTO] Aplicando fuerza: {aceleracion * accelerateSpeed * Runner.DeltaTime}, Velocidad actual: {rb.linearVelocity.magnitude}");
        }
        else
        {
            // FRENADO AUTOMÁTICO - Simula la resistencia del agua
            Vector3 horizontalVelocity = rb.linearVelocity;
            horizontalVelocity.y = 0; // Mantener velocidad vertical intacta

            Vector3 fuerzaFrenado = -horizontalVelocity * brakeForce;
            rb.AddForce(fuerzaFrenado, ForceMode.Acceleration);
            
            if (horizontalVelocity.magnitude > 0.1f)
            {
                Debug.Log($"[FRENADO] Aplicando resistencia: {fuerzaFrenado.magnitude:F2}");
            }
        }

        // Log de movimiento
        if ((Mathf.Abs(aceleracion) > 0.1f || Mathf.Abs(direccion) > 0.1f) &&
            Time.time - ultimoLogMovimiento > tiempoEntreLogMovimiento)
        {
            float distanciaRecorrida = Vector3.Distance(transform.position, ultimaPosicion);
            Debug.Log($"🚗 Vehículo {Object.Id} - Distancia: {distanciaRecorrida:F2}m, Velocidad: {rb.linearVelocity.magnitude:F2}m/s");
            ultimaPosicion = transform.position;
            ultimoLogMovimiento = Time.time;
        }
    }

    // ELIMINADO: El método Start() que sobreescribía los valores
    // private void Start() { ... }

    private void DispararProyectil(Vector3 direccion, Vector3 posicion)
    {
        if (!prefabProyectil.IsValid || !HasStateAuthority) return;

        if (efectoDisparo != null)
        {
            var efecto = Instantiate(efectoDisparo, posicion, Quaternion.LookRotation(direccion));
            Destroy(efecto, 2f);
        }

        Runner.Spawn(prefabProyectil, posicion, Quaternion.LookRotation(direccion), Object.InputAuthority, (runner, obj) => {
            var c = obj.GetComponent<ControlProyectil>();
            if (c != null)
            {
                c.Propietario = Object.InputAuthority;
                c.Direccion = direccion.normalized;
                c.PosicionInicial = posicion;
                Debug.Log($"Inicializando proyectil - Propietario: {c.Propietario}, Dirección: {c.Direccion}, Posición: {c.PosicionInicial}");
            }
            else
            {
                Debug.LogError("¡El prefab del proyectil no tiene el componente ControlProyectil!");
            }
        });

        MostrarEfectoDisparoRpc();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void MostrarEfectoDisparoRpc()
    {
        if (efectoDisparo != null)
        {
            Vector3 posEfecto = puntoDisparo != null ? puntoDisparo.position : transform.position + transform.forward * 2f;
            var efecto = Instantiate(efectoDisparo, posEfecto, transform.rotation);
            Destroy(efecto, 2f);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RecibirDanoRpc(float cantidad, PlayerRef atacante)
    {
        Debug.Log($"[RPC-DAÑO] {Object.InputAuthority} recibiendo llamada de daño por {cantidad} de {atacante}");

        if (HasStateAuthority && !EstaMuerto)
        {
            float vidaAnterior = Vida;
            Vida = Mathf.Max(0, Vida - cantidad);

            Debug.Log($"[RPC-DAÑO] Vida anterior: {vidaAnterior}, Vida actual: {Vida}, Daño: {cantidad}");

            if (vidaAnterior != Vida)
            {
                Debug.Log($"[RPC-DAÑO] 💥 {Object.InputAuthority} recibió {cantidad} de daño. Vida restante: {Vida}");

                if (atacante != PlayerRef.None)
                {
                    ultimoAtacante = atacante;
                }

                CambiarColorDanoRpc();

                if (Vida <= 0)
                {
                    Debug.Log($"[RPC-DAÑO] ☠️ {Object.InputAuthority} ELIMINADO por {ultimoAtacante}");
                    Morir(ultimoAtacante);
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void CambiarColorDanoRpc()
    {
        if (vehicleRenderer != null)
        {
            StartCoroutine(EfectoDano());
        }
    }

    private System.Collections.IEnumerator EfectoDano()
    {
        if (vehicleRenderer == null) yield break;

        Color colorOriginal = vehicleRenderer.material.color;
        vehicleRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);

        if (vehicleRenderer != null)
            vehicleRenderer.material.color = colorOriginal;
    }

    private void Morir(PlayerRef atacante)
    {
        if (HasStateAuthority)
        {
            Debug.Log($"[MUERTE] Vehículo {Object.InputAuthority} muriendo. HasStateAuthority: {HasStateAuthority}");
            Muertes++;

            if (atacante != PlayerRef.None)
            {
                var atacanteObj = FindVehiculoPorInputAuthority(atacante);
                if (atacanteObj != null)
                {
                    atacanteObj.Kills++;
                    Debug.Log($"[KILL] {atacante} obtiene un punto de kill: {atacanteObj.Kills}");
                }
            }

            MorirRpc(atacante);
            Debug.Log($"[RESPAWN] Programando respawn de {Object.InputAuthority} en 3 segundos");
            Runner.StartCoroutine(RespawnDespuesDeTiempo());
        }
    }

    private ControlVehiculo FindVehiculoPorInputAuthority(PlayerRef playerRef)
    {
        var vehiculos = FindObjectsOfType<ControlVehiculo>();
        foreach (var vehiculo in vehiculos)
        {
            if (vehiculo.Object.InputAuthority == playerRef)
            {
                return vehiculo;
            }
        }
        return null;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void MorirRpc(PlayerRef atacante)
    {
        EstaMuerto = true;

        Debug.Log($"[MUERTE-RPC] 💀 {Object.InputAuthority} fue eliminado por {atacante}");

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (efectoMuerte != null)
        {
            var efecto = Instantiate(efectoMuerte, transform.position, transform.rotation);
            Destroy(efecto, 5f);
        }

        if (vehicleRenderer != null)
        {
            vehicleRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);
        }
    }

    private void Respawn()
    {
        if (HasStateAuthority)
        {
            Debug.Log($"[RESPAWN] Iniciando respawn de {Object.InputAuthority}");

            Vida = 100f;
            EstaMuerto = false;

            Vector3 nuevaPosicion = new Vector3(
                UnityEngine.Random.Range(-20f, 20f),
                alturaFija,
                UnityEngine.Random.Range(-20f, 20f)
            );

            RaycastHit hit;
            if (Physics.Raycast(nuevaPosicion + Vector3.up * 10f, Vector3.down, out hit, 20f, capasSuelo))
            {
                nuevaPosicion.y = hit.point.y + alturaFija;
            }

            transform.position = nuevaPosicion;
            transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[RESPAWN] ✅ {Object.InputAuthority} ha respawneado en {nuevaPosicion}");
            RespawnRpc();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RespawnRpc()
    {
        Debug.Log($"[RESPAWN-RPC] Vehículo {Object.InputAuthority} respawneado");

        if (vehicleRenderer != null)
        {
            vehicleRenderer.material.color = HasInputAuthority ? Color.green : Color.gray;
        }

        StartCoroutine(EfectoRespawn());
    }

    private IEnumerator EfectoRespawn()
    {
        if (vehicleRenderer == null) yield break;

        Color colorOriginal = vehicleRenderer.material.color;

        for (int i = 0; i < 5; i++)
        {
            vehicleRenderer.material.color = new Color(0, 1, 0, 0.5f);
            yield return new WaitForSeconds(0.1f);
            vehicleRenderer.material.color = colorOriginal;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator RespawnDespuesDeTiempo()
    {
        float tiempoRespawn = 3f;
        Debug.Log($"[RESPAWN-TIMER] Esperando {tiempoRespawn} segundos para respawn de {Object.InputAuthority}");

        yield return new WaitForSeconds(tiempoRespawn);

        Debug.Log($"[RESPAWN] Ejecutando respawn de {Object.InputAuthority}");
        Respawn();
    }
}