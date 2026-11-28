namespace Yep.Core
{
    using SoundFlow.Abstracts.Devices;
    using SoundFlow.Backends.MiniAudio;
    using SoundFlow.Components;
    using SoundFlow.Providers;
    using SoundFlow.Structs;

    internal static class Sound
    {
        private static readonly MiniAudioEngine engine = new();
        private static readonly AudioFormat format = AudioFormat.Broadcast;
        private static readonly AudioPlaybackDevice playbackDevice;
        private static Dictionary<string, short[]> SoundCache = new();

        private static readonly List<SoundPlayer> activePlayers = new();

        static Sound()
        {
            engine.UpdateAudioDevicesInfo();

            var defaultDevice = engine.PlaybackDevices.First(x => x.IsDefault);

            playbackDevice = engine.InitializePlaybackDevice(defaultDevice, format);
            playbackDevice.Start();
        }

        internal static void Beep(params (double frequency, double duration)[] notes)
        {
            if (notes.Length == 0)
                return;

            short[] samples;

            if (SoundCache.TryGetValue(string.Join(", ", notes.Select(t => $"{t.Item1}, {t.Item2}")), out short[]? value))
                samples = value;
            else
                samples = GenerateBeep(notes);

            var provider = new RawDataProvider(samples, 44100);
            var player = new SoundPlayer(engine, format, provider);

            playbackDevice.MasterMixer.AddComponent(player);
            activePlayers.Add(player);

            player.Play();
            player.PlaybackEnded += Player_PlaybackEnded;
            provider.EndOfStreamReached += Provider_EndOfStreamReached;
        }

        private static void Provider_EndOfStreamReached(object? sender, EventArgs e)
        {
            ((RawDataProvider)sender).Dispose();
        }

        private static void Player_PlaybackEnded(object? sender, EventArgs e)
        {

            playbackDevice.MasterMixer.RemoveComponent((SoundPlayer)sender);
            activePlayers.Remove((SoundPlayer)sender);
            ((SoundPlayer)sender).Dispose();
        }

        private static short[] GenerateBeep((double frequency, double duration)[] notes, int sampleRate = 44100, double volume = 0.5)
        {
            int totalSamples = notes.Sum(n => (int)(sampleRate * n.duration));

            short[] samples = new short[totalSamples];

            int offset = 0;

            foreach (var note in notes)
            {
                int sampleCount = (int)(sampleRate * note.duration);

                for (int i = 0; i < sampleCount; i++)
                {
                    double time = (double)i / sampleRate;

                    double value = Math.Sin(2 * Math.PI * note.frequency * time);

                    samples[offset + i] = (short)(value * short.MaxValue * volume);
                }

                offset += sampleCount;
            }
            SoundCache[string.Join(", ", notes.Select(t => $"{t.Item1}, {t.Item2}"))] = samples;
            return samples;
        }

        internal static void Shutdown()
        {
            foreach (var player in activePlayers)
            {
                playbackDevice.MasterMixer.RemoveComponent(player);
                player.Dispose();
            }

            activePlayers.Clear();

            playbackDevice.Dispose();
            engine.Dispose();
        }
    }
}
