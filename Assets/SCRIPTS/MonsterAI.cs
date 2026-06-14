using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public float chaseSpeed = 1.5f;
    public float catchDistance = 1.2f;
    public float chaseDelay = 5f;
    public string stoneMonsterResourcePath = "StoneMonster/Stonefbx";

    private NavMeshAgent agent;
    private Transform monsterVisual;
    private Vector3 visualBaseLocalPosition;
    private bool ready = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("MonsterAI needs a NavMeshAgent on the same GameObject.");
            enabled = false;
            return;
        }

        agent.speed = chaseSpeed;
        agent.acceleration = 3f;
        agent.angularSpeed = 80f;
        agent.radius = 0.65f;
        agent.height = 2.2f;
        agent.stoppingDistance = 0.35f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        BuildStoneMonsterVisual();
        ResolvePlayer();
        Invoke(nameof(StartChasing), chaseDelay);
    }

    void StartChasing()
    {
        ready = true;
    }

    void Update()
    {
        AnimateMonster();

        if (!ready) return;
        if (player == null)
            ResolvePlayer();
        if (player == null) return;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
                return;
        }

        if (agent.isOnNavMesh)
            agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < catchDistance)
        {
            CaughtPanel caughtPanel = FindFirstObjectByType<CaughtPanel>();
            if (caughtPanel != null)
                caughtPanel.ShowCaught();
        }
    }

    void ResolvePlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    void AnimateMonster()
    {
        if (monsterVisual == null)
            return;

        float moveAmount = agent != null ? agent.velocity.magnitude : 0f;
        if (moveAmount < 0.05f)
        {
            monsterVisual.localPosition = Vector3.Lerp(monsterVisual.localPosition, visualBaseLocalPosition, Time.deltaTime * 6f);
            monsterVisual.localRotation = Quaternion.Lerp(monsterVisual.localRotation, Quaternion.identity, Time.deltaTime * 6f);
            return;
        }

        float step = Time.time * 5.5f;
        float sideStep = Mathf.Sin(step) * 0.018f;
        float heavyLift = Mathf.Abs(Mathf.Sin(step)) * 0.012f;
        float bodyTilt = Mathf.Sin(step) * 4f;
        float bodyTurn = Mathf.Sin(step * 0.5f) * 2f;

        monsterVisual.localPosition = visualBaseLocalPosition + new Vector3(sideStep, heavyLift, 0f);
        monsterVisual.localRotation = Quaternion.Euler(0f, bodyTurn, bodyTilt);
    }

    void BuildStoneMonsterVisual()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        GameObject modelPrefab = Resources.Load<GameObject>(stoneMonsterResourcePath);
        if (modelPrefab == null)
        {
            Debug.LogWarning($"Stone monster model not found at Resources/{stoneMonsterResourcePath}. Using temporary stone shape.");
            BuildFallbackStoneMonster();
            return;
        }

        GameObject visual = Instantiate(modelPrefab, transform);
        visual.name = "StoneMonsterVisual";
        monsterVisual = visual.transform;
        FitVisualToMonster(monsterVisual);
        ApplyStoneMaterial(monsterVisual);
    }

    void FitVisualToMonster(Transform visual)
    {
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxSize > 0f)
            visual.localScale = Vector3.one * (1.7f / maxSize);

        // Recalculate after scaling, then center the model on the monster object.
        renderers = visual.GetComponentsInChildren<Renderer>();
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 groundCenter = new Vector3(transform.position.x, bounds.min.y, transform.position.z);
        Vector3 worldOffset = transform.position - groundCenter;
        visual.position += worldOffset;
        visualBaseLocalPosition = visual.localPosition;
    }

    void ApplyStoneMaterial(Transform visual)
    {
        Texture2D diffuse = Resources.Load<Texture2D>("StoneMonster/diffuso");
        Texture2D normal = Resources.Load<Texture2D>("StoneMonster/normal");
        Texture2D roughness = Resources.Load<Texture2D>("StoneMonster/rough");

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = Color.white;

        if (diffuse != null)
            material.mainTexture = diffuse;

        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }

        if (roughness != null)
            material.SetTexture("_MetallicGlossMap", roughness);

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = true;
            renderer.material = material;
        }
    }

    void BuildFallbackStoneMonster()
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = new Color(0.42f, 0.45f, 0.42f);

        GameObject root = new GameObject("FallbackStoneMonsterVisual");
        root.transform.SetParent(transform, false);
        monsterVisual = root.transform;

        CreateRockPart(root.transform, new Vector3(0f, 1.1f, 0f), new Vector3(1.1f, 1.5f, 0.8f), material);
        CreateRockPart(root.transform, new Vector3(0f, 2f, 0f), new Vector3(0.85f, 0.75f, 0.65f), material);
        CreateRockPart(root.transform, new Vector3(-0.7f, 1.1f, 0f), new Vector3(0.35f, 0.9f, 0.35f), material);
        CreateRockPart(root.transform, new Vector3(0.7f, 1.1f, 0f), new Vector3(0.35f, 0.9f, 0.35f), material);
    }

    void CreateRockPart(Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Random.rotation;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().material = material;
        Destroy(part.GetComponent<Collider>());
    }
}

