using System;
using System.Collections.Generic;

namespace GamePlay
{
    public class PlayerContext
    {
        private readonly Dictionary<Type, object> _systems = new();

        public void Register<T>(T system)
        {
            _systems[typeof(T)] = system;
        }

        public T Get<T>()
        {
            return (T)_systems[typeof(T)];
        }
        
        
        
    }
}