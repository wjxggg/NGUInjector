using NGUInjector.AllocationProfiles.BreakpointTypes;
using NGUInjector.Managers;
using SimpleJSON;
using System.Linq;

namespace NGUInjector.AllocationProfiles.Breakpoints
{
    public class BeardBreakpoints : BaseBreakpoints<int[]>
    {
        private readonly DiggerBreakpoints diggerbp;

        public BeardBreakpoints(DiggerBreakpoints diggerbp) : base()
        {
            this.diggerbp = diggerbp;
        }

        public BeardBreakpoints(JSONNode bps, DiggerBreakpoints diggerbp) :
            base(bps, (bp) => bp["List"].AsArray.Children.Select(x => x.AsInt).Where(x => x <= 6).ToArray())
        {
            this.diggerbp = diggerbp;
        }

        protected override bool PerformSwap(Breakpoint bp)
        {
            if (!LockManager.CanSwap())
                return false;

            if (BeardManager.EquipBeards(bp.priorities))
            {
                Main.Log($"Equipping Beards: {string.Join(", ", bp.priorities)}");
                current = bp;
                diggerbp.Reset(); // Diggers could turn off due to a deactivation of the Golden Beard
                return true;
            }
            else
            {
                Main.Log($"Failed to equip Beards: {string.Join(", ", bp.priorities)}");
            }

            return false;
        }
    }
}
