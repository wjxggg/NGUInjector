using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using static NGUInjector.Main;
using static NGUInjector.Managers.CombatHelpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskBand;

namespace NGUInjector.Managers
{
    internal class CombatManager
    {
        private readonly Character _character;
        private readonly PlayerController _pc;
        private bool _isFighting = false;
        private float _fightTimer = 0;
        private string _enemyName;
        private DateTime move69Cooldown = DateTime.MinValue;

        private enum WalderpCombatMove { Regular, Strong, Piercing, Ultimate }
        private WalderpCombatMove? _forcedMove = null;
        private WalderpCombatMove? _bannedMove = null;

        private bool _holdBlock = false;
        private bool _usedBlock = false;

        private void SetWalderpCombatMove(WalderpCombatMove? combatMove, bool walderpSays)
        {
            //_walderpAttacksTillCommand = 5;
            _forcedMove = walderpSays ? combatMove : null;
            _bannedMove = walderpSays ? null : combatMove;
        }

        public CombatManager()
        {
            _character = Main.Character;
            _pc = Main.PlayerController;
        }

        internal void UpdateFightTimer(float diff)
        {
            _fightTimer += diff;
        }

        bool HasFullHP()
        {
            return Math.Abs(_character.totalAdvHP() - _character.adventure.curHP) < 5;
        }

        float GetHPPercentage()
        {
            return _character.adventure.curHP / _character.totalAdvHP();
        }

        private void DoCombat(bool fastCombat)
        {
            if (!_pc.moveCheck())
                return;

            if (Main.PlayerController.moveTimer > 0)
                return;

            var ac = _character.adventureController;
            var eai = ac.enemyAI;
            var zone = _character.adventure.zone;

            int? numAttacksTillWalderpCommand = null;
            if (ZoneHelpers.ZoneIsWalderp(zone))
            {
                int attackNumber = eai.growCount % 6;
                numAttacksTillWalderpCommand = ((1 - attackNumber) + 6) % 6;

                //Walderp issues a command and sets the inWaldoSaysLoop flag on growCount % 6 == 2
                if (eai.inWaldoSaysLoop)
                {
                    bool waldoSays = eai.waldoSays;
                    switch (eai.waldoAttackID)
                    {
                        case 3:
                            SetWalderpCombatMove(WalderpCombatMove.Regular, waldoSays);
                            break;
                        case 4:
                            SetWalderpCombatMove(WalderpCombatMove.Strong, waldoSays);
                            break;
                        case 5:
                            SetWalderpCombatMove(WalderpCombatMove.Piercing, waldoSays);
                            break;
                        case 6:
                            SetWalderpCombatMove(WalderpCombatMove.Ultimate, waldoSays);
                            break;
                        default: //should never happen
                            SetWalderpCombatMove(WalderpCombatMove.Regular, waldoSays);
                            break;
                    }
                }
                else
                {
                    _forcedMove = null;
                    _bannedMove = null;
                }

                if (_forcedMove.HasValue)
                {
                    switch (_forcedMove.Value)
                    {
                        case WalderpCombatMove.Regular:
                            if (ac.regularAttackMove.button.IsInteractable())
                            {
                                ac.regularAttackMove.doMove();
                            }
                            break;
                        case WalderpCombatMove.Strong:
                            if (ac.strongAttackMove.button.IsInteractable())
                            {
                                ac.strongAttackMove.doMove();
                            }
                            break;
                        case WalderpCombatMove.Piercing:
                            if (ac.pierceMove.button.IsInteractable())
                            {
                                ac.pierceMove.doMove();
                            }
                            break;
                        case WalderpCombatMove.Ultimate:
                            if (ac.ultimateAttackMove.button.IsInteractable())
                            {
                                ac.ultimateAttackMove.doMove();
                            }
                            break;
                        default:
                            break;
                    }
                    return;
                }
            }

            if (ZoneHelpers.ZoneIsGodmother(zone))
            {
                int attackNumber = eai.growCount % 9;
                int numAttacksTillGodmotherExplode = ((2 - attackNumber) + 9) % 9;

                float blockCooldown = _character.blockCooldown();
                //Dont use block if the cooldown would run through the explosion
                int blockThreshold = (int)Math.Floor(blockCooldown / ac.currentEnemy.attackRate);

                _holdBlock = numAttacksTillGodmotherExplode < blockThreshold;

                //Godmother explodes on growCount % 9 == 3
                if (attackNumber == 3)
                {
                    if (!_usedBlock)
                    {
                        if (ac.blockMove.button.IsInteractable())
                        {
                            ac.blockMove.doMove();
                            _usedBlock = true;
                        }
                        return;
                    }
                }
                else
                {
                    _usedBlock = false;
                }
            }

            if (!fastCombat)
            {
                if (CombatBuffs())
                    return;
            }

            if (ZoneHelpers.ZoneIsWalderp(zone))
            {
                WalderpCombatAttacks(fastCombat, numAttacksTillWalderpCommand.Value);
            }
            else
            {
                CombatAttacks(fastCombat);
            }
        }

