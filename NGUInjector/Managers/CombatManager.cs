using NGUInjector.AllocationProfiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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

        private bool _nextAttackNoDamage = false;
        private bool _nextAttackSpecial = false;
        private bool _holdBlock = false;

        private void SetWalderpCombatMove(WalderpCombatMove? combatMove, bool walderpSays)
        {
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

        private void DoCombat(bool fastCombat, bool smartBeastMode)
        {
            if (!Main.PlayerController.canUseMove || !_pc.moveCheck())
                return;

            var ac = _character.adventureController;
            var eai = ac.enemyAI;
            var zone = _character.adventure.zone;

            var eaifi = eai.GetType().GetField("enemyAttackTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var enemyAttackTimer = (float)eaifi?.GetValue(eai);
            bool enemyIsParalyzed = EnemyIsParalyzed(eai, out float? paralyzeTimeLeft);
            var timeTillAttack = (ac.currentEnemy.attackRate - enemyAttackTimer) + (paralyzeTimeLeft ?? 0f);

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

            if (ZoneHelpers.ZoneIsGodmother(zone) || ZoneHelpers.ZoneIsExile(zone))
            {
                int loopSize = 9;
                int specialMoveNumber = 4;

                switch (ac.currentEnemy.enemyType)
                {
                    case enemyType.bigBoss8V1:
                    case enemyType.bigBoss8V2:
                    case enemyType.bigBoss8V3:
                    case enemyType.bigBoss8V4:
                        loopSize = 9;
                        break;
                    case enemyType.bigBoss9V1:
                        loopSize = 10;
                        break;
                    case enemyType.bigBoss9V2:
                        loopSize = 9;
                        break;
                    case enemyType.bigBoss9V3:
                        loopSize = 8;
                        break;
                    case enemyType.bigBoss9V4:
                        loopSize = 7;
                        break;
                }

                int attackNumber = eai.growCount % loopSize;
                int numAttacksTillSpecialMove = ((((specialMoveNumber - 1) - attackNumber) + loopSize) % loopSize) + 1;
                float timeTillSpecialMove = ((numAttacksTillSpecialMove - 1) * ac.currentEnemy.attackRate) + timeTillAttack;

                var bm = _character.adventureController.blockMove;
                var bmfi = bm.GetType().GetField("blockTimer",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                float blockRemainingCooldown = _character.blockCooldown() - Mathf.Min(_character.blockCooldown(), (float)bmfi?.GetValue(bm));

                var pm = _character.adventureController.parryMove;
                var pmfi = pm.GetType().GetField("parryTimer",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                float parryRemainingCooldown = _character.parryCooldown() - Mathf.Min(_character.parryCooldown(), (float)pmfi?.GetValue(pm));

                var parm = _character.adventureController.paralyzeMove;
                var parfi = parm.GetType().GetField("attackTimer",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                float paralyzeRemainingCooldown = _character.paralyzeCooldown() - Mathf.Min(_character.paralyzeCooldown(), (float)parfi?.GetValue(parm));

                int attacksToBlock = (int)Math.Ceiling(2.9f / ac.currentEnemy.attackRate);
                float optimalTimeToBlock = 2.9f - (ac.currentEnemy.attackRate * (attacksToBlock - 1));

                _nextAttackSpecial = numAttacksTillSpecialMove == 1;
                _nextAttackNoDamage = numAttacksTillSpecialMove == 2;
                bool attackAfterNextNoDamage = numAttacksTillSpecialMove == 3;

                //LogDebug($"");
                //LogDebug($"SpecialIn:{numAttacksTillSpecialMove} | TimeToSpecial:{timeTillSpecialMove} | TimeToAttack:{timeTillAttack}");
                //LogDebug($"BlockCD:{blockRemainingCooldown} | ParalyzeCD:{paralyzeRemainingCooldown} | ParryCD:{parryRemainingCooldown}");

                bool canBlockBeforeHold = false;
                if (Settings.TitanMoreBlockParry && ParalyzeUnlocked() && numAttacksTillSpecialMove > 3 && (blockRemainingCooldown + 0.1f) < timeTillAttack)
                {
                    float projectedTimeLeftOnBlockCooldown = _character.blockCooldown();
                    //Amount of time before the first attack
                    projectedTimeLeftOnBlockCooldown -= Mathf.Min(timeTillAttack, Mathf.Max(optimalTimeToBlock, blockRemainingCooldown));
                    //Amount of time for the blocked attacks
                    projectedTimeLeftOnBlockCooldown -= ac.currentEnemy.attackRate * (attacksToBlock - 1);
                    //Amount of time due to paralyze
                    if ((paralyzeRemainingCooldown + 0.1f) < timeTillSpecialMove && (projectedTimeLeftOnBlockCooldown - paralyzeRemainingCooldown) > 2.0f)
                    {
                        projectedTimeLeftOnBlockCooldown -= 3.0f;
                    }
                    //Amount of time for the warning "attack" and give a 0.2 second buffer for the combat loops
                    projectedTimeLeftOnBlockCooldown -= (ac.currentEnemy.attackRate * 2.0f) - 0.2f;

                    //LogDebug($"Projected BlockCD:{projectedTimeLeftOnBlockCooldown}");

                    canBlockBeforeHold = projectedTimeLeftOnBlockCooldown < 0.0f;
                }

                _holdBlock = (!canBlockBeforeHold && timeTillSpecialMove < _character.blockCooldown() + 0.5f) || _nextAttackNoDamage || attackAfterNextNoDamage;

                if (numAttacksTillSpecialMove == 1)
                {
                    //Exile only does the big attack some of the time
                    if (ZoneHelpers.ZoneIsExile(zone) && eai.auraID != 1000)
                    {
                        _holdBlock = false;
                    }
                    else
                    {
                        //Bypass all other combat logic to wait until the right time to block
                        if (blockRemainingCooldown < timeTillAttack && timeTillAttack < (optimalTimeToBlock + 1.0f))
                        {
                            if (ac.blockMove.button.IsInteractable() && timeTillAttack < optimalTimeToBlock)
                            {
                                //LogDebug($"\tSPECIAL BLOCK");
                                ac.blockMove.doMove();
                            }

                            return;
                        }
                        else
                        {
                            _holdBlock = true;
                        }
                    }
                }
            }

            if (!fastCombat)
            {
                if (CombatBuffs())
                {
                    return;
                }

                //SmartBeastMode routine, turn on Beast Mode when a defensive buff is active and only if it can be turned off before the buff expires
                if (smartBeastMode && BeastModeUnlocked() && BeastModeReady())
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
                                if (CastBeastMode()) return;
                            }
                        }

                        //Turn off Beast Mode if we wont be able to use Pierce or Ultimate attacks before the buffs expire
                        if (BeastModeActive() && !canCastPierce && !canCastUltimate)
                        {
                            if (CastBeastMode()) return;
                        }
                    }
                    else
                    {
                        //Turn off Beast Mode if a defensive buff is not active
                        if (BeastModeActive())
                        {
                            if (CastBeastMode()) return;
                        }
                    }
                }
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

            //Generic AI block/parry logic
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

            if (DoDefensiveCooldowns())
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

                if (GetHPPercentage() < .75 && !UltimateBuffActive(out _) && !BlockActive(out _) && !EnemyIsParalyzed(eai, out _))
                {
                    if (CastDefensiveBuff())
                        return true;
                }
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

        private bool DoDefensiveCooldowns()
        {
            var ac = _character.adventureController;
            var ai = ac.currentEnemy.AI;
            var eai = ac.enemyAI;

            var bm = _character.adventureController.blockMove;
            var bmfi = bm.GetType().GetField("blockTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            float blockRemainingCooldown = _character.blockCooldown() - Mathf.Min(_character.blockCooldown(), (float)bmfi?.GetValue(bm));

            var pm = _character.adventureController.parryMove;
            var pmfi = pm.GetType().GetField("parryTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            float parryRemainingCooldown = _character.parryCooldown() - Mathf.Min(_character.parryCooldown(), (float)pmfi?.GetValue(pm));

            var parm = _character.adventureController.paralyzeMove;
            var parfi = parm.GetType().GetField("attackTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            float paralyzeRemainingCooldown = _character.paralyzeCooldown() - Mathf.Min(_character.paralyzeCooldown(), (float)parfi?.GetValue(parm));

            var eaifi = eai.GetType().GetField("enemyAttackTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var enemyAttackTimer = (float)eaifi?.GetValue(eai);

            bool blockIsActive = BlockActive(out float? blockTimeLeft);
            bool enemyIsParalyzed = EnemyIsParalyzed(eai, out float? paralyzeTimeLeft);

            float timeTillAttack = (ac.currentEnemy.attackRate - enemyAttackTimer) + (paralyzeTimeLeft ?? 0f);
            float timeTillDamagingAttack = timeTillAttack + (_nextAttackNoDamage ? ac.currentEnemy.attackRate : 0f);
            float timeTillUnparriedAttack = timeTillDamagingAttack + (ParryActive() ? ac.currentEnemy.attackRate : 0f);

            bool willBlockNextAttack = blockIsActive && (blockTimeLeft ?? 0) > timeTillDamagingAttack;
            bool willBlockNextTwoAttacks = blockIsActive && (blockTimeLeft ?? 0) > (timeTillDamagingAttack + ac.currentEnemy.attackRate);

            //Use Block so that it covers multiple attacks if possible
            int attacksToBlock = (int)Math.Ceiling(2.9f / ac.currentEnemy.attackRate);
            float optimalTimeToBlock = 2.9f - (ac.currentEnemy.attackRate * (attacksToBlock - 1));

            //Prioritize Block unless its being held for a special attack
            bool shouldBlock = !_holdBlock;
            //Delay Block if its above the optimal time or if we can squeeze in a block before the next attack
            bool delayBlock = timeTillDamagingAttack > optimalTimeToBlock;
            delayBlock |= (blockRemainingCooldown > 0 && (blockRemainingCooldown + 0.1f) < timeTillDamagingAttack);

            //Use Paralyze the next attack will not be blocked and if it will take off at least 2 seconds from block's cooldown (optimally it will take off all 3 seconds)
            bool shouldParalyze = ParalyzeUnlocked() && !willBlockNextAttack && blockRemainingCooldown > 2.0f;
            //Delay Paralyze if we're only blocking one more attack and Block's cooldown will have at least two seconds left after the next attack
            bool delayParalyze = willBlockNextAttack && !willBlockNextTwoAttacks && (blockRemainingCooldown - timeTillDamagingAttack) > 2.0f;

            //Use Parry if the next attack will not be blocked, the next attacks are not special titan cases, and Block/Paralyze will not be available
            bool shouldParry = ParryUnlocked() && !willBlockNextAttack && !_nextAttackNoDamage && !_nextAttackSpecial && (_holdBlock || ((blockRemainingCooldown + 0.1f) > timeTillDamagingAttack && (paralyzeRemainingCooldown + 0.1f) > timeTillDamagingAttack));
            //Delay Parry if we're only blocking one more attack and Paralyze will not fire before the next attack
            bool delayParry = willBlockNextAttack && !willBlockNextTwoAttacks && (_holdBlock || !delayParalyze);

            bool moreBlockParry = ZoneHelpers.ZoneIsTitan(_character.adventure.zone) ? Settings.TitanMoreBlockParry : Settings.MoreBlockParry;
            bool shouldBlockAndParry = moreBlockParry || (!UltimateBuffActive(out _) && !DefenseBuffActive(out _));

            //TODO: Tested with ExileV1 who has an attack rate of 2.0, need to test with ExileV2-4 with attack rates of 2.0-1.8
            //Optimal cycle to spread out Defensive cooldowns: Block > Paralyze > Block > Parry
            if (ai != AI.charger && ai != AI.rapid && ai != AI.exploder && shouldBlockAndParry)
            {
                //LogDebug($"WillBlock:{willBlockNextAttack} | WillParry:{ParryActive()} | HoldBlock:{_holdBlock} | TimeToDamagingAttack:{timeTillDamagingAttack} | TimeToUnparriedAttack:{timeTillUnparriedAttack}");
                //LogDebug($"ShouldBlock:{shouldBlock} | ShouldParalyze:{shouldParalyze} | ShouldParry:{shouldParry}");
                //LogDebug($"DelayBlock:{delayBlock} | DelayParalyze:{delayParalyze} | DelayParry:{delayParry}");

                //Block is the most critical defensive cooldown. Try to use as much as possible and as close to the optimal time as possible to block multiple attacks.
                if (shouldBlock && !delayBlock)
                {
                    if (CastBlock())
                    {
                        //LogDebug("\tBLOCK!");
                        return true;
                    }
                }

                //Paralyze pauses the attack timer, cast ASAP if Block will not cover the next damaging attack and Block is on cooldown to maximize Block uptime
                if (shouldParalyze && !delayParalyze)
                {
                    if (CastParalyze(ai, eai))
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
                if (!willBlockNextAttack)
                {
                    if (CastParalyze(ai, eai))
                    {
                        return true;
                    }
                }

                delayBlock = false;
                delayParalyze = false;
                delayParry = false;
            }

            //If we paused Block usage to cover multiple attacks and Block will be ready before the next player attack, delay further action until Block is cast
            bool waitForBlock = shouldBlock && delayBlock && (blockRemainingCooldown + 0.1f) < timeTillAttack && timeTillAttack < (optimalTimeToBlock + 1.0f);

            //If Block is running and Paralyze will be ready before the next player attack, delay further action until Paralyze is cast
            bool waitForParalyze = shouldParalyze && delayParalyze && paralyzeRemainingCooldown < 1.0f && timeTillAttack < 1.0f;

            //If Block is running and Paralyze will NOT be ready before the next player attack, delay further action until Parry is cast
            bool waitForParry = shouldParry && delayParry && parryRemainingCooldown < 1.0f && timeTillDamagingAttack < 1.0f;

            //LogDebug($"WaitBlock:{waitForBlock} | WaitParalyze:{waitForParalyze} | WaitParry:{waitForParry}");

            //Defensive cooldowns are critical to staying alive, its worth delaying combat for fractions of a second to optimize uptime
            if (waitForBlock || waitForParalyze || waitForParry)
            {
                return true;
            }

            return false;
        }

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

            _nextAttackNoDamage = false;
            _nextAttackSpecial = false;
            _holdBlock = false;

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
            if (BeastModeUnlocked())
            {
                bool needToToggle = BeastModeActive() != beastMode;

                //If the button is inaccessible, we need to stay in manual mode until we can press it
                if (needToToggle)
                {
                    beastModeWasToggled = CastBeastMode();
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

            bool needsToHeal = recoverHealth && !HasFullHP();

            //If we have no enemy and we were fighting, the fight has ended, update combat flags and release any gear locks. Move to safe zone if we need to heal.
            if (_character.adventureController.currentEnemy == null)
            {
                if (_isFighting)
                {
                    if (_fightTimer > 1)
                    {
                        LogCombat($"{_enemyName} killed in {_fightTimer:00.0}s");
                    }

                    _isFighting = false;
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
                }
                else
                {
                    _fightTimer = 0;
                }

                if (_character.adventure.zone != -1 && needsToHeal)
                {
                    MoveToZone(-1);
                    return;
                }
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

            //Check if we're in not in the right zone and not in safe zone, if not move to safe zone first
            if (_character.adventure.zone != zone && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
            }

            //If we're in safe zone, recover health if needed.
            if (_character.adventure.zone == -1 && needsToHeal)
            {
                return;
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
                return;
            }

            //Skip blacklisted enemies
            if (zone < 1000 && Settings.BlacklistedBosses.Contains(_character.adventureController.currentEnemy.spriteID))
            {
                MoveToZone(-1);
                MoveToZone(zone);
                return;
            }

            //Skip regular enemies if we're in bossOnly mode
            if (bossOnly && zone < 1000 && !ZoneHelpers.ZoneIsTitan(zone))
            {
                var ec = _character.adventureController.currentEnemy.enemyType;
                if (ec != enemyType.boss && !ec.ToString().Contains("bigBoss"))
                {
                    MoveToZone(-1);
                    MoveToZone(zone);
                    return;
                }
            }

            //We have a valid enemy and we're ready to fight, allow idle combat to continue
            _isFighting = true;
            _enemyName = _character.adventureController.currentEnemy.name;
        }

        internal void ManualZone(int zone, bool bossOnly, bool recoverHealth, bool precastBuffs, bool fastCombat, bool beastMode, bool smartBeastMode)
        {
            if (fastCombat)
            {
                smartBeastMode = false;
            }

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
                return;
            }

            bool needsToPrecast = precastBuffs &&
                (
                    (ChargeUnlocked() && (!ChargeReady() || !ChargeActive())) ||
                    (ParryUnlocked() && (!ParryReady() || !ParryActive())) ||
                    (smartBeastMode && BeastModeUnlocked() && (!BeastModeReady() || !BeastModeActive())) ||
                    (MegaBuffUnlocked() && !MegaBuffReady()) ||
                    (UltimateBuffUnlocked() && !UltimateBuffReady()) ||
                    (DefensiveBuffUnlocked() && !DefensiveBuffReady())
                );

            bool needsToHeal = recoverHealth && !HasFullHP();

            //If we have no enemy and we were fighting, the fight has ended, update combat flags and release any gear locks. Move to safe zone if we need to heal or precast.
            if (_character.adventureController.currentEnemy == null)
            {
                if (_isFighting)
                {
                    if (_fightTimer > 1)
                    {
                        LogCombat($"{_enemyName} killed in {_fightTimer:00.0}s");
                    }

                    _isFighting = false;
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
                }
                else
                {
                    _fightTimer = 0;
                }

                if (_character.adventure.zone != -1 && (needsToPrecast || needsToHeal))
                {
                    MoveToZone(-1);
                    return;
                }
            }

            //If we need to toggle beast mode, just do the normal combat loop until the cooldown is ready
            if (!smartBeastMode)
            {
                CheckBeastModeToggle(beastMode, out bool beastModeWasToggled);
                if (beastModeWasToggled)
                {
                    return;
                }
            }

            //Check if we're in not in the right zone and not in safe zone, if not move to safe zone first
            if (_character.adventure.zone != zone && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
            }

            //If we're in safe zone, precast buffs and recover health if needed.
            if (_character.adventure.zone == -1)
            {
                if (needsToPrecast)
                {
                    if (ChargeUnlocked() && !ChargeActive())
                    {
                        if (CastCharge()) return;
                    }

                    if (ParryUnlocked() && !ParryActive())
                    {
                        if (CastParry()) return;
                    }

                    if (smartBeastMode && BeastModeUnlocked() && !BeastModeActive())
                    {
                        if (CastBeastMode()) return;
                    }

                    return;
                }

                if (needsToHeal)
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

            //Skip blacklisted enemies
            if (zone < 1000 && Settings.BlacklistedBosses.Contains(_character.adventureController.currentEnemy.spriteID))
            {
                MoveToZone(-1);
                MoveToZone(zone);
                return;
            }

            //Skip regular enemies if we're in bossOnly mode
            if (bossOnly && zone < 1000 && !ZoneHelpers.ZoneIsTitan(zone))
            {
                var ec = _character.adventureController.currentEnemy.enemyType;
                if (ec != enemyType.boss && !ec.ToString().Contains("bigBoss"))
                {
                    MoveToZone(-1);
                    MoveToZone(zone);
                    return;
                }
            }

            //We have a valid enemy and we're ready to fight. Run through our manual combat routine.
            _isFighting = true;
            _enemyName = _character.adventureController.currentEnemy.name;

            DoCombat(fastCombat, smartBeastMode);
        }
    }
}
