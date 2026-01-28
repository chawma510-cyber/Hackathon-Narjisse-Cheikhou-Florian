using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace HackathonVR
{
    /// <summary>
    /// Provides smooth locomotion and snap turn functionality for VR.
    /// Attach to the XR Origin to enable movement.
    /// </summary>
    public class VRLocomotion : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float sprintMultiplier = 2f;
        [SerializeField] private bool useHeadDirection = true;
        
        [Header("Turn Settings")]
        [SerializeField] private bool enableSnapTurn = true;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private float snapTurnCooldown = 0.3f;
        
        [Header("References")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private CharacterController characterController;
        
        [Header("Input")]
        [SerializeField] private InputDeviceCharacteristics leftHandCharacteristics = 
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        [SerializeField] private InputDeviceCharacteristics rightHandCharacteristics = 
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        
        private InputDevice leftController;
        private InputDevice rightController;
        private float lastSnapTurnTime;
        private bool controllersFound = false;
        
        // Gravity
        private float gravity = -9.81f;
        private float verticalVelocity = 0f;
        
        private void Start()
        {
            // Try to find character controller if not assigned
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
                if (characterController == null)
                {
                    characterController = gameObject.AddComponent<CharacterController>();
                    characterController.height = 1.8f;
                    characterController.center = new Vector3(0, 0.9f, 0);
                    characterController.radius = 0.3f;
                }
            }
            
            // Find head transform if not assigned
            if (headTransform == null)
            {
                var camera = GetComponentInChildren<Camera>();
                if (camera != null)
                {
                    headTransform = camera.transform;
                }
            }
            
            TryFindControllers();
        }
        
        private void TryFindControllers()
        {
            var leftDevices = new System.Collections.Generic.List<InputDevice>();
            var rightDevices = new System.Collections.Generic.List<InputDevice>();
            
            InputDevices.GetDevicesWithCharacteristics(leftHandCharacteristics, leftDevices);
            InputDevices.GetDevicesWithCharacteristics(rightHandCharacteristics, rightDevices);
            
            if (leftDevices.Count > 0)
            {
                leftController = leftDevices[0];
            }
            
            if (rightDevices.Count > 0)
            {
                rightController = rightDevices[0];
            }
            
            controllersFound = leftController.isValid && rightController.isValid;
        }
        
        private void Update()
        {
            if (!controllersFound)
            {
                TryFindControllers();
            }
            
            HandleMovement();
            HandleRotation();
            ApplyGravity();
        }
        
        private void HandleMovement()
        {
            if (!leftController.isValid) return;
            
            // Get thumbstick input from left controller
            if (leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 inputAxis))
            {
                if (inputAxis.magnitude > 0.1f)
                {
                    // Calculate movement direction
                    Vector3 moveDirection;
                    
                    if (useHeadDirection && headTransform != null)
                    {
                        // Use head forward direction (more intuitive)
                        Vector3 forward = headTransform.forward;
                        Vector3 right = headTransform.right;
                        
                        // Project onto horizontal plane
                        forward.y = 0;
                        right.y = 0;
                        forward.Normalize();
                        right.Normalize();
                        
                        moveDirection = (forward * inputAxis.y + right * inputAxis.x);
                    }
                    else
                    {
                        // Use XR Origin forward direction
                        moveDirection = (transform.forward * inputAxis.y + transform.right * inputAxis.x);
                    }
                    
                    // Check for sprint (grip button)
                    float speedMultiplier = 1f;
                    if (leftController.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed) && gripPressed)
                    {
                        speedMultiplier = sprintMultiplier;
                    }
                    
                    // Apply movement
                    Vector3 movement = moveDirection * moveSpeed * speedMultiplier * Time.deltaTime;
                    characterController.Move(movement);
                }
            }
        }
        
        private void HandleRotation()
        {
            if (!enableSnapTurn || !rightController.isValid) return;
            
            // Get thumbstick input from right controller
            if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 inputAxis))
            {
                // Check cooldown
                if (Time.time - lastSnapTurnTime < snapTurnCooldown) return;
                
                // Snap turn threshold
                if (Mathf.Abs(inputAxis.x) > 0.7f)
                {
                    float turnDirection = Mathf.Sign(inputAxis.x);
                    transform.Rotate(0, snapTurnAngle * turnDirection, 0);
                    lastSnapTurnTime = Time.time;
                }
            }
        }
        
        private void ApplyGravity()
        {
            if (characterController == null) return;
            
            if (characterController.isGrounded)
            {
                verticalVelocity = -0.5f; // Small downward force to keep grounded
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
            
            characterController.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
        }
        
        /// <summary>
        /// Teleport the player to a specific position
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            // Disable character controller temporarily for teleport
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        
        /// <summary>
        /// Set movement speed at runtime
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }
    }
}
