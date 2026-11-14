using System.Media;

internal static class GameAudio
{
    internal enum Cue { Strike, Dash, Impact, Skill, Loot }

    private const int SampleRate = 22050;
    private static readonly MemoryStream[] streams;
    private static readonly SoundPlayer[] players;
    private static bool muted;

    static GameAudio()
    {
        streams = new MemoryStream[Enum.GetValues<Cue>().Length];
        players = new SoundPlayer[streams.Length];
        for (int i = 0; i < streams.Length; i++)
        {
            streams[i] = new MemoryStream(CreateWave((Cue)i), writable: false);
            players[i] = new SoundPlayer(streams[i]);
        }
    }

    internal static bool Muted => muted;
    internal static bool ToggleMute() => muted = !muted;
    internal static void SilenceForAutomatedRun() => muted = true;

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
}
