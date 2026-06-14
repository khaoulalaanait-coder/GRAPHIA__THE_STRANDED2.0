using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BoatEscape : MonoBehaviour
{
    [SerializeField] private GameObject assembledBoat;
    [SerializeField] private GameObject incompleteBoat;
    [SerializeField] private TMP_FontAsset uiFont;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private string missingText = "Find 4 boat pieces and secure the fuel.";
    [SerializeField] private string readyText = "Press E to escape.";
    [SerializeField] private string winText = "You escaped before the storm.";
    [SerializeField] private float winDisplayDuration = 5f;
    [SerializeField] private string replaySceneName = "TapToPlay";

    private bool playerNearby;
    private bool escaping;
    private bool replayReady;
    private static Sprite winGlowSprite;
    private static Sprite winVignetteSprite;

    private struct EscapePreview
    {
        public GameObject stage;
        public RenderTexture texture;
    }

    private void Start()
    {
        RefreshBoatState();
    }

    private void Update()
    {
        if (escaping)
        {
            HandleReplayInput();
            return;
        }

        if (!playerNearby)
            return;

        RefreshBoatState();

        if (CanEscape() && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            StartCoroutine(EscapeSequence());
    }

    public void BeginAutomaticEscape()
    {
        if (escaping)
            return;

        StartCoroutine(EscapeSequence());
    }

    private void RefreshBoatState()
    {
        bool assembled = CanEscape();

        if (assembledBoat != null)
            assembledBoat.SetActive(assembled);

        if (incompleteBoat != null)
            incompleteBoat.SetActive(!assembled);
    }

    private bool CanEscape()
    {
        BoatPieceManager manager = FindFirstObjectByType<BoatPieceManager>();
        bool allPieces = PlayerData.HasAllBoatPieces || (manager != null && manager.HasAllPieces);
        return allPieces && PlayerData.hasFuel;
    }

    private IEnumerator EscapeSequence()
    {
        escaping = true;
        replayReady = false;
        CanvasGroup group = BuildWinCanvas();
        EscapePreview preview = BuildEscapePreview(group.transform);

        yield return Fade(group, 0f, 1f, fadeDuration);
        replayReady = true;
        yield return new WaitForSecondsRealtime(winDisplayDuration);

        Debug.Log("WIN");
    }

    private CanvasGroup BuildWinCanvas()
    {
        GameObject canvasObject = new GameObject("EscapeWinCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasGroup group = canvasObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;
        group.interactable = true;

        GameObject overlayObject = new GameObject("Overlay");
        overlayObject.transform.SetParent(canvasObject.transform, false);
        Image overlay = overlayObject.AddComponent<Image>();
        overlay.color = new Color(0.01f, 0.025f, 0.025f, 0.72f);
        RectTransform overlayRect = overlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject vignetteObject = new GameObject("Vignette");
        vignetteObject.transform.SetParent(canvasObject.transform, false);
        Image vignette = vignetteObject.AddComponent<Image>();
        vignette.sprite = GetWinVignetteSprite();
        vignette.color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform vignetteRect = vignette.rectTransform;
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;

        GameObject glowObject = new GameObject("GoldGlow");
        glowObject.transform.SetParent(canvasObject.transform, false);
        Image glow = glowObject.AddComponent<Image>();
        glow.sprite = GetWinGlowSprite();
        glow.color = new Color(1f, 0.72f, 0.16f, 0.62f);
        RectTransform glowRect = glow.rectTransform;
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = new Vector2(0f, -40f);
        glowRect.sizeDelta = new Vector2(980f, 760f);

        GameObject textObject = new GameObject("WinText");
        textObject.transform.SetParent(canvasObject.transform, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "YOU ESCAPED!";
        text.fontSize = 86f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.78f, 0.28f, 1f);
        if (uiFont != null)
            text.font = uiFont;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -110f);
        textRect.sizeDelta = new Vector2(1100f, 120f);

        GameObject buttonObject = new GameObject("PlayAgainButton");
        buttonObject.transform.SetParent(canvasObject.transform, false);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.08f, 0.03f, 0.78f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(LoadReplayScene);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 78f);
        buttonRect.sizeDelta = new Vector2(360f, 82f);

        GameObject buttonBorderObject = new GameObject("Border");
        buttonBorderObject.transform.SetParent(buttonObject.transform, false);
        Image buttonBorder = buttonBorderObject.AddComponent<Image>();
        buttonBorder.raycastTarget = false;
        buttonBorder.color = new Color(1f, 0.72f, 0.18f, 0.95f);
        RectTransform borderRect = buttonBorder.rectTransform;
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        GameObject buttonFillObject = new GameObject("Fill");
        buttonFillObject.transform.SetParent(buttonObject.transform, false);
        Image buttonFill = buttonFillObject.AddComponent<Image>();
        buttonFill.raycastTarget = false;
        buttonFill.color = new Color(0.14f, 0.10f, 0.05f, 0.92f);
        RectTransform fillRect = buttonFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        GameObject buttonTextObject = new GameObject("Text");
        buttonTextObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
        buttonText.raycastTarget = false;
        buttonText.text = "PLAY AGAIN";
        buttonText.fontSize = 36f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = new Color(1f, 0.82f, 0.46f, 1f);
        if (uiFont != null)
            buttonText.font = uiFont;

        RectTransform buttonTextRect = buttonText.rectTransform;
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        return group;
    }

    private void LoadReplayScene()
    {
        Time.timeScale = 1f;
        PlayerData.ResetGameProgress();
        Debug.Log("Loading TapToPlay from final screen.");
        SceneManager.LoadScene(replaySceneName);
    }

    private void HandleReplayInput()
    {
        if (!replayReady)
            return;

        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            LoadReplayScene();
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Rect replayButtonRect = new Rect(Screen.width * 0.5f - 180f, 78f, 360f, 82f);
        if (replayButtonRect.Contains(mousePosition))
            LoadReplayScene();
    }

    private EscapePreview BuildEscapePreview(Transform canvasTransform)
    {
        EscapePreview preview = new EscapePreview();
        if (canvasTransform == null)
            return preview;

        RenderTexture texture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        texture.name = "Escape Boat Render Texture";
        texture.Create();
        preview.texture = texture;

        GameObject imageObject = new GameObject("EscapeBoatImage");
        imageObject.transform.SetParent(canvasTransform, false);
        RawImage image = imageObject.AddComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;

        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(0f, -70f);
        imageRect.sizeDelta = new Vector2(1180f, 720f);
        imageObject.transform.SetSiblingIndex(3);

        GameObject stage = new GameObject("EscapeBoatPreviewStage");
        preview.stage = stage;

        int previewLayer = 31;
        BuildEscapeStage(stage.transform, previewLayer);

        GameObject cameraObject = new GameObject("EscapeBoatCamera");
        cameraObject.transform.SetParent(stage.transform, false);
        Camera previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.cullingMask = 1 << previewLayer;
        previewCamera.fieldOfView = 35f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 40f;
        previewCamera.targetTexture = texture;
        cameraObject.transform.localPosition = new Vector3(0f, 1.4f, -7.5f);
        cameraObject.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

        return preview;
    }

    private void BuildEscapeStage(Transform parent, int layer)
    {
        Material waterMaterial = CreateMaterial(new Color(0.025f, 0.19f, 0.18f, 0.95f));
        Material hullMaterial = CreateMaterial(new Color(0.02f, 0.48f, 0.52f, 1f));
        Material trimMaterial = CreateMaterial(new Color(0.86f, 0.82f, 0.72f, 1f));
        Material roofMaterial = CreateMaterial(new Color(0.86f, 0.32f, 0.16f, 1f));
        Material woodMaterial = CreateMaterial(new Color(0.48f, 0.28f, 0.12f, 1f));
        Material glassMaterial = CreateMaterial(new Color(0.035f, 0.055f, 0.075f, 1f));
        Material metalMaterial = CreateMaterial(new Color(0.58f, 0.62f, 0.58f, 1f));
        Material buoyMaterial = CreateMaterial(new Color(0.95f, 0.42f, 0.18f, 1f));
        Material islandMaterial = CreateMaterial(new Color(0.12f, 0.22f, 0.14f, 1f));

        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Quad);
        water.name = "EscapeSea";
        water.transform.SetParent(parent, false);
        water.transform.localPosition = new Vector3(0f, -0.58f, 2.2f);
        water.transform.localRotation = Quaternion.Euler(82f, 0f, 0f);
        water.transform.localScale = new Vector3(9f, 7f, 1f);
        water.GetComponent<Renderer>().sharedMaterial = waterMaterial;
        SetLayerRecursively(water, layer);

        GameObject island = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        island.name = "IslandBehindBoat";
        island.transform.SetParent(parent, false);
        island.transform.localPosition = new Vector3(0.5f, -0.25f, 3.4f);
        island.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        island.transform.localScale = new Vector3(2.9f, 0.36f, 0.6f);
        island.GetComponent<Renderer>().sharedMaterial = islandMaterial;
        SetLayerRecursively(island, layer);

        GameObject boat = new GameObject("EscapingBoat");
        boat.transform.SetParent(parent, false);
        boat.transform.localPosition = new Vector3(0f, -0.05f, -0.05f);
        boat.transform.localRotation = Quaternion.Euler(0f, -21f, 0f);
        boat.transform.localScale = Vector3.one * 1.08f;
        SetLayerRecursively(boat, layer);

        CreateHullMesh("TealHull", boat.transform, hullMaterial, layer);
        CreateCube("CreamHullStripe", boat.transform, new Vector3(-0.05f, -0.02f, -0.47f), new Vector3(2.05f, 0.045f, 0.025f), trimMaterial, layer);
        CreateCube("CreamHullStripeRight", boat.transform, new Vector3(-0.05f, -0.02f, 0.47f), new Vector3(2.05f, 0.045f, 0.025f), trimMaterial, layer);
        CreateCube("WoodDeck", boat.transform, new Vector3(-0.12f, 0.05f, 0f), new Vector3(2.15f, 0.08f, 0.78f), woodMaterial, layer);
        CreateCube("Cabin", boat.transform, new Vector3(-0.35f, 0.45f, 0f), new Vector3(0.76f, 0.58f, 0.56f), trimMaterial, layer);
        CreateCube("CabinRoof", boat.transform, new Vector3(-0.38f, 0.78f, 0f), new Vector3(0.94f, 0.12f, 0.7f), roofMaterial, layer);
        CreateCube("FrontWindow", boat.transform, new Vector3(0.04f, 0.50f, -0.291f), new Vector3(0.25f, 0.31f, 0.018f), glassMaterial, layer);
        CreateCube("SideWindowLeft", boat.transform, new Vector3(-0.41f, 0.51f, -0.291f), new Vector3(0.22f, 0.32f, 0.018f), glassMaterial, layer);
        CreateCube("SideWindowRight", boat.transform, new Vector3(-0.66f, 0.51f, -0.291f), new Vector3(0.18f, 0.32f, 0.018f), glassMaterial, layer);
        CreateCube("BackDeckCrate", boat.transform, new Vector3(0.72f, 0.2f, 0.14f), new Vector3(0.42f, 0.24f, 0.26f), woodMaterial, layer);
        CreateCylinder("RearRailLeft", boat.transform, new Vector3(0.92f, 0.48f, -0.34f), new Vector3(0.025f, 0.36f, 0.025f), Quaternion.identity, metalMaterial, layer);
        CreateCylinder("RearRailRight", boat.transform, new Vector3(0.92f, 0.48f, 0.34f), new Vector3(0.025f, 0.36f, 0.025f), Quaternion.identity, metalMaterial, layer);
        CreateCylinder("RearRailTop", boat.transform, new Vector3(0.92f, 0.84f, 0f), new Vector3(0.022f, 0.36f, 0.022f), Quaternion.Euler(90f, 0f, 0f), metalMaterial, layer);
        CreateAntenna("TallAntenna", boat.transform, new Vector3(-0.35f, 1.2f, 0.18f), 1.0f, metalMaterial, layer);
        CreateAntenna("ShortAntenna", boat.transform, new Vector3(-0.62f, 1.08f, -0.12f), 0.72f, metalMaterial, layer);
        CreateSphere("SideBuoy", boat.transform, new Vector3(0.45f, 0.03f, -0.53f), new Vector3(0.17f, 0.17f, 0.06f), buoyMaterial, layer);
        CreateSphere("BlueBarrel", boat.transform, new Vector3(0.42f, 0.28f, 0.22f), new Vector3(0.15f, 0.23f, 0.15f), CreateMaterial(new Color(0.18f, 0.42f, 0.72f, 1f)), layer);

        GameObject keyLightObject = new GameObject("EscapeKeyLight");
        keyLightObject.transform.SetParent(parent, false);
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1f, 0.88f, 0.58f, 1f);
        keyLight.intensity = 2.6f;
        keyLightObject.transform.localRotation = Quaternion.Euler(42f, -25f, 0f);

        GameObject fillLightObject = new GameObject("EscapeGlowLight");
        fillLightObject.transform.SetParent(parent, false);
        fillLightObject.transform.localPosition = new Vector3(0f, 0.1f, -1.6f);
        Light fillLight = fillLightObject.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(1f, 0.67f, 0.15f, 1f);
        fillLight.intensity = 5f;
        fillLight.range = 6f;

        SetLayerRecursively(keyLightObject, layer);
        SetLayerRecursively(fillLightObject, layer);
        StartCoroutine(AnimateEscapingBoat(boat.transform));
    }

    private void CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, int layer)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        SetLayerRecursively(cube, layer);
    }

    private void CreateHullMesh(string name, Transform parent, Material material, int layer)
    {
        GameObject hull = new GameObject(name);
        hull.transform.SetParent(parent, false);

        Mesh mesh = new Mesh();
        mesh.name = "FishingBoatHullMesh";

        float[] x = { -1.42f, -0.72f, 0.72f, 1.42f };
        float[] topHalfWidth = { 0.32f, 0.53f, 0.53f, 0.16f };
        float[] bottomHalfWidth = { 0.16f, 0.32f, 0.32f, 0.035f };
        Vector3[] vertices = new Vector3[x.Length * 4];

        for (int i = 0; i < x.Length; i++)
        {
            vertices[i * 4] = new Vector3(x[i], 0.09f, -topHalfWidth[i]);
            vertices[i * 4 + 1] = new Vector3(x[i], 0.09f, topHalfWidth[i]);
            vertices[i * 4 + 2] = new Vector3(x[i], -0.43f, -bottomHalfWidth[i]);
            vertices[i * 4 + 3] = new Vector3(x[i], -0.43f, bottomHalfWidth[i]);
        }

        int[] triangles = new int[]
        {
            0, 4, 2, 2, 4, 6,
            4, 8, 6, 6, 8, 10,
            8, 12, 10, 10, 12, 14,

            1, 3, 5, 3, 7, 5,
            5, 7, 9, 7, 11, 9,
            9, 11, 13, 11, 15, 13,

            2, 6, 3, 3, 6, 7,
            6, 10, 7, 7, 10, 11,
            10, 14, 11, 11, 14, 15,

            0, 2, 1, 1, 2, 3,
            12, 13, 14, 13, 15, 14
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = hull.AddComponent<MeshFilter>();
        filter.mesh = mesh;
        MeshRenderer renderer = hull.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        SetLayerRecursively(hull, layer);
    }

    private void CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material, int layer)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localRotation = rotation;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        SetLayerRecursively(cylinder, layer);
    }

    private void CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material, int layer)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = position;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        SetLayerRecursively(sphere, layer);
    }

    private void CreateAntenna(string name, Transform parent, Vector3 basePosition, float height, Material material, int layer)
    {
        CreateCylinder(name, parent, basePosition + new Vector3(0f, height * 0.5f, 0f), new Vector3(0.012f, height * 0.5f, 0.012f), Quaternion.identity, material, layer);
        CreateSphere(name + "Tip", parent, basePosition + new Vector3(0f, height, 0f), new Vector3(0.035f, 0.035f, 0.035f), material, layer);
    }

    private void CreateSail(string name, Transform parent, Vector3 position, float width, float height, Material material, int layer, bool flipped)
    {
        GameObject sail = new GameObject(name);
        sail.transform.SetParent(parent, false);
        sail.transform.localPosition = position;

        Mesh mesh = new Mesh();
        float side = flipped ? 1f : -1f;
        mesh.vertices = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, height, 0f),
            new Vector3(side * width, 0.12f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateNormals();

        MeshFilter filter = sail.AddComponent<MeshFilter>();
        filter.mesh = mesh;
        MeshRenderer renderer = sail.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        SetLayerRecursively(sail, layer);
    }

    private IEnumerator AnimateEscapingBoat(Transform boat)
    {
        Vector3 startPosition = boat.localPosition;
        float elapsed = 0f;

        while (boat != null)
        {
            elapsed += Time.unscaledDeltaTime;
            boat.localPosition = startPosition + new Vector3(0f, Mathf.Sin(elapsed * 1.8f) * 0.035f, elapsed * 0.08f);
            boat.localRotation = Quaternion.Euler(Mathf.Sin(elapsed * 1.4f) * 2f, -18f, Mathf.Sin(elapsed * 1.1f) * 2f);
            yield return null;
        }
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        return material;
    }

    private Sprite GetWinGlowSprite()
    {
        if (winGlowSprite == null)
            winGlowSprite = CreateRadialSprite("Escape Win Glow", 512, false);

        return winGlowSprite;
    }

    private Sprite GetWinVignetteSprite()
    {
        if (winVignetteSprite == null)
            winVignetteSprite = CreateRadialSprite("Escape Win Vignette", 512, true);

        return winVignetteSprite;
    }

    private Sprite CreateRadialSprite(string spriteName, int size, bool invert)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = invert
                    ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance))
                    : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance));

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private IEnumerator Fade(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = endAlpha;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = false;
    }

    private void OnGUI()
    {
        if (!playerNearby || escaping)
            return;

        GUIStyle style = new GUIStyle { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = Color.white;

        string label = CanEscape() ? readyText : missingText;
        GUI.Label(new Rect(Screen.width * 0.5f - 220f, Screen.height - 92f, 440f, 32f), label, style);
    }
}
