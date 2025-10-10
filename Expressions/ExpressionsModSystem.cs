using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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
            .WithArgs(api.ChatCommands.Parsers.Word("expression"))
            .HandleWith(OnExpressCommand);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Logger.Notification("Hello from template mod client side: " + Lang.Get("expressions:hello"));
    }

    public TextCommandResult OnExpressCommand(TextCommandCallingArgs args)
    {
        var serverPlayer = args.Caller.Player as ServerPlayer;
        var text = args[0].ToString();
        UpdateExpression(serverPlayer, text);
        return TextCommandResult.Success("Set expression to " + text);
    }

    public void UpdateExpression(IServerPlayer fromPlayer, string value)
    {
        var behavior = fromPlayer.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
        if (behavior == null)
        {
            return;
        }

        behavior.selectSkinPart("facialexpression", value);
        fromPlayer.Entity.WatchedAttributes.MarkPathDirty("skinConfig");
        fromPlayer.BroadcastPlayerData();
    }
}