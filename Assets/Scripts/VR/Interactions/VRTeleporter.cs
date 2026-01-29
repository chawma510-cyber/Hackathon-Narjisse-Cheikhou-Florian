using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HackathonVR.Interactions
{
    /// <summary>
    /// Teleportation system for VR with arc pointer and ground target.
    /// Hold thumbstick forward to aim, release to teleport.
    /// </summary>
    public class VRTeleporter : MonoBehaviour
    {
        [Header("Controller Settings")]
        [SerializeField] private HandType handType = HandType.Right;
        
        [Header("Teleport Settings")]
        [SerializeField] private float maxDistance = 15f;
        [SerializeField] private float arcHeight = 3f;
        [SerializeField] private int arcSegments = 30;
        [SerializeField] private LayerMask teleportLayerMask = ~0;
        [SerializeField] private float teleportActivationThreshold = 0.7f;
        [SerializeField] private Vector3 aimRotationOffset = new Vector3(60f, 0f, 0f); // Rotate to point forward like index finger
        
        [Header("Visual Settings")]
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private Color validColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        
        [Header("Target Settings")]
        [SerializeField] private float targetRadius = 0.5f;
        [SerializeField] private float targetRotationSpeed = 90f;
        
        [Header("Fade Settings")]
        [SerializeField] private bool useFade = true;
        [SerializeField] private float fadeDuration = 0.15f;
        
        public enum HandType { Left, Right }
        
        // Components
        private LineRenderer lineRenderer;
        private GameObject targetIndicator;
        private GameObject targetRing;
        private GameObject targetArrow;
        private Material lineMaterial;
        private Material targetMaterial;
        
        // State
        private InputDevice controller;
        private bool controllerFound = false;
        private bool isAiming = false;
        private bool canTeleport = false;
        private Vector3 teleportDestination;
        private Vector3 teleportNormal;
        private Transform xrOrigin;
        private Camera vrCamera;
        
        // Arc points
        private Vector3[] arcPoints;
        
        // Fade
        private GameObject fadeQuad;
        private Material fadeMaterial;
        private bool isFading = false;
        
        private void Start()
        {
            SetupLineRenderer();
            SetupTargetIndicator();
            SetupFade();
            FindXROrigin();
            TryFindController();
            
            arcPoints = new Vector3[arcSegments];
        }
        
        private void SetupLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = arcSegments;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth * 0.5f;
            lineRenderer.useWorldSpace = true;
            
            // Create glowing material
            lineMaterial = new Material(Shader.Find("Unlit/Color"));
            lineMaterial.color = validColor;
            lineRenderer.material = lineMaterial;
            
            lineRenderer.enabled = false;
        }
        
        private void SetupTargetIndicator()
        {
            // Main target parent
            targetIndicator = new GameObject("TeleportTarget");
            targetIndicator.transform.SetParent(null);
            
            // Create outer ring
            targetRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetRing.name = "TargetRing";
            targetRing.transform.SetParent(targetIndicator.transform);
            targetRing.transform.localPosition = Vector3.zero;
            targetRing.transform.localScale = new Vector3(targetRadius * 2f, 0.02f, targetRadius * 2f);
            Destroy(targetRing.GetComponent<Collider>());
            
            // Create ring material
            targetMaterial = new Material(Shader.Find("Standard"));
            targetMaterial.color = validColor;
            targetMaterial.EnableKeyword("_EMISSION");
            targetMaterial.SetColor("_EmissionColor", validColor * 0.5f);
            targetMaterial.SetFloat("_Mode", 3); // Transparent
            targetMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            targetMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            targetMaterial.SetInt("_ZWrite", 0);
            targetMaterial.EnableKeyword("_ALPHABLEND_ON");
            targetMaterial.renderQueue = 3000;
            targetRing.GetComponent<Renderer>().material = targetMaterial;
            
            // Create inner circle
            var innerCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerCircle.name = "InnerCircle";
            innerCircle.transform.SetParent(targetIndicator.transform);
            innerCircle.transform.localPosition = new Vector3(0, 0.01f, 0);
            innerCircle.transform.localScale = new Vector3(targetRadius * 0.5f, 0.02f, targetRadius * 0.5f);
            Destroy(innerCircle.GetComponent<Collider>());
            
            var innerMat = new Material(targetMaterial);
            innerMat.color = new Color(validColor.r, validColor.g, validColor.b, 0.8f);
            innerCircle.GetComponent<Renderer>().material = innerMat;
            
            // Create direction arrow
            targetArrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetArrow.name = "DirectionArrow";
            targetArrow.transform.SetParent(targetIndicator.transform);
            targetArrow.transform.localPosition = new Vector3(0, 0.03f, targetRadius * 0.3f);
            targetArrow.transform.localScale = new Vector3(0.1f, 0.02f, 0.3f);
            Destroy(targetArrow.GetComponent<Collider>());
            
            var arrowMat = new Material(Shader.Find("Unlit/Color"));
            arrowMat.color = Color.white;
            targetArrow.GetComponent<Renderer>().material = arrowMat;
            
            targetIndicator.SetActive(false);
        }
        
        private void SetupFade()
        {
            if (!useFade) return;
            
            // Create fade quad that covers the view
            fadeQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fadeQuad.name = "TeleportFade";
            Destroy(fadeQuad.GetComponent<Collider>());
            
            fadeMaterial = new Material(Shader.Find("Unlit/Color"));
            fadeMaterial.color = new Color(0, 0, 0, 0);
            fadeMaterial.SetFloat("_Mode", 3);
            fadeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fadeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fadeMaterial.SetInt("_ZWrite", 0);
            fadeMaterial.EnableKeyword("_ALPHABLEND_ON");
            fadeMaterial.renderQueue = 4000;
            fadeQuad.GetComponent<Renderer>().material = fadeMaterial;
            
            fadeQuad.SetActive(false);
        }
        
        private void FindXROrigin()
        {
            var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
            {
                xrOrigin = origin.transform;
                vrCamera = origin.Camera;
            }
            else
            {
                vrCamera = Camera.main;
                if (vrCamera != null)
                {
                    xrOrigin = vrCamera.transform.parent?.parent ?? vrCamera.transform.parent;
                }
            }
        }
        
        private void TryFindController()
        {
            InputDeviceCharacteristics characteristics = 
                InputDeviceCharacteristics.Controller |
                (handType == HandType.Left ? 
                    InputDeviceCharacteristics.Left : 
                    InputDeviceCharacteristics.Right);
            
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            
            if (devices.Count > 0)
            {
                controller = devices[0];
                controllerFound = true;
            }
        }
        
        private void Update()
        {
            if (isFading) return;
            
            if (!controllerFound)
            {
                TryFindController();
                return;
            }
            
            if (!controller.isValid)
            {
                controllerFound = false;
                return;
            }
            
            UpdateTeleportInput();
            
            if (isAiming)
            {
                UpdateArc();
                UpdateTargetIndicator();
            }
        }
        
        private void UpdateTeleportInput()
        {
            // Use thumbstick Y axis to activate teleport
            controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick);
            
            bool wasAiming = isAiming;
            isAiming = thumbstick.y > teleportActivationThreshold;
            
            // Started aiming
            if (isAiming && !wasAiming)
            {
                lineRenderer.enabled = true;
                targetIndicator.SetActive(true);
                TriggerHaptic(0.1f, 0.05f);
            }
            
            // Stopped aiming (released) - teleport!
            if (!isAiming && wasAiming && canTeleport)
            {
                ExecuteTeleport();
            }
            
            // Stopped aiming without valid target
            if (!isAiming && wasAiming)
            {
                lineRenderer.enabled = false;
                targetIndicator.SetActive(false);
            }
        }
        
        private void UpdateArc()
        {
            Vector3 startPos = transform.position;
            // Apply rotation offset in LOCAL space - rotate local forward, then transform to world
            Vector3 forward = transform.TransformDirection(Quaternion.Euler(aimRotationOffset) * Vector3.forward);
            
            // Calculate arc trajectory
            float stepLength = maxDistance / arcSegments;
            canTeleport = false;
            int validPointCount = arcSegments;
            
            for (int i = 0; i < arcSegments; i++)
            {
                float t = (float)i / (arcSegments - 1);
                
                // Parabolic arc
                float horizontalDist = t * maxDistance;
                float verticalOffset = arcHeight * 4f * t * (1f - t); // Parabola
                verticalOffset -= t * t * arcHeight * 2f; // Gravity drop
                
                Vector3 point = startPos + forward * horizontalDist + Vector3.up * verticalOffset;
                arcPoints[i] = point;
                
                // Check for collision
                if (i > 0)
                {
                    Vector3 dir = arcPoints[i] - arcPoints[i - 1];
                    float dist = dir.magnitude;
                    
                    if (Physics.Raycast(arcPoints[i - 1], dir.normalized, out RaycastHit hit, dist, teleportLayerMask))
                    {
                        arcPoints[i] = hit.point;
                        validPointCount = i + 1;
                        
                        // Check if surface is valid (relatively flat)
                        if (hit.normal.y > 0.7f)
                        {
                            canTeleport = true;
                            teleportDestination = hit.point;
                            teleportNormal = hit.normal;
                        }
                        break;
                    }
                }
            }
            
            // Update line renderer
            lineRenderer.positionCount = validPointCount;
            for (int i = 0; i < validPointCount; i++)
            {
                lineRenderer.SetPosition(i, arcPoints[i]);
            }
            
            // Update colors
            Color currentColor = canTeleport ? validColor : invalidColor;
            lineMaterial.color = currentColor;
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor * 0.5f;
        }
        
        private void UpdateTargetIndicator()
        {
            if (canTeleport)
            {
                targetIndicator.SetActive(true);
                targetIndicator.transform.position = teleportDestination + Vector3.up * 0.01f;
                targetIndicator.transform.up = teleportNormal;
                
                // Rotate the target
                targetIndicator.transform.Rotate(Vector3.up, targetRotationSpeed * Time.deltaTime, Space.Self);
                
                // Update target color
                targetMaterial.color = validColor;
                targetMaterial.SetColor("_EmissionColor", validColor * (0.5f + Mathf.Sin(Time.time * 5f) * 0.2f));
            }
            else
            {
                targetIndicator.SetActive(false);
            }
        }
        
        private void ExecuteTeleport()
        {
            if (!canTeleport || xrOrigin == null) return;
            
            TriggerHaptic(0.5f, 0.15f);
            
            if (useFade)
            {
                StartCoroutine(TeleportWithFade());
            }
            else
            {
                PerformTeleport();
            }
            
            lineRenderer.enabled = false;
            targetIndicator.SetActive(false);
        }
        
        private System.Collections.IEnumerator TeleportWithFade()
        {
            isFading = true;
            
            // Position fade quad in front of camera
            if (vrCamera != null && fadeQuad != null)
            {
                fadeQuad.SetActive(true);
                fadeQuad.transform.SetParent(vrCamera.transform);
                fadeQuad.transform.localPosition = new Vector3(0, 0, vrCamera.nearClipPlane + 0.01f);
                fadeQuad.transform.localRotation = Quaternion.identity;
                fadeQuad.transform.localScale = Vector3.one * 0.5f;
            }
            
            // Fade to black
            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / fadeDuration;
                fadeMaterial.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            
            // Teleport
            PerformTeleport();
            
            // Fade from black
            elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeDuration);
                fadeMaterial.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            
            fadeQuad.SetActive(false);
            isFading = false;
        }
        
        private void PerformTeleport()
        {
            if (xrOrigin == null || vrCamera == null) return;
            
            // Calculate offset from camera to XR Origin
            Vector3 cameraOffset = vrCamera.transform.position - xrOrigin.position;
            cameraOffset.y = 0; // Only horizontal offset
            
            // Teleport XR Origin
            xrOrigin.position = teleportDestination - cameraOffset;
            
            Debug.Log($"[VRTeleporter] Teleported to {teleportDestination}");
        }
        
        private void TriggerHaptic(float amplitude, float duration)
        {
            if (!controllerFound || !controller.isValid) return;
            
            HapticCapabilities capabilities;
            if (controller.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                controller.SendHapticImpulse(0, amplitude, duration);
            }
        }
        
        private void OnDestroy()
        {
            if (targetIndicator != null) Destroy(targetIndicator);
            if (fadeQuad != null) Destroy(fadeQuad);
            if (lineMaterial != null) Destroy(lineMaterial);
            if (targetMaterial != null) Destroy(targetMaterial);
            if (fadeMaterial != null) Destroy(fadeMaterial);
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = validColor;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}
