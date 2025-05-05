using NAudio.Wave;
using RemoteDeskopControlPannel.Network.Packet;

namespace RemoteDeskopControlPannel.Capture
{
    internal class SoundCapture
    {
        private readonly WasapiLoopbackCapture capture = new();
        public readonly int SampleRate;
        public readonly int BitsPerSample;
        public readonly int Channels;
        public SoundCapture(int sampleRate, int bitsPerSample, int channels)
        {
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
            Channels = channels;
            capture.WaveFormat = new WaveFormat(sampleRate, bitsPerSample, channels);
            capture.DataAvailable += (s, a) =>
            {
                MainWindow.Instance.Server?.Broadcast(new PacketSoundChunk(a.Buffer[..a.BytesRecorded]));
            };
            capture.RecordingStopped += (s, a) =>
            {
                capture.Dispose();
            };
        }

        ~SoundCapture() { Close(); }

        public void Start()
        {
            capture.StartRecording();
        }

        public void Close() { capture.StopRecording(); }
    }
}
