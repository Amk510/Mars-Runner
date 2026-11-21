using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controles del astronauta: Flecha Arriba = salto, Flecha Abajo = deslizarse (reduce el collider).
/// Va en el objeto padre "Astronauta" (junto al Rigidbody y al Capsule Collider).
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class ControlJugador : MonoBehaviour
{
    [Header("Salto (Flecha Arriba)")]
    [Tooltip("Altura máxima del salto en metros.")]
    [SerializeField] private float alturaSalto = 2.2f;
    [Tooltip("Gravedad propia del personaje (m/s²). Menor = salto más flotante.")]
    [SerializeField] private float gravedad = 18f;
    [Tooltip("Velocidad de caída si se pulsa Flecha Abajo en el aire.")]
    [SerializeField] private float velocidadCaidaRapida = 15f;

    [Header("Agacharse / Deslizarse (Flecha Abajo)")]
    [Tooltip("Segundos que el personaje permanece agachado.")]
    [SerializeField] private float duracionAgachado = 1f;
    [Tooltip("Proporción de la altura del collider al agacharse (0.5 = mitad).")]
    [Range(0.2f, 1f)]
    [SerializeField] private float factorAlturaAgachado = 0.5f;
    [Tooltip("Clip de la animación de deslizarse (para ajustar su duración).")]
    [SerializeField] private AnimationClip clipDeslizarse;

    [Header("Detección de suelo")]
    [SerializeField] private LayerMask capaSuelo;
    [SerializeField] private float radioDeteccionSuelo = 0.25f;

    [Header("Animaciones (se pueden desactivar en la defensa)")]
    [SerializeField] private bool usarAnimacionSalto = true;
    [SerializeField] private bool usarAnimacionDeslizar = true;
    [SerializeField] private bool usarAnimacionMuerte = true;

    [Header("Nombres de parámetros del Animator")]
    [SerializeField] private string paramSalto = "Jump";
    [SerializeField] private string paramDeslizarse = "Deslizarse";
    [SerializeField] private string paramMuerte = "Muerte";
    [SerializeField] private string paramVelDeslizar = "VelDeslizar";

    private Rigidbody rb;
    private CapsuleCollider capsula;
    private Animator animator;

    private float alturaOriginal;
    private Vector3 centroOriginal;
    private bool saltoPendiente;
    private bool caidaPendiente;
    private float tiempoUltimoSalto = -10f;
    private Coroutine rutinaAgachado;

    public bool EnSuelo { get; private set; }
    public bool Agachado { get; private set; }
    public bool Muerto { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsula = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionX |
                         RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotation;

        alturaOriginal = capsula.height;
        centroOriginal = capsula.center;
    }

    private void Update()
    {
        if (Muerto) return;

        EnSuelo = ComprobarSuelo();

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        // Flecha Arriba: salto
        if (teclado.upArrowKey.wasPressedThisFrame && EnSuelo && !Agachado &&
            Time.time - tiempoUltimoSalto > 0.2f)
        {
            saltoPendiente = true;
            tiempoUltimoSalto = Time.time;
        }

        // Flecha Abajo: deslizarse en el suelo o caída rápida en el aire
        if (teclado.downArrowKey.wasPressedThisFrame)
        {
            if (EnSuelo)
            {
                if (!Agachado) rutinaAgachado = StartCoroutine(Agacharse());
            }
            else
            {
                caidaPendiente = true;
            }
        }
    }

    private void FixedUpdate()
    {
        // Gravedad propia (reducida, estilo Marte)
        rb.AddForce(Vector3.down * gravedad, ForceMode.Acceleration);

        if (saltoPendiente)
        {
            saltoPendiente = false;
            Vector3 v = rb.linearVelocity;
            v.y = Mathf.Sqrt(2f * gravedad * alturaSalto);   // velocidad para alcanzar alturaSalto
            rb.linearVelocity = v;

            if (usarAnimacionSalto && animator != null) animator.SetTrigger(paramSalto);
        }

        if (caidaPendiente)
        {
            caidaPendiente = false;
            Vector3 v = rb.linearVelocity;
            v.y = -velocidadCaidaRapida;
            rb.linearVelocity = v;
        }
    }

    private IEnumerator Agacharse()
    {
        Agachado = true;

        // Reducir el collider proporcionalmente manteniendo la base en los pies
        float baseCapsula = centroOriginal.y - alturaOriginal * 0.5f;
        float nuevaAltura = alturaOriginal * factorAlturaAgachado;
        capsula.height = nuevaAltura;
        capsula.center = new Vector3(centroOriginal.x, baseCapsula + nuevaAltura * 0.5f, centroOriginal.z);

        if (usarAnimacionDeslizar && animator != null)
        {
            float velocidad = (clipDeslizarse != null && duracionAgachado > 0f)
                ? clipDeslizarse.length / duracionAgachado
                : 1f;
            animator.SetFloat(paramVelDeslizar, velocidad);
            animator.SetTrigger(paramDeslizarse);
        }

        yield return new WaitForSeconds(duracionAgachado);

        RestaurarCollider();
        Agachado = false;
        rutinaAgachado = null;
    }

    private void RestaurarCollider()
    {
        capsula.height = alturaOriginal;
        capsula.center = centroOriginal;
    }

    private bool ComprobarSuelo()
    {
        Vector3 punto = transform.position + Vector3.up * (radioDeteccionSuelo - 0.05f);
        return Physics.CheckSphere(punto, radioDeteccionSuelo, capaSuelo, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Lo llama el sistema de vidas (Hito 5) cuando las vidas llegan a 0.</summary>
    public void Morir()
    {
        if (Muerto) return;
        Muerto = true;

        if (rutinaAgachado != null)
        {
            StopCoroutine(rutinaAgachado);
            rutinaAgachado = null;
        }
        RestaurarCollider();
        Agachado = false;

        if (animator != null)
        {
            animator.ResetTrigger(paramSalto);
            animator.ResetTrigger(paramDeslizarse);
            if (usarAnimacionMuerte) animator.SetTrigger(paramMuerte);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * (radioDeteccionSuelo - 0.05f), radioDeteccionSuelo);
    }
}