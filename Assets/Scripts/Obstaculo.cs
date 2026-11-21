using System.Diagnostics;
using UnityEngine;

public enum TipoObstaculo { Bajo, Alto }

/// <summary>
/// Va en la raíz de cada prefab de obstáculo. Lo mueve hacia -Z a la velocidad del juego
/// y lo elimina cuando queda detrás del personaje (límite configurable por el generador).
/// </summary>
public class Obstaculo : MonoBehaviour
{
    [Tooltip("Bajo = se salta (rocas). Alto = se esquiva agachándose (meteoritos).")]
    [SerializeField] private TipoObstaculo tipo = TipoObstaculo.Bajo;

    [Tooltip("Multiplica la velocidad del juego solo para este obstáculo (1 = igual que el suelo).")]
    [SerializeField] private float multiplicadorVelocidad = 1f;

    [Tooltip("Z a partir de la cual se elimina. El generador (Hito 4) sobrescribe este valor.")]
    [SerializeField] private float limiteEliminacion = -15f;

    [Header("Giro visual (opcional, para meteoritos)")]
    [SerializeField] private Transform modeloVisual;
    [SerializeField] private Vector3 velocidadGiro = Vector3.zero;

    public TipoObstaculo Tipo => tipo;
    private bool yaSuperado = false;

    /// <summary>Lo llama el generador al crear el obstáculo.</summary>
    public void Configurar(float limite)
    {
        limiteEliminacion = limite;
    }

    private void Update()
    {
        float velocidad = ControlVelocidad.Instancia != null ? ControlVelocidad.Instancia.VelocidadActual : 0f;

        transform.position += Vector3.back * velocidad * multiplicadorVelocidad * Time.deltaTime;

        if (modeloVisual != null && velocidad > 0f && velocidadGiro != Vector3.zero)
        {
            modeloVisual.Rotate(velocidadGiro * Time.deltaTime, Space.Self);
        }
        /*
        if (!yaSuperado && transform.position.z < 0f)
        {
            yaSuperado = true;
            Debug.Log("Obstáculo superado: " + gameObject.name + " (" + tipo + ")");
        }*/

        if (transform.position.z < limiteEliminacion)
        {
            Destroy(gameObject);
        }
    }
}