using System;
using System.Collections.Generic;
using UnityEngine;

// Core EventBus implementation modeled after the original lightweight client-side bus.
// Stores struct-typed events in a Dictionary<Type, List<Delegate>> with basic attach/detach/raise/clear.
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

    // Register
    public static void OnAttach<T>(Action<T> del) where T : struct, IEvent
    {
        if (del == null)
        {
            return;
        }

        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _handlers[type] = list;
        }

        if (!list.Contains(del))
        {
            list.Add(del);
        }
    }

    // Unregister
    public static void OnDetach<T>(Action<T> del) where T : struct, IEvent
    {
        if (del == null)
        {
            return;
        }

        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var list))
        {
            list.Remove(del);
        }
    }

    // Raise/broadcast
    public static void OnValueChange<T>(T parameter) where T : struct, IEvent
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var list))
        {
            // iterate backwards to allow detach during callbacks
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is Action<T> action)
                {
                    action(parameter);
                }
            }
        }
    }

    // Clear all (e.g., on scene transition)
    public static void OnClearAllDicDELEvents()
    {
        foreach (var kv in _handlers)
        {
            kv.Value.Clear();
        }
        _handlers.Clear();
    }
}

// Marker interface for type safety
public interface IEvent { }
