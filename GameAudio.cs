using System.Media;
using System.Text.Json;

internal static class GameAudio
{
    internal enum Cue { Strike, Dash, Impact, Skill, Loot, BossWarning, EliteWarning }

    private const int SampleRate = 22050;
    private static MemoryStream[] streams = Array.Empty<MemoryStream>();
    private static SoundPlayer[] players = Array.Empty<SoundPlayer>();
    private static MemoryStream[] soundscapeStreams = Array.Empty<MemoryStream>();
    private static SoundPlayer[] soundscapePlayers = Array.Empty<SoundPlayer>();
    private static readonly (float Time, float Frequency)[] UnsentChimeNotes = { (1.25f, 587.33f), (4.5f, 440f), (9.75f, 523.25f), (12.1f, 783.99f) };
    private static bool muted;
    private static int effectsVolume = 75;
    private static int ambienceVolume = 35;
    private static bool ambientRequested;
    private static int ambientRoom;
    private static int playingSoundscape = -1;

    static GameAudio()
    {
        LoadSettings();
        BuildPlayers();
    }

    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scadryk", "settings.json");
    private sealed class AudioSettings { public int EffectsVolume { get; set; } = 75; public int AmbienceVolume { get; set; } = 35; public bool Muted { get; set; } }

    private static void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            AudioSettings? settings = JsonSerializer.Deserialize<AudioSettings>(File.ReadAllText(SettingsPath));
            if (settings is null) return;
            effectsVolume = Math.Clamp(settings.EffectsVolume, 0, 100);
            ambienceVolume = Math.Clamp(settings.AmbienceVolume, 0, 100);
            muted = settings.Muted;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (JsonException) { }
    }

    private static void SaveSettings()
    {
        try
        {
            string path = SettingsPath, directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonSerializer.Serialize(new AudioSettings { EffectsVolume = effectsVolume, AmbienceVolume = ambienceVolume, Muted = muted }));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void BuildPlayers(bool rebuildEffects = true, bool rebuildAmbience = true)
    {
        if (rebuildEffects)
        {
            foreach (SoundPlayer player in players) player.Dispose();
            foreach (MemoryStream stream in streams) stream.Dispose();
            streams = new MemoryStream[Enum.GetValues<Cue>().Length];
            players = new SoundPlayer[streams.Length];
            for (int i = 0; i < streams.Length; i++)
            {
                streams[i] = new MemoryStream(ScaleWave(CreateWave((Cue)i), effectsVolume / 100f), writable: false);
                players[i] = new SoundPlayer(streams[i]);
            }
        }
        if (rebuildAmbience)
        {
            StopSoundscape();
            foreach (SoundPlayer player in soundscapePlayers) player.Dispose();
            foreach (MemoryStream stream in soundscapeStreams) stream.Dispose();
            soundscapeStreams = new MemoryStream[8]; soundscapePlayers = new SoundPlayer[soundscapeStreams.Length];
            byte[] music = CreateAmbientWave();
            for (int i = 0; i < soundscapeStreams.Length; i++)
            {
                soundscapeStreams[i] = new MemoryStream(ScaleWave(MixAmbientLayers(music, CreateSoundscapeWave(i)), ambienceVolume / 100f), writable: false);
                soundscapePlayers[i] = new SoundPlayer(soundscapeStreams[i]);
            }
            playingSoundscape = -1;
            SyncAmbient();
        }
    }

    internal static bool Muted => muted;
    internal static int EffectsVolume => effectsVolume;
    internal static int AmbienceVolume => ambienceVolume;
    internal static bool ToggleMute() { muted = !muted; SaveSettings(); SyncAmbient(); return muted; }
    internal static void SetEffectsVolume(int value) { value = Math.Clamp(value, 0, 100); if (effectsVolume == value) return; effectsVolume = value; SaveSettings(); BuildPlayers(rebuildAmbience: false); }
    internal static void SetAmbienceVolume(int value) { value = Math.Clamp(value, 0, 100); if (ambienceVolume == value) return; ambienceVolume = value; SaveSettings(); BuildPlayers(rebuildEffects: false); }
    internal static void SilenceForAutomatedRun() { muted = true; ambientRequested = false; SyncAmbient(); }
    internal static void SetAmbientActive(bool active, int room = 0) { ambientRequested = active; ambientRoom = room; SyncAmbient(); }

    private static void SyncAmbient()
    {
        if (!ambientRequested || muted)
        {
            StopSoundscape();
            return;
        }
        SyncSoundscape();
    }

    internal static int SoundscapeForRoom(int room) => room switch
    {
        3 or 18 => 0,                         // refúgios: brasa distante e vento
        0 or 1 or 2 or 4 or 5 => 1,           // cripta e arquivos: pedra funda
        6 or 7 or 21 or 22 or 25 or 26 => 2, // leitos e moinhos: água corrente
        8 or 9 => 3,                          // câmara do Lume: brasas
        10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 19 or 20 or 23 or 24 or 27 or 28 => 4, // ecos, margens e várzea
        29 or 30 or 31 or 32 or 33 or 34 => 5, // vidro, cantoria e lanternas
        41 or 42 => 7,                         // badaladas não enviadas
        43 => 4, 44 => 1, 45 => 2,            // margens, gruta e comportas
        >= 35 => 6,                           // rotas, silêncio e maré emprestada
        _ => 1
    };

    private static void StopSoundscape()
    {
        if (playingSoundscape < 0) return;
        soundscapePlayers[playingSoundscape].Stop(); playingSoundscape = -1;
    }

    private static void SyncSoundscape()
    {
        int wanted = SoundscapeForRoom(ambientRoom);
        if (playingSoundscape == wanted) return;
        StopSoundscape();
        try { soundscapePlayers[wanted].PlayLooping(); playingSoundscape = wanted; }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    internal static void Play(Cue cue)
    {
        if (muted) return;
        try { players[(int)cue].Play(); }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    internal static byte[] ScaleWave(byte[] wave, float gain)
    {
        byte[] scaled = (byte[])wave.Clone();
        for (int i = 44; i + 1 < scaled.Length; i += 2)
        {
            short sample = BitConverter.ToInt16(scaled, i);
            short adjusted = (short)Math.Clamp((int)Math.Round(sample * gain), short.MinValue, short.MaxValue);
            BitConverter.TryWriteBytes(scaled.AsSpan(i, 2), adjusted);
        }
        return scaled;
    }

    private static byte[] CreateWave(Cue cue)
    {
        (float duration, float startHz, float endHz, float noise) = cue switch
        {
            Cue.Strike => (0.13f, 310f, 95f, 0.32f),
            Cue.Dash => (0.19f, 180f, 620f, 0.44f),
            Cue.Impact => (0.16f, 105f, 48f, 0.52f),
            Cue.Skill => (0.27f, 260f, 880f, 0.15f),
            Cue.BossWarning => (0.24f, 138f, 72f, 0.36f),
            Cue.EliteWarning => (0.14f, 520f, 270f, 0.15f),
            _ => (0.22f, 520f, 1040f, 0.04f)
        };
        int sampleCount = (int)(SampleRate * duration);
        using var data = new MemoryStream(sampleCount * 2);
        using (var writer = new BinaryWriter(data, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            uint random = 0x6D2B79F5u + (uint)cue * 7919u;
            double phase = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                float envelope = MathF.Pow(1f - progress, cue == Cue.Loot ? 1.5f : 2.7f);
                float frequency = startHz + (endHz - startHz) * progress;
                phase += 2 * Math.PI * frequency / SampleRate;
                random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                float whiteNoise = ((random & 0xFFFF) / 32767.5f) - 1f;
                float tone = (float)(Math.Sin(phase) * 0.72 + Math.Sin(phase * 1.503) * 0.18);
                float sample = Math.Clamp((tone * (1f - noise) + whiteNoise * noise) * envelope * 0.38f, -1f, 1f);
                writer.Write((short)(sample * short.MaxValue));
            }
        }

        using var wave = new MemoryStream(checked((int)data.Length + 44));
        using (var writer = new BinaryWriter(wave, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write((int)data.Length + 36);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate);
            writer.Write(SampleRate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write((int)data.Length);
            writer.Write(data.GetBuffer(), 0, (int)data.Length);
        }
        return wave.ToArray();
    }

    private static byte[] CreateAmbientWave()
    {
        const int seconds = 16, channels = 2;
        int sampleCount = SampleRate * seconds;
        using var data = new MemoryStream(sampleCount * channels * 2);
        using (var writer = new BinaryWriter(data, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            for (int i = 0; i < sampleCount; i++)
            {
                double time = i / (double)SampleRate;
                double swell = 0.78 + 0.22 * Math.Sin(2 * Math.PI * time / seconds);
                double tide = 0.7 + 0.3 * Math.Sin(2 * Math.PI * time / 8);
                double low = Math.Sin(2 * Math.PI * 41.25 * time) * 0.055
                    + Math.Sin(2 * Math.PI * 55 * time) * 0.045
                    + Math.Sin(2 * Math.PI * 82.5 * time) * 0.026
                    + Math.Sin(2 * Math.PI * 110 * time) * 0.014;
                double high = 0;
                double bellPhase = time % 8;
                if (bellPhase >= 4)
                {
                    double ringTime = bellPhase - 4, decay = Math.Exp(-ringTime * 1.8);
                    high = (Math.Sin(2 * Math.PI * 392 * ringTime) * 0.021 + Math.Sin(2 * Math.PI * 587.33 * ringTime) * 0.009) * decay;
                }
                double left = Math.Clamp((low * tide + high) * swell, -1, 1);
                double right = Math.Clamp((low * (1.02 - tide * 0.02) + high * 0.88) * swell, -1, 1);
                writer.Write((short)(left * short.MaxValue));
                writer.Write((short)(right * short.MaxValue));
            }
        }

        using var wave = new MemoryStream(checked((int)data.Length + 44));
        using (var writer = new BinaryWriter(wave, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write((int)data.Length + 36);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)channels); writer.Write(SampleRate);
            writer.Write(SampleRate * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write((int)data.Length);
            writer.Write(data.GetBuffer(), 0, (int)data.Length);
        }
        return wave.ToArray();
    }

    private static byte[] CreateSoundscapeWave(int style)
    {
        const int seconds = 16, channels = 2, fadeSeconds = 1;
        int sampleCount = SampleRate * seconds, fadeSamples = SampleRate * fadeSeconds;
        float[] leftSamples = new float[sampleCount], rightSamples = new float[sampleCount];
        (float lowHz, float highHz, float noise, float pulseHz, float texture) = style switch
        {
            0 => (73.42f, 110f, 0.075f, 0.11f, 0.22f), // refúgio
            1 => (41.2f, 55f, 0.045f, 0.07f, 0.52f),  // pedra e arquivo
            2 => (82.4f, 123.5f, 0.19f, 0.08f, 0.72f), // água
            3 => (55f, 146.8f, 0.12f, 0.19f, 0.2f),   // brasas
            4 => (55f, 82.4f, 0.09f, 0.055f, 0.26f),   // margem
            5 => (196f, 392f, 0.055f, 0.13f, 0.64f),  // vidro e lanternas
            6 => (32.7f, 49f, 0.028f, 0.04f, 0.9f),    // silêncio
            _ => (130.81f, 196f, 0.018f, 0.035f, 0.18f) // badalada não enviada
        };
        uint random = 0x9E3779B9u + (uint)style * 7919u; float filteredNoise = 0;
        double phaseLow = 0, phaseHigh = 0, phaseGlass = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            double time = i / (double)SampleRate;
            phaseLow += 2 * Math.PI * lowHz / SampleRate; phaseHigh += 2 * Math.PI * highHz / SampleRate;
            phaseGlass += 2 * Math.PI * 587.33 / SampleRate;
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            float white = ((random & 0xFFFF) / 32767.5f) - 1f;
            filteredNoise += (white - filteredNoise) * (style == 2 ? 0.012f : 0.004f);
            float moving = 0.76f + 0.24f * (float)Math.Sin(2 * Math.PI * time * pulseHz);
            float crackle = style == 3 && (random & 0x3FF) < 5 ? 0.55f : 0f;
            float resonance = style == 5 ? (float)Math.Sin(phaseGlass) * 0.018f * (0.5f + 0.5f * (float)Math.Sin(2 * Math.PI * time / 7f)) : 0;
            if (style == 7)
            {
                double cycleTime = time % seconds;
                foreach (var note in UnsentChimeNotes)
                {
                    double age = cycleTime - note.Time;
                    if (age >= 0)
                    {
                        double decay = Math.Exp(-age * 1.45);
                        resonance += (float)((Math.Sin(2 * Math.PI * note.Frequency * age) + Math.Sin(2 * Math.PI * note.Frequency * 2.76 * age) * 0.22) * decay * 0.023);
                    }
                }
            }
            float environmentalNoise = white * (1 - texture) + filteredNoise * texture + crackle;
            float center = ((float)Math.Sin(phaseLow) * 0.026f + (float)Math.Sin(phaseHigh) * 0.012f + resonance
                + environmentalNoise * noise) * moving;
            float pan = 0.86f + 0.14f * (float)Math.Sin(2 * Math.PI * time / seconds);
            leftSamples[i] = center * pan * 0.32f;
            rightSamples[i] = center * (1.72f - pan) * 0.32f;
        }
        using var data = new MemoryStream(sampleCount * channels * 2);
        using (var writer = new BinaryWriter(data, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            for (int i = 0; i < sampleCount; i++)
            {
                float left = leftSamples[i], right = rightSamples[i];
                if (i >= sampleCount - fadeSamples)
                {
                    float blend = (i - (sampleCount - fadeSamples)) / (float)fadeSamples;
                    int beginning = i - (sampleCount - fadeSamples);
                    left = left * (1 - blend) + leftSamples[beginning] * blend;
                    right = right * (1 - blend) + rightSamples[beginning] * blend;
                }
                writer.Write((short)(Math.Clamp(left, -1f, 1f) * short.MaxValue));
                writer.Write((short)(Math.Clamp(right, -1f, 1f) * short.MaxValue));
            }
        }
        using var wave = new MemoryStream(checked((int)data.Length + 44));
        using (var writer = new BinaryWriter(wave, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write((int)data.Length + 36);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)channels); writer.Write(SampleRate);
            writer.Write(SampleRate * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write((int)data.Length);
            writer.Write(data.GetBuffer(), 0, (int)data.Length);
        }
        return wave.ToArray();
    }

    private static byte[] MixAmbientLayers(byte[] musicWave, byte[] soundscapeWave)
    {
        const int headerSize = 44, fadeInMilliseconds = 250;
        if (musicWave.Length != soundscapeWave.Length || musicWave.Length < headerSize)
            throw new InvalidDataException("As camadas ambiente precisam ter o mesmo formato e duração.");
        byte[] mixed = new byte[musicWave.Length];
        Buffer.BlockCopy(musicWave, 0, mixed, 0, headerSize);
        for (int offset = headerSize; offset + 1 < musicWave.Length; offset += 2)
        {
            int sample = BitConverter.ToInt16(musicWave, offset) + BitConverter.ToInt16(soundscapeWave, offset);
            int frame = (offset - headerSize) / 4;
            float fadeIn = Math.Min(1f, frame / (SampleRate * fadeInMilliseconds / 1000f));
            sample = (int)(sample * fadeIn);
            short clipped = (short)Math.Clamp(sample, short.MinValue, short.MaxValue);
            mixed[offset] = (byte)(clipped & 0xFF); mixed[offset + 1] = (byte)((clipped >> 8) & 0xFF);
        }
        return mixed;
    }
}
