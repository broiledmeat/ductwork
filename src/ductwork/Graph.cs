using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork
{
    public class Graph
    {
        public static readonly object DefaultKey = new();
        private readonly object _lock = new();
        private readonly HashSet<IComponent> _components = new();
        private readonly HashSet<IComponent> _componentsCompleted = new();
        private readonly Dictionary<IComponent, Dictionary<object, HashSet<IPlug>>> _componentPlugs = new();
        private readonly Dictionary<IPlug, HashSet<IComponent>> _plugComponents = new();

        public void Add(params IComponent[] components)
        {
            foreach (var component in components)
            {
                _components.Add(component);
            }
        }

        public void Connect(IComponent component, object key, IPlug plug)
        {
            Connect_Internal(component, key, plug);
        }

        public void Connect(IComponent component, IPlug plug)
        {
            Connect_Internal(component, DefaultKey, plug);
        }
        
        public void Connect<T>(Component<T> component, object key, Plug<T> plug)
        {
            Connect_Internal(component, key, plug);
        }

        public void Connect<T>(Component<T> component, Plug<T> plug)
        {
            Connect_Internal(component, DefaultKey, plug);
        }

        private void Connect_Internal(IComponent component, object key, IPlug plug)
        {
            if (!_components.Contains(component))
            {
                throw new InvalidOperationException("Component has not been added to the graph.");
            }

            if (component.Type != plug.Type)
            {
                throw new InvalidOperationException(
                    $"Component output type of {component.Type} does not match Plug input type of {plug.Type}");
            }

            lock (_lock)
            {
                if (!_componentPlugs.ContainsKey(component))
                {
                    _componentPlugs.Add(component, new Dictionary<object, HashSet<IPlug>>());
                }

                if (!_componentPlugs[component].ContainsKey(key))
                {
                    _componentPlugs[component].Add(key, new HashSet<IPlug>());
                }

                if (!_plugComponents.ContainsKey(plug))
                {
                    _plugComponents.Add(plug, new HashSet<IComponent>());
                }

                _componentPlugs[component][key].Add(plug);
                _plugComponents[plug].Add(component);
            }
        }

        public async Task Execute(CancellationToken token)
        {
            var componentTasks = _components
                .Select(component => Task.Run(() => ExecuteComponent(component, token).Wait(token), token))
                .ToArray();
            await Task.WhenAll(componentTasks);
        }

        private async Task ExecuteComponent(IComponent component, CancellationToken token)
        {
            await component.ExecuteWithGraph(this, token);
            SetFinished(component);

            var allPlugs = GetAllPlugs(component);
            foreach (var plug in allPlugs)
            {
                if (IsFinished(plug))
                {
                    plug.SetFinished();
                }
            }
        }

        public IComponent[] GetAllComponents(IPlug plug)
        {
            if (_plugComponents.ContainsKey(plug))
            {
                return _plugComponents[plug].ToArray();
            }

            return Array.Empty<IComponent>();
        }

        public IPlug[] GetAllPlugs(IComponent component)
        {
            if (_componentPlugs.ContainsKey(component))
            {
                return _componentPlugs[component]
                    .SelectMany(pair => pair.Value)
                    .ToArray();
            }

            return Array.Empty<IPlug>();
        }

        public Plug<T>[] GetPlugs<T>(IComponent component, object key)
        {
            if (_componentPlugs.ContainsKey(component) &&
                _componentPlugs[component].ContainsKey(key))
            {
                return _componentPlugs[component][key].OfType<Plug<T>>().ToArray();
            }

            return Array.Empty<Plug<T>>();
        }

        public bool IsFinished(IPlug plug)
        {
            lock (_lock)
            {
                var components = GetAllComponents(plug);
                return !components.Any() || components.All(IsFinished);
            }
        }

        private bool IsFinished(IComponent component)
        {
            lock (_lock)
            {
                return _componentsCompleted.Contains(component);
            }
        }

        private void SetFinished(IComponent component)
        {
            lock (_lock)
            {
                _componentsCompleted.Add(component);
            }
        }
    }
}