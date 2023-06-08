using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using static NGUInjector.Main;

namespace NGUInjector.Managers
{
    static class ZoneHelpers
    {
        internal static readonly int[] TitanZones = { 6, 8, 11, 14, 16, 19, 23, 26, 30, 34, 38, 42, 44, 45 };

        private static Dictionary<int, TitanSnapshot> _titanDetails = new Dictionary<int, TitanSnapshot>();

        private static TitanSnapshotSummary _titanSnapshotSummary = new TitanSnapshotSummary();

        public static int TitanCount()
        {
            return TitanZones.Length;
        }

        internal static bool ZoneIsTitan(int zone)
        {
            return TitanZones.Contains(zone);
        }

        internal static bool ZoneIsWalderp(int zone)
        {
            return zone == 16;
        }

        internal static bool TitanSpawningSoon(int titanIndex)
        {
            return Main.Character.buttons.adventure.IsInteractable() && IsTitanSpawningSoon(titanIndex);
        }

        internal static bool AnyTitansSpawningSoon()
        {
            return _titanSnapshotSummary.SpawningSoon;
        }

        internal static bool ShouldRunGoldLoadout()
        {
            return _titanSnapshotSummary.RunGoldLoadout;
        }

        internal static bool ShouldRunTitanLoadout()
        {
            return _titanSnapshotSummary.RunTitanLoadout;
        }

        internal static void RefreshTitanSnapshots()
        {
            if (!Main.Character.buttons.adventure.IsInteractable())
            {
                _titanSnapshotSummary = new TitanSnapshotSummary();
                _titanDetails.Clear();
                return;
            }

            int maxZone = GetMaxReachableZone(true);
            for (int titanIndex = 0; titanIndex < TitanZones.Length; titanIndex++)
            {
                if (TitanZones[titanIndex] <= maxZone)
                {
                    TitanSnapshot currentSnapshot = GetTitanSnapshot(titanIndex);
                    //LogDebug($"Current Titan {titanIndex}: [{currentSnapshot.SpawnSoonTimestamp}], GoldSwap[{currentSnapshot.ShouldUseGoldLoadout}], TitanSwap[{currentSnapshot.ShouldUseTitanLoadout}]");
                    if (_titanDetails.ContainsKey(titanIndex))
                    {
                        TitanSnapshot oldSnapshot = _titanDetails[titanIndex];
                        //LogDebug($"Old Titan {titanIndex}: [{oldSnapshot.SpawnSoonTimestamp}], GoldSwap[{oldSnapshot.ShouldUseGoldLoadout}], TitanSwap[{oldSnapshot.ShouldUseTitanLoadout}]");
                        oldSnapshot.ShouldUseGoldLoadout = currentSnapshot.ShouldUseGoldLoadout;
                        oldSnapshot.ShouldUseTitanLoadout = currentSnapshot.ShouldUseTitanLoadout;

                        //The titan is active, if it has been active for over 5 minutes, stats are probably not be high enough to kill
                        //Disable the titan to prevent sitting with suboptimal gear forever unless combat is currently occurring in that titan zone
                        if (oldSnapshot.SpawnSoonTimestamp.HasValue && currentSnapshot.SpawnSoonTimestamp.HasValue && (currentSnapshot.ShouldUseGoldLoadout || currentSnapshot.ShouldUseTitanLoadout))
                        {
                            //LogDebug("Waiting for kill...");
                            if ((currentSnapshot.SpawnSoonTimestamp.Value - oldSnapshot.SpawnSoonTimestamp.Value).TotalMinutes >= 5 && CombatHelpers.CurrentCombatZone != TitanZones[titanIndex])
                            {
                                Log($"Titan {titanIndex} still available after 300 seconds");
                                if (currentSnapshot.ShouldUseGoldLoadout)
                                {
                                    Log($"Disabling Titan {titanIndex} as a valid gold swap target");

                                    var tempTargets = Settings.TitanGoldTargets.ToArray();
                                    tempTargets[titanIndex] = false;
                                    Settings.TitanGoldTargets = tempTargets;

                                    var tempDone = Settings.TitanMoneyDone.ToArray();
                                    tempDone[titanIndex] = false;
                                    Settings.TitanMoneyDone = tempDone;

                                    currentSnapshot.ShouldUseGoldLoadout = false;
                                    _titanDetails[titanIndex] = currentSnapshot;
                                }
                                else
                                {
                                    Log($"Disabling Titan {titanIndex} as a valid swap target");

                                    var tempTargets = Settings.TitanSwapTargets.ToArray();
                                    tempTargets[titanIndex] = false;
                                    Settings.TitanSwapTargets = tempTargets;

                                    currentSnapshot.ShouldUseTitanLoadout = false;
                                    _titanDetails[titanIndex] = currentSnapshot;
                                }
                            }
                        }
                        //If the timestamp is now null, the titan has been killed, flag the kill as done if the titan was set to use a gold loadout
                        else if (oldSnapshot.SpawnSoonTimestamp.HasValue && !currentSnapshot.SpawnSoonTimestamp.HasValue && currentSnapshot.ShouldUseGoldLoadout)
                        {
                            //LogDebug($"Marking titan gold swap as complete");
                            var tempDone = Settings.TitanMoneyDone.ToArray();
                            tempDone[titanIndex] = true;
                            Settings.TitanMoneyDone = tempDone;


                            _titanDetails[titanIndex] = currentSnapshot;
                        }
                        else
                        {
                            _titanDetails[titanIndex] = currentSnapshot;
                        }
                    }
                    else
                    {
                        _titanDetails[titanIndex] = currentSnapshot;
                    }
                }
            }

            _titanSnapshotSummary.SpawningSoon = _titanDetails.Any(x => x.Value.SpawnSoonTimestamp.HasValue && (x.Value.ShouldUseGoldLoadout || x.Value.ShouldUseTitanLoadout));
            _titanSnapshotSummary.RunGoldLoadout = _titanDetails.Any(x => x.Value.SpawnSoonTimestamp.HasValue && x.Value.ShouldUseGoldLoadout);
            _titanSnapshotSummary.RunTitanLoadout = _titanDetails.Any(x => x.Value.SpawnSoonTimestamp.HasValue && x.Value.ShouldUseTitanLoadout);

            //LogDebug($"Final Summary: SpawnSoon[{_titanSnapshotSummary.SpawningSoon}], GoldSwap[{_titanSnapshotSummary.RunGoldLoadout}], TitanSwap[{_titanSnapshotSummary.RunTitanLoadout}]");
        }