        private bool CombatBuffs()
        {
            var ac = _character.adventureController;
            var ai = ac.currentEnemy.AI;
            var eai = ac.enemyAI;

            if (ai == AI.charger && eai.GetPV<int>("chargeCooldown") >= 3)
            {
                if (ac.blockMove.button.IsInteractable() && !_pc.isParrying)
                {
                    ac.blockMove.doMove();
                    return true;
                }

                if (ac.parryMove.button.IsInteractable() && !_pc.isBlocking && !_pc.isParrying)
                {
                    ac.parryMove.doMove();
                    return true;
                }
            }

            if (ai == AI.rapid && eai.GetPV<int>("rapidEffect") >= 6)
            {
                if (ac.blockMove.button.IsInteractable())
                {
                    ac.blockMove.doMove();
                    return true;
                }
            }

            if (ai == AI.exploder && ac.currentEnemy.attackRate - eai.GetPV<float>("enemyAttackTimer") < 1)
            {
                if (ac.blockMove.button.IsInteractable())
                {
                    ac.blockMove.doMove();
                    return true;
                }
            }

            if (!ZoneHelpers.ZoneIsTitan(_character.adventure.zone) && ac.currentEnemy.curHP / ac.currentEnemy.maxHP < .2)
            {
                return false;
            }

            if (OhShitUnlocked() && GetHPPercentage() < .5 && OhShitReady())
            {
                if (CastOhShit())
                {
                    return true;
                }
            }

            if (GetHPPercentage() < .75)
            {
                if (CastHeal())
                {
                    return true;
                }
            }

            if (GetHPPercentage() < .6 && !HealReady())
            {
                if (CastHyperRegen())
                {
                    return true;
                }
            }

            if (CastMegaBuff())
            {
                return true;
            }

            if (!MegaBuffUnlocked())
            {
                if (!DefenseBuffActive())
                {
                    if (CastUltimateBuff())
                    {
                        return true;
                    }
                }

                if (UltimateBuffActive())
                {
                    if (CastOffensiveBuff())
                        return true;
                }

                if (GetHPPercentage() < .75 && !UltimateBuffActive() && !BlockActive())
                {
                    if (CastDefensiveBuff())
                        return true;
                }
            }

            if (ai != AI.charger && ai != AI.rapid && ai != AI.exploder && (Settings.MoreBlockParry || !UltimateBuffActive() && !DefenseBuffActive()))
            {
                if (!ParryActive() && !BlockActive() && !_holdBlock)
                {
                    if (CastBlock())
                    {
                        return true;
                    }
                }

                if (!BlockActive() && !ParryActive())
                {
                    if (CastParry())
                        return true;
                }
            }

            if (_pc.isBlocking || _pc.isParrying)
            {
                return false;
            }

            if (CastParalyze(ai, eai))
                return true;


            if (ChargeReady())
            {
                if (UltimateAttackReady())
                {
                    if (CastCharge())
                        return true;
                }

                if (GetUltimateAttackCooldown() > .45 && PierceReady())
                {
                    if (CastCharge())
                        return true;
                }
            }

            return false;
        }

        //private bool ParalyzeBoss()
        //{
        //    var ac = _character.adventureController;
        //    var ai = ac.currentEnemy.AI;
        //    var eai = ac.enemyAI;

