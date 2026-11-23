using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScadrykCue { Sword, Dash, Pulse, Power, Warning, Impact, Hurt, Portal, Victory, Memory, LaneWarning, VolleyWarning, AreaWarning, SupportWarning, FinalBossWarning, PlayerStrike, PlayerFinisher, PlayerPowerImpact, CoastalCurrent, EchoReleased, FinalVolleyWarning, FinalStarfallWarning, FinalWaveWarning, SaltCrosswind, NimaFarewell, VerdantCrescentImpact, SaltburstImpact, TidebreakerImpact }

/// <summary>Original, lightweight synthesized motifs and combat signals; no external audio assets.</summary>
public sealed class ScadrykAudio : MonoBehaviour
{
    private const int SampleRate = 22050;
    private static ScadrykAudio instance;
    private AudioSource music, transitionMusic, combatMusic, effects;
    private Coroutine musicTransition;
    private float musicEnvelope = 1f, transitionEnvelope, combatEnvelope, combatTarget;
    private float warningMusicEnvelope = 1f, warningMusicDuckUntil;
    private const float ContextFadeDuration = 1.25f;
    private const float CombatIntensityFadeDuration = 0.85f;
    internal const float WarningMusicDuckLevel = 0.62f;
    internal const int MaxConcurrentWarningCues = 4;
    private readonly List<float> activeWarningCueEndsAt = new List<float>(5);
    private AudioClip[] biomeLoops;
    private AudioClip combatLoop;
    private AudioClip[] cueClips;
    private readonly float[] nextCueAt = new float[System.Enum.GetValues(typeof(ScadrykCue)).Length];
    internal bool HasCue(ScadrykCue cue) => cueClips != null && (int)cue >= 0 && (int)cue < cueClips.Length;
    internal string CueClipName(ScadrykCue cue) => HasCue(cue) && cueClips[(int)cue] != null ? cueClips[(int)cue].name : string.Empty;
    internal float CueDuration(ScadrykCue cue) => HasCue(cue) && cueClips[(int)cue] != null ? cueClips[(int)cue].length : 0f;
    internal bool HasCombatLayer => combatMusic != null && combatMusic.clip != null && combatMusic.clip == combatLoop;

    private void Awake()
    {
        instance = this;
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        transitionMusic = gameObject.AddComponent<AudioSource>();
        transitionMusic.loop = true;
        transitionMusic.playOnAwake = false;
        combatMusic = gameObject.AddComponent<AudioSource>();
        combatMusic.loop = true;
        combatMusic.playOnAwake = false;
        combatMusic.spatialBlend = 0f;
        combatLoop = MakeCombatLoop();
        combatMusic.clip = combatLoop;
        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        biomeLoops = new[] { MakeLoop("Roots remember", 110f, 1.0f), MakeLoop("Salt remembers", 146.83f, 0.72f), MakeLoop("Bells remember", 123.47f, 1.35f), MakeLoop("The silence breaks", 82.41f, 1.8f) };
        cueClips = new[] {
            MakeCue("Nara sword", 410f, 0.16f, 0.46f), MakeCue("Nara dash", 250f, 0.13f, 0.24f),
            MakeCue("Nara pulse", 330f, 0.32f, 0.33f), MakeCue("Nara power charge", 520f, 0.12f, 0.26f),
            MakeCue("Echo warning", 160f, 0.27f, 0.24f), MakeCue("Echo impact", 95f, 0.22f, 0.38f),
            MakeCue("Nara hurt", 135f, 0.25f, 0.4f), MakeCue("Portal passage", 290f, 0.65f, 0.25f),
            MakeCue("Voices return", 440f, 0.9f, 0.32f), MakeCue("Nima remembers", 365f, 0.48f, 0.2f),
            MakeCue("Root lane warning", 112f, 0.34f, 0.3f), MakeCue("Crystal volley warning", 690f, 0.22f, 0.22f),
            MakeCue("Echo area warning", 215f, 0.42f, 0.28f), MakeCue("Weaver support warning", 810f, 0.31f, 0.18f),
            MakeCue("Void herald warning", 146.83f, 0.58f, 0.34f), MakeCue("Nara strike confirmed", 880f, 0.1f, 0.24f),
            MakeCue("Nara finisher confirmed", 660f, 0.2f, 0.36f), MakeCue("Nara power impact", 760f, 0.18f, 0.42f),
            MakeCue("Coastal cross-current", 310f, 0.32f, 0.16f), MakeEchoReleaseCue(),
            MakeWarningMotif("Herald remembered volley", new[] { 392f, 294f, 220f }, 0.12f, 0.055f, 0.26f),
            MakeWarningMotif("Herald falling memory", new[] { 196f, 392f, 587f }, 0.16f, 0.045f, 0.24f),
            MakeWarningMotif("Herald void wave", new[] { 110f, 110f, 165f }, 0.17f, 0.075f, 0.28f),
            MakeCue("Salt grains in the crosswind", 238f, 0.42f, 0.13f), MakeNimaFarewellCue(),
            MakePowerImpactCue("Verdant crescent impact", 440f, 0),
            MakePowerImpactCue("Saltburst impact", 246.94f, 1),
            MakePowerImpactCue("Tidebreaker impact", 146.83f, 2)
        };
    }

