using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CrystalInteract : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;
    public GameObject colorWheelUI;
    public GameObject playerFollowCamera;
    public Vector3 panelOffset = new Vector3(0f, 2f, 0f);

    [Header("Crystal Target")]
    public Renderer targetRenderer;
    public Light targetLight;

    [Header("Orb Settings")]
    public float orbRadius = 60f;
    public float orbSize = 40f;

    [Header("Wrong Color Feedback")]
    public float conflictPushDistance = 3f;
    public float conflictPushDuration = 0.35f;
    public float conflictSoundVolume = 1f;
    public float conflictTimePenalty = 5f;
    public bool spawnStormBurst = true;
    public Color stormBurstColor = new Color(0.72f, 0.86f, 1f, 0.72f);
    public bool playGeneratedConflictSound = true;

    private readonly Color[] colors = {
        new Color(0.2f, 0.6f, 1f),
        new Color(0.2f, 0.9f, 0.3f),
        new Color(1f, 0.2f, 0.2f),
        new Color(0.8f, 0.2f, 1f)
    };

    private readonly string[] colorNames = { "Blue", "Green", "Red", "Purple" };

    private static readonly List<CrystalInteract> allCrystals = new List<CrystalInteract>();
    private static CrystalInteract wheelOwner;

    public event Action<CrystalInteract> ColorChanged;

    private bool playerNearby;
    private bool wheelOpen;
    private int selectedColorIndex = -1;
    private Renderer crystalRenderer;
    private Light crystalLight;
    private Transform player;
    private CameraFollow disabledCameraFollow;
    private Color originalRendererColor = Color.white;
    private Color originalBaseColor = Color.white;
    private Color originalEmissionColor = Color.black;
    private Color originalLightColor = Color.white;
    private bool hasOriginalRendererColor;
    private bool hasOriginalBaseColor;
    private bool hasOriginalEmissionColor;
    private static AudioClip conflictClip;
    private static Material stormBurstMaterial;

    public bool HasColor
    {
        get { return selectedColorIndex >= 0; }
    }

    public int SelectedColorIndex
    {
        get { return selectedColorIndex; }
    }

    public Color SelectedColor
    {
        get
        {
            if (!HasColor)
                return Color.white;

            return colors[selectedColorIndex];
        }
    }

    public Vector3 LineAnchorPosition
    {
        get { return InteractionPosition; }
    }

    private Vector3 InteractionPosition
    {
        get
        {
            if (crystalRenderer != null)
                return crystalRenderer.bounds.center;

            return transform.position;
        }
    }

    private void OnEnable()
    {
        if (!allCrystals.Contains(this))
            allCrystals.Add(this);
    }

    private void OnDisable()
    {
        allCrystals.Remove(this);

        if (wheelOwner == this)
            CloseWheel();
    }

    private void Start()
    {
        crystalRenderer = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
        if (crystalRenderer == null)
            crystalRenderer = GetComponentInChildren<Renderer>();

        crystalLight = targetLight != null ? targetLight : GetComponent<Light>();
        if (crystalLight == null)
            crystalLight = GetComponentInChildren<Light>();

        CacheOriginalColors();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (colorWheelUI == null)
            colorWheelUI = GameObject.Find("ColorWheelUI");

        if (playerFollowCamera == null)
            playerFollowCamera = GameObject.Find("PlayerFollowCamera");

        if (colorWheelUI != null)
        {
            Canvas canvas = colorWheelUI.GetComponentInParent<Canvas>();
            if (canvas != null)
                ConfigureColorWheelCanvas(canvas);

            colorWheelUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null || Keyboard.current == null)
            return;

        playerNearby = Vector3.Distance(InteractionPosition, player.position) <= interactDistance;

        if (wheelOpen && wheelOwner == this)
        {
            FreezeLookInput();

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                CloseWheel();
                return;
            }

            if (!playerNearby)
            {
                CloseWheel();
                return;
            }

            return;
        }

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        CrystalInteract nearestCrystal = FindNearestCrystalToPlayer();
        if (nearestCrystal != this)
            return;

        if (wheelOwner != null && wheelOwner != this)
            wheelOwner.CloseWheel();

        if (wheelOpen)
            CloseWheel();
        else
            OpenWheel();
    }

    private static CrystalInteract FindNearestCrystalToPlayer()
    {
        Transform playerTransform = null;
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        if (playerTransform == null)
            return null;

        CrystalInteract nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (CrystalInteract crystal in allCrystals)
        {
            if (crystal == null || !crystal.isActiveAndEnabled)
                continue;

            float distance = Vector3.Distance(crystal.InteractionPosition, playerTransform.position);
            if (distance > crystal.interactDistance || distance >= nearestDistance)
                continue;

            nearest = crystal;
            nearestDistance = distance;
        }

        return nearest;
    }

    private void BuildOrbUI()
    {
        if (colorWheelUI == null)
            return;

        foreach (Transform child in colorWheelUI.transform)
            Destroy(child.gameObject);

        for (int i = 0; i < colors.Length; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector2 orbPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbRadius;

            GameObject orb = new GameObject("Orb_" + colorNames[i]);
            orb.transform.SetParent(colorWheelUI.transform, false);

            Image orbBg = orb.AddComponent<Image>();
            orbBg.color = new Color(0.08f, 0.08f, 0.15f, 0.9f);

            RectTransform rt = orb.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(orbSize, orbSize);
            rt.anchoredPosition = orbPosition;

            GameObject inner = new GameObject("Inner");
            inner.transform.SetParent(orb.transform, false);

            Image innerImg = inner.AddComponent<Image>();
            innerImg.color = colors[i];

            RectTransform innerRt = inner.GetComponent<RectTransform>();
            innerRt.sizeDelta = new Vector2(orbSize * 0.7f, orbSize * 0.7f);
            innerRt.anchoredPosition = Vector2.zero;

            Button btn = orb.AddComponent<Button>();
            int colorIndex = i;
            btn.onClick.AddListener(() => SelectColor(colorIndex));
            btn.targetGraphic = orbBg;
        }
    }

    private void OpenWheel()
    {
        wheelOpen = true;
        wheelOwner = this;

        BuildOrbUI();

        if (colorWheelUI != null)
        {
            Canvas canvas = colorWheelUI.GetComponentInParent<Canvas>();
            if (canvas != null)
                HideMapImageInColorWheelCanvas(canvas);

            PositionColorWheelOnScreen();
            colorWheelUI.SetActive(true);
        }

        FreezePlayerForColorPick();
    }

    public void CloseWheel()
    {
        bool wasOwner = wheelOwner == this;

        wheelOpen = false;

        if (wasOwner)
            wheelOwner = null;

        if (wasOwner && colorWheelUI != null)
            colorWheelUI.SetActive(false);

        if (wasOwner)
            ReleasePlayerAfterColorPick();
    }

    public void SelectColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= colors.Length)
            return;

        Color chosenColor = colors[colorIndex];
        selectedColorIndex = colorIndex;

        if (crystalLight != null)
            crystalLight.color = chosenColor;

        if (crystalRenderer != null)
        {
            Material material = crystalRenderer.material;
            material.color = chosenColor;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", chosenColor);

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", chosenColor * 2f);
        }

        Debug.Log(gameObject.name + " colored: " + colorNames[colorIndex]);
        ColorChanged?.Invoke(this);

        CrystalConnectionManager manager = FindFirstObjectByType<CrystalConnectionManager>();
        bool createdConflict = manager != null && manager.HasConflictFor(this);

        CloseWheel();

        if (createdConflict)
            PlayConflictFeedback();
    }

    private void ConfigureColorWheelCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;
        canvas.worldCamera = null;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        HideMapImageInColorWheelCanvas(canvas);
    }

    private void HideMapImageInColorWheelCanvas(Canvas canvas)
    {
        Transform mapImage = canvas.transform.Find("MapImage");
        if (mapImage != null)
            mapImage.gameObject.SetActive(false);
    }

    private void PositionColorWheelOnScreen()
    {
        RectTransform rect = colorWheelUI.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        rect.sizeDelta = new Vector2(260f, 260f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    public void ClearColor()
    {
        selectedColorIndex = -1;

        if (crystalLight != null)
            crystalLight.color = originalLightColor;

        if (crystalRenderer != null)
        {
            Material material = crystalRenderer.material;

            if (hasOriginalRendererColor)
                material.color = originalRendererColor;

            if (hasOriginalBaseColor && material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", originalBaseColor);

            if (hasOriginalEmissionColor && material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", originalEmissionColor);
        }

        ColorChanged?.Invoke(this);
    }

    private void CacheOriginalColors()
    {
        if (crystalLight != null)
            originalLightColor = crystalLight.color;

        if (crystalRenderer == null || crystalRenderer.sharedMaterial == null)
            return;

        Material material = crystalRenderer.sharedMaterial;
        originalRendererColor = material.color;
        hasOriginalRendererColor = true;

        if (material.HasProperty("_BaseColor"))
        {
            originalBaseColor = material.GetColor("_BaseColor");
            hasOriginalBaseColor = true;
        }

        if (material.HasProperty("_EmissionColor"))
        {
            originalEmissionColor = material.GetColor("_EmissionColor");
            hasOriginalEmissionColor = true;
        }
    }

    private void PlayConflictFeedback()
    {
        if (playGeneratedConflictSound)
            PlayConflictSound();

        if (spawnStormBurst)
            SpawnStormBurst();

        if (player != null)
            StartCoroutine(PushPlayerAwayFromCrystal());

        PuzzleTimer timer = FindFirstObjectByType<PuzzleTimer>();
        if (timer != null)
            timer.ApplyMistakePenalty(conflictTimePenalty);
    }

    private void SpawnStormBurst()
    {
        GameObject stormObject = new GameObject("Crystal_Storm_Burst");
        stormObject.transform.position = InteractionPosition + Vector3.up * 0.25f;

        ParticleSystem particles = stormObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.75f;
        main.loop = false;
        main.maxParticles = 90;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.68f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = stormBurstColor;
        main.gravityModifier = 0.02f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 42),
            new ParticleSystem.Burst(0.1f, 18)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.85f;
        shape.arc = 360f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.85f;
        noise.frequency = 1.7f;
        noise.scrollSpeed = 1.2f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.95f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.35f, 0.62f, 0.9f), 0.45f),
                new GradientColorKey(new Color(0.78f, 0.68f, 0.42f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.55f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetStormBurstMaterial();

        particles.Play();
        Destroy(stormObject, 1.4f);
    }

    private Material GetStormBurstMaterial()
    {
        if (stormBurstMaterial != null)
            return stormBurstMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        stormBurstMaterial = new Material(shader);
        stormBurstMaterial.name = "Generated Storm Burst Particle";

        if (stormBurstMaterial.HasProperty("_BaseColor"))
            stormBurstMaterial.SetColor("_BaseColor", Color.white);
        if (stormBurstMaterial.HasProperty("_Color"))
            stormBurstMaterial.SetColor("_Color", Color.white);

        return stormBurstMaterial;
    }

    private IEnumerator PushPlayerAwayFromCrystal()
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        Vector3 direction = player.position - InteractionPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = -player.forward;

        direction.Normalize();

        float elapsed = 0f;
        while (elapsed < conflictPushDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            float step = (conflictPushDistance / conflictPushDuration) * deltaTime;

            if (controller != null && controller.enabled)
                controller.Move(direction * step);
            else
                player.position += direction * step;

            elapsed += deltaTime;
            yield return null;
        }
    }

    private void PlayConflictSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1f;
        audioSource.volume = conflictSoundVolume;

        if (conflictClip == null)
            conflictClip = CreateConflictClip();

        audioSource.PlayOneShot(conflictClip, conflictSoundVolume);
    }

    private AudioClip CreateConflictClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.45f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float normalized = t / duration;
            float attack = Mathf.Clamp01(normalized * 18f);
            float release = Mathf.Clamp01((1f - normalized) * 2.2f);
            float envelope = attack * release;
            float thunderCrack = (Mathf.PerlinNoise(t * 950f, 0.17f) - 0.5f) * Mathf.Exp(-normalized * 28f) * 1.35f;
            float windRoar = (Mathf.PerlinNoise(t * 115f, 0.27f) - 0.5f) * Mathf.Exp(-normalized * 1.15f) * 0.85f;
            float waveCrash = (Mathf.PerlinNoise(t * 42f, 0.73f) - 0.5f) * Mathf.Exp(-normalized * 2.1f) * 0.5f;
            float lowThunder = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(64f, 24f, normalized) * t) * Mathf.Exp(-normalized * 1.6f) * 0.72f;
            float templeBoom = Mathf.Sin(2f * Mathf.PI * 92f * t) * Mathf.Exp(-normalized * 9f) * 0.46f;
            float stoneClack = Mathf.Sin(2f * Mathf.PI * 310f * t) * Mathf.Exp(-normalized * 18f) * 0.24f;

            samples[i] = Mathf.Clamp((thunderCrack + windRoar + waveCrash + lowThunder + templeBoom + stoneClack) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Temple Storm Mistake Hit", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void FreezePlayerForColorPick()
    {
        FreezeLookInput();

        var tpc = FindFirstObjectByType<StarterAssets.ThirdPersonController>();
        if (tpc != null)
            tpc.enabled = false;

        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
            playerInput.DeactivateInput();

        CameraFollow cameraFollow = null;
        if (playerFollowCamera != null)
            cameraFollow = playerFollowCamera.GetComponent<CameraFollow>();

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (cameraFollow != null && cameraFollow.enabled)
        {
            disabledCameraFollow = cameraFollow;
            disabledCameraFollow.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void FreezeLookInput()
    {
        var starterInput = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
        if (starterInput == null)
            return;

        starterInput.look = Vector2.zero;
        starterInput.cursorInputForLook = false;
    }

    private void ReleasePlayerAfterColorPick()
    {
        var starterInput = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
        if (starterInput != null)
            starterInput.cursorInputForLook = true;

        var tpc = FindFirstObjectByType<StarterAssets.ThirdPersonController>();
        if (tpc != null)
            tpc.enabled = true;

        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
            playerInput.ActivateInput();

        if (disabledCameraFollow != null)
        {
            disabledCameraFollow.enabled = true;
            disabledCameraFollow = null;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnGUI()
    {
        if (!playerNearby || wheelOpen || wheelOwner != null || Camera.main == null)
            return;

        CrystalInteract nearestCrystal = FindNearestCrystalToPlayer();
        if (nearestCrystal != this)
            return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(InteractionPosition + panelOffset);
        if (screenPos.z <= 0f)
            return;

        GUIStyle style = new GUIStyle
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(screenPos.x - 60f, Screen.height - screenPos.y - 20f, 120f, 30f), "[ E ] Interact", style);
    }
}
