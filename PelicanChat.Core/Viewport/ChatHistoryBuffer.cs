using System;
using System.Collections.Generic;

namespace PelicanChat.Core.Viewport
{
    /// <summary>
    /// Buffer circular acotado de capacidad fija (orden cronológico: índice 0 más antiguo).
    /// </summary>
    public sealed class ChatHistoryBuffer<T>
    {
        private readonly List<T> items;

        public int Capacity { get; }

        public int Count => items.Count;

        public IReadOnlyList<T> Items => items;

        public ChatHistoryBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "La capacidad debe ser positiva.");

            Capacity = capacity;
            items = new List<T>(Math.Min(capacity, 256));
        }

        public void Add(T item)
        {
            if (items.Count >= Capacity)
            {
                items.RemoveAt(0);
            }

            items.Add(item);
        }

        public void Clear() => items.Clear();
    }
}
