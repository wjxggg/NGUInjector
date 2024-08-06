using System;
using static NGUInjector.Main;

namespace NGUInjector.Managers
{
    public static class QuestManager
    {
        private static readonly Character _character = Main.Character;
        private static readonly BeastQuestController _qc = _character.beastQuestController;
        private static bool needsRecache = true;
        private static bool shouldQuest;
        private static bool bankNeedsRecache = true;
        private static bool questBankOverfill;

        private static BeastQuest Quest => _character.beastQuest;

        private static bool ShouldQuest
        {
            get
            {
                if (needsRecache)
                {
                    // Don't quest if we're currently fighting any titans
                    bool isSniping = Settings.ManageTitans && ZoneHelpers.AnyTitansSpawningSoon();
                    bool majorQuest = Quest.inQuest && !Quest.reducedRewards || Settings.QuestsFullBank && QuestBankOverfill;

                    // Major quests take precedence over adventure zones
                    if (!isSniping && !majorQuest)
                    {
                        int snipeZone = Settings.AdventureTargetITOPOD ? 1000 : Settings.SnipeZone;
                        bool zoneIsTitan = ZoneHelpers.ZoneIsTitan(Settings.SnipeZone);
                        bool titanSpawningSoon = zoneIsTitan && ZoneHelpers.TitanSpawningSoon(Array.IndexOf(ZoneHelpers.TitanZones, Settings.SnipeZone));

                        // Don't quest if combat is enabled, the snipe zone is unlocked, the snipe zone is not ITOPOD, AND the snipe zone is either a non-titan or is a spawned titan
                        isSniping = Settings.CombatEnabled;
                        isSniping &= CombatManager.IsZoneUnlocked(Settings.SnipeZone);
                        isSniping &= snipeZone < 1000;
                        isSniping &= !zoneIsTitan || titanSpawningSoon;
                    }

                    if (isSniping)
                    {
                        if (LockManager.HasQuestLock())
                            LockManager.TryQuestSwap();

                        SetIdleMode(Quest.reducedRewards && !Settings.ManualMinors);
                    }

                    shouldQuest = !isSniping;

                    needsRecache = false;
                }

                return shouldQuest;
            }
        }

        private static bool QuestBankOverfill
        {
            get
            {
                if (bankNeedsRecache)
                {
                    var slots = _qc.maxBankedQuests() - Quest.curBankedQuests + 1;
                    var time = slots * _qc.timerThreshold() - Quest.dailyQuestTimer.totalseconds;
                    var averageDrops = Settings.FiftyItemMinors || _character.adventure.itopod.perkLevel[94] >= 610 ? 50f : 54.5f;
                    var remainingDrops = Quest.inQuest ? Quest.targetDrops - Quest.curDrops : averageDrops;
                    var eta = _qc.expectedTimePerDrop() * _qc.idleDropFactor() * remainingDrops;
                    // Give a bit of extra time for safety
                    questBankOverfill = time * 1.1f < eta;

                    bankNeedsRecache = false;
                }
                return questBankOverfill;
            }
        }

        public static void ResetCache()
        {
            needsRecache = true;
            bankNeedsRecache = true;
        }

        public static void CheckQuestTurnin()
        {
            if (_character.beastQuest.curDrops >= _character.beastQuest.targetDrops - 2)
            {
                if (!_character.beastQuest.usedButter)
                {
                    if (_character.beastQuest.reducedRewards && Settings.UseButterMinor)
                    {
                        Log("Buttering Minor Quest");
                        _qc.tryUseButter();
                    }

                    if (!_character.beastQuest.reducedRewards && Settings.UseButterMajor)
                    {
                        Log("Buttering Major Quest");
                        _qc.tryUseButter();
                    }
                }
            }

            if (_qc.readyToHandIn())
            {
                Log("Turning in quest");
                _qc.completeQuest();

                // Check if we need to swap back gear and release lock
                if (LockManager.HasQuestLock())
                {
                    // No more quests, swap back
                    if (_character.beastQuest.curBankedQuests == 0)
                        LockManager.TryQuestSwap();
                    // Else if majors are off and we're not manualing minors, swap back
                    else if (!Settings.AllowMajorQuests && !Settings.ManualMinors)
                        LockManager.TryQuestSwap();
                }
            }
        }

        public static int IsQuesting()
        {
            if (!Settings.AutoQuest)
                return -1;

            if (!Quest.inQuest)
                return -1;

            if (!ShouldQuest)
                return -1;

            if (Quest.reducedRewards && !Settings.ManualMinors)
                return -1;

            int questZone = _qc.curQuestZone();
            if (!CombatManager.IsZoneUnlocked(questZone))
                return -1;

            EquipQuestingLoadout();
            return questZone;
        }

        private static void SetIdleMode(bool idle)
        {
            if (Quest.idleMode != idle)
            {
                Quest.idleMode = idle;
                _qc.updateButtons();
                _qc.updateButtonText();
            }
        }

        public static void ManageQuests()
        {
            if (!Settings.AutoQuest)
            {
                if (LockManager.HasQuestLock())
                    LockManager.TryQuestSwap();
                return;
            }

            var majorQuests = Settings.AllowMajorQuests;
            // Check if Quest Bank will overfill before we can finish the current idle quest
            if (Settings.QuestsFullBank)
                majorQuests |= QuestBankOverfill;

            // First logic: not in a quest
            if (!Quest.inQuest)
            {
                var startQuest = false;

                // If we're allowing major quests and we have a quest available and we should quest
                if (majorQuests && Quest.curBankedQuests > 0 && ShouldQuest)
                {
                    _character.settings.useMajorQuests = true;
                    SetIdleMode(false);
                    EquipQuestingLoadout();
                    startQuest = true;
                }
                else if (!Settings.ManualMinors || ShouldQuest)
                {
                    _character.settings.useMajorQuests = false;
                    SetIdleMode(!Settings.ManualMinors);

                    if (Settings.ManualMinors && ShouldQuest)
                        EquipQuestingLoadout();

                    startQuest = true;
                }

                if (startQuest)
                {
                    _qc.startQuest();
                    _qc.refreshMenu();
                }
                // If we're not questing and we still have the lock, restore gear
                else if (LockManager.HasQuestLock())
                {
                    LockManager.TryQuestSwap();
                }

                return;
            }

            // Second logic, we're in a quest
            if (Quest.reducedRewards)
            {
                var abandonQuest = Settings.QuestsFullBank && QuestBankOverfill;
                if (majorQuests && Settings.AbandonMinors && Quest.curBankedQuests > 0)
                {
                    float progress = Quest.curDrops / (float)Quest.targetDrops * 100;
                    // If all this is true get rid of this minor quest
                    abandonQuest |= progress <= Settings.MinorAbandonThreshold;
                }
                abandonQuest |= Settings.FiftyItemMinors && Quest.targetDrops - Quest.curDrops > 50;

                if (abandonQuest)
                {
                    _qc.skipQuest();
                    _qc.refreshMenu();
                }
                else
                {
                    SetIdleMode(!Settings.ManualMinors);
                }
            }
            else
            {
                SetIdleMode(false);
            }
        }

        public static void EquipQuestingLoadout()
        {
            if (!Settings.ManageQuestLoadouts)
                return;
            if (!LockManager.HasQuestLock())
            {
                if (!LockManager.TryQuestSwap())
                    Log("Tried to equip quest loadout but unable to acquire lock");
            }
        }
    }
}
