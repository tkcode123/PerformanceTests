using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TestNET
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            //Console.WriteLine ("Performance");
            //var start = DateTime.UtcNow;
            //int h = 0;
            //for (int i = 0; i < 10000000; i++)
            //    h += i.ToString ("x8").Length;

            //var end = DateTime.UtcNow;
            //Console.WriteLine ("{1} Took {0}", (end-start), h);

            //var inp = new string[] { "Hallo", "Welt", "bla" };

            //var len = inp.Select(x => Encoding.UTF8.GetBytes(x))
            //               .AggregateMany((x, i) => { var f = new FileStream("TMP" + i, FileMode.Create, FileAccess.Write, FileShare.None); f.Write(x, 0, x.Length); return f; },
            //                            (x, f) => { if (f.Length < 8) { f.Write(x, 0, x.Length); return true; } return false; },
            //                            (f) => { f.Flush(); var x = f.Length; f.Dispose(); return x; }).ToList();
            //Console.WriteLine(len);

            var lru = new SizeLimitedLRUCache<string, Guid>(5, (k, v) => k.Length + 16, (cnt, siz, act) => cnt > 6);
            for (int i = 0; i < 20; i++)
            {
                Guid g = Guid.NewGuid();
                lru.Add(g.ToString(), g);

                Guid g2;
                if (lru.TryGetValue(g.ToString(), out g2) == false)
                    throw new Exception();
                if (g2.Equals(g) == false)
                    throw new Exception();
            }
        }
    }

    class CombinedCache
    {
        private readonly long limit;
        private readonly SizeLimitedLRUCache<string, int> one;

        public CombinedCache(long limit)
        {
            this.limit = limit;
            one = new SizeLimitedLRUCache<string,int>(17, (k,v) => k.Length + 4, (cnt,siz,add) => );
        }


        public LRUCache<string, int> CacheOne
        { 
            get { } 
        }
    }
}
