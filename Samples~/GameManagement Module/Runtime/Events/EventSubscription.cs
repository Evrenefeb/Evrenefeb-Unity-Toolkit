using System;
using System.Collections.Generic;

namespace Evrenefeb.Toolkit.GameManagement {
    internal interface ISubscriptionTarget
    {
        void UnsubscribeById(int id);
    }

    /// <summary>
    /// Token returned by <see cref="EventAggregator.Subscribe{T}"/>. Dispose to unsubscribe.
    /// Double-dispose is a no-op.
    /// </summary>
    public sealed class EventSubscription : IDisposable
    {
        private ISubscriptionTarget _target;
        private readonly int _id;

        internal EventSubscription(ISubscriptionTarget target, int id)
        {
            _target = target;
            _id = id;
        }

        public void Dispose()
        {
            var target = _target;
            _target = null;
            target?.UnsubscribeById(_id);
        }
    }

    /// <summary>
    /// Groups several subscriptions so they can be released together
    /// (typical OnEnable / OnDisable without a Unity owner).
    /// </summary>
    public sealed class EventSubscriptionGroup : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public void Add(IDisposable subscription)
        {
            if (subscription != null)
            {
                _subscriptions.Add(subscription);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i]?.Dispose();
            }

            _subscriptions.Clear();
        }
    }

    public readonly struct EventDiagnostic
    {
        public readonly string EventTypeName;
        public readonly int SubscriberCount;

        public EventDiagnostic(string eventTypeName, int subscriberCount)
        {
            EventTypeName = eventTypeName;
            SubscriberCount = subscriberCount;
        }
    }
}
