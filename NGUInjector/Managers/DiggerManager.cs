using System;
using System.Collections.Generic;
using System.Linq;
using static NGUInjector.Main;

namespace NGUInjector.Managers
{
    public static class DiggerManager
    {
        private static readonly Character _character = Main.Character;
        private static readonly AllGoldDiggerController _dc = _character.allDiggers;

        private static int[] _savedDiggers;
        private static int[] _tempDiggers;
        private static int[] _curDiggers;
        private static int _cheapestDigger;

        public static LockType CurrentLock { get; set; }
        private static readonly int[] TitanDiggers = { 11, 8, 3, 0 };
        private static readonly int[] YggDiggers = { 11, 8 };

        private static List<GoldDigger> Diggers => _character.diggers.diggers;

        private static List<int> ActiveDiggers => _character.diggers.activeDiggers;

        public static void SaveDiggers() => _savedDiggers = ActiveDiggers?.OrderFrom(_curDiggers).ToArray();

        public static void RestoreDiggers()
        {
            EquipDiggers(_savedDiggers);
            RecapDiggers();
        }

        public static void SaveTempDiggers() => _tempDiggers = ActiveDiggers?.OrderFrom(_curDiggers).ToArray();

        public static void RestoreTempDiggers()
        {
            EquipDiggers(_tempDiggers);
            RecapDiggers();
        }

        public static void EquipDiggers(LockType currentLock)
        {
            switch (currentLock)
            {
                case LockType.Titan:
                    EquipDiggers(TitanDiggers, true);
                    return;
                case LockType.Yggdrasil:
                    EquipDiggers(YggDiggers, true);
                    return;
            }
        }

        public static bool EquipDiggers(int[] diggers, bool ignoreCap = false)
        {
            if (!_character.buttons.diggers.interactable)
                return false;

            _dc.clearAllActiveDiggers();

            if (diggers?.Length > 0 == false)
            {
                _curDiggers = null;
                return true;
            }

            Log($"Equipping Diggers: {string.Join(", ", diggers)}");

            var gps = 0.0;
            if (!ignoreCap)
                gps = _character.grossGoldPerSecond() * (100.0 - Settings.DiggerCap) / 100.0;

            var allEquipped = true;

            foreach (var digger in diggers)
            {
                if (ActiveDiggers.Count >= _dc.maxDiggerSlots())
                {
                    allEquipped = false;
                    break;
                }

                if (Diggers[digger].maxLevel <= 0)
                {
                    allEquipped = false;
                    continue;
                }

                Diggers[digger].curLevel = 1;
                if (_character.goldPerSecond() - _dc.drain(digger, true) >= gps)
                    _dc.activateDigger(digger);

                allEquipped &= Diggers[digger].active;
            }

            _curDiggers = diggers.ToArray();

            UpdateCheapestDigger();

            _dc.refreshMenu();
            return allEquipped;
        }

        public static void RecapDiggers(bool ignoreCap = false)
        {
            if (!_character.buttons.diggers.interactable)
                return;

            var gps = _character.grossGoldPerSecond();
            if (gps == 0.0)
                return;

            if (!ignoreCap)
                gps *= Settings.DiggerCap / 100.0;

            var count = ActiveDiggers.Count;

            foreach (var digger in ActiveDiggers)
                SetLevelMaxAffordable(digger, gps / count);

            var ordered = ActiveDiggers?.OrderFrom(_curDiggers).ToArray();

            for (var i = 0; i < ordered?.Length; i++)
            {
                long curLevel = Diggers[ordered[i]].curLevel;
                long num = curLevel + 1;

                if (num > Diggers[ordered[i]].maxLevel)
                    continue;

                Diggers[ordered[i]].curLevel = num;
                if (_character.totalGPSDrain() > gps)
                    Diggers[ordered[i]].curLevel = curLevel;
            }

            UpgradeCheapestDigger();
            _dc.refreshMenu();
        }

        private static void SetLevelMaxAffordable(int id, double cap)
        {
            if (id < 0 || id > Diggers.Count)
                return;
            var curLevel = Diggers[id].curLevel;
            Diggers[id].curLevel = 0L;
            if (cap < _dc.drain(id, 1, true))
                Diggers[id].curLevel = curLevel;
            else
            {
                var num1 = (long)Math.Floor(Math.Log(cap / _dc.baseGPSDrain[id], _dc.gpsGrowthRate[id]) + 1L);
                if (num1 < curLevel)
                    num1 = curLevel;
                if (num1 > Diggers[id].maxLevel)
                    num1 = Diggers[id].maxLevel;
                Diggers[id].curLevel = num1;
                if (Diggers[id].curLevel == 0L && Diggers[id].active)
                    _dc.activateDigger(id);
                if (_character.grossGoldPerSecond() < _dc.totalGPSDrain())
                    Diggers[id].curLevel = curLevel;
                else if (!Diggers[id].active && Diggers[id].curLevel > 0L && ActiveDiggers.Count < _dc.maxDiggerSlots())
                    _dc.activateDigger(id);
            }
        }

        public static void UpdateCheapestDigger()
        {
            if (!Settings.UpgradeDiggers)
                return;
            _cheapestDigger = -1;
            for (var i = 0; i < Diggers.Count; i++)
            {
                if (_cheapestDigger == -1)
                    _cheapestDigger = i;
                if (_dc.upgradeCost(i) < _dc.upgradeCost(_cheapestDigger))
                    _cheapestDigger = i;
            }
        }

        public static void UpgradeCheapestDigger()
        {
            if (!Settings.UpgradeDiggers)
                return;
            if (_cheapestDigger == -1)
                return;
            if (!_character.buttons.diggers.interactable)
                return;
            if (_dc.upgradeCost(_cheapestDigger) + Settings.MoneyPitThreshold > _character.realGold)
                return;

            Log("Upgrading Digger " + _cheapestDigger);
            _dc.upgradeMaxLevel(_cheapestDigger);

            UpdateCheapestDigger();
            UpgradeCheapestDigger();
        }
    }
}
