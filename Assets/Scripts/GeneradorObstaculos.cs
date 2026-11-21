using UnityEngine;

/// <summary>
/// Genera obstáculos aleatorios (tipo e intervalo) fuera de pantalla y define desde un solo lugar
/// la distancia a la que se eliminan. Los obstáculos se crean como hijos de este objeto.
/// </summary>
public class GeneradorObstaculos : MonoBehaviour
{
    [Header("Prefabs de obstáculos")]
    [Tooltip("Obstáculos bajos (se saltan).")]
    [SerializeField] private GameObject[] prefabsBajos;
    [Tooltip("Obstáculos altos (se esquivan agachándose).")]
    [SerializeField] private GameObject[] prefabsAltos;
    [Tooltip("Probabilidad de que salga un obstáculo alto (0 = nunca, 1 = siempre).")]
    [Range(0f, 1f)]
    [SerializeField] private float probabilidadAlto = 0.4f;

    [Header("Intervalo de aparición (segundos)")]
    [Tooltip("Rango aleatorio al inicio de la partida.")]
    [SerializeField] private float intervaloMinimoInicial = 1.4f;
    [SerializeField] private float intervaloMaximoInicial = 2.6f;
    [Tooltip("Rango aleatorio al llegar a la velocidad máxima (más corto = más difícil).")]
    [SerializeField] private float intervaloMinimoFinal = 0.8f;
    [SerializeField] private float intervaloMaximoFinal = 1.5f;
    [Tooltip("Segundos antes del primer obstáculo.")]
    [SerializeField] private float retrasoInicial = 2f;
    [Tooltip("Distancia mínima en metros entre dos obstáculos (evita combinaciones imposibles).")]
    [SerializeField] private float distanciaMinima = 9f;

    [Header("Posiciones")]
    [Tooltip("Z donde aparecen (fuera de pantalla, a la derecha).")]
    [SerializeField] private float zAparicion = 40f;
    [Tooltip("X del camino por donde corre el astronauta.")]
    [SerializeField] private float xCarril = 0f;
    [Tooltip("Z donde se eliminan (detrás del personaje, fuera de pantalla).")]
    [SerializeField] private float limiteEliminacion = -15f;
    [Tooltip("Gira al azar las rocas en Y para que no se vean todas iguales.")]
    [SerializeField] private bool rotacionAleatoriaBajos = true;

    [Header("Estado (solo lectura, para la evidencia)")]
    [SerializeField] private int obstaculosGenerados;
    [SerializeField] private int obstaculosActivos;
    [SerializeField] private int obstaculosEliminados;
    [SerializeField] private float proximoIntervalo;

    private float temporizador;
    private float limiteAplicado;

    private void Start()
    {
        temporizador = retrasoInicial;
        proximoIntervalo = retrasoInicial;
        limiteAplicado = limiteEliminacion;
    }

    private void Update()
    {
        ActualizarEstadisticas();

        // Si se cambia el límite en pleno Play, se aplica también a los obstáculos ya creados
        if (!Mathf.Approximately(limiteEliminacion, limiteAplicado))
        {
            AplicarLimiteATodos();
        }

        ControlVelocidad cv = ControlVelocidad.Instancia;
        if (cv == null || !cv.EnMovimiento) return;

        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            GenerarObstaculo();
            proximoIntervalo = CalcularIntervalo(cv);
            temporizador = proximoIntervalo;
        }
    }

    private float CalcularIntervalo(ControlVelocidad cv)
    {
        float progreso = cv.Progreso; // 0 al inicio, 1 en velocidad máxima
        float min = Mathf.Lerp(intervaloMinimoInicial, intervaloMinimoFinal, progreso);
        float max = Mathf.Lerp(intervaloMaximoInicial, intervaloMaximoFinal, progreso);
        if (max < min) max = min;

        float intervalo = Random.Range(min, max);

        float velocidad = cv.VelocidadActual;
        if (velocidad > 0.01f)
        {
            intervalo = Mathf.Max(intervalo, distanciaMinima / velocidad);
        }
        return intervalo;
    }

    private void GenerarObstaculo()
    {
        bool hayBajos = prefabsBajos != null && prefabsBajos.Length > 0;
        bool hayAltos = prefabsAltos != null && prefabsAltos.Length > 0;
        if (!hayBajos && !hayAltos) return;

        bool esAlto = hayAltos && (!hayBajos || Random.value < probabilidadAlto);
        GameObject[] lista = esAlto ? prefabsAltos : prefabsBajos;
        GameObject prefab = lista[Random.Range(0, lista.Length)];
        if (prefab == null) return;

        Vector3 posicion = new Vector3(xCarril, prefab.transform.position.y, zAparicion);
        Quaternion rotacion = prefab.transform.rotation;
        if (!esAlto && rotacionAleatoriaBajos)
        {
            rotacion = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * rotacion;
        }

        GameObject nuevo = Instantiate(prefab, posicion, rotacion, transform);

        Obstaculo obstaculo = nuevo.GetComponent<Obstaculo>();
        if (obstaculo != null) obstaculo.Configurar(limiteEliminacion);

        obstaculosGenerados++;
    }

    private void AplicarLimiteATodos()
    {
        foreach (Transform hijo in transform)
        {
            Obstaculo obstaculo = hijo.GetComponent<Obstaculo>();
            if (obstaculo != null) obstaculo.Configurar(limiteEliminacion);
        }
        limiteAplicado = limiteEliminacion;
    }

    private void ActualizarEstadisticas()
    {
        obstaculosActivos = transform.childCount;
        obstaculosEliminados = obstaculosGenerados - obstaculosActivos;
    }

    private void OnDrawGizmos()
    {
        // Verde = zona de aparición, Rojo = límite de eliminación
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(xCarril, 1.5f, zAparicion), new Vector3(4f, 3f, 0.1f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(xCarril, 1.5f, limiteEliminacion), new Vector3(4f, 3f, 0.1f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(xCarril, 0.05f, zAparicion), new Vector3(xCarril, 0.05f, limiteEliminacion));
    }
}