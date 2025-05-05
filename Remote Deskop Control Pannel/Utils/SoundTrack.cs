using NAudio.Wave;

namespace RemoteDeskopControlPannel.Utils
{
    internal class SoundTrack
    {
        private readonly BufferedWaveProvider provider;
        private readonly WasapiOut player;
        public SoundTrack(int sampleRate, int bitsPerSample, int channels)
        {
            player = new WasapiOut();
            provider = new BufferedWaveProvider(new WaveFormat(sampleRate, bitsPerSample, channels));
            player.Init(provider);
            player.Play();
        }

        public void Stop()
        {
            player.Stop();
        }

        public void AddChunk(byte[] buffer)
        {
            provider.AddSamples(buffer, 0, buffer.Length);
        }
    }
}
