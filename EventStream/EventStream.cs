using System;
using System.Collections.Generic;
using System.Linq;

namespace Cooke
{
    public class EventStream<TP> : IEventStream<TP>
    {
        private readonly Dictionary<Type, List<InternalEventListener>> _eventListeners = new Dictionary<Type, List<InternalEventListener>>();
        private readonly List<EventStream<TP>> _forwardStreams = new List<EventStream<TP>>();
        private readonly List<EventStream<TP>> _sourceStreams = new List<EventStream<TP>>();

        public void AddListener<T>(Action<T> listener) where T : TP
        {
            var eventType = typeof(T);
            AddListener(eventType, new InternalEventListener(listener));
        }

        public void AddListener(Type eventType, Action<object> listener)
        {
            AddListener(eventType, new InternalEventListener(listener));
        }

        public void RemoveListener<T>(Action<T> listener) where T : TP
        {
            var type = typeof(T);
            RemoveListener(type, listener);
        }

        public void RemoveListener(Type evenType, Action<object> listener)
        {
            RemoveListener(evenType, (object)listener);
        }

        private void RemoveListener(Type type, object listener)
        {
            // We allow trying to removing a registration that does not exist since sometimes a dipose 
            // on the event stream is called before removing a listener
            if (!_eventListeners.ContainsKey(type))
            {
                return;
            }

            var internalEventListener = _eventListeners[type].FirstOrDefault(x => Equals(x.Action, listener));
            if (internalEventListener != null)
            {
                RemoveListener(type, internalEventListener);
            }
        }

        private void AddListener(Type eventType, InternalEventListener listener)
        {
            if (!_eventListeners.ContainsKey(eventType))
            {
                _eventListeners[eventType] = new List<InternalEventListener>();

                foreach (var sourceStream in _sourceStreams)
                {
                    sourceStream.AddListener(eventType, new InternalEventListener(this));
                }
            }

            _eventListeners[eventType].Add(listener);
        }

        private void RemoveListener(Type eventType, EventStream<TP> listenerStream)
        {
            // We allow trying to removing a registration that does not exist since sometimes a dipose 
            // on the event stream is called before removing a listener
            if (!_eventListeners.ContainsKey(eventType))
            {
                return;
            }

            var internalEventListener = _eventListeners[eventType].FirstOrDefault(x => ReferenceEquals(x.Stream, listenerStream));
            if (internalEventListener != null)
            {
                RemoveListener(eventType, internalEventListener);
            }
        }

        private void RemoveListener(Type type, InternalEventListener listener)
        {
            _eventListeners[type].Remove(listener);

            if (!_eventListeners[type].Any())
            {
                _eventListeners.Remove(type);

                foreach (var sourceStream in _sourceStreams)
                {
                    sourceStream.RemoveListener(type, this);
                }
            }
        }

        public void AddListenerStream(EventStream<TP> forwardStream)
        {
            forwardStream._sourceStreams.Add(this);
            _forwardStreams.Add(forwardStream);

            foreach (var eventType in forwardStream._eventListeners.Keys)
            {
                AddListener(eventType, new InternalEventListener(forwardStream));
            }
        }

        public void RemoveListenerStream(EventStream<TP> forwardStream)
        {
            foreach (var listenedEventType in forwardStream._eventListeners.Keys)
            {
                RemoveListener(listenedEventType, forwardStream);
            }

            _forwardStreams.Remove(forwardStream);
            forwardStream._sourceStreams.Remove(this);
        }

        public void TriggerEvent<T>(T ev) where T : TP
        {
            if (!_eventListeners.ContainsKey(typeof(T)))
            {
                return;
            }

            foreach (var listener in _eventListeners[typeof(T)].ToArray())
            {
                listener.TriggerEvent(ev);
            }
        }

        public void Dispose()
        {
            foreach (var forwardStream in _forwardStreams.ToArray())
            {
                RemoveListenerStream(forwardStream);
            }

            foreach (var backStream in _sourceStreams.ToArray())
            {
                backStream.RemoveListenerStream(this);
            }

            _eventListeners.Clear();
            _forwardStreams.Clear();
            _sourceStreams.Clear();
        }

        private class InternalEventListener
        {
            private readonly EventStream<TP> _stream;
            private readonly object _action;

            public InternalEventListener(EventStream<TP> stream)
            {
                _stream = stream;
            }

            public InternalEventListener(object action)
            {
                _action = action;
            }

            public EventStream<TP> Stream => _stream;

            public object Action => _action;

            public void TriggerEvent<T>(T ev) where T : TP
            {
                if (_stream != null)
                {
                    _stream.TriggerEvent(ev);
                }
                else if (_action is Action<object>)
                {
                    ((Action<object>)_action)(ev);
                }
                else
                {
                    ((Action<T>)_action)(ev);
                }
            }
        }
    }
}