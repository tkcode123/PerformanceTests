using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestNET
{
    public static class Extension
    {
       /// <summary>
       /// Aggregates a sequence of one type into a sequence of another type with internal buffering via a state instance.
       /// </summary>
       /// <typeparam name="TSource">Source element type</typeparam>
       /// <typeparam name="TState">State element type</typeparam>
       /// <typeparam name="TResult">Result element type</typeparam>
       /// <param name="source">Source sequence</param>
       /// <param name="createState">Creates the n-th state object to fill in</param>
       /// <param name="fillState">Append to current state object</param>
       /// <param name="finishState">Finish the state object</param>
       /// <returns>Sequence of result elements</returns>
       /// <example>
       /// Aggregates a sequence of strings into UTF8 byte arrays which are written to files that are at most 8 bytes long and returns the
       /// number of bytes written for each file.
       /// <code>
       /// var inp = new string[] { "Hallo", "Welt", "Of", "LINQ" };
       /// var len = inp.Select(x => Encoding.UTF8.GetBytes(x))
       ///              .AggregateMany((x, i) => { var f = new FileStream("TMP" + i, FileMode.Create, FileAccess.Write, FileShare.None); f.Write(x, 0, x.Length); return f; },
       ///                             (x, f) => { if (f.Length+x.Length < 8) { f.Write(x, 0, x.Length); return true; } return false; },
       ///                             (f) => { f.Flush(); var x = f.Length; f.Dispose(); return x; }).ToList();
       /// </code>
       /// </example>
        public static IEnumerable<TResult> AggregateMany<TSource, TState, TResult>(this IEnumerable<TSource> source, 
                                                                                   Func<TSource, int, TState> createState, 
                                                                                   Func<TSource, TState, bool> fillState, 
                                                                                   Func<TState, TResult> finishState)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (createState == null)
                throw new ArgumentNullException("createState");
            if (fillState == null)
                throw new ArgumentNullException("fillState");
            if (finishState == null)
                throw new ArgumentNullException("finishState");
            return new AggregateManyEnumerable<TSource, TState, TResult>(source, createState, fillState, finishState);
        }

        class AggregateManyEnumerable<TSource, TState, TResult> : IEnumerable<TResult>
        {
            private readonly IEnumerable<TSource> source;
            private readonly Func<TSource, int, TState> createState;
            private readonly Func<TSource, TState, bool> fillState;
            private readonly Func<TState, TResult> finishState;

            internal AggregateManyEnumerable(IEnumerable<TSource> source, 
                                             Func<TSource, int, TState> createState, 
                                             Func<TSource, TState, bool> fillState, 
                                             Func<TState, TResult> finishState)
            {
                this.source = source;
                this.createState = createState;
                this.fillState = fillState;
                this.finishState = finishState;
            }

            #region IEnumerable<R> Members

            public IEnumerator<TResult> GetEnumerator()
            {
                return new AggregateManyEnumerator(this);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            class AggregateManyEnumerator : IEnumerator<TResult> 
            {
                private readonly AggregateManyEnumerable<TSource, TState, TResult> master;
                private TState infly;
                private int num;

                private IEnumerator<TSource> source;
                private TResult curr;
                private int status;

                internal AggregateManyEnumerator(AggregateManyEnumerable<TSource, TState, TResult> orig)
                {
                    this.master = orig;
                }

                public TResult Current
                {
                    get
                    {
                        if (status == 0)
                            throw new InvalidOperationException();
                        return curr;
                    }
                }

                public bool MoveNext()
                {
                    if (status == 0)
                    {
                        this.source = this.master.source.GetEnumerator();
                        status = 1;
                    }
                    while (status >= 1 && source.MoveNext())
                    {
                        var c = this.source.Current;
                        if (this.status == 1)
                        {
                            this.infly = master.createState(c, num);
                            this.status = 2;
                            this.num++;
                        }
                        else if (this.master.fillState(c, this.infly) == false)
                        {
                            this.curr = this.master.finishState(this.infly);
                            this.infly = master.createState(c, num);
                            this.num++;
                            return true;
                        }
                    }
                    if (this.status == 2)
                    {
                        this.curr = this.master.finishState(this.infly);
                        this.infly = default(TState);
                        this.status = 3;
                        return true;
                    }
                    Done();
                    return false;
                }

                private void Done()
                {
                    if (this.status != 4)
                    {
                        this.status = 4;
                        this.curr = default(TResult);
                        this.infly = default(TState);
                        var disp = this.source as IDisposable;
                        this.source = null;
                        if (disp != null)
                            disp.Dispose();
                    }
                }

                #region IDisposable Members

                public void Dispose()
                {
                    Done();
                }

                #endregion

                #region IEnumerator Members

                object System.Collections.IEnumerator.Current
                {
                    get { return this.Current; }
                }

                public void Reset()
                {
                    throw new InvalidOperationException();
                }

                #endregion
            }
        }
    }
}
