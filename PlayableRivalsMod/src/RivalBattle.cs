using System.Globalization;
using System.Text;
using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private const string BattleKey = "rivalsFight";
    private const string Used = "rivalsUsed";
    private const string NoMagicKey = "rivalsNoMagic";
    private const string StealKey = "rivalsStealReady";
    private const string Enchantment = "rivalsElement";
    private const string DollHp = "rivalsDollHp";
    private const string BarrierHp = "rivalsBarrierHp";
    private readonly Dictionary<IntPtr, Action> commandRestore = new();
    private int Round => Number("turnCount");
    private bool NoMagic => Flag(NoMagicKey) && Flag(BattleKey) && Role == Rival.Dropout;
    private bool ArmorLockedInFinale => BattleRival == Rival.Dropout && Read("battleTarget") == "priest" &&
        Read("xPriestFinalRoute") == "mageMaryoku" && (Number("xPriestFinalPhase") < 4 ||
        (Number("xPriestFinalMageMaryokuForcedPhase") > 0 && !Flag("xSwitchPriestFinalMageMaryokuForcedActionDone")));
    private bool StealPrepared => Flag(StealKey);
    private int UnlockedFloor => Math.Max(Number(FloorKey), Math.Clamp(Number("dungeonId"), 1, 3));
    private static string UsedKey(Card card) => Used + RivalRules.CooldownKey(card);
    private int LastUsed(Card card) => Number(UsedKey(card));

    private static readonly Card[] Elements = { Card.Fire, Card.Freeze, Card.Thunder, Card.Poison };

    private void HandleBattleCommand(Command command, string script, int line)
    {
        HandleDamageCap(command, script, line);
        if (script == "Action")
        {
            if (line == 778 && IsRivalBattle) ShieldParty();
            return;
        }
        if (script != "Battle") return;

        if (line == 47) ResetBattle(true);
        if (line == 4977) ResetBattle(false);
        if (!IsRivalBattle) return;

        // Repair older saves before building the forced hand, or when resuming a hand already on screen.
        if (line is 1879 or 2286) RepairFinaleArmor();

        switch (line)
        {
            case 1128:
                if (smokeSkills && Round == 1) Write("turnCount", 4);
                else if (smokeRobbery && Round == 1) Write("turnCount", 2);
                Write(FloorKey, UnlockedFloor);
                if (Round > Number("rivalsBarrierEnd"))
                {
                    if (Number(BarrierHp) > 0) ShowBattleFeedback("屏障消散");
                    Write(BarrierHp, 0);
                    Write("rivalsBarrierBlock", 0);
                }
                break;
            case 1879 when BattleRival == Rival.Villainess:
                ClearWarriorCombatState();
                break;
            case 1610:
                if (Role == Rival.Outcast && LastUsed(Card.Switch) > 0 && LastUsed(Card.Switch) < Round &&
                    Number("rivalsDaggerRound") != Round && !Flag("partnerDeath"))
                {
                    Write("rivalsDaggerRound", Round);
                    Write("partnerStateActionPlus", Number("partnerStateActionPlus") + 1);
                }
                break;
            case 2060 when Read("turnCharacter") == "partner":
                // Some scripted phases send the partner straight to the default weapon.
                RefreshCards();
                UpdateStealEffects();
                Write("defaultActionNumber", (int)RivalRules.FormFor(Role, Round, LastUsed(Card.Switch), NoMagic));
                break;
            case 2286 when NoMagic && Read("turnCharacter") == "partner" && Number("selectCommand") > 0:
                var set = Engine.GetService<ItemService>().GetItemBySlotNumber(Number("selectCommand"))?.battleActionSetData;
                if (set != null && set.actionNames.Length > 0 &&
                    Enumerable.Range(0, set.actionNames.Length).All(index => set.IsMagic(index)))
                    Write("cantUse", 1);
                break;
            case 2319 when Read("turnCharacter") == "partner":
                RefreshCards();
                UpdateStealEffects();
                Write("rivalsConsumeSteal", BattleRival == Rival.Outcast && StealPrepared &&
                    IsWeaponAttack(Engine.GetService<ItemService>().GetItemBySlotNumber(Number("selectCommand"))) ? 1 : 0);
                Write("rivalsCommittedCard", TryCard(Number("selectCommand"), out var selected) ? (int)selected : 0);
                break;
            case 2358 when HasCommittedModAction():
                OverrideCondition(command, "true");
                break;
            case 2360 when HasCommittedModAction():
                // Keep the native item setup and end-of-action bookkeeping, skipping the placeholder action.
                var finishAction = command.Cast<Goto>();
                var path = finishAction.Path;
                commandRestore[command.Pointer] = () => finishAction.Path = path;
                finishAction.Path = new NamedString("Battle", "ActionOwariEnsyutuEnd");
                break;
            case 2937 when BattleRival == Rival.Villainess && Read("turnCharacter") == "partner":
                // Keep the route's learn flag for the story and suppress its original combat effect.
                OverrideCondition(command, "false");
                break;
            case 3354 when BattleRival == Rival.Villainess:
            case 3535 or 3567 when BattleRival == Rival.Outcast:
                OverrideCondition(command, "false");
                break;
            case 3468:
                if (Read("turnCharacter") == "partner" && Flag("rivalsConsumeSteal"))
                {
                    Write(StealKey, 0);
                    Write("rivalsConsumeSteal", 0);
                }
                ResolveCommittedCard();
                break;
        }
    }

    private void ClearWarriorCombatState()
    {
        if (Flag("showBattleWarriorDestroyMode")) Write("showBattleWarriorDestroyMode", "false");
        if (Flag("battleWarriorDestroyMode")) ItemCustomFunctions.CancelBattleWarriorDestroyModeAndRefundCurse();
        if (Number("partnerStateWarriorPowerUpCount") != 0) Write("partnerStateWarriorPowerUpCount", 0);
    }

    private void RepairFinaleArmor()
    {
        if (!NoMagic || !ArmorLockedInFinale) return;
        Write(NoMagicKey, 0);
        Write("partnerMag", RivalRules.GetStats(Rival.Dropout, Number("partnerLv")).Mag);
    }

    private void ResetBattle(bool starting)
    {
        if (NoMagic)
            Write("partnerMag", RivalRules.GetStats(Role, Number("partnerLv")).Mag);
        ClearBattleFeedback();
        Write(BattleKey, starting ? 1 : 0);
        foreach (var key in new[]
        {
            NoMagicKey, StealKey, "rivalsConsumeSteal", "rivalsCommittedCard", "rivalsDaggerRound", "rivalsElementHit",
            "rivalsDollHp", "rivalsDollMaxHp", "rivalsDollCover", "rivalsDollAttack", "rivalsDollCount",
            "rivalsDollStart", "rivalsDollLast", "rivalsBarrierHp", "rivalsBarrierMaxHp", "rivalsBarrierBlock",
            "rivalsBarrierEnd", "rivalsSwordHit"
        }) Write(key, 0);
        Write(Enchantment, "");
        foreach (var card in RivalRules.Skills(Role))
            Write(UsedKey(card), 0);
    }

    private void RouteBattleHand(Goto command)
    {
        // The native script reuses this Goto when formation changes. Resolve its
        // destination on every visit, including after switching back to the soldier.
        bool useRivalHand = IsRivalBattle && Read("turnCharacter") == "partner" &&
                            Flag("showPartnerStatus") && !Flag("partnerDeath");
        if (useRivalHand) UpdateBattleCards();
        command.Path = useRivalHand
            ? new NamedString(ScriptName, "Cards")
            : new NamedString("Battle", "Magics");
    }

    private bool CanCommitRivalCard()
    {
        if (!IsRivalBattle || Read("turnCharacter") != "partner" || Flag("partnerDeath") ||
            !Flag("showPartnerStatus") || !TryCard(Number("selectCommand"), out var card)) return false;
        if (RivalRules.GetSkill(card).Floor == 0)
            return card == RivalRules.FormFor(Role, Round, LastUsed(Card.Switch), NoMagic);

        bool belongsToRole = RivalRules.Skills(Role).Contains(RivalRules.CooldownKey(card));
        return belongsToRole && Ready(card) &&
            (card != Card.Armor || (Flag("showPlayerStatus") && !Flag("playerDeath")));
    }

    private void UpdateBattleCards()
    {
        RegisterScript();
        RefreshCards();
        UpdateStealEffects();
        Write("rivalsRole", RivalRules.Id(Role));
        Write("rivalsWeapon", (int)RivalRules.FormFor(Role, Round, LastUsed(Card.Switch), NoMagic));
        foreach (var card in RivalRules.Skills(Role))
        {
            var skill = RivalRules.GetSkill(card);
            Write("rivalsUnlocked" + card, UnlockedFloor >= skill.Floor ? 1 : 0);
            bool ready = Ready(card);
            if (card == Card.Armor && (!Flag("showPlayerStatus") || Flag("playerDeath"))) ready = false;
            Write("rivalsCif" + card, ready ? "" : "[cif=false]");
        }
    }

    private bool Ready(Card card) => (card != Card.Armor || !ArmorLockedInFinale) &&
        RivalRules.CanUse(card, UnlockedFloor, Round, LastUsed(card), Read("forwardCharacter") == "partner", NoMagic);

    private bool HasCommittedModAction() => Read("turnCharacter") == "partner" &&
        Number("rivalsCommittedCard") == Number("selectCommand") &&
        TryCard(Number("selectCommand"), out var card) && UsesModAction(card);

    private void ResolveCommittedCard()
    {
        if (!TryCard(Number("rivalsCommittedCard"), out var card) ||
            Read("turnCharacter") != "partner" || Number("selectCommand") != (int)card) return;
        Write("rivalsCommittedCard", 0);
        Write("rivalsSwordHit", 0);
        if (card is Card.Sword or Card.Dagger)
        {
            Write(StealKey, 0);
            return;
        }

        if (RivalRules.GetSkill(card).Floor == 0 || !Ready(card)) return;
        Write(UsedKey(card), Round);
        var stats = CurrentStats();
        switch (card)
        {
            case Card.Steal: Write(StealKey, 1); break;
            case Card.Fire or Card.Freeze or Card.Thunder or Card.Poison:
                Write(Enchantment, RivalRules.Element(card));
                break;
            case Card.Doll:
                int hp = RivalRules.Amount(card, Number("partnerLv"), stats);
                Write(DollHp, hp);
                Write("rivalsDollMaxHp", hp);
                Write("rivalsDollStart", Round);
                Write("rivalsDollCount", 3);
                Write("rivalsDollCover", 1);
                Write("rivalsDollAttack", (stats.Dex + 2) / 3);
                ShowBattleFeedback("傀儡登场");
                break;
            case Card.Barrier:
                int barrierHp = RivalRules.Amount(card, Number("partnerLv"), stats);
                Write(BarrierHp, barrierHp);
                Write("rivalsBarrierMaxHp", barrierHp);
                Write("rivalsBarrierBlock", (stats.Mag + 1) / 2);
                Write("rivalsBarrierEnd", Round + 1);
                ShowBattleFeedback("屏障展开");
                break;
            case Card.Armor:
                Write(NoMagicKey, 1);
                Write("partnerMag", 0);
                break;
        }
    }

    private void OnAttackContact()
    {
        if (BattleRival != Rival.Outcast || Read("actionUserId") != "partner" ||
            !Flag("sectionEffectEligibleAction") || Read("actionType") != "attack" ||
            !TryCard(Number("rivalsCommittedCard"), out var card) || card is not (Card.Sword or Card.Dagger) ||
            Flag("rivalsSwordHit")) return;
        string enemy = Read("actionTargetId");
        if (!enemy.StartsWith("enemy", StringComparison.Ordinal)) return;
        Write("rivalsSwordHit", 1);

        var read = (Il2CppSystem.Func<StateType,float>)(state => GetState(enemy, state.ToString()));
        var write = (Il2CppSystem.Action<StateType,float>)((state, value) => SetState(enemy, state.ToString(), value));
        var transferred = new Dictionary<StateType, float>();
        var receive = (Il2CppSystem.Action<StateType,float>)((state, value) =>
        {
            var key = state.ToString();
            SetState("partner", key, GetState("partner", key) + value);
            if (value > 0) transferred[state] = transferred.GetValueOrDefault(state) + value;
        });
        StateSelectionEffects.Remove("Group_Buff", 2, true, read, write, receive);
        StateSelectionEffects.Remove("Group_Buff", 2, true, read, write, receive);
        if (transferred.Count > 0)
        {
            string gains = string.Join("  ", transferred.Select(pair =>
                ActionOrStateFunctions.GetStateValueReferenceText(pair.Key.ToString(), pair.Value)));
            ShowBattleFeedback($"{CustomFunctions.TranslateName(enemy)} → 放逐者\n{gains}");
        }

        string element = Read(Enchantment);
        if (element.Length == 0 || Round >= LastUsed(Card.Enchant) + 3 || Number("rivalsElementHit") == Round) return;
        var stacks = RivalRules.ElementStacks(element, CurrentStats().Mag);
        if (stacks <= 0) return;
        Write("rivalsElementHit", Round);
        SetState(enemy, element, GetState(enemy, element) + stacks);
    }

    private static float GetState(string owner, string state) => (float)CustomFunctions.GetStateValue(owner, state);
    private void SetState(string owner, string state, float value)
    {
        if (Enum.TryParse<StateType>(state, true, out var kind))
            Write(owner + "State" + kind, Math.Max(0, value).ToString(CultureInfo.InvariantCulture));
    }

    private void ShieldParty()
    {
        string target = Read("actionTargetId");
        string source = Read("actionUserId");
        if (target is not ("player" or "partner") || !source.StartsWith("enemy", StringComparison.Ordinal) ||
            Read("damageType") is not ("" or "attack" or "rangeAttack" or "killAttack")) return;
        if (!double.TryParse(Read("actionValue"), NumberStyles.Float, CultureInfo.InvariantCulture, out double damage) ||
            !double.IsFinite(damage) || damage <= 0) return;

        if (Number(BarrierHp) > 0 && Round <= Number("rivalsBarrierEnd"))
        {
            int block = Number("rivalsBarrierBlock");
            int hp = Number(BarrierHp);
            int usedBlock = (int)Math.Min(block, Math.Ceiling(damage));
            double unblocked = Math.Max(0, damage - usedBlock);
            int usedHealth = (int)Math.Min(hp, Math.Ceiling(unblocked));
            Write("rivalsBarrierBlock", block - usedBlock);
            Write(BarrierHp, hp - usedHealth);
            double remaining = Math.Max(0, unblocked - usedHealth);
            Write("actionValue", remaining.ToString(CultureInfo.InvariantCulture));
            ShowCoverFeedback("屏障", target, damage - remaining, remaining, hp == usedHealth ? "屏障破裂" : "");
        }
        else if (Number(DollHp) > 0 && Flag("rivalsDollCover"))
        {
            Write("rivalsDollCover", 0);
            int hp = Number(DollHp);
            int absorbed = (int)Math.Min(hp, Math.Ceiling(damage));
            Write(DollHp, hp - absorbed);
            double remaining = Math.Max(0, damage - absorbed);
            Write("actionValue", remaining.ToString(CultureInfo.InvariantCulture));
            ShowCoverFeedback("傀儡", target, damage - remaining, remaining, hp == absorbed ? "傀儡倒下" : "");
        }
    }

    private void RouteDollTurnEnd(Gosub command)
    {
        command.Path = new NamedString("Battle", "TurnEndEffectsLoop");
        if (BattleRival == Rival.Villainess && Number(DollHp) > 0 &&
            Number("rivalsDollLast") < Round && Number("rivalsDollStart") < Round)
            PrepareDollAttack(command);
    }

    private void PrepareDollAttack(Gosub command)
    {
        for (int index = 1; index <= Number("enemyMaxCount"); index++)
        {
            string enemy = "enemy" + index.ToString(CultureInfo.InvariantCulture);
            if (!EnemyCustomFunctions.IsEnemyTargetable(index) || Number(enemy + "Health") <= 0) continue;
            RegisterScript();
            Write("rivalsDollTarget", enemy);
            Write("rivalsDollLast", Round);
            command.Path = new NamedString(ScriptName, "DollAttack");
            string slot = EnemyCustomFunctions.CountTargetableEnemies(Number("enemyMaxCount")) > 1 ? $"（{index} 号位）" : "";
            string last = Number("rivalsDollCount") == 1 ? "\n最后一次攻击 · 随后退场" : "";
            ShowBattleFeedback($"傀儡攻击 {CustomFunctions.TranslateName(enemy)}{slot}{last}");
            return;
        }
    }

    private void OverrideCondition(Command command, string expression)
    {
        var condition = command.Cast<BeginIf>();
        var original = condition.Expression;
        commandRestore[command.Pointer] = () => condition.Expression = original;
        condition.Expression = expression;
    }

    private void RestoreCommand(Command command)
    {
        if (commandRestore.Remove(command.Pointer, out var restore)) restore();
    }

    private static string BattleScript()
    {
        var text = new StringBuilder("""
# Use
@set z=0
@goto Battle.RebuildBattleCommand if:rivalsCardAllowed!=1
@goto PlayableRivals.Elements if:selectCommand==-7113
@goto Battle.PlayerAttack

# Cards
@goto Battle.Magics if:turnCharacter!="partner"
""").AppendLine();
        foreach (var role in new[] { Rival.Outcast, Rival.Villainess, Rival.Dropout })
        {
            text.AppendLine($"@if rivalsRole==\"{RivalRules.Id(role)}\"");
            foreach (var card in RivalRules.Skills(role))
            {
                text.AppendLine($"    @if rivalsUnlocked{card}==1");
                const string dest = ScriptName + ".Use";
                text.AppendLine(Choice($"{{rivalsCif{card}}}{(int)card}", ((int)card).ToString(CultureInfo.InvariantCulture), dest));
                text.AppendLine("    @endif");
            }
            text.AppendLine("@endif");
        }
        text.AppendLine(Choice("{rivalsWeapon}", "{rivalsWeapon}", ScriptName + ".Use"));
        text.AppendLine("@goto Battle.ShowCommandTail");
        text.AppendLine("# Elements");
        text.AppendLine("@goto Battle.RebuildBattleCommand if:turnCharacter!=\"partner\"");
        text.AppendLine("@clearChoice * wait:false");
        foreach (var card in Elements)
        {
            var num = ((int)card).ToString(CultureInfo.InvariantCulture);
            text.AppendLine(Choice(num, num, ScriptName + ".Use"));
        }
        text.AppendLine("@choice \"返回\" goto:Battle.RebuildBattleCommand isTrans:false");
        text.AppendLine("@goto Battle.ShowCommandTail");
        text.AppendLine("""
# DollAttack
@gosub Action.PushActionContext
@set z=PushEmptyBattleActionSetContext()
@set actionUserId="partner"
@set actionTargetId=rivalsDollTarget
@set actionTarget="enemy"
@set actionType="attack"
@set damageType="attack"
@set actionTargetArray=""
@set damageBeforeText=""
@set actionCountTemp=0
@set actionExEffectIndex=-1
@set sectionEffectEligibleAction=false
@set hpSacrificeAtkBonus=0
@set actionValue=rivalsDollAttack
@gosub Action.Damage
@gosub Action.PopActionContext
@set z=PopBattleActionSetContext()
@set rivalsDollCount-=1
@set rivalsDollHp=0 if:rivalsDollCount<=0
@gosub Battle.TurnEndEffectsLoop
@return
""");
        return text.ToString();
    }

    private static string Choice(string title, string value, string destination) =>
        $"@choiceR \"{title}\" button:Buttons/CustomChoiceHandlerButtonBattleCommand handler:BattleCommand set:selectCommand=000 value:\"{value}\" goto:{destination} isTrans:false";
}