        private static TitanSnapshot GetTitanSnapshot(int titanIndex)
        {
            DateTime? spawnSoonTimestamp = IsTitanSpawningSoon(titanIndex) ? (DateTime?)DateTime.Now : null;
            bool shouldUseTitanLoadout = Main.Settings.SwapTitanLoadouts && Main.Settings.TitanSwapTargets[titanIndex];
            bool shouldUseGoldLoadout = Main.Settings.ManageGoldLoadouts && Main.Settings.TitanGoldTargets[titanIndex] && !Main.Settings.TitanMoneyDone[titanIndex];

            TitanSnapshot titanSnapshot = new TitanSnapshot(titanIndex, spawnSoonTimestamp, shouldUseTitanLoadout, shouldUseGoldLoadout);

            return titanSnapshot;
        }

        //internal static TitanSpawn TitansSpawningSoon()
        //{
        //    var result = new TitanSpawn();

        //    if (!Main.Character.buttons.adventure.IsInteractable())
        //        return result;
        //    for (var i = 0; i < TitanZones.Length; i++)
        //    {
        //        result.Merge(GetTitanSpawn(i));
        //    }
        //    return result;
        //}

        //private static TitanSpawn GetTitanSpawn(int bossId)
        //{
        //    var result = new TitanSpawn();

        //    if (TitanZones[bossId] > GetMaxReachableZone(true))
        //        return result;

        //    if (!CheckTitanSpawnTime(bossId)) return result;

        //    // Run money once for each boss
        //    result.RunMoneyLoadout = Main.Settings.ManageGoldLoadouts && Main.Settings.TitanGoldTargets[bossId] && !Main.Settings.TitanMoneyDone[bossId];

        //    result.SpawningSoon = result.RunMoneyLoadout || (Main.Settings.SwapTitanLoadouts && Main.Settings.TitanSwapTargets[bossId]);

