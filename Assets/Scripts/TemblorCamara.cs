using UnityEngine;

/// <summary>Temblor de cámara al recibir un impacto. Va en Main Camera.</summary>
public class TemblorCamara : MonoBehaviour
{
    [SerializeField] private float duracion = 0.3f;
    [SerializeField] private float intensidad = 0.2f;

    private Vector3 posicionBase;
    private float tiempoRestante;
    private float intensidadActual;

    private void Start()
    {
        posicionBase = transform.localPosition;
    }

    public void Temblar(float multiplicador = 1f)
    {
        tiempoRestante = duracion;
        intensidadActual = intensidad * multiplicador;
    }

    private void LateUpdate()
    {
        if (tiempoRestante <= 0f) return;

        tiempoRestante -= Time.unscaledDeltaTime;
        if (tiempoRestante <= 0f)
        {
            transform.localPosition = posicionBase;
            return;
        }

        float factor = tiempoRestante / duracion;
        transform.localPosition = posicionBase + Random.insideUnitSphere * intensidadActual * factor;
    }
}