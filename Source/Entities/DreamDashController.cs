using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.PandorasBox;

[CustomEntity("pandorasBox/dreamDashController")]
[Tracked]
internal class DreamDashController : Entity
{
    private class DreamDashControllerComponent(DreamDashController controller, bool setupByController) : Component(false, false)
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
        
    private readonly bool allowSameDirectionDash;
    private readonly bool allowDreamDashRedirection;
    private readonly bool overrideDreamDashSpeed;
    private readonly bool overrideColors;
    private readonly bool neverSlowDown;
    private readonly bool useEntrySpeedAngle;
    private readonly bool bounceOnCollision;
    private readonly bool collideStickToWalls;

    private readonly float sameDirectionSpeedMultiplier;
    private readonly float dreamDashSpeed;

    private readonly Color activeBackColor;
    private readonly Color disabledBackColor;
    private readonly Color activeLineColor;
    private readonly Color disabledLineColor;
    private readonly List<List<Color>> particleLayerColors;

    // ModInterop stuff
    private static readonly List<Type> SetupIgnoringTypes = [];
    internal static void AddSetupIgnoringTypes(List<Type> types) => SetupIgnoringTypes.AddRange(types);
    internal static void RemoveSetupIgnoringTypes(List<Type> types) => types.ForEach(t => SetupIgnoringTypes.Remove(t));
    internal (bool, Color, Color, Color, Color, List<List<Color>>) GetVisualSettings()
        => (overrideColors,
            activeBackColor,
            disabledBackColor,
            activeLineColor,
            disabledLineColor,
            particleLayerColors);
        
    private static readonly ConditionalWeakTable<Player, ValueHolder<float>> WallPlayerRotations = new();
    private static readonly ConditionalWeakTable<Player, ValueHolder<Vector2>> WallPlayerRenderOffset = new();
    private static readonly ConditionalWeakTable<Player, ValueHolder<Vector2>> WallPlayerSpeed = new();

    private readonly List<DreamBlock> blocksToSetup = [];

    public DreamDashController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        allowSameDirectionDash = data.Bool("allowSameDirectionDash", false);
        allowDreamDashRedirection = data.Bool("allowDreamDashRedirect", true);
        overrideDreamDashSpeed = data.Bool("overrideDreamDashSpeed", false);
        overrideColors = data.Bool("overrideColors", false);
        neverSlowDown = data.Bool("neverSlowDown", false);
        useEntrySpeedAngle = data.Bool("useEntrySpeedAngle", false);
        bounceOnCollision = data.Bool("bounceOnCollision", false);
        collideStickToWalls = data.Bool("stickOnCollision", false);

        sameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1.0f);
        dreamDashSpeed = data.Float("dreamDashSpeed", 240f);

        activeBackColor = ColorHelper.GetColor(data.Attr("activeBackColor", "Black"));
        disabledBackColor = ColorHelper.GetColor(data.Attr("disabledBackColor", "1f2e2d"));
        activeLineColor = ColorHelper.GetColor(data.Attr("activeLineColor", "White"));
        disabledLineColor = ColorHelper.GetColor(data.Attr("disabledLineColor", "6a8480"));

        particleLayerColors = [
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
        }
        else
            Collider = null;
    }

    private static void DreamDashRedirect(DreamBlock block, Player player)
    {
        if (block?.Get<DreamDashControllerComponent>()?.Controller is not { } dreamDashController)
            return;
            
        if (player.StateMachine.State == Player.StDreamDash)
        {
            player.dashCooldownTimer = 0f;

            if (player.CanDash)
            {
                bool sameDirection = Input.GetAimVector() == player.DashDir;

                if (dreamDashController.allowDreamDashRedirection && !sameDirection || dreamDashController.allowSameDirectionDash && sameDirection)
                {
                    player.Dashes = Math.Max(0, player.Dashes - 1);

                    Audio.Play("event:/char/madeline/dreamblock_enter");

                    // Freeze game when redirecting dash
                    // Consistent with dashing in the base game
#pragma warning disable CS0618 // Type or member is obsolete
                    if (Engine.TimeRate > 0.25f)
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        Celeste.Freeze(0.05f);
                    }

                    if (sameDirection)
                    {
                        player.Speed *= dreamDashController.sameDirectionSpeedMultiplier;
                        player.DashDir *= Math.Sign(dreamDashController.sameDirectionSpeedMultiplier);
                    }
                    else
                    {
                        player.DashDir = Input.GetAimVector();
                        player.Speed = player.DashDir * player.Speed.Length();
                    }

                    Input.Dash.ConsumeBuffer();
                }
            }
        }
    }

    // Do a bounce check and bounce if possible
    public static bool AttemptBounce(DreamBlock block, Player player)
    {
        if (block?.Get<DreamDashControllerComponent>()?.Controller is not { } dreamDashController)
            return false;
            
        if (dreamDashController.bounceOnCollision || dreamDashController.collideStickToWalls)
        {
            Vector2 moveCheckVector = player.Speed * Engine.DeltaTime;

            player.NaiveMove(moveCheckVector);

            DreamBlock dreamBlock = player.CollideFirst<DreamBlock>();

            if (dreamBlock == null)
            {
                bool inSolid = player.DreamDashedIntoSolid();
                if (inSolid)
                {
                    // Move the player out of the wall properly, then bounce
                    player.NaiveMove(-moveCheckVector);

                    if (dreamDashController.bounceOnCollision)
                    {
                        BouncePlayer(player);
                    }
                    else
                    {
                        StickPlayer(player);
                    }

                    return true;
                }
            }

            // Make sure we undo the check movement
            player.NaiveMove(-moveCheckVector);
        }

        return false;
    }

    private static bool OutsideAfterMove(Player player, Vector2 offset)
    {
        player.NaiveMove(offset);
        bool outside = player.CollideFirst<DreamBlock>() == null;
        player.NaiveMove(-offset);

        return outside;
    }

    private static void BouncePlayer(Player player)
    {
        Vector2 horizontalMoveCheckVector = new Vector2(player.Speed.X * Engine.DeltaTime, 0f);
        Vector2 verticalMoveCheckVector = new Vector2(0f, player.Speed.Y * Engine.DeltaTime);

        bool horizontal = OutsideAfterMove(player, horizontalMoveCheckVector);
        bool vertical = OutsideAfterMove(player, verticalMoveCheckVector);

        if (horizontal)
        {
            player.Speed.X *= -1;
        }
            
        if (vertical)
        {
            player.Speed.Y *= -1;
        }
    }

    private static void StickPlayer(Player player)
    {
        DreamBlock dreamBlock = player.CollideFirst<DreamBlock>();

        if (dreamBlock == null)
        {
            return;
        }

        WallPlayerSpeed.AddOrUpdate(player, new ValueHolder<Vector2>(player.Speed));

        Collider playerCollider = player.Collider;
        Collider dreamBlockCollider = dreamBlock.Collider;

        Vector2 horizontalMoveCheckVector = new Vector2(player.Speed.X * Engine.DeltaTime, 0f);
        Vector2 verticalMoveCheckVector = new Vector2(0f, player.Speed.Y * Engine.DeltaTime);

        bool horizontal = OutsideAfterMove(player, horizontalMoveCheckVector);
        bool vertical = OutsideAfterMove(player, verticalMoveCheckVector);

        player.StateMachine.State = Player.StNormal;
        player.Ducking = true;
       
        float moveOffsetX = 0f;
        float moveOffsetY = 0f;
        float renderOffsetX = 0f;
        float renderOffsetY = 0f;
        double rotation = 0.0;

        if (horizontal)
        {
            if (player.Speed.X < 0)
            {
                rotation = Math.PI / 2;
                moveOffsetX = dreamBlockCollider.AbsoluteLeft - player.X + playerCollider.Width / 2.0f;
                renderOffsetX = -playerCollider.Width / 2.0f;
                renderOffsetY = -playerCollider.Height / 2.0f;
            }
            else
            {
                rotation = Math.PI * 3 / 2;
                moveOffsetX = dreamBlockCollider.AbsoluteRight - player.X - playerCollider.Width / 2.0f;
                renderOffsetX = playerCollider.Width / 2.0f;
                renderOffsetY = -playerCollider.Height / 2.0f;
            }
        }

        if (vertical)
        {
            if (player.Speed.Y < 0)
            {
                rotation = Math.PI;
                moveOffsetY = dreamBlockCollider.AbsoluteTop - player.Y + playerCollider.Height - 1;
                renderOffsetY = -playerCollider.Height;
            }
            else
            {
                rotation = 0.0;
                moveOffsetY = dreamBlockCollider.AbsoluteBottom - player.Y + 1;
            }
        }

        WallPlayerRotations.AddOrUpdate(player, new ValueHolder<float>((float)rotation));
        WallPlayerRenderOffset.AddOrUpdate(player, new ValueHolder<Vector2>(new Vector2(renderOffsetX, renderOffsetY)));

        player.NaiveMove(new Vector2(moveOffsetX, moveOffsetY));
    }

    private static void DreamDashStartBefore(DreamBlock block, Player player)
    {
        if (block?.Get<DreamDashControllerComponent>()?.Controller is not { } dreamDashController)
            return;
            
        Vector2 stickSpeed = WallPlayerSpeed.GetOrDefault(player, new ValueHolder<Vector2>(player.Speed)).value;

        WallPlayerRenderOffset.Remove(player);
        WallPlayerRotations.Remove(player);
        WallPlayerSpeed.Remove(player);

        if (dreamDashController.useEntrySpeedAngle)
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
            
        Vector2 dashDirection = dreamDashController.useEntrySpeedAngle ? preEnterSpeed.SafeNormalize() : player.DashDir;

        if (dreamDashController.overrideDreamDashSpeed)
        {
            player.Speed = dashDirection * dreamDashController.dreamDashSpeed;
        }

        if (dreamDashController.neverSlowDown)
        {
            if (player.Speed.LengthSquared() < preEnterSpeed.LengthSquared())
            {
                player.Speed = dashDirection * preEnterSpeed.Length();
            }
        }
    }

    private void AddParticleColors(Scene scene)
    {
        Level level = (scene as Level)!;

        foreach (DreamBlock dreamBlock in blocksToSetup.Where(block => level.IsInBounds(block)))
        {
            ChangeDreamBlockParticleColors(dreamBlock);
        }
    }

    private void ChangeDreamBlockParticleColors(DreamBlock dreamBlock)
    {
        if (dreamBlock.particles != null)
        {
            for (int i = 0; i < dreamBlock.particles.Length; i++)
            {
                int layer = dreamBlock.particles[i].Layer;
                dreamBlock.particles[i].Color = Calc.Random.Choose(particleLayerColors[layer]);
            }
        }
    }

    public override void Awake(Scene scene)
    {
        foreach (DreamBlock block in scene.Tracker.GetEntities<DreamBlock>()
                                          .Cast<DreamBlock>()
                                          .Where(b => Collider is null || CollideCheck(b)))
        {
            bool shouldSetup = !SetupIgnoringTypes.Contains(block.GetType());
                
            block.Add(new DreamDashControllerComponent(this, shouldSetup));
            if (shouldSetup)
                blocksToSetup.Add(block);
        }
            
        if (overrideColors)
        {
            AddParticleColors(scene);
        }

        base.Awake(scene);
    }

    private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player self)
    {
        bool bounced = AttemptBounce(self.dreamBlock, self);

        if (bounced)
        {
            return self.StateMachine.State;
        }
        else
        {
            return orig(self);
        }
    }

    private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player self)
    {
        DreamBlock currentBlock = self.CollideFirst<DreamBlock>(self.Position + self.Speed.Sign());
            
        DreamDashStartBefore(currentBlock, self);
        Vector2 beforeSpeed = self.Speed;

        orig(self);

        DreamDashStartAfter(currentBlock, self, beforeSpeed);
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        if (self.dreamBlock != null)
        {
            if (Input.Dash.Pressed && Input.Aim.Value != Vector2.Zero)
            {
                DreamDashRedirect(self.dreamBlock, self);
            }
        }

        Facings preOrigFacing = self.Facing;
        Vector2 preOrigScale = self.Sprite.Scale;

        orig(self);

        if (WallPlayerRotations.TryGetValue(self, out var rotationHolder))
        {
            self.Facing = preOrigFacing;
            self.Sprite.Scale = preOrigScale;

            Vector2 inputAim = Input.Aim.Value;

            if (inputAim != Vector2.Zero)
            {
                float inputAngleOffset = (inputAim.Angle() - rotationHolder!.value + MathHelper.TwoPi) % MathHelper.TwoPi;
                Facings newFacing = self.Facing;

                if (inputAngleOffset >= Math.PI * 0.75 && inputAngleOffset <= Math.PI * 1.25)
                {
                    newFacing = Facings.Left;
                }
                else if (inputAngleOffset >= Math.PI * -0.25 && inputAngleOffset <= Math.PI * 0.25 || inputAngleOffset - MathHelper.TwoPi >= Math.PI * -0.25 && inputAngleOffset - MathHelper.TwoPi <= Math.PI * 0.25)
                {
                    newFacing = Facings.Right;
                }

                self.Facing = newFacing;
            }
        }
    }

    private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
    {
        Level level = self.Scene as Level;

        float playerRotation = WallPlayerRotations.GetOrDefault(self, new ValueHolder<float>(0f)).value;
        Vector2 renderOffset = WallPlayerRenderOffset.GetOrDefault(self, new ValueHolder<Vector2>(new Vector2(0f, 0f))).value;

        if (level != null && playerRotation != 0f) {
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
        else
        {
            orig(self);
        }
    }

    private static void ModifyDreamBlockColors(ILContext il)
    {
        ILCursor cursor = new(il);

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(DreamBlock), "activeBackColor")))
        {
            cursor.EmitLdarg0();
            cursor.EmitDelegate(GetActiveBackColor);
        }
        cursor.Index = 0;
            
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(DreamBlock), "disabledBackColor")))
        {
            cursor.EmitLdarg0();
            cursor.EmitDelegate(GetDisabledBackColor);
        }
        cursor.Index = 0;
            
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(DreamBlock), "activeLineColor")))
        {
            cursor.EmitLdarg0();
            cursor.EmitDelegate(GetActiveLineColor);
        }
        cursor.Index = 0;
            
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(DreamBlock), "disabledLineColor")))
        {
            cursor.EmitLdarg0();
            cursor.EmitDelegate(GetDisabledLineColor);
        }

        return;

        static Color GetActiveBackColor(Color orig, DreamBlock block)
        {
            if (block?.Get<DreamDashControllerComponent>() is not { } component)
                return orig;

            return component.SetupByController && component.Controller.overrideColors ? component.Controller.activeBackColor : orig;
        }
            
        static Color GetDisabledBackColor(Color orig, DreamBlock block)
        {
            if (block?.Get<DreamDashControllerComponent>() is not { } component)
                return orig;

            return component.SetupByController && component.Controller.overrideColors ? component.Controller.disabledBackColor : orig;
        }
            
        static Color GetActiveLineColor(Color orig, DreamBlock block)
        {
            if (block?.Get<DreamDashControllerComponent>() is not { } component)
                return orig;

            return component.SetupByController && component.Controller.overrideColors ? component.Controller.activeLineColor : orig;
        }
            
        static Color GetDisabledLineColor(Color orig, DreamBlock block)
        {
            if (block?.Get<DreamDashControllerComponent>() is not { } component)
                return orig;

            return component.SetupByController && component.Controller.overrideColors ? component.Controller.disabledLineColor : orig;
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
}