        //    if (!ac.paralyzeMove.button.IsInteractable())
        //        return false;

        //    if (GetHPPercentage() < .2)
        //        return false;

        //    if (UltimateBuffActive())
        //        return false;

        //    if (ai == AI.charger && eai.GetPV<int>("chargeCooldown") == 0)
        //    {
        //        ac.paralyzeMove.doMove();
        //        return true;
        //    }

        //    if (ai == AI.rapid && eai.GetPV<int>("rapidEffect") < 5)
        //    {
        //        ac.paralyzeMove.doMove();
        //        return true;
        //    }

        //    if (ai != AI.rapid && ai != AI.charger)
        //    {
        //        ac.paralyzeMove.doMove();
        //        return true;
        //    }

        //    return false;
        //}

        private void WalderpCombatAttacks(bool fastCombat, int numAttacksTillWalderpCommand)
        {
            var ac = _character.adventureController;

            float walderpAttackRate = ac.currentEnemy.attackRate;
            float pierceCooldown = _character.pierceAttackCooldown();
            float ultCooldown = _character.ultimateAttackCooldown();

            //Dont use ultimate or pierce attacks if the cooldown would run through a command from walderp
            int ultThreshold = (int)Math.Floor(ultCooldown / walderpAttackRate);
            int pierceThreshold = (int)Math.Floor(pierceCooldown / walderpAttackRate);

            if (numAttacksTillWalderpCommand >= ultThreshold)
            {
                CombatAttacks(fastCombat);
            }
            else
            {
                if (ac.pierceMove.button.IsInteractable() && numAttacksTillWalderpCommand >= pierceThreshold)
                {
                    ac.pierceMove.doMove();
                    return;
                }
                
                if (ac.strongAttackMove.button.IsInteractable())
                {
                    ac.strongAttackMove.doMove();
                    return;
                }
                
                if (ac.regularAttackMove.button.IsInteractable())
                {
                    ac.regularAttackMove.doMove();
                    return;
                }
            }
        }

        private void CombatAttacks(bool fastCombat)
        {
            var ac = _character.adventureController;

            if (ac.ultimateAttackMove.button.IsInteractable() && _bannedMove != WalderpCombatMove.Ultimate)
            {
                if (fastCombat || ChargeActive() || GetChargeCooldown() > .45)
                {
                    ac.ultimateAttackMove.doMove();
                    return;
                }
            }

            if (ac.pierceMove.button.IsInteractable() && _bannedMove != WalderpCombatMove.Piercing)
            {
                ac.pierceMove.doMove();
                return;
            }

            if (Settings.DoMove69 && Move69Ready() && Move69CooldownReady())
            {
                Main.PlayerController.move69();
                move69Cooldown = DateTime.Now;
            }

            if (ac.strongAttackMove.button.IsInteractable() && _bannedMove != WalderpCombatMove.Strong)
            {
                ac.strongAttackMove.doMove();
                return;
            }

            if (ac.regularAttackMove.button.IsInteractable() && _bannedMove != WalderpCombatMove.Regular)
            {
                ac.regularAttackMove.doMove();
                return;
            }
        }

        //private List<string> GetLog()
        //{
        //    List<string> log = new List<string>();

        //    var bLog = _character.adventureController.log;
        //    var type = bLog.GetType().GetField("Eventlog",
        //        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //    var val = type?.GetValue(bLog);

        //    if (val != null)
        //    {
        //        log = (List<string>)val;
        //    }

        //    return log;
        //}

        //private void ParseLogForWalderp()
        //{
        //    var log = GetLog();
        //    for (var i = log.Count - 1; i >= 0; i--)
        //    {
        //        var line = log[i];

        //        if (!line.StartsWith("<color=red>")) continue;
        //        if (line.EndsWith("<b></b>")) continue;

        //        //LogDebug(line);

        //        if (line.StartsWith("<color=red>HIT ME"))
        //        {
        //            //LogDebug($"Banning {GetWalderpCombatMove(line)}");
        //            SetWalderpBannedMove(GetWalderpCombatMove(line));

        //            log[i] = $"{line}<b></b>";

