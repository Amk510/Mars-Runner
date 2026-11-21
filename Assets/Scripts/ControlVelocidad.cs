using UnityEngine;

/// <summary>
/// Controla la velocidad global del juego (escenario y obstáculos) y la dificultad progresiva.
/// Va en el objeto GameManager. Los demás scripts leen ControlVelocidad.Instancia.VelocidadActual.
/// </summary>
public class ControlVelocidad : MonoBehaviour
{
    public static ControlVelocidad Instancia { get; private set; }

    [Header("Velocidad del escenario y obstáculos (m/s)")]
    [Tooltip("Velocidad con la que empieza la partida.")]
    [SerializeField] private float velocidadInicial = 8f;
    [Tooltip("Velocidad máxima que se puede alcanzar.")]
    [SerializeField] private float velocidadMaxima = 18f;

    [Header("Dificultad progresiva")]
    [Tooltip("Cuánto aumenta la velocidad en cada incremento.")]
    [SerializeField] private float incrementoVelocidad = 1f;
    [Tooltip("Cada cuántos segundos se aplica un incremento.")]
    [SerializeField] private float intervaloIncremento = 10f;

    [Header("Estado (solo lectura, para ver en Play)")]
    [SerializeField] private float velocidadActualVisible;

    private int nivel;
    private float temporizador;
    private bool enMovimiento = true;

    /// <summary>Velocidad actual en m/s (0 si el juego se detuvo).</summary>
    public float VelocidadActual =>
        enMovimiento ? Mathf.Min(velocidadInicial + nivel * incrementoVelocidad, velocidadMaxima) : 0f;

    /// <summary>0 = velocidad inicial, 1 = velocidad máxima. Lo usa el generador de obstáculos.</summary>
    public float Progreso => Mathf.InverseLerp(velocidadInicial, velocidadMaxima, VelocidadActual);

    public bool EnMovimiento => enMovimiento;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this);
            return;
        }
        Instancia = this;
    }

    private void Update()
    {
        if (!enMovimiento) return;

        if (intervaloIncremento > 0f)
        {
            temporizador += Time.deltaTime;
            if (temporizador >= intervaloIncremento)
            {
                temporizador -= intervaloIncremento;
                nivel++;
            }
        }

        velocidadActualVisible = VelocidadActual;
    }

    /// <summary>Detiene todo el movimiento (Game Over o Victoria).</summary>
    public void Detener()
    {
        enMovimiento = false;
        velocidadActualVisible = 0f;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }
}