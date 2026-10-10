using System.Collections.Generic;
using UnityEngine;

namespace Evrenefeb.Toolkit.GameManagement {
    public interface IEntityTick
    {
        void OnTick(float deltaTime);
    }

    public interface IFixedEntityTick
    {
        void OnFixedTick(float fixedDeltaTime);
    }

    public interface ILateEntityTick
    {
        void OnLateTick(float deltaTime);
    }

    public interface IUpdateManagerService : IGameService
    {
        void Register(IEntityTick entity);
        void Unregister(IEntityTick entity);

        void RegisterFixed(IFixedEntityTick entity);
        void UnregisterFixed(IFixedEntityTick entity);

        void RegisterLate(ILateEntityTick entity);
        void UnregisterLate(ILateEntityTick entity);
    }

    public class UpdateManagerService : IUpdateManagerService, ITickableService, IFixedTickableService, ILateTickableService
    {
        // Pre-allocated to avoid resizing garbage for large open worlds
        private readonly List<IEntityTick> _tickables;
        private readonly List<IFixedEntityTick> _fixedTickables;
        private readonly List<ILateEntityTick> _lateTickables;

        public UpdateManagerService(int tickCapacity = 1000, int fixedCapacity = 500, int lateCapacity = 500)
        {
            _tickables = new List<IEntityTick>(tickCapacity);
            _fixedTickables = new List<IFixedEntityTick>(fixedCapacity);
            _lateTickables = new List<ILateEntityTick>(lateCapacity);
        }

        public void Register(IEntityTick entity) => _tickables.Add(entity);
        public void RegisterFixed(IFixedEntityTick entity) => _fixedTickables.Add(entity);
        public void RegisterLate(ILateEntityTick entity) => _lateTickables.Add(entity);

        public void Unregister(IEntityTick entity) => FastRemove(_tickables, entity);
        public void UnregisterFixed(IFixedEntityTick entity) => FastRemove(_fixedTickables, entity);
        public void UnregisterLate(ILateEntityTick entity) => FastRemove(_lateTickables, entity);

        public void Tick()
        {
            float dt = Time.deltaTime;
            for (int i = _tickables.Count - 1; i >= 0; i--)
            {
                _tickables[i].OnTick(dt);
            }
        }

        public void FixedTick()
        {
            float dt = Time.fixedDeltaTime;
            for (int i = _fixedTickables.Count - 1; i >= 0; i--)
            {
                _fixedTickables[i].OnFixedTick(dt);
            }
        }

        public void LateTick()
        {
            float dt = Time.deltaTime;
            for (int i = _lateTickables.Count - 1; i >= 0; i--)
            {
                _lateTickables[i].OnLateTick(dt);
            }
        }

        // O(1) removal to prevent list-shifting overhead when destroying thousands of objects
        private void FastRemove<T>(List<T> list, T item)
        {
            int index = list.IndexOf(item);
            if (index < 0) return;

            int lastIndex = list.Count - 1;
            if (index != lastIndex)
            {
                list[index] = list[lastIndex]; // Swap removed item with the last one
            }
            list.RemoveAt(lastIndex); // Remove the last one (O(1) operation)
        }
    }
}