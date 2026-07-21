using System.Linq;
using System.Runtime.CompilerServices;
using PlayerModelLib;
using Vintagestory.API.Common;

namespace Expressions;

internal static class RacialEqualityCompat
{
    internal static void Register(ICoreAPI api)
    {
        if (!api.ModLoader.IsModEnabled("playermodellib")) return;
        RegisterInternal(api);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterInternal(ICoreAPI api)
    {
        var modelsSystem = api.ModLoader.GetModSystem<CustomModelsSystem>();
        if (modelsSystem == null) return;
        modelsSystem.OnCustomModelsLoaded += () => ProcessModels(modelsSystem);
    }

    private static void ProcessModels(CustomModelsSystem modelsSystem)
    {
        foreach (var (_, model) in modelsSystem.CustomModels)
        {
            foreach (var part in model.SkinPartsArray)
            {
                if (part.VariantsByCode == null || part.VariantsByCode.Count == 0)
                    part.VariantsByCode = part.Variants.ToDictionary(v => v.Code, v => v);
            }

            if (model.SkinParts.TryGetValue("iriscolor", out var irisPart) && irisPart is SkinnablePartExtended ext)
                ext.TargetSkinParts = ["eye"];
        }
    }
}
