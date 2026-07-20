using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSpeed = 10f;
    public float jumpForce = 7f;
    public float sprintMultiplier = 1.75f; // hold Left Shift to run

    [Header("Ground Check")]
    public Transform groundCheck;      // empty child object at the character's feet
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;      // set this to whatever layer your Terrain is on

    [Header("Camera")]
    public Transform cameraTransform;  // drag Main Camera here, or leave empty to auto-find

    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded;
    private bool isSprinting;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Rotation is fully handled by this script, so freeze all physics rotation
        // (otherwise friction/contact torque can make the character slowly spin when idle)
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // --- Ground check ---
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        else
            isGrounded = true; // fallback if you haven't set up a ground check point yet

        // --- Input (new Input System) ---
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }
        Vector3 inputDir = new Vector3(input.x, 0f, input.y).normalized;

        isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        if (inputDir.magnitude >= 0.1f)
        {
            // Make movement relative to the camera's facing direction
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            if (cameraTransform != null)
                targetAngle += cameraTransform.eulerAngles.y;

            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        // --- Jump ---
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            if (animator != null)
                animator.SetTrigger("Jump");
        }

        // --- Animator params ---
        if (animator != null)
        {
            // 0 = idle, 1 = walk, 2 = run (matches the Speed thresholds in the Animator Controller)
            float animSpeed = inputDir.magnitude * (isSprinting ? 2f : 1f);
            animator.SetFloat("Speed", animSpeed);
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector3 velocity = moveDirection * speed;
        velocity.y = rb.linearVelocity.y; // preserve gravity/jump vertical velocity
        rb.linearVelocity = velocity;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}