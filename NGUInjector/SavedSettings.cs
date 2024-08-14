using NGUInjector.Managers;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NGUInjector
{
    [Serializable]
    public class SavedSettings
    {
        [SerializeField] private int _snipeZone = -1;
        [SerializeField] private bool _manageTitans;
        [SerializeField] private bool _swapTitanLoadouts;
        [SerializeField] private bool _swapTitanDiggers;
        [SerializeField] private bool _swapTitanBeards;
        [SerializeField] private bool _swapYggdrasilLoadouts;
        [SerializeField] private bool _swapYggdrasilDiggers;
        [SerializeField] private bool _swapYggdrasilBeards;
        [SerializeField] private int[] _priorityBoosts;
        [SerializeField] private bool _manageEnergy;
        [SerializeField] private bool _manageMagic;
        [SerializeField] private bool _manageGear;
        [SerializeField] private bool _manageDiggers;
        [SerializeField] private bool _upgradeDiggers;
        [SerializeField] private double _diggerCap;
        [SerializeField] private bool _manageBeards;
        [SerializeField] private bool _manageYggdrasil;
        [SerializeField] private int[] _titanLoadout;
        [SerializeField] private int[] _yggdrasilLoadout;
        [SerializeField] private bool _manageInventory;
        [SerializeField] private bool _autoFight;
        [SerializeField] private bool _autoQuest;
        [SerializeField] private bool _allowMajorQuests;
        [SerializeField] private bool _questsFullBank;
        [SerializeField] private bool _autoConvertBoosts;
        [SerializeField] private int[] _goldDropLoadout;
        [SerializeField] private bool _autoMoneyPit;
        [SerializeField] private bool _swapPitDiggers;
        [SerializeField] private bool _predictMoneyPit;
        [SerializeField] private bool _moneyPitDaycare;
        [SerializeField] private bool _autoSpin;
        [SerializeField] private int[] _shockwave;
        [SerializeField] private bool _autoRebirth;
        [SerializeField] private bool _manageWandoos;
        [SerializeField] private double _moneyPitThreshold;
        [SerializeField] private int _daycareThreshold;
        [SerializeField] private int[] _boostBlacklist;
        [SerializeField] private bool _snipeBossOnly;
        [SerializeField] private int _combatMode;
        [SerializeField] private bool _allowZoneFallback;
        [SerializeField] private bool _abandonMinors;
        [SerializeField] private int _minorAbandonThreshold;
        [SerializeField] private int _questCombatMode;
        [SerializeField] private bool _questBeastMode;
        [SerializeField] private bool _autoSpellSwap;
        [SerializeField] private int _spaghettiThreshold;
        [SerializeField] private int _counterfeitThreshold;
        [SerializeField] private bool _castBloodSpells;
        [SerializeField] private double _ironPillThreshold;
        [SerializeField] private int _bloodMacGuffinAThreshold;
        [SerializeField] private int _bloodMacGuffinBThreshold;
        [SerializeField] private bool _ironPillOnRebirth;
        [SerializeField] private bool _bloodMacGuffinAOnRebirth;
        [SerializeField] private bool _bloodMacGuffinBOnRebirth;
        [SerializeField] private bool _autoBuyEm;
        [SerializeField] private bool _autoBuyAdventure;
        [SerializeField] private double _bloodNumberThreshold;
        [SerializeField] private int[] _quickLoadout;
        [SerializeField] private int[] _quickDiggers;
        [SerializeField] private int[] _quickBeards;
        [SerializeField] private bool _globalEnabled;
        [SerializeField] private bool _combatEnabled;
        [SerializeField] private bool _useButterMajor;
        [SerializeField] private bool _useButterMinor;
        [SerializeField] private bool _manualMinors;
        [SerializeField] private bool _fiftyItemMinors;
        [SerializeField] private bool _manageR3;
        [SerializeField] private bool _activateFruits;
        [SerializeField] private int[] _wishPriorities;
        [SerializeField] private int[] _wishBlacklist;
        [SerializeField] private bool _weakPriorities;
        [SerializeField] private bool _manageWishes;
        [SerializeField] private int _wishLimit;
        [SerializeField] private int _wishMode;
        [SerializeField] private double _wishEnergy;
        [SerializeField] private double _wishMagic;
        [SerializeField] private double _wishR3;
        [SerializeField] private bool _beastMode;
        [SerializeField] private int _cubePriority;
        [SerializeField] private int _favoredMacguffin;
        [SerializeField] private bool _manageNguDiff;
        [SerializeField] private string _allocationFile;
        [SerializeField] private bool _manageGoldLoadouts;
        [SerializeField] private int _resnipeTime;
        [SerializeField] private bool[] _titanMoneyDone;
        [SerializeField] private bool[] _titanGoldTargets;
        [SerializeField] private bool[] _titanSwapTargets;
        [SerializeField] private bool _goldSnipeComplete;
        [SerializeField] private bool _goldCBlockMode;
        [SerializeField] private int _itopodCombatMode;
        [SerializeField] private int _itopodOptimizeMode;
        [SerializeField] private bool _itopodBeastMode;
        [SerializeField] private bool _itopodAutoPush;
        [SerializeField] private bool _adventureTargetItopod;
        [SerializeField] private int _titanCombatMode;
        [SerializeField] private bool _titanBeastMode;
        [SerializeField] private bool _disableOverlay;
        [SerializeField] private bool _moneyPitRunMode;
        [SerializeField] private int _yggSwapThreshold;
        [SerializeField] private int[] _specialBoostBlacklist;
        [SerializeField] private int[] _blacklistedBosses;
        [SerializeField] private bool _manageMayo;
        [SerializeField] private bool _trashCards;
        [SerializeField] private bool _autoCastCards;
        [SerializeField] private bool _castProtectedCards;
        [SerializeField] private bool _trashProtectedCards;
        [SerializeField] private string[] _cardSortOrder;
        [SerializeField] private bool _cardSortEnabled;
        [SerializeField] private bool _hackAdvance;
        [SerializeField] private bool _manageCooking;
        [SerializeField] private bool _manageQuestLoadouts;
        [SerializeField] private bool _manageCookingLoadouts;
        [SerializeField] private int[] _questLoadout;
        [SerializeField] private int[] _cookingLoadout;
        [SerializeField] private bool _manageConsumables;
        [SerializeField] private bool _autoBuyConsumables;
        [SerializeField] private bool _consumeIfAlreadyRunning;
        [SerializeField] private bool _autosave;
        [SerializeField] private int[] _mergeBlacklist;
        [SerializeField] private string[] _boostPriority;
        [SerializeField] private int[] _cardRarities;
        [SerializeField] private int[] _cardCosts;

        private readonly string _savePath;
        private bool _disableSave;

        public SavedSettings(string dir)
        {
            if (dir != null)
                _savePath = Path.Combine(dir, "settings.json");
        }

        public void SaveSettings()
        {
            if (_savePath == null)
                return;
            if (_disableSave)
                return;
            Main.Log("Saving Settings");
            Main.IgnoreNextChange = true;
            var serialized = JsonUtility.ToJson(this, true);
            using (var writer = new StreamWriter(_savePath))
            {
                writer.Write(serialized);
                writer.Flush();
            }
            Main.UpdateForm(this);
        }

        public void SetSaveDisabled(bool disabled) => _disableSave = disabled;

        public bool LoadSettings()
        {
            if (File.Exists(_savePath))
            {
                try
                {
                    var newSettings = JsonUtility.FromJson<SavedSettings>(File.ReadAllText(_savePath));
                    if (newSettings.TitanSwapTargets?.Length != ZoneHelpers.TitanCount)
                        newSettings.TitanSwapTargets = new bool[ZoneHelpers.TitanCount];
                    if (newSettings.TitanGoldTargets?.Length != ZoneHelpers.TitanCount)
                        newSettings.TitanGoldTargets = new bool[ZoneHelpers.TitanCount];
                    if (newSettings.WishLimit <= 0)
                        newSettings.WishLimit = 4;
                    if (newSettings.CardRarities?.Length != 14)
                        newSettings.CardRarities = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
                    if (newSettings.CardCosts?.Length != 14)
                        newSettings.CardCosts = new int[14];
                    MassUpdate(newSettings);
                    Main.Log("Loaded Settings");
                    Main.Log(JsonUtility.ToJson(this, true));
                    return true;
                }
                catch (Exception e)
                {
                    Main.Log(e.Message);
                    Main.Log(e.StackTrace);
                    return false;
                }
            }

            return false;
        }

        public void MassUpdate(SavedSettings other)
        {
            _priorityBoosts = other.PriorityBoosts;
            _boostBlacklist = other.BoostBlacklist;

            _yggdrasilLoadout = other.YggdrasilLoadout;
            _swapYggdrasilLoadouts = other.SwapYggdrasilLoadouts;
            _swapTitanDiggers = other.SwapYggdrasilDiggers;
            _swapTitanBeards = other.SwapYggdrasilBeards;

            _manageTitans = other.ManageTitans;
            _swapTitanLoadouts = other.SwapTitanLoadouts;
            _swapTitanDiggers = other.SwapTitanDiggers;
            _swapTitanBeards = other.SwapTitanBeards;
            _titanLoadout = other.TitanLoadout;

            _manageBeards = other.ManageBeards;
            _manageDiggers = other.ManageDiggers;
            _manageYggdrasil = other.ManageYggdrasil;
            _manageEnergy = other.ManageEnergy;
            _manageMagic = other.ManageMagic;
            _manageInventory = other.ManageInventory;
            _manageGear = other.ManageGear;
            _manageWandoos = other.ManageWandoos;
            _autoConvertBoosts = other.AutoConvertBoosts;

            _snipeZone = other.SnipeZone;

            _autoFight = other.AutoFight;

            _autoQuest = other.AutoQuest;
            _allowMajorQuests = other.AllowMajorQuests;
            _questsFullBank = other.QuestsFullBank;

            _goldDropLoadout = other.GoldDropLoadout;

            _autoMoneyPit = other.AutoMoneyPit;
            _swapPitDiggers = other.SwapPitDiggers;
            _predictMoneyPit = other.PredictMoneyPit;
            _moneyPitDaycare = other.MoneyPitDaycare;
            _autoSpin = other.AutoSpin;
            _shockwave = other.Shockwave;
            _moneyPitThreshold = other.MoneyPitThreshold;
            _daycareThreshold = other.DaycareThreshold;

            _autoRebirth = other.AutoRebirth;
            _manageWandoos = other.ManageWandoos;

            _combatMode = other.CombatMode;
            _snipeBossOnly = other.SnipeBossOnly;
            _allowZoneFallback = other.AllowZoneFallback;
            _abandonMinors = other.AbandonMinors;
            _minorAbandonThreshold = other.MinorAbandonThreshold;
            _questCombatMode = other.QuestCombatMode;
            _questBeastMode = other.QuestBeastMode;
            _autoSpellSwap = other.AutoSpellSwap;
            _counterfeitThreshold = other.CounterfeitThreshold;
            _spaghettiThreshold = other.SpaghettiThreshold;
            _castBloodSpells = other.CastBloodSpells;
            _ironPillThreshold = other.IronPillThreshold;
            _bloodMacGuffinAThreshold = other.BloodMacGuffinAThreshold;
            _bloodMacGuffinBThreshold = other.BloodMacGuffinBThreshold;
            _ironPillOnRebirth = other.IronPillOnRebirth;
            _bloodMacGuffinAOnRebirth = other.BloodMacGuffinAOnRebirth;
            _bloodMacGuffinBOnRebirth = other.BloodMacGuffinBOnRebirth;
            _autoBuyEm = other.AutoBuyEM;
            _autoBuyAdventure = other.AutoBuyAdventure;
            _bloodNumberThreshold = other.BloodNumberThreshold;
            _quickDiggers = other.QuickDiggers;
            _quickLoadout = other.QuickLoadout;
            _combatEnabled = other.CombatEnabled;
            _globalEnabled = other.GlobalEnabled;
            _useButterMajor = other.UseButterMajor;
            _useButterMinor = other.UseButterMinor;
            _manualMinors = other.ManualMinors;
            _fiftyItemMinors = other.FiftyItemMinors;
            _manageR3 = other.ManageR3;
            _activateFruits = other.ActivateFruits;
            _wishPriorities = other.WishPriorities;
            _wishBlacklist = other.WishBlacklist;
            _weakPriorities = other.WeakPriorities;
            _manageWishes = other.ManageWishes;
            _wishLimit = other.WishLimit;
            _wishMode = other.WishMode;
            _wishEnergy = other.WishEnergy;
            _wishMagic = other.WishMagic;
            _wishR3 = other.WishR3;
            _beastMode = other.BeastMode;
            _cubePriority = other.CubePriority;
            _favoredMacguffin = other.FavoredMacguffin;
            _manageNguDiff = other.ManageNGUDiff;
            _allocationFile = other.AllocationFile;
            _manageGoldLoadouts = other._manageGoldLoadouts;

            _titanGoldTargets = other.TitanGoldTargets;
            _titanSwapTargets = other.TitanSwapTargets;
            _titanMoneyDone = other.TitanMoneyDone;

            _resnipeTime = other.ResnipeTime;
            _goldSnipeComplete = other.GoldSnipeComplete;
            _goldCBlockMode = other._goldCBlockMode;
            _adventureTargetItopod = other.AdventureTargetITOPOD;
            _titanCombatMode = other.TitanCombatMode;
            _titanBeastMode = other.TitanBeastMode;
            _itopodBeastMode = other.ITOPODBeastMode;
            _itopodAutoPush = other.ITOPODAutoPush;
            _itopodCombatMode = other.ITOPODCombatMode;
            _itopodOptimizeMode = other.ITOPODOptimizeMode;
            _disableOverlay = other.DisableOverlay;
            _moneyPitRunMode = other.MoneyPitRunMode;
            _upgradeDiggers = other._upgradeDiggers;
            _diggerCap = other.DiggerCap;
            _yggSwapThreshold = other.YggSwapThreshold;
            _specialBoostBlacklist = other.SpecialBoostBlacklist;

            _blacklistedBosses = other.BlacklistedBosses;
            CombatManager.UpdateBlacklists();

            _manageMayo = other.ManageMayo;
            _trashCards = other.TrashCards;
            _autoCastCards = other.AutoCastCards;
            _castProtectedCards = other.CastProtectedCards;
            _cardRarities = other.CardRarities;
            _cardCosts = other.CardCosts;
            _cardSortOrder = other.CardSortOrder;
            _boostPriority = other.BoostPriority;
            _cardSortEnabled = other.CardSortEnabled;
            _hackAdvance = other.HackAdvance;
            _manageCooking = other.ManageCooking;
            _manageQuestLoadouts = other.ManageQuestLoadouts;
            _manageCookingLoadouts = other.ManageCookingLoadouts;
            _questLoadout = other.QuestLoadout;
            _cookingLoadout = other.CookingLoadout;
            _manageConsumables = other.ManageConsumables;
            _autoBuyConsumables = other.AutoBuyConsumables;
            _consumeIfAlreadyRunning = other.ConsumeIfAlreadyRunning;
            _autosave = other.Autosave;
            _mergeBlacklist = other.MergeBlacklist;
        }

        public int SnipeZone
        {
            get => _snipeZone;
            set
            {
                if (value == _snipeZone) return;
                _snipeZone = value;
                SaveSettings();
            }
        }

        public bool ManageTitans
        {
            get => _manageTitans;
            set
            {
                if (value == _manageTitans) return;
                _manageTitans = value;
                SaveSettings();
            }
        }

        public bool SwapTitanLoadouts
        {
            get => _swapTitanLoadouts;
            set
            {
                if (value == _swapTitanLoadouts) return;
                _swapTitanLoadouts = value;
                SaveSettings();
            }
        }

        public bool SwapTitanDiggers
        {
            get => _swapTitanDiggers;
            set
            {
                if (value == _swapTitanDiggers) return;
                _swapTitanDiggers = value;
                SaveSettings();
            }
        }

        public bool SwapTitanBeards
        {
            get => _swapTitanBeards;
            set
            {
                if (value == _swapTitanBeards) return;
                _swapTitanBeards = value;
                SaveSettings();
            }
        }

        public bool SwapYggdrasilLoadouts
        {
            get => _swapYggdrasilLoadouts;
            set
            {
                if (value == _swapYggdrasilLoadouts) return;
                _swapYggdrasilLoadouts = value;
                SaveSettings();
            }
        }

        public bool SwapYggdrasilDiggers
        {
            get => _swapYggdrasilDiggers;
            set
            {
                if (value == _swapYggdrasilDiggers) return;
                _swapYggdrasilDiggers = value;
                SaveSettings();
            }
        }
        public bool SwapYggdrasilBeards
        {
            get => _swapYggdrasilBeards;
            set
            {
                if (value == _swapYggdrasilBeards) return;
                _swapYggdrasilBeards = value;
                SaveSettings();
            }
        }

        public int[] PriorityBoosts
        {
            get => _priorityBoosts;
            set
            {
                _priorityBoosts = value;
                SaveSettings();
            }
        }

        public bool ManageEnergy
        {
            get => _manageEnergy;
            set
            {
                if (value == _manageEnergy) return;
                _manageEnergy = value;
                SaveSettings();
            }
        }

        public bool ManageMagic
        {
            get => _manageMagic;
            set
            {
                if (value == _manageMagic) return;
                _manageMagic = value;
                SaveSettings();
            }
        }

        public bool ManageGear
        {
            get => _manageGear;
            set
            {
                if (value == _manageGear) return;
                _manageGear = value;
                SaveSettings();
            }
        }

        public int[] TitanLoadout
        {
            get => _titanLoadout;
            set
            {
                _titanLoadout = value;
                SaveSettings();
            }
        }

        public int[] YggdrasilLoadout
        {
            get => _yggdrasilLoadout;
            set
            {
                _yggdrasilLoadout = value;
                SaveSettings();
            }
        }

        public bool ManageYggdrasil
        {
            get => _manageYggdrasil;
            set
            {
                if (value == _manageYggdrasil) return;
                _manageYggdrasil = value;
                SaveSettings();
            }
        }

        public bool ManageDiggers
        {
            get => _manageDiggers;
            set
            {
                if (value == _manageDiggers) return;
                _manageDiggers = value;
                SaveSettings();
            }
        }

        public bool UpgradeDiggers
        {
            get => _upgradeDiggers;
            set
            {
                if (value == _upgradeDiggers) return;
                _upgradeDiggers = value;
                SaveSettings();
            }
        }

        public double DiggerCap
        {
            get => _diggerCap;
            set
            {
                if (value == _diggerCap) return;
                _diggerCap = value;
                SaveSettings();
            }
        }

        public bool ManageBeards
        {
            get => _manageBeards;
            set
            {
                if (value == _manageBeards) return;
                _manageBeards = value;
                SaveSettings();
            }
        }

        public bool ManageInventory
        {
            get => _manageInventory;
            set
            {
                if (value == _manageInventory) return;
                _manageInventory = value;
                SaveSettings();
            }
        }

        public bool AutoFight
        {
            get => _autoFight;
            set
            {
                if (value == _autoFight) return;
                _autoFight = value;
                SaveSettings();
            }
        }

        public bool AutoQuest
        {
            get => _autoQuest;
            set
            {
                if (value == _autoQuest) return;
                _autoQuest = value;
                SaveSettings();
            }
        }

        public bool AllowMajorQuests
        {
            get => _allowMajorQuests;
            set
            {
                if (value == _allowMajorQuests) return;
                _allowMajorQuests = value;
                SaveSettings();
            }
        }

        public bool QuestsFullBank
        {
            get => _questsFullBank;
            set
            {
                if (value == _questsFullBank) return;
                _questsFullBank = value;
                SaveSettings();
            }
        }

        public bool AutoConvertBoosts
        {
            get => _autoConvertBoosts;
            set
            {
                if (value == _autoConvertBoosts) return;
                _autoConvertBoosts = value;
                SaveSettings();
            }
        }

        public int[] GoldDropLoadout
        {
            get => _goldDropLoadout;
            set
            {
                _goldDropLoadout = value;
                SaveSettings();
            }
        }

        public int[] Shockwave
        {
            get => _shockwave;
            set
            {
                _shockwave = value;
                SaveSettings();
            }
        }

        public bool AutoMoneyPit
        {
            get => _autoMoneyPit;
            set
            {
                if (value == _autoMoneyPit) return;
                _autoMoneyPit = value;
                SaveSettings();
            }
        }

        public bool SwapPitDiggers
        {
            get => _swapPitDiggers;
            set
            {
                if (value == _swapPitDiggers) return;
                _swapPitDiggers = value;
                SaveSettings();
            }
        }

        public bool PredictMoneyPit
        {
            get => _predictMoneyPit;
            set
            {
                if (value == _predictMoneyPit) return;
                _predictMoneyPit = value;
                SaveSettings();
            }
        }

        public bool MoneyPitDaycare
        {
            get => _moneyPitDaycare;
            set
            {
                if (value == _moneyPitDaycare) return;
                _moneyPitDaycare = value;
                SaveSettings();
            }
        }

        public bool AutoSpin
        {
            get => _autoSpin;
            set
            {
                if (value == _autoSpin) return;
                _autoSpin = value;
                SaveSettings();
            }
        }

        public bool AutoRebirth
        {
            get => _autoRebirth;
            set
            {
                if (value == _autoRebirth) return;
                _autoRebirth = value;
                SaveSettings();
            }
        }

        public bool ManageWandoos
        {
            get => _manageWandoos;
            set
            {
                if (value == _manageWandoos) return;
                _manageWandoos = value;
                SaveSettings();
            }
        }

        public double MoneyPitThreshold
        {
            get => _moneyPitThreshold;
            set
            {
                _moneyPitThreshold = value;
                SaveSettings();
            }
        }

        public int DaycareThreshold
        {
            get => _daycareThreshold;
            set
            {
                _daycareThreshold = value;
                SaveSettings();
            }
        }

        public bool SnipeBossOnly
        {
            get => _snipeBossOnly;
            set
            {
                if (value == _snipeBossOnly) return;
                _snipeBossOnly = value;
                SaveSettings();
            }
        }

        public int[] BoostBlacklist
        {
            get => _boostBlacklist;
            set
            {
                _boostBlacklist = value;
                SaveSettings();
            }
        }

        public int CombatMode
        {
            get => _combatMode;
            set
            {
                if (value == _combatMode) return;
                _combatMode = value;
                SaveSettings();
            }
        }

        public bool AllowZoneFallback
        {
            get => _allowZoneFallback;
            set
            {
                if (value == _allowZoneFallback) return;
                _allowZoneFallback = value;
                SaveSettings();
            }
        }

        public bool AbandonMinors
        {
            get => _abandonMinors;
            set
            {
                if (value == _abandonMinors) return;
                _abandonMinors = value;
                SaveSettings();
            }
        }

        public int MinorAbandonThreshold
        {
            get => _minorAbandonThreshold;
            set
            {
                if (value == _minorAbandonThreshold) return;
                _minorAbandonThreshold = value;
                SaveSettings();
            }
        }

        public int QuestCombatMode
        {
            get => _questCombatMode;
            set
            {
                if (value == _questCombatMode) return;
                _questCombatMode = value;
                SaveSettings();
            }
        }

        public bool QuestBeastMode
        {
            get => _questBeastMode;
            set
            {
                if (value == _questBeastMode) return;
                _questBeastMode = value;
                SaveSettings();
            }
        }

        public bool AutoSpellSwap
        {
            get => _autoSpellSwap;
            set
            {
                if (value == _autoSpellSwap) return;
                _autoSpellSwap = value;
                SaveSettings();
            }
        }

        public int SpaghettiThreshold
        {
            get => _spaghettiThreshold;
            set
            {
                if (value == _spaghettiThreshold) return;
                _spaghettiThreshold = value;
                SaveSettings();
            }
        }

        public bool CastBloodSpells
        {
            get => _castBloodSpells;
            set
            {
                if (value == _castBloodSpells) return;
                _castBloodSpells = value;
                SaveSettings();
            }
        }

        public double IronPillThreshold
        {
            get => _ironPillThreshold;
            set
            {
                var round = Math.Floor(value);
                if (round == _ironPillThreshold) return;
                _ironPillThreshold = round;
                SaveSettings();
            }
        }

        public int BloodMacGuffinAThreshold
        {
            get => _bloodMacGuffinAThreshold;
            set
            {
                if (value == _bloodMacGuffinAThreshold) return;
                _bloodMacGuffinAThreshold = value;
                SaveSettings();
            }
        }

        public int BloodMacGuffinBThreshold
        {
            get => _bloodMacGuffinBThreshold;
            set
            {
                if (value == _bloodMacGuffinBThreshold) return;
                _bloodMacGuffinBThreshold = value;
                SaveSettings();
            }
        }

        public bool IronPillOnRebirth
        {
            get => _ironPillOnRebirth;
            set
            {
                if (value == _ironPillOnRebirth) return;
                _ironPillOnRebirth = value;
                SaveSettings();
            }
        }

        public bool BloodMacGuffinAOnRebirth
        {
            get => _bloodMacGuffinAOnRebirth;
            set
            {
                if (value == _bloodMacGuffinAOnRebirth) return;
                _bloodMacGuffinAOnRebirth = value;
                SaveSettings();
            }
        }

        public bool BloodMacGuffinBOnRebirth
        {
            get => _bloodMacGuffinBOnRebirth;
            set
            {
                if (value == _bloodMacGuffinBOnRebirth) return;
                _bloodMacGuffinBOnRebirth = value;
                SaveSettings();
            }
        }

        public int CounterfeitThreshold
        {
            get => _counterfeitThreshold;
            set
            {
                if (value == _counterfeitThreshold) return;
                _counterfeitThreshold = value;
                SaveSettings();
            }
        }

        public bool AutoBuyEM
        {
            get => _autoBuyEm;
            set
            {
                if (value == _autoBuyEm) return;
                _autoBuyEm = value;
                SaveSettings();
            }
        }

        public bool AutoBuyAdventure
        {
            get => _autoBuyAdventure;
            set
            {
                if (value == _autoBuyAdventure) return;
                _autoBuyAdventure = value;
                SaveSettings();
            }
        }

        public double BloodNumberThreshold
        {
            get => _bloodNumberThreshold;
            set
            {
                if (value == _bloodNumberThreshold) return;
                _bloodNumberThreshold = value;
                SaveSettings();
            }
        }

        public int[] QuickLoadout
        {
            get => _quickLoadout;
            set => _quickLoadout = value;
        }

        public int[] QuickDiggers
        {
            get => _quickDiggers;
            set => _quickDiggers = value;
        }

        public int[] QuickBeards
        {
            get => _quickBeards;
            set => _quickBeards = value;
        }

        public bool GlobalEnabled
        {
            get => _globalEnabled;
            set
            {
                if (value == _globalEnabled) return;
                _globalEnabled = value;
                SaveSettings();
            }
        }

        public bool CombatEnabled
        {
            get => _combatEnabled;
            set
            {
                if (value == _combatEnabled) return;
                _combatEnabled = value;
                SaveSettings();
            }
        }

        public bool ManualMinors
        {
            get => _manualMinors;
            set
            {
                if (value == _manualMinors) return;
                _manualMinors = value;
                SaveSettings();
            }
        }

        public bool FiftyItemMinors
        {
            get => _fiftyItemMinors;
            set
            {
                if (value == _fiftyItemMinors) return;
                _fiftyItemMinors = value;
                SaveSettings();
            }
        }

        public bool UseButterMajor
        {
            get => _useButterMajor;
            set
            {
                if (value == _useButterMajor) return;
                _useButterMajor = value;
                SaveSettings();
            }
        }

        public bool ManageR3
        {
            get => _manageR3;
            set
            {
                if (value == _manageR3) return;
                _manageR3 = value;
                SaveSettings();
            }
        }

        public bool UseButterMinor
        {
            get => _useButterMinor;
            set
            {
                if (value == _useButterMinor) return;
                _useButterMinor = value;
                SaveSettings();
            }
        }

        public bool ActivateFruits
        {
            get => _activateFruits;
            set
            {
                if (value == _activateFruits) return;
                _activateFruits = value;
                SaveSettings();
            }
        }
        public int[] WishPriorities
        {
            get => _wishPriorities;
            set
            {
                if (value == _wishPriorities) return;
                _wishPriorities = value;
                SaveSettings();
            }
        }
        public int[] WishBlacklist
        {
            get => _wishBlacklist;
            set
            {
                if (value == _wishBlacklist) return;
                _wishBlacklist = value;
                SaveSettings();
            }
        }

        public bool WeakPriorities
        {
            get => _weakPriorities;
            set
            {
                if (value == _weakPriorities) return;
                _weakPriorities = value;
                SaveSettings();
            }
        }

        public bool ManageWishes
        {
            get => _manageWishes;
            set
            {
                if (value == _manageWishes) return;
                _manageWishes = value;
                SaveSettings();
            }
        }

        public int WishLimit
        {
            get => _wishLimit;
            set
            {
                if (value == _wishLimit) return;
                _wishLimit = value;
                SaveSettings();
            }
        }

        public int WishMode
        {
            get => _wishMode;
            set
            {
                if (value == _wishMode) return;
                _wishMode = value;
                SaveSettings();
            }
        }

        public double WishEnergy
        {
            get => _wishEnergy;
            set
            {
                if (value == _wishEnergy) return;
                _wishEnergy = value;
                SaveSettings();
            }
        }

        public double WishMagic
        {
            get => _wishMagic;
            set
            {
                if (value == _wishMagic) return;
                _wishMagic = value;
                SaveSettings();
            }
        }

        public double WishR3
        {
            get => _wishR3;
            set
            {
                if (value == _wishR3) return;
                _wishR3 = value;
                SaveSettings();
            }
        }

        public bool BeastMode
        {
            get => _beastMode;
            set
            {
                if (value == _beastMode) return;
                _beastMode = value;
                SaveSettings();
            }
        }

        public int CubePriority
        {
            get => _cubePriority;
            set
            {
                if (value == _cubePriority) return;
                _cubePriority = value;
                SaveSettings();
            }
        }

        public int FavoredMacguffin
        {
            get => _favoredMacguffin;
            set
            {
                if (value == _favoredMacguffin) return;
                _favoredMacguffin = value;
                SaveSettings();
            }
        }

        public bool ManageNGUDiff
        {
            get => _manageNguDiff;
            set
            {
                if (value == _manageNguDiff) return;
                _manageNguDiff = value;
                SaveSettings();
            }
        }

        public string AllocationFile
        {
            get => _allocationFile;
            set
            {
                if (value == _allocationFile) return;
                _allocationFile = value;
                SaveSettings();
            }
        }

        public bool ManageGoldLoadouts
        {
            get => _manageGoldLoadouts || MoneyPitRunMode;
            set
            {
                if (value == _manageGoldLoadouts) return;
                _manageGoldLoadouts = value;
                SaveSettings();
            }
        }

        public int ResnipeTime
        {
            get => _resnipeTime;
            set
            {
                if (value == _resnipeTime) return;
                _resnipeTime = value;
                SaveSettings();
            }
        }

        public bool[] TitanMoneyDone
        {
            get => _titanMoneyDone;
            set
            {
                if (_titanMoneyDone?.SequenceEqual(value) ?? false) return;
                _titanMoneyDone = value;
                SaveSettings();
            }
        }

        public bool[] TitanGoldTargets
        {
            get => _titanGoldTargets;
            set
            {
                if (_titanGoldTargets?.SequenceEqual(value) ?? false) return;
                _titanGoldTargets = value;
                SaveSettings();
            }
        }

        public bool[] TitanSwapTargets
        {
            get => _titanSwapTargets;
            set
            {
                if (_titanSwapTargets?.SequenceEqual(value) ?? false) return;
                _titanSwapTargets = value;
                SaveSettings();
            }
        }

        public bool GoldSnipeComplete
        {
            get => _goldSnipeComplete;
            set
            {
                if (value == _goldSnipeComplete) return;
                _goldSnipeComplete = value;
                SaveSettings();
            }
        }

        public bool GoldCBlockMode
        {
            get => _goldCBlockMode || MoneyPitRunMode;
            set
            {
                if (value == _goldCBlockMode) return;
                _goldCBlockMode = value;
                SaveSettings();
            }
        }

        public bool AdventureTargetITOPOD
        {
            get => _adventureTargetItopod;
            set
            {
                if (value == _adventureTargetItopod) return;
                _adventureTargetItopod = value;
                SaveSettings();
            }
        }

        public int TitanCombatMode
        {
            get => _titanCombatMode;
            set
            {
                if (value == _titanCombatMode) return;
                _titanCombatMode = value;
                SaveSettings();
            }
        }

        public bool TitanBeastMode
        {
            get => _titanBeastMode;
            set
            {
                if (value == _titanBeastMode) return;
                _titanBeastMode = value;
                SaveSettings();
            }
        }

        public int ITOPODCombatMode
        {
            get => _itopodCombatMode;
            set
            {
                if (value == _itopodCombatMode) return;
                _itopodCombatMode = value;
                SaveSettings();
            }
        }

        public int ITOPODOptimizeMode
        {
            get => _itopodOptimizeMode;
            set
            {
                if (value == _itopodOptimizeMode) return;
                _itopodOptimizeMode = value;
                SaveSettings();
            }
        }

        public bool ITOPODBeastMode
        {
            get => _itopodBeastMode;
            set
            {
                if (value == _itopodBeastMode) return;
                _itopodBeastMode = value;
                SaveSettings();
            }
        }

        public bool ITOPODAutoPush
        {
            get => _itopodAutoPush;
            set
            {
                if (value == _itopodAutoPush) return;
                _itopodAutoPush = value;
                SaveSettings();
            }
        }

        public bool DisableOverlay
        {
            get => _disableOverlay;
            set
            {
                if (value == _disableOverlay) return;
                _disableOverlay = value;
                SaveSettings();
            }
        }

        public bool MoneyPitRunMode
        {
            get => _moneyPitRunMode;
            set
            {
                if (value == _moneyPitRunMode) return;
                _moneyPitRunMode = value;
                SaveSettings();
            }
        }

        public int YggSwapThreshold
        {
            get => _yggSwapThreshold;
            set
            {
                if (value == _yggSwapThreshold) return;
                _yggSwapThreshold = value;
                SaveSettings();
            }
        }

        public int[] SpecialBoostBlacklist
        {
            get => _specialBoostBlacklist;
            set
            {
                if (_specialBoostBlacklist != null && _specialBoostBlacklist.SequenceEqual(value)) return;
                _specialBoostBlacklist = value;
                SaveSettings();
            }
        }

        public int[] BlacklistedBosses
        {
            get => _blacklistedBosses;
            set
            {
                if (_blacklistedBosses?.SequenceEqual(value) ?? false) return;
                _blacklistedBosses = value;
                SaveSettings();
                CombatManager.UpdateBlacklists();
            }
        }

        public bool ManageMayo
        {
            get => _manageMayo;
            set
            {
                if (value == _manageMayo) return;
                _manageMayo = value;
                SaveSettings();
            }
        }

        public bool TrashCards
        {
            get => _trashCards;
            set
            {
                if (value == _trashCards) return;
                _trashCards = value;
                SaveSettings();
            }
        }

        public bool AutoCastCards
        {
            get => _autoCastCards;
            set
            {
                if (value == _autoCastCards) return;
                _autoCastCards = value;
                SaveSettings();
            }
        }

        public bool CastProtectedCards
        {
            get => _castProtectedCards;
            set
            {
                if (value == _castProtectedCards) return;
                _castProtectedCards = value;
                SaveSettings();
            }
        }

        public bool TrashProtectedCards
        {
            get => _trashProtectedCards;
            set
            {
                if (value == _trashProtectedCards) return;
                _trashProtectedCards = value;
                SaveSettings();
            }
        }

        public int[] CardRarities
        {
            get => _cardRarities;
            set
            {
                if (value == _cardRarities) return;
                _cardRarities = value;
                SaveSettings();
            }
        }

        public void SetCardRarity(int index, int cardRarity)
        {
            if (cardRarity == _cardRarities[index]) return;
            _cardRarities[index] = cardRarity;
            SaveSettings();
        }

        public int[] CardCosts
        {
            get => _cardCosts;
            set
            {
                if (value == _cardCosts) return;
                _cardCosts = value;
                SaveSettings();
            }
        }

        public void SetCardCost(int index, int cardTier)
        {
            if (cardTier == _cardCosts[index]) return;
            _cardCosts[index] = cardTier;
            SaveSettings();
        }

        public string[] CardSortOrder
        {
            get => _cardSortOrder;
            set
            {
                if (value == _cardSortOrder) return;
                _cardSortOrder = value;
                SaveSettings();
            }
        }

        public string[] BoostPriority
        {
            get => _boostPriority;
            set
            {
                if (value == _boostPriority) return;
                _boostPriority = value;
                SaveSettings();
            }
        }

        public bool CardSortEnabled
        {
            get => _cardSortEnabled;
            set
            {
                if (value == _cardSortEnabled) return;
                _cardSortEnabled = value;
                SaveSettings();
            }
        }

        public bool NeedsGoldSwap()
        {
            for (var i = 0; i < TitanSwapTargets.Length; i++)
            {
                if (!TitanSwapTargets[i])
                    continue;

                if (TitanSwapTargets[i] && !TitanMoneyDone[i])
                    return true;
            }

            return false;
        }

        public bool HackAdvance
        {
            get => _hackAdvance;
            set
            {
                if (value == _hackAdvance) return;
                _hackAdvance = value;
                SaveSettings();
            }
        }

        public bool ManageCooking
        {
            get => _manageCooking;
            set
            {
                if (value == _manageCooking) return;
                _manageCooking = value;
                SaveSettings();
            }
        }

        public bool ManageQuestLoadouts
        {
            get => _manageQuestLoadouts;
            set
            {
                if (value == _manageQuestLoadouts) return;
                _manageQuestLoadouts = value;
                SaveSettings();
            }
        }

        public bool ManageCookingLoadouts
        {
            get => _manageCookingLoadouts;
            set
            {
                if (value == _manageCookingLoadouts) return;
                _manageCookingLoadouts = value;
                SaveSettings();
            }
        }

        public int[] QuestLoadout
        {
            get => _questLoadout;
            set
            {
                _questLoadout = value;
                SaveSettings();
            }
        }

        public int[] CookingLoadout
        {
            get => _cookingLoadout;
            set
            {
                _cookingLoadout = value;
                SaveSettings();
            }
        }

        public bool AutoBuyConsumables
        {
            get => _autoBuyConsumables;
            set
            {
                if (value == _autoBuyConsumables) return;
                _autoBuyConsumables = value;
                SaveSettings();
            }
        }

        public bool ConsumeIfAlreadyRunning
        {
            get => _consumeIfAlreadyRunning;
            set
            {
                if (value == _consumeIfAlreadyRunning) return;
                _consumeIfAlreadyRunning = value;
                SaveSettings();
            }
        }

        public bool Autosave
        {
            get => _autosave;
            set
            {
                if (value == _autosave) return;
                _autosave = value;
                SaveSettings();
            }
        }

        public bool ManageConsumables
        {
            get => _manageConsumables;
            set
            {
                if (value == _manageConsumables) return;
                _manageConsumables = value;
                SaveSettings();
            }
        }

        public int[] MergeBlacklist
        {
            get => _mergeBlacklist;
            set
            {
                _mergeBlacklist = value;
                SaveSettings();
            }
        }
    }
}
