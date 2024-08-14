using System;
using System.Collections.Generic;
using System.Linq;
using static NGUInjector.Main;
using static NGUInjector.Managers.CombatHelpers;

namespace NGUInjector.Managers
{
    public static class CombatManager
    {
        private static readonly Character _character = Main.Character;
        private static readonly AdventureController _ac = _character.adventureController;
        private static readonly PlayerController _pc = _ac.playerController;
        private static bool isFighting = false;
        private static float fightTimer = 0;
        private static string enemyName;
        private static List<int> blacklistZones = new List<int>();

        private static Adventure Adventure => _character.adventure;

        public static void UpdateFightTimer(float diff) => fightTimer += diff;

        private static void DoBuffs(int combatMode, bool bossOnly)
        {
            // Use Move 69 between fights
            if (combatMode >= 3 && CastMove69())
                return;

            // Do not cast buffs if idling or one-shotting
            if (combatMode <= 0 || combatMode >= 4)
                return;

            // Can't cast buffs while global cooldown is on
            if (RemainingGlobalCooldown() > 0f)
                return;

            // Amount of time we have until enemy spawn
            float remainingTime = RemainingRespawnTime();
            if (combatMode == 3)
                remainingTime -= BaseGlobalCooldown() + 0.05f;

            // The list of buffs we might want to cast
            var buffs = new List<Skill>();
            if (!bossOnly)
            {
                if (MegaBuffUnlocked())
                {
                    buffs.Add(Skill.MegaBuff);
                }
                else
                {
                    buffs.Add(Skill.OffensiveBuff);
                    buffs.Add(Skill.UltimateBuff);
                }
            }
            if (!ParryActive())
                buffs.Add(Skill.Parry);
            if (!ChargeActive())
                buffs.Add(Skill.Charge);

            // Remaining cooldowns of these buffs
            var cooldowns = AllCooldowns().Where(x => buffs.Contains(x.Key) && x.Value < remainingTime).ToDictionary(x => x.Key, x => x.Value);

            // Can't cast any buff yet
            if (!cooldowns.Any(x => x.Value <= 0f))
                return;

            while (cooldowns.Count > 0 && remainingTime >= 0f)
            {
                // Schedule the buff with the longest cooldown
                var skill = cooldowns.AllMaxBy(x => x.Value).First().Key;
                cooldowns.Remove(skill);

                // Go back in time by one move
                remainingTime -= BaseGlobalCooldown() + 0.05f;
                cooldowns = cooldowns.Where(x => x.Value < remainingTime).ToDictionary(x => x.Key, x => x.Value);
            }

            // It's too early to cast buffs
            if (remainingTime > 0f)
                return;

            if (!bossOnly)
            {
                if (MegaBuffUnlocked())
                {
                    if (CastMegaBuff())
                        return;
                }
                else if (CastOffensiveBuff() || CastUltimateBuff())
                {
                    return;
                }
            }

            if (CastCharge())
                return;

            CastParry();
        }

        private static void DoCombat(int combatMode)
        {
            if (!_pc.canUseMove || !_pc.moveCheck())
                return;

            if (combatMode == 4)
            {
                _ac.regularAttackMove.doMove();
                return;
            }

            var combatAI = new CombatAI(_character, combatMode);
            if (combatAI.DoPreCombat())
                return;
            if (combatAI.DoCombatBuffs())
                return;
            combatAI.DoCombat();
        }

        public static bool IsZoneUnlocked(int zone)
        {
            if (zone == -1)
                return true;
            if (zone >= 1000)
                return _character.settings.itopodOn;
            return zone <= _ac.zoneDropdown.options.Count - 2;
        }

        private static bool MoveToZone(int zone)
        {
            if (IsZoneUnlocked(zone) && Adventure.zone != zone)
            {
                isFighting = false;
                fightTimer = 0;
                _ac.zoneSelector.changeZone(zone);
                return true;
            }

            return false;
        }

        private static bool CheckBeastModeToggle(bool beastMode, out bool beastModeWasToggled)
        {
            beastModeWasToggled = false;
            // Beast mode checks
            if (BeastModeUnlocked())
            {
                bool needToToggle = BeastModeActive() != beastMode;

                // If the button is inaccessible, we need to stay in manual mode until we can press it
                if (needToToggle)
                {
                    beastModeWasToggled = CastBeastMode();
                    return true;
                }
            }

            return false;
        }

        private static bool CheckEnemy()
        {
            // Skip blacklisted enemies
            if (Adventure.zone < 1000 && Settings.BlacklistedBosses.Contains(_ac.currentEnemy.spriteID))
                return MoveToZone(-1);

            bool bossOnly = IsCurrentlyAdventuring ? Settings.SnipeBossOnly : IsCurrentlyGoldSniping;

            // Skip regular enemies if we're in bossOnly mode
            if (bossOnly && Adventure.zone < 1000 && !ZoneHelpers.ZoneIsTitan(Adventure.zone))
            {
                if (_ac.currentEnemy.enemyType != enemyType.boss)
                    return MoveToZone(-1);
            }

            return false;
        }

