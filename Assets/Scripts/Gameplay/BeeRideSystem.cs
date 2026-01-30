using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils; // For XROrigin

public class BeeRideSystem : MonoBehaviour
{
    [Header("Configuration")]
    public Transform beeMountPoint; // The seat (POV abeille)
    public float playerScale = 0.1f; // Minimoys size
    public float mountDelay = 0.5f;

    private Transform xrOriginTransform;

    private void Start()
    {
        // 1. Auto-Find Mount Point
        if (beeMountPoint == null)
        {
            GameObject pov = GameObject.Find("POV abeille");
            if (pov != null) beeMountPoint = pov.transform;
            else
            {
                // Fallback: Create one
                GameObject m = new GameObject("MountPoint");
                m.transform.SetParent(transform);
                m.transform.localPosition = new Vector3(0, 0.5f, 0);
                beeMountPoint = m.transform;
            }
        }
        
        // Ensure Mount Point is child of Bee
        if (beeMountPoint.parent != transform)
        {
            beeMountPoint.SetParent(transform, true);
        }

        StartCoroutine(MountRoutine());
    }

    private IEnumerator MountRoutine()
    {
        yield return new WaitForSeconds(mountDelay);

        // 2. Find Player
        XROrigin origin = FindFirstObjectByType<XROrigin>();
        if (origin != null)
        {
            xrOriginTransform = origin.transform;
            MountPlayer();
        }
        else
        {
            Debug.LogError("[BeeRideSystem] XR Origin not found!");
        }
    }

    private void MountPlayer()
    {
        Debug.Log("[BeeRideSystem] Mounting Player via TELEPORT (Continuous)...");

        // A. GHOST MODE
        var cc = xrOriginTransform.GetComponentInChildren<CharacterController>();
        if (cc != null) Destroy(cc);

        var rb = xrOriginTransform.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
        
        foreach(var mono in xrOriginTransform.GetComponentsInChildren<MonoBehaviour>())
        {
             string name = mono.GetType().Name;
             if (name.Contains("Locomotion") || name.Contains("MoveProvider") || 
                 name.Contains("Teleport") || name.Contains("Turn"))
             {
                 if (mono != this) Destroy(mono);
             }
        }

        // B. UNPARENT (To be free from Hierarchy issues)
        xrOriginTransform.SetParent(null);
        
        // C. SCALE
        xrOriginTransform.localScale = Vector3.one * playerScale;

        // D. INITIAL ORIENTATION
        Camera cam = Camera.main;
        if (cam != null)
        {
            xrOriginTransform.rotation = beeMountPoint.rotation;
            float headY = cam.transform.localEulerAngles.y;
            Vector3 currentRot = xrOriginTransform.localEulerAngles;
            xrOriginTransform.localEulerAngles = new Vector3(currentRot.x, -headY, currentRot.z);
            cam.nearClipPlane = 0.01f;
        }

        Debug.Log("[BeeRideSystem] Player teleport mode activated.");
    }
    
    // CONTINUOUS TELEPORT
    private void LateUpdate()
    {
        if (xrOriginTransform != null && beeMountPoint != null)
        {
            // Force Position
            xrOriginTransform.position = beeMountPoint.position;
            
            // Force Scale (just in case)
            if (xrOriginTransform.localScale.x != playerScale) 
                xrOriginTransform.localScale = Vector3.one * playerScale;
                
            // Optional: Match Rotation? The user complained about orientation, so let's lock it to the bee's turn?
            // "Tp sans arret a sa position" implies position. Rotation might be disorienting if locked perfectly.
            // Let's lock it for now as requested "fixation".
            // Actually, keep World Rotation separation to allow looking around, but maybe drag the "body" along?
            // The simplest "fixation" matches both.
            // But if the bee turns, the player should turn.
            
            // Apply Bee Rotation offset? 
            // Simple: Match Rotation.
            // xrOriginTransform.rotation = beeMountPoint.rotation; 
            // WAIT: If we lock rotation every frame, the user CANNOT turn their head (the camera is child of origin).
            // NO. The Camera turns inside the Origin. We can rotate the Origin.
            xrOriginTransform.rotation = beeMountPoint.rotation;
        }
    }
}
