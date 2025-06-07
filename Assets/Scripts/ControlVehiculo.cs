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
    public float velocidad = 100f; // Reducido drásticamente para un movimiento más lento
    public float rotacion = 30f; // Reducido a la mitad para una rotación más controlada
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
    public float fuerzaMovimiento = 1f; // Reducido drásticamente

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
        // Código existente para Spawned
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
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.mass = 1000f;
            rb.linearDamping = 5f; // Aumentado para movimiento más lento
            rb.angularDamping = 10f; // Aumentado para rotación más lenta
            rb.useGravity = !usoModoForzado;
        }
        else
        {
            Debug.LogError("¡CRÍTICO! Rigidbody no encontrado en el vehículo. Agregando uno...");
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 1000f;
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

        // Posición inicial fija
        if (HasStateAuthority)
        {
            transform.position = new Vector3(transform.position.x, alturaFija, transform.position.z);

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

        // Activar el modo forzado
        ActivarModoForzado();

        inicializado = true;
    }

    private void ActivarModoForzado()
    {
        usoModoForzado = true;
        Debug.Log("Activando modo forzado de posición para el vehículo");

        if (rb != null)
        {
            rb.useGravity = false;
        }
    }

    private void DiagnosticarColisiones()
    {
        if (!HasStateAuthority) return;

        Vector3[] direcciones = new Vector3[] {
            Vector3.down,
            new Vector3(0.5f, -1, 0).normalized,
            new Vector3(-0.5f, -1, 0).normalized,
            new Vector3(0, -1, 0.5f).normalized,
            new Vector3(0, -1, -0.5f).normalized
        };

        string resultados = "Diagnóstico de raycast: ";
        int hitCount = 0;

        foreach (var dir in direcciones)
        {
            RaycastHit hit;
            bool didHit = Physics.Raycast(transform.position, dir, out hit, 10f, capasSuelo);

            if (didHit)
            {
                hitCount++;
                resultados += $"Hit: {hit.collider.gameObject.name} a {hit.distance}m, ";
            }

            Debug.DrawRay(transform.position, dir * 10f, didHit ? Color.green : Color.red, 1f);
        }

        resultados += $"Total hits: {hitCount}/5";
        Debug.Log(resultados);
    }

    private void Start()
    {
      velocidad = 100f; // Reducido drásticamente para un movimiento más lento
      rotacion = 100f; // Reducido a la mitad para una rotación más controlada
      tiempoEntreLogMovimiento = 1f;
    }


    public override void FixedUpdateNetwork()
    {
        if (!inicializado || EstaMuerto) return;

        // Modo forzado para mantener altura constante
        if (usoModoForzado && HasStateAuthority)
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, alturaFija, pos.z);

            if (rb != null && Mathf.Abs(rb.linearVelocity.y) > 0.1f)
            {
                Vector3 vel = rb.linearVelocity;
                vel.y = 0;
                rb.linearVelocity = vel;
            }

            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"Modo forzado: Pos={transform.position}, Vel={(rb != null ? rb.linearVelocity : Vector3.zero)}");
            }
        }

        // Obtener inputs
        if (GetInput<DatosEntrada>(out var input))
        {
            // Sistema de disparo - IMPORTANTE: Ya no usamos RPC directamente aquí
            // En su lugar, establecemos una variable Networked que el estado observa
            if (input.Disparar && CooldownDisparo.ExpiredOrNotRunning(Runner))
            {
                DisparoSolicitado = true;
                CooldownDisparo = TickTimer.CreateFromSeconds(Runner, tiempoEntreDisparo);
            }
            if (HasStateAuthority)
            {
                if (DisparoSolicitado)
                {
                    // IMPORTANTE: puntoDisparo nunca null
                    Vector3 dir = puntoDisparo != null ? puntoDisparo.forward : transform.forward;
                    Vector3 pos = puntoDisparo != null ? puntoDisparo.position : transform.position + transform.forward * 2f;
                    DispararProyectil(dir, pos);
                    DisparoSolicitado = false;
                }
                MoverVehiculo(input.Aceleracion, input.Direccion);
            }
        }
        else if (HasInputAuthority)
        {
            // Si tenemos input authority pero no recibimos inputs, es un problema
            Debug.LogWarning("Tenemos InputAuthority pero no recibimos inputs!");
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

    // Método separado para mover el vehículo - velocidad drasticamente reducida
    private void MoverVehiculo(float aceleracion, float direccion)
    {
        // Movimiento muy suave
        // MÉTODO 1: Movimiento directo usando Transform (muy reducido)
        if (Mathf.Abs(aceleracion) > 0.1f)
        {
            // Movimiento directo muy lento
            Vector3 movimientoDirecto = transform.forward * aceleracion * velocidad * Runner.DeltaTime * 0.1f;
            transform.position += movimientoDirecto;
        }

        // MÉTODO 2: Usar Rigidbody si está disponible (fuerza mínima)
        if (rb != null && Mathf.Abs(aceleracion) > 0.1f)
        {
            // Aplicar fuerza mínima al Rigidbody
            Vector3 direccionMovimiento = transform.forward * aceleracion * fuerzaMovimiento * 0.1f;
            direccionMovimiento.y = 0; // Eliminar componente vertical

            // Usar ForceMode.Force para más suavidad
            rb.AddForce(direccionMovimiento, ForceMode.Force);

            // Limitar la velocidad máxima del vehículo
            if (rb.linearVelocity.magnitude > velocidad)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * velocidad * 0.5f;
            }
        }

        // Rotación muy lenta
        if (Mathf.Abs(direccion) > 0.1f)
        {
            float rotacionAplicada = direccion * rotacion * Runner.DeltaTime * 0.5f;
            transform.Rotate(0f, rotacionAplicada, 0f);
        }

        // Depuración para confirmar movimiento
        if ((Mathf.Abs(aceleracion) > 0.1f || Mathf.Abs(direccion) > 0.1f) &&
            Time.time - ultimoLogMovimiento > tiempoEntreLogMovimiento)
        {
            float distanciaRecorrida = Vector3.Distance(transform.position, ultimaPosicion);
            if (distanciaRecorrida > 0.1f)
            {
                Debug.Log($"🚗 Vehículo {Object.Id} - Movimiento: {distanciaRecorrida:F2}m, Vel: {(distanciaRecorrida / tiempoEntreLogMovimiento):F2}m/s");
                ultimaPosicion = transform.position;
                ultimoLogMovimiento = Time.time;
            }
        }
    }

    // ¡IMPORTANTE! Reemplazamos el método RPC por un método directo
    private void DispararProyectil(Vector3 direccion, Vector3 posicion)
    {
        if (!prefabProyectil.IsValid || !HasStateAuthority) return;

        // Asegúrate de que se muestre algún efecto visual
        if (efectoDisparo != null)
        {
            var efecto = Instantiate(efectoDisparo, posicion, Quaternion.LookRotation(direccion));
            Destroy(efecto, 2f);
        }

        // Spawnear el proyectil con todos los datos correctos
        Runner.Spawn(prefabProyectil, posicion, Quaternion.LookRotation(direccion), Object.InputAuthority, (runner, obj) => {
            var c = obj.GetComponent<ControlProyectil>();
            if (c != null)
            {
                c.Propietario = Object.InputAuthority;
                c.Direccion = direccion.normalized;
                c.PosicionInicial = posicion;

                // Añadir log para verificar la inicialización
                Debug.Log($"Inicializando proyectil - Propietario: {c.Propietario}, Dirección: {c.Direccion}, Posición: {c.PosicionInicial}");
            }
            else
            {
                Debug.LogError("¡El prefab del proyectil no tiene el componente ControlProyectil!");
            }
        });

        // Informar a todos los clientes sobre el disparo
        MostrarEfectoDisparoRpc();
    }

    // Mantenemos el RPC para los efectos de disparo
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

                // Notificar a todos los clientes del daño
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

            // Notificar a todos los clientes sobre la muerte
            MorirRpc(atacante);

            // Programar respawn usando coroutine de Fusion en lugar de Invoke
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

        // Desactivar movimiento y controles
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Efecto visual de muerte
        if (efectoMuerte != null)
        {
            var efecto = Instantiate(efectoMuerte, transform.position, transform.rotation);
            Destroy(efecto, 5f);
        }

        // Cambiar apariencia del vehículo
        if (vehicleRenderer != null)
        {
            // Color gris oscuro para indicar muerte
            vehicleRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);
        }
        
    }

    private void Respawn()
    {
        if (HasStateAuthority)
        {
            Debug.Log($"[RESPAWN] Iniciando respawn de {Object.InputAuthority}");

            // Restaurar vida y estado
            Vida = 100f;
            EstaMuerto = false;

           // Generar posición aleatoria más amplia
           Vector3 nuevaPosicion = new Vector3(
           UnityEngine.Random.Range(-20f, 20f),
           alturaFija,
           UnityEngine.Random.Range(-20f, 20f)
       );

            // Asegurar que está por encima del suelo
            RaycastHit hit;
            if (Physics.Raycast(nuevaPosicion + Vector3.up * 10f, Vector3.down, out hit, 20f, capasSuelo))
            {
                nuevaPosicion.y = hit.point.y + alturaFija;
            }

            // Aplicar posición y rotación aleatoria
            transform.position = nuevaPosicion;
            transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

            // Resetear físicas
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[RESPAWN] ✅ {Object.InputAuthority} ha respawneado en {nuevaPosicion}");

            // Notificar a todos los clientes del respawn
            RespawnRpc();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RespawnRpc()
    {
        Debug.Log($"[RESPAWN-RPC] Vehículo {Object.InputAuthority} respawneado");

        // Restaurar apariencia visual
        if (vehicleRenderer != null)
        {
            // Restaurar color original (verde para jugador local, gris para otros)
            vehicleRenderer.material.color = HasInputAuthority ? Color.green : Color.gray;
        }

        // Efectos visuales de respawn
        StartCoroutine(EfectoRespawn());
    }

    private IEnumerator DescongelarDespuesDeRespawn()
    {
        yield return new WaitForSeconds(0.5f);
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void NotificarRespawnRpc()
    {
        // Efectos visuales de respawn si los tienes
        if (vehicleRenderer != null)
        {
            StartCoroutine(EfectoRespawn());
        }
    }

    private IEnumerator EfectoRespawn()
    {
        // Efecto de parpadeo para indicar respawn
        if (vehicleRenderer == null) yield break;

        Color colorOriginal = vehicleRenderer.material.color;

        for (int i = 0; i < 5; i++)
        {
            vehicleRenderer.material.color = new Color(0, 1, 0, 0.5f); // Verde transparente
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