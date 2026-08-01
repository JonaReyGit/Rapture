using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Title / pause screen. Drop this on ANY empty GameObject in the scene ("TitleScreen" is a
// good name) and it works - the whole menu (canvas, text, buttons) is built in code at
// startup, so there's nothing to wire up in the Inspector.
//
// While the menu is open the game is frozen (Time.timeScale = 0) and other scripts sit out
// via the TitleScreen.GameplayBlocked flag, so the player can't walk/look/grab behind the menu.
public class TitleScreen : MonoBehaviour
{
    public static TitleScreen Instance { get; private set; }

    // Other gameplay scripts check this and skip their Update while the menu is up.
    // Static so nothing has to hold a reference to (or even know about) the menu.
    //
    // It stays true for the rest of the frame the menu was dismissed on: the click that hit
    // PLAY (or the Esc that closed the menu) still reads as "pressed this frame", and script
    // execution order doesn't guarantee those scripts run before the UI handles the click.
    // Without the extra frame, hitting PLAY also grabs whatever was behind the button.
    public static bool GameplayBlocked => blocked || Time.frameCount == unblockedFrame;

    private static bool blocked;
    private static int unblockedFrame = -1;

    private static void SetBlocked(bool value)
    {
        if (blocked && !value) unblockedFrame = Time.frameCount;
        blocked = value;
    }

    [Header("Text")]
    public string gameTitle = "RAPTURE";
    public string tagline = "Everything here is sinking, slowly.";

    [Header("Behaviour")]
    [Tooltip("Show the title screen as soon as play mode starts.")]
    public bool showOnStart = true;
    [Tooltip("Esc opens/closes the menu as a pause screen during play.")]
    public bool escapeTogglesPause = true;
    [Tooltip("Lock and hide the mouse cursor once the player hits Play.")]
    public bool lockCursorDuringPlay = true;
    public float fadeDuration = 0.35f;

    [Header("Look")]
    [Tooltip("Optional splash image behind the title. Leave empty for a plain dark backdrop.")]
    public Sprite backgroundImage;
    public Color backdropColor = new Color(0.03f, 0.05f, 0.08f, 0.94f);
    public Color accentColor = new Color(0.45f, 0.86f, 0.95f, 1f);

    [Header("Narrator")]
    [Tooltip("Lines the narrator types out the first time the player hits Play. Leave empty to skip.")]
    public List<string> introLines = new List<string>
    {
        "You weren't the first one here.",
        "You're just the one who stayed."
    };

    [Header("Controls Shown On Screen")]
    public List<string> controls = new List<string>
    {
        "WASD  -  Move",
        "Shift  -  Sprint",
        "Space  -  Jump",
        "Mouse  -  Look",
        "Left Click  -  Grab  /  Scroll  -  Push, pull",
        "Right Click  -  Throw",
        "E  -  Interact",
        "G  -  Grapple  /  Scroll  -  Reel rope",
        "Esc  -  Pause"
    };

    // --- runtime UI, all built in Build() ---
    private Canvas canvas;
    private CanvasGroup group;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI taglineLabel;
    private TextMeshProUGUI playLabel;
    private GameObject controlsBlock;
    private Coroutine fadeRoutine;

    private bool isOpen;
    private bool hasStartedOnce;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Build();

        // Claim the block in Awake, not Start. Start order between components isn't
        // guaranteed, and FirstPersonCamera grabs the cursor in its own Start - it needs to
        // already see that the menu is up, or it would steal the cursor from the menu.
        if (showOnStart)
        {
            SetBlocked(true);
            Time.timeScale = 0f;
        }
    }

    void Start()
    {
        // In Start, not Awake: EventSystem.current is only set in the EventSystem's own
        // OnEnable, and doing this in Awake could race it and spawn a second one.
        EnsureEventSystem();

        if (showOnStart)
        {
            Open(asPauseMenu: false);
        }
        else
        {
            // Skipping the title screen means play has already begun, so Esc should behave
            // as a normal pause toggle right away instead of refusing to close.
            hasStartedOnce = true;
            CloseImmediate();
        }
    }

    void OnDestroy()
    {
        // Never leave the game frozen behind us if this object is torn down mid-pause.
        if (Instance == this)
        {
            Time.timeScale = 1f;
            SetBlocked(false);
            Instance = null;
        }
    }

    void Update()
    {
        if (!escapeTogglesPause) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // Esc on the very first title screen shouldn't drop you into a world you
        // haven't started yet, so it only closes the menu once you've played.
        if (isOpen)
        {
            if (hasStartedOnce) StartGame();
        }
        else
        {
            Open(asPauseMenu: true);
        }
    }

    // ---------------------------------------------------------------- public API

    public void Open(bool asPauseMenu)
    {
        isOpen = true;
        SetBlocked(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        titleLabel.text = asPauseMenu ? "PAUSED" : gameTitle;
        taglineLabel.text = asPauseMenu ? "" : tagline;
        playLabel.text = asPauseMenu ? "RESUME" : "PLAY";
        controlsBlock.SetActive(true);

        canvas.gameObject.SetActive(true);
        Fade(1f);
    }

    // Hooked to the Play / Resume button.
    public void StartGame()
    {
        isOpen = false;
        SetBlocked(false);
        Time.timeScale = 1f;

        if (lockCursorDuringPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Fade(0f);

        bool firstTime = !hasStartedOnce;
        hasStartedOnce = true;

        if (firstTime && introLines != null && introLines.Count > 0 && NarratorController.Instance != null)
            NarratorController.Instance.Play(introLines);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Application.Quit() does nothing in the editor, so stop play mode instead.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- fading

    private void CloseImmediate()
    {
        isOpen = false;
        SetBlocked(false);
        group.alpha = 0f;
        canvas.gameObject.SetActive(false);
    }

    private void Fade(float targetAlpha)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        group.blocksRaycasts = targetAlpha > 0.5f;
        group.interactable = targetAlpha > 0.5f;

        float start = group.alpha;
        float t = 0f;

        // unscaledDeltaTime, because Time.timeScale is 0 while the menu is up.
        while (t < fadeDuration && fadeDuration > 0f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, targetAlpha, t / fadeDuration);
            yield return null;
        }

        group.alpha = targetAlpha;

        if (targetAlpha <= 0f)
            canvas.gameObject.SetActive(false);

        fadeRoutine = null;
    }

    // ---------------------------------------------------------------- UI construction

    private void Build()
    {
        // --- canvas ---
        GameObject canvasGO = new GameObject("TitleScreen Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // sit above any other UI in the scene

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        group = canvasGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // --- backdrop ---
        Image backdrop = NewImage("Backdrop", canvasGO.transform);
        Stretch(backdrop.rectTransform);
        if (backgroundImage != null)
        {
            backdrop.sprite = backgroundImage;
            backdrop.preserveAspect = false;
            backdrop.color = Color.white;

            // Dim the photo so text stays readable on top of it.
            Image scrim = NewImage("Scrim", canvasGO.transform);
            Stretch(scrim.rectTransform);
            scrim.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, 0.72f);
        }
        else
        {
            backdrop.color = backdropColor;
        }

        // Soft dark corners - just a generated radial texture, no art assets needed.
        Image vignette = NewImage("Vignette", canvasGO.transform);
        Stretch(vignette.rectTransform);
        vignette.sprite = CreateVignetteSprite();
        vignette.color = new Color(0f, 0f, 0f, 0.55f);
        vignette.raycastTarget = false;

        // --- centred content column ---
        GameObject column = new GameObject("Content");
        column.transform.SetParent(canvasGO.transform, false);
        RectTransform columnRect = column.AddComponent<RectTransform>();
        columnRect.anchorMin = new Vector2(0.5f, 0.5f);
        columnRect.anchorMax = new Vector2(0.5f, 0.5f);
        columnRect.pivot = new Vector2(0.5f, 0.5f);
        columnRect.anchoredPosition = Vector2.zero;
        columnRect.sizeDelta = new Vector2(900f, 820f);

        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 14f;

        // Title
        titleLabel = NewText("Title", column.transform, gameTitle, 120f, FontStyles.Bold);
        titleLabel.characterSpacing = 24f;
        titleLabel.color = Color.white;
        SetHeight(titleLabel.rectTransform, 150f);

        // Accent rule under the title
        Image rule = NewImage("Rule", NewCenteredRow("RuleRow", column.transform));
        rule.color = accentColor;
        rule.raycastTarget = false;
        LayoutElement ruleLayout = rule.gameObject.AddComponent<LayoutElement>();
        ruleLayout.preferredHeight = 2f;
        ruleLayout.preferredWidth = 320f;
        ruleLayout.flexibleWidth = 0f;

        // Tagline
        taglineLabel = NewText("Tagline", column.transform, tagline, 26f, FontStyles.Italic);
        taglineLabel.color = new Color(0.78f, 0.85f, 0.9f, 1f);
        SetHeight(taglineLabel.rectTransform, 40f);

        AddSpacer(column.transform, 26f);

        // Buttons - each sits in its own centred row so the vertical layout above doesn't
        // stretch it across the full column width.
        Button playButton = NewButton("PlayButton", NewCenteredRow("PlayRow", column.transform), "PLAY", out playLabel);
        playButton.onClick.AddListener(StartGame);

        TextMeshProUGUI quitLabel;
        Button quitButton = NewButton("QuitButton", NewCenteredRow("QuitRow", column.transform), "QUIT", out quitLabel);
        quitButton.onClick.AddListener(QuitGame);

        AddSpacer(column.transform, 22f);

        // Controls list
        controlsBlock = new GameObject("Controls");
        controlsBlock.transform.SetParent(column.transform, false);
        controlsBlock.AddComponent<RectTransform>();
        VerticalLayoutGroup controlsLayout = controlsBlock.AddComponent<VerticalLayoutGroup>();
        controlsLayout.childAlignment = TextAnchor.UpperCenter;
        controlsLayout.childControlWidth = true;
        controlsLayout.childControlHeight = true;
        controlsLayout.childForceExpandWidth = true;
        controlsLayout.childForceExpandHeight = false;
        controlsLayout.spacing = 4f;

        TextMeshProUGUI controlsHeader = NewText("ControlsHeader", controlsBlock.transform, "CONTROLS", 18f, FontStyles.Bold);
        controlsHeader.characterSpacing = 10f;
        controlsHeader.color = accentColor;
        SetHeight(controlsHeader.rectTransform, 28f);

        foreach (string line in controls)
        {
            TextMeshProUGUI row = NewText("Control", controlsBlock.transform, line, 18f, FontStyles.Normal);
            row.color = new Color(0.7f, 0.76f, 0.82f, 1f);
            SetHeight(row.rectTransform, 24f);
        }

        // Corner credit
        TextMeshProUGUI credit = NewText("Credit", canvasGO.transform, "a school project", 16f, FontStyles.Normal);
        credit.color = new Color(0.5f, 0.56f, 0.62f, 1f);
        credit.alignment = TextAlignmentOptions.BottomRight;
        RectTransform creditRect = credit.rectTransform;
        creditRect.anchorMin = new Vector2(1f, 0f);
        creditRect.anchorMax = new Vector2(1f, 0f);
        creditRect.pivot = new Vector2(1f, 0f);
        creditRect.anchoredPosition = new Vector2(-36f, 28f);
        creditRect.sizeDelta = new Vector2(300f, 24f);

        canvasGO.SetActive(false);
    }

    // ---------------------------------------------------------------- small UI helpers

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetHeight(RectTransform rect, float height)
    {
        LayoutElement element = rect.gameObject.GetComponent<LayoutElement>();
        if (element == null) element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
    }

    // A full-width row that centres whatever gets parented into it, at that child's own
    // preferred size. Lets fixed-size things (buttons, the accent rule) live inside a
    // vertical layout that otherwise force-expands its children to full width.
    private static Transform NewCenteredRow(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return go.transform;
    }

    private static Image NewImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<Image>();
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string content, float size, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private Button NewButton(string name, Transform parent, string label, out TextMeshProUGUI labelText)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image background = go.AddComponent<Image>();
        background.color = Color.white; // tinted by the ColorBlock below

        Button button = go.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.08f);
        colors.highlightedColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.30f);
        colors.pressedColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.14f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.04f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredWidth = 300f;
        element.preferredHeight = 58f;
        element.flexibleWidth = 0f;

        labelText = NewText("Label", go.transform, label, 26f, FontStyles.Bold);
        labelText.characterSpacing = 8f;
        labelText.color = Color.white;
        Stretch(labelText.rectTransform);

        return button;
    }

    private static void AddSpacer(Transform parent, float height)
    {
        GameObject go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
    }

    // A 128x128 radial falloff, used as the vignette. Generated so the project needs no
    // extra texture asset for this.
    private static Sprite CreateVignetteSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDistance = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDistance;
                // Nothing in the middle, ramping up towards the corners.
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, distance));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private static void EnsureEventSystem()
    {
        // Buttons need an EventSystem to receive clicks. The scene normally has one; this is
        // just so the menu still works if it's missing.
        if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }
}
