# Juego Multijugador de Vehículos con Fusion Network

Este proyecto implementa un juego de combate de vehículos multijugador utilizando Fusion Network en Unity, donde los jugadores pueden disparar proyectiles para dañar a los vehículos enemigos.


## Características

- Sistema multijugador completo con arquitectura de autoridad cliente-servidor
- Vehículos controlables con física realista
- Sistema de disparos y proyectiles con efectos de explosión
- Detección de daño y sistema de vida de los vehículos
- Respawn automático en posiciones aleatorias
- Interfaz de usuario para mostrar vida, kills y muertes

## Problemas Solucionados

Este proyecto resuelve varios desafíos comunes en el desarrollo de juegos multijugador:

1. **Sincronización de Proyectiles**: Solución para que los proyectiles se muevan correctamente en todos los clientes
2. **Sistema de Daño en Red**: Implementación robusta para aplicar daño a vehículos desde cualquier cliente
3. **Destrucción Confiable de Objetos**: Mecanismo para garantizar que los proyectiles se destruyan correctamente
4. **Respawn Automático**: Sistema para regenerar vehículos destruidos en posiciones aleatorias

## Arquitectura de Red

El proyecto utiliza la arquitectura de Fusion Network, que sigue un modelo de autoridad de servidor:

- **State Authority**: El host/servidor tiene autoridad sobre el estado del juego
- **Input Authority**: Cada cliente tiene autoridad sobre sus propias entradas
- **NetworkObject**: Componente base para objetos sincronizados en red
- **NetworkBehaviour**: Componentes de comportamiento que funcionan en red
- **Networked Properties**: Variables sincronizadas automáticamente en red
- **RPC (Remote Procedure Calls)**: Funciones que se pueden llamar a través de la red

## Componentes Principales

### 1. GestorRed.cs

Encargado de iniciar la sesión de red y gestionar la conexión de jugadores.

```csharp
// Spawner de vehículos para jugadores
private void SpawnVehiculoJugador(NetworkRunner runner, PlayerRef player)
{
    // Generar posición aleatoria para el spawn
    Vector3 pos = new Vector3(
        Random.Range(-15f, 15f),
        1f,
        Random.Range(-15f, 15f)
    );
    
    // Rotación aleatoria para variar la dirección inicial
    Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

    // Spawnear el vehículo con autoridad de input explícita
    var carro = runner.Spawn(prefabVehiculo, pos, rot, player);
}
```

### 2. CapturaEntrada.cs

Captura las entradas del jugador y las envía a través de la red.

```csharp
public void OnInput(NetworkRunner runner, NetworkInput input)
{
    // Capturar inputs
    var datos = new DatosEntrada
    {
        Aceleracion = Input.GetAxis("Vertical"),
        Direccion = Input.GetAxis("Horizontal"),
        Disparar = Input.GetKey(teclaDisparo)
    };
    
    // Enviar los datos al runner
    input.Set(datos);
}
```

### 3. ControlVehiculo.cs

Gestiona el comportamiento del vehículo, incluyendo movimiento, disparo y sistema de vida.

**Sistema de Disparo**:
```csharp
private void DispararProyectil(Vector3 direccion, Vector3 posicion)
{
    if (!prefabProyectil.IsValid || !HasStateAuthority) return;
    
    // Spawnear el proyectil con valores iniciales correctos
    Runner.Spawn(prefabProyectil, posicion, Quaternion.LookRotation(direccion), 
        Object.InputAuthority, (runner, obj) => {
        var proyectil = obj.GetComponent<ControlProyectil>();
        if (proyectil != null)
        {
            proyectil.Propietario = Object.InputAuthority;
            proyectil.Direccion = direccion.normalized;
        }
    });
}
```

**Sistema de Daño**:
```csharp
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RecibirDanoRpc(float cantidad, PlayerRef atacante)
{
    if (HasStateAuthority && !EstaMuerto)
    {
        Vida = Mathf.Max(0, Vida - cantidad);
        
        if (Vida <= 0)
        {
            Morir(atacante);
        }
    }
}
```

**Sistema de Respawn**:
```csharp
private void Respawn()
{
    if (HasStateAuthority)
    {
        // Restaurar vida y estado
        Vida = 100f;
        EstaMuerto = false;

        // Generar posición aleatoria
        Vector3 nuevaPosicion = new Vector3(
            UnityEngine.Random.Range(-20f, 20f),
            alturaFija,
            UnityEngine.Random.Range(-20f, 20f)
        );
        
        // Aplicar posición y rotación aleatoria
        transform.position = nuevaPosicion;
        transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
    }
}
```

