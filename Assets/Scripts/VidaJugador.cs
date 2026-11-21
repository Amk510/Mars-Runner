using System.Collections;
using UnityEngine;

/// <summary>
/// Detecta los choques con obstáculos y hace parpadear al astronauta durante la invulnerabilidad.
/// Va en el objeto padre "Astronauta".
/// </summary>
public class VidaJugador : MonoBehaviour
{
    [Tooltip("Tag que tienen los obstáculos.")]
    [SerializeField] private string tagObstaculo = "Obstaculo";
    [Tooltip("Segundos entre cada encendido/apagado del parpadeo.")]
    [SerializeField] private float intervaloParpadeo = 0.1f;

    private Renderer[] renderersModelo;
    private Coroutine rutinaParpadeo;

    private void Awake()
    {
        renderersModelo = GetComponentsInChildren<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tagObstaculo)) return;
        if (GameManager.Instancia != null) GameManager.Instancia.RecibirImpacto();
    }

    public void Parpadear(float duracion)
    {
        DetenerParpadeo();
        rutinaParpadeo = StartCoroutine(RutinaParpadeo(duracion));
    }

    public void DetenerParpadeo()
    {
        if (rutinaParpadeo != null) StopCoroutine(rutinaParpadeo);
        rutinaParpadeo = null;
        MostrarModelo(true);
    }

    private IEnumerator RutinaParpadeo(float duracion)
    {
        float fin = Time.time + duracion;
        bool visible = true;
        while (Time.time < fin)
        {
            visible = !visible;
            MostrarModelo(visible);
            yield return new WaitForSeconds(intervaloParpadeo);
        }
        MostrarModelo(true);
        rutinaParpadeo = null;
    }

    private void MostrarModelo(bool visible)
    {
        foreach (Renderer r in renderersModelo)
        {
            if (r != null) r.enabled = visible;
        }
    }
}