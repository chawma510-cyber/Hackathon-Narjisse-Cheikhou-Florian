using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HackathonVR
{
    /// <summary>
    /// Animated VR hand controller that responds to controller inputs.
    /// Works like VRChat hands - fingers curl based on trigger/grip/thumbstick input.
    /// 
    /// SETUP:
    /// 1. Import Oculus Integration package from Asset Store (free)
    /// 2. Find the hand prefabs in: Oculus/VR/Meshes/OculusTouchForQuest2/
    /// 3. Assign left/right hand prefabs to this script
    /// 4. The script will animate the fingers based on controller input
    /// </summary>
    public class VRAnimatedHand : MonoBehaviour
    {
        [Header("Hand Configuration")]
        [SerializeField] private HandSide handSide = HandSide.Right;
        [SerializeField] private GameObject handModelPrefab;
        [SerializeField] private bool hideControllerVisual = true;
        [SerializeField] private Vector3 proceduralRotationOffset = new Vector3(90f, 0f, 0f); // Adjusted to align with wrist
        [SerializeField] private Vector3 fingerRotationOffset = new Vector3(90f, 0f, 0f); // Fingers rotated 90 deg relative to hand
        
        [Header("Animation Settings")]
        [SerializeField] private float fingerSpeed = 15f;
        [SerializeField] private bool usePhysicsFingers = false;
        
        [Header("Finger Curl Ranges (degrees)")]
        [SerializeField] private float indexCurlMin = 0f;
        [SerializeField] private float indexCurlMax = 90f;
        [SerializeField] private float middleCurlMin = 0f;
        [SerializeField] private float middleCurlMax = 90f;
        [SerializeField] private float ringCurlMin = 0f;
        [SerializeField] private float ringCurlMax = 90f;
        [SerializeField] private float pinkyCurlMin = 0f;
        [SerializeField] private float pinkyCurlMax = 90f;
        [SerializeField] private float thumbCurlMin = 0f;
        [SerializeField] private float thumbCurlMax = 45f;
        
        public enum HandSide { Left, Right }
        
        // Finger transforms
        private Transform thumbRoot, thumbMiddle, thumbTip;
        private Transform indexRoot, indexMiddle, indexTip;
        private Transform middleRoot, middleMiddle, middleTip;
        private Transform ringRoot, ringMiddle, ringTip;
        private Transform pinkyRoot, pinkyMiddle, pinkyTip;
        
        // Current curl values (0-1)
        private float indexCurl = 0f;
        private float gripCurl = 0f;   // Controls middle, ring, pinky
        private float thumbCurl = 0f;
        
        // Target curl values
        private float targetIndexCurl = 0f;
        private float targetGripCurl = 0f;
        private float targetThumbCurl = 0f;
        
        // Controller
        private InputDevice controller;
        private bool controllerFound = false;
        
        // Spawned hand
        private GameObject spawnedHand;
        private Animator handAnimator;
        private bool useAnimator = false;
        
        // Animator parameter hashes
        private static readonly int GripHash = Animator.StringToHash("Grip");
        private static readonly int TriggerHash = Animator.StringToHash("Trigger");
        private static readonly int ThumbHash = Animator.StringToHash("Thumb");
        
        private void Start()
        {
            TryFindController();
            SpawnHandModel();
            
            if (hideControllerVisual)
            {
                HideDefaultVisual();
            }
        }
        
        private void SpawnHandModel()
        {
            if (handModelPrefab != null)
            {
                spawnedHand = Instantiate(handModelPrefab, transform);
                spawnedHand.transform.localPosition = Vector3.zero;
                spawnedHand.transform.localRotation = Quaternion.identity;
                
                // Try to get animator (Oculus hands have animators)
                handAnimator = spawnedHand.GetComponent<Animator>();
                if (handAnimator == null)
                {
                    handAnimator = spawnedHand.GetComponentInChildren<Animator>();
                }
                
                if (handAnimator != null)
                {
                    useAnimator = true;
                    Debug.Log($"[VRAnimatedHand] Using Animator for {handSide} hand");
                }
                else
                {
                    // Try to find finger bones for manual animation
                    FindFingerBones();
                }
            }
            else
            {
                // Create a simple procedural hand if no prefab
                CreateProceduralHand();
            }
        }
        
        private void CreateProceduralHand()
        {
            spawnedHand = new GameObject("ProceduralHand");
            spawnedHand.transform.SetParent(transform);
            spawnedHand.transform.localPosition = Vector3.zero;
            
            // Rotate hand to correct orientation - palm down, fingers pointing forward
            // Left hand needs to be mirrored
            float mirror = handSide == HandSide.Left ? -1f : 1f;
            spawnedHand.transform.localRotation = Quaternion.Euler(proceduralRotationOffset);
            
            // Palm - flat box
            var palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = "Palm";
            palm.transform.SetParent(spawnedHand.transform);
            palm.transform.localPosition = new Vector3(0, 0, -0.02f);
            palm.transform.localScale = new Vector3(0.08f * mirror, 0.025f, 0.1f);
            palm.transform.localRotation = Quaternion.identity;
            Destroy(palm.GetComponent<Collider>());
            SetHandMaterial(palm);
            
            // Finger positions (X offset from center, Z offset from palm)
            // Order: Thumb, Index, Middle, Ring, Pinky
            // Corrected for Palm Down: Thumb should be Left (-X) for Right Hand, Right (+X) for Left Hand
            // Since mirror is 1 for Right, we negate the values to put Thumb on Left (-0.045)
            float[] fingerXOffsets = { -0.045f * mirror, -0.025f * mirror, 0f, 0.025f * mirror, 0.045f * mirror };
            float[] fingerZOffsets = { 0.02f, 0.05f, 0.055f, 0.05f, 0.04f }; // Thumb starts lower
            float[] fingerLengths = { 0.04f, 0.065f, 0.075f, 0.07f, 0.055f }; // Segment length
            float[] fingerWidths = { 0.018f, 0.015f, 0.016f, 0.015f, 0.013f };
            string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
            
            for (int i = 0; i < 5; i++)
            {
                CreateProceduralFinger(
                    spawnedHand.transform, 
                    fingerNames[i], 
                    new Vector3(fingerXOffsets[i], 0, fingerZOffsets[i]), 
                    fingerLengths[i],
                    fingerWidths[i],
                    i == 0, // isThumb
                    mirror
                );
            }
            
            FindFingerBones();
            Debug.Log($"[VRAnimatedHand] Created procedural {handSide} hand");
        }
        
        private void CreateProceduralFinger(Transform parent, string name, Vector3 offset, float segmentLength, float width, bool isThumb, float mirror)
        {
            // Root segment - attached to palm
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = name + "_Root";
            root.transform.SetParent(parent);
            root.transform.localPosition = offset;
            
            // Thumb rotates outward, other fingers point forward
            if (isThumb)
            {
                root.transform.localRotation = Quaternion.Euler(0f, -30f * mirror, -60f * mirror);
            }
            else
            {
                // Default was -90 (pointing forward Z from Y-up capsule)
                // Add configurable offset
                root.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(fingerRotationOffset);
            }
            
            // Scale: capsule is 2 units tall by default, so height = scale.y * 2
            root.transform.localScale = new Vector3(width, segmentLength * 0.5f, width);
            Destroy(root.GetComponent<Collider>());
            SetHandMaterial(root);
            
            // Middle segment
            var middle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            middle.name = name + "_Middle";
            middle.transform.SetParent(root.transform);
            // Position at end of previous segment (local Y direction)
            middle.transform.localPosition = new Vector3(0, 2f, 0); // 2 = full capsule length in local space
            middle.transform.localRotation = Quaternion.identity;
            middle.transform.localScale = new Vector3(0.9f, 0.85f, 0.9f); // Slightly smaller
            Destroy(middle.GetComponent<Collider>());
            SetHandMaterial(middle);
            
            // Tip segment
            var tip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tip.name = name + "_Tip";
            tip.transform.SetParent(middle.transform);
            tip.transform.localPosition = new Vector3(0, 2f, 0);
            tip.transform.localRotation = Quaternion.identity;
            tip.transform.localScale = new Vector3(0.85f, 0.7f, 0.85f); // Even smaller for fingertip
            Destroy(tip.GetComponent<Collider>());
            SetHandMaterial(tip);
        }
        
        private void SetHandMaterial(GameObject obj)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.9f, 0.75f, 0.65f); // Skin tone
                mat.SetFloat("_Glossiness", 0.3f);
                renderer.material = mat;
            }
        }
        
        private void FindFingerBones()
        {
            if (spawnedHand == null) return;
            
            // Find finger transforms by name
            thumbRoot = FindBone("Thumb_Root", "thumb_0", "thumb0");
            thumbMiddle = FindBone("Thumb_Middle", "thumb_1", "thumb1");
            thumbTip = FindBone("Thumb_Tip", "thumb_2", "thumb2");
            
            indexRoot = FindBone("Index_Root", "index_0", "index1");
            indexMiddle = FindBone("Index_Middle", "index_1", "index2");
            indexTip = FindBone("Index_Tip", "index_2", "index3");
            
            middleRoot = FindBone("Middle_Root", "middle_0", "middle1");
            middleMiddle = FindBone("Middle_Middle", "middle_1", "middle2");
            middleTip = FindBone("Middle_Tip", "middle_2", "middle3");
            
            ringRoot = FindBone("Ring_Root", "ring_0", "ring1");
            ringMiddle = FindBone("Ring_Middle", "ring_1", "ring2");
            ringTip = FindBone("Ring_Tip", "ring_2", "ring3");
            
            pinkyRoot = FindBone("Pinky_Root", "pinky_0", "pinky1");
            pinkyMiddle = FindBone("Pinky_Middle", "pinky_1", "pinky2");
            pinkyTip = FindBone("Pinky_Tip", "pinky_2", "pinky3");
        }
        
        private Transform FindBone(params string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                var found = spawnedHand.transform.Find(name);
                if (found != null) return found;
                
                // Search recursively
                found = FindDeepChild(spawnedHand.transform, name);
                if (found != null) return found;
            }
            return null;
        }
        
        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                    return child;
                
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }
        
        private void HideDefaultVisual()
        {
            // Hide the default sphere visual
            var visual = transform.Find("Visual");
            if (visual != null)
            {
                visual.gameObject.SetActive(false);
            }
            
            // Also hide the pointer line if present
            var pointer = transform.Find("Pointer");
            if (pointer != null)
            {
                pointer.gameObject.SetActive(false);
            }
        }
        
        private void TryFindController()
        {
            InputDeviceCharacteristics characteristics = 
                InputDeviceCharacteristics.Controller |
                (handSide == HandSide.Left ? 
                    InputDeviceCharacteristics.Left : 
                    InputDeviceCharacteristics.Right);
            
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            
            if (devices.Count > 0)
            {
                controller = devices[0];
                controllerFound = true;
                Debug.Log($"[VRAnimatedHand] Found {handSide} controller");
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
            
            UpdateInputs();
            UpdateFingerAnimation();
        }
        
        private void UpdateInputs()
        {
            // Get trigger value (0-1) for index finger
            if (controller.TryGetFeatureValue(CommonUsages.trigger, out float trigger))
            {
                targetIndexCurl = trigger;
            }
            
            // Get grip value (0-1) for middle, ring, pinky
            if (controller.TryGetFeatureValue(CommonUsages.grip, out float grip))
            {
                targetGripCurl = grip;
            }
            
            // Get thumb input from thumbstick touch or button press
            float thumbValue = 0f;
            
            if (controller.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out bool thumbstickTouch) && thumbstickTouch)
            {
                thumbValue = 0.5f;
            }
            if (controller.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
            {
                thumbValue = 1f;
            }
            if (controller.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryPressed) && secondaryPressed)
            {
                thumbValue = 1f;
            }
            
            targetThumbCurl = thumbValue;
        }
        
        private void UpdateFingerAnimation()
        {
            // Smooth lerp current values to targets
            indexCurl = Mathf.Lerp(indexCurl, targetIndexCurl, fingerSpeed * Time.deltaTime);
            gripCurl = Mathf.Lerp(gripCurl, targetGripCurl, fingerSpeed * Time.deltaTime);
            thumbCurl = Mathf.Lerp(thumbCurl, targetThumbCurl, fingerSpeed * Time.deltaTime);
            
            if (useAnimator && handAnimator != null)
            {
                // Use animator parameters (for Oculus hands)
                handAnimator.SetFloat(TriggerHash, indexCurl);
                handAnimator.SetFloat(GripHash, gripCurl);
                
                // Some hand models have a Thumb parameter
                if (HasParameter(handAnimator, "Thumb"))
                {
                    handAnimator.SetFloat(ThumbHash, thumbCurl);
                }
            }
            else
            {
                // Manual bone rotation
                AnimateFinger(indexRoot, indexMiddle, indexTip, indexCurl, indexCurlMin, indexCurlMax);
                AnimateFinger(middleRoot, middleMiddle, middleTip, gripCurl, middleCurlMin, middleCurlMax);
                AnimateFinger(ringRoot, ringMiddle, ringTip, gripCurl, ringCurlMin, ringCurlMax);
                AnimateFinger(pinkyRoot, pinkyMiddle, pinkyTip, gripCurl, pinkyCurlMin, pinkyCurlMax);
                AnimateFinger(thumbRoot, thumbMiddle, thumbTip, thumbCurl, thumbCurlMin, thumbCurlMax);
            }
        }
        
        private void AnimateFinger(Transform root, Transform middle, Transform tip, float curl, float minAngle, float maxAngle)
        {
            if (root == null) return;
            
            float angle = Mathf.Lerp(minAngle, maxAngle, curl);
            Quaternion rotation = Quaternion.Euler(angle, 0, 0);
            
            root.localRotation = rotation;
            if (middle != null) middle.localRotation = rotation;
            if (tip != null) tip.localRotation = rotation;
        }
        
        private bool HasParameter(Animator animator, string paramName)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }
        
        private void OnDestroy()
        {
            if (spawnedHand != null)
            {
                Destroy(spawnedHand);
            }
        }
    }
}
