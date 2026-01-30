using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Handles Spider interaction:
/// 1. Hover -> Highlight (Emission).
/// 2. Select (Grab/Trigger) -> Mount the spider (Enable controller).
/// Requires XR Simple Interactable on the GameObject.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class SpiderInteraction : MonoBehaviour
{
    [Header("Components")]
    public SpiderPlayerController spiderController;
    public Renderer[] renderersToHighlight;

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    public float emissionIntensity = 1.0f;
    private Color originalEmissionColor;
    private bool isHovered = false;

    private void Start()
    {
        if (spiderController == null)
            spiderController = GetComponent<SpiderPlayerController>();

        if (renderersToHighlight == null || renderersToHighlight.Length == 0)
            renderersToHighlight = GetComponentsInChildren<Renderer>();
        
        // Setup Interactable Events
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelect);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        isHovered = true;
        SetHighlight(true);
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        isHovered = false;
        SetHighlight(false);
    }

    private void OnSelect(SelectEnterEventArgs args)
    {
        if (spiderController != null)
        {
            spiderController.Mount();
            // Optional: Disable highlight once mounted or keep it?
            SetHighlight(false); 
        }
    }

    private void SetHighlight(bool active)
    {
        if (renderersToHighlight == null) return;

        foreach (var r in renderersToHighlight)
        {
            if (r == null) continue;
            
            foreach (var mat in r.materials)
            {
                if (active)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", highlightColor * emissionIntensity);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}
