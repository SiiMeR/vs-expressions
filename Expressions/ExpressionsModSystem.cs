using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace Expressions;

public class ExpressionsModSystem : ModSystem
{
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {
        api.Logger.Notification("Hello from template mod: " + api.Side);
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