using System;
using System.Diagnostics;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Expressions;

public class GuiDialogExpressionSelector : GuiDialog
{
    private readonly float charZoom = 1.7f;
    private readonly CharacterSystem modSys;
    public bool charNaked = true;
    private int currentClassIndex;

    protected int dlgHeight = 433 + 80;
    protected ElementBounds insetSlotBounds;

    protected Action<GuiComposer> onBeforeCompose;
    private int rows = 7;

    public string[] variantCategories = ["standard"];

    public GuiDialogExpressionSelector(ICoreClientAPI capi, CharacterSystem modSys) : base(capi)
    {
        this.modSys = modSys;
    }

    protected virtual bool AllowClassSelection => true;
    protected virtual bool AllowKeepCurrent => false;


    public override string ToggleKeyCombinationCode => null;

    public override bool PrefersUngrabbedMouse => true;


    public override float ZSize => (float)GuiElement.scaled(280);

    protected virtual bool AllowedSkinPartSelection(string code)
    {
        return true;
    }

    protected void ComposeGuis()
    {
        var pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
        var ypos = 20 + pad;

        var bgBounds = ElementBounds.FixedSize(487, dlgHeight).WithFixedPadding(GuiStyle.ElementToDialogPadding);

        var dialogBounds = ElementBounds.FixedSize(527, dlgHeight + 40).WithAlignment(EnumDialogArea.CenterMiddle)
            .WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0);

        GuiComposer customizeExpressionComposer;
        Composers["customizeexpression"] = customizeExpressionComposer =
                capi.Gui
                    .CreateCompo("customizeexpression", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(
                        Lang.Get("Customize Expression"),
                        OnTitleBarClose)
                    .BeginChildElements(bgBounds)
            ;

        var bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
        bh.hideClothing = false;

        var skinMod = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();

        bh.hideClothing = charNaked;

        var essr = capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer;
        essr.TesselateShape();

        var colorIconSize = 22;

        var leftColBounds = ElementBounds.Fixed(0, ypos, 204, dlgHeight - 59).FixedGrow(2 * pad, 2 * pad);

        insetSlotBounds = ElementBounds.Fixed(0, ypos + 2, 265, leftColBounds.fixedHeight - 2 * pad - 10)
            .FixedRightOf(leftColBounds, 10);

        ElementBounds bounds = null;
        ElementBounds prevbounds = null;

        double leftX = 0;


        var skinPartsToRender =
            skinMod.AvailableSkinParts.Where(sp => sp.Code is "eyebrow" or "eye" or "mouth");

        foreach (var skinpart in skinPartsToRender)
        {
            bounds = ElementBounds.Fixed(leftX,
                prevbounds == null || prevbounds.fixedY == 0 ? -10 : prevbounds.fixedY + 8, colorIconSize,
                colorIconSize);
            if (!AllowedSkinPartSelection(skinpart.Code))
            {
                continue;
            }

            var code = skinpart.Code;

            var appliedVar = skinMod.AppliedSkinParts.FirstOrDefault(sp => sp.PartCode == code);

            var variants = skinpart.Variants.Where(p =>
                variantCategories.Contains(p.Category) || (AllowKeepCurrent && p.Code == appliedVar.Code)).ToArray();

            if (skinpart.Type == EnumSkinnableType.Texture && !skinpart.UseDropDown)
            {
                var colors = variants.Select(p => p.Color).ToArray();
                var selectedIndex = 0;

                customizeExpressionComposer.AddRichtext(Lang.Get("skinpart-" + code), CairoFont.WhiteSmallText(),
                    bounds = bounds.BelowCopy(0, 10).WithFixedSize(210, 22));
                customizeExpressionComposer.AddColorListPicker(colors, index => onToggleSkinPart(code, index),
                    bounds = bounds.BelowCopy().WithFixedSize(colorIconSize, colorIconSize), 180, "picker-" + code);

                for (var i = 0; i < variants.Length; i++)
                {
                    if (variants[i].Code == appliedVar?.Code)
                    {
                        selectedIndex = i;
                    }

                    var picker = customizeExpressionComposer.GetColorListPicker("picker-" + code + "-" + i);
                    picker.ShowToolTip = true;
                    picker.TooltipText = Lang.Get("color-" + variants[i].Code);
#if DEBUG
                    if (!Lang.HasTranslation("color-" + variants[i].Code))
                    {
                        Debug.WriteLine("\"" + Lang.Get("color-" + skinpart.Variants[i].Code) + "\": \"" +
                                        skinpart.Variants[i].Code + "\",");
                    }
#endif
                }

                customizeExpressionComposer.ColorListPickerSetValue("picker-" + code, selectedIndex);
            }
            else
            {
                var selectedIndex = Math.Max(0, variants.IndexOf(v => v.Code == appliedVar?.Code));
                var names = variants.Select(v => Lang.Get("skinpart-" + code + "-" + v.Code)).ToArray();
                var values = variants.Select(v => v.Code).ToArray();
#if DEBUG
                for (var i = 0; i < names.Length; i++)
                {
                    var v = variants[i];
                    if (!Lang.HasTranslation("skinpart-" + code + "-" + v.Code))
                    {
                        Debug.WriteLine("\"" + names[i] + "\": \"" + v.Code + "\",");
                    }
                }
#endif

                customizeExpressionComposer.AddRichtext(Lang.Get("skinpart-" + code), CairoFont.WhiteSmallText(),
                    bounds = bounds.BelowCopy(0, 10).WithFixedSize(210, 22));

                var tooltip = Lang.GetIfExists("skinpartdesc-" + code);
                if (tooltip != null)
                {
                    customizeExpressionComposer.AddHoverText(tooltip, CairoFont.WhiteSmallText(), 300,
                        bounds = bounds.FlatCopy());
                }

                customizeExpressionComposer.AddDropDown(values, names, selectedIndex,
                    (variantcode, selected) => onToggleSkinPart(code, variantcode),
                    bounds = bounds.BelowCopy().WithFixedSize(200, 25), "dropdown-" + code);
            }

            prevbounds = bounds.FlatCopy();
        }

        customizeExpressionComposer
            .AddInset(insetSlotBounds, 2)
            .AddSmallButton(Lang.Get("Confirm Expression"), OnNext,
                ElementBounds.Fixed(0, dlgHeight - 25).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(12, 6))
            ;

        onBeforeCompose?.Invoke(customizeExpressionComposer);

        customizeExpressionComposer.Compose();
    }


