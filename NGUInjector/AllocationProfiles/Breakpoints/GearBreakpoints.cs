using NGUInjector.AllocationProfiles.BreakpointTypes;
using NGUInjector.Managers;
using SimpleJSON;
using System.Linq;

namespace NGUInjector.AllocationProfiles.Breakpoints
{
    public class GearBreakpoints : BaseBreakpoints<int[]>
    {
        public GearBreakpoints() : base() { }

        public GearBreakpoints(JSONNode bps) : base(bps, (bp) => bp["ID"].AsArray.Children.Select(x => x.AsInt).ToArray()) { }

        protected override bool PerformSwap(Breakpoint bp)
        {
            if (!LockManager.CanSwap())
                return false;

            current = bp;
            LoadoutManager.ChangeGear(bp.priorities);
            Main.InventoryController.assignCurrentEquipToLoadout(0);

            return true;
        }
    }
}
