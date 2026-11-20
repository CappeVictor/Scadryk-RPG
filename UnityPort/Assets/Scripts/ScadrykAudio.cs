using System.Collections;
using UnityEngine;

public enum ScadrykCue { Sword, Dash, Pulse, Power, Warning, Impact, Hurt, Portal, Victory, Memory, LaneWarning, VolleyWarning, AreaWarning, SupportWarning, FinalBossWarning, PlayerStrike }

/// <summary>Original, lightweight synthesized motifs and combat signals; no external audio assets.</summary>
public sealed class ScadrykAudio : MonoBehaviour
{
    private const int SampleRate = 22050;
    private static ScadrykAudio instance;
    private AudioSource music, transitionMusic, effects;
    private Coroutine musicTransition;
    private float musicEnvelope = 1f, transitionEnvelope;
    private const float ContextFadeDuration = 1.25f;
    private AudioClip[] biomeLoops;
    private AudioClip[] cueClips;
    private readonly float[] nextCueAt = new float[System.Enum.GetValues(typeof(ScadrykCue)).Length];
    internal bool HasCue(ScadrykCue cue) => cueClips != null && (int)cue >= 0 && (int)cue < cueClips.Length;

    private void Awake()
    {
        instance = this;
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        transitionMusic = gameObject.AddComponent<AudioSource>();
        transitionMusic.loop = true;
        transitionMusic.playOnAwake = false;
        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        biomeLoops = new[] { MakeLoop("Roots remember", 110f, 1.0f), MakeLoop("Salt remembers", 146.83f, 0.72f), MakeLoop("Bells remember", 123.47f, 1.35f), MakeLoop("The silence breaks", 82.41f, 1.8f) };
        cueClips = new[] {
            MakeCue("Nara sword", 410f, 0.16f, 0.46f), MakeCue("Nara dash", 250f, 0.13f, 0.24f),
            MakeCue("Nara pulse", 330f, 0.32f, 0.33f), MakeCue("Nara power", 520f, 0.45f, 0.38f),
            MakeCue("Echo warning", 160f, 0.27f, 0.24f), MakeCue("Echo impact", 95f, 0.22f, 0.38f),
            MakeCue("Nara hurt", 135f, 0.25f, 0.4f), MakeCue("Portal passage", 290f, 0.65f, 0.25f),
            MakeCue("Voices return", 440f, 0.9f, 0.32f), MakeCue("Nima remembers", 365f, 0.48f, 0.2f),
            MakeCue("Root lane warning", 112f, 0.34f, 0.3f), MakeCue("Crystal volley warning", 690f, 0.22f, 0.22f),
            MakeCue("Echo area warning", 215f, 0.42f, 0.28f), MakeCue("Weaver support warning", 810f, 0.31f, 0.18f),
            MakeCue("Void herald warning", 146.83f, 0.58f, 0.34f), MakeCue("Nara strike confirmed", 880f, 0.1f, 0.24f)
        };
    }

    private void Update()
    {
        float musicVolume = ScadrykSettings.MusicVolume * 0.34f;
        if (music != null) music.volume = musicVolume * musicEnvelope;
        if (transitionMusic != null) transitionMusic.volume = musicVolume * transitionEnvelope;
        if (effects != null) effects.volume = ScadrykSettings.EffectsVolume * 0.65f;
    }

    public void SetContext(int biome, bool finalBoss)
    {
        int index = finalBoss ? 3 : Mathf.Clamp(biome, 0, 2);
        AudioClip target = biomeLoops[index];
        if (musicTransition != null)
        {
            StopCoroutine(musicTransition);
            musicTransition = null;
            if (transitionEnvelope > musicEnvelope)
            {
                AudioSource previous = music;
                music = transitionMusic;
                transitionMusic = previous;
                transitionMusic.Stop();
                musicEnvelope = 1f;
            }
            else
            {
                transitionMusic.Stop();
                musicEnvelope = 1f;
            }
            transitionEnvelope = 0f;
        }

        if (music.clip == target)
        {
            musicEnvelope = 1f;
            return;
        }

        if (music.clip == null)
        {
            music.clip = target;
            musicEnvelope = 1f;
            music.Play();
            return;
        }

        transitionMusic.clip = target;
        transitionMusic.Play();
        transitionEnvelope = 0f;
        musicTransition = StartCoroutine(CrossfadeMusic());
    }

    private IEnumerator CrossfadeMusic()
    {
        float elapsed = 0f;
        while (elapsed < ContextFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ContextFadeDuration));
            musicEnvelope = Mathf.Cos(blend * Mathf.PI * 0.5f);
            transitionEnvelope = Mathf.Sin(blend * Mathf.PI * 0.5f);
            yield return null;
        }

        music.Stop();
        AudioSource previous = music;
        music = transitionMusic;
        transitionMusic = previous;
        musicEnvelope = 1f;
        transitionEnvelope = 0f;
        musicTransition = null;
    }

    public static void Play(ScadrykCue cue)
    {
        if (instance == null || instance.effects == null) return;
        int index = (int)cue;
        if (index < 0 || index >= instance.cueClips.Length || Time.unscaledTime < instance.nextCueAt[index]) return;
        instance.nextCueAt[index] = Time.unscaledTime + (cue == ScadrykCue.Warning || cue == ScadrykCue.Impact || IsWarningCue(cue) ? 0.13f : 0.04f);
        instance.effects.PlayOneShot(instance.cueClips[index]);
    }

    private static bool IsWarningCue(ScadrykCue cue) => cue is ScadrykCue.LaneWarning or ScadrykCue.VolleyWarning or
        ScadrykCue.AreaWarning or ScadrykCue.SupportWarning or ScadrykCue.FinalBossWarning;

    private static AudioClip MakeLoop(string name, float root, float character)
    {
        int count = SampleRate * 8;
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float edge = Mathf.Min(1f, Mathf.Min(t, 8f - t) * 6f);
            float pad = Mathf.Sin(2f * Mathf.PI * root * t) * 0.11f + Mathf.Sin(2f * Mathf.PI * root * 1.5f * t) * 0.065f;
            float beat = t % 2f;
            float bellAttack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(beat / 0.018f));
            float bell = Mathf.Sin(2f * Mathf.PI * root * (character + 1.5f) * t) * bellAttack * Mathf.Exp(-beat * 4f) * 0.08f;
            data[i] = (pad * (0.8f + 0.2f * Mathf.Sin(t * 0.8f)) + bell) * edge;
        }
        AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeCue(string name, float frequency, float duration, float strength)
    {
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float envelope = Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-t * 3f);
            float sweep = frequency * (1f + 0.28f * (1f - t / duration));
            data[i] = (Mathf.Sin(2f * Mathf.PI * sweep * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * sweep * 2.01f * t)) * envelope * strength;
        }
        AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
