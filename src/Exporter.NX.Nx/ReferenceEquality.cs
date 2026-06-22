// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Identity comparer for keying dictionaries by NX object reference (netstandard2.0 has
    /// no built-in ReferenceEqualityComparer). Two NX handles are the same key only when they
    /// are the same object.
    /// </summary>
    internal sealed class ReferenceEquality<T> : IEqualityComparer<T>
        where T : class
    {
        public static readonly ReferenceEquality<T> Default = new ReferenceEquality<T>();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
