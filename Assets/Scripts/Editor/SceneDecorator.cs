using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HackathonVR.Core;
using HackathonVR.Gameplay;

namespace HackathonVR.Editor
{
    public class SceneDecorator : MonoBehaviour
    {
        [MenuItem("Hackathon/Setup Scene 1 (Jardin - Intro)", false, 1)]
        public static void SetupScene1()
        {
            SetupScene("1", "Assets/Scenes/1.unity");
            SpawnBook();
        }

        [MenuItem("Hackathon/Setup Scene 2 (Réduit)", false, 2)]
        public static void SetupScene2()
        {
            SetupScene("2", "Assets/Scenes/2.unity");
        }

        [MenuItem("Hackathon/Setup Scene 3 (Nuit - Lampe)", false, 3)]
        public static void SetupScene3()
        {
            SetupScene("3", "Assets/Scenes/3.unity");
            SpawnBees();
        }

        [MenuItem("Hackathon/Setup Scene 4", false, 4)]
        public static void SetupScene4()
        {
            SetupScene("4", "Assets/Scenes/4.unity");
        }
        
        [MenuItem("Hackathon/Setup/Force Refresh VR", false, 50)]
        public static void RefreshVR()
        {
            GameObject setup = GameObject.Find("VR Setup");
            if (setup != null) DestroyImmediate(setup);
            
            var go = new GameObject("VR Setup");
            var script = go.AddComponent<XRSetup>();
            script.SetupXR();
        }

        private static void SetupScene(string sceneShortName, string scenePath)
        {
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                if (EditorUtility.DisplayDialog("Open Scene", $"Open Scene {sceneShortName}?", "Yes", "No"))
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }

            // 1. Create Managers if needed
            EnsureManager<GameManager>("GameManager");
            EnsureManager<SceneSpawnManager>("SceneSpawnManager");
            EnsureManager<DialogueManager>("DialogueManager");
            EnsureManager<MusicManager>("MusicManager");

            // 2. Setup VR
            GameObject vrSetup = GameObject.Find("VR Setup");
            if (vrSetup == null)
            {
                vrSetup = new GameObject("VR Setup");
                var xrSetup = vrSetup.AddComponent<XRSetup>();
                xrSetup.SetupXR();
            }

            // 3. Apply Spawn
            GameObject spawnMgrGO = GameObject.Find("SceneSpawnManager");
            if (spawnMgrGO != null)
            {
                var mgr = spawnMgrGO.GetComponent<SceneSpawnManager>();
                mgr.ApplySpawnForScene(sceneShortName);
            }
            
            Debug.Log($"[SceneDecorator] Setup complete for Scene {sceneShortName}");
        }

        private static void SpawnBees()
        {
            if (GameObject.Find("Bees_Parent") != null) return;

            GameObject beesParent = new GameObject("Bees_Parent");
            
            // Spawn 3 bees
            for (int i = 0; i < 3; i++)
            {
                if (System.Type.GetType("HackathonVR.Gameplay.BeeChase") == null) continue;

                GameObject bee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bee.name = $"Bee_{i}";
                bee.transform.SetParent(beesParent.transform);
                // Position randomly around Scene 3 spawn
                bee.transform.position = new Vector3(5.4f + Random.Range(-5, 5), 1f, 7.3f + Random.Range(5, 15));
                bee.transform.localScale = Vector3.one * 0.3f;
                
                var rend = bee.GetComponent<Renderer>();
                if (rend != null) rend.material.color = Color.yellow;
                
                // Add NavMeshAgent
                var agent = bee.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed = 3.5f;
                agent.radius = 0.2f;
                agent.height = 0.5f;
                
                // Add BeeChase
                bee.AddComponent<BeeChase>();
            }
            
            Debug.Log("[SceneDecorator] Spawned Bees for Scene 3");
            
            // Create HideSpot
            GameObject hideSpot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hideSpot.name = "HideSpot";
            hideSpot.transform.SetParent(beesParent.transform);
            hideSpot.transform.position = new Vector3(5.4f, 0.5f, 15f); // Somewhere ahead
            hideSpot.transform.localScale = new Vector3(2, 2, 2);
            hideSpot.GetComponent<Collider>().isTrigger = true;
            
            var hideMat = hideSpot.GetComponent<Renderer>().material;
            hideMat.color = new Color(0, 1, 0, 0.3f);
            SetTransparent(hideMat);
            
            if (System.Type.GetType("HackathonVR.Gameplay.HideSpot") != null)
            {
                hideSpot.AddComponent<HideSpot>();
            }
        }

