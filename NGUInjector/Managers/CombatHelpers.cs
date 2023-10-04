using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Remoting.Messaging;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace NGUInjector.Managers
{
    internal static class CombatHelpers
    {
        private static int _currentCombatZone = -1;
        internal static int CurrentCombatZone
        {
            get { return IsCurrentlyGoldSniping || IsCurrentlyQuesting || IsCurrentlyAdventuring ? _currentCombatZone : -1; }
            set { _currentCombatZone = value; }
        }

        internal static bool IsCurrentlyGoldSniping { get; set; }
        internal static bool IsCurrentlyQuesting { get; set; }
        internal static bool IsCurrentlyAdventuring { get; set; }

        internal static bool CastCharge()
        {
            if (Main.Character.adventureController.chargeMove.button.IsInteractable())
            {
                Main.Character.adventureController.chargeMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool CastParry()
        {
            if (Main.Character.adventureController.parryMove.button.IsInteractable())
            {
                Main.Character.adventureController.parryMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool CastBeastMode()
        {
            if (Main.Character.adventureController.beastModeMove.button.IsInteractable())
            {
                Main.Character.adventureController.beastModeMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool ChargeReady()
        {
            return Main.Character.adventureController.chargeMove.button.IsInteractable();
        }

        internal static bool ParryReady()
        {
            return Main.Character.adventureController.parryMove.button.IsInteractable();
        }

        internal static float GetChargeCooldown()
        {
            var ua = Main.Character.adventureController.chargeMove;
            var type = ua.GetType().GetField("chargeTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var val = type?.GetValue(ua);
            if (val == null)
            {
                return 0;
            }

            return (float)val / Main.Character.chargeCooldown();
        }

        internal static bool HealReady()
        {
            return Main.Character.adventureController.healMove.button.IsInteractable();
        }

        internal static bool CastHeal()
        {
            if (Main.Character.adventureController.healMove.button.IsInteractable())
            {
                Main.Character.adventureController.healMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool CastHyperRegen()
        {
            if (Main.Character.adventureController.hyperRegenMove.button.IsInteractable())
            {
                Main.Character.adventureController.hyperRegenMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool ParryActive()
        {
            return Main.PlayerController.isParrying;
        }

        internal static bool ChargeActive()
        {
            return Main.PlayerController.chargeFactor > 1.05;
        }

        internal static bool UltimateBuffActive(out float? timeLeft)
        {
            bool isActive = Main.PlayerController.ultimateBuffTime > 0 && Main.PlayerController.ultimateBuffTime < Main.Character.ultimateBuffDuration();

            timeLeft = isActive ? (float?)(Main.Character.ultimateBuffDuration() - Main.PlayerController.ultimateBuffTime) : null;

            return isActive;
        }

        internal static bool DefenseBuffActive(out float? timeLeft)
        {
            bool isActive = Main.PlayerController.defenseBuffTime > 0 && Main.PlayerController.defenseBuffTime < Main.Character.defenseBuffDuration();

            timeLeft = isActive ? (float?)(Main.Character.defenseBuffDuration() - Main.PlayerController.defenseBuffTime) : null;

            return isActive;
        }

        internal static float GetUltimateAttackCooldown()
        {
            var ua = Main.Character.adventureController.ultimateAttackMove;
            var type = ua.GetType().GetField("ultimateAttackTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var val = type?.GetValue(ua);
            if (val == null)
            {
                return 0;
            }

            return (float)val / Main.Character.ultimateAttackCooldown();
        }

        internal static bool CastUltimateBuff()
        {
            if (Main.Character.adventureController.ultimateBuffMove.button.IsInteractable())
            {
                Main.Character.adventureController.ultimateBuffMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool CastMegaBuff()
        {
            if (Main.Character.adventureController.megaBuffMove.button.IsInteractable())
            {
                Main.Character.adventureController.megaBuffMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool CastOffensiveBuff()
        {
            if (Main.Character.adventureController.offenseBuffMove.button.IsInteractable())
            {
                Main.Character.adventureController.offenseBuffMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool BlockActive(out float? timeLeft)
        {
            bool isActive = Main.PlayerController.isBlocking;

            timeLeft = isActive ? (float?)(3.0f - Main.PlayerController.blockTime) : null;

            return isActive;
        }

        internal static bool CastBlock()
        {
            if (Main.Character.adventureController.blockMove.button.IsInteractable())
            {
                Main.Character.adventureController.blockMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool CastDefensiveBuff()
        {
            if (Main.Character.adventureController.defenseBuffMove.button.IsInteractable())
            {
                Main.Character.adventureController.defenseBuffMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool EnemyIsParalyzed(EnemyAI eai, out float? timeLeft)
        {
            var type = eai.GetType().GetField("paralyzeTime",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var paralyzeTime = (float)type?.GetValue(eai);

            bool isActive = paralyzeTime > 0;

            timeLeft = isActive ? (float?)(paralyzeTime) : null;

            return isActive;
        }

        internal static bool CastParalyze()
        {
            if (Main.Character.adventureController.paralyzeMove.button.IsInteractable())
            {
                Main.Character.adventureController.paralyzeMove.doMove();
                return true;
            }

            return false;


        }

        internal static bool UltimateAttackReady()
        {
            return Main.Character.adventureController.ultimateAttackMove.button.IsInteractable();
        }

        internal static bool PierceReady()
        {
            return Main.Character.adventureController.pierceMove.button.IsInteractable();
        }

        internal static bool ChargeUnlocked()
        {
            return Main.Character.training.defenseTraining[4] > 0;
        }

        internal static bool ParryUnlocked()
        {
            return Main.Character.training.attackTraining[3] > 0;
        }

        internal static bool UltimateBuffUnlocked()
        {
            return Main.Character.training.defenseTraining[5] > 0;
        }

        internal static bool UltimateBuffReady()
        {
            return Main.Character.adventureController.ultimateBuffMove.button.IsInteractable();
        }

        internal static bool DefensiveBuffUnlocked()
        {
            return Main.Character.training.defenseTraining[1] > 0;
        }

        internal static bool DefensiveBuffReady()
        {
            return Main.Character.adventureController.defenseBuffMove.button.IsInteractable();
        }

        internal static bool ParalyzeUnlocked()
        {
            return Main.Character.allChallenges.hasParalyze();
        }

        internal static bool BeastModeUnlocked()
        {
            return Main.Character.adventureController.hasBeastMode();
        }

        internal static bool BeastModeReady()
        {
            return Main.Character.adventureController.beastModeMove.button.IsInteractable();
        }

        internal static bool BeastModeActive()
        {
            return Main.Character.adventure.beastModeOn;
        }

        internal static bool MegaBuffUnlocked()
        {
            return Main.Character.training.defenseTraining[4] >= 25000L && Main.Character.wishes.wishes[8].level >= 1;
        }

        internal static bool MegaBuffReady()
        {
            return Main.Character.adventureController.megaBuffMove.button.IsInteractable();
        }

        internal static bool OhShitUnlocked()
        {
            return Main.Character.wishes.wishes[58].level >= 1 && Main.Character.allChallenges.hasParalyze() &&
                   Main.Character.training.defenseTraining[1] >= 10000L && Main.Character.settings.hasHyperRegen;
        }

        internal static bool OhShitReady()
        {
            return Main.Character.adventureController.ohShitMove.button.IsInteractable();
        }

        internal static bool CastOhShit()
        {
            if (Main.Character.adventureController.ohShitMove.button.IsInteractable())
            {
                Main.Character.adventureController.ohShitMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool Move69Ready()
        {
            return Main.Character.adventure.move69Unlocked && Main.Move69.button.IsInteractable() && Main.Character.adventure.move69Used < 69;
        }
    }
}