        public static void UpdateBlacklists()
        {
            blacklistZones.Clear();
            if (Settings.BlacklistedBosses?.Length > 0 == false)
                return;
            for (int i = 0; i < _ac.enemyList.Count; i++)
            {
                var enemyList = _ac.enemyList[i];
                if (enemyList.Any(x => Settings.BlacklistedBosses?.Contains(x.spriteID) ?? false))
                    blacklistZones.Add(i);
            }
        }

        public static void DoZone(int zone)
        {
            // If we have no enemy and we were fighting, the fight has ended, update combat flags and release any gear locks. Move to safe zone if we need to heal.
            if (_ac.currentEnemy == null)
            {
                if (isFighting)
                {
                    if (fightTimer >= 1f && _ac.globalKillCounter > 0L)
                        LogCombat($"{enemyName} killed in {fightTimer:0.0}s");

                    if (LockManager.HasGoldLock())
                        IsCurrentlyGoldSniping = false;
                }

                isFighting = false;
                fightTimer = 0f;
            }

            // Equip Gold loadout if we are gold sniping
            if (IsCurrentlyGoldSniping != LockManager.HasGoldLock())
            {
                LockManager.TryGoldDropSwap();
                return;
            }

            // ITOPOD is handled by ITOPOD manager
            if (zone >= 1000)
                return;

            var combatMode = 2;
            if (IsCurrentlyAdventuring)
                combatMode = Settings.CombatMode;
            else if (IsCurrentlyQuesting)
                combatMode = Settings.QuestCombatMode;
            else if (IsCurrentlyFightingTitan)
                combatMode = Settings.TitanCombatMode;

            if (combatMode == 0 && (ZoneHelpers.ZoneIsWalderp(zone) || ZoneHelpers.ZoneIsGodmother(zone)))
                combatMode = 3;

            if (combatMode == 0 || !RegularAttackUnlocked())
                IdleZone(zone);
            else
                ManualZone(zone, combatMode);
        }

        private static void IdleZone(int zone)
        {
            var beastMode = false;
            if (IsCurrentlyAdventuring)
                beastMode = Settings.BeastMode;
            else if (IsCurrentlyGoldSniping)
                beastMode = BeastModeActive();
            else if (IsCurrentlyQuesting)
                beastMode = Settings.QuestBeastMode;
            else if (IsCurrentlyFightingTitan)
                beastMode = Settings.TitanBeastMode;

            // If we need to toggle beast mode, wait in the safe zone in manual mode until beast mode is toggled
            if (CheckBeastModeToggle(beastMode, out bool beastModeWasToggled))
            {
                if (!beastModeWasToggled)
                {
                    if (MoveToZone(-1))
                        return;
                    if (Adventure.autoattacking)
                        _ac.idleAttackMove.setToggle();
                }
                return;
            }

            // Wait for titan spawn in Safe Zone
            if (!isFighting && ZoneHelpers.ZoneIsTitan(zone))
            {
                float time = ZoneHelpers.TimeTillTitanSpawn(Array.IndexOf(ZoneHelpers.TitanZones, zone)) ?? BaseRespawnTime();
                if (time > BaseRespawnTime() - 0.05f)
                {
                    MoveToZone(-1);
                    return;
                }
            }

            // Move to the zone
            if (Adventure.zone != zone)
            {
                // Check if we're in not in the right zone and not in safe zone, if not move to safe zone first
                if (MoveToZone(-1))
                    return;

                // We're in safe zone, recover health
                if (!HasFullHP())
                    return;

                MoveToZone(zone);
                return;
            }

            // Enable idle attack if it's off
            if (!Adventure.autoattacking)
            {
                _ac.idleAttackMove.setToggle();
                return;
            }

            // Wait for an enemy to spawn
            if (_ac.currentEnemy == null)
                return;

            if (CheckEnemy())
                return;

            // We have a valid enemy and we're ready to fight, allow idle combat to continue
            isFighting = true;
            enemyName = _ac.currentEnemy.name;
        }

