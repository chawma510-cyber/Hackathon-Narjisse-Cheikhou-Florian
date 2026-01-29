using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SceneDecorator : EditorWindow
{
    /* 
    [MenuItem("Hackathon/Setup All Story Scenes (1-4)")]
    public static void SetupAllScenes() { ... }
    */

    [MenuItem("Hackathon/Setup VR (Ready for Play)")]
    public static void SetupVR()
    {
        Debug.Log("Adding VR Rig...");
        
        // 1. Remove default camera if it exists and is not VR
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.transform.root.name != "XR Origin (XR Rig)" && mainCam.transform.root.name != "VR Setup")
        {
            Debug.Log("Removing default Main Camera.");
            DestroyImmediate(mainCam.gameObject);
        }
        
        // 2. Add XRSetup if missing
        HackathonVR.XRSetup setupScript = Object.FindFirstObjectByType<HackathonVR.XRSetup>();
        
        if (setupScript == null)
        {
            GameObject setupGO = new GameObject("VR Setup");
            // Default position at 0,0,0
            setupGO.transform.position = Vector3.zero; 
            
            var xrScript = setupGO.AddComponent<HackathonVR.XRSetup>();
            
            // CRITICAL: Ensure NO Extra Content is spawned
            xrScript.createFloor = false;
            xrScript.createDecor = false;
            xrScript.createGrabbableTestObjects = false;
            
            // Enable Mechanics
            xrScript.createInteractionManager = true;
            xrScript.enableGrabInteraction = true;
            
            Debug.Log("VR Setup added. NO extra objects spawned.");
        }
        else
        {
            // If exists, FORCE disable the junk just in case
            setupScript.createFloor = false;
            setupScript.createDecor = false;
            setupScript.createGrabbableTestObjects = false;
            Debug.Log("VR Setup updated to be CLEAN (No floor/decor).");
        }

        // 3. Add Music Manager if missing
        if (Object.FindFirstObjectByType<HackathonVR.MusicManager>() == null)
        {
            GameObject musicGO = new GameObject("Music Manager");
            musicGO.AddComponent<HackathonVR.MusicManager>();
        }
        
        Debug.Log("Ready to Play in VR.");
    }

    /*
    [MenuItem("Hackathon/Make Existing Objects Grabbable")]
    public static void MakeInteractive() { ... }

    [MenuItem("Hackathon/Clean Current Scene")]
    public static void CleanCurrentScene() { ... }

    [MenuItem("Hackathon/Decorate Scene")]
    public static void Decorate() { ... }
    
    [MenuItem("Hackathon/Setup Scene 3 (Cave)")]
    public static void SetupScene3() { ... }
    */
    private static void SpawnGrass()
    {
        string texPath = "Assets/ALP_Assets/GrassFlowersFREE/Textures/GrassFlowers";
        if (!Directory.Exists(texPath))
        {
            Debug.LogError($"Grass texture directory not found: {texPath}");
            return;
        }

        string[] texFiles = Directory.GetFiles(texPath, "*.tga");
        if (texFiles.Length == 0)
        {
            Debug.LogWarning("No grass textures found.");
            return;
        }

        GameObject parent = GameObject.Find("Decorations_Grass");
        if (parent) DestroyImmediate(parent);
        parent = new GameObject("Decorations_Grass");

        // Grass Material Template (using Particles/Standard Unlit or similar for double-sided alpha)
        // Or Standard Shader with Cutout
        Shader grassShader = Shader.Find("Standard");
        
        int grassCount = 3000;
        float range = 15f;

        for (int i = 0; i < grassCount; i++)
        {
            string randomTexPath = texFiles[Random.Range(0, texFiles.Length)];
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(randomTexPath);

            if (tex == null) continue;

            // Create Grass Object
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            grass.name = "Grass_" + i;
            grass.transform.SetParent(parent.transform);
            DestroyImmediate(grass.GetComponent<Collider>());

            // Position
            Vector3 pos = new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range));
            float scale = Random.Range(0.4f, 1.0f);
            
            grass.transform.position = pos + Vector3.up * (scale * 0.5f); // Half height up
            grass.transform.localScale = new Vector3(scale, scale, 1f);
            
            // Random Y rotation
            grass.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            // Material Setup
            Material mat = new Material(grassShader);
            mat.mainTexture = tex;
            
            // Configure Fade/Cutout
            mat.SetFloat("_Mode", 1); // 1 = Cutout
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 2450;
            mat.SetFloat("_Cutoff", 0.3f);
            mat.SetFloat("_Glossiness", 0f); // Not shiny
            
            // Tint slightly green for variety
            mat.color = Color.Lerp(Color.white, new Color(0.7f, 1f, 0.7f), Random.value);

            grass.GetComponent<Renderer>().material = mat;
            
            // Cross-Quad for volume (Second quad rotated 90 degrees)
            GameObject grass2 = Instantiate(grass, parent.transform);
            grass2.transform.position = grass.transform.position;
            grass2.transform.rotation = grass.transform.rotation * Quaternion.Euler(0, 90, 0);
            grass2.transform.localScale = grass.transform.localScale;
            grass2.GetComponent<Renderer>().material = mat;
        }
        Debug.Log($"Spawned {grassCount} grass clumps.");
    }
}