    private void Update()
    {
        combatEnvelope = BlendCombatIntensity(combatEnvelope, combatTarget, Time.unscaledDeltaTime);
        warningMusicEnvelope = BlendWarningMusicEnvelope(warningMusicEnvelope,
            Time.unscaledTime < warningMusicDuckUntil, Time.unscaledDeltaTime);
        float musicVolume = ScadrykSettings.MusicVolume * 0.34f;
        if (music != null) music.volume = musicVolume * musicEnvelope * warningMusicEnvelope;
        if (transitionMusic != null) transitionMusic.volume = musicVolume * transitionEnvelope * warningMusicEnvelope;
        if (combatMusic != null)
        {
            combatMusic.volume = ScadrykSettings.MusicVolume * 0.11f * combatEnvelope * warningMusicEnvelope;
            if (combatTarget > 0f && !combatMusic.isPlaying) combatMusic.Play();
            else if (combatTarget <= 0f && combatEnvelope <= 0.001f && combatMusic.isPlaying) combatMusic.Stop();
        }
        if (effects != null) effects.volume = ScadrykSettings.EffectsVolume * 0.65f;
    }

    public void SetCombatIntensity(float intensity)
    {
        combatTarget = Mathf.Clamp01(intensity);
        if (combatTarget > 0f && combatMusic != null && combatMusic.clip != null && !combatMusic.isPlaying) combatMusic.Play();
    }

    internal static float CombatIntensityFor(int activeEnemies, bool bossEncounter, bool paused)
    {
        if (paused || activeEnemies <= 0) return 0f;
        if (bossEncounter) return 1f;
        return Mathf.Lerp(0.32f, 0.78f, Mathf.Clamp01((activeEnemies - 1f) / 3f));
    }

    internal static float BlendCombatIntensity(float current, float target, float deltaTime) =>
        Mathf.MoveTowards(current, target, Mathf.Max(0f, deltaTime) / CombatIntensityFadeDuration);

    internal static float BlendWarningMusicEnvelope(float current, bool warningActive, float deltaTime) =>
        Mathf.MoveTowards(current, warningActive ? WarningMusicDuckLevel : 1f,
            Mathf.Max(0f, deltaTime) / (warningActive ? 0.12f : 0.48f));

