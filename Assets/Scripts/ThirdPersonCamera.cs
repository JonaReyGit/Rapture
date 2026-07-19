using UnityEngine;

// Fixed-offset follow camera. Drag this onto your Main Camera.
// The offset is relative to the target's facing direction, so the camera
// stays behind/above the player and swings around as they turn.
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;           // drag the player here
    public Vector3 offset = new Vector3(0f, 3f, -6f); // offset in the target's local space (relative to its facing)

    [Header("Smoothing")]
    public float followSmoothTime = 0.15f;
    public float rotationSmoothSpeed = 8f;
    public bool lookAtTarget = true;
    public Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f); // aim roughly at chest/head height

    private Vector3 velocity;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Rotate the offset with the player's facing direction so the camera swings around as they turn
        Vector3 desiredPosition = target.position + target.rotation * offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, followSmoothTime);

        if (lookAtTarget)
        {
            Quaternion desiredRotation = Quaternion.LookRotation((target.position + lookAtOffset) - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }
}
