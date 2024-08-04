using System;

namespace NGUInjector.AllocationProfiles.BreakpointTypes
{
    public class BasicTrainingBP : ResourceBreakpoint
    {
        protected override bool CorrectResourceType() => Type == ResourceType.Energy;

        protected override bool Unlocked()
        {
            if (Index > 11)
                return false;

            if (Index % 6 == 0)
                return true;

            long[] trainings = Index <= 5 ? _character.training.attackTraining : _character.training.defenseTraining;

            return trainings[Index % 6 - 1] >= 5000 * (Index % 6);
        }

        protected override bool TargetMet() => false;

        public override bool Allocate()
        {
            if (Index <= 5)
            {
                var cap = _character.training.attackCaps[Index % 6];
                SetInput(Math.Min(cap, MaxAllocation));
                _character.allOffenseController.trains[Index % 6].addEnergy();
            }
            else
            {
                var cap = _character.training.defenseCaps[Index % 6];
                SetInput(Math.Min(cap, MaxAllocation));
                _character.allDefenseController.trains[Index % 6].addEnergy();
            }

            return true;
        }
    }
}
