namespace Evrenefeb.Toolkit.GameManagement {
    /// <summary>
    /// Marker for GameManager event payloads. Payloads must be structs so
    /// <see cref="EventAggregator"/> can publish without boxing or nulls.
    /// </summary>
    public interface IGameEvent
    {
    }
}
