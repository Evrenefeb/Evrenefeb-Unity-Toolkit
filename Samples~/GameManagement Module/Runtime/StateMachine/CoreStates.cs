using System.Threading.Tasks;

namespace Toolkit.Core.State
{
    public class BootState : BaseGameState
    {
        public override Task EnterAsync()
        {
            GameLog.Info("Entering Boot State...");
            return Task.CompletedTask;
        }
    }

    public class RunningState : BaseGameState
    {
        public override Task EnterAsync()
        {
            GameLog.Info("Entering Running State...");
            return Task.CompletedTask;
        }
    }
}