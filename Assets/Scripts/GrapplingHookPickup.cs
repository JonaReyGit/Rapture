using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Drop this on an empty GameObject placed in the world (spawned next to the player). It
// builds its own hook-shaped visual and "Press E" prompt in code - like TitleScreen, there's
// nothing to wire up in the Inspector beyond position. Walking up and pressing E hands the
// player's Rigidbody to GrapplingHookGun.Equip() and removes the pickup.
[RequireComponent(typeof(SphereCollider))]
public class GrapplingHookPickup : MonoBehaviour
{
    public string playerTag = "Player";
    public float spinSpeed = 90f;
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;
    public Color hookColor = new Color(0.75f, 0.78f, 0.82f);

    private bool playerInRange;
    private Rigidbody playerBody;
    private Transform visual;
    private TextMeshPro prompt;

    void Reset()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f;
    }

    void Awake()
    {
        BuildVisual();
        BuildPrompt();
    }

    void Update()
    {
        visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobHeight, 0f);

        if (prompt != null)
        {
            prompt.gameObject.SetActive(playerInRange);
            if (playerInRange && Camera.main != null)
                prompt.transform.rotation = Quaternion.LookRotation(prompt.transform.position - Camera.main.transform.position);
        }

        if (TitleScreen.GameplayBlocked) return;
        if (!playerInRange) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            PickUp();
    }

    private void PickUp()
    {
        if (GrapplingHookGun.Instance != null && playerBody != null)
            GrapplingHookGun.Instance.Equip(playerBody);

        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = true;
        playerBody = other.attachedRigidbody;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = false;
        playerBody = null;
    }

    // ---------------------------------------------------------------- generated look

    private void BuildVisual()
    {
        GameObject go = new GameObject("Visual");
        go.transform.SetParent(transform, false);
        visual = go.transform;

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = hookColor;

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(visual, false);
        shaft.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
        Destroy(shaft.GetComponent<Collider>());
        shaft.GetComponent<Renderer>().sharedMaterial = material;

        // Three angled prongs around the top of the shaft so it reads as a hook, not a peg.
        for (int i = 0; i < 3; i++)
        {
            GameObject prong = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prong.name = "Prong";
            prong.transform.SetParent(visual, false);
            prong.transform.localScale = new Vector3(0.08f, 0.32f, 0.08f);

            float angle = i * 120f;
            Quaternion spin = Quaternion.Euler(0f, angle, 0f);
            prong.transform.localPosition = spin * new Vector3(0f, 0.55f, 0.12f);
            prong.transform.localRotation = spin * Quaternion.Euler(35f, 0f, 0f);
            Destroy(prong.GetComponent<Collider>());
            prong.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private void BuildPrompt()
    {
        GameObject go = new GameObject("Prompt");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        prompt = go.AddComponent<TextMeshPro>();
        prompt.text = "Press E to pick up";
        prompt.fontSize = 3f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = Color.white;
        go.SetActive(false);
    }
}