        //            break;
        //        }
        //        else if (line.StartsWith("<color=red>WALDERP SAYS"))
        //        {
        //            //LogDebug($"Forcing {GetWalderpCombatMove(line)}");
        //            SetWalderpForcedMove(GetWalderpCombatMove(line));

        //            log[i] = $"{line}<b></b>";

        //            break;
        //        }
        //        else if (line.StartsWith("<color=red>WALDERP "))
        //        {
        //            _walderpAttacksTillCommand--;
        //            //LogDebug($"Walderp Attacks Left: {_walderpAttacksTillCommand}");

        //            log[i] = $"{line}<b></b>";

        //            break;
        //        }
        //        else
        //        {
        //            log[i] = $"{line}<b></b>";
        //        }
        //    }
        //}

        //private void ParseLogForGodmother()
        //{
        //    var log = GetLog();
        //    for (var i = log.Count - 1; i >= 0; i--)
        //    {
        //        var line = log[i];

        //        if (!line.StartsWith("<color=red>")) continue;
        //        if (line.EndsWith("<b></b>")) continue;

        //        //LogDebug(line);

        //        if (line.StartsWith("<color=red>The Godmother"))
        //        {
        //            //These two lines count as normal attacks before the explosion (first explosion is after x3 attacks, then x9 attacks between each explosion)
        //            //The Godmother V1 <attack string> for xxxx damage!
        //            //The Godmother orders one of her goons to bash your knees with a baseball bat! OWIE OW OW!!
        //            if (line.EndsWith("damage!") || line.Contains("orders one of her goons"))
        //            {
        //                _godmotherAttacksTillExplosion--;
        //            }

        //            //Explosion text
        //            //The Godmother V1 starts glowing white! HIT THE FREAKIN' DECK!
        //            else if (line.Contains("glowing white!"))
        //            {
        //                _forceBlock = true;
        //                _holdBlock = false;
        //            }
        //            //The Godmother V1 STARTS EXPLODING LIKE CRAZY!!!
        //            //The Godmother V1 EXPLODES FOR xxxx DAMAGE! (x4)
        //            else if (line.EndsWith("DAMAGE!") || line.EndsWith("STARTS EXPLODING LIKE CRAZY!"))
        //            {
        //                _godmotherAttacksTillExplosion = 9;
        //                _forceBlock = false;
        //                _holdBlock = false;
        //            }

        //            log[i] = $"{line}<b></b>";

        //            break;
        //        }
        //        else
        //        {
        //            log[i] = $"{line}<b></b>";
        //        }
        //    }
        //}

        //private WalderpCombatMove? GetWalderpCombatMove(string line)
        //{
        //    WalderpCombatMove? move = null;

        //    if (line.Contains("REGULAR ATTACK"))
        //    {
        //        move = WalderpCombatMove.Regular;
        //    }
        //    else if (line.Contains("STRONG ATTACK"))
        //    {
        //        move = WalderpCombatMove.Strong;
        //    }
        //    else if (line.Contains("PIERCING ATTACK"))
        //    {
        //        move = WalderpCombatMove.Piercing;
        //    }
        //    else if (line.Contains("ULTIMATE ATTACK"))
        //    {
        //        move = WalderpCombatMove.Ultimate;
        //    }

        //    return move;
        //}

        internal bool Move69CooldownReady()
        {
            TimeSpan ts = (DateTime.Now - move69Cooldown);
            if (ts.TotalMilliseconds > (1000 * 60 * 60)) // cooldown: 3600s or 1 hour
            {
                return true;
            }
            return false;
        }

        internal static bool IsZoneUnlocked(int zone)
        {
            return zone <= Main.Character.adventureController.zoneDropdown.options.Count - 2;
        }

        internal void MoveToZone(int zone)
        {
            _forcedMove = null;
            _bannedMove = null;

            _holdBlock = false;
            _usedBlock = false;

            if (_character.adventure.zone != zone)
            {
                _isFighting = false;
                _fightTimer = 0;
            }
            CurrentCombatZone = zone;
            _character.adventureController.zoneSelector.changeZone(zone);
        }

