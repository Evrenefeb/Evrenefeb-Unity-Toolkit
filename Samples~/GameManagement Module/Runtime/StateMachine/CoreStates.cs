using System.Threading.Tasks;

namespace Evrenefeb.Toolkit.GameManagement {
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