    protected virtual void onToggleSkinPart(string partCode, string variantCode)
    {
        var skinMod = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
        skinMod.selectSkinPart(partCode, variantCode);
    }

    protected virtual void onToggleSkinPart(string partCode, int index)
    {
        var skinMod = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
        var variantCode = skinMod.AvailableSkinPartsByCode[partCode].Variants[index].Code;
        skinMod.selectSkinPart(partCode, variantCode);
    }

    protected virtual bool OnNext()
    {
        TryClose();
        return true;
    }

    public override void OnGuiOpened()
    {
        var charclass = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass");
        if (AllowClassSelection)
        {
            if (charclass != null)
            {
                modSys.setCharacterClass(capi.World.Player.Entity, charclass);
            }
            else
            {
                modSys.setCharacterClass(capi.World.Player.Entity, modSys.characterClasses[0].Code);
            }
        }

        ComposeGuis();
        var essr = capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer;
        essr.TesselateShape();
    }


    public override void OnGuiClosed()
    {
        if (modSys != null)
        {
            var chclass = modSys.characterClasses[currentClassIndex];
            // modSys.ClientSelectionDone(characterInv, chclass.Code, didSelect);
        }

        var bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
        bh.hideClothing = false;
        reTesselate();
    }


    private bool OnConfirm()
    {
        TryClose();
        return true;
    }

    protected virtual void OnTitleBarClose()
    {
        TryClose();
    }

    protected void SendInvPacket(object packet)
    {
        capi.Network.SendPacketClient(packet);
    }

    protected void reTesselate()
    {
        var essr = capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer;
        essr.TesselateShape();
    }

    public void PrepAndOpen()
    {
        TryOpen();
    }

    public override bool CaptureAllInputs()
    {
        return IsOpened();
    }


    #region Character render

    protected float yaw = -GameMath.PIHALF + 0.3f;
    protected bool rotateCharacter;

    public override void OnMouseDown(MouseEvent args)
    {
        base.OnMouseDown(args);

        // rotateCharacter = insetSlotBounds.PointInside(args.X, args.Y);
    }

    public override void OnMouseUp(MouseEvent args)
    {
        base.OnMouseUp(args);

        rotateCharacter = false;
    }

    public override void OnMouseMove(MouseEvent args)
    {
        base.OnMouseMove(args);

        if (rotateCharacter)
        {
            yaw -= args.DeltaX / 100f;
        }
    }


    private readonly Vec4f lighPos = new Vec4f(-1, -1, 0, 0).NormalizeXYZ();
    private readonly Matrixf mat = new();

    public override void OnRenderGUI(float deltaTime)
    {
        base.OnRenderGUI(deltaTime);

        if (capi.IsGamePaused)
        {
            capi.World.Player.Entity.talkUtil.OnGameTick(deltaTime);
        }

        capi.Render.GlPushMatrix();

        if (focused)
        {
            capi.Render.GlTranslate(0, 0, 150);
        }

        capi.Render.GlRotate(-14, 1, 0, 0);

        mat.Identity();
        mat.RotateXDeg(-14);
        var lightRot = mat.TransformVector(lighPos);
        var pad = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);

        capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(lightRot.X, lightRot.Y, lightRot.Z));
        capi.Render.PushScissor(insetSlotBounds);

        var posX = insetSlotBounds.renderX + pad - GuiElement.scaled(195) * charZoom +
                   GuiElement.scaled(115 * (1 - charZoom));
        var posY = insetSlotBounds.renderY + pad + GuiElement.scaled(10 * (1 - charZoom));
        double posZ = (float)GuiElement.scaled(230);
        var size = (float)GuiElement.scaled(330 * charZoom);

        capi.Render.RenderEntityToGui(deltaTime, capi.World.Player.Entity, posX, posY, posZ, yaw, size,
            ColorUtil.WhiteArgb);
        capi.Render.PopScissor();
        capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(1, -1, 0).Normalize());
        capi.Render.GlPopMatrix();
    }

    #endregion
}