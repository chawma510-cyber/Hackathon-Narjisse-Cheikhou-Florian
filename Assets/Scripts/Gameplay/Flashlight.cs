using UnityEngine;
using HackathonVR.Interactions;
using UnityEngine.XR;

namespace HackathonVR.Gameplay
{
    [RequireComponent(typeof(VRGrabInteractable))]
    public class Flashlight : MonoBehaviour
    {
        [Header("Components")]
        public Light spotlight;
        public AudioSource audioSource;
        public AudioClip clickSound;

        [Header("Settings")]
        public bool startOn = false;

        private VRGrabInteractable interactable;
        private bool isOn = false;
        private bool wasPressed = false;

        private void Start()
        {
            interactable = GetComponent<VRGrabInteractable>();
            if (spotlight == null) spotlight = GetComponentInChildren<Light>();
            
            isOn = startOn;
            UpdateLightState();
        }

        private void Update()
        {
            if (interactable != null && interactable.IsGrabbed && interactable.CurrentGrabber != null)
            {
                // Use Primary Button (A on Quest Right, X on Quest Left) to toggle
                bool pressed = interactable.CurrentGrabber.IsButtonPressed(CommonUsages.primaryButton);
                
                // Toggle on button down
                if (pressed && !wasPressed)
                {
                    Toggle();
                }
                wasPressed = pressed;
            }
        }

        public void Toggle()
        {
            isOn = !isOn;
            UpdateLightState();
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        private void UpdateLightState()
        {
            if (spotlight != null)
            {
                spotlight.enabled = isOn;
            }
        }
    }
}
