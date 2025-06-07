using UnityEngine;
using Fusion;
using System.Collections;

public class ControlProyectil : NetworkBehaviour
{
    [Header("Configuración del Proyectil")]
    public float velocidad = 20f;
    public float dano = 25f;
    public float tiempoVida = 5f;
    public float radioExplosion = 2f;

    [Header("Efectos")]
    public GameObject efectoExplosion;
    public GameObject efectoTrail;
    public LayerMask capasObjetivos = -1;

    [Networked] public PlayerRef Propietario { get; set; }
    [Networked] public Vector3 Direccion { get; set; }
    [Networked] public TickTimer TiempoDestruir { get; set; }
    [Networked] public Vector3 PosicionInicial { get; set; }
    [Networked] public NetworkBool InicializacionCompleta { get; set; }

    private Rigidbody rb;
    private Collider proyectilCollider;
    private NetworkTransform networkTransform;
    private bool haExplotado = false;
    private bool posicionCorregida = false;
    private float tiempoUltimoLog = 0;

    public override void Spawned()
    {
        Debug.Log($"[ControlProyectil] Spawned en {Runner.LocalPlayer} | StateAuthority: {HasStateAuthority} | InputAuthority: {Object.InputAuthority} | ID:{Object.Id}");

        // Inicializar componentes básicos
        rb = GetComponent<Rigidbody>();
        proyectilCollider = GetComponent<Collider>();
        networkTransform = GetComponent<NetworkTransform>();

        // Aplicar posición inicial si está definida
        if (PosicionInicial != Vector3.zero)
        {
            transform.position = PosicionInicial;
            Debug.Log($"[SPAWN] Usando posición inicial definida: {PosicionInicial}");
        }
        else
        {
            PosicionInicial = transform.position;
            Debug.Log($"[SPAWN] Guardando posición actual como inicial: {PosicionInicial}");
        }

        // Verificar/añadir NetworkTransform
        if (networkTransform == null)
        {
            Debug.LogError("¡Proyectil no tiene NetworkTransform! Añadiendo uno...");
            networkTransform = gameObject.AddComponent<NetworkTransform>();
        }

        // Configurar el Rigidbody para interpolación local
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = false;

            // Inicializar velocidad del Rigidbody según la dirección (si existe)
            if (Direccion != Vector3.zero && !rb.isKinematic)
            {
                rb.linearVelocity = Direccion * velocidad;
                Debug.Log($"[SPAWN] Estableciendo velocidad inicial: {rb.linearVelocity.magnitude} m/s");
            }
        }

        // Asegurarnos que el collider esté configurado para detectar colisiones
        if (proyectilCollider != null)
        {
            proyectilCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("¡Proyectil no tiene collider! Añadiendo uno...");
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.25f;
            collider.isTrigger = true;
            proyectilCollider = collider;
        }

        // Guardar la posición inicial
        Vector3 posInicial = transform.position;

        // Configurar el timer de destrucción (solo servidor)
        if (HasStateAuthority)
        {
            TiempoDestruir = TickTimer.CreateFromSeconds(Runner, tiempoVida);

            // Verificación de posición inicial
            if (posInicial == Vector3.zero)
            {
                Debug.LogWarning($"⚠️ Proyectil spawneado en origen (0,0,0). Propietario: {Propietario}");

                // Corregir posición basada en el propietario si es posible
                var jugadores = FindObjectsOfType<ControlVehiculo>();
                foreach (var jugador in jugadores)
                {
                    if (jugador.Object != null && jugador.Object.InputAuthority == Propietario)
                    {
                        posInicial = jugador.transform.position + jugador.transform.forward * 2f;
                        transform.position = posInicial;
                        transform.rotation = jugador.transform.rotation;
                        Debug.Log($"[SPAWN] Posición corregida a {posInicial} basado en propietario {Propietario}");
                        break;
                    }
                }
            }

            // Almacenar la posición inicial como networked para sincronización
            PosicionInicial = posInicial;
        }

        // Orientar el proyectil en la dirección del movimiento
        if (Direccion != Vector3.zero)
        {
            transform.forward = Direccion;
            Debug.Log($"[SPAWN] Orientando proyectil en dirección: {Direccion}");
        }

        // Activar trail si existe
        if (efectoTrail != null)
        {
            efectoTrail.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[SPAWN] No se encontró efectoTrail asignado");
        }

        // Añadir bandera de seguridad para destrucción
        haExplotado = false;
        posicionCorregida = (PosicionInicial != Vector3.zero);

        Debug.Log($"🚀 Proyectil ID:{Object.Id} spawneado por {Propietario} en posición {posInicial}, dirección {Direccion}");

