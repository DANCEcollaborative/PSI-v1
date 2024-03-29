﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Provides a pool of shared objects organized by a key. The key is used both to group
    /// interchangeable objects as well as a parameter to the object allocation function.
    /// </summary>
    /// <typeparam name="T">The type of the objects managed by this pool.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class KeyedSharedPool<T, TKey> : IDisposable
        where T : class
    {
        private readonly ConcurrentDictionary<TKey, SharedPool<T>> sharedPools = new ConcurrentDictionary<TKey, SharedPool<T>>();
        private readonly Func<TKey, T> allocator;
        private readonly int initialSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyedSharedPool{T, TKey}"/> class.
        /// </summary>
        /// <param name="allocator">The allocation function for constructing a new object.</param>
        /// <param name="initialSize">Initial size of each pool.</param>
        public KeyedSharedPool(Func<TKey, T> allocator, int initialSize = 10)
        {
            this.allocator = allocator;
            this.initialSize = initialSize;
        }

        /// <summary>
        /// Get or creates a shared object from the pool.
        /// </summary>
        /// <param name="key">The shared object key.</param>
        /// <returns>A shared object from the pool.</returns>
        public Shared<T> GetOrCreate(TKey key)
        {
            return this.GetSharedPool(key).GetOrCreate();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var sharedPool in this.sharedPools.Values)
            {
                sharedPool.Dispose();
            }
        }

        private SharedPool<T> GetSharedPool(TKey key)
        {
            return this.sharedPools.GetOrAdd(key, new SharedPool<T>(() => this.allocator(key), this.initialSize));
        }
    }
}
