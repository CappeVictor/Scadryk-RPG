using System.Media;

internal static class GameAudio
{
    internal enum Cue { Strike, Dash, Impact, Skill, Loot, BossWarning, EliteWarning }

    private const int SampleRate = 22050;
    private static readonly MemoryStream[] streams;
    private static readonly SoundPlayer[] players;
    private static readonly MemoryStream ambientStream;
    private static readonly SoundPlayer ambientPlayer;
    private static bool muted;
    private static bool ambientRequested;
    private static bool ambientPlaying;

    static GameAudio()
    {
        streams = new MemoryStream[Enum.GetValues<Cue>().Length];
        players = new SoundPlayer[streams.Length];
        for (int i = 0; i < streams.Length; i++)
        {
            streams[i] = new MemoryStream(CreateWave((Cue)i), writable: false);
            players[i] = new SoundPlayer(streams[i]);
        }
        ambientStream = new MemoryStream(CreateAmbientWave(), writable: false);
        ambientPlayer = new SoundPlayer(ambientStream);
    }

    internal static bool Muted => muted;
    internal static bool ToggleMute() { muted = !muted; SyncAmbient(); return muted; }
    internal static void SilenceForAutomatedRun() { muted = true; ambientRequested = false; SyncAmbient(); }
    internal static void SetAmbientActive(bool active) { ambientRequested = active; SyncAmbient(); }

    private static void SyncAmbient()
    {
        if (!ambientRequested || muted)
        {
            if (ambientPlaying) { ambientPlayer.Stop(); ambientPlaying = false; }
            return;
        }
        if (ambientPlaying) return;
        try { ambientPlayer.PlayLooping(); ambientPlaying = true; }
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
}