    internal static float WarningCueVolumeScale(int overlappingWarnings) => 0.8f / Mathf.Max(1, overlappingWarnings + 1);
    internal static bool CanPlayWarningCue(int overlappingWarnings) => overlappingWarnings < MaxConcurrentWarningCues;
    internal static ScadrykCue AbilityImpactCue(int ability) => Mathf.Clamp(ability, 0, 2) switch
    {
        0 => ScadrykCue.VerdantCrescentImpact,
        1 => ScadrykCue.SaltburstImpact,
        _ => ScadrykCue.TidebreakerImpact
    };

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
        float volumeScale = 1f;
        if (IsWarningCue(cue))
        {
            float now = Time.unscaledTime;
            for (int active = instance.activeWarningCueEndsAt.Count - 1; active >= 0; active--)
                if (instance.activeWarningCueEndsAt[active] <= now) instance.activeWarningCueEndsAt.RemoveAt(active);
            int overlappingWarnings = instance.activeWarningCueEndsAt.Count;
            if (!CanPlayWarningCue(overlappingWarnings)) return;
            volumeScale = WarningCueVolumeScale(overlappingWarnings);
            instance.activeWarningCueEndsAt.Add(now + instance.cueClips[index].length);
            instance.warningMusicDuckUntil = Mathf.Max(instance.warningMusicDuckUntil,
                now + instance.cueClips[index].length + 0.12f);
        }
        instance.nextCueAt[index] = Time.unscaledTime + (cue == ScadrykCue.Warning || cue == ScadrykCue.Impact || IsWarningCue(cue) ? 0.13f : 0.04f);
        instance.effects.PlayOneShot(instance.cueClips[index], volumeScale);
    }

    private static bool IsWarningCue(ScadrykCue cue) => cue is ScadrykCue.LaneWarning or ScadrykCue.VolleyWarning or
        ScadrykCue.AreaWarning or ScadrykCue.SupportWarning or ScadrykCue.FinalBossWarning or
        ScadrykCue.FinalVolleyWarning or ScadrykCue.FinalStarfallWarning or ScadrykCue.FinalWaveWarning;

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

    private static AudioClip MakeCombatLoop()
    {
        const float duration = 8f;
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float beat = t % 0.5f;
            int beatInBar = Mathf.FloorToInt(t / 0.5f) % 4;
            float accent = beatInBar == 0 ? 1f : 0.62f;
            float edge = Mathf.Min(1f, Mathf.Min(t, duration - t) * 8f);
            float bass = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.055f +
                Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.025f;
            float pulse = Mathf.Sin(2f * Mathf.PI * 82.41f * beat) * Mathf.Exp(-beat * 13f) * 0.105f * accent;
            float chime = Mathf.Sin(2f * Mathf.PI * 293.66f * t) * Mathf.Exp(-beat * 5f) * 0.035f;
            data[i] = (bass + pulse + chime) * edge;
        }
        AudioClip clip = AudioClip.Create("Scadryk combat undercurrent", count, 1, SampleRate, false);
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

    private static AudioClip MakePowerImpactCue(string name, float root, int character)
    {
        float duration = character == 0 ? 0.25f : character == 1 ? 0.32f : 0.38f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float progress = Mathf.Clamp01(t / duration);
            float sample;
            if (character == 0)
            {
                float sweep = root * Mathf.Lerp(2.25f, 0.72f, progress);
                float edge = Mathf.Sin(Mathf.PI * progress) * Mathf.Exp(-t * 2.4f);
                sample = (Mathf.Sin(2f * Mathf.PI * sweep * t) + 0.24f * Mathf.Sin(2f * Mathf.PI * sweep * 2.03f * t)) * edge * 0.25f;
            }
            else if (character == 1)
            {
                float crack = Mathf.Sin(2f * Mathf.PI * root * 6.7f * t) * Mathf.Exp(-t * 32f) * 0.19f;
                float crystal = Mathf.Sin(2f * Mathf.PI * root * 2.52f * t) * Mathf.Exp(-t * 8f) +
                    0.36f * Mathf.Sin(2f * Mathf.PI * root * 4.03f * t) * Mathf.Exp(-t * 11f);
                sample = crack + crystal * 0.18f * Mathf.Sin(Mathf.PI * progress);
            }
            else
            {
                float swell = Mathf.Sin(Mathf.PI * Mathf.Min(1f, progress * 1.35f));
                float low = Mathf.Sin(2f * Mathf.PI * root * 0.72f * t) * Mathf.Exp(-t * 4f);
                float wash = Mathf.Sin(2f * Mathf.PI * root * (1.6f - progress * 0.55f) * t) * Mathf.Exp(-t * 3.2f);
                sample = (low * 0.28f + wash * 0.12f) * swell;
            }
            data[i] = sample;
        }
        AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeEchoReleaseCue()
    {
        const float firstNoteDuration = 0.22f;
        const float duration = 0.62f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            bool secondNote = t >= firstNoteDuration;
            float noteTime = secondNote ? t - firstNoteDuration : t;
            float noteDuration = secondNote ? duration - firstNoteDuration : firstNoteDuration;
            float frequency = secondNote ? 659.25f : 523.25f;
            float envelope = Mathf.Sin(Mathf.PI * noteTime / noteDuration) * Mathf.Exp(-noteTime * 3.2f);
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * noteTime) +
                0.22f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * noteTime);
            data[i] = tone * envelope * 0.2f;
        }
        AudioClip clip = AudioClip.Create("Liberated echo — rising bell", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeNimaFarewellCue()
    {
        const float noteDuration = 0.24f;
        const float gap = 0.055f;
        float[] notes = { 659.25f, 523.25f, 392f };
        int count = Mathf.CeilToInt(SampleRate * (notes.Length * noteDuration + (notes.Length - 1) * gap));
        float[] data = new float[count];
        float step = noteDuration + gap;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            int note = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / step));
            float noteTime = t - note * step;
            if (noteTime >= noteDuration) continue;
            float envelope = Mathf.Sin(Mathf.PI * noteTime / noteDuration) * Mathf.Exp(-noteTime * 2.8f);
            float tone = Mathf.Sin(2f * Mathf.PI * notes[note] * noteTime) +
                0.18f * Mathf.Sin(2f * Mathf.PI * notes[note] * 2f * noteTime);
            data[i] = tone * envelope * 0.16f;
        }
        AudioClip clip = AudioClip.Create("Nima — the last crossing", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeWarningMotif(string name, float[] frequencies, float noteDuration, float gap, float strength)
    {
        int noteCount = frequencies != null ? frequencies.Length : 0;
        if (noteCount == 0) return MakeCue(name, 146.83f, 0.28f, strength);
        int count = Mathf.CeilToInt(SampleRate * (noteCount * noteDuration + (noteCount - 1) * gap));
        float[] data = new float[count];
        float step = noteDuration + gap;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            int note = Mathf.Min(noteCount - 1, Mathf.FloorToInt(t / step));
            float noteTime = t - note * step;
            if (noteTime >= noteDuration) continue;
            float envelope = Mathf.Sin(Mathf.PI * noteTime / noteDuration) * Mathf.Exp(-noteTime * 3.5f);
            float frequency = frequencies[note];
            data[i] = (Mathf.Sin(2f * Mathf.PI * frequency * noteTime) +
                0.24f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * noteTime)) * envelope * strength;
        }
        AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
