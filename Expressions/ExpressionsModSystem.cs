using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace Expressions;

public class ExpressionsModSystem : ModSystem
{
    public override void StartClientSide(ICoreClientAPI api)
    {
        var harmony = new Harmony(Mod.Info.ModID);

        var method =
            AccessTools.Method(typeof(GuiDialogCreateCharacter), "ComposeGuis");

        var patch1 = AccessTools.Method(typeof(ExpressionsModSystem), nameof(Prefix));

        harmony.Patch(method, new HarmonyMethod(patch1));

        base.StartClientSide(api);
    }

    private static void Prefix(GuiDialogCreateCharacter __instance)
    {
        var dlgHeightRef = AccessTools.FieldRefAccess<GuiDialogCreateCharacter, int>("dlgHeight");

        // Modify it just before the method runs
        dlgHeightRef(__instance) = GetCustomHeight(__instance);
    }

    private static int GetCustomHeight(GuiDialogCreateCharacter instance)
    {
        return 625;
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        api.ChatCommands.Create("expressions").WithDescription("Set your expression")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("eyebrow")
            .WithArgs(api.ChatCommands.Parsers.Word("eyebrow"))
            .HandleWith(OnEyebrowCommand)
            .EndSubCommand()
            .BeginSubCommand("eye")
            .WithArgs(api.ChatCommands.Parsers.Word("eye"))
            .HandleWith(OnEyeCommand)
            .EndSubCommand()
            .BeginSubCommand("mouth")
            .WithArgs(api.ChatCommands.Parsers.Word("mouth"))
            .HandleWith(OnMouthCommand)
            .EndSubCommand();
    }

    private TextCommandResult OnMouthCommand(TextCommandCallingArgs args)
    {
        var serverPlayer = args.Caller.Player as ServerPlayer;
        var text = args[0].ToString();
        UpdateExpression(serverPlayer, "mouth", text);
        return TextCommandResult.Success("Set mouth to " + text);
    }

    private TextCommandResult OnEyeCommand(TextCommandCallingArgs args)
    {
        var serverPlayer = args.Caller.Player as ServerPlayer;
        var text = args[0].ToString();
        UpdateExpression(serverPlayer, "eye", text);
        return TextCommandResult.Success("Set eye to " + text);
    }

    private TextCommandResult OnEyebrowCommand(TextCommandCallingArgs args)
    {
        var serverPlayer = args.Caller.Player as ServerPlayer;
        var text = args[0].ToString();
        UpdateExpression(serverPlayer, "eyebrow", text);
        return TextCommandResult.Success("Set eyebrow to " + text);
    }

    public void UpdateExpression(IServerPlayer fromPlayer, string facepart, string value)
    {
        var behavior = fromPlayer.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
        if (behavior == null)
        {
            return;
        }

        behavior.selectSkinPart(facepart, value);
        fromPlayer.Entity.WatchedAttributes.MarkPathDirty("skinConfig");
        fromPlayer.BroadcastPlayerData();
    }
}