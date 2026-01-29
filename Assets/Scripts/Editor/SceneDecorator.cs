using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SceneDecorator : EditorWindow
{
    [MenuItem("Hackathon/Setup VR in Current Scene")]
    public static void SetupVR()
    {
        Debug.Log("Setting up VR...");
        
        // 1. Remove default camera
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.transform.root.name != "XR Origin (XR Rig)")
        {
            DestroyImmediate(mainCam.gameObject);
        }
        
        // 2. Add XRSetup if missing
        if (Object.FindFirstObjectByType<HackathonVR.XRSetup>() == null)
        {
            GameObject setupGO = new GameObject("VR Setup");
            setupGO.AddComponent<HackathonVR.XRSetup>();
            Debug.Log("Added XRSetup.");
        }
        else
        {
            Debug.Log("XRSetup already present.");
        }

        // 3. Add Music Manager if missing
        if (Object.FindFirstObjectByType<HackathonVR.MusicManager>() == null)
        {
            GameObject musicGO = new GameObject("Music Manager");
            musicGO.AddComponent<HackathonVR.MusicManager>();
            Debug.Log("Added Music Manager.");
        }
        
        Debug.Log("VR Setup Complete! Press Play to initialize rig.");
    }

    [MenuItem("Hackathon/Decorate Scene")]
    public static void Decorate()
    {
        Debug.Log("Starting scene decoration...");

        // Ensure VR setup is done first just in case
        SetupVR();

        // 1. Set Skybox
        SetSkybox();

        // 2. Spawn Balloons
        SpawnBalloons();

        // 3. Spawn Grass
        SpawnGrass();
        
        Debug.Log("Scene Decoration Complete!");
    }

    private static void SetSkybox()
    {
        string skyboxPath = "Assets/Saritasa/Skybox/Skybox.mat";
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
            // Enable fog for better depth with skybox
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.5f, 0.6f, 0.7f);
            RenderSettings.fogDensity = 0.02f;
            Debug.Log("Skybox applied.");
        }
        else
        {
            Debug.LogError($"Skybox material not found at {skyboxPath}");
        }
    }

    private static void SpawnBalloons()
    {
        string path = "Assets/Saritasa/Models/Sport_Balls";
        if (!Directory.Exists(path))
        {
            Debug.LogError($"Balloons directory not found: {path}");
            return;
        }

        string[] balloonFiles = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
        if (balloonFiles.Length == 0)
        {
            Debug.LogWarning("No balloon prefabs found.");
            return;
        }

        GameObject parent = GameObject.Find("Decorations_Balloons");
        if (parent) DestroyImmediate(parent);
        parent = new GameObject("Decorations_Balloons");

        for (int i = 0; i < 15; i++)
        {
            string randomPath = balloonFiles[Random.Range(0, balloonFiles.Length)];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(randomPath);
            
            if (prefab)
            {
                // Instantiate as prefab link
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(parent.transform);
                
                // Random position in air
                instance.transform.position = new Vector3(
                    Random.Range(-8f, 8f), 
                    Random.Range(2f, 8f), 
                    Random.Range(-8f, 8f)
                );
                
                // Ensure physics
                if (instance.GetComponent<Rigidbody>() == null)
                    instance.AddComponent<Rigidbody>();
                    
                // Ensure grabbable
                if (instance.GetComponent<HackathonVR.Interactions.VRGrabInteractable>() == null)
                    instance.AddComponent<HackathonVR.Interactions.VRGrabInteractable>();
            }
        }
        Debug.Log($"Spawned 15 balloons.");
    }

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
