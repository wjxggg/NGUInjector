using System;
using UnityEngine;

namespace NGUInjector.AllocationProfiles.BreakpointTypes
{
    public class AdvancedTrainingBP : ResourceBreakpoint
    {
        protected override bool CorrectResourceType() => Type == ResourceType.Energy;

        protected override bool Unlocked() => Index <= _character.advancedTrainingController.length && _character.buttons.advancedTraining.interactable;

        protected override bool TargetMet()
        {
            long target = _character.advancedTraining.levelTarget[Index];
            if (target < 0L)
                return true;

            return target != 0L && _character.advancedTraining.level[Index] >= target;
        }

        private AdvancedTrainingController Controller()
        {
            var allController = _character.advancedTrainingController;

            switch (Index)
            {
                case 0:
                    return allController.defense;
                case 1:
                    return allController.attack;
                case 2:
                    return allController.block;
                case 3:
                    return allController.wandoosEnergy;
                case 4:
                    return allController.wandoosMagic;
            }

            return null;
        }

        public override bool Allocate()
        {
            if (_character.wishes.wishes[190].level >= 1)
                return true;
            SetInput(CalculateATCap());
            Controller().addEnergy();

            return true;
        }

        private long CalculateATCap()
        {
            var calcA = CalculateATCap(500);
            if (calcA.PPT < 1)
            {
                var calcB = CalculateATCap(calcA.Offset);
                return calcB.Num;
            }

            return calcA.Num;
        }

        private CapCalc CalculateATCap(int offset)
        {
            var ret = new CapCalc(1, 0);
            var divisor = GetDivisor(Index, offset);
            if (divisor == 0.0)
                return ret;

            double formula = Math.Ceiling(50.0 * divisor /
                (Mathf.Sqrt(_character.totalEnergyPower()) * _character.totalAdvancedTrainingSpeedBonus()));
            if (formula < 1.0)
                formula = 1.0;

            double num = Math.Ceiling(formula / Math.Ceiling(formula / MaxAllocation) * 1.00000202655792);
            long num1;
            if (num > _character.idleEnergy)
                num1 = _character.idleEnergy;
            else
                num1 = (long)num;

            ret.Num = num1;
            ret.PPT = (double)num / formula;
            return ret;
        }

        private float GetDivisor(int index, int offset) => Controller().baseTime * (_character.advancedTraining.level[index] + offset + 1f);
    }
}
