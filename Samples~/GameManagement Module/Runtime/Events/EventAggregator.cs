using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evrenefeb.Toolkit.GameManagement {
    public class EventAggregator
    {
        private readonly Dictionary<Type, IEventChannel> _channels = new Dictionary<Type, IEventChannel>();

        public IDisposable Subscribe<T>(Action<T> handler, int priority = 0, UnityEngine.Object owner = null)
            where T : struct, IGameEvent
        {
            return GetOrCreateChannel<T>().Subscribe(handler, priority, owner, once: false);
        }

        public IDisposable Subscribe<T>(Action<T> handler, UnityEngine.Object owner)
            where T : struct, IGameEvent
        {
            return Subscribe(handler, 0, owner);
        }

        public IDisposable SubscribeOnce<T>(Action<T> handler, int priority = 0, UnityEngine.Object owner = null)
            where T : struct, IGameEvent
        {
            return GetOrCreateChannel<T>().Subscribe(handler, priority, owner, once: true);
        }

        public IDisposable SubscribeOnce<T>(Action<T> handler, UnityEngine.Object owner)
            where T : struct, IGameEvent
        {
            return SubscribeOnce(handler, 0, owner);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            TryGetChannel<T>()?.Unsubscribe(handler);
        }

        public void Publish<T>(in T eventMessage) where T : struct, IGameEvent
        {
            TryGetChannel<T>()?.Publish(in eventMessage);
        }

        public void Clear()
        {
            foreach (var channel in _channels.Values)
            {
                channel.Clear();
            }

            _channels.Clear();
        }

        public void Clear<T>() where T : struct, IGameEvent
        {
            var type = typeof(T);
            if (_channels.TryGetValue(type, out var channel))
            {
                channel.Clear();
                _channels.Remove(type);
            }
        }

        public int GetSubscriberCount()
        {
            int count = 0;
            foreach (var channel in _channels.Values)
            {
                count += channel.AliveCount;
            }

            return count;
        }

        public int GetSubscriberCount<T>() where T : struct, IGameEvent
        {
            var channel = TryGetChannel<T>();
            return channel != null ? channel.AliveCount : 0;
        }

        public void GetDiagnostics(List<EventDiagnostic> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            foreach (var channel in _channels.Values)
            {
                int count = channel.AliveCount;
                if (count > 0)
                {
                    results.Add(new EventDiagnostic(channel.EventTypeName, count));
                }
            }
        }

        private EventChannel<T> GetOrCreateChannel<T>() where T : struct, IGameEvent
        {
            var type = typeof(T);
            if (_channels.TryGetValue(type, out var channel))
            {
                return (EventChannel<T>)channel;
            }

            var created = new EventChannel<T>();
            _channels[type] = created;
            return created;
        }

        private EventChannel<T> TryGetChannel<T>() where T : struct, IGameEvent
        {
            return _channels.TryGetValue(typeof(T), out var channel)
                ? (EventChannel<T>)channel
                : null;
        }

        private interface IEventChannel
        {
            int AliveCount { get; }
            string EventTypeName { get; }
            void Clear();
        }

        private sealed class EventChannel<T> : IEventChannel, ISubscriptionTarget where T : struct, IGameEvent
        {
            private struct Slot
            {
                public Action<T> Handler;
                public UnityEngine.Object Owner;
                public int Priority;
                public bool Once;
                public int Id;
                public bool Alive;
            }

            private readonly List<Slot> _slots = new List<Slot>();
            private readonly List<Slot> _invokeBuffer = new List<Slot>();
            private int _nextId = 1;
            private int _publishDepth;

            public string EventTypeName => typeof(T).Name;

            public int AliveCount
            {
                get
                {
                    CompactIfIdle();
                    int count = 0;
                    for (int i = 0; i < _slots.Count; i++)
                    {
                        if (IsActive(_slots[i]))
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }

            public IDisposable Subscribe(Action<T> handler, int priority, UnityEngine.Object owner, bool once)
            {
                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                CompactIfIdle();

                int id = _nextId++;
                var slot = new Slot
                {
                    Handler = handler,
                    Owner = owner,
                    Priority = priority,
                    Once = once,
                    Id = id,
                    Alive = true
                };

                int insertAt = _slots.Count;
                for (int i = 0; i < _slots.Count; i++)
                {
                    if (priority > _slots[i].Priority)
                    {
                        insertAt = i;
                        break;
                    }
                }

                _slots.Insert(insertAt, slot);
                return new EventSubscription(this, id);
            }

            public void Unsubscribe(Action<T> handler)
            {
                if (handler == null)
                {
                    return;
                }

                for (int i = 0; i < _slots.Count; i++)
                {
                    var slot = _slots[i];
                    if (slot.Alive && Equals(slot.Handler, handler))
                    {
                        slot.Alive = false;
                        _slots[i] = slot;
                    }
                }

                CompactIfIdle();
            }

            public void Publish(in T eventMessage)
            {
                CompactIfIdle();

                _publishDepth++;
                try
                {
                    _invokeBuffer.Clear();
                    for (int i = 0; i < _slots.Count; i++)
                    {
                        var slot = _slots[i];
                        if (!IsActive(slot))
                        {
                            slot.Alive = false;
                            _slots[i] = slot;
                            continue;
                        }

                        _invokeBuffer.Add(slot);
                    }

                    for (int i = 0; i < _invokeBuffer.Count; i++)
                    {
                        var snapshot = _invokeBuffer[i];
                        if (!IsIdAlive(snapshot.Id))
                        {
                            continue;
                        }

                        if (snapshot.Once)
                        {
                            UnsubscribeById(snapshot.Id);
                        }

                        try
                        {
                            snapshot.Handler.Invoke(eventMessage);
                        }
                        catch (Exception ex)
                        {
                            GameLog.Error($"Event handler for {typeof(T).Name} threw {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                }
                finally
                {
                    _publishDepth--;
                    CompactIfIdle();
                }
            }

            public void Clear()
            {
                _slots.Clear();
                _invokeBuffer.Clear();
            }

            public void UnsubscribeById(int id)
            {
                for (int i = 0; i < _slots.Count; i++)
                {
                    var slot = _slots[i];
                    if (slot.Id == id)
                    {
                        slot.Alive = false;
                        _slots[i] = slot;
                        break;
                    }
                }

                CompactIfIdle();
            }

            private bool IsIdAlive(int id)
            {
                for (int i = 0; i < _slots.Count; i++)
                {
                    var slot = _slots[i];
                    if (slot.Id == id)
                    {
                        return IsActive(slot);
                    }
                }

                return false;
            }

            private void CompactIfIdle()
            {
                if (_publishDepth == 0)
                {
                    CompactDead();
                }
            }

            private void CompactDead()
            {
                int write = 0;
                for (int read = 0; read < _slots.Count; read++)
                {
                    var slot = _slots[read];
                    if (!IsActive(slot))
                    {
                        continue;
                    }

                    if (write != read)
                    {
                        _slots[write] = slot;
                    }

                    write++;
                }

                if (write < _slots.Count)
                {
                    _slots.RemoveRange(write, _slots.Count - write);
                }
            }

            private static bool IsActive(Slot slot)
            {
                return slot.Alive && !IsOwnerDestroyed(slot.Owner);
            }

            // Unity overloads == to true for destroyed objects. ReferenceEquals stays
            // false when an owner was supplied, so "no owner" is distinct from fake-null.
            private static bool IsOwnerDestroyed(UnityEngine.Object owner)
            {
                return !ReferenceEquals(owner, null) && owner == null;
            }
        }
    }
}
