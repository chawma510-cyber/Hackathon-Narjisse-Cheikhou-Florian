using UnityEngine;
using System.Collections.Generic;

public class SimpleBeeFlight : MonoBehaviour
{
    [Header("Path Settings")]
    public List<Transform> waypoints = new List<Transform>();
    public float speed = 2.0f;
    public float rotationSpeed = 2.0f;
    public bool loop = false;
    public bool autoStart = true;

    private int currentTargetIndex = 0;
    private bool isFlying = false;

    private void Start()
    {
        // SAFEGUARD: If user forgot to assign points, generate a simple forward path
        if (waypoints == null || waypoints.Count < 2)
        {
            Debug.LogWarning("[SimpleBeeFlight] No waypoints assigned! Generating default forward path and FORCING FLIGHT.");
            
            waypoints = new List<Transform>();
            
            // Point A (Current Pos)
            GameObject p1 = new GameObject("Path_Start");
            p1.transform.position = transform.position;
            waypoints.Add(p1.transform);
            
            // Point B (10m Forward)
            GameObject p2 = new GameObject("Path_End");
            p2.transform.position = transform.position + transform.forward * 10f;
            waypoints.Add(p2.transform);

            // FORCE DEFAULTS
            if (speed <= 0.1f) speed = 2.0f;
            if (rotationSpeed <= 0.1f) rotationSpeed = 2.0f;
            
            // FORCE START regardless of boolean
            StartFlight();
            return;
        }

        if (autoStart) StartFlight();
    }

    public void StartFlight()
    {
        if (waypoints.Count < 2) return;

        // Teleport to first point
        transform.position = waypoints[0].position; 
        
        currentTargetIndex = 1; 
        isFlying = true;
        
        // Ensure speeds are valid just in case
        if (speed <= 0.01f) speed = 2.0f;
        if (rotationSpeed <= 0.01f) rotationSpeed = 2.0f;

        Debug.Log($"[SimpleBeeFlight] Flight Started. Target: {waypoints[1].name} at {waypoints[1].position}. Speed: {speed}");
    }

    private void Update()
    {
        // FORCE MOVEMENT LOGIC
        
        // 1. If we have points, do the Waypoint logic
        if (isFlying && waypoints.Count >= 2)
        {
            // CHECK COMPLETION
            if (currentTargetIndex >= waypoints.Count)
            {
                if (loop)
                {
                    Debug.Log("[SimpleBeeFlight] Looping path.");
                    currentTargetIndex = 0;
                }
                else
                {
                    Debug.Log("[SimpleBeeFlight] Reached Final Destination.");
                    isFlying = false;
                    return;
                }
            }

            Transform target = waypoints[currentTargetIndex];
            
            // Move
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.position, step);
            
            // Rotate
            Vector3 direction = (target.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
            }

            // Check Arrival
            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                Debug.Log($"[SimpleBeeFlight] Reached point {currentTargetIndex}");
                currentTargetIndex++;
            }
        }
        else
        {
            // FALLBACK: Just move forward if the logic above fails but script is running
            // This ensures "Something happens"
             // transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        // DEBUG: Prove it moves
        // Debug.Log($"[SimpleBeeFlight] Pos: {transform.position}");
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 0.3f);
                if (i < waypoints.Count - 1 && waypoints[i+1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
                }
            }
        }
        
        // Loop line
        if (loop && waypoints.Count > 1 && waypoints[0] != null && waypoints[waypoints.Count-1] != null)
        {
            Gizmos.color = Color.green * 0.5f;
            Gizmos.DrawLine(waypoints[waypoints.Count-1].position, waypoints[0].position);
        }
    }
}
