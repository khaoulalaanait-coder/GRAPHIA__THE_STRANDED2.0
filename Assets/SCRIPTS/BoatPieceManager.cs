using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Attach to an empty GameObject called BoatPieceManager in the scene
public class BoatPieceManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnZones;    // 5 zone transforms (SpawnZone_Beach, etc.)
    [SerializeField] private GameObject[] piecePrefabs; // 5 boat piece prefabs
    [SerializeField] private bool randomizePieceZones = false;
    [SerializeField] private GameObject enginePiece;    // Piece_Engine already in the scene
    [SerializeField] private TMP_FontAsset pieceCounterFont;
    [SerializeField] private float engineRewardDuration = 8f;

    private const int totalPieces = 4;
    private int collectedCount;
    private bool engineCollected;
    private bool notifiedEngineLastPiece;
    private bool engineRevealed;
    private bool engineRewardShowing;
    private TextMeshProUGUI pieceCounterLabel;
    private static Sprite rewardGlowSprite;
    private static Sprite rewardVignetteSprite;

    private struct RewardPreview
    {
        public GameObject stage;
        public RenderTexture texture;
    }

    private void Start()
    {
        // Engine stays hidden until the stone puzzle is solved
        if (enginePiece != null)
            enginePiece.SetActive(false);

        SpawnPieces();
        BuildPieceCounterUi();
        collectedCount = Mathf.Clamp(PlayerData.BoatPiecesCollectedCount, 0, totalPieces);
        UnlockProgressionBlocks();
        UpdatePieceCounterUi();
    }

    // Instantiates one prefab per spawn zone, shuffled so placement is random each run
    private void SpawnPieces()
    {
        if (spawnZones == null || piecePrefabs == null)
            return;

        int regularPieceCount = totalPieces - 1;
        int count = Mathf.Min(spawnZones.Length, piecePrefabs.Length, regularPieceCount);
        GameObject[] piecesToSpawn = randomizePieceZones ? ShuffledCopy(piecePrefabs) : piecePrefabs;

        for (int i = 0; i < count; i++)
        {
            if (spawnZones[i] == null || piecesToSpawn[i] == null)
                continue;

            Vector3 spawnPos = spawnZones[i].position;
            Instantiate(piecesToSpawn[i], spawnPos, Quaternion.identity);
        }
    }

    private GameObject[] ShuffledCopy(GameObject[] source)
    {
        GameObject[] copy = (GameObject[])source.Clone();
        for (int i = copy.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GameObject temp = copy[i];
            copy[i] = copy[j];
            copy[j] = temp;
        }
        return copy;
    }

    // Called by CrystalConnectionManager when the stone puzzle is solved
    public void OnStonePuzzleComplete()
    {
        if (engineRevealed)
            return;

        engineRevealed = true;

        if (enginePiece == null)
            enginePiece = FindEnginePiece();

        if (enginePiece == null)
            Debug.LogWarning("Stone puzzle solved, but Piece_Engine was not found or assigned.");

        GrantEnginePiece();
        ShowEngineRewardScreen();

        Debug.Log("Stone puzzle solved - Engine piece received!");
    }

    private void GrantEnginePiece()
    {
        bool newlyCollected = PlayerData.MarkBoatPieceCollected("engine");
        collectedCount = Mathf.Clamp(PlayerData.BoatPiecesCollectedCount, 0, totalPieces);
        engineCollected = true;

        if (enginePiece != null)
            enginePiece.SetActive(false);

        UnlockProgressionBlocks();
        UpdatePieceCounterUi();

        if (!newlyCollected)
            return;

        if (JournalManager.instance != null)
            JournalManager.instance.UnlockNextPage();
        else
            PlayerData.UnlockJournalPage(PlayerData.journalUnlockedPages + 1);

        if (collectedCount >= totalPieces)
            Debug.Log("All boat pieces collected. Return to the boat with fuel.");
    }

    private void ShowEngineRewardScreen()
    {
        if (engineRewardShowing)
            return;

        GameObject oldReward = GameObject.Find("EngineRewardCanvas");
        if (oldReward != null)
            Destroy(oldReward);

        StartCoroutine(ShowEngineRewardRoutine());
    }

    private IEnumerator ShowEngineRewardRoutine()
    {
        engineRewardShowing = true;

        CanvasGroup group = BuildEngineRewardCanvas();
        RewardPreview preview = BuildEnginePreview(group.transform);
        group.alpha = 1f;
        Debug.Log("Engine reward screen displayed.");

        float displayDuration = Mathf.Max(engineRewardDuration, 8f);
        yield return new WaitForSecondsRealtime(displayDuration);
        yield return Fade(group, 1f, 0f, 0.35f);

        if (preview.stage != null)
            Destroy(preview.stage);

        if (preview.texture != null)
        {
            preview.texture.Release();
            Destroy(preview.texture);
        }

        if (group != null)
            Destroy(group.gameObject);

        engineRewardShowing = false;
        TriggerEscapeAfterEngineReward();
    }

    private void TriggerEscapeAfterEngineReward()
    {
        BoatEscape escape = FindFirstObjectByType<BoatEscape>();
        if (escape == null)
        {
            Debug.LogWarning("Engine reward finished, but BoatEscape was not found in the scene.");
            return;
        }

        escape.BeginAutomaticEscape();
    }

    private CanvasGroup BuildEngineRewardCanvas()
    {
        GameObject canvasObject = new GameObject("EngineRewardCanvas");
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
        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = false;

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0.015f, 0.025f, 0.022f, 0.72f);
        RectTransform backgroundRt = backgroundObject.GetComponent<RectTransform>();
        backgroundRt.anchorMin = Vector2.zero;
        backgroundRt.anchorMax = Vector2.one;
        backgroundRt.offsetMin = Vector2.zero;
        backgroundRt.offsetMax = Vector2.zero;

        GameObject vignetteObject = new GameObject("Vignette");
        vignetteObject.transform.SetParent(canvasObject.transform, false);
        Image vignette = vignetteObject.AddComponent<Image>();
        vignette.sprite = GetRewardVignetteSprite();
        vignette.color = new Color(0f, 0f, 0f, 0.72f);
        RectTransform vignetteRt = vignetteObject.GetComponent<RectTransform>();
        vignetteRt.anchorMin = Vector2.zero;
        vignetteRt.anchorMax = Vector2.one;
        vignetteRt.offsetMin = Vector2.zero;
        vignetteRt.offsetMax = Vector2.zero;

        GameObject glowObject = new GameObject("GoldGlow");
        glowObject.transform.SetParent(canvasObject.transform, false);
        Image glow = glowObject.AddComponent<Image>();
        glow.sprite = GetRewardGlowSprite();
        glow.color = new Color(1f, 0.68f, 0.12f, 0.62f);
        RectTransform glowRt = glowObject.GetComponent<RectTransform>();
        glowRt.anchorMin = new Vector2(0.5f, 0.5f);
        glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.pivot = new Vector2(0.5f, 0.5f);
        glowRt.anchoredPosition = new Vector2(0f, -40f);
        glowRt.sizeDelta = new Vector2(1050f, 1050f);

        GameObject subtitleObject = new GameObject("Subtitle");
        subtitleObject.transform.SetParent(canvasObject.transform, false);
        TextMeshProUGUI subtitle = subtitleObject.AddComponent<TextMeshProUGUI>();
        subtitle.text = "You received";
        subtitle.fontSize = 58f;
        subtitle.fontStyle = FontStyles.Bold;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = Color.white;
        ApplyRewardFont(subtitle);
        RectTransform subtitleRt = subtitleObject.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0.5f, 1f);
        subtitleRt.anchorMax = new Vector2(0.5f, 1f);
        subtitleRt.pivot = new Vector2(0.5f, 1f);
        subtitleRt.anchoredPosition = new Vector2(0f, -96f);
        subtitleRt.sizeDelta = new Vector2(900f, 80f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(canvasObject.transform, false);
        TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
        title.text = "Piece Engine!";
        title.fontSize = 82f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.76f, 0.27f, 1f);
        ApplyRewardFont(title);
        RectTransform titleRt = titleObject.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -174f);
        titleRt.sizeDelta = new Vector2(1000f, 110f);

        return group;
    }

    private Sprite GetRewardGlowSprite()
    {
        if (rewardGlowSprite == null)
            rewardGlowSprite = CreateRadialSprite("Engine Reward Glow", 512, false);

        return rewardGlowSprite;
    }

    private Sprite GetRewardVignetteSprite()
    {
        if (rewardVignetteSprite == null)
            rewardVignetteSprite = CreateRadialSprite("Engine Reward Vignette", 512, true);

        return rewardVignetteSprite;
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

    private void ApplyRewardFont(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        if (pieceCounterFont == null && JournalManager.instance != null)
            pieceCounterFont = JournalManager.instance.JournalFont;

        if (pieceCounterFont != null)
            text.font = pieceCounterFont;
    }

    private RewardPreview BuildEnginePreview(Transform canvasTransform)
    {
        RewardPreview rewardPreview = new RewardPreview();

        if (enginePiece == null || canvasTransform == null)
            return rewardPreview;

        RenderTexture texture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
        texture.name = "Engine Reward Render Texture";
        texture.Create();
        rewardPreview.texture = texture;

        GameObject imageObject = new GameObject("EngineRewardImage");
        imageObject.transform.SetParent(canvasTransform, false);
        RawImage image = imageObject.AddComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;

        RectTransform imageRt = imageObject.GetComponent<RectTransform>();
        imageRt.anchorMin = new Vector2(0.5f, 0.5f);
        imageRt.anchorMax = new Vector2(0.5f, 0.5f);
        imageRt.pivot = new Vector2(0.5f, 0.5f);
        imageRt.anchoredPosition = new Vector2(0f, -70f);
        imageRt.sizeDelta = new Vector2(980f, 700f);
        imageObject.transform.SetSiblingIndex(3);

        GameObject stage = new GameObject("EngineRewardPreviewStage");
        rewardPreview.stage = stage;

        int previewLayer = 31;

        GameObject pivot = new GameObject("EngineRewardModelPivot");
        pivot.transform.SetParent(stage.transform, false);
        SetLayerRecursively(pivot, previewLayer);

        GameObject preview = Instantiate(enginePiece, pivot.transform);
        preview.name = "EngineRewardPreview";
        preview.SetActive(true);
        preview.transform.localPosition = Vector3.zero;
        preview.transform.localRotation = Quaternion.Euler(8f, -22f, 0f);
        preview.transform.localScale = Vector3.one;
        SetLayerRecursively(preview, previewLayer);

        foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (BoatPiece piece in preview.GetComponentsInChildren<BoatPiece>(true))
            piece.enabled = false;

        Bounds bounds = CalculateRendererBounds(preview);
        if (bounds.size != Vector3.zero)
        {
            preview.transform.position -= bounds.center;
            bounds = CalculateRendererBounds(preview);

            float largestSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestSize > 0.01f)
                pivot.transform.localScale = Vector3.one * (2.35f / largestSize);
        }

        GameObject cameraObject = new GameObject("EngineRewardCamera");
        cameraObject.transform.SetParent(stage.transform, false);
        Camera previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.cullingMask = 1 << previewLayer;
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 1.35f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 20f;
        previewCamera.targetTexture = texture;
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -5f);
        cameraObject.transform.localRotation = Quaternion.identity;

        GameObject keyLightObject = new GameObject("EngineRewardKeyLight");
        keyLightObject.transform.SetParent(stage.transform, false);
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1f, 0.88f, 0.68f, 1f);
        keyLight.intensity = 2.4f;
        keyLightObject.transform.localRotation = Quaternion.Euler(35f, -30f, 0f);

        GameObject fillLightObject = new GameObject("EngineRewardFillLight");
        fillLightObject.transform.SetParent(stage.transform, false);
        Light fillLight = fillLightObject.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(1f, 0.68f, 0.2f, 1f);
        fillLight.intensity = 4.2f;
        fillLight.range = 7f;
        fillLightObject.transform.localPosition = new Vector3(0f, -0.3f, -2f);

        StartCoroutine(SpinRewardPreview(pivot.transform));
        return rewardPreview;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private Bounds CalculateRendererBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(target.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private IEnumerator SpinRewardPreview(Transform preview)
    {
        while (preview != null)
        {
            preview.Rotate(Vector3.up, 28f * Time.unscaledDeltaTime, Space.World);
            yield return null;
        }
    }

    private IEnumerator Fade(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = endAlpha;
    }

    private GameObject FindEnginePiece()
    {
        GameObject activeEngine = GameObject.Find("Piece_Engine");
        if (activeEngine != null)
            return activeEngine;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || obj.name != "Piece_Engine")
                continue;

            if (!obj.scene.IsValid() || obj.scene != SceneManager.GetActiveScene())
                continue;

            return obj;
        }

        return null;
    }

    private void ActivateParents(Transform target)
    {
        if (target == null)
            return;

        if (target.parent != null)
            ActivateParents(target.parent);

        target.gameObject.SetActive(true);
    }

    // Called by BoatPiece when a piece is collected
    public bool OnPieceCollected(string pieceId, bool isEnginePiece)
    {
        if (!PlayerData.MarkBoatPieceCollected(pieceId))
            return false;

        collectedCount = Mathf.Clamp(PlayerData.boatPiecesCollected, 0, totalPieces);
        if (isEnginePiece)
            engineCollected = true;

        Debug.Log($"Boat pieces: {collectedCount}/{totalPieces}");
        UnlockProgressionBlocks();
        UpdatePieceCounterUi();

        // Reveal the storm timer UI once only when the engine is the final missing piece.
        if (!notifiedEngineLastPiece && collectedCount >= totalPieces - 1 && !engineCollected && PuzzleTimer.instance != null)
        {
            notifiedEngineLastPiece = true;
            PuzzleTimer.instance.OnEngineIsLastPiece();
        }

        // Reveal the next journal entry for each piece found
        if (JournalManager.instance != null)
            JournalManager.instance.UnlockNextPage();
        else
            PlayerData.UnlockJournalPage(PlayerData.journalUnlockedPages + 1);

        if (collectedCount >= totalPieces)
            Debug.Log("All boat pieces collected. Return to the boat with fuel.");

        return true;
    }

    private void BuildPieceCounterUi()
    {
        GameObject canvasObj = new GameObject("PieceCounterCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("PieceCounterPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.769f, 0.635f, 0.396f, 0.93f); // #C4A265 parchment

        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(24f, -24f);
        panelRt.sizeDelta = new Vector2(250f, 56f);

        GameObject edgeObj = new GameObject("InnerEdge");
        edgeObj.transform.SetParent(panelObj.transform, false);
        Image edgeImage = edgeObj.AddComponent<Image>();
        edgeImage.color = new Color(0.08f, 0.04f, 0.01f, 0.20f);

        RectTransform edgeRt = edgeObj.GetComponent<RectTransform>();
        edgeRt.anchorMin = Vector2.zero;
        edgeRt.anchorMax = Vector2.one;
        edgeRt.offsetMin = new Vector2(3f, 3f);
        edgeRt.offsetMax = new Vector2(-3f, -3f);

        GameObject labelObj = new GameObject("PieceCounterLabel");
        labelObj.transform.SetParent(panelObj.transform, false);
        pieceCounterLabel = labelObj.AddComponent<TextMeshProUGUI>();
        pieceCounterLabel.fontSize = 28f;
        pieceCounterLabel.alignment = TextAlignmentOptions.Center;
        pieceCounterLabel.color = new Color(0.13f, 0.07f, 0.02f, 1f);

        if (pieceCounterFont == null && JournalManager.instance != null)
            pieceCounterFont = JournalManager.instance.JournalFont;

        if (pieceCounterFont != null)
            pieceCounterLabel.font = pieceCounterFont;

        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(10f, 6f);
        labelRt.offsetMax = new Vector2(-10f, -6f);
    }

    private void UpdatePieceCounterUi()
    {
        if (pieceCounterLabel == null)
            return;

        pieceCounterLabel.text = $"Boat Pieces: {collectedCount} / {totalPieces}";
    }

    private void UnlockProgressionBlocks()
    {
        int pieces = Mathf.Clamp(PlayerData.BoatPiecesCollectedCount, 0, totalPieces);

        if (pieces >= 1)
            DisableBlock("BlockCliff");

        if (pieces >= 2)
        {
            DisableBlock("BlockRuins");
            DisableBlock("BlockRuins(1)");
        }

        if (pieces >= 3)
            DisableBlock("BlockStones");
    }

    private void DisableBlock(string blockName)
    {
        GameObject block = GameObject.Find(blockName);
        if (block != null && block.activeSelf)
            block.SetActive(false);
    }

    public int CollectedCount
    {
        get { return collectedCount; }
    }

    public bool HasAllPieces
    {
        get { return collectedCount >= totalPieces; }
    }
}




