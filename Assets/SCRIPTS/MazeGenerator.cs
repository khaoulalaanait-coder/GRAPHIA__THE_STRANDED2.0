using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Size")]
    public int width = 12;
    public int height = 12;
    public float cellSize = 4f;
    public int extraOpenings = 22;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Player & Monster")]
    public GameObject player;
    public GameObject monster;
    public GameObject exitTrigger;

    private bool[,] visited;
    private bool[,] wallsH; // horizontal walls (top/bottom)
    private bool[,] wallsV; // vertical walls (left/right)

    public bool IsGenerated => wallsH != null && wallsV != null;

    void Start()
    {
        width = Mathf.Clamp(width, 7, 12);
        height = Mathf.Clamp(height, 7, 12);
        SetupAtmosphere();
        GenerateMaze();
        BuildMaze();
        PlaceActors();
        Invoke("BakeNavMesh", 0.5f);
    }

    void SetupAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.18f, 0.24f, 0.28f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.025f;
        RenderSettings.ambientLight = new Color(0.18f, 0.2f, 0.24f);

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light sceneLight in lights)
        {
            if (sceneLight.type == LightType.Directional)
            {
                sceneLight.color = new Color(0.62f, 0.74f, 0.9f);
                sceneLight.intensity = 0.55f;
                sceneLight.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            }
        }
    }

    void BakeNavMesh()
    {
        bool monsterWasActive = monster != null && monster.activeSelf;
        if (monsterWasActive)
            monster.SetActive(false);

        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface != null)
        {
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();
        }

        if (monsterWasActive)
            monster.SetActive(true);

        // Reposition monster after the NavMesh is ready.
        if (monster != null)
            PlaceOnNavMesh(monster, GetMonsterStartPosition());
    }

    void GenerateMaze()
    {
        visited = new bool[width, height];
        // true = wall exists
        wallsH = new bool[width, height + 1];
        wallsV = new bool[width + 1, height];

        // Start with all walls on
        for (int x = 0; x < width; x++)
            for (int y = 0; y <= height; y++)
                wallsH[x, y] = true;

        for (int x = 0; x <= width; x++)
            for (int y = 0; y < height; y++)
                wallsV[x, y] = true;

        // Carve paths using recursive backtracking
        CarveFrom(0, 0);

        AddExtraOpenings();

        // Create exit opening at bottom-right
        wallsV[width, height - 1] = false;
    }

    void AddExtraOpenings()
    {
        int openings = Mathf.Clamp(extraOpenings, 0, width + height);

        for (int i = 0; i < openings; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            if (Random.value > 0.5f && x < width - 1)
                wallsV[x + 1, y] = false;
            else if (y < height - 1)
                wallsH[x, y + 1] = false;
        }
    }

    void CarveFrom(int x, int y)
    {
        visited[x, y] = true;

        // Randomize direction order
        int[] dirs = { 0, 1, 2, 3 };
        Shuffle(dirs);

        foreach (int dir in dirs)
        {
            int nx = x, ny = y;
            if (dir == 0) nx++; // right
            if (dir == 1) nx--; // left
            if (dir == 2) ny++; // up
            if (dir == 3) ny--; // down

            if (nx < 0 || nx >= width ||
                ny < 0 || ny >= height) continue;
            if (visited[nx, ny]) continue;

            // Remove wall between current and neighbor
            if (dir == 0) wallsV[x + 1, y] = false;
            if (dir == 1) wallsV[x, y] = false;
            if (dir == 2) wallsH[x, y + 1] = false;
            if (dir == 3) wallsH[x, y] = false;

            CarveFrom(nx, ny);
        }
    }

    void BuildMaze()
    {
        // Make a parent object to keep Hierarchy clean
        GameObject mazeParent = new GameObject("GeneratedMaze");

        float wallHeight = 4f;
        float wallThickness = 0.3f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 cell = new Vector3(
                    x * cellSize, 0, y * cellSize);

                // Floor tile
                GameObject floor = Instantiate(floorPrefab,
                    cell + new Vector3(cellSize / 2, 0, cellSize / 2),
                    Quaternion.identity, mazeParent.transform);
                floor.transform.localScale = new Vector3(
                    cellSize, 0.2f, cellSize);

                // Bottom wall (horizontal)
                if (wallsH[x, y])
                    SpawnWall(mazeParent,
                        cell + new Vector3(cellSize / 2, wallHeight / 2, 0),
                        new Vector3(cellSize, wallHeight, wallThickness));

                // Left wall (vertical)
                if (wallsV[x, y])
                    SpawnWall(mazeParent,
                        cell + new Vector3(0, wallHeight / 2, cellSize / 2),
                        new Vector3(wallThickness, wallHeight, cellSize));

                // Top row - add top walls
                if (y == height - 1 && wallsH[x, y + 1])
                    SpawnWall(mazeParent,
                        cell + new Vector3(cellSize / 2, wallHeight / 2, cellSize),
                        new Vector3(cellSize, wallHeight, wallThickness));

                // Right column - add right walls
                if (x == width - 1 && wallsV[x + 1, y])
                    SpawnWall(mazeParent,
                        cell + new Vector3(cellSize, wallHeight / 2, cellSize / 2),
                        new Vector3(wallThickness, wallHeight, cellSize));
            }
        }
        GameObject exitMarker = GameObject.CreatePrimitive(
            PrimitiveType.Cube);
        exitMarker.name = "ExitMarker";
        exitMarker.transform.position = new Vector3(
            width * cellSize + cellSize / 2, 1f,
            (height - 1) * cellSize + cellSize / 2);
        exitMarker.transform.localScale = new Vector3(
            0.5f, 2f, cellSize);

        Renderer r = exitMarker.GetComponent<Renderer>();
        Material m = CreateMaterial(new Color(0.1f, 1f, 0.48f), true);
        r.material = m;
        exitMarker.transform.parent = mazeParent.transform;

        BuildExitBeacon(mazeParent.transform);
    }

    void BuildExitBeacon(Transform parent)
    {
        Vector3 exitCenter = new Vector3(
            width * cellSize + cellSize / 2,
            0f,
            (height - 1) * cellSize + cellSize / 2);

        Material stone = CreateMaterial(new Color(0.16f, 0.18f, 0.2f), false);
        Material glow = CreateMaterial(new Color(0.15f, 1f, 0.58f), true);

        CreatePrimitive("ExitArchLeft", PrimitiveType.Cube, parent,
            exitCenter + new Vector3(0f, 2f, -cellSize * 0.45f),
            new Vector3(0.55f, 4f, 0.55f), stone);
        CreatePrimitive("ExitArchRight", PrimitiveType.Cube, parent,
            exitCenter + new Vector3(0f, 2f, cellSize * 0.45f),
            new Vector3(0.55f, 4f, 0.55f), stone);
        CreatePrimitive("ExitArchTop", PrimitiveType.Cube, parent,
            exitCenter + new Vector3(0f, 4.15f, 0f),
            new Vector3(0.55f, 0.45f, cellSize * 1.15f), stone);
        CreatePrimitive("ExitGlow", PrimitiveType.Sphere, parent,
            exitCenter + new Vector3(0f, 2.15f, 0f),
            new Vector3(1.15f, 1.15f, 1.15f), glow);

        GameObject lightObject = new GameObject("ExitGreenLight");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = exitCenter + new Vector3(0f, 2.4f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.24f, 1f, 0.62f);
        light.intensity = 3.5f;
        light.range = 12f;
    }

    Material CreateMaterial(Color color, bool emissive)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;

        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.5f);
        }

        return material;
    }

    GameObject CreatePrimitive(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(primitive);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = material;
        return obj;
    }

    void SpawnWall(GameObject parent, Vector3 pos, Vector3 scale)
    {
        GameObject wall = Instantiate(wallPrefab,
            pos, Quaternion.identity, parent.transform);
        wall.transform.localScale = scale;
    }

    void PlaceActors()
    {
        // Player at top-left cell
        if (player != null)
            player.transform.position = new Vector3(
                cellSize / 2, 2f, cellSize / 2);

        // Monster starts away from the exit, then chases the player.
        if (monster != null)
            monster.transform.position = GetMonsterStartPosition();

        // Exit trigger at the opening we carved
        if (exitTrigger != null)
        {
            exitTrigger.transform.position = new Vector3(
                width * cellSize + cellSize / 2f,
                2f,
                (height - 1) * cellSize + cellSize / 2);
            exitTrigger.transform.localScale = new Vector3(cellSize * 0.75f, 6f, cellSize * 1.25f);
        }
    }

    void Shuffle(int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }
    }

    Vector3 GetMonsterStartPosition()
    {
        int startX = Mathf.Clamp(width / 2, 2, width - 3);
        int startY = Mathf.Clamp(1, 0, height - 1);

        return new Vector3(
            startX * cellSize + cellSize / 2,
            2f,
            startY * cellSize + cellSize / 2);
    }

    void PlaceOnNavMesh(GameObject actor, Vector3 targetPosition)
    {
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, cellSize * 2f, NavMesh.AllAreas))
        {
            NavMeshAgent agent = actor.GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.Warp(hit.position);
            else
                actor.transform.position = hit.position;
        }
        else
        {
            actor.transform.position = targetPosition;
            Debug.LogWarning($"Could not find a NavMesh position near {actor.name} start point.");
        }
    }

    public bool HasHorizontalWall(int x, int y)
    {
        if (wallsH == null || x < 0 || x >= width || y < 0 || y > height)
            return true;

        return wallsH[x, y];
    }

    public bool HasVerticalWall(int x, int y)
    {
        if (wallsV == null || x < 0 || x > width || y < 0 || y >= height)
            return true;

        return wallsV[x, y];
    }
}

