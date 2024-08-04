using UnityEngine;
using static NGUInjector.Main;

namespace NGUInjector.Managers
{
    public static class MoneyPitManager
    {
        public enum Outcomes
        {
            None,
            IronPill,
            Worn,
            Exp,
            Pomegranate,
            Daycare
        }

        private static readonly Character _character = Main.Character;

        public static float TimeUntilReady() => Mathf.Max(0f, _character.pitController.currentPitTime() - (float)_character.pit.pitTime.totalseconds);

        public static bool MoneyPitReady() => TimeUntilReady() <= 0f;

        public static double ShockwaveTier()
        {
            if (!Settings.MoneyPitRunMode)
                return 0.0;

            Outcomes outcome;

            outcome = PredictMoneyPit(1e50);
            if (outcome == Outcomes.Worn || outcome == Outcomes.Daycare)
                return 1e50;

            outcome = PredictMoneyPit(1e18);
            if (outcome == Outcomes.Worn || outcome == Outcomes.Daycare)
                return 1e18;
            if (PredictMoneyPit(1e15) == Outcomes.Worn)
                return 1e15;
            if (PredictMoneyPit(1e13) == Outcomes.Worn)
                return 1e13;

            return 0.0;
        }

        public static bool NeedsLowerTier()
        {
            if (!Settings.MoneyPitRunMode)
                return false;

            if (!MoneyPitReady())
                return false;

            var tier = ShockwaveTier();

            if (tier == 1e50 || tier == 0.0)
                return false;

            return true;
        }

        public static bool NeedsGold()
        {
            if (!Settings.MoneyPitRunMode)
                return false;

            if (!NeedsLowerTier() || NeedsRebirth())
                return false;

            var tier = ShockwaveTier();
            double gold = _character.realGold;
            var needGold = gold < tier;
            if (tier == 1e15)
                needGold &= gold % 8e16 < 1e15;
            else if (tier == 1e13)
                needGold &= gold % 4e14 < 1e13;
            return needGold;
        }

        public static bool NeedsRebirth()
        {
            if (!Settings.MoneyPitRunMode)
                return false;

            if (_character.machine.realBaseGold <= 0.0)
                return false;

            return NeedsLowerTier();
        }

        public static void CheckMoneyPit()
        {
            if (!MoneyPitReady())
                return;

            var predictionEnabled = Settings.PredictMoneyPit || Settings.MoneyPitRunMode;
            if (!predictionEnabled && _character.realGold < Settings.MoneyPitThreshold)
                return;

            double gold = _character.realGold;
            if (gold < 1e5)
                return;

            if (Settings.MoneyPitRunMode)
            {
                if (NeedsRebirth() || NeedsGold())
                    return;

                var tier = ShockwaveTier();
                if (tier == 1e15 && gold >= 1e18)
                    return;
                if (tier == 1e13 && gold >= 1e15)
                    return;
            }

            if (predictionEnabled)
            {
                switch (PredictMoneyPit())
                {
                    case Outcomes.IronPill:
                        if (gold < Settings.MoneyPitThreshold)
                            return;

                        if (!LockManager.TryMoneyPitSwap(null, new int[] { 10 }))
                            return;

                        if (Settings.ManageMagic)
                        {
                            _character.removeMostMagic();
                            _character.bloodMagicController.capAllRituals();
                        }

                        break;
                    case Outcomes.Worn:
                        LoadoutManager.SaveDaycare();
                        if (!LockManager.TryMoneyPitSwap(Settings.Shockwave, null, true))
                            return;

                        break;
                    case Outcomes.Exp:
                        if (gold < Settings.MoneyPitThreshold)
                            return;

                        if (!LockManager.TryMoneyPitSwap(null, new int[] { 11 }))
                            return;

                        break;
                    case Outcomes.Pomegranate:
                        if (gold < Settings.MoneyPitThreshold)
                            return;

                        if (!LockManager.TryMoneyPitSwap(Settings.YggdrasilLoadout))
                            return;

                        break;
                    case Outcomes.Daycare:
                        LoadoutManager.SaveDaycare();

                        if (!LockManager.TryMoneyPitSwap())
                            return;

                        LoadoutManager.FillDaycare();

                        break;
                    default:
                        if (gold >= Settings.MoneyPitThreshold)
                            DoMoneyPit();

                        return;
                }
            }
            else
            {
                if (gold < Settings.MoneyPitThreshold)
                    return;

                if (gold >= 1e50 && _character.wishes.wishes[4].level > 0)
                {
                    if (!LockManager.TryMoneyPitSwap(Settings.Shockwave, new[] { 11, 10 }))
                        return;

                    if (Settings.ManageMagic)
                    {
                        _character.removeMostMagic();
                        _character.bloodMagicController.capAllRituals();
                    }
                }
            }

            DoMoneyPit();

            LoadoutManager.RestoreDaycare();
            if (LockManager.HasMoneyPitLock())
                LockManager.TryMoneyPitSwap();
        }

        private static Outcomes PredictMoneyPit(double gold = -1.0)
        {
            if (gold < 0.0)
                gold = Main.Character.realGold;
            if (gold >= 1e50 && _character.wishes.wishes[4].level > 0)
            {
                var tempState = Random.state;
                Random.state = _character.pit.pitState;
                int num = Random.Range(1, 6);
                Random.state = tempState;
                return (Outcomes)num;
            }
            else if (gold >= 1e13)
            {
                int num;
                var tempState = Random.state;
                Random.state = _character.pit.pitState;
                if (gold >= 1e18)
                    num = Random.Range(1, 13);
                else if (gold >= 1e15)
                    num = Random.Range(1, 12);
                else
                    num = Random.Range(1, 11);
                Random.state = tempState;
                switch (num)
                {
                    case 4:
                        return Outcomes.Worn;
                    case 12:
                        return Outcomes.Daycare;
                }
            }
            return Outcomes.None;
        }

        private static void DoMoneyPit()
        {
            _character.pitController.CallMethod("engage");
            LogPitSpin($"Money Pit Reward: {_character.pitController.pitText.text}");
        }

        public static void DoDailySpin()
        {
            var controller = _character.dailyController;
            if (_character.daily.spinTime.totalseconds < controller.targetSpinTime())
                return;

            controller.startNoBullshitSpin();
            string result = controller.outcomeText.text;
            LogPitSpin($"Daily Spin Reward: {result}");
        }
    }
}
