using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Collections.Generic;
using HackathonVR.Interactions;

namespace HackathonVR
{
    /// <summary>
    /// Automatically sets up a complete XR Rig at runtime.
    /// Just add this script to an empty GameObject in your scene.
    /// </summary>
    public class XRSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        public bool createFloor = false;
        public bool createInteractionManager = true;
        public bool createDecor = false;
        public bool enableGrabInteraction = true;
        public bool createGrabbableTestObjects = false;
        
        private Camera vrCamera;
        private Transform cameraTransform;
        
        private void Awake()
        {
            SetupXR();
        }
        
        private void SetupXR()
        {
            // FORCE DISABLE EXTRA CONTENT (Fix User Request: No Table/Floor/Decor)
            createFloor = false;
            createDecor = false;
            createGrabbableTestObjects = false;

            // Create XR Interaction Manager if needed
            if (createInteractionManager && FindFirstObjectByType<XRInteractionManager>() == null)
            {
                var interactionManager = new GameObject("XR Interaction Manager");
                interactionManager.AddComponent<XRInteractionManager>();
                Debug.Log("[XRSetup] Created XR Interaction Manager");
            }
            
            // Check if XR Origin already exists
            if (FindFirstObjectByType<XROrigin>() != null)
            {
                Debug.Log("[XRSetup] XR Origin already exists in scene");
                return;
            }
            
            // Create XR Origin
            var xrOrigin = new GameObject("XR Origin (XR Rig)");
            var origin = xrOrigin.AddComponent<XROrigin>();
            
            // Create Camera Offset
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOrigin.transform);
            cameraOffset.transform.localPosition = Vector3.zero;
            
            // Create Main Camera
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            cameraGO.transform.SetParent(cameraOffset.transform);
            cameraGO.transform.localPosition = Vector3.zero;
            
            vrCamera = cameraGO.AddComponent<Camera>();
            vrCamera.clearFlags = CameraClearFlags.Skybox;
            vrCamera.nearClipPlane = 0.1f;
            vrCamera.farClipPlane = 1000f;
            vrCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            
            cameraGO.AddComponent<AudioListener>();
            cameraTransform = cameraGO.transform;
            
            // Configure XR Origin
            origin.Camera = vrCamera;
            origin.CameraFloorOffsetObject = cameraOffset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            
            // Create Left Controller
            var leftController = CreateController("Left Controller", cameraOffset.transform, true);
            
            // Create Right Controller  
            var rightController = CreateController("Right Controller", cameraOffset.transform, false);
            
            Debug.Log("[XRSetup] XR Origin created successfully!");
            
            // Create floor if needed
            if (createFloor)
            {
                CreateFloor();
            }
            
            // Create decor if needed
            if (createDecor)
            {
                CreateDecor();
            }
            
            // Create grabbable test objects
            if (createGrabbableTestObjects && enableGrabInteraction)
            {
                CreateGrabbableObjects();
            }
            
