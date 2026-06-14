using UnityEngine;

public class IslandBackgroundMusic : MonoBehaviour
{
    [Header("Island Music")]
    public bool playOnStart = true;
    public AudioClip musicClip;
    public string resourcesClipPath = "Audio/ocean_trader_market";
    [Range(0f, 1f)] public float volume = 0.15f;
    public float loopLength = 12f;
    public float tempo = 112f;

    private AudioSource audioSource;
    private AudioClip generatedClip;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;

        if (musicClip == null && !string.IsNullOrWhiteSpace(resourcesClipPath))
            musicClip = Resources.Load<AudioClip>(resourcesClipPath);

        if (musicClip != null)
            audioSource.clip = musicClip;
        else
        {
            generatedClip = CreateIslandLoop();
            audioSource.clip = generatedClip;
        }

        if (playOnStart)
            audioSource.Play();
    }

    private AudioClip CreateIslandLoop()
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * loopLength);
        float[] samples = new float[sampleCount];

        float beatLength = 60f / Mathf.Max(1f, tempo);
        float[] melody = { 523.25f, 659.25f, 783.99f, 880f, 783.99f, 659.25f, 587.33f, 659.25f };
        float[] harmony = { 261.63f, 329.63f, 392f, 349.23f };
        float[] bass = { 130.81f, 164.81f, 196f, 174.61f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float beat = t / beatLength;
            float halfBeat = beat * 2f;
            float barPosition = beat % 4f;

            int melodyIndex = Mathf.FloorToInt(halfBeat) % melody.Length;
            float melodyLocal = halfBeat % 1f;
            float melodyEnvelope = Mathf.Exp(-melodyLocal * 6.8f) * SmoothPulse(melodyLocal);
            float melodyTone = CasualIslandPluck(melody[melodyIndex], t) * melodyEnvelope * 0.19f;

            int harmonyIndex = Mathf.FloorToInt(beat / 2f) % harmony.Length;
            float harmonyTone = SoftSine(harmony[harmonyIndex], t) * 0.035f;
            harmonyTone += SoftSine(harmony[harmonyIndex] * 1.5f, t) * 0.025f;

            int bassIndex = Mathf.FloorToInt(beat / 2f) % bass.Length;
            float bassLocal = (beat / 2f) % 1f;
            float bassEnvelope = Mathf.Exp(-bassLocal * 3.2f);
            float bassTone = SoftSine(bass[bassIndex], t) * bassEnvelope * 0.07f;

            float oceanBreeze = (Mathf.PerlinNoise(t * 0.5f, 0.21f) - 0.5f) * 0.045f;
            oceanBreeze += SoftSine(0.18f, t) * 0.018f;

            float shakerPulse = Mathf.Abs(Mathf.Sin(2f * Mathf.PI * beat * 2f));
            float shakerEnvelope = Mathf.Pow(shakerPulse, 24f);
            float sandShaker = (Mathf.PerlinNoise(t * 230f, 0.63f) - 0.5f) * shakerEnvelope * 0.06f;

            float softKickPoint = Mathf.Min(barPosition, Mathf.Abs(barPosition - 2f));
            float softKickEnvelope = Mathf.Exp(-softKickPoint * 18f);
            float softHandDrum = SoftSine(Mathf.Lerp(74f, 46f, Mathf.Clamp01(softKickPoint)), t) * softKickEnvelope * 0.055f;

            float palmClickPoint = Mathf.Abs((beat % 1f) - 0.5f);
            float woodClick = (Mathf.PerlinNoise(t * 470f, 0.91f) - 0.5f) * Mathf.Exp(-palmClickPoint * 38f) * 0.045f;

            samples[i] = Mathf.Clamp(melodyTone + harmonyTone + bassTone + oceanBreeze + sandShaker + softHandDrum + woodClick, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Casual Island Game Loop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private float CasualIslandPluck(float frequency, float time)
    {
        float main = SoftSine(frequency, time);
        float bell = SoftSine(frequency * 2.01f, time) * 0.22f;
        float wood = SoftSine(frequency * 3.02f, time) * 0.08f;
        return main + bell + wood;
    }

    private float SoftSine(float frequency, float time)
    {
        return Mathf.Sin(2f * Mathf.PI * frequency * time);
    }

    private float SmoothPulse(float value)
    {
        return Mathf.Clamp01(value * 12f) * Mathf.Clamp01((1f - value) * 8f);
    }
}
