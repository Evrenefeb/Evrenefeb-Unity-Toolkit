using System;
using System.Threading.Tasks;

namespace Toolkit.Core
{
    public interface IGameService
    {
    }

    public interface IInitializableService : IGameService
    {
        Task InitializeAsync();
    }

    public interface IDisposableService : IGameService
    {
        void Dispose();
    }

    // Called every frame via GameManager.Update()
    public interface ITickableService : IGameService
    {
        void Tick();
    }

    // Called every fixed physics step via GameManager.FixedUpdate()
    public interface IFixedTickableService : IGameService
    {
        void FixedTick();
    }

    // Called every late frame via GameManager.LateUpdate()
    public interface ILateTickableService : IGameService
    {
        void LateTick();
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        None
    }
}
