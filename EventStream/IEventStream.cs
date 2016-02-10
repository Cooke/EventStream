using System;

namespace Cooke
{
    public interface IEventStream<TP> : IDisposable
    {
        void AddListener<T>(Action<T> handler) where T : TP;

        void RemoveListener<T>(Action<T> handler) where T : TP;

        void AddListener(Type eventType, Action<object> handler);

        void RemoveListener(Type eventType, Action<object> handler);

        void AddListenerStream(EventStream<TP> forwardStream);

        void RemoveListenerStream(EventStream<TP> forwardStream);
    }
}