        private static void SpawnBook()
        {
            // 1. Ensure Story Manager exists
            var storyMgrGO = GameObject.Find("StoryManager");
            if (storyMgrGO == null)
            {
                storyMgrGO = new GameObject("StoryManager");
                storyMgrGO.AddComponent<StoryManager>();
            }
            var storyManager = storyMgrGO.GetComponent<StoryManager>();

            // 2. Spawn Book (Real Prefab)
            GameObject book = GameObject.Find("NarrativeBook");
            if (book == null)
            {
                // Try load prefab
                var bookPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Books/Prefabs/book_0001a.prefab");
                if (bookPrefab != null)
                {
                    book = PrefabUtility.InstantiatePrefab(bookPrefab) as GameObject;
                }
                else
                {
                    book = GameObject.CreatePrimitive(PrimitiveType.Cube); // Fallback
                    book.transform.localScale = new Vector3(0.3f, 0.05f, 0.4f);
                    var rend = book.GetComponent<Renderer>();
                    if (rend != null) rend.material.color = new Color(0.6f, 0.1f, 0.1f);
                }
                book.name = "NarrativeBook";
            }
            
            // Initial Position (Between Player and Narjisse - Approx)
            book.transform.position = new Vector3(3.5f, 1.0f, 1.0f); // Floats in air
            book.transform.rotation = Quaternion.Euler(60, 0, 0); // Angled for reading

            // Add Components
            var rb = book.GetComponent<Rigidbody>();
            if (rb == null) rb = book.AddComponent<Rigidbody>();
            rb.mass = 1f;

             // Add Interactable if missing
            if (book.GetComponent<HackathonVR.Interactions.VRGrabInteractable>() == null)
                book.AddComponent<HackathonVR.Interactions.VRGrabInteractable>();

            // Add Book Logic
            var bookLogic = book.GetComponent<BookLogic>();
            if (bookLogic == null) bookLogic = book.AddComponent<BookLogic>();
            
            // WIRE: Book -> StoryManager
            // We use UnityEvent.AddListener at runtime or safely here? 
            // Better to do it via script wiring if possible or verified.
            // Since UnityEvents don't serialize well via script on non-prefab instances without care, 
            // let's assign the book reference to StoryManager if we update StoryManager to have one.
            // Or simpler: StoryManager observes the book?
            // Let's stick to wiring:
            UnityEditor.Events.UnityEventTools.AddPersistentListener(bookLogic.onBookClosed, storyManager.OnBookFinished);


            // 3. Spawn Telescope
            GameObject telescope = GameObject.Find("Telescope");
            if (telescope == null)
            {
                var telePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmaceArt/it's 80's_FREE/Prefabs/Youth bedroom/Toys/EAThe80_Prop_Telescope_01a_PRE.prefab");
                 if (telePrefab != null)
                {
                    telescope = PrefabUtility.InstantiatePrefab(telePrefab) as GameObject;
                }
                else
                {
                    telescope = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // Fallback
                    telescope.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
                }
                telescope.name = "Telescope";
            }
            // Position near where Narjisse would stand
            telescope.transform.position = new Vector3(2.0f, 0.0f, 2.0f); 
            telescope.transform.LookAt(new Vector3(100, 100, 100)); // Look at sky

            // 4. Update StoryManager References
            storyManager.narjisseObject = GameObject.Find("Sequence4DS"); // Preferred name based on user screenshot
            if (storyManager.narjisseObject == null) storyManager.narjisseObject = GameObject.Find("Narjisse"); // Fallback
            
            // Find Narjisse dialogue if possible
            if (storyManager.narjisseObject != null)
                 storyManager.narjisseDialogue = storyManager.narjisseObject.GetComponent<SimpleDialogue>();
            
            storyManager.telescopeLookPoint = telescope.transform; // Look at telescope base/tube
            
            // 5. Wire Dialogue -> Book (Intro Dialogue, separate from Narjisse)
            // The intro dialogue seems to be separate? Or is it the same bubble?
            // Assuming "DialogueManager" or a global "SimpleDialogue" handles the intro.
            var introDialogue = FindFirstObjectByType<SimpleDialogue>(); 
            // Warning: FindFirstObjectByType might find the Narjisse dialogue if it's the only one. 
            // We need to distinguish between "Intro" and "Narjisse".
            // If they are the same object, logic holds. If distinct, we might need names.
            
            if (introDialogue != null)
            {
                // Only set if this is the intro dialogue (e.g. check lines or name)
                // For now, let's assume valid wiring if we rely on Scene events.
                introDialogue.objectToActivateOnFinish = book;
                book.SetActive(false); 
                Debug.Log("[SceneDecorator] Wired and Configured Narrative Items.");
            }
        }

        private static void EnsureManager<T>(string name) where T : Component
        {
            if (GameObject.Find(name) == null)
            {
                var go = new GameObject(name);
                go.AddComponent<T>();
            }
        }
        
        private static void SetTransparent(Material mat)
        {
             mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
             mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
             mat.SetInt("_ZWrite", 0);
             mat.DisableKeyword("_ALPHATEST_ON");
             mat.DisableKeyword("_ALPHABLEND_ON");
             mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
             mat.renderQueue = 3000;
        }
    }
}
