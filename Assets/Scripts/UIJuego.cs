using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HUD animado (puntos, vidas, récord, velocidad, avisos) y panel final de Game Over / Victoria.
/// Va en el objeto CanvasUI.
/// </summary>
public class UIJuego : MonoBehaviour
{
    [Header("HUD general")]
    [SerializeField] private CanvasGroup hud;

    [Header("HUD - Puntos")]
    [SerializeField] private TMP_Text textoPuntos;
    [SerializeField] private TMP_Text textoMeta;
    [SerializeField] private Image rellenoProgreso;
    [Tooltip("Cada cuántos puntos el marcador hace un pulso.")]
    [SerializeField] private int pasoPulsoPuntos = 100;
    [SerializeField] private Color colorPuntos = Color.white;
    [SerializeField] private Color colorPulsoPuntos = new Color(1f, 0.82f, 0.48f);

    [Header("HUD - Vidas (celdas de oxígeno)")]
    [SerializeField] private GameObject plantillaCelda;
    [SerializeField] private TMP_Text textoVidas;
    [SerializeField] private Color colorVidaLlena = new Color(0.31f, 0.85f, 1f);
    [SerializeField] private Color colorVidaVacia = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color colorVidaCritica = new Color(1f, 0.29f, 0.23f);

    [Header("HUD - Récord y velocidad")]
    [SerializeField] private TMP_Text textoRecord;
    [SerializeField] private TMP_Text textoVelocidad;

    [Header("Avisos en pantalla")]
    [SerializeField] private TMP_Text textoAviso;
    [SerializeField] private string mensajeInicio = "¡CORRE HACIA LA BASE!";
    [SerializeField] private string mensajeImpacto = "¡IMPACTO! -1 OXÍGENO";
    [SerializeField] private string mensajeVelocidad = "¡VELOCIDAD AUMENTADA!";
    [SerializeField] private string mensajeRecord = "¡NUEVO RÉCORD!";
    [SerializeField] private Color colorAviso = new Color(1f, 0.82f, 0.48f);
    [SerializeField] private Image flashDanio;
    [Range(0f, 1f)][SerializeField] private float alfaFlash = 0.45f;

    [Header("Panel final")]
    [SerializeField] private CanvasGroup panelFin;
    [SerializeField] private RectTransform tarjeta;
    [SerializeField] private Outline bordeTarjeta;
    [SerializeField] private Image lineaSeparadora;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoSubtitulo;
    [SerializeField] private TMP_Text textoPuntuacionFinal;
    [SerializeField] private TMP_Text textoRecordFinal;
    [SerializeField] private RectTransform insigniaRecord;
    [SerializeField] private Button botonReintentar;
    [SerializeField] private Button botonSalir;

    [Header("Game Over: texto y color")]
    [SerializeField] private string tituloGameOver = "MISIÓN FALLIDA";
    [SerializeField] private string subtituloGameOver = "JUEGO TERMINADO";
    [SerializeField] private Color colorGameOver = new Color(1f, 0.29f, 0.23f);

    [Header("Victoria: texto y color")]
    [SerializeField] private string tituloVictoria = "¡MISIÓN CUMPLIDA!";
    [SerializeField] private string subtituloVictoria = "LLEGASTE A LA BASE DE RESCATE";
    [SerializeField] private Color colorVictoria = new Color(0.36f, 1f, 0.69f);

    [Header("Tiempos de animación (segundos)")]
    [SerializeField] private float duracionEntradaPanel = 0.5f;
    [SerializeField] private float duracionConteoFinal = 1.2f;
    [SerializeField] private float tiempoPorLetra = 0.04f;

    private readonly List<Image> celdas = new List<Image>();
    private int metaPuntos;
    private int ultimoPuntos = -1;
    private int ultimoPulso;
    private int recordInicial;
    private float velocidadAnterior = -1f;
    private bool finMostrado;
    private bool esVictoria;

    private Coroutine rutinaAviso;
    private Coroutine rutinaPulso;
    private Coroutine rutinaCritica;
    private Coroutine rutinaFlash;

