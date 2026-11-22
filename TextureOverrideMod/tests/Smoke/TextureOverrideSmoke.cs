using System.Collections;
using Il2Cpp;
using Il2CppNaninovel;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(TextureOverrideSmoke), "Texture Override Smoke", "0.1.0", "Local test")]
[assembly: MelonGame("DeathKumiai", "Annihilated")]

public sealed class TextureOverrideSmoke : MelonMod
{
    private bool started;
    private bool requireBattle;
    private bool expectOriginal;
    private bool expectOriginalSword;
    private bool requireSheet;
    private bool requireDragonEye;
    private bool requireDragonBody;

    public override void OnInitializeMelon()
    {
        var args = Environment.GetCommandLineArgs();
        requireBattle = args.Contains("--texture-smoke-battle");
        expectOriginal = args.Contains("--texture-smoke-original");
        expectOriginalSword = expectOriginal || args.Contains("--texture-smoke-invalid-sword");
        requireSheet = args.Contains("--texture-smoke-sheet");
        requireDragonEye = args.Contains("--texture-smoke-dragon-eye");
        requireDragonBody = args.Contains("--texture-smoke-dragon-body");
    }

    public override void OnUpdate()
    {
        if (Time.realtimeSinceStartup > 75)
            Fail("Timed out");
        if (started || !Engine.Initialized)
            return;
        var service = Engine.GetService<ItemService>();
        if (service == null || service.itemDataBase == null)
            return;
        started = true;
        MelonCoroutines.Start(Check(service));
    }

    private IEnumerator Check(ItemService service)
    {
        var sword = service.GetItemIconSpriteAsync("ironSword");
        while (!sword.IsCompleted)
            yield return null;
        var potion = service.GetItemIconSpriteAsync("potion");
        while (!potion.IsCompleted)
            yield return null;

        try
        {
            var ironSword = sword.GetAwaiter().GetResult();
            RequirePicture(ironSword, "ironSword", 65, 65, expectOriginalSword);
            RequirePicture(potion.GetAwaiter().GetResult(), "potion", 65, 65, true);
            Require(service.TryGetLoadedItemIconSprite("ironSword", out var cached) && cached == ironSword,
                "The preloaded sword and async sword icon differ");
        }
        catch (Exception ex)
        {
            Fail(ex.ToString());
            yield break;
        }

        if (requireDragonEye)
        {
            var dragonEye = service.GetItemIconSpriteAsync("dragonEye");
            while (!dragonEye.IsCompleted)
                yield return null;
            var seaDragonEye = service.GetItemIconSpriteAsync("seaDragonEye");
            while (!seaDragonEye.IsCompleted)
                yield return null;

            try
            {
                RequirePicture(dragonEye.GetAwaiter().GetResult(), "dragonEye", 65, 65, false);
                RequirePicture(seaDragonEye.GetAwaiter().GetResult(), "seaDragonEye", 65, 65, true);
            }
            catch (Exception ex)
            {
                Fail(ex.ToString());
                yield break;
            }
        }

        if (requireBattle || requireDragonBody)
        {
            var enemies = Engine.GetService<EnemyService>();
            Sprite? displayed = null;
            BattleEnemySpriteController? displayedBy = null;
            while (displayed == null)
            {
                if (enemies.enemyControllerList != null)
                {
                    foreach (var enemy in enemies.enemyControllerList)
                    {
                        var controller = enemy?.battleEnemySpriteController;
                        var image = controller?.enemyImage;
                        if (image == null || !image.isActiveAndEnabled || image.sprite == null)
                            continue;
                        var imageSprite = image.sprite;
                        LoggerInstance.Msg($"Battle enemy image = {imageSprite.name}/{imageSprite.texture?.name}");
                        if (imageSprite.name.StartsWith("goblin", StringComparison.OrdinalIgnoreCase))
                        {
                            displayed = imageSprite;
                            displayedBy = controller;
                            break;
                        }
                    }
                }
                yield return null;
            }
            try
            {
                RequirePicture(displayed, "goblin", 511, 481, expectOriginal);
                LoggerInstance.Msg("Live goblin battle image checked.");
            }
            catch (Exception ex)
            {
                Fail(ex.ToString());
                yield break;
            }

            if (requireDragonBody)
            {
                var dragon = displayedBy!.LoadEnemyVisualAssetByKey("Naninovel/Characters/Enemy/dragon", true);
                while (!dragon.IsCompleted)
                    yield return null;
                try
                {
                    var sprite = dragon.GetAwaiter().GetResult()?.Frames?.FirstOrDefault();
                    RequirePicture(sprite, "dragon", 800, 800, false);
                    LoggerInstance.Msg("Native dragon visual loader returned the replacement body.");
                }
                catch (Exception ex)
                {
                    Fail(ex.ToString());
                    yield break;
                }
            }

            if (requireSheet)
            {
                var sheet = displayedBy!.LoadEnemyVisualAssetByKey("Naninovel/Characters/Enemy/priest", true);
                while (!sheet.IsCompleted)
                    yield return null;
                try
                {
                    var frames = sheet.GetAwaiter().GetResult()?.Frames;
                    Require(frames != null && frames.Length == 10, $"Expected ten priest sprite frames, got {frames?.Length ?? 0}");
                    var first = frames![0];
                    RequirePicture(first, "priest_x10", 11010, 1038, false);
                    var positions = new HashSet<float>();
                    foreach (var frame in frames)
                    {
                        Require(frame.texture == first.texture, "Expected all priest frames to share the replacement texture");
                        positions.Add(frame.rect.x);
                    }
                    Require(positions.Count == 10, "The priest frames no longer select distinct sheet regions");
                    LoggerInstance.Msg("Native enemy visual loader returned ten replacement frames with distinct rectangles.");
                }
                catch (Exception ex)
                {
                    Fail(ex.ToString());
                    yield break;
                }
            }
        }

        LoggerInstance.Msg("TEXTURE OVERRIDE SMOKE PASSED");
        Application.Quit(0);
    }

    private void RequirePicture(Sprite? sprite, string name, int width, int height, bool original)
    {
        Require(sprite != null && sprite.texture != null, $"Missing {name} sprite");
        var texture = sprite!.texture!;
        LoggerInstance.Msg($"{name} texture = {texture.name}, {texture.width} x {texture.height}");
        Require(texture.width == width && texture.height == height, $"Wrong dimensions for {name}");
        Require(texture.name == (original ? name : $"PNG/{name}"), $"Unexpected texture for {name}: {texture.name}");
    }

    private static void Require(bool pass, string reason)
    {
        if (!pass)
            throw new InvalidOperationException(reason);
    }

    private void Fail(string error)
    {
        LoggerInstance.Error(error);
        Application.Quit(21);
    }
}
