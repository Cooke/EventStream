using System;
using System.Linq;
using Cooke;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var children = Enumerable.Range(0, 5).Select(x => new Child($"Child {x}")).ToArray();
            var parent = new Parent(children);

            parent.ChildEvents.AddListener<RunningChildEvent>(x => Console.WriteLine($"{x.Source.Name} is running with speed {x.Speed}."));
            
            foreach (var randomChild in children.OrderBy(x => DateTime.Now.Ticks % 100))
            {
                randomChild.Run();
            }

            Console.ReadLine();
        }

        public abstract class ChildEvent
        {
            public Child Source { get; set; }
        }

        public class RunningChildEvent : ChildEvent
        {
            public float Speed { get; set; }
        }

        public class Child
        {
            private readonly string _name;
            private readonly EventStream<ChildEvent> _events = new EventStream<ChildEvent>();

            public Child(string name)
            {
                _name = name;
            }

            public IEventStream<ChildEvent> Events => _events;

            public string Name => _name;

            public void Run()
            {
                _events.TriggerEvent(new RunningChildEvent { Speed = DateTime.Now.Ticks % 10, Source = this });
            }
        }

        public class Parent
        {
            private readonly EventStream<ChildEvent> _childEvents = new EventStream<ChildEvent>();

            public IEventStream<ChildEvent> ChildEvents => _childEvents;

            public Parent(Child[] children)
            {
                foreach (var child in children)
                {
                    child.Events.AddListenerStream(_childEvents);
                }
            }
        }
    }
}
