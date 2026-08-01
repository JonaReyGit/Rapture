using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectGrabber : MonoBehaviour
{
    [Header("Reach")]
    public float maxGrabDistance = 5f;
    public LayerMask grabbableMask = ~0;

    [Header("Hold Behavior")]
    public float followStrength = 12f;
    public float minHoldDistance = 1f;
    public float maxHoldDistance = 6f;
    public float scrollAdjustSpeed = 2f;

    [Header("Throw")]
    public float throwForce = 8f;

    private Camera cam;
    private Rigidbody grabbedBody;
    private float holdDistance;
    private bool wasKinematic;
    private bool hadGravity;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        // Don't grab/throw through the menu - clicking a button would otherwise also fire
        // a raycast into the world behind it.
        if (TitleScreen.GameplayBlocked)
        {
            if (grabbedBody != null) Release(throwForce: false);
            return;
        }

        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryGrab();

        if (Mouse.current.leftButton.wasReleasedThisFrame && grabbedBody != null)
            Release(throwForce: false);

        if (grabbedBody != null)
        {
            // scroll to adjust distance
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                holdDistance += scroll * scrollAdjustSpeed * 0.01f;
                holdDistance = Mathf.Clamp(holdDistance, minHoldDistance, maxHoldDistance);
            }
        }

        // right click = throw
        if (Mouse.current.rightButton.wasPressedThisFrame && grabbedBody != null)
            Release(throwForce: true);
    }

    void FixedUpdate()
    {
        if (grabbedBody == null || cam == null) return;

        Vector3 targetPosition = cam.transform.position + cam.transform.forward * holdDistance;
        Vector3 velocity = (targetPosition - grabbedBody.position) * followStrength;

        grabbedBody.linearVelocity = velocity;
        grabbedBody.angularVelocity = Vector3.zero;
    }

    private void TryGrab()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabbableMask))
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic) return;

            grabbedBody = rb;
            holdDistance = Mathf.Clamp(hit.distance, minHoldDistance, maxHoldDistance);

            wasKinematic = rb.isKinematic;
            hadGravity = rb.useGravity;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    private void Release(bool throwForce)
    {
        if (grabbedBody == null) return;

        grabbedBody.useGravity = hadGravity;
        grabbedBody.isKinematic = wasKinematic;

        if (throwForce)
            grabbedBody.AddForce(cam.transform.forward * this.throwForce, ForceMode.VelocityChange);

        grabbedBody = null;
    }
}