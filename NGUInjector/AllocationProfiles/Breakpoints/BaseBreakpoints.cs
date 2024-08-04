using SimpleJSON;
using System;
using System.Linq;

namespace NGUInjector.AllocationProfiles.BreakpointTypes
{
    public abstract class BaseBreakpoints<T>
    {
        public class Breakpoint
        {
            public double time;
            public T priorities;

            public Breakpoint(JSONNode bp, T priorities)
            {
                time = ParseTime(bp["Time"]);
                this.priorities = priorities;
            }

            private static double ParseTime(JSONNode timeNode)
            {
                var time = 0;

                if (timeNode.IsObject)
                {
                    foreach (var N in timeNode)
                    {
                        if (N.Value.IsNumber)
                        {
                            switch (N.Key.ToLower())
                            {
                                case "h":
                                    time += 60 * 60 * N.Value.AsInt;
                                    break;
                                case "m":
                                    time += 60 * N.Value.AsInt;
                                    break;
                                default:
                                    time += N.Value.AsInt;
                                    break;
                            }
                        }
                    }
                }

                if (timeNode.IsNumber)
                    time = timeNode.AsInt;

                return time;
            }
        }

        protected static readonly Character _character = Main.Character;
        protected Breakpoint[] breakpoints = new Breakpoint[0];
        protected Breakpoint current = null;
        protected bool swapped = false;

        public int Length => breakpoints.Length;

        protected BaseBreakpoints() { }

        protected BaseBreakpoints(JSONNode bps, Func<JSONNode, T> selector)
        {
            breakpoints = bps?.Children.Select(bp => new Breakpoint(bp, selector(bp))).OrderByDescending(x => x.time).ToArray();
        }

        public Breakpoint GetCurrentBreakpoint()
        {
            if (breakpoints == null)
                return null;

            foreach (var b in breakpoints)
            {
                if (Main.Character.rebirthTime.totalseconds > b.time)
                {
                    if (current == null)
                    {
                        swapped = false;
                        current = b;
                    }

                    return b;
                }
            }

            current = null;
            return null;
        }

        public void Swap()
        {
            var bp = GetCurrentBreakpoint();
            if (bp == null)
                return;

            if (current == null || bp.time != current.time)
            {
                current = bp;
                swapped = false;
            }

            if (swapped)
                return;

            swapped = PerformSwap(bp);
        }

        protected abstract bool PerformSwap(Breakpoint bp);

        public virtual void Reset() => current = null;
    }
}