        // Marcar como inicializado
        InicializacionCompleta = true;
    }

  

    public override void FixedUpdateNetwork()
    {
        // Verificar posición en cada frame
        VerificarPosicion();

        // SISTEMA DE SEGURIDAD: Si ya explotó pero sigue existiendo, forzar destrucción
        if (haExplotado && HasStateAuthority)
        {
            // Si no hay un timer de autodestrucción en marcha, crear uno
            if (!TiempoDestruir.IsRunning)
            {
                Debug.Log($"[SEGURIDAD] Proyectil ID:{Object.Id} explotado pero no destruido. Iniciando timer de autodestrucción");
                TiempoDestruir = TickTimer.CreateFromSeconds(Runner, 2f); // 2 segundos de gracia
            }
            // Si el timer expiró y el objeto sigue existiendo, forzar destrucción
            else if (TiempoDestruir.Expired(Runner))
            {
                Debug.Log($"[FORZAR-DESTRUCCIÓN] Proyectil ID:{Object.Id} no destruido después de explotar. Forzando Despawn");
                Runner.Despawn(Object);
                return;
            }
        }

        // Si el proyectil no está explotado, actualizar su movimiento
        if (!haExplotado)
        {
            // El servidor verifica tiempo de vida
            if (HasStateAuthority && TiempoDestruir.Expired(Runner))
            {
                Debug.Log($"[TIEMPO] Tiempo de vida expirado, destruyendo proyectil ID:{Object.Id}");
                ExplotarProyectil(transform.position, false);
                return;
            }

            // Verificar si necesitamos corregir la posición (solo servidor)
            if (HasStateAuthority && (transform.position == Vector3.zero || Vector3.Distance(transform.position, Vector3.zero) < 0.1f)
                && PosicionInicial != Vector3.zero)
            {
                transform.position = PosicionInicial;

                // Limitar logs para evitar spam
                if (Time.time - tiempoUltimoLog > 1f)
                {
                    Debug.LogWarning($"Corrigiendo posición del proyectil en FixedUpdateNetwork a {PosicionInicial}");
                    tiempoUltimoLog = Time.time;
                }
            }

            // Movimiento del proyectil basado en su dirección
            // Este código se ejecuta en TODOS los clientes, pero solo el servidor actualiza la posición en red
            if (Direccion != Vector3.zero)
            {
                transform.position += Direccion * velocidad * Runner.DeltaTime;

                // Si tenemos Rigidbody, sincronizar su velocidad (opcional)
                if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = Direccion * velocidad;
                }
            }
            else
            {
                transform.position += transform.forward * velocidad * Runner.DeltaTime;

                // Si tenemos Rigidbody, sincronizar su velocidad (opcional)
                if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = transform.forward * velocidad;
                }
            }

            // Verificar límites de mapa (opcional, solo servidor)
            if (HasStateAuthority)
            {
                float maxDistancia = 100f; // Ajustar según el tamaño de tu mapa
                if (transform.position.magnitude > maxDistancia)
                {
                    Debug.Log($"[LÍMITE] Proyectil ID:{Object.Id} salió del área de juego. Destruyendo");
                    ExplotarProyectil(transform.position, false);
                }
            }
        }
    }

    // En lugar de usar OnChanged, verificamos en cada frame
    private void VerificarPosicion()
    {
        if (!posicionCorregida && PosicionInicial != Vector3.zero)
        {
            // Si el proyectil está en el origen o muy cerca, corregir su posición
            if (transform.position == Vector3.zero || Vector3.Distance(transform.position, Vector3.zero) < 0.1f)
            {
                transform.position = PosicionInicial;
                Debug.Log($"Corrigiendo posición del proyectil a {PosicionInicial}");
                posicionCorregida = true;
            }
        }
    }

    // El método puede eliminarse si no se usa
    public void InicializarProyectil(PlayerRef propietario, Vector3 direccion, float fuerzaDisparo)
    {
        if (HasStateAuthority)
        {
            Propietario = propietario;
            PosicionInicial = transform.position;
            Direccion = direccion.normalized;
            Debug.Log($"Proyectil inicializado - Dirección: {Direccion}, Posición: {transform.position}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug para TODOS los clientes, incluso si no tienen autoridad
        Debug.Log($"[COLISIÓN DETECTADA] Proyectil colisionó con: {other.gameObject.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        // Solo el servidor procesa la colisión
        if (!HasStateAuthority || haExplotado) return;

        Debug.Log($"[SERVIDOR] Procesando colisión con: {other.gameObject.name}");

        // Comprobar si colisiona con un vehículo
        var vehiculo = other.GetComponentInParent<ControlVehiculo>();

        if (vehiculo != null)
        {
            Debug.Log($"[SERVIDOR] Colisión con vehículo: ID={vehiculo.Object.Id}, InputAuth={vehiculo.Object.InputAuthority}, Propietario={Propietario}");

            // No dañar al propietario
            if (vehiculo.Object.InputAuthority == Propietario)
            {
                Debug.Log("[SERVIDOR] Ignorando colisión con propietario");
                return;
            }

            // Aplicar daño directo (además de la explosión)
            Debug.Log($"[SERVIDOR] Aplicando daño directo al vehículo {vehiculo.Object.InputAuthority}");
            vehiculo.RecibirDanoRpc(dano * 0.5f, Propietario);
        }

        // Explotar para daño en área
        Debug.Log($"[SERVIDOR] Explotando proyectil en posición {transform.position}");
        ExplotarProyectil(transform.position, true);
    }

    private void ExplotarProyectil(Vector3 posicionExplosion, bool impacto)
    {
        if (HasStateAuthority)
        {
            // Llamar al RPC para todos los clientes
            ExplotarProyectilRpc(posicionExplosion, impacto);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void ExplotarProyectilRpc(Vector3 posicionExplosion, bool impacto)
    {
        if (haExplotado) return;
        haExplotado = true;

        Debug.Log($"[RPC-EXPLOSIÓN] Proyectil ID:{Object.Id} explotando en {posicionExplosion}");

        // Hacer el proyectil invisible inmediatamente
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        // Desactivar colisiones para evitar múltiples explosiones
        if (proyectilCollider != null)
        {
            proyectilCollider.enabled = false;
        }

        // Detener movimiento si tenemos Rigidbody
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        // Efectos visuales (todos los clientes)
        if (efectoExplosion != null)
        {
            var explosion = Instantiate(efectoExplosion, posicionExplosion, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        // Solo el servidor maneja el daño de área
        if (HasStateAuthority && impacto)
        {
            // Usar layer mask = -1 para incluir TODAS las capas
            Collider[] objetivosEnRango = Physics.OverlapSphere(posicionExplosion, radioExplosion, -1);
            Debug.Log($"[DAÑO-ÁREA] Encontrados {objetivosEnRango.Length} objetos en radio de explosión");

            foreach (var objetivo in objetivosEnRango)
            {
                // Intentar obtener ControlVehiculo del collider o sus padres
                var vehiculo = objetivo.GetComponentInParent<ControlVehiculo>();
                if (vehiculo != null && vehiculo.Object != null)
                {
                    Debug.Log($"[DAÑO-ÁREA] Vehículo encontrado: {vehiculo.gameObject.name}, InputAuth: {vehiculo.Object.InputAuthority}");

                    // No dañar al propietario
                    if (vehiculo.Object.InputAuthority != Propietario)
                    {
                        // Calcular daño por distancia
                        float distancia = Vector3.Distance(posicionExplosion, vehiculo.transform.position);
                        float factorDano = 1f - Mathf.Clamp01(distancia / radioExplosion);
                        float danoFinal = dano * factorDano;

                        if (danoFinal > 0)
                        {
                            Debug.Log($"[DAÑO-ÁREA] Aplicando {danoFinal} de daño a vehículo {vehiculo.Object.InputAuthority}");

                            // Llamar al RPC de daño directamente
                            vehiculo.RecibirDanoRpc(danoFinal, Propietario);

                            // Aplicar fuerza de explosión
                            var rbVehiculo = vehiculo.GetComponent<Rigidbody>();
                            if (rbVehiculo != null && !rbVehiculo.isKinematic)
                            {
                                Vector3 direccionExplosion = (vehiculo.transform.position - posicionExplosion).normalized;
                                float fuerzaEmpuje = 300f * factorDano;
                                rbVehiculo.AddForce(direccionExplosion * fuerzaEmpuje, ForceMode.Impulse);
                                Debug.Log($"[FÍSICA] Aplicando fuerza de explosión: {fuerzaEmpuje} en dirección {direccionExplosion}");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"[DAÑO-ÁREA] No dañando al propietario {Propietario}");
                    }
                }
            }
        }

        // IMPORTANTE: Destruir el proyectil definitivamente
        if (HasStateAuthority)
        {
            Debug.Log($"[CLEANUP] Programando destrucción del proyectil ID:{Object.Id}");
            StartCoroutine(DestruirDespuesDeExplosion());
        }
    }

    private IEnumerator DestruirDespuesDeExplosion()
    {
        // Pequeño retraso para que se vean los efectos
        yield return new WaitForSeconds(0.2f);

        if (HasStateAuthority && Runner != null && Object != null && Object.IsValid)
        {
            Debug.Log($"[CLEANUP] Destruyendo proyectil ID:{Object.Id}");
            try
            {
                Runner.Despawn(Object);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ERROR] Error al hacer despawn del proyectil: {e.Message}");

                // Intento final de destrucción
                Destroy(gameObject, 0.5f);
            }
        }
    }

    // Visualización del radio de explosión en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}