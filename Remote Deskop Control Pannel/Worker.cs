using System.IO;
using RemoteDeskopControlPannel.Capture;
using RemoteDeskopControlPannel.Network;

namespace RemoteDeskopControlPannel
{
    internal class Worker(int delay)
    {
        public bool IsActive { get; private set; } = true;
        public int Delay { get; private set; } = delay;

        public async void Execute(Server server)
        {
            while (IsActive)
            {
                try
                {
                    ScreenCapture.Run(server);
                    await Task.Delay(Delay);
                }
                catch (Exception e)
                {
                    try
                    {
                        File.AppendAllText("err.log", $"{e}\n");
                    }
                    catch (Exception) { }
                    throw;
                }
            }
        }

        public void Stop()
        {
            IsActive = false;
        }
    }
}
