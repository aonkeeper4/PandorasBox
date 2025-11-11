using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.PandorasBox;

[CustomEntity("pandorasBox/dreamDashController")]
[Tracked]
internal class DreamDashController : Entity
{
    internal class DreamDashControllerComponent(DreamDashController controller, bool setupByController) : Component(false, false)
    {
        public readonly DreamDashController Controller = controller;
        public readonly bool SetupByController = setupByController;

        public override void Added(Entity entity)
        {
            if (entity is not DreamBlock)
                throw new InvalidOperationException($"{nameof(DreamDashControllerComponent)} added to non-{nameof(DreamBlock)} entity!");
                
            base.Added(entity);
        }
    }

    private class WallDataComponent(float? rotation = null, Vector2? renderOffset = null, Vector2? speed = null) : Component(false, false)
    {
        public float? Rotation = rotation;
        public Vector2? RenderOffset = renderOffset;
        public Vector2? Speed = speed;
        
        public override void Added(Entity entity)
        {
            if (entity is not Player)
                throw new InvalidOperationException($"{nameof(WallDataComponent)} added to non-{nameof(Player)} entity!");
                
            base.Added(entity);
        }
    }

    public readonly bool AllowSameDirectionDash;
    public readonly bool AllowDreamDashRedirection;
    public readonly bool OverrideDreamDashSpeed;
    public readonly bool OverrideColors;
    public readonly bool NeverSlowDown;
    public readonly bool UseEntrySpeedAngle;
    public readonly bool BounceOnCollision;
    public readonly bool CollideStickToWalls;

    public readonly float SameDirectionSpeedMultiplier;
    public readonly float DreamDashSpeed;

    public readonly Color ActiveBackColor;
    public readonly Color DisabledBackColor;
    public readonly Color ActiveLineColor;
    public readonly Color DisabledLineColor;
    public readonly List<List<Color>> ParticleLayerColors;

    // ModInterop stuff
    private static readonly List<Type> SetupIgnoringTypes = [];
    internal static void AddSetupIgnoringTypes(List<Type> types) => SetupIgnoringTypes.AddRange(types);
    internal static void RemoveSetupIgnoringTypes(List<Type> types) => types.ForEach(t => SetupIgnoringTypes.Remove(t));
    
    private static readonly List<Type> ControlledTypes = [];
    internal static void AddControlledTypes(List<Type> types) => ControlledTypes.AddRange(types);
    internal static void RemoveControlledTypes(List<Type> types) => types.ForEach(t => ControlledTypes.Remove(t));
    
    private readonly bool roomWide;
    private readonly List<DreamBlock> blocksToSetup = [];

    public DreamDashController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        AllowSameDirectionDash = data.Bool("allowSameDirectionDash", false);
        AllowDreamDashRedirection = data.Bool("allowDreamDashRedirect", true);
        OverrideDreamDashSpeed = data.Bool("overrideDreamDashSpeed", false);
        OverrideColors = data.Bool("overrideColors", false);
        NeverSlowDown = data.Bool("neverSlowDown", false);
        UseEntrySpeedAngle = data.Bool("useEntrySpeedAngle", false);
        BounceOnCollision = data.Bool("bounceOnCollision", false);
        CollideStickToWalls = data.Bool("stickOnCollision", false);

        SameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1.0f);
        DreamDashSpeed = data.Float("dreamDashSpeed", 240f);

        ActiveBackColor = ColorHelper.GetColor(data.Attr("activeBackColor", "Black"));
        DisabledBackColor = ColorHelper.GetColor(data.Attr("disabledBackColor", "1f2e2d"));
        ActiveLineColor = ColorHelper.GetColor(data.Attr("activeLineColor", "White"));
        DisabledLineColor = ColorHelper.GetColor(data.Attr("disabledLineColor", "6a8480"));

        ParticleLayerColors = [
            ColorHelper.GetColors(data.Attr("particleLayer0Colors", "ffef11,ff00d0,08a310")),
            ColorHelper.GetColors(data.Attr("particleLayer1Colors", "5fcde4,7fb25e,e0564c")),
            ColorHelper.GetColors(data.Attr("particleLayer2Colors", "5b6ee1,CC3B3B,7daa64"))
        ];

        Vector2[] nodes = data.NodesOffset(offset);
        if (nodes.Length == 2)
        {
            float topLeftX = MathF.Min(nodes[0].X, nodes[1].X);
            float topLeftY = MathF.Min(nodes[0].Y, nodes[1].Y);
            float width = MathF.Max(nodes[0].X, nodes[1].X) - topLeftX;
            float height = MathF.Max(nodes[0].Y, nodes[1].Y) - topLeftY;

            Collider = new Hitbox(width, height, topLeftX - X, topLeftY - Y);
            roomWide = false;
        }
        else
        {
            Collider = null;
            roomWide = true;
        }
    }
    
    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        
        foreach (DreamBlock block in scene.Tracker.GetEntities<DreamBlock>()
                                                  .Cast<DreamBlock>()
                                                  .Where(IsAffected))
        {
            bool shouldSetup = !SetupIgnoringTypes.Contains(block.GetType());
                
            block.Add(new DreamDashControllerComponent(this, shouldSetup));
            if (shouldSetup)
                blocksToSetup.Add(block);
        }

        foreach (Entity entity in scene.Entities.Where(e => ControlledTypes.Contains(e.GetType()))
                                                .Where(IsAffected))
            entity.Add(new DreamDashControllerComponent(this, false));
            
        if (OverrideColors)
            AddParticleColors();
    }

    private bool IsAffected(Entity e)
        => roomWide || CollideCheck(e) || CollidePoint(e.Position);
    
    private void AddParticleColors()
    {
        foreach (DreamBlock dreamBlock in blocksToSetup.Where(block => block.SceneAs<Level>().IsInBounds(block)))
            ChangeDreamBlockParticleColors(dreamBlock);
    }

    private void ChangeDreamBlockParticleColors(DreamBlock dreamBlock)
    {
        if (dreamBlock.particles is null)
            return;
        
        for (int i = 0; i < dreamBlock.particles.Length; i++)
        {
            int layer = dreamBlock.particles[i].Layer;
            dreamBlock.particles[i].Color = Calc.Random.Choose(ParticleLayerColors[layer]);
        }
    }

    private static void DreamDashRedirect(DreamBlock block, Player player)
    {
        if (block?.Get<DreamDashControllerComponent>()?.Controller is not { } dreamDashController)
            return;

        if (player.StateMachine.State != Player.StDreamDash)
            return;
        
        player.dashCooldownTimer = 0f;
        if (!player.CanDash)
            return;
        
        bool sameDirection = Input.GetAimVector() == player.DashDir;
        bool canRedirect = dreamDashController.AllowDreamDashRedirection && !sameDirection
            || dreamDashController.AllowSameDirectionDash && sameDirection;
        if (!canRedirect)
            return;
        
        Audio.Play("event:/char/madeline/dreamblock_enter");
        
        player.Dashes = Math.Max(0, player.Dashes - 1);

        // Freeze game when redirecting dash
        // Consistent with dashing in the base game
#pragma warning disable CS0618 // Type or member is obsolete
        if (Engine.TimeRate > 0.25f)
#pragma warning restore CS0618 // Type or member is obsolete
            Celeste.Freeze(0.05f);

        if (sameDirection)
        {
            player.Speed *= dreamDashController.SameDirectionSpeedMultiplier;
            player.DashDir *= Math.Sign(dreamDashController.SameDirectionSpeedMultiplier);
        }
        else
        {
            player.DashDir = Input.GetAimVector();
            player.Speed = player.DashDir * player.Speed.Length();
        }

        Input.Dash.ConsumeBuffer();
    }

    // Do a bounce check and bounce if possible
    private static bool AttemptBounce(DreamBlock block, Player player)
    {
        if (block?.Get<DreamDashControllerComponent>()?.Controller is not { } dreamDashController)
            return false;

        if (!dreamDashController.BounceOnCollision && !dreamDashController.CollideStickToWalls)
            return false;
        
        Vector2 moveCheckVector = player.Speed * Engine.DeltaTime;
        player.NaiveMove(moveCheckVector);

        DreamBlock dreamBlock = player.CollideFirst<DreamBlock>();
        if (dreamBlock is null)
        {
            bool inSolid = player.DreamDashedIntoSolid();
            if (inSolid)
            {
                // Move the player out of the wall properly, then bounce
                player.NaiveMove(-moveCheckVector);

                if (dreamDashController.BounceOnCollision)
                    BouncePlayer(player);
                else
                    StickPlayer(player);

                return true;
            }
        }

        // Make sure we undo the check movement
        player.NaiveMove(-moveCheckVector);

        return false;
    }

    private static void BouncePlayer(Player player)
    {
        Vector2 horizontalMoveCheckVector = new(player.Speed.X * Engine.DeltaTime, 0f);
        Vector2 verticalMoveCheckVector = new(0f, player.Speed.Y * Engine.DeltaTime);

        bool horizontal = OutsideAfterMove(player, horizontalMoveCheckVector);
        bool vertical = OutsideAfterMove(player, verticalMoveCheckVector);

        if (horizontal)
            player.Speed.X *= -1;
        if (vertical)
            player.Speed.Y *= -1;
    }

    private static void StickPlayer(Player player)
    {
        DreamBlock dreamBlock = player.CollideFirst<DreamBlock>();
        if (dreamBlock is null)
            return;

        if (player.Get<WallDataComponent>() is not { } wallData)
            player.Add(wallData = new WallDataComponent(speed: player.Speed));

        Collider playerCollider = player.Collider;
        Collider dreamBlockCollider = dreamBlock.Collider;

        Vector2 horizontalMoveCheckVector = new(player.Speed.X * Engine.DeltaTime, 0f);
        Vector2 verticalMoveCheckVector = new(0f, player.Speed.Y * Engine.DeltaTime);

        bool horizontal = OutsideAfterMove(player, horizontalMoveCheckVector);
        bool vertical = OutsideAfterMove(player, verticalMoveCheckVector);

        player.StateMachine.State = Player.StNormal;
        player.Ducking = true;
       
        float moveOffsetX = 0f;
        float moveOffsetY = 0f;
        float renderOffsetX = 0f;
        float renderOffsetY = 0f;
        float rotation = 0f;

        if (horizontal)
        {
            if (player.Speed.X < 0)
            {
                rotation = MathF.PI / 2;
                moveOffsetX = dreamBlockCollider.AbsoluteLeft - player.X + playerCollider.Width / 2.0f;
                renderOffsetX = -playerCollider.Width / 2.0f;
                renderOffsetY = -playerCollider.Height / 2.0f;
            }
            else
            {
                rotation = MathF.PI * 3 / 2;
                moveOffsetX = dreamBlockCollider.AbsoluteRight - player.X - playerCollider.Width / 2.0f;
                renderOffsetX = playerCollider.Width / 2.0f;
                renderOffsetY = -playerCollider.Height / 2.0f;
            }
        }

        if (vertical)
        {
            if (player.Speed.Y < 0)
            {
                rotation = MathF.PI;
                moveOffsetY = dreamBlockCollider.AbsoluteTop - player.Y + playerCollider.Height - 1;
                renderOffsetY = -playerCollider.Height;
            }
            else
            {
                rotation = 0f;
                moveOffsetY = dreamBlockCollider.AbsoluteBottom - player.Y + 1;
            }
        }

        wallData.Rotation = rotation;
        wallData.RenderOffset = new Vector2(renderOffsetX, renderOffsetY);

        player.NaiveMove(new Vector2(moveOffsetX, moveOffsetY));
    }
    
    private static bool OutsideAfterMove(Player player, Vector2 offset)
    {
        player.NaiveMove(offset);
        bool outside = player.CollideFirst<DreamBlock>() is null;
        player.NaiveMove(-offset);

        return outside;
    }

    private static void DreamDashStartBefore(DreamBlock block, Player player)
    {
        if (block?.Get<DreamDashControllerComponent>()?.Controller is not { } dreamDashController)
            return;

        WallDataComponent wallData = player.Get<WallDataComponent>();
        Vector2 stickSpeed = wallData?.Speed ?? player.Speed;
        player.Remove(wallData);

        if (dreamDashController.UseEntrySpeedAngle)
        {
            Vector2 entryVector = stickSpeed.SafeNormalize();
            float magnitude = stickSpeed.Length();

            player.Speed = entryVector * magnitude;
        }
    }

    private static void DreamDashStartAfter(DreamBlock block, Player player, Vector2 preEnterSpeed)
    {
        if (block?.Get<DreamDashControllerComponent>()?.Controller is not { } dreamDashController)
            return;
            
        Vector2 dashDirection = dreamDashController.UseEntrySpeedAngle ? preEnterSpeed.SafeNormalize() : player.DashDir;

        if (dreamDashController.OverrideDreamDashSpeed)
            player.Speed = dashDirection * dreamDashController.DreamDashSpeed;

        if (dreamDashController.NeverSlowDown && player.Speed.LengthSquared() < preEnterSpeed.LengthSquared())
            player.Speed = dashDirection * preEnterSpeed.Length();
    }
    
    #region Hooks

    private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player self)
        => AttemptBounce(self.dreamBlock, self) ? self.StateMachine.State : orig(self);

    private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player self)
    {
        DreamDashStartBefore(self.dreamBlock, self);
        Vector2 beforeSpeed = self.Speed;

        orig(self);

        DreamDashStartAfter(self.dreamBlock, self, beforeSpeed);
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        if (self.dreamBlock is not null && Input.Dash.Pressed && Input.Aim.Value != Vector2.Zero)
            DreamDashRedirect(self.dreamBlock, self);

        Facings preOrigFacing = self.Facing;
        Vector2 preOrigScale = self.Sprite.Scale;

        orig(self);

        if (self.Get<WallDataComponent>()?.Rotation is not { } rotation)
            return;
        
        self.Facing = preOrigFacing;
        self.Sprite.Scale = preOrigScale;

        Vector2 inputAim = Input.Aim.Value;
        if (inputAim == Vector2.Zero)
            return;
        
        float inputAngleOffset = (inputAim.Angle() - rotation + MathHelper.TwoPi) % MathHelper.TwoPi;
        Facings newFacing = self.Facing;

        if (inputAngleOffset >= Math.PI * 0.75 && inputAngleOffset <= Math.PI * 1.25)
            newFacing = Facings.Left;
        else if (inputAngleOffset >= Math.PI * -0.25 && inputAngleOffset <= Math.PI * 0.25
            || inputAngleOffset - MathHelper.TwoPi >= Math.PI * -0.25 && inputAngleOffset - MathHelper.TwoPi <= Math.PI * 0.25)
            newFacing = Facings.Right;

        self.Facing = newFacing;
    }

    private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
    {
        Level level = self.SceneAs<Level>();
        
        WallDataComponent wallData = self.Get<WallDataComponent>();
        float playerRotation = wallData?.Rotation ?? 0f;
        Vector2 renderOffset = wallData?.RenderOffset ?? Vector2.Zero;

        if (level is null || playerRotation == 0f)
        {
            orig(self);
            return;
        }

        Camera camera = level.Camera;

        float originalAngle = camera.Angle;
        Vector2 originalCameraPosition = camera.Position;
        Vector2 originalCameraOrigin = camera.Origin;
        Vector2 originalPlayerPosition = self.Sprite.Position;

        GameplayRenderer.End();
        camera.Angle = playerRotation;
        camera.Origin = self.Position + renderOffset - camera.Position;
        camera.Position += camera.Origin;
        self.Sprite.Position += renderOffset;
        self.Hair.MoveHairBy(renderOffset);
        GameplayRenderer.Begin();

        orig(self);

        GameplayRenderer.End();
        camera.Angle = originalAngle;
        camera.Origin = originalCameraOrigin;
        camera.Position = originalCameraPosition;
        self.Sprite.Position = originalPlayerPosition;
        self.Hair.MoveHairBy(-renderOffset);
        GameplayRenderer.Begin();
    }

    private static void ModifyDreamBlockColors(ILContext il)
    {
        ILCursor cursor = new(il);

        // back colors
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(DreamBlock), "activeBackColor")))
        {
            cursor.EmitLdarg0();
            cursor.EmitLdcI4(0);
            cursor.EmitDelegate(GetColorFromController);
        }
        cursor.Index = 0;
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(DreamBlock), "disabledBackColor")))
        {
            cursor.EmitLdarg0();
            cursor.EmitLdcI4(1);
            cursor.EmitDelegate(GetColorFromController);
        }
        cursor.Index = 0;
        
        // line colors
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(DreamBlock), "activeLineColor")))
        {
            cursor.EmitLdarg0();
            cursor.EmitLdcI4(2);
            cursor.EmitDelegate(GetColorFromController);
        }
        cursor.Index = 0;
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(DreamBlock), "disabledLineColor")))
        {
            cursor.EmitLdarg0();
            cursor.EmitLdcI4(3);
            cursor.EmitDelegate(GetColorFromController);
        }
        cursor.Index = 0;

        return;

        static Color GetColorFromController(Color orig, DreamBlock block, int colorIndex)
        {
            if (block?.Get<DreamDashControllerComponent>() is not { Controller: { } controller } component)
                return orig;

            Color colorFromController = colorIndex switch
            {
                0 => controller.ActiveBackColor,
                1 => controller.DisabledBackColor,
                2 => controller.ActiveLineColor,
                3 => controller.DisabledLineColor,
                _ => throw new ArgumentOutOfRangeException()
            };
            return component.SetupByController && controller.OverrideColors ? colorFromController : orig;
        }
    }

    public static void Load()
    {
        On.Celeste.Player.Render += Player_Render;
        On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
        On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
        On.Celeste.Player.Update += Player_Update;

        IL.Celeste.DreamBlock.Render += ModifyDreamBlockColors;
        IL.Celeste.DreamBlock.WobbleLine += ModifyDreamBlockColors;
    }

    public static void Unload()
    {
        On.Celeste.Player.Render -= Player_Render;
        On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
        On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
        On.Celeste.Player.Update -= Player_Update;
            
        IL.Celeste.DreamBlock.Render -= ModifyDreamBlockColors;
        IL.Celeste.DreamBlock.WobbleLine -= ModifyDreamBlockColors;
    }
    
    #endregion
}