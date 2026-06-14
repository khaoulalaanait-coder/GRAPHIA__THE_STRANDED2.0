using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MinimapUI : MonoBehaviour
{
    public MazeGenerator mazeGen;
    public RawImage minimapImage;
    public GameObject mapPanel;
    public Transform player;
    public Transform monster;
    public KeyCode toggleKey = KeyCode.M;
    public KeyCode alternateToggleKey = KeyCode.Tab;

    private Texture2D mapTexture;
    private bool mapOpen = false;
    private bool runtimeOverlayCreated = false;
    private GameObject runtimeMapButton;
    private const int CellPixels = 12;
    private static readonly Color WallColor = new Color(0.02f, 0.018f, 0.016f);
    private static readonly Color CorridorColor = new Color(0.82f, 0.78f, 0.68f);
    private static readonly Color PathColor = new Color(1f, 0.84f, 0.16f);

    void Start()
    {
        ResolveMissingReferences();
        EnsureMapOverlay();
        SetMapOpen(false);
        Invoke(nameof(GenerateMap), 0.8f);
    }

    void Update()
    {
        KeepCursorReadyForMapButton();

        if (WasTogglePressed())
            ToggleMap();

        if (mapOpen)
            UpdateMapMarkers();
    }

    void KeepCursorReadyForMapButton()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    bool WasTogglePressed()
    {
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternateToggleKey))
            return true;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.mKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame))
            return true;
