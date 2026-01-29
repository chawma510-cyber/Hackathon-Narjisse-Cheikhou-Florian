using UnityEngine;
using UnityEngine.SceneManagement;

namespace HackathonVR.UI
{
    /// <summary>
    /// Main menu manager for VR welcome screen.
    /// Creates floating 3D buttons that can be pressed in VR.
    /// </summary>
    public class VRMainMenu : MonoBehaviour
    {
        [Header("Menu Settings")]
        [SerializeField] private string gameSceneName = "VRScene";
        [SerializeField] private float buttonHeight = 1.3f; // Slightly lower
        [SerializeField] private float buttonDistance = 2f;
        
        [Header("Title Settings")]
        [SerializeField] private string gameTitle = "Mini-Me";
        [SerializeField] private Color titleColor = new Color(0.2f, 0.9f, 1f);
        
        [Header("Button Appearance")]
        [SerializeField] private Color playButtonColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color quitButtonColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color sliderColor = new Color(0.4f, 0.4f, 0.9f);
        
        [Header("Audio")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip buttonClickSound;
        
        // Components
        private GameObject menuParent;
        private AudioSource musicSource;
        private AudioSource sfxSource;
        
        // Volume control
        private Transform volumeKnob;
        private float currentVolume = 0.5f;
        
        private void Start()
        {
            menuParent = new GameObject("MainMenu");
            menuParent.transform.position = new Vector3(0, buttonHeight, buttonDistance);
            
            SetupAudio();
            CreateTitle();
            CreateButtons();
            CreateVolumeControl();
            
            // Try to load music from Resources if not assigned
            if (backgroundMusic == null)
            {
                backgroundMusic = Resources.Load<AudioClip>("Musiques/The Minimoys Overture");
            }
            
            PlayBackgroundMusic();
        }
        
        private void CreateTitle()
        {
            var titleObject = new GameObject("Title");
            titleObject.transform.SetParent(menuParent.transform);
            titleObject.transform.localPosition = new Vector3(0, 0.6f, 0);
            
            // Create 3D text using TextMesh
            var textMesh = titleObject.AddComponent<TextMesh>();
            textMesh.text = gameTitle;
            textMesh.fontSize = 120;
            textMesh.characterSize = 0.02f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = titleColor;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // Add glow effect with second text behind
            var glowObj = new GameObject("TitleGlow");
            glowObj.transform.SetParent(titleObject.transform);
            glowObj.transform.localPosition = new Vector3(0, 0, 0.02f);
            
            var glowMesh = glowObj.AddComponent<TextMesh>();
            glowMesh.text = gameTitle;
            glowMesh.fontSize = 120;
            glowMesh.characterSize = 0.02f;
            glowMesh.anchor = TextAnchor.MiddleCenter;
            glowMesh.alignment = TextAlignment.Center;
            glowMesh.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0.3f);
        }
        
        private void CreateButtons()
        {
            // Create flat play button
            CreateFlatButton("JOUER", playButtonColor, new Vector3(-0.4f, 0, 0), OnPlayPressed);
            
            // Create flat quit button
            CreateFlatButton("QUITTER", quitButtonColor, new Vector3(0.4f, 0, 0), OnQuitPressed);
        }
        
        private void CreateFlatButton(string text, Color color, Vector3 pos, System.Action action)
        {
            var button = new GameObject("Button_" + text);
            button.transform.SetParent(menuParent.transform);
            button.transform.localPosition = pos;
            
            // Flat background (Quad)
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad); // Using Quad for flat look
            bg.transform.SetParent(button.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(0.6f, 0.2f, 1f);
            
            var bgRenderer = bg.GetComponent<Renderer>();
            var bgMat = new Material(Shader.Find("Unlit/Color")); // Unlit for flat design
            bgMat.color = color;
            bgRenderer.material = bgMat;
            
            // Text on top
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.01f);
            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 60;
            textMesh.characterSize = 0.01f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            
            // Interaction collider
            var collider = bg.GetComponent<Collider>();
            collider.isTrigger = true;
            
            // VR interaction
            var behavior = bg.AddComponent<VRMenuButton>();
            behavior.Initialize(action, sfxSource, buttonHoverSound, buttonClickSound);
            behavior.targetRenderer = bgRenderer;
            behavior.normalColor = color;
            behavior.hoverColor = Color.Lerp(color, Color.white, 0.3f);
        }
        
        private void CreateVolumeControl()
        {
            var sliderParent = new GameObject("VolumeControl");
            sliderParent.transform.SetParent(menuParent.transform);
            sliderParent.transform.localPosition = new Vector3(0, -0.3f, 0);
            
            // Label
            var label = new GameObject("Label");
            label.transform.SetParent(sliderParent.transform);
            label.transform.localPosition = new Vector3(-0.5f, 0, 0);
            var text = label.AddComponent<TextMesh>();
            text.text = "VOLUME";
            text.fontSize = 40;
            text.characterSize = 0.01f;
            text.anchor = TextAnchor.MiddleRight;
            text.color = Color.white;
            
            // Slider bar
            var bar = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bar.transform.SetParent(sliderParent.transform);
            bar.transform.localPosition = Vector3.zero;
            bar.transform.localScale = new Vector3(0.8f, 0.02f, 1f);
            var barMat = new Material(Shader.Find("Unlit/Color"));
            barMat.color = Color.gray;
            bar.GetComponent<Renderer>().material = barMat;
            
            // Slider knob (interactive)
            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "VolumeKnob";
            knob.transform.SetParent(sliderParent.transform);
            knob.transform.localPosition = Vector3.zero; // Center (0.5 volume)
            knob.transform.localScale = Vector3.one * 0.08f;
            
            var knobMat = new Material(Shader.Find("Unlit/Color"));
            knobMat.color = sliderColor;
            knob.GetComponent<Renderer>().material = knobMat;
            
            var knobRb = knob.AddComponent<Rigidbody>();
            knobRb.isKinematic = true;
            knobRb.useGravity = false;
            
            // Add interaction script for sliding
            var slider = knob.AddComponent<VRVolumeSlider>();
            slider.Initialize(this, -0.4f, 0.4f);
            
            volumeKnob = knob.transform;
            UpdateVolume(0.5f); // Set initial volume display
        }
        