    public bool PanelFinInteractivo => panelFin != null && panelFin.interactable;

    private void Awake()
    {
        if (botonReintentar != null)
            botonReintentar.onClick.AddListener(() => { if (GameManager.Instancia != null) GameManager.Instancia.Reiniciar(); });
        if (botonSalir != null)
            botonSalir.onClick.AddListener(() => { if (GameManager.Instancia != null) GameManager.Instancia.Salir(); });

        if (panelFin != null)
        {
            panelFin.alpha = 0f;
            panelFin.interactable = false;
            panelFin.blocksRaycasts = false;
            panelFin.gameObject.SetActive(false);
        }
        if (flashDanio != null) PonerAlfa(flashDanio, 0f);
        if (textoAviso != null) textoAviso.alpha = 0f;
        if (plantillaCelda != null) plantillaCelda.SetActive(false);
    }

    // ---------- Inicialización ----------
    public void Inicializar(int vidas, int record, int meta)
    {
        metaPuntos = meta;
        recordInicial = record;

        foreach (Image c in celdas) if (c != null) Destroy(c.transform.parent.gameObject);
        celdas.Clear();
        if (plantillaCelda != null)
        {
            for (int i = 0; i < vidas; i++)
            {
                GameObject celda = Instantiate(plantillaCelda, plantillaCelda.transform.parent);
                celda.SetActive(true);
                Image icono = celda.GetComponentInChildren<Image>();
                icono.color = colorVidaLlena;
                celdas.Add(icono);
            }
        }

        if (textoVidas != null) textoVidas.text = "x" + vidas;
        if (textoRecord != null) textoRecord.text = "RÉCORD " + record.ToString("D5");
        if (textoMeta != null) textoMeta.text = meta > 0 ? "META " + meta : "SIN META";
        if (rellenoProgreso != null)
        {
            rellenoProgreso.fillAmount = 0f;
            rellenoProgreso.transform.parent.gameObject.SetActive(meta > 0);
        }

        ActualizarPuntos(0);
        if (vidas == 1 && celdas.Count > 0) rutinaCritica = StartCoroutine(PulsoCritico(celdas[0]));
        MostrarAviso(mensajeInicio, colorAviso, 1.6f);
    }

