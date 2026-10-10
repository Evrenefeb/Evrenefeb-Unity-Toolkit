using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evrenefeb.Toolkit.GameManagement {
    public class ServiceRegistry
    {
        private readonly Dictionary<Type, IGameService> _services = new Dictionary<Type, IGameService>();

        // Dedicated fast-access lists — no per-frame allocation or type-checking
        private readonly List<ITickableService>      _tickables      = new List<ITickableService>();
        private readonly List<IFixedTickableService> _fixedTickables = new List<IFixedTickableService>();
        private readonly List<ILateTickableService>  _lateTickables  = new List<ILateTickableService>();

        public void Register<T>(T service) where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var existing))
            {
                GameLog.Warning($"Service {type.Name} is already registered. Overwriting...");
                RemoveFromTickLists(existing);
                DisposeService(existing);
                _services[type] = service;
            }
            else
            {
                _services.Add(type, service);
                GameLog.Info($"Registered Service: {type.Name}");
            }

            AddToTickLists(service);
        }

        public void Unregister<T>() where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                RemoveFromTickLists(service);
                _services.Remove(type);
                DisposeService(service);
                GameLog.Info($"Unregistered Service: {type.Name}");
            }
        }

        public async Task InitializeAllAsync()
        {
            // Snapshot so an initializer can Register more services without mutating mid-enumeration.
            var snapshot = new List<IGameService>(_services.Values);
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i] is IInitializableService initializable)
                {
                    await initializable.InitializeAsync();
                }
            }
        }

        public T Get<T>() where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            GameLog.Error($"Service of type {type.Name} not found!");
            return null;
        }

        public bool TryGet<T>(out T service) where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var foundService))
            {
                service = foundService as T;
                return true;
            }
            service = null;
            return false;
        }

        // --- Tick pumps (called by GameManager each frame) ---

        public void TickAll()
        {
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick();
        }

        public void FixedTickAll()
        {
            for (int i = 0; i < _fixedTickables.Count; i++)
                _fixedTickables[i].FixedTick();
        }

        public void LateTickAll()
        {
            for (int i = 0; i < _lateTickables.Count; i++)
                _lateTickables[i].LateTick();
        }

        // --- Disposal (called by GameManager.OnDestroy) ---

        public void DisposeAll()
        {
            foreach (var service in _services.Values)
            {
                DisposeService(service);
            }

            _services.Clear();
            _tickables.Clear();
            _fixedTickables.Clear();
            _lateTickables.Clear();
        }

        // --- Helpers ---

        private static void DisposeService(IGameService service)
        {
            if (service is IDisposableService disposable)
            {
                disposable.Dispose();
                GameLog.Info($"Disposed Service: {service.GetType().Name}");
            }
        }

        private void AddToTickLists(IGameService service)
        {
            if (service is ITickableService tickable)
                _tickables.Add(tickable);
            if (service is IFixedTickableService fixedTickable)
                _fixedTickables.Add(fixedTickable);
            if (service is ILateTickableService lateTickable)
                _lateTickables.Add(lateTickable);
        }

        private void RemoveFromTickLists(IGameService service)
        {
            if (service is ITickableService tickable)
                _tickables.Remove(tickable);
            if (service is IFixedTickableService fixedTickable)
                _fixedTickables.Remove(fixedTickable);
            if (service is ILateTickableService lateTickable)
                _lateTickables.Remove(lateTickable);
        }
    }
}
