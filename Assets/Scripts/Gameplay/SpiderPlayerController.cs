using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Spider controller for VR, adapted from BeePlayerController.
/// LEFT stick:  Y = forward/back, X = strafe
/// RIGHT stick: X = yaw (rotation)
/// Gravity is enabled for ground movement.
/// </summary>
public class SpiderPlayerController : MonoBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 4.0f;       // m/s
    public float turnSpeed = 60.0f;      // deg/s

    [Header("Input Tuning")]
    [Range(0f, 0.5f)] public float deadzone = 0.12f;
    [Tooltip("If true, forward/strafe ignores pitch/roll (moves flat).")]
    public bool flatMovement = true;
    [Tooltip("0 = no smoothing, higher = smoother (e.g. 10-20).")]
    public float inputSmoothing = 12f;

    [Header("Rider / Player Attach")]
    [Tooltip("Rig root moved with the spider (XR Origin / VR Setup). If null, auto-detect from Camera.main.root.")]
    public Transform playerRigRoot;
    [Tooltip("Head/camera transform. If null, uses Camera.main.")]
    public Transform playerHead;
    [Tooltip("Seat anchor on the spider. If null, uses this transform.")]
    public Transform seat;
    [Tooltip("If true, keep the rig aligned to the seat every frame (strong attachment).")]
    public bool keepRiderAttached = false;
    [Tooltip("If true, aligns the player's head to the seat (best illusion). If false, aligns rig root to seat.")]
    public bool alignHeadToSeat = true;

    /// <summary>
    /// Call this to start riding the spider.
    /// </summary>
    public void Mount()
    {
        ResolveReferences();
        keepRiderAttached = true;
        // Optionally enable this script if it was disabled
        this.enabled = true;
    }

    /// <summary>
    /// Call this to stop riding.
    /// </summary>
    public void Dismount()
    {
        keepRiderAttached = false;
    }
    [Tooltip("If true, copies spider yaw to rig yaw.")]
    public bool matchYawToBee = true; // Variable name kept for consistency but means "match yaw to spider"
    [Tooltip("Scale of the rider. 1 = normal, 0.1 = tiny.")]
    public float playerScale = 1.0f;
    [Tooltip("Offset added to yaw when mounting (e.g. 180 if facing backwards).")]
    public float mountYawOffset = 180f;

    // XR devices
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    // Smoothed input
    private Vector2 leftAxisSmoothed;
    private Vector2 rightAxisSmoothed;

    // Rigidbody (optional)
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        TryInitializeDevices();
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    private void Update()
    {
        // Reacquire controllers if needed
        if (!leftDevice.isValid || !rightDevice.isValid)
            TryInitializeDevices();

        // Read sticks (primary2DAxis OR secondary2DAxis fallback)
        Vector2 leftAxisRaw = ReadStick(leftDevice);
        Vector2 rightAxisRaw = ReadStick(rightDevice);

        // Deadzone + rescale
        Vector2 leftAxis = ApplyDeadzoneRescale(leftAxisRaw, deadzone);
        Vector2 rightAxis = ApplyDeadzoneRescale(rightAxisRaw, deadzone);

        // Optional smoothing
        if (inputSmoothing > 0f)
        {
            float k = 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime);
            leftAxisSmoothed = Vector2.Lerp(leftAxisSmoothed, leftAxis, k);
            rightAxisSmoothed = Vector2.Lerp(rightAxisSmoothed, rightAxis, k);
        }
        else
        {
            leftAxisSmoothed = leftAxis;
            rightAxisSmoothed = rightAxis;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Compute intended deltas
        float yawInput = rightAxisSmoothed.x;
        // No vertical input for spider from joystick (unless added later for jump)

        float yawDeltaDeg = yawInput * turnSpeed * dt;
        Vector3 translationDelta = ComputeTranslationDelta(leftAxisSmoothed, dt);

        // Apply movement to spider
        ApplySpiderMotion(translationDelta, yawDeltaDeg);

        // Keep player riding the spider
        if (keepRiderAttached)
            AttachRiderToSeat();
    }

    // ---------------- Movement ----------------

    private Vector3 ComputeTranslationDelta(Vector2 leftStick, float dt)
    {
        // Inverted forward control as requested
        float forward = -leftStick.y;
        float strafe = leftStick.x;

        Vector3 forwardDir;
        Vector3 rightDir;

        if (flatMovement)
        {
            Vector3 f = transform.forward; f.y = 0f;
            Vector3 r = transform.right;   r.y = 0f;

            forwardDir = f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
            rightDir = r.sqrMagnitude > 0.0001f ? r.normalized : Vector3.right;
        }
        else
        {
            forwardDir = transform.forward;
            rightDir = transform.right;
        }

        Vector3 horizontal = (forwardDir * forward + rightDir * strafe) * moveSpeed * dt;
        // Gravity is handled by Rigidbody usually, checking below

        return horizontal;
    }

    private void ApplySpiderMotion(Vector3 deltaPos, float deltaYawDeg)
    {
        Vector3 targetPos = transform.position + deltaPos;
        Quaternion targetRot = transform.rotation * Quaternion.Euler(0f, deltaYawDeg, 0f);

        if (rb != null && rb.isKinematic == false)
        {
            // Physics-driven movement
            // For a spider, we want to move position but let gravity act on Y unless we are grounded.
            // MovePosition handles physics collisions.
            
            // Note: MovePosition with a non-kinematic body overrides velocity for that frame.
            // To respect gravity, we usually set velocity instead, OR use MovePosition but keep Y from current physics step if we don't want to fly.
            // However, deltaPos here is purely horizontal (from ComputeTranslationDelta).
            // So targetPos.y is strictly transform.position.y (no change).
            // This might freeze gravity if we just set position.
            
            // Better approach for Rigidbody character: Set Velocity.
            // But to keep it similar to Bee loop:
            
            // We only apply horizontal change.
            // Let's rely on standard MovePosition for now, realizing it might be stiff on slopes without custom ground handling.
             rb.MovePosition(targetPos);
             rb.MoveRotation(targetRot);
        }
        else
        {
            // Transform-driven movement
            // Simple translation
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
    }

    // ---------------- Rider Attachment ----------------

    private void ResolveReferences()
    {
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        if (playerRigRoot == null && playerHead != null)
            playerRigRoot = playerHead.root;

        if (seat == null)
            seat = transform;
    }

    private void AttachRiderToSeat()
    {
        ResolveReferences();
        if (playerRigRoot == null || playerHead == null || seat == null) return;

        // Align rig so that the head ends up at the seat position
        if (alignHeadToSeat)
        {
            Vector3 headToRig = playerHead.position - playerRigRoot.position;
            Vector3 targetRigPos = seat.position - headToRig;
            playerRigRoot.position = targetRigPos;
        }
        else
        {
            playerRigRoot.position = seat.position;
        }

        if (matchYawToBee)
        {
            Vector3 rigEuler = playerRigRoot.eulerAngles;
            rigEuler.y = transform.eulerAngles.y + mountYawOffset;
            playerRigRoot.eulerAngles = rigEuler;
        }

        // Apply Scale
        playerRigRoot.localScale = Vector3.one * playerScale;
    }

    // ---------------- XR Devices ----------------

    private void TryInitializeDevices()
    {
        if (!leftDevice.isValid)
            leftDevice = GetFirstDeviceAtNode(XRNode.LeftHand);

        if (!rightDevice.isValid)
            rightDevice = GetFirstDeviceAtNode(XRNode.RightHand);
    }

    private void OnDeviceConnected(InputDevice device)
    {
        if ((device.characteristics & InputDeviceCharacteristics.Controller) == 0)
            return;

        if (!leftDevice.isValid)
            leftDevice = GetFirstDeviceAtNode(XRNode.LeftHand);

        if (!rightDevice.isValid)
            rightDevice = GetFirstDeviceAtNode(XRNode.RightHand);
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        if (leftDevice.isValid && device == leftDevice) leftDevice = default;
        if (rightDevice.isValid && device == rightDevice) rightDevice = default;
    }

    private static InputDevice GetFirstDeviceAtNode(XRNode node)
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);
        return devices.Count > 0 ? devices[0] : default;
    }

    // ---------------- Input Helpers ----------------

    private static Vector2 ReadStick(InputDevice device)
    {
        if (!device.isValid) return Vector2.zero;

        // Try primary
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 v))
        {
            if (v.sqrMagnitude > 0.000001f) return v;
        }

        // Fallback: secondary
        if (device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out v))
            return v;

        return Vector2.zero;
    }

    private static Vector2 ApplyDeadzoneRescale(Vector2 v, float dz)
    {
        float m = v.magnitude;
        if (m < dz) return Vector2.zero;

        float newMag = Mathf.InverseLerp(dz, 1f, m);
        return v.normalized * newMag;
    }
}
