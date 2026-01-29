using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HackathonVR.Interactions
{
    /// <summary>
    /// Allows a VR controller/hand to grab VRGrabInteractable objects.
    /// Supports both proximity grab and distance grab with laser pointer.
    /// Attach this to your controller GameObjects.
    /// </summary>
    public class VRGrabber : MonoBehaviour
    {
        [Header("Hand Configuration")]
        [SerializeField] private HandType handType = HandType.Right;
        [SerializeField] private Transform grabPoint;
        [SerializeField] private float grabRadius = 0.1f;
        [SerializeField] private LayerMask grabLayerMask = ~0;
        
        [Header("Distance Grab Settings")]
        [SerializeField] private bool enableDistanceGrab = true;
        [SerializeField] private float distanceGrabRange = 10f;
        [SerializeField] private float distanceGrabActivation = 0.3f; // Trigger threshold to show laser
        
        [Header("Telekinesis Settings")]
        [SerializeField] private bool useTelekinesisMode = true; // Object floats along laser instead of sticking to hand
        [SerializeField] private float minGrabDistance = 0.3f;
        [SerializeField] private float maxGrabDistance = 8f;
        [SerializeField] private float distanceChangeSpeed = 3f; // How fast thumbstick changes distance
        [SerializeField] private float positionSmoothSpeed = 20f; // How fast object follows target position
        [SerializeField] private float rotationSmoothSpeed = 15f; // How fast object rotates to follow wrist
        
        [Header("Laser Pointer Settings")]
        [SerializeField] private float laserWidth = 0.008f;
        [SerializeField] private Color laserColor = new Color(0.3f, 0.8f, 1f, 0.8f);
        [SerializeField] private Color laserHitColor = new Color(0.3f, 1f, 0.5f, 1f);
        [SerializeField] private Vector3 laserRotationOffset = new Vector3(60f, 0f, 0f); // Rotate to point forward like index finger
        
        [Header("Input Settings")]
        [SerializeField] private GrabInputType grabInput = GrabInputType.Grip;
        [SerializeField] private float grabThreshold = 0.7f;
        [SerializeField] private float releaseThreshold = 0.3f;
        
        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private float defaultHapticAmplitude = 0.5f;
        [SerializeField] private float defaultHapticDuration = 0.1f;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool showGrabPoint = true;
        [SerializeField] private Color grabPointColor = Color.cyan;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        public enum HandType { Left, Right }
        public enum GrabInputType { Grip, Trigger, GripOrTrigger }
        
        // State
        private InputDevice controller;
        private bool controllerFound = false;
        private VRGrabInteractable currentlyGrabbed;
        private VRGrabInteractable currentHoverTarget;
        private VRGrabInteractable distanceHoverTarget;
        private bool isGrabbing = false;
        private float currentGrabValue = 0f;
        private float currentTriggerValue = 0f;
        private bool isShowingLaser = false;
        
        // Telekinesis state
        private float currentGrabDistance = 2f;
        private Quaternion grabRotationOffset;
        
        // Laser
        private LineRenderer laserLine;
        private Material laserMaterial;
        private GameObject laserDot;
        private Material laserDotMaterial;
        
        // Sphere overlap results buffer
        private Collider[] overlapResults = new Collider[10];
        
        public Transform GrabPoint => grabPoint != null ? grabPoint : transform;
        public bool IsGrabbing => isGrabbing && currentlyGrabbed != null;
        public VRGrabInteractable GrabbedObject => currentlyGrabbed;
        public HandType Hand => handType;
        
        // Laser direction with rotation offset applied in LOCAL space (points forward like index finger)
        // The offset rotates the local forward vector, then transforms to world space
        private Vector3 LaserDirection => transform.TransformDirection(Quaternion.Euler(laserRotationOffset) * Vector3.forward);
        
        private void Start()
        {
            if (grabPoint == null)
            {
                // Create a default grab point
                GameObject grabPointObj = new GameObject("GrabPoint");
                grabPointObj.transform.SetParent(transform);
                grabPointObj.transform.localPosition = Vector3.zero;
                grabPoint = grabPointObj.transform;
            }
            
            SetupLaser();
            TryFindController();
        }
        
        private void SetupLaser()
        {
            // Create laser line renderer
            laserLine = gameObject.AddComponent<LineRenderer>();
            laserLine.positionCount = 2;
            laserLine.startWidth = laserWidth;
            laserLine.endWidth = laserWidth * 0.5f;
            laserLine.useWorldSpace = true;
            
            laserMaterial = new Material(Shader.Find("Unlit/Color"));
            laserMaterial.color = laserColor;
            laserLine.material = laserMaterial;
            laserLine.enabled = false;
            
            // Create laser dot (hit point indicator)
            laserDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            laserDot.name = "LaserDot";
            laserDot.transform.localScale = Vector3.one * 0.03f;
            Destroy(laserDot.GetComponent<Collider>());
            
            laserDotMaterial = new Material(Shader.Find("Unlit/Color"));
            laserDotMaterial.color = laserHitColor;
            laserDot.GetComponent<Renderer>().material = laserDotMaterial;
            laserDot.SetActive(false);
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
                Debug.Log($"[VRGrabber] Found {handType} controller: {controller.name}");
            }
        }
        
        private void Update()
        {
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
            
            UpdateGrabInput();
            
            if (!isGrabbing)
            {
                CheckForHoverTargets();
                
                if (enableDistanceGrab)
                {
                    UpdateDistanceGrab();
                }
            }
            else if (useTelekinesisMode && currentlyGrabbed != null)
            {
                // Update telekinesis - object follows laser and thumbstick controls distance
                UpdateTelekinesis();
            }
            
            UpdateLaserVisual();
        }
        
        private void UpdateTelekinesis()
        {
            // Get thumbstick input to control distance
            if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
            {
                // Use Y axis to push/pull object
                currentGrabDistance += thumbstick.y * distanceChangeSpeed * Time.deltaTime;
                currentGrabDistance = Mathf.Clamp(currentGrabDistance, minGrabDistance, maxGrabDistance);
            }
            
            // Calculate target position along laser direction
            Vector3 targetPosition = transform.position + LaserDirection * currentGrabDistance;
            
            // Smoothly move object to target position
            currentlyGrabbed.transform.position = Vector3.Lerp(
                currentlyGrabbed.transform.position,
                targetPosition,
                positionSmoothSpeed * Time.deltaTime
            );
            
            // Rotate object to follow wrist rotation (with offset preserved)
            Quaternion targetRotation = transform.rotation * grabRotationOffset;
            currentlyGrabbed.transform.rotation = Quaternion.Slerp(
                currentlyGrabbed.transform.rotation,
                targetRotation,
                rotationSmoothSpeed * Time.deltaTime
            );
        }
        
        private void UpdateGrabInput()
        {
            float gripValue = 0f;
            float triggerValue = 0f;
            
            controller.TryGetFeatureValue(CommonUsages.grip, out gripValue);
            controller.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
            
            currentTriggerValue = triggerValue;
            
            // Determine grab value based on input type
            switch (grabInput)
            {
                case GrabInputType.Grip:
                    currentGrabValue = gripValue;
                    break;
                case GrabInputType.Trigger:
                    currentGrabValue = triggerValue;
                    break;
                case GrabInputType.GripOrTrigger:
                    currentGrabValue = Mathf.Max(gripValue, triggerValue);
                    break;
            }
            
            // Handle grab/release
            if (!isGrabbing && currentGrabValue >= grabThreshold)
            {
                TryGrab();
            }
            else if (isGrabbing && currentGrabValue <= releaseThreshold)
            {
                Release();
            }
        }
        
        private void CheckForHoverTargets()
        {
            // Find nearby grabbable objects (proximity)
            int hitCount = Physics.OverlapSphereNonAlloc(
                GrabPoint.position, 
                grabRadius, 
                overlapResults, 
                grabLayerMask,
                QueryTriggerInteraction.Ignore
            );
            
            VRGrabInteractable closestGrabbable = null;
            float closestDistance = float.MaxValue;
            
            for (int i = 0; i < hitCount; i++)
            {
                VRGrabInteractable grabbable = overlapResults[i].GetComponentInParent<VRGrabInteractable>();
                
                if (grabbable != null && grabbable.CanBeGrabbed)
                {
                    float distance = Vector3.Distance(GrabPoint.position, overlapResults[i].transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestGrabbable = grabbable;
                    }
                }
            }
            
            // Update hover state
            if (closestGrabbable != currentHoverTarget)
            {
                // Exit old hover
                if (currentHoverTarget != null)
                {
                    currentHoverTarget.OnHoverEnd(this);
                }
                
                // Enter new hover
                currentHoverTarget = closestGrabbable;
                
                if (currentHoverTarget != null)
                {
                    currentHoverTarget.OnHoverStart(this);
                }
            }
        }
        
        private void UpdateDistanceGrab()
        {
            // Laser is always visible when not grabbing something nearby
            isShowingLaser = currentHoverTarget == null;
            
            if (!isShowingLaser)
            {
                // Clear distance hover target when not showing laser
                if (distanceHoverTarget != null)
                {
                    distanceHoverTarget.OnHoverEnd(this);
                    distanceHoverTarget = null;
                }
                return;
            }
            
            // Raycast to find distant objects
            Ray ray = new Ray(transform.position, LaserDirection);
            VRGrabInteractable hitGrabbable = null;
            
            if (Physics.Raycast(ray, out RaycastHit hit, distanceGrabRange, grabLayerMask))
            {
                hitGrabbable = hit.collider.GetComponentInParent<VRGrabInteractable>();
            }
            
            // Update distance hover target
            if (hitGrabbable != distanceHoverTarget)
            {
                // Exit old distance hover
                if (distanceHoverTarget != null)
                {
                    distanceHoverTarget.OnHoverEnd(this);
                }
                
                // Enter new distance hover
                distanceHoverTarget = hitGrabbable;
                
                if (distanceHoverTarget != null && distanceHoverTarget.CanBeGrabbed)
                {
                    distanceHoverTarget.OnHoverStart(this);
                    TriggerHaptic(0.15f, 0.05f); // Light haptic on target acquired
                }
            }
        }
        
        private void UpdateLaserVisual()
        {
            if (!enableDistanceGrab || !isShowingLaser || isGrabbing)
            {
                laserLine.enabled = false;
                laserDot.SetActive(false);
                return;
            }
            
            laserLine.enabled = true;
            
            Ray ray = new Ray(transform.position, LaserDirection);
            Vector3 endPoint = transform.position + LaserDirection * distanceGrabRange;
            bool hitSomething = false;
            
            if (Physics.Raycast(ray, out RaycastHit hit, distanceGrabRange, grabLayerMask))
            {
                endPoint = hit.point;
                hitSomething = true;
                
                // Show dot at hit point
                laserDot.SetActive(true);
                laserDot.transform.position = hit.point + hit.normal * 0.01f;
            }
            else
            {
                laserDot.SetActive(false);
            }
            
            // Update line positions
            laserLine.SetPosition(0, transform.position);
            laserLine.SetPosition(1, endPoint);
            
            // Update colors based on whether we're targeting a grabbable
            Color currentColor = (distanceHoverTarget != null && distanceHoverTarget.CanBeGrabbed) 
                ? laserHitColor 
                : laserColor;
            
            laserMaterial.color = currentColor;
            laserLine.startColor = currentColor;
            laserLine.endColor = currentColor * 0.5f;
            
            if (hitSomething)
            {
                laserDotMaterial.color = currentColor;
                
                // Pulse effect on valid target
                if (distanceHoverTarget != null)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.3f;
                    laserDot.transform.localScale = Vector3.one * 0.03f * pulse;
                }
            }
        }
        
        private void TryGrab()
        {
            // Priority 1: Proximity grab
            if (currentHoverTarget != null && currentHoverTarget.CanBeGrabbed)
            {
                currentlyGrabbed = currentHoverTarget;
                currentHoverTarget.OnHoverEnd(this);
                currentHoverTarget = null;
                
                currentlyGrabbed.OnGrab(this);
                isGrabbing = true;
                
                if (debugMode)
                {
                    Debug.Log($"[VRGrabber] {handType} hand grabbed {currentlyGrabbed.gameObject.name} (proximity)");
                }
                return;
            }
            
            // Priority 2: Distance grab
            if (enableDistanceGrab && distanceHoverTarget != null && distanceHoverTarget.CanBeGrabbed)
            {
                currentlyGrabbed = distanceHoverTarget;
                distanceHoverTarget.OnHoverEnd(this);
                distanceHoverTarget = null;
                
                currentlyGrabbed.OnGrab(this);
                isGrabbing = true;
                isShowingLaser = false;
                
                TriggerHaptic(0.4f, 0.1f); // Stronger haptic for distance grab
                
                if (debugMode)
                {
                    Debug.Log($"[VRGrabber] {handType} hand grabbed {currentlyGrabbed.gameObject.name} (distance)");
                }
            }
        }
        
        private void Release()
        {
            if (currentlyGrabbed != null)
            {
                if (debugMode)
                {
                    Debug.Log($"[VRGrabber] {handType} hand released {currentlyGrabbed.gameObject.name}");
                }
                
                currentlyGrabbed.OnRelease(this);
                currentlyGrabbed = null;
            }
            
            isGrabbing = false;
        }
        
        /// <summary>
        /// Force release the currently grabbed object
        /// </summary>
        public void ForceRelease()
        {
            Release();
        }
        
        /// <summary>
        /// Trigger haptic feedback on this controller
        /// </summary>
        public void TriggerHaptic()
        {
            TriggerHaptic(defaultHapticAmplitude, defaultHapticDuration);
        }
        
        /// <summary>
        /// Trigger haptic feedback with custom parameters
        /// </summary>
        public void TriggerHaptic(float amplitude, float duration)
        {
            if (!enableHaptics || !controllerFound || !controller.isValid) return;
            
            HapticCapabilities capabilities;
            if (controller.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                controller.SendHapticImpulse(0, amplitude, duration);
            }
        }
        
        /// <summary>
        /// Check if a specific button is pressed
        /// </summary>
        public bool IsButtonPressed(InputFeatureUsage<bool> button)
        {
            if (controller.TryGetFeatureValue(button, out bool pressed))
            {
                return pressed;
            }
            return false;
        }
        
        /// <summary>
        /// Get the thumbstick value
        /// </summary>
        public Vector2 GetThumbstickValue()
        {
            if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 value))
            {
                return value;
            }
            return Vector2.zero;
        }
        
        private void OnDestroy()
        {
            if (laserMaterial != null) Destroy(laserMaterial);
            if (laserDotMaterial != null) Destroy(laserDotMaterial);
            if (laserDot != null) Destroy(laserDot);
        }
        
        private void OnDrawGizmos()
        {
            if (!showGrabPoint) return;
            
            Transform point = grabPoint != null ? grabPoint : transform;
            
            Gizmos.color = isGrabbing ? Color.green : (currentHoverTarget != null ? Color.yellow : grabPointColor);
            Gizmos.DrawWireSphere(point.position, grabRadius);
            
            if (currentHoverTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(point.position, currentHoverTarget.transform.position);
            }
            
            // Draw distance grab ray
            if (enableDistanceGrab)
            {
                Gizmos.color = laserColor;
                Gizmos.DrawRay(point.position, transform.forward * distanceGrabRange);
            }
        }
    }
}