        private bool CheckBeastModeToggle(bool beastMode, out bool beastModeWasToggled)
        {
            beastModeWasToggled = false;
            //Beast mode checks
            if (_character.adventureController.hasBeastMode())
            {
                bool canToggleBeastMode = _character.adventureController.beastModeMove.button.interactable;
                bool isBeastModeActive = _character.adventure.beastModeOn;

                bool needToToggle = isBeastModeActive != beastMode;

                //If the button is inaccessible, we need to stay in manual mode until we can press it
                if (needToToggle)
                {
                    if (canToggleBeastMode)
                    {
                        _character.adventureController.beastModeMove.doMove();
                        beastModeWasToggled = true;
                    }
                    return true;
                }
            }

            return false;
        }

        internal void IdleZone(int zone, bool bossOnly, bool recoverHealth, bool beastMode)
        {
            if (zone == -1 && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
                return;
            }

            //If we need to toggle beast mode, wait in the safe zone in manual mode until beast mode is enabled
            if (CheckBeastModeToggle(beastMode, out bool beastModeWasToggled))
            {
                if (!beastModeWasToggled)
                {
                    if (_character.adventure.zone != -1)
                    {
                        MoveToZone(-1);
                    }
                    if (_character.adventure.autoattacking)
                    {
                        _character.adventureController.idleAttackMove.setToggle();
                    }
                }
                return;
            }

            //Enable idle attack if its not on
            if (!_character.adventure.autoattacking)
            {
                _character.adventureController.idleAttackMove.setToggle();
                return;
            }

            if (_character.adventure.zone == -1 && !HasFullHP() && recoverHealth)
            {
                return;
            }

            //Check if we're in not in the right zone and not in safe zone, if not move to safe zone first
            if (_character.adventure.zone != zone && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
            }

            //Move to the zone
            if (_character.adventure.zone != zone)
            {
                MoveToZone(zone);
                return;
            }

            //Wait for an enemy to spawn
            if (_character.adventureController.currentEnemy == null)
            {
                if (_isFighting)
                {
                    _isFighting = false;
                    if (_fightTimer > 1)
                        LogCombat($"{_enemyName} killed in {_fightTimer:00.0}s");

                    _fightTimer = 0;
                    if (LoadoutManager.CurrentLock == LockType.Gold)
                    {
                        Log("Gold Loadout kill done. Turning off setting and swapping gear");
                        Settings.DoGoldSwap = false;
                        LoadoutManager.RestoreGear();
                        LoadoutManager.ReleaseLock();
                        MoveToZone(-1);
                        return;
                    }

                    if (recoverHealth && !HasFullHP())
                    {
                        MoveToZone(-1);
                        return;
                    }
                }
                _fightTimer = 0;
                return;
            }

            if (zone < 1000 && Settings.BlacklistedBosses.Contains(_character.adventureController.currentEnemy.spriteID))
            {
                MoveToZone(-1);
                MoveToZone(zone);
                return;
            }

            //We have an enemy. Lets check if we're in bossOnly mode
            if (bossOnly && zone < 1000)
            {
                var ec = _character.adventureController.currentEnemy.enemyType;
                if (ec != enemyType.boss && !ec.ToString().Contains("bigBoss"))
                {
                    MoveToZone(-1);
                    MoveToZone(zone);
                    return;
                }
            }

            _isFighting = true;
            _enemyName = _character.adventureController.currentEnemy.name;
        }

