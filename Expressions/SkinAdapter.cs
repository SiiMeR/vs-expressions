using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Expressions;

internal sealed class SkinAdapter
{
    private readonly EntityBehaviorExtraSkinnable _skin;

    private SkinAdapter(EntityBehaviorExtraSkinnable skin) => _skin = skin;

    public static SkinAdapter? Get(Entity entity)
    {
        if (entity.Api?.ModLoader.IsModEnabled("playermodellib") == true)
        {
            var pml = GetPmlAdapter(entity);
            if (pml != null) return pml;
        }
        var vanilla = entity.GetBehavior<EntityBehaviorExtraSkinnable>();
        if (vanilla != null) return new SkinAdapter(vanilla);
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SkinAdapter? GetPmlAdapter(Entity entity)
    {
        var pml = entity.GetBehavior<PlayerModelLib.PlayerSkinBehavior>();
        return pml == null ? null : new SkinAdapter(pml);
    }

    public IEnumerable<SkinnablePart> AvailableSkinParts => _skin.AvailableSkinParts;

    public IEnumerable<AppliedSkinnablePartVariant> AppliedSkinParts => _skin.AppliedSkinParts;

    public SkinnablePart? GetPart(string code)
    {
        _skin.AvailableSkinPartsByCode.TryGetValue(code, out var part);
        return part;
    }

    public void SelectSkinPart(string partCode, string variantCode) =>
        _skin.selectSkinPart(partCode, variantCode);
}
