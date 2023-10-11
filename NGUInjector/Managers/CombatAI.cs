using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static NGUInjector.Main;
using static NGUInjector.Managers.CombatHelpers;

namespace NGUInjector.Managers
{
    internal class CombatAI
    {
        private readonly Character _character;
        private readonly AdventureController _ac;
        private readonly EnemyAI _eai;
        private readonly Enemy _enemy;
        private readonly int _zone;

        private readonly bool _fastCombat;
        private readonly bool _smartBeastMode;
        private readonly bool _moreBlockParry;

        private readonly float _globalMoveCooldown;

        private readonly float _blockRemainingCooldown;
        private readonly bool _blockIsActive;
        private readonly float? _blockTimeLeft;
        private readonly bool _willBlockNextAttack;
        private readonly bool _willOnlyBlockNextAttack;

        private readonly float _parryRemainingCooldown;

        private readonly float _paralyzeRemainingCooldown;
        private readonly bool _enemyIsParalyzed;
        private readonly float? _paralyzeTimeLeft;
        private readonly bool _useOhShitInsteadOfParalyze;

        private readonly float _enemyAttackRate;
        private readonly float _enemyAttackTimer;
        private readonly float _timeTillAttack;

        private readonly int _attacksToBlock;
        private readonly float _optimalTimeToBlock;

        private CombatSnapshot _combatSnapshot = null;

        private class CombatSnapshot
        {
            private readonly int _loopSize;
            private readonly int _attackNumber;
            private readonly int _specialMoveNumber;
            private readonly int? _warningMoveNumber;

            public float? TimeTillSpecialMove { get; } = null;
            public int? SpecialMoveInNumAttacks { get; } = null;
            public bool NextAttackSpecialMove { get; } = false;
            public bool NextAttackNoDamage { get; } = false;
            public bool AttackAfterNextNoDamage { get; } = false;
            public float TimeTillNextDamagingAttack { get; } = 0f;

            public bool EnemyHasBlockableSpecialMove { get; } = false;
            public bool HoldBlock { get; set; } = false;

            public enum WalderpCombatMove { Regular, Strong, Piercing, Ultimate }
            public WalderpCombatMove? ForcedMove { get; private set; } = null;
            public WalderpCombatMove? BannedMove { get; private set; } = null;

            public CombatSnapshot(float timeTillNextAttack)
            {
                TimeTillNextDamagingAttack = timeTillNextAttack;
            }

            public CombatSnapshot(float enemyAttackRate, float timeTillNextAttack, int loopSize, int attackNumber, int specialMoveNumber, int? warningMoveNumber)
            {
                _loopSize = loopSize;
                _attackNumber = attackNumber % loopSize;
                _specialMoveNumber = specialMoveNumber;
                _warningMoveNumber = warningMoveNumber;

                SpecialMoveInNumAttacks = ((((_specialMoveNumber - 1) - _attackNumber) + _loopSize) % _loopSize) + 1;
                TimeTillSpecialMove = ((SpecialMoveInNumAttacks - 1) * enemyAttackRate) + timeTillNextAttack;
                NextAttackSpecialMove = SpecialMoveInNumAttacks == 1;

                int attacksBetweenSpecialAndWarning = (!_warningMoveNumber.HasValue || _warningMoveNumber >= _specialMoveNumber) ? 0 : _specialMoveNumber - _warningMoveNumber.Value;
                NextAttackNoDamage = _warningMoveNumber.HasValue && SpecialMoveInNumAttacks <= attacksBetweenSpecialAndWarning + 1 && SpecialMoveInNumAttacks > 1;
                AttackAfterNextNoDamage = _warningMoveNumber.HasValue && SpecialMoveInNumAttacks <= attacksBetweenSpecialAndWarning + 2 && SpecialMoveInNumAttacks > 2;

                TimeTillNextDamagingAttack = NextAttackNoDamage ? TimeTillSpecialMove.Value : timeTillNextAttack;

                EnemyHasBlockableSpecialMove = true;
                HoldBlock = NextAttackNoDamage || AttackAfterNextNoDamage;
            }

