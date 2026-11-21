using UnityEngine;

/// <summary>
/// Mueve los tramos hacia -Z a la velocidad del juego y recoloca delante el que queda atrás.
/// Sirve para el suelo (factor 1) y para capas lejanas como montañas (factor menor que 1 = paralaje).
/// </summary>
public class SueloInfinito : MonoBehaviour
{
    [Tooltip("Tramos que forman el bucle (arrastra aquí los hijos).")]
    [SerializeField] private Transform[] tramos;

    [Tooltip("Largo de cada tramo en el eje Z (metros).")]
    [SerializeField] private float largoTramo = 100f;

    [Tooltip("Cuando un tramo llega a esta Z, se recoloca delante del más adelantado.")]
    [SerializeField] private float limiteTrasero = -200f;

    [Tooltip("1 = misma velocidad que el juego. Menor que 1 = se mueve más lento (efecto de lejanía).")]
    [Range(0f, 1f)]
    [SerializeField] private float factorVelocidad = 1f;

    private void Update()
    {
        if (ControlVelocidad.Instancia == null || tramos == null || tramos.Length == 0) return;

        float desplazamiento = ControlVelocidad.Instancia.VelocidadActual * factorVelocidad * Time.deltaTime;
        if (desplazamiento <= 0f) return;

        for (int i = 0; i < tramos.Length; i++)
        {
            tramos[i].position += Vector3.back * desplazamiento;
        }

        for (int i = 0; i < tramos.Length; i++)
        {
            if (tramos[i].position.z <= limiteTrasero)
            {
                Vector3 p = tramos[i].position;
                tramos[i].position = new Vector3(p.x, p.y, ZMaxima() + largoTramo);
            }
        }
    }

    private float ZMaxima()
    {
        float max = float.MinValue;
        for (int i = 0; i < tramos.Length; i++)
        {
            if (tramos[i].position.z > max) max = tramos[i].position.z;
        }
        return max;
    }
}