        //    if (result.SpawningSoon)
        //    {
        //        LogDebug($"Adding {bossId} to TitanTargetList");
        //        result.TitanTargetList.Add(bossId);
        //    }

        //    if (!result.RunMoneyLoadout) return result;
        //    Main.Log($"Running money loadout for {bossId}");
        //    var temp = Main.Settings.TitanMoneyDone.ToArray();
        //    temp[bossId] = true;
        //    Main.Settings.TitanMoneyDone = temp;

        //    return result;
        //}

        private static bool IsTitanSpawningSoon(int bossId)
        {
            //fake IsTitanSpawningSoon for testing
            //return true;

            var controller = Main.Character.adventureController;
            var adventure = Main.Character.adventure;

            var spawnMethod = controller.GetType().GetMethod($"boss{bossId + 1}SpawnTime",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnTimeObj = spawnMethod?.Invoke(controller, null);
            if (spawnTimeObj == null)
                return false;
            var spawnTime = (float)spawnTimeObj;

            var spawnField = adventure.GetType().GetField($"boss{bossId + 1}Spawn",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnObj = spawnField?.GetValue(adventure);

            if (spawnObj == null)
                return false;

            var spawn = (PlayerTime)spawnObj;

            //The way this works is that spawn.totalseconds will count up until it reaches spawnTime and then stays there
            //This triggers the boss to be available, and after killed spawn.totalseconds will reset back down to 0
            return Math.Abs(spawnTime - spawn.totalseconds) < 20;
        }

        internal static int GetMaxReachableZone(bool includingTitans)
        {
            for (var i = Main.Character.adventureController.zoneDropdown.options.Count - 2; i >= 0; i--)
            {
                if (!ZoneIsTitan(i))
                    return i;
                if (includingTitans)
                    return i;
            }
            return 0;
        }

        internal static void OptimizeITOPOD()
        {
            if (!Main.Settings.OptimizeITOPODFloor) return;
            if (Main.Character.arbitrary.boughtLazyITOPOD && Main.Character.arbitrary.lazyITOPODOn) return;
            if (Main.Character.adventure.zone < 1000) return;
            var controller = Main.Character.adventureController;
            var level = controller.itopodLevel;
            var optimal = CalculateBestItopodLevel();
            if (level == optimal) return; // we are on optimal floor
            var highestOpen = Main.Character.adventure.highestItopodLevel;
            var climbing = (level < optimal && level >= highestOpen - 1);
            controller.itopodStartInput.text = optimal.ToString();
            if (climbing)
                optimal++;
            controller.itopodEndInput.text = optimal.ToString();
            controller.verifyItopodInputs();
            if (!climbing)
                controller.zoneSelector.changeZone(1000);
        }

        internal static int CalculateBestItopodLevel()
        {
            var c = Main.Character;
            var num1 = c.totalAdvAttack() / 765f * (Main.Settings.ITOPODCombatMode == 1 || c.training.attackTraining[1] == 0 ? c.idleAttackPower() : c.regAttackPower());
            if (c.totalAdvAttack() < 700.0)
                return 0;
            var num2 = Convert.ToInt32(Math.Floor(Math.Log(num1, 1.05)));
            if (num2 < 1)
                return 1;
            var maxLevel = c.adventureController.maxItopodLevel();
            if (num2 > maxLevel)
                num2 = maxLevel;
            return num2;
        }
    }

    public class TitanSnapshotSummary
    {
        internal bool SpawningSoon { get; set; } = false;
        internal bool RunTitanLoadout { get; set; } = false;
        internal bool RunGoldLoadout { get; set; } = false;
    }

    internal class TitanSnapshot
    {
        internal int TitanIndex { get; set; }
        internal DateTime? SpawnSoonTimestamp { get; set; }
        internal bool ShouldUseTitanLoadout { get; set; }
        internal bool ShouldUseGoldLoadout { get; set; }

        public TitanSnapshot(int titanIndex, DateTime? spawnSoonTimestamp, bool shouldUseTitanLoadout, bool shouldUseGoldLoadout)
        {
            TitanIndex = titanIndex;
            SpawnSoonTimestamp = spawnSoonTimestamp;
            ShouldUseTitanLoadout = shouldUseTitanLoadout;
            ShouldUseGoldLoadout = shouldUseGoldLoadout;
        }
    }
}
