using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Lógica de partida: vidas, invulnerabilidad, puntaje, récord, Game Over y Victoria.
/// Va en el objeto GameManager (junto a ControlVelocidad y SelectorApariencia).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    [Header("Referencias")]
    [SerializeField] private ControlJugador jugador;
    [SerializeField] private VidaJugador vidaJugador;
    [SerializeField] private UIJuego ui;
    [SerializeField] private GeneradorObstaculos generador;
    [SerializeField] private TemblorCamara temblorCamara;

    [Header("Vidas")]
    [Tooltip("Vidas con las que empieza la partida.")]
    [Min(1)][SerializeField] private int vidasIniciales = 3;
    [Tooltip("Segundos de invulnerabilidad después de un choque.")]
    [SerializeField] private float tiempoInvulnerabilidad = 2f;

    [Header("Puntaje")]
    [Tooltip("Puntos que se ganan por cada segundo de supervivencia.")]
    [SerializeField] private float puntosPorSegundo = 10f;
    [Tooltip("Si está activo, se gana al llegar a 'Puntos Victoria'.")]
    [SerializeField] private bool habilitarVictoria = true;
    [Min(1)][SerializeField] private int puntosVictoria = 1000;

    [Header("Fin de partida")]
    [Tooltip("Segundos de espera antes del panel de Game Over (para ver la animación de Muerte).")]
    [SerializeField] private float retrasoPanelGameOver = 1.6f;

    [Header("Estado (solo lectura)")]
    [SerializeField] private int vidasActuales;
    [SerializeField] private int puntosActuales;
    [SerializeField] private bool invulnerable;
    [SerializeField] private bool partidaTerminada;

    private const string ClaveRecord = "MarsRunner_Record";
    private float puntos;
    private int record;
    private bool avisoRecordMostrado;
    private float finInvulnerabilidad;
    private bool reiniciando;

    public bool PartidaTerminada => partidaTerminada;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this);
            return;
        }
        Instancia = this;
        record = PlayerPrefs.GetInt(ClaveRecord, 0);
        vidasActuales = vidasIniciales;
    }

    private void Start()
    {
        if (ui != null) ui.Inicializar(vidasIniciales, record, habilitarVictoria ? puntosVictoria : 0);
    }

    private void Update()
    {
        if (partidaTerminada)
        {
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.rKey.wasPressedThisFrame && ui != null && ui.PanelFinInteractivo)
            {
                Reiniciar();
            }
            return;
        }

        if (invulnerable && Time.time >= finInvulnerabilidad) invulnerable = false;

        puntos += puntosPorSegundo * Time.deltaTime;
        if (habilitarVictoria && puntos >= puntosVictoria) puntos = puntosVictoria;
        puntosActuales = Mathf.FloorToInt(puntos);

        if (ui != null) ui.ActualizarPuntos(puntosActuales);

        if (!avisoRecordMostrado && record > 0 && puntosActuales > record)
        {
            avisoRecordMostrado = true;
            if (ui != null) ui.AvisoNuevoRecord();
        }

        if (habilitarVictoria && puntosActuales >= puntosVictoria) Victoria();
    }

    /// <summary>Lo llama VidaJugador cuando el astronauta toca un obstáculo.</summary>
    public void RecibirImpacto()
    {
        if (partidaTerminada || invulnerable) return;

        vidasActuales = Mathf.Max(0, vidasActuales - 1);

        if (ui != null) ui.PerderVida(vidasActuales);
        if (temblorCamara != null) temblorCamara.Temblar(vidasActuales <= 0 ? 2f : 1f);

        if (vidasActuales <= 0)
        {
            GameOver();
            return;
        }

        invulnerable = true;
        finInvulnerabilidad = Time.time + tiempoInvulnerabilidad;
        if (vidaJugador != null) vidaJugador.Parpadear(tiempoInvulnerabilidad);
    }

    private void GameOver()
    {
        partidaTerminada = true;
        invulnerable = false;

        if (vidaJugador != null) vidaJugador.DetenerParpadeo();
        if (jugador != null) jugador.Morir();
        if (ControlVelocidad.Instancia != null) ControlVelocidad.Instancia.Detener();

        bool nuevoRecord = GuardarRecord();
        StartCoroutine(MostrarGameOverConRetraso(nuevoRecord));
    }

    private IEnumerator MostrarGameOverConRetraso(bool nuevoRecord)
    {
        yield return new WaitForSeconds(retrasoPanelGameOver);
        if (ui != null) ui.MostrarFin(false, puntosActuales, record, nuevoRecord);
    }

    private void Victoria()
    {
        partidaTerminada = true;
        invulnerable = false;

        if (vidaJugador != null) vidaJugador.DetenerParpadeo();

        // Deja de generar y limpia el camino: el astronauta sigue corriendo hacia la base
        if (generador != null)
        {
            generador.enabled = false;
            for (int i = generador.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(generador.transform.GetChild(i).gameObject);
            }
        }

        bool nuevoRecord = GuardarRecord();
        if (ui != null) ui.MostrarFin(true, puntosActuales, record, nuevoRecord);
    }

    private bool GuardarRecord()
    {
        if (puntosActuales > record)
        {
            record = puntosActuales;
            PlayerPrefs.SetInt(ClaveRecord, record);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    public void Reiniciar()
    {
        if (reiniciando) return;
        reiniciando = true;

        Scene escena = SceneManager.GetActiveScene();
        if (escena.buildIndex >= 0) SceneManager.LoadScene(escena.buildIndex);
        else SceneManager.LoadScene(escena.name);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    [ContextMenu("Borrar récord guardado")]
    private void BorrarRecord()
    {
        PlayerPrefs.DeleteKey(ClaveRecord);
        record = 0;
        Debug.Log("Récord borrado.");
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }
}