using HarmonyLib;
using Il2Cpp;
using Il2CppDemo;
using Il2CppNaninovel;

namespace CupCompetition;

public sealed partial class CupMod
{
    // These rewards are scripted in Dungeon.eventN rather than in the item or encounter tables.
    private static readonly Dictionary<int, string[]> CurseEventRewards = new()
    {
        [1] = new[] { "trap" }, [3] = new[] { "poleSpear", "spikeBlade" },
        [4] = new[] { "junkHammer", "debriSpear", "golemHand" }, [5] = new[] { "emptyTreasure" },
        [6] = new[] { "thunderGemS" }, [9] = new[] { "iceGemS", "rainMaker", "thunderCaller", "icicles", "poleSpear", "club" },
        [11] = new[] { "fireGemS", "fire", "fireRod" }, [12] = new[] { "toxicTonic", "colossionFlower", "corrosionStiletto" },
        [13] = new[] { "scythe", "painRipper" }, [14] = new[] { "hunterBow", "shotBow" },
        [21] = new[] { "metamolizer", "brokenMirror" }, [22] = new[] { "slate" },
        [23] = new[] { "bunanShimeji", "poisonMushroom" }, [24] = new[] { "giantSword" },
        [25] = new[] { "fakeGold" }
    };

    private int cursePoolDungeon;
    private HashSet<string>? cursePool;

    private void InstallCursePoolHooks()
    {
        Hook(typeof(ItemService), nameof(ItemService.GetCurseGenerateCandidateResourceNames), nameof(UseCupCurseCandidates));
        Hook(typeof(ItemService), nameof(ItemService.GetGlobalGotItemResourceNameSet), nameof(UseCupGlobalCurseCandidates));
        var localNames = AccessTools.Method(typeof(ItemService), nameof(ItemService.GetLocalGotItemResourceNameSet));
        HarmonyInstance.Patch(localNames, postfix: new HarmonyMethod(typeof(CupMod), nameof(IncludeCupLocalCurseCandidates)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(ItemScrollList), nameof(ItemScrollList.UpdateCurseGenerateCache)),
            postfix: new HarmonyMethod(typeof(CupMod), nameof(SyncCupCurseSelectionCache)));
    }

    private static bool UseCupCurseCandidates(ref Il2CppSystem.Collections.Generic.IReadOnlyList<string> __result)
    {
        if (Active is not { cursePool: { } pool } cup || !cup.Run.Running) return true;
        var result = new Il2CppSystem.Collections.Generic.List<string>();
        foreach (string name in pool.OrderBy(name => name, StringComparer.Ordinal)) result.Add(name);
        __result = result.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>();
        return false;
    }

    private static bool UseCupGlobalCurseCandidates(ref Il2CppSystem.Collections.Generic.HashSet<string> __result)
    {
        if (Active is not { cursePool: { } pool } cup || !cup.Run.Running) return true;
        var allowed = new Il2CppSystem.Collections.Generic.HashSet<string>();
        foreach (string name in pool) allowed.Add(name);
        __result = allowed;
        return false;
    }

    private static void IncludeCupLocalCurseCandidates(Il2CppSystem.Collections.Generic.HashSet<string> __result)
    {
        if (Active is not { cursePool: { } pool } cup || !cup.Run.Running || __result == null) return;
        foreach (string name in pool) __result.Add(name);
    }

    private static void SyncCupCurseSelectionCache(ItemScrollList __instance)
    {
        if (Active is not { cursePool: { } pool } cup || !cup.Run.Running ||
            !__instance.IsCurseGenerationList()) return;
        // The UI builds its own discovery caches directly, independently of ItemService's validation sets.
        __instance.cachedGlobalGotItemResourceNames.Clear();
        __instance.cachedLocalGotItemResourceNames.Clear();
        foreach (string name in pool)
        {
            __instance.cachedGlobalGotItemResourceNames.Add(name);
            __instance.cachedLocalGotItemResourceNames.Add(name);
        }
    }

    private void ObserveCurseMenu(string script, int line)
    {
        if (script != "GameOver") return;
        // Only the death-menu entry calls BuyByCurseCore through line 269. Its other callers have their own rules.
        if (line is 0 or 270 or 322)
        {
            cursePoolDungeon = 0;
            cursePool = null;
        }
        else if (line == 269) cursePoolDungeon = Number("dungeonId");
        else if (line == 299 && cursePoolDungeon > 0)
            cursePool = BuildCursePool(cursePoolDungeon);
    }

