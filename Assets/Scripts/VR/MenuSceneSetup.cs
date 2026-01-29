using UnityEngine;

namespace HackathonVR
{
    /// <summary>
    /// Setup script for the Menu scene.
    /// Creates the menu environment and XR rig for the welcome screen.
    /// </summary>
    public class MenuSceneSetup : MonoBehaviour
    {
        [Header("Environment")]
        [SerializeField] private Color ambientLightColor = new Color(0.1f, 0.15f, 0.25f);
        [SerializeField] private Color fogColor = new Color(0.02f, 0.03f, 0.08f);
        [SerializeField] private float fogDensity = 0.03f;
        
        [Header("Skybox")]
        [SerializeField] private Color skyboxTop = new Color(0.02f, 0.02f, 0.1f);
        [SerializeField] private Color skyboxBottom = new Color(0.05f, 0.08f, 0.15f);
        
        private void Start()
        {
            SetupEnvironment();
            CreateFloor();
            CreateAmbientParticles();
            CreateDecoLights();
            
            // Ensure VR is set up
            if (FindFirstObjectByType<XRSetup>() == null)
            {
                var xrSetup = new GameObject("VR Setup");
                xrSetup.AddComponent<XRSetup>();
            }
            
            // Add menu
            if (FindFirstObjectByType<UI.VRMainMenu>() == null)
            {
                var menu = new GameObject("Main Menu");
                menu.AddComponent<UI.VRMainMenu>();
            }
            
            // Add music manager
            if (FindFirstObjectByType<MusicManager>() == null)
            {
                var music = new GameObject("Music Manager");
                var musicManager = music.AddComponent<MusicManager>();
            }
        }
        
        private void SetupEnvironment()
        {
            // Ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientLightColor;
            
            // Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            
            // Create gradient skybox
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = skyboxTop;
        }
        
        private void CreateFloor()
        {
            // Create a large floor with cool pattern
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "MenuFloor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(10, 1, 10);
            
            var floorMat = new Material(Shader.Find("Standard"));
            floorMat.color = new Color(0.08f, 0.1f, 0.15f);
            floorMat.SetFloat("_Glossiness", 0.8f);
            floorMat.SetFloat("_Metallic", 0.3f);
            floor.GetComponent<Renderer>().material = floorMat;
            
            // Add grid lines
            CreateFloorGrid();
        }
        
        private void CreateFloorGrid()
        {
            var gridParent = new GameObject("FloorGrid");
            
            float gridSize = 20f;
            float lineSpacing = 2f;
            float lineWidth = 0.02f;
            Color gridColor = new Color(0.2f, 0.4f, 0.6f, 0.5f);
            
            // Create grid lines
            for (float x = -gridSize; x <= gridSize; x += lineSpacing)
            {
                CreateGridLine(gridParent.transform, new Vector3(x, 0.001f, 0), new Vector3(lineWidth, 0.001f, gridSize * 2), gridColor);
            }
            
            for (float z = -gridSize; z <= gridSize; z += lineSpacing)
            {
                CreateGridLine(gridParent.transform, new Vector3(0, 0.001f, z), new Vector3(gridSize * 2, 0.001f, lineWidth), gridColor);
            }
        }
        
        private void CreateGridLine(Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GridLine";
            line.transform.SetParent(parent);
            line.transform.position = position;
            line.transform.localScale = scale;
            Destroy(line.GetComponent<Collider>());
            
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color;
            line.GetComponent<Renderer>().material = mat;
        }
        
        private void CreateAmbientParticles()
        {
            var particlesObj = new GameObject("AmbientParticles");
            var ps = particlesObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 8f;
            main.startSpeed = 0.2f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.3f, 0.5f, 1f, 0.3f);
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = ps.emission;
            emission.rateOverTime = 20;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15, 5, 15);
            shape.position = new Vector3(0, 2, 0);
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.3f, 0.5f, 1f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.3f, 1f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.5f, 1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.5f, 0.2f),
                    new GradientAlphaKey(0.5f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;
            
            // Use a simple additive material
            var psRenderer = particlesObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        }
        
        private void CreateDecoLights()
        {
            // Create floating orbs of light
            Color[] lightColors = {
                new Color(0.3f, 0.5f, 1f),
                new Color(0.5f, 0.3f, 1f),
                new Color(0.2f, 0.8f, 0.8f),
                new Color(1f, 0.3f, 0.5f)
            };
            
            Vector3[] positions = {
                new Vector3(-4, 2, 3),
                new Vector3(4, 1.5f, 4),
                new Vector3(-3, 3, -2),
                new Vector3(5, 2.5f, -3),
                new Vector3(0, 4, 5)
            };
            
            var lightsParent = new GameObject("DecoLights");
            
            for (int i = 0; i < positions.Length; i++)
            {
                CreateFloatingOrb(lightsParent.transform, positions[i], lightColors[i % lightColors.Length], i * 0.5f);
            }
        }
        
        private void CreateFloatingOrb(Transform parent, Vector3 position, Color color, float animOffset)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "FloatingOrb";
            orb.transform.SetParent(parent);
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one * 0.15f;
            Destroy(orb.GetComponent<Collider>());
            
            // Glowing material
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2f);
            orb.GetComponent<Renderer>().material = mat;
            
            // Add point light
            var light = orb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 1f;
            light.range = 5f;
            
            // Add floating animation
            var floater = orb.AddComponent<MenuFloatingOrb>();
            floater.Initialize(animOffset);
        }
    }
    
    /// <summary>
    /// Simple floating animation for decorative orbs in menu
    /// </summary>
    public class MenuFloatingOrb : MonoBehaviour
    {
        private Vector3 startPos;
        private float animOffset;
        private float amplitude = 0.3f;
        private float speed = 1f;
        
        public void Initialize(float offset)
        {
            animOffset = offset;
            startPos = transform.position;
        }
        
        private void Update()
        {
            float y = Mathf.Sin((Time.time + animOffset) * speed) * amplitude;
            float x = Mathf.Cos((Time.time + animOffset) * speed * 0.7f) * amplitude * 0.5f;
            transform.position = startPos + new Vector3(x, y, 0);
        }
    }
}
