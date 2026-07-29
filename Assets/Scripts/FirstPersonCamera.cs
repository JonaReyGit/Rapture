using UnityEngine;
using UnityEngine.InputSystem;

// First-person mouse-look camera. Drag this onto your Main Camera (replaces ThirdPersonCamera).
// Horizontal mouse movement turns the player body (target) so movement stays intuitive;
// vertical mouse movement pitches only the camera, so the player model doesn't tilt.
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;           // drag the player here, or leave empty to auto-find by tag
    public Vector3 eyeOffset = new Vector3(0f, 1.7f, 0f); // head position, in the target's local space

    [Header("Look")]
    public float mouseSensitivity = 0.15f; // degrees of turn per pixel of mouse movement
    [Range(-90f, 90f)]
    public float minPitch = -85f;
    [Range(-90f, 90f)]
    public float maxPitch = 85f;
    public bool lockCursor = true;

    [Header("Body Visibility")]
    // The camera sits inside the character's head, so its own body/face renders right in
    // front of the lens ("inside of my face"). Hiding it (but still casting a shadow) is
    // the standard fix until there's a dedicated arms/viewmodel to show instead.
    public bool hidePlayerModel = true;

    private float yaw;
    private float pitch;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        if (target != null)
            yaw = target.eulerAngles.y;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (hidePlayerModel && target != null)
        {
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    void Update()
    {
        // Let the player free the cursor without leaving play mode
        if (lockCursor && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool nowLocked = Cursor.lockState != CursorLockMode.Locked;
            Cursor.lockState = nowLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !nowLocked;
        }

        if (target == null || Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        // Mouse.delta is already the pixel movement that happened this frame (not a rate),
        // so it must NOT be multiplied by Time.deltaTime - doing that made turn speed
        // inversely proportional to frame rate, i.e. sensitivity visibly drifted as FPS changed.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Turning the player body directly (rather than smoothing) keeps the look
        // response tight, which matters a lot more in first person than in third.
        target.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + target.rotation * eyeOffset;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
