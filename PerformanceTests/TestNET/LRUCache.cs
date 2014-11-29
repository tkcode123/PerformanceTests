﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestNET
{
    public interface LRUCache<K,V> where K : IEquatable<K>
    {
        void Add(K k, V v);
        bool TryGetValue(K k, out V v);
        void Remove(K k);
        void Clear();

        int Count { get; }

        long Size { get; }
    }

    public sealed class SizeLimitedLRUCache<K,V> : LRUCache<K,V>, IEqualityComparer<int> where K : IEquatable<K>
    {
        struct LRUEntry
        {
            internal int lruOlder;
            internal int lruYounger;
            internal K key;
            internal V value;
        }

        private readonly Dictionary<int, int> index;
        private LRUEntry[] entries;
        private readonly Func<K, V, int> sizeFunc;
        private readonly Func<int, long, int, bool> evictFunc;
        private long size;
        private int posFree;
        private int posFresh;
        private int posOldest;
        private int posYoungest;

        public SizeLimitedLRUCache(int initialSize, Func<K,V,int> sizeFunc, Func<int, long,int,bool> evictFunc) 
        {
            this.index = new Dictionary<int, int>(initialSize, this);
            this.entries = new LRUEntry[Math.Max(4, initialSize)];
            this.sizeFunc = sizeFunc;
            this.evictFunc = evictFunc;
        }
       
        #region LRUCache<K,V> Members

        public void Add(K k, V v)
        {
            var pos = FindPos(k);
            if (pos.HasValue)
            {
                SetYoungest(pos.Value);
                this.entries[pos.Value].value = v;
            }
            else
            {
                int addedSize = sizeFunc(k, v);
                while (this.size > 0 && evictFunc(this.index.Count, this.size, addedSize))
                {
                    EvictOldest();
                }
                int p = PopEntryPos();
                this.entries[p].key = k;
                this.entries[p].value = v;
                this.index.Add(p, p);
                this.size += addedSize;
                SetYoungest(p);
            }
        }

        public bool TryGetValue(K k, out V v)
        {
            var pos = FindPos(k);
            if (pos.HasValue)
            {
                SetYoungest(pos.Value);
                v = this.entries[pos.Value].value;
                return true;
            }
            v = default(V);
            return false;
        }

        public void Remove(K k)
        {
            var pos = FindPos(k);
            if (pos.HasValue)
            {
                Evict(pos.Value);
            }
        }

        public void Clear()
        {
            if (this.index.Count > 0)
            {
                this.index.Clear();
                this.entries = new LRUEntry[4];
                this.posFree = this.posFresh = 0;
                this.size = 0L;
                this.posOldest = this.posYoungest = 0;
            }
        }

        public int Count
        {
            get { return this.index.Count; }
        }

        public long Size
        {
            get { return this.size; }
        }

        #endregion

        #region LRU/Find methods

        private int? FindPos(K k)
        {
            this.entries[0].key = k;
            int pos;
            if (this.index.TryGetValue(0, out pos))
                return pos;
            return null;
        }

        private void SetYoungest(int pos)
        {
            if (pos != this.posYoungest)
            {
                Unchain(pos);
                this.entries[pos].lruOlder = this.posYoungest;
                this.entries[pos].lruYounger = 0;
                this.posYoungest = pos;
                if (this.posOldest == 0)
                    this.posOldest = pos;
            }
        }

        private void Unchain(int pos)
        {
            int x;
            if ((x = this.entries[pos].lruOlder) != 0)
            {
                this.entries[x].lruYounger = this.entries[pos].lruYounger;
            }
            if ((x = this.entries[pos].lruYounger) != 0)
            {
                this.entries[x].lruOlder = this.entries[pos].lruOlder;
            }
            if (this.posOldest == pos)
                this.posOldest = this.entries[pos].lruYounger;
            if (this.posYoungest == pos)
                this.posYoungest = this.entries[pos].lruOlder;
        }

        private void EvictOldest()
        {
            Evict(this.posOldest);
        }

        private void Evict(int pos)
        {
            this.index.Remove(pos);
            int removedSize = sizeFunc(entries[pos].key, entries[pos].value);
            this.size -= removedSize;
            Unchain(pos);
            PushEntryPos(pos);
        }
        #endregion

        #region Storage Management
        int PopEntryPos()
        {
            int x = posFree;
            if (x > 0)
            {
                this.posFree = this.entries[x].lruOlder;
                this.entries[x].lruOlder = 0;
                return x;
            }
            x = posFresh;
            if (x < this.entries.Length)
            {
                if (index.Count == 0)
                    x++;
                this.posFresh = x + 1;
                return x;
            }
            var copy = new LRUEntry[Math.Max(4, (index.Count*3)>>1)];
            if (this.entries.Length > 0)
                Array.Copy(this.entries, 0, copy, 0, this.entries.Length);
            this.entries = copy;
            this.posFresh = 0;
            return PopEntryPos();
        }

        void PushEntryPos(int i)
        {
            this.entries[i].lruOlder = this.posFree;
            this.posFree = i;
        }

        #endregion

        #region IEqualityComparer<int> Members

        bool IEqualityComparer<int>.Equals(int x, int y)
        {
            return entries[x].key.Equals(entries[y].key);
        }

        int IEqualityComparer<int>.GetHashCode(int obj)
        {
            return entries[obj].key.GetHashCode();
        }

        #endregion
    }
}