    // ---------- HUD ----------
    public void ActualizarPuntos(int puntos)
    {
        if (puntos == ultimoPuntos) return;
        ultimoPuntos = puntos;

        if (textoPuntos != null) textoPuntos.text = puntos.ToString("D5");

        if (recordInicial > 0 && puntos > recordInicial && textoRecord != null)
        {
            textoRecord.text = "RÉCORD " + puntos.ToString("D5");
            textoRecord.color = colorPulsoPuntos;
        }

        if (pasoPulsoPuntos > 0)
        {
            int hito = puntos / pasoPulsoPuntos;
            if (hito > ultimoPulso)
            {
                ultimoPulso = hito;
                if (rutinaPulso != null) StopCoroutine(rutinaPulso);
                rutinaPulso = StartCoroutine(PulsoTexto(textoPuntos, colorPuntos, colorPulsoPuntos, 1.25f));
            }
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (rellenoProgreso != null && metaPuntos > 0)
        {
            float objetivo = Mathf.Clamp01(ultimoPuntos / (float)metaPuntos);
            rellenoProgreso.fillAmount = Mathf.Lerp(rellenoProgreso.fillAmount, objetivo, 1f - Mathf.Exp(-8f * dt));
        }

        ControlVelocidad cv = ControlVelocidad.Instancia;
        if (cv != null && !finMostrado)
        {
            float v = cv.VelocidadActual;
            if (textoVelocidad != null) textoVelocidad.text = "VELOCIDAD " + v.ToString("0.0") + " m/s";
            if (velocidadAnterior >= 0f && v > velocidadAnterior + 0.01f)
            {
                MostrarAviso(mensajeVelocidad, colorVidaLlena, 1.2f);
            }
            velocidadAnterior = v;
        }

        if (finMostrado) AplicarEstiloFin(); // permite cambiar texto/color en vivo desde el Inspector
    }

    public void PerderVida(int vidasRestantes)
    {
        if (textoVidas != null) textoVidas.text = "x" + Mathf.Max(0, vidasRestantes);

        if (rutinaCritica != null)
        {
            StopCoroutine(rutinaCritica);
            rutinaCritica = null;
        }

        int indice = vidasRestantes;
        if (indice >= 0 && indice < celdas.Count) StartCoroutine(AnimarCeldaPerdida(celdas[indice]));

        if (vidasRestantes == 1 && celdas.Count > 0) rutinaCritica = StartCoroutine(PulsoCritico(celdas[0]));

        if (rutinaFlash != null) StopCoroutine(rutinaFlash);
        rutinaFlash = StartCoroutine(Flash());

        if (vidasRestantes > 0) MostrarAviso(mensajeImpacto, colorVidaCritica, 1f);
    }

    public void AvisoNuevoRecord()
    {
        MostrarAviso(mensajeRecord, colorPulsoPuntos, 1.5f);
    }

    // ---------- Panel final ----------
    public void MostrarFin(bool victoria, int puntos, int record, bool nuevoRecord)
    {
        if (rutinaCritica != null)
        {
            StopCoroutine(rutinaCritica);
            rutinaCritica = null;
        }
        StartCoroutine(SecuenciaFin(victoria, puntos, record, nuevoRecord));
    }

    private IEnumerator SecuenciaFin(bool victoria, int puntos, int record, bool nuevoRecord)
    {
        esVictoria = victoria;

        if (rutinaAviso != null) StopCoroutine(rutinaAviso);
        if (textoAviso != null) textoAviso.alpha = 0f;
        if (hud != null) StartCoroutine(Fundido(hud, hud.alpha, 0f, 0.4f));

        if (panelFin == null) yield break;

        panelFin.gameObject.SetActive(true);
        panelFin.alpha = 0f;
        panelFin.interactable = false;
        panelFin.blocksRaycasts = false;

        finMostrado = true;
        AplicarEstiloFin();

        if (textoTitulo != null) textoTitulo.maxVisibleCharacters = 0;
        if (textoPuntuacionFinal != null) textoPuntuacionFinal.text = 0.ToString("D5");
        if (textoRecordFinal != null) textoRecordFinal.text = "RÉCORD " + record.ToString("D5");
        if (insigniaRecord != null) insigniaRecord.gameObject.SetActive(false);

        // 1) Entrada con rebote
        float t = 0f;
        if (tarjeta != null) tarjeta.localScale = Vector3.one * 0.6f;
        while (t < duracionEntradaPanel)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracionEntradaPanel);
            panelFin.alpha = Mathf.Clamp01(k * 1.5f);
            if (tarjeta != null) tarjeta.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(k));
            yield return null;
        }
        panelFin.alpha = 1f;
        if (tarjeta != null) tarjeta.localScale = Vector3.one;

        // 2) Título letra por letra
        if (textoTitulo != null)
        {
            textoTitulo.ForceMeshUpdate();
            int total = textoTitulo.textInfo.characterCount;
            for (int i = 0; i <= total; i++)
            {
                textoTitulo.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(tiempoPorLetra);
            }
            textoTitulo.maxVisibleCharacters = 99999;
        }

        // 3) Conteo de la puntuación final
        if (textoPuntuacionFinal != null)
        {
            t = 0f;
            while (t < duracionConteoFinal)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duracionConteoFinal), 3f);
                textoPuntuacionFinal.text = Mathf.RoundToInt(puntos * k).ToString("D5");
                yield return null;
            }
            textoPuntuacionFinal.text = puntos.ToString("D5");
            StartCoroutine(PulsoTexto(textoPuntuacionFinal, Color.white, ColorFin(), 1.2f));
        }

        // 4) Insignia de récord
        if (nuevoRecord && insigniaRecord != null)
        {
            insigniaRecord.gameObject.SetActive(true);
            StartCoroutine(LatidoInsignia());
        }

        // 5) Botones activos
        panelFin.interactable = true;
        panelFin.blocksRaycasts = true;
        if (botonReintentar != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(botonReintentar.gameObject);
        }
    }

    private Color ColorFin() => esVictoria ? colorVictoria : colorGameOver;

    private void AplicarEstiloFin()
    {
        Color c = ColorFin();
        if (textoTitulo != null)
        {
            textoTitulo.text = esVictoria ? tituloVictoria : tituloGameOver;
            textoTitulo.color = c;
        }
        if (textoSubtitulo != null) textoSubtitulo.text = esVictoria ? subtituloVictoria : subtituloGameOver;
        if (bordeTarjeta != null) bordeTarjeta.effectColor = c;
        if (lineaSeparadora != null) lineaSeparadora.color = c;
    }

    // ---------- Animaciones ----------
    public void MostrarAviso(string mensaje, Color color, float duracion)
    {
        if (textoAviso == null || string.IsNullOrEmpty(mensaje)) return;
        if (rutinaAviso != null) StopCoroutine(rutinaAviso);
        rutinaAviso = StartCoroutine(RutinaAviso(mensaje, color, duracion));
    }

    private IEnumerator RutinaAviso(string mensaje, Color color, float duracion)
    {
        textoAviso.text = mensaje;
        textoAviso.color = color;
        RectTransform rt = textoAviso.rectTransform;

        float t = 0f;
        const float entrada = 0.25f;
        while (t < entrada)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / entrada);
            textoAviso.alpha = k;
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(1.5f, 1f, EaseOutBack(k));
            yield return null;
        }
        textoAviso.alpha = 1f;
        rt.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(duracion);

        t = 0f;
        const float salida = 0.35f;
        while (t < salida)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / salida);
            textoAviso.alpha = 1f - k;
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, k);
            yield return null;
        }
        textoAviso.alpha = 0f;
        rutinaAviso = null;
    }

    private IEnumerator PulsoTexto(TMP_Text texto, Color colorBase, Color colorPulso, float escala)
    {
        if (texto == null) yield break;
        RectTransform rt = texto.rectTransform;
        float t = 0f;
        const float d = 0.3f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            float s = Mathf.Sin(k * Mathf.PI);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, escala, s);
            texto.color = Color.Lerp(colorBase, colorPulso, s);
            yield return null;
        }
        rt.localScale = Vector3.one;
        texto.color = colorBase;
    }

    private IEnumerator AnimarCeldaPerdida(Image icono)
    {
        RectTransform rt = icono.rectTransform;
        Vector2 posicionBase = rt.anchoredPosition;
        float t = 0f;
        const float d = 0.5f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            rt.anchoredPosition = posicionBase + new Vector2(Mathf.Sin(k * 40f) * 10f * (1f - k), 0f);
            rt.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, k);
            icono.color = Color.Lerp(colorVidaCritica, colorVidaVacia, k);
            yield return null;
        }
        rt.anchoredPosition = posicionBase;
        rt.localScale = Vector3.one;
        icono.color = colorVidaVacia;
    }

    private IEnumerator PulsoCritico(Image icono)
    {
        RectTransform rt = icono.rectTransform;
        while (true)
        {
            float s = (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.5f;
            icono.color = Color.Lerp(colorVidaLlena, colorVidaCritica, s);
            rt.localScale = Vector3.one * (1f + 0.12f * s);
            yield return null;
        }
    }

    private IEnumerator Flash()
    {
        if (flashDanio == null) yield break;
        float t = 0f;
        const float d = 0.4f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            PonerAlfa(flashDanio, Mathf.Lerp(alfaFlash, 0f, t / d));
            yield return null;
        }
        PonerAlfa(flashDanio, 0f);
        rutinaFlash = null;
    }

    private IEnumerator LatidoInsignia()
    {
        while (insigniaRecord != null && insigniaRecord.gameObject.activeInHierarchy)
        {
            float s = 1f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f);
            insigniaRecord.localScale = Vector3.one * s;
            yield return null;
        }
    }

    private IEnumerator Fundido(CanvasGroup grupo, float desde, float hasta, float d)
    {
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / d);
            yield return null;
        }
        grupo.alpha = hasta;
    }

    private static void PonerAlfa(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}