        internal void ManualZone(int zone, bool bossOnly, bool recoverHealth, bool precastBuffs, bool fastCombat, bool beastMode)
        {
            if (zone == -1 && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
                return;
            }

            //If we havent unlocked any attacks yet, use the Idle loop, otherwise turn off idle mode
            if (_character.training.attackTraining[1] == 0)
            {
                IdleZone(zone, bossOnly, recoverHealth, beastMode);
                return;
            }
            else if (_character.adventure.autoattacking)
            {
                _character.adventureController.idleAttackMove.setToggle();
            }

            //If we need to toggle beast mode, just do the normal combat loop until the cooldown is ready
            CheckBeastModeToggle(beastMode, out bool beastModeWasToggled);
            if (beastModeWasToggled)
            {
                return;
            }

            //if (_character.adventure.beastModeOn && !beastMode && _character.adventureController.beastModeMove.button.interactable)
            //{
            //    _character.adventureController.beastModeMove.doMove();
            //    return;
            //}

            //if (!_character.adventure.beastModeOn && beastMode &&
            //    _character.adventureController.beastModeMove.button.interactable)
            //{
            //    _character.adventureController.beastModeMove.doMove();
            //    return;
            //}

            //Move back to safe zone if we're in the wrong zone
            if (_character.adventure.zone != zone && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
            }

            //If precast buffs is true and we have no enemy and charge isn't active, go back to safe zone
            if (precastBuffs && !ChargeActive() && _character.adventureController.currentEnemy == null)
            {
                MoveToZone(-1);
            }

            //If we're in safe zone, recover health if needed. Also precast buffs
            if (_character.adventure.zone == -1)
            {
                if (precastBuffs)
                {
                    if (ChargeUnlocked() && !ChargeActive())
                    {
                        if (CastCharge()) return;
                    }

                    if (ParryUnlocked() && !ParryActive())
                    {
                        if (CastParry()) return;
                    }

                    //Wait for Charge to be ready again, as well as other buffs
                    if (ChargeUnlocked() && !ChargeReady()) return;
                    if (ParryUnlocked() && !ParryReady()) return;
                    if (MegaBuffUnlocked() && !MegaBuffReady()) return;
                    if (UltimateBuffUnlocked() && !UltimateBuffReady()) return;
                    if (DefensiveBuffUnlocked() && !DefensiveBuffReady()) return;
                }

                if (recoverHealth && !HasFullHP())
                {
                    if (ChargeUnlocked() && !ChargeActive())
                    {
                        if (CastCharge()) return;
                    }

                    if (ParryUnlocked() && !ParryActive())
                    {
                        if (CastParry()) return;
                    }
                    return;
                }
            }

            //Move to the zone
            if (_character.adventure.zone != zone)
            {
                MoveToZone(zone);
                return;
            }

            //Wait for an enemy to spawn
            if (_character.adventureController.currentEnemy == null)
            {
                if (_isFighting)
                {
                    _isFighting = false;
                    if (_fightTimer > 1)
                        LogCombat($"{_enemyName} killed in {_fightTimer:00.0}s");

                    _fightTimer = 0;
                    if (LoadoutManager.CurrentLock == LockType.Gold)
                    {
                        Log("Gold Loadout kill done. Turning off setting and swapping gear");
                        Settings.DoGoldSwap = false;
                        LoadoutManager.RestoreGear();
                        LoadoutManager.ReleaseLock();
                        MoveToZone(-1);
                        return;
                    }

                    if (precastBuffs || recoverHealth && !HasFullHP())
                    {
                        MoveToZone(-1);
                        return;
                    }
                }
                _fightTimer = 0;
                if (!precastBuffs && bossOnly)
                {
                    if (!ChargeActive())
                    {
                        if (CastCharge())
                        {
                            return;
                        }
                    }

                    if (!ParryActive())
                    {
                        if (CastParry())
                        {
                            return;
                        }
                    }

                    if (GetHPPercentage() < .75)
                    {
                        if (CastHeal())
                            return;
                    }
                }

                if (fastCombat)
                {
                    if (GetHPPercentage() < .75)
                    {
                        if (CastHeal())
                            return;
                    }

                    if (GetHPPercentage() < .60)
                    {
                        if (CastHyperRegen())
                            return;
                    }
                }


                return;
            }

            if (zone < 1000 && Settings.BlacklistedBosses.Contains(_character.adventureController.currentEnemy.spriteID))
            {
                MoveToZone(-1);
                MoveToZone(zone);
                return;
            }

            //We have an enemy. Lets check if we're in bossOnly mode
            if (bossOnly && zone < 1000)
            {
                var ec = _character.adventureController.currentEnemy.enemyType;
                if (ec != enemyType.boss && !ec.ToString().Contains("bigBoss"))
                {
                    MoveToZone(-1);
                    MoveToZone(zone);
                    return;
                }
            }

            _isFighting = true;
            _enemyName = _character.adventureController.currentEnemy.name;
            //We have an enemy and we're ready to fight. Run through our combat routine
            if (_character.training.attackTraining[1] > 0)
                DoCombat(fastCombat);
        }
    }
}