    private HashSet<string> BuildCursePool(int dungeon)
    {
        var items = Engine.GetService<ItemService>();
        var enemyData = Engine.GetService<EnemyService>().enemyDataBase.datas;
        var map = Engine.GetService<MapService>();
        var pool = new HashSet<string>(StringComparer.Ordinal);
        for (int floor = 1; floor < dungeon && floor <= 4; floor++)
            AddCurseFloor(pool, floor, map, items, enemyData);
        // Dungeon IDs 5–9 are alternative floor-5 routes. Floor 6 follows the priest's route (ID 7).
        if (dungeon == 10) AddCurseFloor(pool, 7, map, items, enemyData);
        AddCurseFloor(pool, dungeon, map, items, enemyData);
        pool.RemoveWhere(name => items.FindExactItemDataByResourceName(name) is not { price: > 0 });
        return pool;
    }

    private void AddCurseFloor(HashSet<string> pool, int dungeon, MapService map, ItemService items,
        IEnumerable<EnemyData> enemyData)
    {
        if (dungeon <= 4)
        {
            foreach (var item in items.TreasureItemCandidates)
            {
                if (item.IsAvailableInDungeon(dungeon))
                    pool.Add(item.resourceName);
            }
        }

        var encounters = new HashSet<string>(StringComparer.Ordinal);
        int encounterFloor = dungeon is >= 5 and <= 9 ? 5 : dungeon;
        foreach (var sets in new[] { map.enemyEncountSetData, map.eliteEnemyEncountSetData })
        {
            if (sets?.dungeons == null || encounterFloor > sets.dungeons.Count) continue;
            foreach (var section in sets.dungeons[encounterFloor - 1].sections)
                foreach (var entry in section.entries)
                    AddResourceNames(encounters, entry.enemyId);
        }

        var events = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tile in map.mapDataBase.datas)
        {
            if (tile.dungeonId != dungeon) continue;
            AddResourceNames(encounters, tile.option);
            if (dungeon <= 4)
                foreach (bool danger in new[] { false, true })
                    if (map.GetRandomEventSet(dungeon, tile.sectionId, danger) is { } options)
                        events.UnionWith(options);
        }

        foreach (string option in events)
        {
            if (!int.TryParse(option.StartsWith("event", StringComparison.OrdinalIgnoreCase) ? option[5..] : option, out int eventId))
                continue;
            if (CurseEventRewards.TryGetValue(eventId, out var rewards)) pool.UnionWith(rewards);
            if (eventId == 17) pool.Add(dungeon switch { 1 => "vampireFang", 2 => "fire", 3 => "tentacle", _ => "eaterFang" });
            if (eventId == 28) AddResourceNames(encounters, dungeon switch
            {
                1 => "zombie", 2 => "steelGolem,golem", 3 => "seaMedusa,seaSummoner", _ => "magicer,greaterSkeleton"
            });
        }

        // Named bosses use scripted forms and additional enemies absent from the map tables.
        switch (dungeon)
        {
            case 1:
                encounters.Add("outcast");
                break;
            case 2:
                AddResourceNames(encounters, "villainessWithDoll,doll,dealerHandSword,dealerHandShield,dealerHandLance,dealerHandTea");
                pool.Add("pikapikaSword");
                break;
            case 3:
                AddResourceNames(encounters, "seaDragonPart,seaSummon");
                break;
            case 4:
                AddResourceNames(encounters, "giantHand,slimeForm2Me,slime,soldier,magicSoldier,subCommander,commander");
                break;
            case 5:
                AddResourceNames(encounters, "outcast,villainessWithDoll,dropout");
                pool.UnionWith(new[] { "destructionMagicSword", "magiaTakt", "doll" });
                break;
            case 6:
                AddResourceNames(encounters, "warriorIllusionA,warriorHydraIllusion,warriorIllusionC");
                break;
            case 7:
            case 8:
            case 9:
                AddResourceNames(encounters, "priestSlime,priestUltimatePlayerA,priestUltimatePlayerB");
                break;
        }

        foreach (var enemy in enemyData)
            if (encounters.Contains(enemy.resourceName) && enemy.stealItems != null)
                pool.UnionWith(enemy.stealItems);
    }

    private static void AddResourceNames(HashSet<string> names, string? list)
    {
        if (list != null)
            names.UnionWith(list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