            public void SetWalderpCombatMove(WalderpCombatMove? combatMove, bool walderpSays)
            {
                ForcedMove = walderpSays ? combatMove : null;
                BannedMove = walderpSays ? null : combatMove;
            }

            public void ClearWalderpCombatMove()
            {
                ForcedMove = null;
                BannedMove = null;
            }
        }

        public CombatAI(Character character, bool fastCombat, bool smartBeastMode)
        {
            //LogDebug($"");
            _character = character;
            _ac = _character.adventureController;
            _eai = _ac.enemyAI;
            _enemy = _ac.currentEnemy;
            _zone = _character.adventure.zone;

            _fastCombat = fastCombat;
            _smartBeastMode = smartBeastMode;
            _moreBlockParry = ZoneHelpers.ZoneIsTitan(_zone) ? Settings.TitanMoreBlockParry : Settings.MoreBlockParry;

            _globalMoveCooldown = (_character.inventory.itemList.redLiquidComplete ? 0.8f : 1.0f) + 0.1f;

            var bm = _ac.blockMove;
            var bmfi = bm.GetType().GetField("blockTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _blockRemainingCooldown = _character.blockCooldown() - Mathf.Min(_character.blockCooldown(), (float)bmfi?.GetValue(bm));

            var pm = _ac.parryMove;
            var pmfi = pm.GetType().GetField("parryTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _parryRemainingCooldown = _character.parryCooldown() - Mathf.Min(_character.parryCooldown(), (float)pmfi?.GetValue(pm));

            var parm = _ac.paralyzeMove;
            var parfi = parm.GetType().GetField("attackTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _paralyzeRemainingCooldown = _character.paralyzeCooldown() - Mathf.Min(_character.paralyzeCooldown(), (float)parfi?.GetValue(parm));

            _enemyIsParalyzed = EnemyIsParalyzed(_eai, out _paralyzeTimeLeft);
            _useOhShitInsteadOfParalyze = OhShitUnlocked() && OhShitReady() && GetHPPercentage() < 0.6f;

            _enemyAttackTimer = _eai.GetPV<float>("enemyAttackTimer");
            _enemyAttackRate = _enemy.attackRate;

            //GM is a bit quirky, she resets attack timer at 5x the rate while exploding, but only does so 4 times, so the time to the next "attack" is only 80% of normal
            if (ZoneHelpers.ZoneIsGodmother(_zone) && _eai.explosionMode)
            {
                _enemyAttackRate = _enemyAttackRate * 0.8f;
            }

            _timeTillAttack = (_enemyAttackRate - _enemyAttackTimer) + (_paralyzeTimeLeft ?? 0f);

            //Normal zones initial strike is slower than others
            if (!ZoneHelpers.ZoneIsTitan(_zone) && _eai.GetPV<bool>("firstStrike"))
            {
                _timeTillAttack += _enemyAttackRate * 0.5f;
            }

            _attacksToBlock = (int)Math.Ceiling(2.9f / _enemyAttackRate);
            _optimalTimeToBlock = 2.9f - (_enemyAttackRate * (_attacksToBlock - 1));

            _blockIsActive = BlockActive(out _blockTimeLeft);
            //LogDebug($"BlockActive:{_blockIsActive} | TimeLeft:{_blockTimeLeft}");
            _willBlockNextAttack = _blockIsActive && (_blockTimeLeft ?? 0) > _timeTillAttack;
            _willOnlyBlockNextAttack = _willBlockNextAttack && (_blockTimeLeft ?? 0) < (_timeTillAttack + _enemyAttackRate);

            SetCombatSnapshot();

            if (_combatSnapshot == null)
            {
                _combatSnapshot = new CombatSnapshot(_timeTillAttack);
            }
        }

        private void SetCombatSnapshot()
        {
            if (ZoneHelpers.ZoneIsWalderp(_zone))
            {
                //Walderp calls out his move on 2 with no damage, and kills you on 3 if the conditions are not met
                //This works differently to the other "blockable" special moves and should be handled separately
                _combatSnapshot = new CombatSnapshot(_enemyAttackRate, _timeTillAttack, 6, _eai.growCount, 3, 2);
            }
            else if (ZoneHelpers.ZoneIsNerd(_zone))
            {
                //Nerd does a damaging warning on 3 and a big attack on 4
                _combatSnapshot = new CombatSnapshot(_enemyAttackRate, _timeTillAttack, 8, _eai.growCount, 4, null);
            }
            else if (ZoneHelpers.ZoneIsGodmother(_zone))
            {
                //GM does a damaging warning on 3 and a big attack on 4
                _combatSnapshot = new CombatSnapshot(_enemyAttackRate, _timeTillAttack, 9, _eai.growCount, 4, null);
            }
            else if (ZoneHelpers.ZoneIsExile(_zone))
            {
                int loopSize;
                int specialMoveNumber;
                int? warningMoveNumber;

                //TODO: Tested with ExileV1 who has an attack rate of 2.1, need to test with ExileV2-4 with attack rates of 2.0-1.8

                //Exile V1 warns on 3 with no damage and uses a big special move on 4
                //V2 does an unblockable special on 3 so may as well keep the V1 logic
                //V3 (and V4) can do an attack which should be blocked on 3, so move the special back to 3 with no "warning"
                //This should make it so Block runs through moves 3 and 4 and will cover all V1-V4 scenarios
                switch (_enemy.enemyType)
                {
                    case enemyType.bigBoss9V1:
                        loopSize = 10;
                        specialMoveNumber = 4;
                        warningMoveNumber = 3;
                        break;
                    case enemyType.bigBoss9V2:
                        loopSize = 9;
                        specialMoveNumber = 4;
                        warningMoveNumber = 3;
                        break;
                    case enemyType.bigBoss9V3:
                        loopSize = 8;
                        specialMoveNumber = 3;
                        warningMoveNumber = null;
                        break;
                    case enemyType.bigBoss9V4:
                        loopSize = 7;
                        specialMoveNumber = 3;
                        warningMoveNumber = null;
                        break;
                    default:
                        return;
                }

                _combatSnapshot = new CombatSnapshot(_enemyAttackRate, _timeTillAttack, loopSize, _eai.growCount, specialMoveNumber, warningMoveNumber);
            }
            else if (!ZoneHelpers.ZoneIsTitan(_zone))
            {
                if (_enemy.AI == AI.charger)
                {
                    _combatSnapshot = new CombatSnapshot(_enemyAttackRate, _timeTillAttack, 6, _eai.GetPV<int>("chargeCooldown"), 5, 3);
                }
                else if (_enemy.AI == AI.rapid)
                {
                    _combatSnapshot = new CombatSnapshot(_enemyAttackRate, _timeTillAttack, 15, _eai.GetPV<int>("rapidEffect"), 8, 5);
                }
                else if (_enemy.AI == AI.exploder)
                {
                    _combatSnapshot = new CombatSnapshot(_enemyAttackRate, _timeTillAttack, 1, 1, 1, null);
                }
            }
        }

        public bool DoPreCombat()
        {
            if (ZoneHelpers.ZoneIsWalderp(_zone))
            {
                return WalderpPreCombat();
            }
            else if (_combatSnapshot.EnemyHasBlockableSpecialMove)
            {
                return BlockableEnemyPreCombat();
            }

            return false;
        }

        public bool DoCombatBuffs()
        {
            if (!_fastCombat)
            {
                if (ApplyCombatBuffs())
                {
                    return true;
                }

                //SmartBeastMode routine, turn on Beast Mode when a defensive buff is active and only if it can be turned off before the buff expires
                if (_smartBeastMode && BeastModeUnlocked() && BeastModeReady() && DoSmartBeastMode())
                {
                    return true;
                }
            }

            return false;
        }

        public void DoCombat()
        {
            if (ZoneHelpers.ZoneIsWalderp(_zone))
            {
                WalderpCombatAttacks();
            }
            else
            {
                //Simulate an attack for testing defensive CD use
                //LogDebug($"\tAttack");
                //Main.PlayerController.canUseMove = false;
                //Main.PlayerController.moveTimer = 0.8f;
                CombatAttacks();
            }
        }

        public bool WalderpPreCombat()
        {
            //Walderp issues a command and sets the inWaldoSaysLoop flag on attackNumber 2
            //If he issues a waldoSays command that attack must be used by attackNumber 3 
            //If he issues a !waldoSays command any attack aside from that one must be used by attackNumber 3 
            if (_eai.inWaldoSaysLoop)
            {
                bool waldoSays = _eai.waldoSays;
                switch (_eai.waldoAttackID)
                {
                    case 3:
                        _combatSnapshot.SetWalderpCombatMove(CombatSnapshot.WalderpCombatMove.Regular, waldoSays);
                        break;
                    case 4:
                        _combatSnapshot.SetWalderpCombatMove(CombatSnapshot.WalderpCombatMove.Strong, waldoSays);
                        break;
                    case 5:
                        _combatSnapshot.SetWalderpCombatMove(CombatSnapshot.WalderpCombatMove.Piercing, waldoSays);
                        break;
                    case 6:
                        _combatSnapshot.SetWalderpCombatMove(CombatSnapshot.WalderpCombatMove.Ultimate, waldoSays);
                        break;
                    default: //should never happen
                        _combatSnapshot.SetWalderpCombatMove(CombatSnapshot.WalderpCombatMove.Regular, waldoSays);
                        break;
                }
            }
            else
            {
                _combatSnapshot.ClearWalderpCombatMove();
            }

            if (_combatSnapshot.ForcedMove.HasValue)
            {
                switch (_combatSnapshot.ForcedMove.Value)
                {
                    case CombatSnapshot.WalderpCombatMove.Regular:
                        if (_ac.regularAttackMove.button.IsInteractable())
                        {
                            _ac.regularAttackMove.doMove();
                        }
                        break;
                    case CombatSnapshot.WalderpCombatMove.Strong:
                        if (_ac.strongAttackMove.button.IsInteractable())
                        {
                            _ac.strongAttackMove.doMove();
                        }
                        break;
                    case CombatSnapshot.WalderpCombatMove.Piercing:
                        if (_ac.pierceMove.button.IsInteractable())
                        {
                            _ac.pierceMove.doMove();
                        }
                        break;
                    case CombatSnapshot.WalderpCombatMove.Ultimate:
                        if (_ac.ultimateAttackMove.button.IsInteractable())
                        {
                            _ac.ultimateAttackMove.doMove();
                        }
                        break;
                    default:
                        break;
                }
                return true;
            }
            else if (_combatSnapshot.BannedMove.HasValue)
            {
                CombatAttacks();
                return true;
            }

            return false;
        }

        public bool BlockableEnemyPreCombat()
        {
            //LogDebug($"AI:{_enemy.AI} | SpecialIn:{_combatSnapshot.SpecialMoveInNumAttacks} | TimeToSpecial:{_combatSnapshot.TimeTillSpecialMove} | TimeToAttack:{_timeTillAttack}");
            //LogDebug($"BlockCD:{_blockRemainingCooldown} | ParalyzeCD:{_paralyzeRemainingCooldown} | ParryCD:{_parryRemainingCooldown}");

            bool canBlockBeforeHold = false;
            if (_moreBlockParry && _combatSnapshot.SpecialMoveInNumAttacks > _attacksToBlock && !_combatSnapshot.NextAttackNoDamage && !_combatSnapshot.AttackAfterNextNoDamage && (_blockRemainingCooldown + 0.1f) < _timeTillAttack)
            {
                float projectedTimeLeftOnBlockCooldown = _character.blockCooldown();
                //Amount of time before the first (blocked) attack
                projectedTimeLeftOnBlockCooldown -= Mathf.Min(_timeTillAttack, Mathf.Max(_optimalTimeToBlock, _blockRemainingCooldown));
                //Amount of time for any other blocked attacks
                projectedTimeLeftOnBlockCooldown -= _enemyAttackRate * (_attacksToBlock - 1);
                //Amount of time due to paralyze
                if (ParalyzeUnlocked() && (_paralyzeRemainingCooldown + 0.1f) < _combatSnapshot.TimeTillSpecialMove && (projectedTimeLeftOnBlockCooldown - _paralyzeRemainingCooldown) > 2.0f)
                {
                    projectedTimeLeftOnBlockCooldown -= 3.0f;
                }
                //Amount of time for the any attacks between the last block and the special attack
                projectedTimeLeftOnBlockCooldown -= _enemyAttackRate * ((_combatSnapshot.SpecialMoveInNumAttacks.Value - 1) - _attacksToBlock);
                //The most time we can wait before the special attack fires, give a bit of wiggle room for combat looping
                projectedTimeLeftOnBlockCooldown -= (_enemyAttackRate * -0.1f);

                canBlockBeforeHold = projectedTimeLeftOnBlockCooldown < 0.0f;
            }

            _combatSnapshot.HoldBlock = (!canBlockBeforeHold && _combatSnapshot.TimeTillSpecialMove < _character.blockCooldown() + 0.3f) || _combatSnapshot.NextAttackSpecialMove || _combatSnapshot.NextAttackNoDamage || _combatSnapshot.AttackAfterNextNoDamage;

            if (_combatSnapshot.SpecialMoveInNumAttacks == 1)
            {
                //Exile only does the big attack some of the time
                if (ZoneHelpers.ZoneIsExile(_zone) && _eai.auraID != 1000)
                {
                    _combatSnapshot.HoldBlock = false;
                }
                else
                {
                    //LogDebug($"BlockCD:{_blockRemainingCooldown} | TimeTillAttack:{_timeTillAttack} | GlobalMove:{_globalMoveCooldown}");
                    //Bypass all other combat logic to wait until the right time to block
                    if (_blockRemainingCooldown < _globalMoveCooldown && _blockRemainingCooldown < _timeTillAttack && _timeTillAttack < (_optimalTimeToBlock + _globalMoveCooldown))
                    {
                        //LogDebug($"Waiting for special.... TimeToAttack:{_combatSnapshot.TimeTillNextDamagingAttack}");
                        if (_ac.blockMove.button.IsInteractable() && _timeTillAttack < _optimalTimeToBlock)
                        {
                            //LogDebug($"\tSPECIAL BLOCK");
                            _ac.blockMove.doMove();
                        }

                        return true;
                    }
                    else
                    {
                        //LogDebug($"Optimal Block:{_optimalTimeToBlock} | GlobalMoveCD:{_globalMoveCooldown}");
                        _combatSnapshot.HoldBlock = true;
                    }
                }
            }

            return false;
        }

        private bool ApplyCombatBuffs()
        {
            if (!ZoneHelpers.ZoneIsTitan(_zone) && _enemy.curHP / _enemy.maxHP < .2)
            {
                return false;
            }

            if (UseDefensiveCooldowns())
            {
                return true;
            }

            if (CastMegaBuff())
            {
                return true;
            }

            if (!MegaBuffUnlocked())
            {
                if (!DefenseBuffActive(out _))
                {
                    if (CastUltimateBuff())
                    {
                        return true;
                    }
                }

                if (UltimateBuffActive(out _))
                {
                    if (CastOffensiveBuff())
                        return true;
                }

                if (GetHPPercentage() < .75 && !UltimateBuffActive(out _) && !BlockActive(out _) && !EnemyIsParalyzed(_eai, out _))
                {
                    if (CastDefensiveBuff())
                        return true;
                }
            }

            //We NEVER want to paralyze outside of the defensive cooldown cycle. Cast OhShit in the defensive cooldown check INSTEAD of paralyze if possible.
            //if (OhShitUnlocked() && OhShitReady() && GetHPPercentage() < .5)
            //{
            //    if (CastOhShit())
            //    {
            //        return true;
            //    }
            //}

            if (GetHPPercentage() < .75)
            {
                if (CastHeal())
                {
                    return true;
                }
            }

            if (GetHPPercentage() < .65 && !HealReady())
            {
                if (CastHyperRegen())
                {
                    return true;
                }
            }

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

        private bool UseDefensiveCooldowns()
        {
            //Each defensive move is broken down into 3 logic checks:
            //  1) Should - Do we want to use the move?
            //  2) Delay - Do we want to use the move, but the timing isn't right? If so delay usage.
            //  3) WaitFor - Do we need to delay, or do we want to use the move and it will come off cooldown before the next enemy attack? If so, is the remaining cooldown less than the global move cooldown?
            //               If all of those conditions are met and it is not the best time to use the move, then don't allow another action to take place until we can use the move.

            // **Block**
            //Should - Always Block unless its being held for a special attack
            bool shouldBlock = !_combatSnapshot.HoldBlock;
            //Delay - If the time till the next attack is above the optimal time for blocking multiple attacks
            bool delayBlock = shouldBlock && _combatSnapshot.TimeTillNextDamagingAttack > _optimalTimeToBlock;
            //WaitFor - The best time to block is immediately after the optimal time to block (..duh)
            bool waitForBlock = (delayBlock || (shouldBlock && _blockRemainingCooldown < _combatSnapshot.TimeTillNextDamagingAttack)) && _blockRemainingCooldown < _globalMoveCooldown && (_combatSnapshot.TimeTillNextDamagingAttack - _globalMoveCooldown) < _optimalTimeToBlock;

            // **Paralyze**
            //Should - Paralyze if it will take off at least 2 seconds from block's cooldown (optimally it will take off all 3 seconds)
            bool shouldParalyze = ParalyzeUnlocked() && _blockRemainingCooldown > 2.0f;
            //Delay - If we're blocking the next attack (never Paralyze an attack that can be blocked)
            bool delayParalyze = shouldParalyze && _willBlockNextAttack;
            //WaitFor - The best time to paralyze is immediately after the final blocked attack occurs
            bool waitForParalyze = (delayParalyze || (shouldParalyze && _paralyzeRemainingCooldown < _combatSnapshot.TimeTillNextDamagingAttack)) && _paralyzeRemainingCooldown < _globalMoveCooldown && _willOnlyBlockNextAttack && _combatSnapshot.TimeTillNextDamagingAttack < _globalMoveCooldown;

            // **Parry**
            //Should - Parry if the next attacks are not special moves, and Block/Paralyze is not available
            bool shouldParry = ParryUnlocked() && !ParryActive() && !_combatSnapshot.NextAttackNoDamage && !_combatSnapshot.NextAttackSpecialMove && (_combatSnapshot.HoldBlock || (_blockRemainingCooldown > _combatSnapshot.TimeTillNextDamagingAttack && _paralyzeRemainingCooldown > _combatSnapshot.TimeTillNextDamagingAttack));
            //Delay - If we're blocking the next attack (never Parry an attack that can be blocked)
            bool delayParry = shouldParry && _willBlockNextAttack;
            //WaitFor - The best time to parry is immediately after the final blocked attack occurs
            bool waitForParry = (delayParry || (shouldParry && _parryRemainingCooldown < _combatSnapshot.TimeTillNextDamagingAttack)) && _parryRemainingCooldown < _globalMoveCooldown && _willOnlyBlockNextAttack && _combatSnapshot.TimeTillNextDamagingAttack < _globalMoveCooldown;

            bool shouldBlockAndParry = _moreBlockParry || (!UltimateBuffActive(out _) && !DefenseBuffActive(out _));

            //Optimal cycle to spread out Defensive cooldowns: Block > Paralyze > Block > Parry
            if (shouldBlockAndParry)
            {
                //LogDebug($"WillBlock:{_willBlockNextAttack} | WillParry:{ParryActive()} | HoldBlock:{_combatSnapshot.HoldBlock} | TimeToDamagingAttack:{_combatSnapshot.TimeTillNextDamagingAttack}");
                //LogDebug($"ShouldBlock:{shouldBlock} | ShouldParalyze:{shouldParalyze} | ShouldParry:{shouldParry}");
                //LogDebug($"DelayBlock:{delayBlock} | DelayParalyze:{delayParalyze} | DelayParry:{delayParry}");
                //LogDebug($"BlockCD:{_blockRemainingCooldown} | ParalyzeCD:{_paralyzeRemainingCooldown} | ParryCD:{_parryRemainingCooldown}");

                //Block is the most critical defensive cooldown. Try to use as much as possible and as close to the optimal time as possible to block multiple attacks.
                if (shouldBlock && !delayBlock)
                {
                    if (CastBlock())
                    {
                        //LogDebug("\tBLOCK!");
                        return true;
                    }
                }

                //Paralyze pauses the enemy attack timer, cast ASAP if Block will not cover the next damaging attack and if Block is on cooldown to maximize Block uptime
                if (shouldParalyze && !delayParalyze)
                {
                    if (CastParalyze(_useOhShitInsteadOfParalyze))
                    {
                        //LogDebug("\tPARALYZE!");
                        return true;
                    }
                }

                //Parry works best when staggered with Block and cast when Block will not be able to cover the next attack
                //Parry does next to nothing when active during a blocked attack, but it halves incoming damage from an unblocked attack
                //While not as impactful as Block, weaving with Block will stretch out the coverage of defensive buffs as much as possible
                if (shouldParry && !delayParry)
                {
                    if (CastParry())
                    {
                        //LogDebug("\tPARRY!");
                        return true;
                    }
                }
            }
            else
            {
                //Paralyze pauses the attack timer, if we're not blocking just use it when ready
                if (!_willBlockNextAttack)
                {
                    if (CastParalyze(_useOhShitInsteadOfParalyze))
                    {
                        return true;
                    }
                }

                waitForBlock = false;
                waitForParalyze = false;
                waitForParry = false;
            }

            //LogDebug($"WaitBlock:{waitForBlock} | WaitParalyze:{waitForParalyze} | WaitParry:{waitForParry}");

            //Defensive cooldowns are critical to staying alive, its worth delaying combat for fractions of a second to optimize uptime
            if (waitForBlock || waitForParalyze || waitForParry)
            {
                //LogDebug($"Waiting.... TimeToAttack:{_combatSnapshot.TimeTillNextDamagingAttack} | WaitBlock:{waitForBlock} | WaitParalyze:{waitForParalyze} | WaitParry:{waitForParry}");
                //LogDebug($"            BlockCD:{_blockRemainingCooldown} | ParalyzeCD:{_paralyzeRemainingCooldown} | ParryCD:{_parryRemainingCooldown}");
                return true;
            }

            return false;
        }

        private bool DoSmartBeastMode()
        {
            DefenseBuffActive(out float? defBuffTimeRemaining);
            UltimateBuffActive(out float? ultBuffTimeRemaining);

            if (defBuffTimeRemaining.HasValue || ultBuffTimeRemaining.HasValue)
            {
                //Add a buffer of a few seconds to the move cooldowns to allow for a few other actions to execute
                var pa = _character.adventureController.pierceMove;
                var pafi = pa.GetType().GetField("attackTimer",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                float pierceRemainingCooldown = (_character.pierceAttackCooldown() + 3f) - Mathf.Min(_character.pierceAttackCooldown(), (float)pafi?.GetValue(pa));

                var ua = _character.adventureController.ultimateAttackMove;
                var uafi = ua.GetType().GetField("ultimateAttackTimer",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                float ultRemainingCooldown = (_character.ultimateAttackCooldown() + 3f) - Mathf.Min(_character.ultimateAttackCooldown(), (float)uafi?.GetValue(ua));

                bool canCastPierce = (defBuffTimeRemaining.HasValue && pierceRemainingCooldown < defBuffTimeRemaining) || (ultBuffTimeRemaining.HasValue && pierceRemainingCooldown < ultBuffTimeRemaining);
                bool canCastUltimate = (defBuffTimeRemaining.HasValue && ultRemainingCooldown < defBuffTimeRemaining) || (ultBuffTimeRemaining.HasValue && ultRemainingCooldown < ultBuffTimeRemaining);

                //Turn on Beast Mode if a defensive buff is active and BeastMode's cooldown is lower than the remaining time on the buff
                if (!BeastModeActive() && (canCastPierce || canCastUltimate))
                {
                    float BMcd = _character.beastModeCooldown();
                    if ((defBuffTimeRemaining.HasValue && BMcd <= defBuffTimeRemaining.Value) || (ultBuffTimeRemaining.HasValue && BMcd <= ultBuffTimeRemaining.Value))
                    {
                        if (CastBeastMode()) return true;
                    }
                }

                //Turn off Beast Mode if we wont be able to use Pierce or Ultimate attacks before the buffs expire
                if (BeastModeActive() && !canCastPierce && !canCastUltimate)
                {
                    if (CastBeastMode()) return true;
                }
            }
            else
            {
                //Turn off Beast Mode if a defensive buff is not active
                if (BeastModeActive())
                {
                    if (CastBeastMode()) return true;
                }
            }

            return false;
        }

        //This is a bit basic, it can be tightened up but probably not necessary
        private void WalderpCombatAttacks()
        {
            if (_combatSnapshot.TimeTillSpecialMove > _character.ultimateAttackCooldown() + _globalMoveCooldown)
            {
                CombatAttacks();
            }
            else
            {
                if (_ac.pierceMove.button.IsInteractable() && _combatSnapshot.TimeTillSpecialMove > _character.pierceAttackCooldown() + _globalMoveCooldown)
                {
                    _ac.pierceMove.doMove();
                    return;
                }

                if (_ac.strongAttackMove.button.IsInteractable() && _combatSnapshot.TimeTillSpecialMove > _character.strongAttackCooldown() + _globalMoveCooldown)
                {
                    _ac.strongAttackMove.doMove();
                    return;
                }

                if (_ac.regularAttackMove.button.IsInteractable())
                {
                    _ac.regularAttackMove.doMove();
                    return;
                }
            }
        }

        private void CombatAttacks()
        {
            if (_ac.ultimateAttackMove.button.IsInteractable() && _combatSnapshot?.BannedMove != CombatSnapshot.WalderpCombatMove.Ultimate)
            {
                if (_fastCombat || ChargeActive() || GetChargeCooldown() > .45)
                {
                    _ac.ultimateAttackMove.doMove();
                    return;
                }
            }

            if (_ac.pierceMove.button.IsInteractable() && _combatSnapshot?.BannedMove != CombatSnapshot.WalderpCombatMove.Piercing)
            {
                _ac.pierceMove.doMove();
                return;
            }

            if (Move69Ready())
            {
                Main.Move69.doMove();
                return;
            }

            if (_ac.strongAttackMove.button.IsInteractable() && _combatSnapshot?.BannedMove != CombatSnapshot.WalderpCombatMove.Strong)
            {
                _ac.strongAttackMove.doMove();
                return;
            }

            if (_ac.regularAttackMove.button.IsInteractable() && _combatSnapshot?.BannedMove != CombatSnapshot.WalderpCombatMove.Regular)
            {
                _ac.regularAttackMove.doMove();
                return;
            }
        }
    }
}