            // Destroy any old cameras that aren't ours
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam != vrCamera && cam.gameObject != cameraGO)
                {
                    Debug.Log($"[XRSetup] Removing duplicate camera: {cam.gameObject.name}");
                    Destroy(cam.gameObject);
                }
            }
        }
        
        private void Update()
        {
            UpdateHeadTracking();
        }
        
        private void UpdateHeadTracking()
        {
            if (cameraTransform == null) return;
            
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            
            if (devices.Count > 0)
            {
                InputDevice headDevice = devices[0];
                
                if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
                {
                    cameraTransform.localPosition = position;
                }
                
                if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                {
                    cameraTransform.localRotation = rotation;
                }
            }
        }
        
        private GameObject CreateController(string name, Transform parent, bool isLeft)
        {
            var controller = new GameObject(name);
            controller.transform.SetParent(parent);
            controller.transform.localPosition = Vector3.zero;
            
            var characteristics = isLeft 
                ? InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller
                : InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            
            var tracker = controller.AddComponent<ControllerTracker>();
            tracker.Initialize(characteristics, isLeft);
            
            // Add VRGrabber for interaction
            if (enableGrabInteraction)
            {
                var grabber = controller.AddComponent<VRGrabber>();
                Debug.Log($"[XRSetup] Added VRGrabber to {name}");
            }
            
            // Add VRTeleporter to right controller only
            if (!isLeft && enableGrabInteraction)
            {
                var teleporter = controller.AddComponent<VRTeleporter>();
                Debug.Log($"[XRSetup] Added VRTeleporter to {name}");
            }
            
            // Add animated hand
            var animatedHand = controller.AddComponent<VRAnimatedHand>();
            Debug.Log($"[XRSetup] Added VRAnimatedHand to {name}");
            
            // Create controller visual
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(controller.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.08f;
            
            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) Destroy(visualCollider);
            
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateEmissiveMaterial(isLeft ? new Color(0.2f, 0.4f, 1f) : new Color(1f, 0.3f, 0.2f));
            }
            
            // Add a pointer line
            var pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.name = "Pointer";
            pointer.transform.SetParent(controller.transform);
            pointer.transform.localPosition = new Vector3(0, 0, 0.15f);
            pointer.transform.localScale = new Vector3(0.005f, 0.005f, 0.3f);
            
            var pointerCollider = pointer.GetComponent<Collider>();
            if (pointerCollider != null) Destroy(pointerCollider);
            
            var pointerRenderer = pointer.GetComponent<Renderer>();
            if (pointerRenderer != null)
            {
                pointerRenderer.material = CreateEmissiveMaterial(isLeft ? new Color(0.3f, 0.5f, 1f) : new Color(1f, 0.4f, 0.3f));
            }
            
            return controller;
        }
        
        private void CreateFloor()
        {
            if (GameObject.Find("Floor") != null) return;
            
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.05f, 0);
            floor.transform.localScale = new Vector3(30, 0.1f, 30);
            
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.15f, 0.15f, 0.2f);
                mat.SetFloat("_Glossiness", 0.8f);
                mat.SetFloat("_Metallic", 0.3f);
                renderer.material = mat;
            }
            
            Debug.Log("[XRSetup] Floor created");
        }
        
        private void CreateDecor()
        {
            var decorParent = new GameObject("Decor");
            
            // Create room walls
            CreateWall(decorParent.transform, new Vector3(0, 2, 15), new Vector3(30, 4, 0.2f), new Color(0.3f, 0.35f, 0.4f));  // Back
            CreateWall(decorParent.transform, new Vector3(0, 2, -15), new Vector3(30, 4, 0.2f), new Color(0.3f, 0.35f, 0.4f)); // Front
            CreateWall(decorParent.transform, new Vector3(15, 2, 0), new Vector3(0.2f, 4, 30), new Color(0.25f, 0.3f, 0.35f)); // Right
            CreateWall(decorParent.transform, new Vector3(-15, 2, 0), new Vector3(0.2f, 4, 30), new Color(0.25f, 0.3f, 0.35f)); // Left
            
            // Create some floating cubes with different colors
            CreateFloatingCube(decorParent.transform, new Vector3(3, 1.2f, 3), 0.5f, new Color(0.9f, 0.2f, 0.3f));
            CreateFloatingCube(decorParent.transform, new Vector3(-3, 0.8f, 2), 0.4f, new Color(0.2f, 0.8f, 0.3f));
            CreateFloatingCube(decorParent.transform, new Vector3(0, 1.5f, 5), 0.6f, new Color(0.2f, 0.4f, 0.9f));
            CreateFloatingCube(decorParent.transform, new Vector3(-2, 1f, -3), 0.35f, new Color(0.9f, 0.7f, 0.1f));
            CreateFloatingCube(decorParent.transform, new Vector3(4, 0.7f, -2), 0.45f, new Color(0.8f, 0.2f, 0.8f));
            
            // Create pillars
            CreatePillar(decorParent.transform, new Vector3(8, 0, 8), new Color(0.4f, 0.4f, 0.5f));
            CreatePillar(decorParent.transform, new Vector3(-8, 0, 8), new Color(0.4f, 0.4f, 0.5f));
            CreatePillar(decorParent.transform, new Vector3(8, 0, -8), new Color(0.4f, 0.4f, 0.5f));
            CreatePillar(decorParent.transform, new Vector3(-8, 0, -8), new Color(0.4f, 0.4f, 0.5f));
            
            // Create some spheres
            CreateSphere(decorParent.transform, new Vector3(5, 0.5f, 0), 0.5f, new Color(1f, 0.5f, 0f));
            CreateSphere(decorParent.transform, new Vector3(-5, 0.7f, 1), 0.7f, new Color(0f, 0.8f, 0.8f));
            CreateSphere(decorParent.transform, new Vector3(0, 0.4f, -4), 0.4f, new Color(0.8f, 0.8f, 0.2f));
            
            // Create a table
            CreateTable(decorParent.transform, new Vector3(0, 0, 2));
            
            // Add some point lights for atmosphere
            CreateLight(decorParent.transform, new Vector3(5, 3, 5), new Color(1f, 0.8f, 0.6f), 10f);
            CreateLight(decorParent.transform, new Vector3(-5, 3, -5), new Color(0.6f, 0.8f, 1f), 10f);
            CreateLight(decorParent.transform, new Vector3(0, 4, 0), new Color(1f, 1f, 1f), 15f);
            
            Debug.Log("[XRSetup] Decor created");
        }
        
        private void CreateGrabbableObjects()
        {
            var grabbablesParent = new GameObject("Grabbable Objects");
            
            // Create grabbable cubes on the table
            CreateGrabbableCube(grabbablesParent.transform, new Vector3(-0.3f, 0.9f, 2f), 0.12f, new Color(1f, 0.3f, 0.3f), "RedCube");
            CreateGrabbableCube(grabbablesParent.transform, new Vector3(0f, 0.9f, 2f), 0.1f, new Color(0.3f, 1f, 0.3f), "GreenCube");
            CreateGrabbableCube(grabbablesParent.transform, new Vector3(0.3f, 0.9f, 2f), 0.11f, new Color(0.3f, 0.3f, 1f), "BlueCube");
            
            // Create grabbable spheres
            CreateGrabbableSphere(grabbablesParent.transform, new Vector3(-0.5f, 0.95f, 2.3f), 0.07f, new Color(1f, 0.8f, 0f), "GoldBall");
            CreateGrabbableSphere(grabbablesParent.transform, new Vector3(0.5f, 0.95f, 2.3f), 0.08f, new Color(0f, 0.8f, 0.8f), "CyanBall");
            
            // Create a grabbable cylinder
            CreateGrabbableCylinder(grabbablesParent.transform, new Vector3(0f, 0.95f, 1.7f), 0.05f, 0.15f, new Color(0.8f, 0.2f, 0.8f), "PurpleCylinder");
            
            Debug.Log("[XRSetup] Grabbable test objects created on table");
        }
        
        private void CreateGrabbableCube(Transform parent, Vector3 position, float size, Color color, string name)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * size;
            
            // Add rigidbody
            var rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Add grabbable component
            cube.AddComponent<VRGrabInteractable>();
            
            // Set material
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateEmissiveMaterial(color, 0.3f);
            }
        }
        
        private void CreateGrabbableSphere(Transform parent, Vector3 position, float radius, Color color, string name)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent);
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * radius * 2f;
            
            var rb = sphere.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            sphere.AddComponent<VRGrabInteractable>();
            
            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateEmissiveMaterial(color, 0.4f);
            }
        }
        
        private void CreateGrabbableCylinder(Transform parent, Vector3 position, float radius, float height, Color color, string name)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent);
            cylinder.transform.position = position;
            cylinder.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
            
            var rb = cylinder.AddComponent<Rigidbody>();
            rb.mass = 0.25f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            cylinder.AddComponent<VRGrabInteractable>();
            
            var renderer = cylinder.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateEmissiveMaterial(color, 0.35f);
            }
        }
        
        private void CreateWall(Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            
            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Glossiness", 0.3f);
                renderer.material = mat;
            }
        }
        
        private void CreateFloatingCube(Transform parent, Vector3 position, float size, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "FloatingCube";
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * size;
            cube.transform.rotation = Quaternion.Euler(Random.Range(0, 45), Random.Range(0, 360), Random.Range(0, 45));
            
            // Add floating animation
            cube.AddComponent<FloatingObject>();
            
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateEmissiveMaterial(color, 0.5f);
            }
        }
        
        private void CreatePillar(Transform parent, Vector3 position, Color color)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.SetParent(parent);
            pillar.transform.position = position + new Vector3(0, 2, 0);
            pillar.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
            
            var renderer = pillar.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Glossiness", 0.6f);
                mat.SetFloat("_Metallic", 0.4f);
                renderer.material = mat;
            }
        }
        
        private void CreateSphere(Transform parent, Vector3 position, float radius, Color color)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Sphere";
            sphere.transform.SetParent(parent);
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * radius * 2;
            
            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateEmissiveMaterial(color, 0.3f);
            }
        }
        
        private void CreateTable(Transform parent, Vector3 position)
        {
            var table = new GameObject("Table");
            table.transform.SetParent(parent);
            table.transform.position = position;
            
            // Table top
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "TableTop";
            top.transform.SetParent(table.transform);
            top.transform.localPosition = new Vector3(0, 0.75f, 0);
            top.transform.localScale = new Vector3(1.5f, 0.1f, 1f);
            
            var topRenderer = top.GetComponent<Renderer>();
            if (topRenderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.4f, 0.25f, 0.15f);
                mat.SetFloat("_Glossiness", 0.7f);
                topRenderer.material = mat;
            }
            
            // Table legs
            float legHeight = 0.35f;
            float legSize = 0.08f;
            Vector3[] legPositions = {
                new Vector3(0.6f, legHeight, 0.4f),
                new Vector3(-0.6f, legHeight, 0.4f),
                new Vector3(0.6f, legHeight, -0.4f),
                new Vector3(-0.6f, legHeight, -0.4f)
            };
            
            foreach (var legPos in legPositions)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = "TableLeg";
                leg.transform.SetParent(table.transform);
                leg.transform.localPosition = legPos;
                leg.transform.localScale = new Vector3(legSize, 0.7f, legSize);
                
                var legRenderer = leg.GetComponent<Renderer>();
                if (legRenderer != null)
                {
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.35f, 0.2f, 0.1f);
                    legRenderer.material = mat;
                }
            }
        }
        
        private void CreateLight(Transform parent, Vector3 position, Color color, float range)
        {
            var lightGO = new GameObject("PointLight");
            lightGO.transform.SetParent(parent);
            lightGO.transform.position = position;
            
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
        }
        
        private Material CreateEmissiveMaterial(Color color, float emissionStrength = 1f)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionStrength);
            mat.SetFloat("_Glossiness", 0.8f);
            return mat;
        }
    }
    
    /// <summary>
    /// Simple floating animation for objects
    /// </summary>
    public class FloatingObject : MonoBehaviour
    {
        private Vector3 startPosition;
        private float randomOffset;
        
        private void Start()
        {
            startPosition = transform.position;
            randomOffset = Random.Range(0f, Mathf.PI * 2);
        }
        
        private void Update()
        {
            float y = Mathf.Sin(Time.time + randomOffset) * 0.15f;
            transform.position = startPosition + new Vector3(0, y, 0);
            transform.Rotate(Vector3.up, 20f * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Simple controller tracker using XR InputDevice API
    /// </summary>
    public class ControllerTracker : MonoBehaviour
    {
        private InputDeviceCharacteristics characteristics;
        private InputDevice device;
        private bool deviceFound = false;
        private bool isLeft;
        
        public void Initialize(InputDeviceCharacteristics chars, bool left)
        {
            characteristics = chars;
            isLeft = left;
            TryFindDevice();
        }
        
        private void TryFindDevice()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            
            if (devices.Count > 0)
            {
                device = devices[0];
                deviceFound = true;
                Debug.Log($"[ControllerTracker] Found {(isLeft ? "left" : "right")} controller: {device.name}");
            }
        }
        
        private void Update()
        {
            if (!deviceFound || !device.isValid)
            {
                TryFindDevice();
                return;
            }
            
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                transform.localPosition = position;
            }
            
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                transform.localRotation = rotation;
            }
        }
    }
}
