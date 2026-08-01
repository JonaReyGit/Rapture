using UnityEngine;
using UnityEngine.InputSystem;

// Lives on the Main Camera next to ObjectGrabber, but starts disabled - there's no hook to
// fire until GrapplingHookPickup.Equip() hands it the player's Rigidbody. G fires a rope at
// whatever the camera is looking at; a SpringJoint on the player does the actual swinging,
// so PlayerMovement has to get out of the way (see the GrapplingHookGun.Instance check in
// its FixedUpdate) or it would stomp the swing velocity every physics step.
[RequireComponent(typeof(LineRenderer))]
public class GrapplingHookGun : MonoBehaviour
{
    public static GrapplingHookGun Instance { get; private set; }

    [Header("Reach")]
    public float maxRange = 40f;
    public LayerMask hookableMask = ~0;

    [Header("Swing")]
    public float spring = 4.5f;
    public float damper = 7f;
    public float massScale = 4.5f;
    [Tooltip("Scroll while attached to reel the rope in/out.")]
    public float reelSpeed = 8f;
    public float minRopeLength = 2f;

    [Header("Rope Visual")]
    public Color ropeColor = new Color(0.85f, 0.7f, 0.35f);
    public float ropeWidth = 0.05f;
    [Tooltip("Where the rope appears to leave from, in the camera's local space (right, up, forward). Keeps it out of dead-center so it doesn't read as shooting out of your eyes.")]
    public Vector3 ropeOrigin = new Vector3(0.3f, -0.3f, 0.6f);

    public bool HasHook { get; private set; }
    public bool IsSwinging => joint != null;

    private Camera cam;
    private Rigidbody playerBody;
    private SpringJoint joint;
    private LineRenderer rope;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        rope = GetComponent<LineRenderer>();
        rope.positionCount = 2;
        rope.widthMultiplier = ropeWidth;
        rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rope.receiveShadows = false;
        rope.useWorldSpace = true;
        // Sprites/Default is a Built-in RP shader with no URP pass, so URP draws it as
        // nothing - this project uses URP, so the rope needs a URP-compatible shader instead.
        Shader ropeShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (ropeShader == null) ropeShader = Shader.Find("Sprites/Default");
        rope.material = new Material(ropeShader);
        // Material.color, not just the LineRenderer's vertex colors below - URP/Unlit reads
        // its _BaseColor property and ignores mesh vertex colors, unlike Sprites/Default.
        rope.material.color = ropeColor;
        rope.startColor = ropeColor;
        rope.endColor = ropeColor;
        rope.enabled = false;

        // No hook picked up yet - GrapplingHookPickup turns this on.
        enabled = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Equip(Rigidbody player)
    {
        playerBody = player;
        HasHook = true;
        enabled = true;
    }

    void Update()
    {
        if (TitleScreen.GameplayBlocked)
        {
            if (joint != null) Detach();
            return;
        }
        if (Keyboard.current == null) return;

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            if (joint == null) TryFire();
            else Detach();
        }

        if (joint != null && Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                joint.maxDistance = Mathf.Max(minRopeLength, joint.maxDistance - scroll * reelSpeed * 0.01f);
        }
    }

    void LateUpdate()
    {
        if (joint == null)
        {
            rope.enabled = false;
            return;
        }

        rope.enabled = true;
        // Never start the line exactly at the camera's own position - LineRenderer bills each
        // vertex to face the rendering camera by normalizing (vertex - cameraPosition), and at
        // distance zero that's a NaN. A NaN vertex corrupts the renderer's bounds, so Unity
        // culls the entire line rather than drawing a broken one - it just vanishes outright.
        // ropeOrigin also nudges it off dead-center so it doesn't read as firing from your eyes.
        rope.SetPosition(0, transform.TransformPoint(ropeOrigin));
        rope.SetPosition(1, joint.connectedAnchor);
    }

    private void TryFire()
    {
        if (!HasHook || cam == null || playerBody == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRange, hookableMask)) return;

        // Only anchor to fixed geometry - grappling onto a loose grabbable prop would just
        // yank it toward the player instead of swinging the player toward it.
        if (hit.rigidbody != null && !hit.rigidbody.isKinematic) return;

        joint = playerBody.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = hit.point;
        joint.maxDistance = Vector3.Distance(playerBody.position, hit.point);
        joint.minDistance = 0f;
        joint.spring = spring;
        joint.damper = damper;
        joint.massScale = massScale;
    }

    public void Detach()
    {
        if (joint != null) Destroy(joint);
        joint = null;
    }
}
