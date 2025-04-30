using RemoteDeskopControlPannel.Capture;
using RemoteDeskopControlPannel.Network;

namespace RemoteDeskopControlPannel
{
    internal class Worker
    {
        public bool IsActive { get; private set; } = true;
        public int Delay { get; private set; }
        public Worker(int delay)
        {
            Delay = delay;
        }

        public async void Execute(Server server)
        {
            while (IsActive)
            {
                ScreenCapture.Run(server);
                await Task.Delay(Delay);
            }
        }

        public void Stop()
        {
            IsActive = false;
        }
    }
}
