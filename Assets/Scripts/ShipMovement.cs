using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SpaceshipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 50f;
    public float strafeSpeed = 30f;
    public float boostMultiplier = 2f;

    [Header("Takeoff Settings")]
    public float takeoffHeight = 15f;
    public float takeoffDuration = 5f;

    [Header("Look Settings")]
    [Range(0.01f, 1f)]
    public float horizontalSensitivity = 0.1f;
    [Range(0.01f, 1f)]
    public float verticalSensitivity = 0.1f;
    public float rotationSmoothing = 25f;
    public float rollSpeed = 60f;
    public bool invertY = false;

    private Rigidbody rb;
    private bool isFlying = false;
    private bool isTakingOff = false;
    private float takeoffTimer = 0f;
    private float pitch = 0f;

    void Start()
    {
        // Setup Rigidbody on the Parent
        rb = GetComponent<Rigidbody>();

        // Settings to prevent jitter and falling through floors
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2f;

        LockCursor();
    }

    void Update()
    {
        if (rb == null || Keyboard.current == null) return;

        // Mouse Lock Management
        if (Keyboard.current.escapeKey.wasPressedThisFrame) UnlockCursor();
        if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor();

        if (!isFlying && !isTakingOff)
        {
            HandleTakeoffInput();
        }
    }

    void FixedUpdate()
    {
        if (isFlying)
        {
            HandlePhysicsMovement();
            HandleFlightRotation();
        }
    }

    private void HandleTakeoffInput()
    {
        if (Keyboard.current.spaceKey.isPressed)
        {
            takeoffTimer += Time.deltaTime;
            if (takeoffTimer >= takeoffDuration)
            {
                StartCoroutine(ExecuteTakeoff());
            }
        }
        else
        {
            takeoffTimer = 0f;
        }
    }

    IEnumerator ExecuteTakeoff()
    {
        isTakingOff = true;
        rb.useGravity = false;
        rb.isKinematic = true; // Use kinematic for the smooth vertical rise

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * takeoffHeight;
        float elapsed = 0;
        float liftTime = 3f;

        while (elapsed < liftTime)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / liftTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        rb.isKinematic = false;
        isFlying = true;
        isTakingOff = false;

        // Sync the pitch variable with the ship's current rotation
        pitch = transform.eulerAngles.x;
        if (pitch > 180) pitch -= 360;
    }

    private void HandlePhysicsMovement()
    {
        var kb = Keyboard.current;
        float currentSpeed = forwardSpeed;

        // W + Shift = Boost
        if (kb.leftShiftKey.isPressed && kb.wKey.isPressed) currentSpeed *= boostMultiplier;

        // Forward movement only (No Reverse)
        float moveForward = kb.wKey.isPressed ? currentSpeed : 0f;

        // Strafe A/D
        float moveStrafe = 0f;
        if (kb.aKey.isPressed) moveStrafe = -strafeSpeed;
        if (kb.dKey.isPressed) moveStrafe = strafeSpeed;

        Vector3 moveVector = (transform.forward * moveForward) + (transform.right * moveStrafe);

        // Apply velocity with smoothing
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, moveVector, Time.fixedDeltaTime * 2f);

        // Braking S
        if (kb.sKey.isPressed)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
        }
    }

    private void HandleFlightRotation()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        var mouse = Mouse.current;
        var kb = Keyboard.current;
        Vector2 mouseDelta = mouse.delta.ReadValue();

        // 1. Yaw (Left/Right) - Unlimited rotation
        float yawInput = mouseDelta.x * horizontalSensitivity;
        Quaternion yawRotation = Quaternion.Euler(0, yawInput, 0);

        // 2. Pitch (Up/Down) - Clamped to 180 total degrees
        float pitchInput = invertY ? mouseDelta.y : -mouseDelta.y;
        pitch += pitchInput * verticalSensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        // 3. Roll (Q/E)
        float rollInput = 0;
        if (kb.qKey.isPressed) rollInput = rollSpeed;
        if (kb.eKey.isPressed) rollInput = -rollSpeed;
        Quaternion rollRotation = Quaternion.Euler(0, 0, rollInput * Time.fixedDeltaTime);

        // 4. Combine and Apply
        // We apply Yaw to the existing rotation, then set the local pitch and roll
        Quaternion yawedRotation = rb.rotation * yawRotation;
        Vector3 currentEuler = yawedRotation.eulerAngles;

        Quaternion targetRotation = Quaternion.Euler(pitch, currentEuler.y, currentEuler.z) * rollRotation;

        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothing));
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}