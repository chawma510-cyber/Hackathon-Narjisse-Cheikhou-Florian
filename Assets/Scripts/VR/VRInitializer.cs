using UnityEngine;
using UnityEngine.XR.Management;
using System.Collections;

namespace HackathonVR
{
    /// <summary>
    /// Initializes and manages the XR system for VR experiences.
    /// Attach this to a GameObject in your scene to ensure VR is properly started.
    /// </summary>
    public class VRInitializer : MonoBehaviour
    {
        [Header("VR Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private float initializationTimeout = 10f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        private bool isInitialized = false;
        
        public bool IsVRActive => isInitialized && XRGeneralSettings.Instance?.Manager?.activeLoader != null;
        
        public static VRInitializer Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private IEnumerator Start()
        {
            if (autoInitialize)
            {
                yield return StartCoroutine(InitializeVR());
            }
        }
        
        /// <summary>
        /// Initialize the VR system
        /// </summary>
        public IEnumerator InitializeVR()
        {
            if (isInitialized)
            {
                Debug.Log("[VRInitializer] VR already initialized");
                yield break;
            }
            
            Debug.Log("[VRInitializer] Starting VR initialization...");
            
            // Check if XR is available
            if (XRGeneralSettings.Instance == null)
            {
                Debug.LogError("[VRInitializer] XRGeneralSettings not found! Please configure XR in Project Settings.");
                yield break;
            }
            
            var xrManager = XRGeneralSettings.Instance.Manager;
            if (xrManager == null)
            {
                Debug.LogError("[VRInitializer] XR Manager not found!");
                yield break;
            }
            
            // Initialize XR if not already done
            if (xrManager.activeLoader == null)
            {
                Debug.Log("[VRInitializer] Initializing XR Loader...");
                yield return xrManager.InitializeLoader();
                
                float timeout = initializationTimeout;
                while (xrManager.activeLoader == null && timeout > 0)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                
                if (xrManager.activeLoader == null)
                {
                    Debug.LogError("[VRInitializer] Failed to initialize XR Loader. " +
                                   "Make sure a VR headset is connected and OpenXR runtime is configured.");
                    yield break;
                }
            }
            
            // Start the XR subsystems
            xrManager.StartSubsystems();
            
            isInitialized = true;
            Debug.Log($"[VRInitializer] VR initialized successfully! Using: {xrManager.activeLoader.name}");
        }
        
        /// <summary>
        /// Stop the VR system
        /// </summary>
        public void StopVR()
        {
            if (!isInitialized)
            {
                return;
            }
            
            Debug.Log("[VRInitializer] Stopping VR...");
            
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager != null)
            {
                xrManager.StopSubsystems();
                xrManager.DeinitializeLoader();
            }
            
            isInitialized = false;
            Debug.Log("[VRInitializer] VR stopped");
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                StopVR();
                Instance = null;
            }
        }
        
        private void OnApplicationQuit()
        {
            StopVR();
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"VR Status: {(IsVRActive ? "ACTIVE" : "INACTIVE")}");
            
            if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
            {
                GUILayout.Label($"Loader: {XRGeneralSettings.Instance.Manager.activeLoader.name}");
            }
            else
            {
                GUILayout.Label("Loader: None");
            }
            
            GUILayout.Label($"Refresh Rate: {XRDevice.refreshRate:F0} Hz");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