### 4. ControlProyectil.cs

Gestiona el comportamiento de los proyectiles, incluyendo movimiento, colisión y sistema de daño por explosión.

**Inicialización del Proyectil**:
```csharp
public override void Spawned()
{
    // Inicializar componentes físicos
    rb = GetComponent<Rigidbody>();
    proyectilCollider = GetComponent<Collider>();
    networkTransform = GetComponent<NetworkTransform>();
    
    // Configurar posición inicial
    if (PosicionInicial != Vector3.zero)
    {
        transform.position = PosicionInicial;
    }
    
    // Configurar timer de vida (solo servidor)
    if (HasStateAuthority)
    {
        TiempoDestruir = TickTimer.CreateFromSeconds(Runner, tiempoVida);
    }
}
```

**Movimiento del Proyectil**:
```csharp
public override void FixedUpdateNetwork()
{
    // Si el proyectil no está explotado, moverlo
    if (!haExplotado)
    {
        // El servidor verifica tiempo de vida
        if (HasStateAuthority && TiempoDestruir.Expired(Runner))
        {
            ExplotarProyectil(transform.position, false);
            return;
        }

        // Movimiento - procesado por TODOS los clientes 
        transform.position += Direccion * velocidad * Runner.DeltaTime;
    }
}
```

**Sistema de Colisión y Daño**:
```csharp
private void OnTriggerEnter(Collider other)
{
    // Solo el servidor procesa el daño
    if (!HasStateAuthority || haExplotado) return;
    
    // Ignorar colisiones con el propietario
    var vehiculo = other.GetComponentInParent<ControlVehiculo>();
    if (vehiculo != null && vehiculo.Object.InputAuthority == Propietario)
    {
        return;
    }
    
    // Explotar al impactar
    ExplotarProyectil(transform.position, true);
}
```

**Sistema de Explosión**:
```csharp
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void ExplotarProyectilRpc(Vector3 posicionExplosion, bool impacto)
{
    if (haExplotado) return;
    haExplotado = true;
    
    // Efectos visuales
    if (efectoExplosion != null)
    {
        var explosion = Instantiate(efectoExplosion, posicionExplosion, Quaternion.identity);
        Destroy(explosion, 3f);
    }
    
    // Daño en área (solo servidor)
    if (HasStateAuthority && impacto)
    {
        Collider[] objetivosEnRango = Physics.OverlapSphere(posicionExplosion, radioExplosion, -1);
        
        foreach (var objetivo in objetivosEnRango)
        {
            var vehiculo = objetivo.GetComponentInParent<ControlVehiculo>();
            if (vehiculo != null && vehiculo.Object.InputAuthority != Propietario)
            {
                // Calcular daño por distancia
                float distancia = Vector3.Distance(posicionExplosion, vehiculo.transform.position);
                float factorDano = 1f - Mathf.Clamp01(distancia / radioExplosion);
                float danoFinal = dano * factorDano;
                
                if (danoFinal > 0)
                {
                    vehiculo.RecibirDanoRpc(danoFinal, Propietario);
                }
            }
        }
    }
    
    // Destruir el proyectil
    if (HasStateAuthority)
    {
        StartCoroutine(DestruirDespuesDeExplosion());
    }
}
```

## Soluciones Implementadas

### 1. Sincronización de Proyectiles

El problema de sincronización de proyectiles se resolvió mediante:

- **NetworkTransform**: Asegurando que cada proyectil tenga un componente NetworkTransform.
- **Movimiento Consistente**: Actualizando la posición en todos los clientes basado en la dirección del proyectil.
- **Variables Networked**: Sincronizando estado y dirección del proyectil.
- **Verificación de Posición**: Implementando un sistema para corregir proyectiles que aparecen en origen incorrecto.

### 2. Sistema de Daño

El problema de daño se resolvió mediante:

- **RPCs Bidireccionales**: Usando RPCs desde el cliente al servidor y viceversa.
- **Layer Masks Inclusivas**: Asegurando que todas las capas (-1) se consideran para colisiones.
- **Daño por Colisión Directa**: Aplicando daño directo en colisiones.
- **Daño por Área**: Calculando daño basado en la distancia al centro de la explosión.

### 3. Destrucción de Proyectiles

El problema de proyectiles que no se destruían se resolvió mediante:

