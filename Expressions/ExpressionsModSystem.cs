using System.Linq;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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
        RacialEqualityCompat.Register(api);

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
        RacialEqualityCompat.Register(api);
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
            var adapter = SkinAdapter.Get(player.Entity);
            if (adapter == null) return;

            var applied = adapter.AppliedSkinParts;

            foreach (var part in adapter.AvailableSkinParts.Where(sp => sp.Code is "facialexpression" or "eyebrow" or "eye" or "mouth"))
            {
                if (applied.Any(sp => sp.PartCode == part.Code)) continue;
                var variantCode = part.Variants.Any(v => v.Code == "neutral")
                    ? "neutral"
                    : part.Variants.FirstOrDefault()?.Code;
                if (variantCode != null)
                    UpdateExpression(player, part.Code, variantCode);
            }

            if (applied.All(sp => sp.PartCode != "iriscolor"))
            {
                var value = player.Entity.WatchedAttributes
                    .GetTreeAttribute("skinConfig")
                    ?.GetTreeAttribute("appliedParts")
                    ?.GetAsString("eyecolor");

                if (value != null)
                    UpdateExpression(player, "iriscolor", value);
            }
        };
    }

    private void OnExpressionSelectionPacket(IServerPlayer fromPlayer, ExpressionSelectionPacket packet)
    {
        if (packet.FacialExpressionVariant != null)
            UpdateExpression(fromPlayer, "facialexpression", packet.FacialExpressionVariant);
        if (packet.EyebrowsVariant != null)
            UpdateExpression(fromPlayer, "eyebrow", packet.EyebrowsVariant);
        if (packet.EyesVariant != null)
            UpdateExpression(fromPlayer, "eye", packet.EyesVariant);
        if (packet.MouthVariant != null)
            UpdateExpression(fromPlayer, "mouth", packet.MouthVariant);
    }

    private TextCommandResult OnSelectExpression(TextCommandCallingArgs args)
    {
        if (!GuiDialogExpressionSelector.HasExpressionParts(ClientApi))
            return TextCommandResult.Error("Your character race does not support expressions.");

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
        var adapter = SkinAdapter.Get(fromPlayer.Entity);
        if (adapter == null) return false;

        var part = adapter.GetPart(facepart);
        if (part == null) return false;
        bool hasVariant = part.VariantsByCode?.Count > 0
            ? part.VariantsByCode.ContainsKey(value)
            : part.Variants.Any(v => v.Code == value);
        if (!hasVariant) return false;

        adapter.SelectSkinPart(facepart, value);
        fromPlayer.Entity.WatchedAttributes.MarkAllDirty();
        fromPlayer.BroadcastPlayerData();

        return true;
    }
}