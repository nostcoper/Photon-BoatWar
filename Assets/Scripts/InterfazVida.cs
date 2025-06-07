using UnityEngine;
using Fusion;
using UnityEngine.UI;
using TMPro;

public class InterfazVida : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider barraVida;
    public Text textoVida; // Usar Text normal como respaldo si TMPro no está disponible
    public TextMeshProUGUI textoVidaTMP; // TextMeshPro opcional
    public Image fondoBarraVida;
    public Text textoKills; // Usar Text normal como respaldo
    public TextMeshProUGUI textoKillsTMP; // TextMeshPro opcional
    public Text textoMuertes; // Usar Text normal como respaldo
    public TextMeshProUGUI textoMuertesTMP; // TextMeshPro opcional

    [Header("Colores")]
    public Color colorVidaCompleta = Color.green;
    public Color colorVidaMedia = Color.yellow;
    public Color colorVidaBaja = Color.red;

    private ControlVehiculo vehiculoLocal;
    private Canvas interfazCanvas;
    private int ultimasKills = -1;
    private int ultimasMuertes = -1;
    private float ultimaVida = -1;

    void Start()
    {
        // Buscar el vehículo del jugador local
        StartCoroutine(BuscarVehiculoLocal());

        // Configurar canvas
        interfazCanvas = GetComponent<Canvas>();
        if (interfazCanvas != null)
        {
            interfazCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            interfazCanvas.sortingOrder = 100;
        }

        // Configurar barra de vida inicial
        if (barraVida != null)
        {
            barraVida.maxValue = 100f;
            barraVida.value = 100f;
        }

        // Verificación inicial de componentes UI
        VerificarComponentesUI();
    }

    private void VerificarComponentesUI()
    {
        // Verificar si tenemos los componentes TextMeshPro
        if (textoVidaTMP == null && textoVida == null)
        {
            Debug.LogWarning("InterfazVida: No se encontró componente de texto para Vida");
        }

        if (textoKillsTMP == null && textoKills == null)
        {
            Debug.LogWarning("InterfazVida: No se encontró componente de texto para Kills");
        }

        if (textoMuertesTMP == null && textoMuertes == null)
        {
            Debug.LogWarning("InterfazVida: No se encontró componente de texto para Muertes");
        }

        // Verificar la barra de vida
        if (barraVida == null)
        {
            Debug.LogWarning("InterfazVida: No se encontró la barra de vida");
        }
    }

    void Update()
    {
        if (vehiculoLocal != null)
        {
            ActualizarInterfazVida();
        }
        else if (Time.frameCount % 30 == 0) // Reintentar cada cierto tiempo
        {
            StartCoroutine(BuscarVehiculoLocal());
        }
    }

    private System.Collections.IEnumerator BuscarVehiculoLocal()
    {
        // Esperar hasta encontrar el vehículo local
        int intentos = 0;
        while (vehiculoLocal == null && intentos < 10) // Máximo 10 intentos
        {
            var vehiculos = FindObjectsOfType<ControlVehiculo>();
            foreach (var vehiculo in vehiculos)
            {
                if (vehiculo.HasInputAuthority)
                {
                    vehiculoLocal = vehiculo;
                    Debug.Log("🎯 Vehículo local encontrado para la UI");
                    break;
                }
            }
            intentos++;
            yield return new WaitForSeconds(0.5f);
        }

        if (vehiculoLocal == null && intentos >= 10)
        {
            Debug.LogWarning("No se pudo encontrar el vehículo local después de varios intentos");
        }
    }

    private void ActualizarInterfazVida()
    {
        if (vehiculoLocal == null) return;

        float vidaActual = vehiculoLocal.Vida;
        int kills = vehiculoLocal.Kills;
        int muertes = vehiculoLocal.Muertes;

        // Solo actualizar si ha cambiado algo (optimización)
        bool actualizar = vidaActual != ultimaVida || kills != ultimasKills || muertes != ultimasMuertes;

        if (!actualizar) return;

        // Guardar valores actuales
        ultimaVida = vidaActual;
        ultimasKills = kills;
        ultimasMuertes = muertes;

        // Actualizar barra de vida
        if (barraVida != null)
        {
            barraVida.value = vidaActual;

            // Cambiar color según la vida
            var imagenBarra = barraVida.fillRect.GetComponent<Image>();
            if (imagenBarra != null)
            {
                float porcentajeVida = vidaActual / 100f;

                if (porcentajeVida > 0.6f)
                    imagenBarra.color = colorVidaCompleta;
                else if (porcentajeVida > 0.3f)
                    imagenBarra.color = colorVidaMedia;
                else
                    imagenBarra.color = colorVidaBaja;

                // Animación de parpadeo para vida baja
                if (porcentajeVida < 0.3f)
                {
                    float alpha = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
                    var tempColor = imagenBarra.color;
                    tempColor.a = alpha;
                    imagenBarra.color = tempColor;
                }
            }
        }

        // Actualizar texto de vida - soporta ambos tipos de texto
        ActualizarTexto(textoVida, textoVidaTMP, $"Vida: {vidaActual:F0}/100");

        // Actualizar estadísticas
        ActualizarTexto(textoKills, textoKillsTMP, $"Kills: {kills}");
        ActualizarTexto(textoMuertes, textoMuertesTMP, $"Muertes: {muertes}");
    }

    // Helper para actualizar texto con soporte para Text normal o TextMeshPro
    private void ActualizarTexto(Text textoNormal, TextMeshProUGUI textoTMP, string contenido)
    {
        if (textoTMP != null)
        {
            textoTMP.text = contenido;
        }
        else if (textoNormal != null)
        {
            textoNormal.text = contenido;
        }
    }
}