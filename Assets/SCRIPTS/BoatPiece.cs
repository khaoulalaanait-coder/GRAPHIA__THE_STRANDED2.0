using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Attach to each of the 5 boat piece prefabs AND to Piece_Engine in the scene
public class BoatPiece : MonoBehaviour
{
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private string pieceId;

    [Header("Engine Piece")]
    [SerializeField] private bool isEnginePiece;                    // Tick on Piece_Engine only
    [SerializeField] private TMP_FontAsset journalFont;             // Assign Caveat-Regular SDF (same asset as JournalManager)

    private bool playerNearby;
    private bool collected;
    private BoatPieceManager manager;
    private Transform player;
    private Canvas promptCanvas;
    private TextMeshProUGUI promptText;

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(pieceId))
            pieceId = BuildDefaultPieceId();

        if (PlayerData.IsBoatPieceCollected(pieceId))
        {
            collected = true;
            gameObject.SetActive(false);
            return;
        }

        // Large sphere trigger for proximity detection; does not disturb the existing mesh collider
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2f;

        manager = FindFirstObjectByType<BoatPieceManager>();
        FindPlayer();
    }

    private void Update()
    {
        if (collected)
            return;

        if (player == null)
            FindPlayer();

        if (player != null)
            playerNearby = Vector3.Distance(transform.position, player.position) <= interactDistance;

        UpdatePrompt();

        if (!playerNearby || Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            Collect();
    }

    private void Collect()
    {
        // Guard prevents re-entry if trigger fires again before SetActive(false) completes
        if (collected)
            return;

        collected = true;

        bool newlyCollected = manager != null
            ? manager.OnPieceCollected(pieceId, isEnginePiece)
            : PlayerData.MarkBoatPieceCollected(pieceId);

        if (!newlyCollected)
        {
            HidePrompt();
            gameObject.SetActive(false);
            return;
        }

        // PlayClipAtPoint spawns its own temporary AudioSource so the sound survives deactivation
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // Show dramatic message before hiding - only for the engine piece
        if (isEnginePiece)
            StartCoroutine(ShowEnginePopup());
        else
        {
            HidePrompt();
            gameObject.SetActive(false);
        }
    }

    private string BuildDefaultPieceId()
    {
        string normalizedName = gameObject.name.Replace("(Clone)", "").Trim();
        return isEnginePiece ? "engine" : normalizedName;
    }

    // Builds the full-screen overlay, fades it in (1 s), holds (3 s), fades out, then hides the piece
    private IEnumerator ShowEnginePopup()
    {
        Canvas canvas = BuildPopupCanvas();
        CanvasGroup group = canvas.GetComponent<CanvasGroup>();

        // Fade IN over 1 second
        yield return Fade(group, 0f, 1f, 1f);

        // Hold for 3 seconds
        yield return new WaitForSeconds(3f);

        // Fade OUT over 1 second
        yield return Fade(group, 1f, 0f, 1f);

        // Clean up canvas, then hide the piece
        Destroy(canvas.gameObject);
        gameObject.SetActive(false);
    }

    // Animates a CanvasGroup alpha from startAlpha to endAlpha over duration seconds
    private IEnumerator Fade(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        group.alpha = endAlpha;
    }

    // Creates the popup Canvas entirely in code - no prefab needed
    private Canvas BuildPopupCanvas()
    {
        var go = new GameObject("EnginePopupCanvas");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // Above map, journal, HUD

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // CanvasGroup drives the fade - alpha starts at 0
        var group = go.AddComponent<CanvasGroup>();
        group.alpha          = 0f;
        group.blocksRaycasts = false; // Don't swallow clicks during the popup

        // Full-screen semi-transparent black overlay
        var overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(go.transform, false);
        var overlay = overlayGO.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.82f);
        var overlayRT = overlay.rectTransform;
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        // Centred text - Caveat font if assigned, TMP default otherwise
        var textGO = new GameObject("MessageText");
        textGO.transform.SetParent(go.transform, false);
        var tmp       = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "The engine is yours...\nDon't end up like me.";
        tmp.fontSize  = 36;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (journalFont != null) tmp.font = journalFont;

        var textRT        = tmp.rectTransform;
        textRT.anchorMin  = new Vector2(0.15f, 0.35f);
        textRT.anchorMax  = new Vector2(0.85f, 0.65f);
        textRT.offsetMin  = Vector2.zero;
        textRT.offsetMax  = Vector2.zero;

        return canvas;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            UpdatePrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            UpdatePrompt();
        }
    }

    private void UpdatePrompt()
    {
        if (collected)
        {
            HidePrompt();
            return;
        }

        if (playerNearby)
            ShowPrompt();
        else
            HidePrompt();
    }

    private void ShowPrompt()
    {
        if (promptCanvas == null)
            BuildPromptCanvas();

        promptCanvas.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(false);
    }

    private void BuildPromptCanvas()
    {
        GameObject canvasObj = new GameObject("BoatPiecePromptCanvas");
        promptCanvas = canvasObj.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject labelObj = new GameObject("PromptText");
        labelObj.transform.SetParent(canvasObj.transform, false);
        promptText = labelObj.AddComponent<TextMeshProUGUI>();
        promptText.text = "[ E ] Collect Piece";
        promptText.fontSize = 30f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.outlineWidth = 0.2f;
        promptText.outlineColor = Color.black;

        RectTransform rt = promptText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 90f);
        rt.sizeDelta = new Vector2(420f, 60f);

        promptCanvas.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        HidePrompt();
    }

    private void OnDestroy()
    {
        if (promptCanvas != null)
            Destroy(promptCanvas.gameObject);
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }
}