        public void UpdateVolume(float percent)
        {
            currentVolume = Mathf.Clamp01(percent);
            if (musicSource != null) musicSource.volume = currentVolume;
            if (MusicManager.Instance != null) MusicManager.Instance.SetVolume(currentVolume);
            
            // Move knob physically
            if (volumeKnob != null)
            {
                volumeKnob.localPosition = new Vector3(Mathf.Lerp(-0.4f, 0.4f, currentVolume), 0, 0);
            }
        }
        
        private void SetupAudio()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = currentVolume;
            musicSource.spatialBlend = 0f;
            
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
        }

        private void PlayBackgroundMusic()
        {
            if (backgroundMusic != null && musicSource != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
                Debug.Log("[VRMainMenu] Playing background music");
            }
        }
        
        private void Update()
        {
            // Rotate menu to face camera
            if (Camera.main != null && menuParent != null)
            {
                Vector3 lookDir = Camera.main.transform.position - menuParent.transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(-lookDir);
                    menuParent.transform.rotation = Quaternion.Slerp(
                        menuParent.transform.rotation, 
                        targetRot, 
                        Time.deltaTime * 2f
                    );
                }
            }
        }
        
        private void OnPlayPressed()
        {
            Debug.Log("[VRMainMenu] Play button pressed - Loading game scene");
            SceneManager.LoadScene(gameSceneName);
        }
        
        private void OnQuitPressed()
        {
            Debug.Log("[VRMainMenu] Quit button pressed");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
    
    public class VRMenuButton : MonoBehaviour
    {
        private System.Action onPressCallback;
        private AudioSource audioSource;
        private AudioClip hoverSound;
        private AudioClip clickSound;
        
        public Renderer targetRenderer;
        public Color normalColor;
        public Color hoverColor;
        
        private bool isHovered = false;
        private bool isPressed = false;
        
        public void Initialize(System.Action callback, AudioSource audio, AudioClip hover, AudioClip click)
        {
            onPressCallback = callback;
            audioSource = audio;
            hoverSound = hover;
            clickSound = click;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (IsHandOrController(other))
            {
                if (!isHovered)
                {
                    isHovered = true;
                    targetRenderer.material.color = hoverColor;
                    if (audioSource) audioSource.PlayOneShot(hoverSound);
                }
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            var grabber = other.GetComponentInParent<HackathonVR.Interactions.VRGrabber>();
            // Press on trigger or grip
            if (grabber != null && !isPressed && grabber.IsGrabbing)
            {
                OnPress();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (IsHandOrController(other))
            {
                isHovered = false;
                targetRenderer.material.color = normalColor;
                isPressed = false;
            }
        }
        
        private bool IsHandOrController(Collider col)
        {
            return col.GetComponentInParent<HackathonVR.Interactions.VRGrabber>() != null ||
                   col.CompareTag("Hand") || col.CompareTag("Controller");
        }
        
        private void OnPress()
        {
            isPressed = true;
            if (audioSource) audioSource.PlayOneShot(clickSound);
            
            // Visual pulse
            StartCoroutine(PulseEffect());
            
            // Haptic
            var grabbers = FindObjectsByType<HackathonVR.Interactions.VRGrabber>(FindObjectsSortMode.None);
            foreach (var g in grabbers) g.TriggerHaptic(0.8f, 0.1f);
            
            onPressCallback?.Invoke();
        }
        
        private System.Collections.IEnumerator PulseEffect()
        {
            transform.localScale *= 0.9f;
            yield return new WaitForSeconds(0.1f);
            transform.localScale /= 0.9f;
        }
        
        public void OnPointerClick()
        {
            if (!isPressed) OnPress();
        }
    }
    
    public class VRVolumeSlider : MonoBehaviour
    {
        private VRMainMenu menu;
        private float minX, maxX;
        private bool isDragging = false;
        private Transform draggingHand;
        
        public void Initialize(VRMainMenu menuRef, float min, float max)
        {
            menu = menuRef;
            minX = min;
            maxX = max;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var grabber = other.GetComponentInParent<HackathonVR.Interactions.VRGrabber>();
            if (grabber != null)
            {
                grabber.TriggerHaptic(0.1f, 0.05f);
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            var grabber = other.GetComponentInParent<HackathonVR.Interactions.VRGrabber>();
            if (grabber != null && grabber.IsGrabbing)
            {
                isDragging = true;
                draggingHand = grabber.transform;
            }
            else
            {
                isDragging = false;
                draggingHand = null;
            }
        }
        
        private void Update()
        {
            if (isDragging && draggingHand != null)
            {
                // Project hand position onto slider axis (local X)
                Vector3 localHandPos = transform.parent.InverseTransformPoint(draggingHand.position);
                float newX = Mathf.Clamp(localHandPos.x, minX, maxX);
                
                transform.localPosition = new Vector3(newX, 0, 0);
                
                // Calculate percentage
                float pct = Mathf.InverseLerp(minX, maxX, newX);
                menu.UpdateVolume(pct);
            }
        }
    }
}
