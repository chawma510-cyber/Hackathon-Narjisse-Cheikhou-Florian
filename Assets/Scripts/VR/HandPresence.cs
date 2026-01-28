using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

namespace HackathonVR
{
    /// <summary>
    /// Manages hand presence and controller visualization in VR.
    /// Provides haptic feedback and controller state information.
    /// </summary>
    public class HandPresence : MonoBehaviour
    {
        [Header("Hand Configuration")]
        [SerializeField] private InputDeviceCharacteristics controllerCharacteristics;
        [SerializeField] private GameObject handModelPrefab;
        [SerializeField] private bool showController = true;

        [Header("Haptic Settings")]
        [SerializeField] private float hapticAmplitude = 0.5f;
        [SerializeField] private float hapticDuration = 0.1f;

        private InputDevice targetDevice;
        private GameObject spawnedHandModel;
        private Animator handAnimator;
        private bool deviceFound = false;

        // Animation parameter hashes for performance
        private static readonly int TriggerHash = Animator.StringToHash("Trigger");
        private static readonly int GripHash = Animator.StringToHash("Grip");

        private void Start()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);

            if (devices.Count > 0)
            {
                targetDevice = devices[0];
                deviceFound = true;
                Debug.Log($"[HandPresence] Found device: {targetDevice.name}");

                if (handModelPrefab != null)
                {
                    spawnedHandModel = Instantiate(handModelPrefab, transform);
                    handAnimator = spawnedHandModel.GetComponent<Animator>();
                }
            }
        }

        private void Update()
        {
            if (!deviceFound)
            {
                TryInitialize();
                return;
            }

            if (!targetDevice.isValid)
            {
                deviceFound = false;
                if (spawnedHandModel != null)
                {
                    Destroy(spawnedHandModel);
                }
                return;
            }

            UpdateHandAnimation();
        }

        private void UpdateHandAnimation()
        {
            if (handAnimator == null) return;

            // Update trigger animation
            if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
            {
                handAnimator.SetFloat(TriggerHash, triggerValue);
            }

            // Update grip animation
            if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
            {
                handAnimator.SetFloat(GripHash, gripValue);
            }
        }

        /// <summary>
        /// Send haptic feedback to the controller
        /// </summary>
        public void TriggerHaptic()
        {
            TriggerHaptic(hapticAmplitude, hapticDuration);
        }

        /// <summary>
        /// Send haptic feedback with custom amplitude and duration
        /// </summary>
        public void TriggerHaptic(float amplitude, float duration)
        {
            if (!deviceFound || !targetDevice.isValid) return;

            HapticCapabilities capabilities;
            if (targetDevice.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                targetDevice.SendHapticImpulse(0, amplitude, duration);
            }
        }

        /// <summary>
        /// Check if the primary button is pressed
        /// </summary>
        public bool IsPrimaryButtonPressed()
        {
            if (targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed))
            {
                return pressed;
            }
            return false;
        }

        /// <summary>
        /// Check if the trigger is pressed
        /// </summary>
        public bool IsTriggerPressed()
        {
            if (targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed))
            {
                return pressed;
            }
            return false;
        }

        /// <summary>
        /// Get the trigger value (0-1)
        /// </summary>
        public float GetTriggerValue()
        {
            if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float value))
            {
                return value;
            }
            return 0f;
        }

        /// <summary>
        /// Get the grip value (0-1)
        /// </summary>
        public float GetGripValue()
        {
            if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float value))
            {
                return value;
            }
            return 0f;
        }

        /// <summary>
        /// Get the thumbstick/joystick value
        /// </summary>
        public Vector2 GetThumbstickValue()
        {
            if (targetDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 value))
            {
                return value;
            }
            return Vector2.zero;
        }
    }
}
