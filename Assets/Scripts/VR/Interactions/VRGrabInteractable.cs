using UnityEngine;
using UnityEngine.Events;

namespace HackathonVR.Interactions
{
    /// <summary>
    /// Makes an object grabbable in VR.
    /// Attach this to any object you want the player to be able to pick up.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VRGrabInteractable : MonoBehaviour
    {
        [Header("Grab Settings")]
        [SerializeField] private GrabType grabType = GrabType.Kinematic;
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private bool returnToOriginalPosition = false;
        [SerializeField] private float returnSpeed = 5f;
        
        [Header("Physics Settings")]
        [SerializeField] private float throwForceMultiplier = 1.5f;
        [SerializeField] private float throwAngularForceMultiplier = 1f;
        
        [Header("Snap Settings")]
        [SerializeField] private bool useSnapPoint = false;
        [SerializeField] private Vector3 snapPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 snapRotationOffset = Vector3.zero;
        
        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHapticOnGrab = true;
        [SerializeField] private float hapticAmplitude = 0.3f;
        [SerializeField] private float hapticDuration = 0.1f;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool highlightOnHover = true;
        [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private float highlightIntensity = 0.3f;
        
        [Header("Events")]
        public UnityEvent OnGrabbed;
        public UnityEvent OnReleased;
        public UnityEvent OnHoverEnter;
        public UnityEvent OnHoverExit;
        
        public enum GrabType
        {
            Kinematic,      // Object follows hand exactly (best for most cases)
            Physics,        // Object uses physics joints (more realistic but can be jittery)
            Instantaneous   // Object snaps to hand instantly
        }
        
        // State
        private bool isGrabbed = false;
        private bool isHovered = false;
        private VRGrabber currentGrabber;
        private Rigidbody rb;
        private Collider[] colliders;
        
        // Original state for return
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Transform originalParent;
        
        // Physics tracking for throwing
        private Vector3 previousPosition;
        private Vector3 velocity;
        private Vector3 angularVelocity;
        private Vector3 previousForward;
        
        // Visual feedback
        private Material[] originalMaterials;
        private Material[] highlightMaterials;
        private Renderer objectRenderer;
        
        public bool IsGrabbed => isGrabbed;
        public bool IsHovered => isHovered;
        public bool CanBeGrabbed => canBeGrabbed && !isGrabbed;
        public VRGrabber CurrentGrabber => currentGrabber;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            objectRenderer = GetComponentInChildren<Renderer>();
            
            // Store original state
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalParent = transform.parent;
            
            // Setup highlight materials
            if (highlightOnHover && objectRenderer != null)
            {
                SetupHighlightMaterials();
            }
            
            // Ensure we have required components properly configured
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
        
        private void SetupHighlightMaterials()
        {
            originalMaterials = objectRenderer.materials;
            highlightMaterials = new Material[originalMaterials.Length];
            
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                highlightMaterials[i] = new Material(originalMaterials[i]);
                if (highlightMaterials[i].HasProperty("_EmissionColor"))
                {
                    highlightMaterials[i].EnableKeyword("_EMISSION");
                    highlightMaterials[i].SetColor("_EmissionColor", highlightColor * highlightIntensity);
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (isGrabbed)
            {
                TrackVelocity();
            }
        }
        
        private void TrackVelocity()
        {
            // Calculate velocity for throwing
            velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
            
            // Calculate angular velocity
            Vector3 deltaRotation = Quaternion.FromToRotation(previousForward, transform.forward).eulerAngles;
            // Convert from 0-360 to -180 to 180
            if (deltaRotation.x > 180) deltaRotation.x -= 360;
            if (deltaRotation.y > 180) deltaRotation.y -= 360;
            if (deltaRotation.z > 180) deltaRotation.z -= 360;
            angularVelocity = deltaRotation * Mathf.Deg2Rad / Time.fixedDeltaTime;
            
            previousPosition = transform.position;
            previousForward = transform.forward;
        }
        
        /// <summary>
        /// Called when a VRGrabber starts grabbing this object
        /// </summary>
        public void OnGrab(VRGrabber grabber)
        {
            if (!canBeGrabbed || isGrabbed) return;
            
            isGrabbed = true;
            currentGrabber = grabber;
            
            // Initialize velocity tracking
            previousPosition = transform.position;
            previousForward = transform.forward;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            
            // Configure physics based on grab type
            switch (grabType)
            {
                case GrabType.Kinematic:
                case GrabType.Instantaneous:
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    break;
                case GrabType.Physics:
                    // Keep physics but disable gravity temporarily
                    rb.useGravity = false;
                    break;
            }
            
            // Parent to grabber for perfect following
            transform.SetParent(grabber.GrabPoint);
            
            // Apply snap offset if configured, otherwise center on hand
            if (useSnapPoint)
            {
                transform.localPosition = snapPositionOffset;
                transform.localRotation = Quaternion.Euler(snapRotationOffset);
            }
            else
            {
                // Center object on grab point for perfect following
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            
            // Trigger haptic feedback
            if (enableHapticOnGrab)
            {
                grabber.TriggerHaptic(hapticAmplitude, hapticDuration);
            }
            
            // Remove highlight
            if (isHovered)
            {
                SetHighlight(false);
                isHovered = false;
            }
            
            OnGrabbed?.Invoke();
            Debug.Log($"[VRGrabInteractable] {gameObject.name} grabbed by {grabber.gameObject.name}");
        }
        
        /// <summary>
        /// Called when the VRGrabber releases this object
        /// </summary>
        public void OnRelease(VRGrabber grabber)
        {
            if (!isGrabbed || currentGrabber != grabber) return;
            
            isGrabbed = false;
            currentGrabber = null;
            
            // Unparent
            transform.SetParent(originalParent);
            
            // Restore physics
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Apply throw velocity
            rb.linearVelocity = velocity * throwForceMultiplier;
            rb.angularVelocity = angularVelocity * throwAngularForceMultiplier;
            
            // Return to original position if configured
            if (returnToOriginalPosition)
            {
                StartCoroutine(ReturnToOriginalPosition());
            }
            
            OnReleased?.Invoke();
            Debug.Log($"[VRGrabInteractable] {gameObject.name} released with velocity {velocity.magnitude:F2} m/s");
        }
        
        private System.Collections.IEnumerator ReturnToOriginalPosition()
        {
            yield return new WaitForSeconds(2f); // Wait before returning
            
            if (isGrabbed) yield break; // Don't return if grabbed again
            
            rb.isKinematic = true;
            
            while (Vector3.Distance(transform.position, originalPosition) > 0.01f)
            {
                if (isGrabbed) 
                {
                    rb.isKinematic = false;
                    yield break;
                }
                
                transform.position = Vector3.Lerp(transform.position, originalPosition, returnSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, returnSpeed * Time.deltaTime);
                yield return null;
            }
            
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        /// <summary>
        /// Called when a grabber starts hovering over this object
        /// </summary>
        public void OnHoverStart(VRGrabber grabber)
        {
            if (isGrabbed || isHovered || !canBeGrabbed) return;
            
            isHovered = true;
            SetHighlight(true);
            
            // Light haptic to indicate hoverable
            grabber.TriggerHaptic(0.1f, 0.05f);
            
            OnHoverEnter?.Invoke();
        }
        
        /// <summary>
        /// Called when a grabber stops hovering over this object
        /// </summary>
        public void OnHoverEnd(VRGrabber grabber)
        {
            if (!isHovered) return;
            
            isHovered = false;
            SetHighlight(false);
            
            OnHoverExit?.Invoke();
        }
        
        private void SetHighlight(bool enabled)
        {
            if (!highlightOnHover || objectRenderer == null) return;
            
            objectRenderer.materials = enabled ? highlightMaterials : originalMaterials;
        }
        
        /// <summary>
        /// Force drop the object (useful for game logic)
        /// </summary>
        public void ForceDrop()
        {
            if (isGrabbed && currentGrabber != null)
            {
                currentGrabber.ForceRelease();
            }
        }
        
        /// <summary>
        /// Enable or disable grab capability at runtime
        /// </summary>
        public void SetGrabbable(bool grabbable)
        {
            canBeGrabbed = grabbable;
            
            if (!grabbable && isGrabbed)
            {
                ForceDrop();
            }
        }
        
        private void OnDestroy()
        {
            // Clean up highlight materials
            if (highlightMaterials != null)
            {
                foreach (var mat in highlightMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
            }
        }
    }
}
