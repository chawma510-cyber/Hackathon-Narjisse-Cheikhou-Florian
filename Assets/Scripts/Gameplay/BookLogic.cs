using UnityEngine;
using TMPro;
using HackathonVR.Interactions;
using UnityEngine.Events;

namespace HackathonVR.Gameplay
{
    public class BookLogic : MonoBehaviour
    {
        [TextArea(3, 10)]
        public string loreText = "Dernières notes de grand-père, il y a exactement 1 an, le jour de sa disparition, alors qu'il était parti jeter un coup d'oeil dans son télescope : \"J'adorais la légende du télescope, c'était toujours un plaisir de rêver à ce que je pourrais voir à travers. Le vieux Chaman me disait toujours que les plus belles surprises se trouvaient là où on s'y attendait le moins. En suivant sa logique je devrais prendre un télescope pour regarder le sol ... Ce ne sont que des dictons après tout. Ma foi pourquoi ne pas essayer ahah!\"";
        
        public UnityEvent onBookClosed;

        private VRGrabInteractable interactable;
        private GameObject tooltipObj;
        private GameObject lorePanelObj;
        private bool isReading = false;
        
        // Floating animation
        private Vector3 startPos;
        private float floatSpeed = 1f;
        private float floatAmplitude = 0.1f;

        private void Start()
        {
            interactable = GetComponent<VRGrabInteractable>();
            startPos = transform.position;
            
            // Create Tooltip "Lire (A)"
            CreateTooltip();
            
            // Create Lore Panel (Hidden by default)
            CreateLorePanel();
            
            // Subscribe to events
            if (interactable != null)
            {
                interactable.OnHoverEnter.AddListener(OnHoverEnter);
                interactable.OnHoverExit.AddListener(OnHoverExit);
                interactable.OnGrabbed.AddListener(OnGrabbed);
                interactable.OnReleased.AddListener(OnReleased);
            }
        }

        private void Update()
        {
            // Floating Animation (only when not held)
            if (interactable != null && !interactable.IsGrabbed)
            {
                float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Face camera for tooltip
            if (tooltipObj != null && tooltipObj.activeSelf)
            {
                tooltipObj.transform.LookAt(Camera.main.transform);
                tooltipObj.transform.Rotate(0, 180, 0); // Fix mirrored
            }
            
            // Read Input (A Button)
            if (isReading)
            {
                // If user presses A again (or B/X/Y generic close interaction), close book
                // For simplicity, we can close it if they release/drop it, OR use input.
                // Let's rely on Drop/Release to close for now, or add a 'Close' button in UI.
            }
        }
        
        private void CreateTooltip()
        {
            tooltipObj = new GameObject("Tooltip_Lire");
            tooltipObj.transform.SetParent(transform);
            tooltipObj.transform.localPosition = new Vector3(0, 0.2f, 0); 
            tooltipObj.transform.localScale = Vector3.one * 0.15f; 
            
            var canvas = tooltipObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 50); 
            
            var txt = tooltipObj.AddComponent<TextMeshProUGUI>();
            txt.text = "Lire (A)"; 
            txt.fontSize = 24;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            
            tooltipObj.SetActive(false);
        }
        
        private void CreateLorePanel()
        {
            lorePanelObj = new GameObject("LorePanel");
            lorePanelObj.transform.SetParent(transform);
            lorePanelObj.transform.localPosition = new Vector3(0, 0.3f, 0.1f); 
            lorePanelObj.transform.localRotation = Quaternion.Euler(-30, 0, 0); 
            lorePanelObj.transform.localScale = Vector3.one * 0.2f;
            
            var canvas = lorePanelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 300);
            
            // Background
            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(lorePanelObj.transform, false);
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0.9f);
            bgObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            
            // Text
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(lorePanelObj.transform, false);
            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = loreText;
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = true;
            
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(20, 20);
            txtRt.offsetMax = new Vector2(-20, -20);
            
            lorePanelObj.SetActive(false);
        }
        
        private void OnHoverEnter()
        {
            if (tooltipObj != null && !interactable.IsGrabbed)
                tooltipObj.SetActive(true);
        }
        
        private void OnHoverExit()
        {
            if (tooltipObj != null)
                tooltipObj.SetActive(false);
        }
        
        private void OnGrabbed()
        {
            // When grabbed, show story
            if (tooltipObj != null) tooltipObj.SetActive(false);
            if (lorePanelObj != null) lorePanelObj.SetActive(true);
            isReading = true;
        }
        
        private void OnReleased()
        {
            // When released, hide story and trigger event
            if (lorePanelObj != null && lorePanelObj.activeSelf)
            {
                lorePanelObj.SetActive(false);
                onBookClosed?.Invoke();
                Debug.Log("[BookLogic] Book closed/released - Triggering next event.");
            }
            isReading = false;
        }
    }
}
