using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HackathonVR
{
    /// <summary>
    /// VR Locomotion System:
    /// - RIGHT Joystick: Smooth movement (forward/back/strafe left/right)
    /// - LEFT Joystick: Teleport with visual indicator + snap turn
    /// - Both Grips: Skip to next scene (debug)
    /// </summary>
    public class VRLocomotion : MonoBehaviour
    {
        [Header("Smooth Movement (Right Joystick)")]
        public float moveSpeed = 2f;
        public float strafeSpeed = 1.5f;

        [Header("Teleport (Left Joystick)")]
        public float teleportRange = 10f;
        public float teleportChargeTime = 0.5f;
        public Color teleportIndicatorColor = Color.cyan;

        [Header("Snap Turn (Left Joystick Left/Right)")]
        public float snapTurnAngle = 45f;
        public float snapTurnCooldown = 0.3f;

        [Header("Debug")]
        public float debugSkipHoldTime = 2f;

        // Controllers
        private InputDevice leftController;
        private InputDevice rightController;
        private bool controllersFound = false;

        // Teleport state
        private GameObject teleportIndicator;
        private float teleportChargeProgress = 0f;
        private Vector3 teleportTarget;

        // Cooldowns
        private float lastSnapTurnTime = 0f;
        private float debugGripHeldTime = 0f;
        private bool debugSkipped = false;

        private Transform vrRig;
        private Camera vrCamera;

        private void Start()
        {
            // Teleport removed
        }

        private void Update()
        {
            if (!controllersFound)
            {
                TryFindControllers();
                FindVRRig();
            }

            if (controllersFound && vrRig != null)
            {
                HandleSmoothMovement();  // Right joystick
                // HandleTeleport();     // DISABLED - Left joystick (up)
                HandleSnapTurn();        // Left joystick (left/right)
                HandleDebugSceneSkip();
            }
        }

        private void TryFindControllers()
        {
            List<InputDevice> leftDevices = new List<InputDevice>();
            List<InputDevice> rightDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftDevices);
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightDevices);

            if (leftDevices.Count > 0 && rightDevices.Count > 0)
            {
                leftController = leftDevices[0];
                rightController = rightDevices[0];
                controllersFound = true;
                Debug.Log("[VRLocomotion] Controllers found!");
            }
        }

        private void FindVRRig()
        {
            GameObject rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) rig = GameObject.Find("VR Setup");
            if (rig != null) vrRig = rig.transform;

            vrCamera = Camera.main;
        }

        // ==========================================
        // SMOOTH MOVEMENT (Right Joystick)
        // ==========================================
        private void HandleSmoothMovement()
        {
            Vector2 joystickInput = Vector2.zero;
            rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickInput);

            if (joystickInput.magnitude > 0.1f)
            {
                // Get camera forward/right for movement direction
                Vector3 forward = vrCamera.transform.forward;
                Vector3 right = vrCamera.transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                // Calculate movement
                Vector3 movement = (forward * joystickInput.y * moveSpeed) + 
                                   (right * joystickInput.x * strafeSpeed);
                
                vrRig.position += movement * Time.deltaTime;
            }
        }

        // ==========================================
        // SNAP TURN (Left Joystick Left/Right)
        // ==========================================
        private void HandleSnapTurn()
        {
            if (Time.time - lastSnapTurnTime < snapTurnCooldown) return;

            Vector2 joystickInput = Vector2.zero;
            leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickInput);

            // Snap turn
            if (joystickInput.x < -0.7f && Mathf.Abs(joystickInput.y) < 0.5f)
            {
                vrRig.Rotate(0, -snapTurnAngle, 0);
                lastSnapTurnTime = Time.time;
            }
            else if (joystickInput.x > 0.7f && Mathf.Abs(joystickInput.y) < 0.5f)
            {
                vrRig.Rotate(0, snapTurnAngle, 0);
                lastSnapTurnTime = Time.time;
            }
        }

        // ==========================================
        // DEBUG SCENE SKIP (Both Grips)
        // ==========================================
        private void HandleDebugSceneSkip()
        {
            bool leftGrip = false;
            bool rightGrip = false;

            leftController.TryGetFeatureValue(CommonUsages.gripButton, out leftGrip);
            rightController.TryGetFeatureValue(CommonUsages.gripButton, out rightGrip);

            if (leftGrip && rightGrip)
            {
                debugGripHeldTime += Time.deltaTime;

                if (debugGripHeldTime >= debugSkipHoldTime && !debugSkipped)
                {
                    debugSkipped = true;
                    var gm = Core.GameManager.Instance;
                    if (gm != null)
                    {
                        Debug.Log("[VRLocomotion] DEBUG: Skipping to next scene!");
                        gm.LoadNextScene();
                    }
                }
            }
            else
            {
                debugGripHeldTime = 0f;
                debugSkipped = false;
            }
        }
    }
}