#endif

        return false;
    }

    public void ToggleMap()
    {
        ResolveMissingReferences();
        EnsureMapOverlay();
        SetMapOpen(!mapOpen);
    }

    void SetMapOpen(bool open)
    {
        mapOpen = open;

        if (mapPanel != null)
        {
            mapPanel.SetActive(mapOpen);
            mapPanel.transform.SetAsLastSibling();
        }

        if (runtimeMapButton != null)
            runtimeMapButton.SetActive(!mapOpen);

        Time.timeScale = mapOpen ? 0.15f : 1f;
        KeepCursorReadyForMapButton();

        if (mapOpen)
            UpdateMapMarkers();
    }

    public void GenerateMap()
    {
        ResolveMissingReferences();
        EnsureMapOverlay();

        if (minimapImage == null)
        {
            Debug.LogError("MinimapImage is NULL!");
            return;
        }

        if (mazeGen == null)
        {
            Debug.LogError("MazeGen is NULL!");
            return;
        }

        if (!mazeGen.IsGenerated)
        {
            Invoke(nameof(GenerateMap), 0.2f);
            return;
        }

        int texW = mazeGen.width * CellPixels;
        int texH = mazeGen.height * CellPixels;
        mapTexture = new Texture2D(texW, texH);
        mapTexture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = WallColor;
        mapTexture.SetPixels(pixels);

        DrawMazePattern();
        DrawRouteToExit();
        DrawMapMarkers();

        mapTexture.Apply();
        minimapImage.texture = mapTexture;
    }

    void DrawMazePattern()
    {
        for (int x = 0; x < mazeGen.width; x++)
        {
            for (int y = 0; y < mazeGen.height; y++)
            {
                DrawRect(x * CellPixels + 2, y * CellPixels + 2, CellPixels - 4, CellPixels - 4, CorridorColor);

                if (x < mazeGen.width - 1 && !mazeGen.HasVerticalWall(x + 1, y))
                    DrawRect(x * CellPixels + CellPixels - 2, y * CellPixels + 2, 4, CellPixels - 4, CorridorColor);

                if (y < mazeGen.height - 1 && !mazeGen.HasHorizontalWall(x, y + 1))
                    DrawRect(x * CellPixels + 2, y * CellPixels + CellPixels - 2, CellPixels - 4, 4, CorridorColor);
            }
        }
    }

    void DrawRouteToExit()
    {
        if (player == null)
            return;

        Vector2Int start = new Vector2Int(WorldToCell(player.position.x, mazeGen.width), WorldToCell(player.position.z, mazeGen.height));
        Vector2Int exit = new Vector2Int(mazeGen.width - 1, mazeGen.height - 1);
        List<Vector2Int> route = FindRoute(start, exit);

        for (int i = 0; i < route.Count; i++)
        {
            DrawDot(route[i].x, route[i].y, 2, PathColor);

            if (i > 0)
                DrawLine(CellCenter(route[i - 1]), CellCenter(route[i]), PathColor);
        }
    }

    List<Vector2Int> FindRoute(Vector2Int start, Vector2Int exit)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        cameFrom[start] = start;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == exit)
                break;

            foreach (Vector2Int next in GetOpenNeighbors(current))
            {
                if (cameFrom.ContainsKey(next))
                    continue;

                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        List<Vector2Int> route = new List<Vector2Int>();
        if (!cameFrom.ContainsKey(exit))
            return route;

        Vector2Int step = exit;
        while (step != start)
        {
            route.Add(step);
            step = cameFrom[step];
        }

        route.Add(start);
        route.Reverse();
        return route;
    }

    IEnumerable<Vector2Int> GetOpenNeighbors(Vector2Int cell)
    {
        int x = cell.x;
        int y = cell.y;

        if (x < mazeGen.width - 1 && !mazeGen.HasVerticalWall(x + 1, y))
            yield return new Vector2Int(x + 1, y);

        if (x > 0 && !mazeGen.HasVerticalWall(x, y))
            yield return new Vector2Int(x - 1, y);

        if (y < mazeGen.height - 1 && !mazeGen.HasHorizontalWall(x, y + 1))
            yield return new Vector2Int(x, y + 1);

        if (y > 0 && !mazeGen.HasHorizontalWall(x, y))
            yield return new Vector2Int(x, y - 1);
    }

    void UpdateMapMarkers()
    {
        GenerateMap();
    }

    void DrawMapMarkers()
    {
        DrawDot(0, 0, 3, Color.cyan);
        DrawDot(mazeGen.width - 1, mazeGen.height - 1, 3, Color.green);

        if (player != null)
            DrawDot(WorldToCell(player.position.x, mazeGen.width), WorldToCell(player.position.z, mazeGen.height), 3, Color.blue);

        if (monster != null)
            DrawDot(WorldToCell(monster.position.x, mazeGen.width), WorldToCell(monster.position.z, mazeGen.height), 3, Color.red);
    }

    int WorldToCell(float value, int max)
    {
        return Mathf.Clamp(Mathf.FloorToInt(value / mazeGen.cellSize), 0, max - 1);
    }

    Vector2Int CellCenter(Vector2Int cell)
    {
        return new Vector2Int(cell.x * CellPixels + CellPixels / 2, cell.y * CellPixels + CellPixels / 2);
    }

    void DrawDot(int cx, int cy, int radius, Color color)
    {
        Vector2Int center = CellCenter(new Vector2Int(cx, cy));
        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                if (x * x + y * y <= radius * radius)
                    SetPixelSafe(center.x + x, center.y + y, color);
    }

    void DrawLine(Vector2Int from, Vector2Int to, Color color)
    {
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        int sx = from.x < to.x ? 1 : -1;
        int sy = from.y < to.y ? 1 : -1;
        int err = dx - dy;
        int x = from.x;
        int y = from.y;

        while (true)
        {
            DrawPixelBrush(x, y, 2, color);
            if (x == to.x && y == to.y)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    void DrawPixelBrush(int centerX, int centerY, int radius, Color color)
    {
        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                SetPixelSafe(centerX + x, centerY + y, color);
    }

    void DrawRect(int startX, int startY, int width, int height, Color color)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                SetPixelSafe(startX + x, startY + y, color);
    }

    void SetPixelSafe(int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= mapTexture.width || y >= mapTexture.height)
            return;

        mapTexture.SetPixel(x, y, color);
    }

    void EnsureMapOverlay()
    {
        if (runtimeOverlayCreated && mapPanel != null && minimapImage != null)
            return;

        if (mapPanel != null)
            mapPanel.SetActive(false);

        GameObject canvasObject = new GameObject("RuntimeMapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        runtimeMapButton = CreateButton(canvas.transform, "MAP", new Vector2(140f, 62f), new Vector2(-95f, -60f));
        runtimeMapButton.GetComponent<Button>().onClick.AddListener(ToggleMap);

        GameObject panel = new GameObject("RuntimeMapPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(620f, 620f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.04f, 0.035f, 0.03f, 0.96f);

        GameObject imageObject = new GameObject("RuntimeMinimapImage", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(panel.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(0f, 30f);
        imageRect.sizeDelta = new Vector2(520f, 520f);

        minimapImage = imageObject.GetComponent<RawImage>();
        minimapImage.color = Color.white;

        GameObject closeButton = CreateButton(panel.transform, "CLOSE", new Vector2(150f, 48f), new Vector2(0f, 32f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 32f);
        closeButton.GetComponent<Button>().onClick.AddListener(ToggleMap);

        mapPanel = panel;
        runtimeOverlayCreated = true;
    }

    GameObject CreateButton(Transform parent, string label, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.96f, 0.92f, 0.78f, 1f);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.08f, 0.07f, 0.06f, 1f);
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);

        return buttonObject;
    }

    void ResolveMissingReferences()
    {
        if (mazeGen == null)
            mazeGen = FindFirstObjectByType<MazeGenerator>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (monster == null)
        {
            MonsterAI monsterAI = FindFirstObjectByType<MonsterAI>();
            if (monsterAI != null)
                monster = monsterAI.transform;
        }
    }
}