- **Destrucción Inmediata Visual**: Desactivando renderers inmediatamente tras explotar.
- **Destrucción Retrasada**: Usando coroutines para dar tiempo a efectos visuales.
- **Sistema de Seguridad**: Implementando un timer de respaldo para forzar destrucción.
- **Manejo de Errores**: Capturando excepciones durante el proceso de destrucción.

```csharp
private IEnumerator DestruirDespuesDeExplosion()
{
    yield return new WaitForSeconds(0.2f);
    
    if (HasStateAuthority && Runner != null && Object != null && Object.IsValid)
    {
        Runner.Despawn(Object);
    }
}
```

### 4. Respawn de Vehículos

El problema de respawn se resolvió mediante:

- **Coroutines de Fusion**: Usando Runner.StartCoroutine en lugar de Invoke.
- **Flujo de Autoridad**: Asegurando que solo el servidor controla el respawn.
- **Posiciones Aleatorias**: Generando posiciones aleatorias para respawn.
- **Efectos Visuales**: Añadiendo efectos para indicar el respawn.

```csharp
private IEnumerator RespawnDespuesDeTiempo()
{
    yield return new WaitForSeconds(3f);
    Respawn();
}
```

## Lecciones Aprendidas

1. **Autoridad de Red**: Es crucial entender qué cliente tiene autoridad sobre qué acciones. En Fusion:
   - El servidor (StateAuthority) controla el estado del juego
   - Los clientes (InputAuthority) controlan solo sus entradas

2. **Depuración en Red**: Los logs detallados son esenciales para depurar juegos en red.
   ```csharp
   Debug.Log($"[DAÑO-ÁREA] Aplicando {danoFinal} de daño a vehículo {vehiculo.Object.InputAuthority}");
   ```

3. **Sistemas de Respaldo**: Siempre implementar mecanismos de seguridad para casos extremos.
   ```csharp
   // Sistema de seguridad para proyectiles huérfanos
   if (haExplotado && HasStateAuthority && TiempoDestruir.Expired(Runner))
   {
       Runner.Despawn(Object);
   }
   ```

4. **Interpolación de Movimiento**: Para movimiento suave en red, se utilizaron técnicas de interpolación.
   ```csharp
   rb.interpolation = RigidbodyInterpolation.Interpolate;
   ```

## Consejos para Proyectos Fusion

1. **Objetos Prefabs**: Todos los objetos en red deben tener NetworkObject como componente raíz.
2. **Networked Variables**: Usa atributo [Networked] para variables que necesitan sincronización.
3. **RPC Usage**: Usa RPCs para acciones puntuales; Networked Variables para estado continuo.
4. **Autoridad**: Respeta el flujo de autoridad; solo el servidor debe tomar decisiones críticas.
5. **Logging**: Implementa logs detallados con tags que identifiquen subsistemas.

## Configuración del Proyecto

Para implementar este sistema en tu proyecto:

1. **Fusion Setup**: Instala Fusion Network desde el Asset Store.
2. **NetworkRunner**: Configura un NetworkRunner en la escena principal.
3. **Prefabs**: Configura los prefabs para vehículos y proyectiles asegurando que tienen todos los componentes necesarios.
4. **Layers**: Configura correctamente las capas de colisión en Physics Settings.

## Depuración

Para facilitar la depuración, se implementaron mensajes de log detallados con etiquetas específicas:

- `[SPAWN]`: Inicialización de objetos
- `[RPC-EXPLOSIÓN]`: Procesamiento de explosiones
- `[DAÑO-ÁREA]`: Cálculos de daño por área
- `[MUERTE]`: Procesamiento de muerte de vehículos
- `[RESPAWN]`: Sistema de respawn
- `[SEGURIDAD]`: Sistemas de respaldo

Estas etiquetas facilitan filtrar los logs cuando se depura en entornos multijugador complejos.

## Requisitos

- Unity 2021.3 o superior (Recomendado Unity 6)
- Photon - Fusion 2
- Paquete de Input System

## Instalación

1. Clona este repositorio
2. Abre el proyecto en Unity
3. Importa Fusion Network desde el Asset Store
4. Abre la escena principal y ejecuta el juego

## Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo LICENSE para más detalles.

## Contribuciones

Las contribuciones son bienvenidas. Por favor, crea un issue o un pull request para sugerir cambios o mejoras.

## Contacto

Si tienes preguntas o comentarios, por favor abre un issue en este repositorio.

---

Desarrollado con ❤️ usando Fusion Network y Unity.
Ragross Studios
