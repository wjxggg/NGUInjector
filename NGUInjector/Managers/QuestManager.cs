using static NGUInjector.Main;

namespace NGUInjector.Managers
{
    public static class QuestManager
    {
        private static readonly Character _character = Main.Character;
        private static readonly BeastQuestController _qc = _character.beastQuestController;
        private static bool shouldQuest;
        private static bool questBankOverfill;

        private static BeastQuest Quest => _character.beastQuest;

        public static void PerformSlowActions()
        {
            UpdateBankOverfill();
            UpdateShouldQuest();
            CheckQuestTurnin();
        }

        private static void UpdateBankOverfill()
        {
            if (!Settings.AutoQuest)
            {
                questBankOverfill = false;
                return;
            }

            var slots = _qc.maxBankedQuests() - Quest.curBankedQuests + 1;
            var time = slots * _qc.timerThreshold() - Quest.dailyQuestTimer.totalseconds;
            var averageDrops = Settings.FiftyItemMinors || _character.adventure.itopod.perkLevel[94] >= 610 ? 50f : 54.5f;
            var remainingDrops = Quest.inQuest ? Quest.targetDrops - Quest.curDrops : averageDrops;
            var eta = _qc.expectedTimePerDrop() * _qc.idleDropFactor() * remainingDrops;
            // Give a bit of extra time for safety
            questBankOverfill = time * 1.1f < eta;
        }

        private static void UpdateShouldQuest()
        {
            if (!Settings.AutoQuest)
            {
                shouldQuest = false;
                return;
            }

            // Major quests take precedence over adventure zones
            if (Quest.inQuest && !Quest.reducedRewards || Settings.QuestsFullBank && questBankOverfill)
            {
                shouldQuest = true;
            }
            else if (Settings.CombatEnabled)
            {
                // Don't quest if combat is enabled, the snipe zone is unlocked, not farming ITOPOD and Fallthrough is not allowed
                var isSniping = CombatManager.IsZoneUnlocked(Settings.SnipeZone) && !Settings.AdventureTargetITOPOD && !Settings.AllowZoneFallback;

                if (isSniping)
                {
                    if (LockManager.HasQuestLock())
                        LockManager.TryQuestSwap();

                    SetIdleMode(Quest.reducedRewards && !Settings.ManualMinors);
                }

                shouldQuest = !isSniping;
            }
        }

        private static void CheckQuestTurnin()
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

            if (!shouldQuest)
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
                majorQuests |= questBankOverfill;

            // First logic: not in a quest
            if (!Quest.inQuest)
            {
                var startQuest = false;

                // If we're allowing major quests and we have a quest available and we should quest
                if (majorQuests && Quest.curBankedQuests > 0 && shouldQuest)
                {
                    _character.settings.useMajorQuests = true;
                    SetIdleMode(false);
                    EquipQuestingLoadout();
                    startQuest = true;
                }
                else if (!Settings.ManualMinors || shouldQuest)
                {
                    _character.settings.useMajorQuests = false;
                    SetIdleMode(!Settings.ManualMinors);

                    if (Settings.ManualMinors && shouldQuest)
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
                var abandonQuest = Settings.QuestsFullBank && questBankOverfill;
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
