using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.PandorasBox
{
    [Tracked]
    [CustomEntity("pandorasBox/entityActivator")]
    class EntityActivator : Trigger
    {
        public enum EffectModes
        {
            ActivateInsideDeactivateOutside,
            ActivateInside,
            ActivateOutside,
            DeactivateInside,
            DeactivateOutside,
            ActivateOnScreenDeactivateOffScreen
        }

        public enum ActivationModes
        {
            OnEnter,
            OnStay,
            OnLeave,
            OnFlagActive,
            OnFlagInactive,
            OnFlagActivated,
            OnFlagDeactivated,
            OnUpdate,
            OnCameraMoved,
            OnAwake
        }

        public EffectModes Mode;
        public ActivationModes ActivationMode;

        public HashSet<Type> Targets;

        public bool UseTracked;

        public string Flag;

        public float UpdateInterval;

        public bool ChangeCollidable;
        public bool ChangeActive;
        public bool ChangeVisible;

        public bool CacheTargets;

        public bool AffectComponents;

        private bool previousFlagValue = false;
        private bool updateFlagValues = false;

        private List<Entity> cachedTargets;

        private Vector2 previousCameraPosition;
        private float previousCameraZoom;

        public EntityActivator(EntityData data, Vector2 offset) : base(data, offset)
        {
            Tag = Tags.TransitionUpdate;

            Mode = data.Enum<EffectModes>("mode", EffectModes.ActivateInsideDeactivateOutside);
            ActivationMode = data.Enum<ActivationModes>("activationMode", ActivationModes.OnEnter);

            Targets = TypeHelper.GetTypesFromString(data.Attr("targets", ""));

            UseTracked = data.Bool("useTracked", true);

            Flag = data.Attr("flag", "");

            ChangeCollidable = data.Bool("changeCollision", true);
            ChangeActive = data.Bool("changeActive", true);
            ChangeVisible = data.Bool("changeVisible", true);

            CacheTargets = data.Bool("cacheTargets", false);

            AffectComponents = data.Bool("affectComponents", false);

            UpdateInterval = data.Float("updateInterval", -1f);
        }

        public override void OnEnter(Player player)
        {
            if (ActivationMode == ActivationModes.OnEnter)
            {
                UpdateEntities();
            }
        }

        public override void OnStay(Player player)
        {
            if (ActivationMode == ActivationModes.OnStay)
            {
                UpdateEntities();
            }
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);

            if (ActivationMode == ActivationModes.OnLeave)
            {
                UpdateEntities();
            }
        }

        public override void Update()
        {
            if (ActivationMode == ActivationModes.OnUpdate && OnInterval())
            {
                UpdateEntities();
            }
            else if (ActivationMode == ActivationModes.OnCameraMoved)
            {
                Camera camera = SceneAs<Level>().Camera;

                if (Math.Abs(camera.X - previousCameraPosition.X) > 8 || Math.Abs(camera.Y - previousCameraPosition.Y) > 8 || camera.Zoom != previousCameraZoom)
                {
                    UpdateEntities();

                    previousCameraPosition = camera.Position;
                    previousCameraZoom = camera.Zoom;
                }
            }
            else if (updateFlagValues)
            {
                bool currentFlagValue = SceneAs<Level>().Session.GetFlag(Flag);

                if (ActivationMode == ActivationModes.OnFlagActive && currentFlagValue && OnInterval())
                {
                    UpdateEntities();
                }
                else if (ActivationMode == ActivationModes.OnFlagInactive && !currentFlagValue && OnInterval())
                {
                    UpdateEntities();
                }
                else if (ActivationMode == ActivationModes.OnFlagActivated && currentFlagValue && !previousFlagValue)
                {
                    UpdateEntities();
                }
                else if (ActivationMode == ActivationModes.OnFlagDeactivated && !currentFlagValue && previousFlagValue)
                {
                    UpdateEntities();
                }

                previousFlagValue = currentFlagValue;
            }

            base.Update();
        }

        public override void Awake(Scene scene)
        {
            if (CacheTargets)
            {
                UpdateTargetCache();
            }

            if (!string.IsNullOrEmpty(Flag))
            {
                previousFlagValue = SceneAs<Level>()?.Session?.GetFlag(Flag) ?? false;
                updateFlagValues = true;
            }

            Camera camera = SceneAs<Level>().Camera;

            previousCameraPosition = camera.Position;
            previousCameraZoom = camera.Zoom;

            if (ActivationMode == ActivationModes.OnAwake)
            {
                UpdateEntities();
            }

            if (ActivationMode == ActivationModes.OnCameraMoved)
            {
                scene.OnEndOfFrame += () =>
                {
                    UpdateEntities();
                };
            }

            // Always fire update entities with OnFlagActive/OnFlagInactive in Awake
            // Then respect the update rate afterwards
            if (updateFlagValues)
            {
                if (ActivationMode == ActivationModes.OnFlagActive && previousFlagValue)
                {
                    UpdateEntities();
                }
                else if (ActivationMode == ActivationModes.OnFlagInactive && !previousFlagValue)
                {
                    UpdateEntities();
                }
            }
        }

        public void UpdateEntities()
        {
            switch (Mode)
            {
                case EffectModes.ActivateInsideDeactivateOutside:
                    ActivateInsideDeactivateOutside();
                    break;

                case EffectModes.ActivateInside:
                    ActivateInside();
                    break;

                case EffectModes.DeactivateInside:
                    DeactivateInside();
                    break;

                case EffectModes.ActivateOutside:
                    ActivateOutside();
                    break;

                case EffectModes.DeactivateOutside:
                    DeactivateOutside();
                    break;

                case EffectModes.ActivateOnScreenDeactivateOffScreen:
                    ActivateOnScreenDeactivateOffScreen();
                    break;

                default:
                    Logger.Log(PandorasBoxModule.LoggerTag, $"Unsupported entity activator mode: {Mode}");
                    break;
            }
        }

        public void UpdateTargetCache()
        {
            cachedTargets = FindTargetEntities(true);
        }

        public List<Entity> FindTargetEntities(bool skipCache=false)
        {
            if (CacheTargets && !skipCache)
            {
                return cachedTargets;
            }

            return TypeHelper.FindTargetEntities(Scene, Targets, UseTracked);
        }

        public bool OnInterval()
        {
            return UpdateInterval <= 0f || Scene.OnInterval(UpdateInterval);
        }

        public void UpdateTarget(Entity target, bool visible, bool active, bool collidable)
        {
            if (ChangeCollidable)
            {
                target.Collidable = collidable;
            }

            if (ChangeVisible)
            {
                target.Visible = visible;
            }

            if (ChangeActive)
            {
                target.Active = active;
            }

            // Pointless to iterate if we aren't changing anything anyway
            if (AffectComponents && (ChangeVisible || ChangeActive))
            {
                foreach (Component component in target.Components)
                {
                    if (ChangeVisible)
                    {
                        component.Visible = visible;
                    }

                    if (ChangeActive)
                    {
                        component.Active = active;
                    }
                }
            }
        }

        public bool EntityInside(Entity target)
        {
            if (target.Collider == null)
            {
                return target.X >= Collider.AbsoluteLeft && target.X <= Collider.AbsoluteRight && target.Y >= Collider.AbsoluteTop && target.Y <= Collider.AbsoluteBottom;
            }

            return target.Collider.Collide(Collider);
        }

        public void ActivateInsideDeactivateOutside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (EntityInside(entity))
                {
                    UpdateTarget(entity, true, true, true);
                }
                else
                {
                    UpdateTarget(entity, false, false, false);
                }
            }
        }

        public void ActivateInside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (EntityInside(entity))
                {
                    UpdateTarget(entity, true, true, true);
                }
            }
        }

        public void DeactivateInside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (EntityInside(entity))
                {
                    UpdateTarget(entity, false, false, false);
                }
            }
        }

        public void ActivateOutside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (!EntityInside(entity))
                {
                    UpdateTarget(entity, true, true, true);
                }
            }
        }

        public void DeactivateOutside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (!EntityInside(entity))
                {
                    UpdateTarget(entity, false, false, false);
                }
            }
        }

        private void ActivateOnScreenDeactivateOffScreen()
        {
            Collider triggerCollider = Collider;
            Camera camera = SceneAs<Level>().Camera;
            float cameraWidth = camera.Right - camera.Left;
            float cameraHeight = camera.Bottom - camera.Top;

            Collider = new Hitbox(cameraWidth * 3, cameraHeight * 3, camera.Position.X - cameraWidth - Position.X, camera.Position.Y - cameraHeight - Position.Y);

            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (EntityInside(entity))
                {
                    UpdateTarget(entity, true, true, true);
                }
                else
                {
                    UpdateTarget(entity, false, false, false);
                }
            }

            Collider = triggerCollider;
        }
    }
}