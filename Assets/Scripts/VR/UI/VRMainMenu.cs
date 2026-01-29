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
        [SerializeField] private float buttonHeight = 1.5f;
        [SerializeField] private float buttonDistance = 2f;
        
        [Header("Title Settings")]
        [SerializeField] private string gameTitle = "VR EXPERIENCE";
        [SerializeField] private Color titleColor = new Color(0.3f, 0.8f, 1f);
        
        [Header("Button Appearance")]
        [SerializeField] private Color playButtonColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color quitButtonColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private float buttonWidth = 0.8f;
        [SerializeField] private float buttonHeightSize = 0.3f;
        
        [Header("Animation")]
        [SerializeField] private float floatAmplitude = 0.05f;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float rotateSpeed = 15f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip buttonClickSound;
        
        // Components
        private GameObject menuParent;
        private GameObject titleObject;
        private GameObject playButton;
        private GameObject quitButton;
        private AudioSource musicSource;
        private AudioSource sfxSource;
        
        // State
        private Vector3 playButtonBasePos;
        private Vector3 quitButtonBasePos;
        private float animTime = 0f;
        
        private void Start()
        {
            CreateMenu();
            SetupAudio();
            
            // Try to load music from Resources if not assigned
            if (backgroundMusic == null)
            {
                backgroundMusic = Resources.Load<AudioClip>("Musiques/The Minimoys Overture");
            }
            
            PlayBackgroundMusic();
        }
        
        private void CreateMenu()
        {
            menuParent = new GameObject("MainMenu");
            menuParent.transform.position = new Vector3(0, buttonHeight, buttonDistance);
            
            // Create floating title
            CreateTitle();
            
            // Create play button
            playButton = CreateButton("JOUER", playButtonColor, new Vector3(-0.5f, 0, 0));
            playButtonBasePos = playButton.transform.localPosition;
            
            // Create quit button
            quitButton = CreateButton("QUITTER", quitButtonColor, new Vector3(0.5f, 0, 0));
            quitButtonBasePos = quitButton.transform.localPosition;
            
            // Add VR button components for interaction
            AddVRButtonBehavior(playButton, OnPlayPressed);
            AddVRButtonBehavior(quitButton, OnQuitPressed);
        }
        
        private void CreateTitle()
        {
            titleObject = new GameObject("Title");
            titleObject.transform.SetParent(menuParent.transform);
            titleObject.transform.localPosition = new Vector3(0, 0.6f, 0);
            
            // Create 3D text using TextMesh
            var textMesh = titleObject.AddComponent<TextMesh>();
            textMesh.text = gameTitle;
            textMesh.fontSize = 100;
            textMesh.characterSize = 0.02f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = titleColor;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // Add glow effect with second text behind
            var glowObj = new GameObject("TitleGlow");
            glowObj.transform.SetParent(titleObject.transform);
            glowObj.transform.localPosition = new Vector3(0, 0, 0.01f);
            glowObj.transform.localScale = Vector3.one * 1.05f;
            
            var glowMesh = glowObj.AddComponent<TextMesh>();
            glowMesh.text = gameTitle;
            glowMesh.fontSize = 100;
            glowMesh.characterSize = 0.02f;
            glowMesh.anchor = TextAnchor.MiddleCenter;
            glowMesh.alignment = TextAlignment.Center;
            glowMesh.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0.3f);
        }
        
        private GameObject CreateButton(string text, Color color, Vector3 localOffset)
        {
            var button = new GameObject("Button_" + text);
            button.transform.SetParent(menuParent.transform);
            button.transform.localPosition = localOffset;
            
            // Button background (rounded cube effect with multiple cubes)
            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Background";
            bg.transform.SetParent(button.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(buttonWidth, buttonHeightSize, 0.08f);
            
            var bgRenderer = bg.GetComponent<Renderer>();
            var bgMat = new Material(Shader.Find("Standard"));
            bgMat.color = color;
            bgMat.EnableKeyword("_EMISSION");
            bgMat.SetColor("_EmissionColor", color * 0.3f);
            bgRenderer.material = bgMat;
            
            // Keep collider for VR interaction
            var collider = bg.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            
            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.05f);
            
            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 80;
            textMesh.characterSize = 0.015f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            
            // Add frame/border
            CreateButtonFrame(button.transform, color);
            
            return button;
        }
        
        private void CreateButtonFrame(Transform parent, Color color)
        {
            float frameThickness = 0.02f;
            Color frameColor = Color.Lerp(color, Color.white, 0.5f);
            
            // Top frame
            CreateFramePiece(parent, "TopFrame", 
                new Vector3(0, buttonHeightSize/2 + frameThickness/2, 0),
                new Vector3(buttonWidth + frameThickness*2, frameThickness, 0.09f),
                frameColor);
            
            // Bottom frame
            CreateFramePiece(parent, "BottomFrame", 
                new Vector3(0, -buttonHeightSize/2 - frameThickness/2, 0),
                new Vector3(buttonWidth + frameThickness*2, frameThickness, 0.09f),
                frameColor);
            
            // Left frame
            CreateFramePiece(parent, "LeftFrame", 
                new Vector3(-buttonWidth/2 - frameThickness/2, 0, 0),
                new Vector3(frameThickness, buttonHeightSize, 0.09f),
                frameColor);
            
            // Right frame
            CreateFramePiece(parent, "RightFrame", 
                new Vector3(buttonWidth/2 + frameThickness/2, 0, 0),
                new Vector3(frameThickness, buttonHeightSize, 0.09f),
                frameColor);
        }
        
        private void CreateFramePiece(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent);
            piece.transform.localPosition = pos;
            piece.transform.localScale = scale;
            Destroy(piece.GetComponent<Collider>());
            
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color;
            piece.GetComponent<Renderer>().material = mat;
        }
        
        private void AddVRButtonBehavior(GameObject button, System.Action onPress)
        {
            var behavior = button.AddComponent<VRMenuButton>();
            behavior.Initialize(onPress, sfxSource, buttonHoverSound, buttonClickSound);
        }
        
        private void SetupAudio()
        {
            // Background music source
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = 0.5f;
            musicSource.spatialBlend = 0f; // 2D sound
            
            // SFX source
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.7f;
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
            AnimateButtons();
        }
        
        private void AnimateButtons()
        {
            animTime += Time.deltaTime;
            
            // Float animation
            float floatOffset = Mathf.Sin(animTime * floatSpeed) * floatAmplitude;
            
            if (playButton != null)
            {
                playButton.transform.localPosition = playButtonBasePos + Vector3.up * floatOffset;
            }
            
            if (quitButton != null)
            {
                float quitOffset = Mathf.Sin(animTime * floatSpeed + Mathf.PI * 0.5f) * floatAmplitude;
                quitButton.transform.localPosition = quitButtonBasePos + Vector3.up * quitOffset;
            }
            
            // Title gentle rotation
            if (titleObject != null)
            {
                titleObject.transform.localRotation = Quaternion.Euler(
                    Mathf.Sin(animTime * 0.5f) * 3f,
                    Mathf.Sin(animTime * 0.3f) * 5f,
                    0
                );
            }
            
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
            
            // Store that we should play music in game scene
            PlayerPrefs.SetInt("PlayMusic", 1);
            PlayerPrefs.Save();
            
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
    
    /// <summary>
    /// VR-compatible menu button that can be pressed by hand collision or laser pointer
    /// </summary>
    public class VRMenuButton : MonoBehaviour
    {
        private System.Action onPressCallback;
        private AudioSource audioSource;
        private AudioClip hoverSound;
        private AudioClip clickSound;
        
        private Renderer buttonRenderer;
        private Color originalColor;
        private Color hoverColor;
        private bool isHovered = false;
        private float pressedScale = 0.95f;
        private bool isPressed = false;
        
        public void Initialize(System.Action callback, AudioSource audio, AudioClip hover, AudioClip click)
        {
            onPressCallback = callback;
            audioSource = audio;
            hoverSound = hover;
            clickSound = click;
            
            // Get background renderer
            var bg = transform.Find("Background");
            if (bg != null)
            {
                buttonRenderer = bg.GetComponent<Renderer>();
                if (buttonRenderer != null)
                {
                    originalColor = buttonRenderer.material.color;
                    hoverColor = Color.Lerp(originalColor, Color.white, 0.3f);
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if it's a VR hand/controller
            if (other.GetComponentInParent<HackathonVR.Interactions.VRGrabber>() != null ||
                other.CompareTag("Hand") || other.CompareTag("Controller"))
            {
                if (!isHovered)
                {
                    isHovered = true;
                    OnHoverEnter();
                }
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            // Check for press (grip button)
            var grabber = other.GetComponentInParent<HackathonVR.Interactions.VRGrabber>();
            if (grabber != null && !isPressed)
            {
                // Check if grip is pressed
                if (grabber.IsGrabbing)
                {
                    OnPress();
                }
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<HackathonVR.Interactions.VRGrabber>() != null ||
                other.CompareTag("Hand") || other.CompareTag("Controller"))
            {
                isHovered = false;
                OnHoverExit();
            }
        }
        
        private void OnHoverEnter()
        {
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = hoverColor;
                buttonRenderer.material.SetColor("_EmissionColor", hoverColor * 0.5f);
            }
            
            if (audioSource != null && hoverSound != null)
            {
                audioSource.PlayOneShot(hoverSound);
            }
            
            // Slight scale up
            transform.localScale = Vector3.one * 1.05f;
        }
        
        private void OnHoverExit()
        {
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = originalColor;
                buttonRenderer.material.SetColor("_EmissionColor", originalColor * 0.3f);
            }
            
            transform.localScale = Vector3.one;
            isPressed = false;
        }
        
        private void OnPress()
        {
            isPressed = true;
            
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            
            // Visual feedback
            transform.localScale = Vector3.one * pressedScale;
            
            // Haptic feedback if possible
            var grabbers = FindObjectsByType<HackathonVR.Interactions.VRGrabber>(FindObjectsSortMode.None);
            foreach (var grabber in grabbers)
            {
                grabber.TriggerHaptic(0.5f, 0.15f);
            }
            
            // Invoke callback
            onPressCallback?.Invoke();
        }
        
        // Also support laser pointer click
        public void OnPointerClick()
        {
            if (!isPressed)
            {
                OnPress();
            }
        }
    }
}
