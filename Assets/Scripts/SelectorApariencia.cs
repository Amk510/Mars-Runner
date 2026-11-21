using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cambia material o color del personaje y de los obstáculos en tiempo de ejecución,
/// sin modificar los archivos de material. Va en el objeto GameManager.
/// Tecla C = siguiente material del personaje. Tecla V = alterna el material de los obstáculos.
/// </summary>
public class SelectorApariencia : MonoBehaviour
{
    [Header("Personaje")]
    [SerializeField] private Renderer[] renderersPersonaje;
    [SerializeField] private Material[] materialesPersonaje;
    [SerializeField] private int indiceMaterialPersonaje = 0;
    [Tooltip("Activa para teñir al personaje con el color de abajo (se puede cambiar en Play).")]
    [SerializeField] private bool aplicarTintePersonaje = false;
    [SerializeField] private Color tintePersonaje = new Color(1f, 0.6f, 0.6f);

    [Header("Obstáculos")]
    [SerializeField] private Transform contenedorObstaculos;
    [SerializeField] private Material materialAlternativoBajos;
    [SerializeField] private Material materialAlternativoAltos;
    [SerializeField] private bool usarMaterialAlternativo = false;
    [Tooltip("Activa para teñir los obstáculos con el color de abajo (se puede cambiar en Play).")]
    [SerializeField] private bool aplicarTinteObstaculos = false;
    [SerializeField] private Color tinteObstaculos = new Color(0.4f, 0.8f, 1f);

    [Header("Teclas")]
    [SerializeField] private Key teclaMaterialPersonaje = Key.C;
    [SerializeField] private Key teclaMaterialObstaculos = Key.V;

    private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock bloque;
    private readonly Dictionary<Renderer, Material[]> materialesOriginales = new Dictionary<Renderer, Material[]>();
    private readonly List<MeshRenderer> buffer = new List<MeshRenderer>();
    private int indiceAplicado = -1;

    private void Awake()
    {
        bloque = new MaterialPropertyBlock();
    }

    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (teclaMaterialPersonaje != Key.None && teclado[teclaMaterialPersonaje].wasPressedThisFrame &&
            materialesPersonaje != null && materialesPersonaje.Length > 0)
        {
            indiceMaterialPersonaje = (indiceMaterialPersonaje + 1) % materialesPersonaje.Length;
        }

        if (teclaMaterialObstaculos != Key.None && teclado[teclaMaterialObstaculos].wasPressedThisFrame)
        {
            usarMaterialAlternativo = !usarMaterialAlternativo;
        }
    }

    private void LateUpdate()
    {
        AplicarPersonaje();
        AplicarObstaculos();
    }

    private void AplicarPersonaje()
    {
        if (renderersPersonaje == null) return;

        if (materialesPersonaje != null && materialesPersonaje.Length > 0)
        {
            int i = Mathf.Clamp(indiceMaterialPersonaje, 0, materialesPersonaje.Length - 1);
            if (i != indiceAplicado && materialesPersonaje[i] != null)
            {
                foreach (Renderer r in renderersPersonaje)
                {
                    if (r != null) r.sharedMaterial = materialesPersonaje[i];
                }
                indiceAplicado = i;
            }
        }

        foreach (Renderer r in renderersPersonaje)
        {
            if (r != null) AplicarTinte(r, aplicarTintePersonaje, tintePersonaje);
        }
    }

    private void AplicarObstaculos()
    {
        if (contenedorObstaculos == null) return;

        foreach (Transform hijo in contenedorObstaculos)
        {
            Obstaculo obstaculo = hijo.GetComponent<Obstaculo>();
            Material alternativo = (obstaculo != null && obstaculo.Tipo == TipoObstaculo.Alto)
                ? materialAlternativoAltos
                : materialAlternativoBajos;

            hijo.GetComponentsInChildren(buffer);
            foreach (MeshRenderer r in buffer)
            {
                if (usarMaterialAlternativo && alternativo != null)
                {
                    if (!materialesOriginales.ContainsKey(r)) materialesOriginales[r] = r.sharedMaterials;
                    if (r.sharedMaterial != alternativo)
                    {
                        Material[] nuevos = new Material[r.sharedMaterials.Length];
                        for (int k = 0; k < nuevos.Length; k++) nuevos[k] = alternativo;
                        r.sharedMaterials = nuevos;
                    }
                }
                else if (materialesOriginales.TryGetValue(r, out Material[] originales))
                {
                    r.sharedMaterials = originales;
                    materialesOriginales.Remove(r);
                }

                AplicarTinte(r, aplicarTinteObstaculos, tinteObstaculos);
            }
        }

        if (materialesOriginales.Count > 100) LimpiarDestruidos();
    }

    private void AplicarTinte(Renderer r, bool aplicar, Color color)
    {
        if (aplicar)
        {
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdBaseColor, color);
            r.SetPropertyBlock(bloque);
        }
        else if (r.HasPropertyBlock())
        {
            r.SetPropertyBlock(null);
        }
    }

    private void LimpiarDestruidos()
    {
        List<Renderer> borrar = new List<Renderer>();
        foreach (Renderer r in materialesOriginales.Keys) if (r == null) borrar.Add(r);
        foreach (Renderer r in borrar) materialesOriginales.Remove(r);
    }
}