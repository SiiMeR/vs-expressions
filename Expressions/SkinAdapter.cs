using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Expressions;

internal sealed class SkinAdapter
{
    private readonly EntityBehaviorExtraSkinnable? _vanilla;
    private readonly PlayerModelLib.PlayerSkinBehavior? _pml;

    private SkinAdapter(EntityBehaviorExtraSkinnable vanilla) => _vanilla = vanilla;
    private SkinAdapter(PlayerModelLib.PlayerSkinBehavior pml) => _pml = pml;

    public static SkinAdapter? Get(Entity entity)
    {
        var pml = entity.GetBehavior<PlayerModelLib.PlayerSkinBehavior>();
        if (pml != null) return new SkinAdapter(pml);
        var vanilla = entity.GetBehavior<EntityBehaviorExtraSkinnable>();
        if (vanilla != null) return new SkinAdapter(vanilla);
        return null;
    }

    public IEnumerable<SkinnablePart> AvailableSkinParts =>
        _pml != null ? (IEnumerable<SkinnablePart>)_pml.AvailableSkinParts.Get() : _vanilla!.AvailableSkinParts;

    public IEnumerable<AppliedSkinnablePartVariant> AppliedSkinParts =>
        _pml != null ? (IEnumerable<AppliedSkinnablePartVariant>)_pml.AppliedSkinParts.Get() : _vanilla!.AppliedSkinParts;

    public SkinnablePart? GetPart(string code)
    {
        if (_pml != null) return _pml.AvailableSkinPartsByCode.GetValue(code);
        _vanilla!.AvailableSkinPartsByCode.TryGetValue(code, out var part);
        return part;
    }

    public void SelectSkinPart(string partCode, string variantCode)
    {
        if (_pml != null)
            _pml.SelectSkinPart(partCode, variantCode);
        else
            _vanilla!.selectSkinPart(partCode, variantCode);
    }
}
