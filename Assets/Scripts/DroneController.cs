using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple arcade-style drone controller.
/// W/S or Up/Down  : fly forward / backward
/// A/D or Left/Right: turn (yaw)
/// Q/E             : strafe left / right
/// Space           : ascend
/// Left Shift / C  : descend
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal speed (units/sec).")]
    public float speed = 2.0f;

    [Tooltip("Vertical speed (units/sec).")]
    public float climbSpeed = 1.5f;

    [Tooltip("Turn speed (degrees/sec).")]
    public float rotationSpeed = 120.0f;

    [Tooltip("How quickly the drone reaches target velocity. Higher = snappier.")]
    public float acceleration = 4.0f;

    [Header("Visuals")]
    [Tooltip("Child that holds the drone model. It tilts while moving.")]
    public Transform model;

    [Tooltip("Rotor transforms that spin around their local Y axis.")]
    public Transform[] rotors;

    public float rotorSpinSpeed = 1800.0f;

    [Tooltip("Max tilt angle (degrees) when moving.")]
    public float maxTilt = 20.0f;

    [Tooltip("Gentle up/down hover bob.")]
    public float bobAmount = 0.01f;
    public float bobSpeed = 3.0f;

    private Rigidbody rb;
    private Vector3 moveInput;   // x = strafe, y = vertical, z = forward
    private float turnInput;
    private Vector3 modelBasePos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (model != null) modelBasePos = model.localPosition;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        moveInput = Vector3.zero;
        turnInput = 0f;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.z += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.z -= 1f;
        if (kb.eKey.isPressed) moveInput.x += 1f;
        if (kb.qKey.isPressed) moveInput.x -= 1f;
        if (kb.spaceKey.isPressed) moveInput.y += 1f;
        if (kb.leftShiftKey.isPressed || kb.cKey.isPressed) moveInput.y -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) turnInput += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) turnInput -= 1f;

        AnimateVisuals();
    }

    void FixedUpdate()
    {
        // Desired velocity in world space, relative to the drone's heading.
        Vector3 horizontal = (transform.forward * moveInput.z + transform.right * moveInput.x);
        if (horizontal.sqrMagnitude > 1f) horizontal.Normalize();

        Vector3 targetVelocity = horizontal * speed + Vector3.up * moveInput.y * climbSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        float turn = turnInput * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
    }

    private void AnimateVisuals()
    {
        // Spin rotors (faster when climbing).
        if (rotors != null)
        {
            float spin = rotorSpinSpeed * (1f + 0.5f * Mathf.Max(0f, moveInput.y)) * Time.deltaTime;
            for (int i = 0; i < rotors.Length; i++)
            {
                if (rotors[i] == null) continue;
                // Alternate direction like a real quadcopter.
                rotors[i].Rotate(0f, (i % 2 == 0 ? spin : -spin), 0f, Space.Self);
            }
        }

        if (model == null) return;

        // Tilt toward movement direction: pitch forward when moving forward, roll when strafing/turning.
        float pitch = moveInput.z * maxTilt;
        float roll = -(moveInput.x + turnInput * 0.5f) * maxTilt;
        Quaternion targetTilt = Quaternion.Euler(pitch, 0f, roll);
        model.localRotation = Quaternion.Slerp(model.localRotation, targetTilt, 5f * Time.deltaTime);

        // Hover bob.
        model.localPosition = modelBasePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmount;
    }
}
