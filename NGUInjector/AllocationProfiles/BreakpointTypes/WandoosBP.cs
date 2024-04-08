using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGUInjector.AllocationProfiles.BreakpointTypes
{
    internal class WandoosBP : BaseBreakpoint
    {
        protected override bool Unlocked()
        {
            return Character.buttons.wandoos.interactable && !Character.wandoos98.disabled;
        }

        protected override bool TargetMet()
        {
            return false;
        }

        internal override bool Allocate()
        {
            if (Type == ResourceType.Energy)
            {
                AllocateEnergy();
            }
            else
            {
                AllocateMagic();
            }

            return true;
        }

        private void AllocateEnergy()
        {
            long num1 = Character.wandoos98Controller.capAmountEnergy();
            long num2 = (long)(num1 / (long)Math.Ceiling(num1 / (double)MaxAllocation) * 1.00000202655792);
            SetInput(num2);
            Character.wandoos98Controller.addEnergy();
        }

        private void AllocateMagic()
        {
            long num1 = Character.wandoos98Controller.capAmountMagic();
            long num2 = (long)(num1 / (long)Math.Ceiling(num1 / (double)MaxAllocation) * 1.00000202655792);
            SetInput(num2);
            Character.wandoos98Controller.addMagic();
        }

        protected override bool CorrectResourceType()
        {
            return Type == ResourceType.Energy || Type == ResourceType.Magic;
        }
    }
}