        private static void ManualZone(int zone, int combatMode)
        {
            // Disable idle attack if it's on
            if (Adventure.autoattacking)
            {
                _ac.idleAttackMove.setToggle();
                return;
            }

            // Enable Beast Mode when one-shotting
            if (combatMode == 4 && BeastModeAvailable() && !BeastModeActive())
            {
                if (CastBeastMode())
                    return;
            }

            var needsToPrecast = false;
            var needsToHeal = false;
            var hpThreshold = 0f;
            switch (combatMode)
            {
                case 1:
                    var activeBuffs = new List<Skill>();
                    if (DefensiveBuffActive())
                        activeBuffs.Add(Skill.DefensiveBuff);
                    if (OffensiveBuffActive())
                        activeBuffs.Add(Skill.OffensiveBuff);
                    if (UltimateBuffActive())
                        activeBuffs.Add(Skill.UltimateBuff);
                    if (MegaBuffActive())
                        activeBuffs.Add(Skill.MegaBuff);
                    var inactiveCooldowns = AllCooldowns().Where(x => !activeBuffs.Contains(x.Key));
                    needsToPrecast = inactiveCooldowns.Any(x => x.Value > 0f); // If any inactive skill is on cooldown, wait for it
                    needsToPrecast |= ChargeUnlocked() && !ChargeActive(); // If Charge is unlocked and not active, cast it
                    needsToPrecast |= ParryUnlocked() && !ParryActive(); // If Parry is unlocked and not active, cast it
                    needsToPrecast |= BeastModeUnlocked() && !BeastModeActive(); // If Beast mode is unlocked and not active, cast it
                    needsToHeal = !HasFullHP();
                    break;
                case 2:
                    needsToHeal = GetHPPercentage() < 0.8f;
                    hpThreshold = 0.8f;
                    break;
                case 3:
                    needsToHeal = GetHPPercentage() < 0.6f;
                    hpThreshold = 0.6f;
                    break;
            }

            if (!isFighting && (needsToPrecast || needsToHeal))
            {
                // Move to safe zone if we're not fighting and need to precast buffs or heal
                if (MoveToZone(-1))
                    return;

                if (combatMode == 1)
                {
                    // When sniping cast Charge if it's not active
                    if (CastCharge())
                        return;

                    // When sniping cast Parry if it's not active
                    if (CastParry())
                        return;

                    // When sniping cast Beast Mode if it's not active
                    if (!BeastModeActive())
                        CastBeastMode();
                }
                // Cast Hyper Regen if not sniping, need at least 10s to regenerate and Hyper Regen is ready
                else if (HyperRegenReady())
                {
                    float regen = _character.totalAdvHPRegen() * (_character.inventory.itemList.GRBComplete ? 10f : 5f);
                    float timeToRegen = (_ac.maxHP() * hpThreshold - Adventure.curHP) / regen;
                    if (timeToRegen >= 10f)
                        CastHyperRegen();
                }
                return;
            }

            // Wait for titan spawn in Safe Zone
            if (!isFighting && ZoneHelpers.ZoneIsTitan(zone))
            {
                float time = ZoneHelpers.TimeTillTitanSpawn(Array.IndexOf(ZoneHelpers.TitanZones, zone)) ?? BaseRespawnTime();
                if (time > BaseRespawnTime() - 0.05f)
                {
                    MoveToZone(-1);
                    return;
                }
            }

            bool bossOnly = IsCurrentlyAdventuring ? Settings.SnipeBossOnly : IsCurrentlyGoldSniping;

            if (!isFighting && combatMode == 1)
            {
                bool skipEnemies = bossOnly || blacklistZones.Contains(zone);
                if (!skipEnemies)
                {
                    // When sniping, cast buffs before the fight
                    float time = RemainingGlobalCooldown();
                    if (MegaBuffUnlocked() && !MegaBuffActive())
                    {
                        time += BaseGlobalCooldown();

                        if (time > BaseRespawnTime())
                        {
                            if (MoveToZone(-1))
                                return;

                            CastMegaBuff();

                            return;
                        }
                    }
                    else
                    {
                        if (OffensiveBuffUnlocked() && !OffensiveBuffActive())
                            time += BaseGlobalCooldown();
                        if (UltimateBuffUnlocked() && !UltimateBuffActive())
                            time += BaseGlobalCooldown();

                        if (time > BaseRespawnTime())
                        {
                            if (MoveToZone(-1))
                                return;

                            if (CastOffensiveBuff())
                                return;

                            CastUltimateBuff();

                            return;
                        }
                    }
                }
            }

            // Move to the zone
            if (Adventure.zone != zone)
            {
                // Check if we're in not in the right zone and not in safe zone, if not move to safe zone first
                if (MoveToZone(-1))
                    return;

                MoveToZone(zone);
                return;
            }

            // Wait for an enemy to spawn
            if (_ac.currentEnemy == null)
            {
                // Cast buffs while waiting
                DoBuffs(combatMode, bossOnly);
                return;
            }

            if (CheckEnemy())
                return;

            // We have a valid enemy and we're ready to fight. Run through our manual combat routine.
            isFighting = true;
            enemyName = _ac.currentEnemy.name;

            DoCombat(combatMode);
        }
    }
}
