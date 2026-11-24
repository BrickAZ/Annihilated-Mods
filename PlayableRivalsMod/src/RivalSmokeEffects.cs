using Il2Cpp;
using Il2CppNaninovel;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private bool smokeSkills;

    private void VerifySmokeEffects(Card usedInGame)
    {
        static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException(message);
        }

        const string enemy = "enemy1";
        Write("turnCharacter", "partner");
        Check(LastUsed(usedInGame) > 0, "Native card did not finish and record its turn.");
        if (Role == Rival.Outcast)
        {
            string SwordFace() => Engine.GetService<ItemService>().GetOrBuildPresentation(new ItemPresentationRequest(
                GetCard(Card.Sword), "rivalsSmokeSword", "partner", "battle", false)).Description;
            Check(!SwordFace().Contains(PreparedRobberyNotice, StringComparison.Ordinal),
                "The weapon shows a prepared attack before Steal is used.");
            Check(RivalRules.FormFor(Role, Round, LastUsed(Card.Switch), false) == Card.Sword &&
                RivalRules.FormFor(Role, Round + 1, LastUsed(Card.Switch), false) == Card.Dagger,
                "Native Switch did not queue the dagger for the next round.");
            Write("rivalsCommittedCard", (int)Card.Steal);
            Write("selectCommand", (int)Card.Steal);
            ResolveCommittedCard();
            UpdateStealEffects();
            Check(StealPrepared && GetCard(Card.Sword).canStealItem() &&
                GetCard(Card.Sword).battleActionSetData.exEffects[0] == nameof(ExEffect.canStealWithoutBreaking),
                "Prepared native Sword does not expose the robbery action.");
            Check(SwordFace().Contains(PreparedRobberyNotice, StringComparison.Ordinal),
                "The weapon card did not display the prepared attack.");
            Write("rivalsCommittedCard", (int)Card.Fire);
            Write("selectCommand", (int)Card.Fire);
            ResolveCommittedCard();
            Check(Read(Enchantment) == "fire" &&
                GetCard(Card.Fire).battleActionSetData.exEffects[0] == nameof(ExEffect.dontUseTurn),
                "Fire does not set the enchantment or preserve the current action.");
            SetState(enemy, "Strength", 5);
            SetState(enemy, "Concentration", 3);
            SetState(enemy, "Barrier", 1);
            SetState("partner", "Strength", 0);
            SetState("partner", "Concentration", 0);
            SetState(enemy, "Fire", 0);
            Write("rivalsCommittedCard", (int)Card.Sword);
            Write("selectCommand", (int)Card.Sword);
            Write("actionUserId", "partner");
            Write("actionTargetId", enemy);
            Write("actionType", "attack");
            Write("sectionEffectEligibleAction", "true");
            OnAttackContact();
            Check(GetState(enemy,"Strength") == 1 && GetState("partner","Strength") == 4 &&
                GetState(enemy,"Concentration") == 0 && GetState("partner","Concentration") == 3 &&
                GetState(enemy,"Barrier") == 1, "Native Group_Buff transfer did not complete twice.");
            float fire = GetState(enemy, "Fire");
            Check(fire == RivalRules.ElementStacks("fire", CurrentStats().Mag), "The enchanted weapon did not apply fire.");
            Write("rivalsSwordHit", 0);
            OnAttackContact();
            Check(GetState(enemy, "Fire") == fire, "A second hit in the same round applied enchantment again.");
            ResolveCommittedCard();
            Check(!StealPrepared && !SwordFace().Contains(PreparedRobberyNotice, StringComparison.Ordinal),
                "The weapon kept its prepared label after consuming Steal.");
        }
        else if (Role == Rival.Villainess)
        {
            Check(Number(DollHp) == 12 && Number("rivalsDollAttack") == 2 &&
                Number("rivalsDollCount") == 3 && Flag("rivalsDollCover"), "The native Doll card did not create its ally.");
            var love = GetCard(Card.Love).battleActionSetData;
            Check(love.actionNames[0] == ActionType.fire && love.actionValues[0] == 2 &&
                love.actionNames[1] == ActionType.poison && love.actionValues[1] == 1 &&
                love.actionNames[2] == ActionType.heal && love.actionValues[2] == 6,
                "The native Love action list has incorrect effects.");
            var domination = GetCard(Card.Dominate).battleActionSetData;
            Check(domination.actionNames[0] == ActionType.domination && domination.actionValues[0] == 1,
                "The Domination card is not bound to the native one-layer effect.");
            Write("actionUserId", enemy);
            Write("actionTargetId", "player");
            Write("damageType", "attack");
            Write("actionValue", 5);
            ShieldParty();
            Check(Number(DollHp) == 7 && Number("actionValue") == 0 && !Flag("rivalsDollCover"),
                "The Doll did not cover one native incoming hit.");
        }
        else if (Role == Rival.Dropout)
        {
            Check(NoMagic && Number("partnerMag") == 0 && GetState("player", "Block") == 9 &&
                GetState("player", "BlockProtect") == 1, "Armor did not use pre-cast MAG or set its magic lock.");
            Check(RivalRules.FormFor(Role, Round, 0, NoMagic) == Card.Punch &&
                ItemCustomFunctions.IsActionUnavailableForMagic((int)Card.Codex, 0, "partner") &&
                !ItemCustomFunctions.IsActionUnavailableForMagic((int)Card.Punch, 0, "partner"),
                "The magic lock leaked into physical attacks or did not disable Codex.");
            ResetBattle(false);
            Check(!NoMagic && Number("partnerMag") == 3, "Battle cleanup did not restore MAG.");
            ResetBattle(true);
            Write("rivalsCommittedCard", (int)Card.Barrier);
            Write("selectCommand", (int)Card.Barrier);
            ResolveCommittedCard();
            Check(Number(BarrierHp) == 15 && Number("rivalsBarrierBlock") == 2 &&
                Number("rivalsBarrierEnd") == Round + 1, "The Barrier did not last two enemy phases.");
            Write("actionUserId", enemy);
            Write("actionTargetId", "player");
            Write("damageType", "attack");
            Write("actionValue", 20);
            ShieldParty();
            Check(Number(BarrierHp) == 0 && Number("actionValue") == 3, "Barrier overkill did not spill to the original target.");
        }
    }
}
