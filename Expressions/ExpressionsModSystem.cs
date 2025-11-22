using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace Expressions;

public class ExpressionsModSystem : ModSystem
{
    public static ICoreClientAPI ClientApi;

    public static IClientNetworkChannel ClientNetworkChannel;
    public static IServerNetworkChannel ServerNetworkChannel;

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;

        ClientNetworkChannel = api.Network.RegisterChannel("exsel").RegisterMessageType<ExpressionSelectionPacket>();

        // api.Input.RegisterHotKey(
        //     "openExSel",
        //     "Open Expression Selection Menu",
        //     GlKeys.N,
        //     HotkeyType.GUIOrOtherControls
        // );
        // api.Input.SetHotKeyHandler("openExSel", _ => OpenExSel());

        var harmony = new Harmony(Mod.Info.ModID);

        var method =
            AccessTools.Method(typeof(GuiDialogCreateCharacter), "ComposeGuis");

        var patch1 = AccessTools.Method(typeof(ExpressionsModSystem), nameof(Prefix));

        harmony.Patch(method, new HarmonyMethod(patch1));


        api.ChatCommands.Create("expressionselect").WithAlias("exsel").WithDescription("Set your expression")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnSelectExpression);
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
        ServerNetworkChannel = api.Network.RegisterChannel("exsel").RegisterMessageType<ExpressionSelectionPacket>()
            .SetMessageHandler<ExpressionSelectionPacket>(OnExpressionSelectionPacket);

        api.ChatCommands.Create("expressions").WithAlias("expr").WithDescription("Set your expression")
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

        api.Event.PlayerNowPlaying += player =>
        {
            var behavior = player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
            if (behavior == null)
            {
                return;
            }

            if (behavior.AppliedSkinParts.All(sp => sp.PartCode != "eyebrow"))
            {
                UpdateExpression(player, "eyebrow", "neutral");
            }

            if (behavior.AppliedSkinParts.All(sp => sp.PartCode != "eye"))
            {
                UpdateExpression(player, "eye", "neutral");
            }

            if (behavior.AppliedSkinParts.All(sp => sp.PartCode != "mouth"))
            {
                UpdateExpression(player, "mouth", "neutral");
            }


            if (behavior.AppliedSkinParts.All(sp => sp.PartCode != "iriscolor"))
            {
                var field = behavior.GetType()
                    .GetField("skintree", BindingFlags.Instance | BindingFlags.NonPublic);

                if (field == null)
                {
                    return;
                }

                var tree = (ITreeAttribute)field.GetValue(behavior);

                if (tree == null)
                {
                    return;
                }

                var value = tree.GetTreeAttribute("appliedParts").GetAsString("eyecolor");

                if (value == null)
                {
                    return;
                }


                UpdateExpression(player, "iriscolor", value);
            }
        };
    }

    private void OnExpressionSelectionPacket(IServerPlayer fromPlayer, ExpressionSelectionPacket packet)
    {
        UpdateExpression(fromPlayer, "eyebrow", packet.EyebrowsVariant);
        UpdateExpression(fromPlayer, "eye", packet.EyesVariant);
        UpdateExpression(fromPlayer, "mouth", packet.MouthVariant);
    }

    private TextCommandResult OnSelectExpression(TextCommandCallingArgs args)
    {
        new GuiDialogExpressionSelector(ClientApi, ClientApi.ModLoader.GetModSystem<CharacterSystem>()).TryOpen();

        return TextCommandResult.Success();
    }

    private TextCommandResult OnMouthCommand(TextCommandCallingArgs args)
    {
        var serverPlayer = args.Caller.Player as ServerPlayer;
        var text = args[0].ToString();
        var result = UpdateExpression(serverPlayer, "mouth", text);
        return result
            ? TextCommandResult.Success("Set mouth to " + text)
            : TextCommandResult.Error("No such style for " + text);
    }

    private TextCommandResult OnEyeCommand(TextCommandCallingArgs args)
    {
        var serverPlayer = args.Caller.Player as ServerPlayer;
        var text = args[0].ToString();
        var result = UpdateExpression(serverPlayer, "eye", text);
        return result
            ? TextCommandResult.Success("Set eye to " + text)
            : TextCommandResult.Error("No such style for " + text);
    }

    private TextCommandResult OnEyebrowCommand(TextCommandCallingArgs args)
    {
        var serverPlayer = args.Caller.Player as ServerPlayer;
        var text = args[0].ToString();
        var result = UpdateExpression(serverPlayer, "eyebrow", text);
        return result
            ? TextCommandResult.Success("Set eyebrow to " + text)
            : TextCommandResult.Error("No such style for " + text);
    }

    public bool UpdateExpression(IServerPlayer fromPlayer, string facepart, string value)
    {
        var behavior = fromPlayer.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
        if (behavior == null)
        {
            return false;
        }

        if (!behavior.AvailableSkinPartsByCode[facepart].VariantsByCode.ContainsKey(value))
        {
            return false;
        }

        behavior.selectSkinPart(facepart, value);
        fromPlayer.Entity.WatchedAttributes.MarkPathDirty("skinConfig");
        fromPlayer.BroadcastPlayerData();

        return true;
    }
}