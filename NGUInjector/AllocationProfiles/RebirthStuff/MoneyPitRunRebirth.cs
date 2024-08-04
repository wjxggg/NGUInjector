using NGUInjector.Managers;

namespace NGUInjector.AllocationProfiles.RebirthStuff
{
    public static class MoneyPitRunRebirth
    {
        public static bool RebirthAvailable()
        {
            if (!Main.Settings.MoneyPitRunMode)
                return false;

            double rebirthDuration = Main.Character.rebirthTime.totalseconds;
            if (rebirthDuration >= 1800.0 && rebirthDuration < 2700.0 && (MoneyPitManager.TimeUntilReady() >= 3600f || MoneyPitManager.NeedsRebirth()))
                return true;
            if (rebirthDuration < 3600.0)
                return false;
            if (!MoneyPitManager.MoneyPitReady())
            {
                if (MoneyPitManager.TimeUntilReady() <= 900f)
                    return false;
                return true;
            }

            return MoneyPitManager.NeedsRebirth();
        }
    }
}