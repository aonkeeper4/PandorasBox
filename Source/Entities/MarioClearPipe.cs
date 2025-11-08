using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;
using Celeste.Mod.PandorasBox.Entities.ClearPipeInteractions;

using static Celeste.Mod.PandorasBox.MarioClearPipeHelper;

// TODO - Attributes
// - Can player enter
// - Blocking off exits
// - Launch power?
// - Force dash entry?
// - Disable dash entry?

namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    [CustomEntity("pandorasBox/clearPipe")]
    public class MarioClearPipe : Entity
    {
        // Pixels smaller than the pipe width
        private static int pipeWidthColliderValue = 4;

        // Transport speed Per second
        public float TransportSpeed { get; protected set; } = 175f;
        private float transportSpeedEnterMultiplier = 0.75f;

        private int pipeWidth = 32;
        private int pipeColliderWidth = 28;
        private int pipeColliderDepth = 4;

        private string texturePath;
        private int surfaceSound;

        public bool HasPipeSolids { get; protected set; }

        private Vector2[] nodes;

        private Direction startDirection;
        private Direction endDirection;
        private Vector2 startDirectionVector;
        private Vector2 endDirectionVector;

        private Hitbox startCollider;
        private Hitbox endCollider;

        private bool legacyPipeMovementYields;

        private List<MarioClearPipeSolid> pipeSolids;

        public MarioClearPipe(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Depth = -5;

            nodes = data.NodesWithPosition(offset);

            removeRedundantNodes();

            texturePath = data.Attr("texture", "glass");
            surfaceSound = data.Int("surfaceSound", -1);

            HasPipeSolids = data.Bool("hasPipeSolids", true);
            TransportSpeed = data.Float("transportSpeed", 175f);

            pipeWidth = data.Int("pipeWidth", 32);
            pipeColliderWidth = pipeWidth - pipeWidthColliderValue;

            startDirection = GetPipeExitDirection(nodes[0], nodes[1]);
            endDirection = GetPipeExitDirection(nodes[nodes.Length - 1], nodes[nodes.Length - 2]);

            startDirectionVector = GetPipeExitDirectionVector(nodes[0], nodes[1]);
            endDirectionVector = GetPipeExitDirectionVector(nodes[nodes.Length - 1], nodes[nodes.Length - 2]);

            startCollider = getPipeCollider(Vector2.Zero, startDirection, pipeWidth, pipeColliderWidth, pipeColliderDepth);
            endCollider = getPipeCollider(new Vector2(nodes.Last().X - nodes.First().X, nodes.Last().Y - nodes.First().Y), endDirection, pipeWidth, pipeColliderWidth, pipeColliderDepth);

            legacyPipeMovementYields = data.Bool("useLegacyMovement", true);

            Collider = new ColliderList(startCollider, endCollider);

            pipeSolids = new List<MarioClearPipeSolid>();
        }

        // Node is considered redundant if it does not change the direction
        private void removeRedundantNodes()
        {
            List<Vector2> newNodes = new List<Vector2>();

            Vector2 previousNode = Vector2.Zero;
            Vector2 previousDirection = Vector2.Zero;
            bool hasPreviousNode = false;

            foreach (Vector2 node in nodes)
            {
                if (hasPreviousNode)
                {
                    Vector2 direction = (previousNode - node).SafeNormalize();

                    if (Math.Abs(direction.X - previousDirection.X) > 0.0005 || Math.Abs(direction.Y - previousDirection.Y) > 0.0005)
                    {
                        newNodes.Add(previousNode);
                    }

                    previousDirection = direction;
                }

                hasPreviousNode = true;
                previousNode = node;
            }

            newNodes.Add(nodes.Last());

            nodes = newNodes.ToArray();
        }

        private Hitbox getPipeCollider(Vector2 position, Direction exitDireciton, int pipeWidth, int pipeColliderWidth, int colliderDepth)
        {
            // Weird offset on Right/Down facing pipes with non multiples of 16 pipe width (8, 24, etc)
            switch (exitDireciton)
            {
                case Direction.Up:
                    return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y - colliderDepth);

                case Direction.Right:
                    if (pipeWidth / 8 % 2 == 1 && nodes.Length > 2)
                    {
                        return new Hitbox(colliderDepth, pipeColliderWidth, position.X - 4, position.Y - pipeColliderWidth / 2);
                    }
                    else
                    {
                        return new Hitbox(colliderDepth, pipeColliderWidth, position.X, position.Y - pipeColliderWidth / 2);
                    }

                case Direction.Down:
                    if (pipeWidth / 8 % 2 == 1 && nodes.Length > 2)
                    {
                        return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y - 4);
                    }
                    else
                    {
                        return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y);
                    }

                case Direction.Left:
                    return new Hitbox(colliderDepth, pipeColliderWidth, position.X - colliderDepth, position.Y - pipeColliderWidth / 2);

                default:
                    return new Hitbox(pipeWidth, pipeWidth, position.X - pipeWidth / 2, position.Y - pipeWidth / 2);
            }
        }

        private void addPipeSolids(int pipeWidth)
        {
            Vector2 previousNode = Vector2.Zero;
            bool hasPreviousNode = false;

            int i = 0;

            float halfPipeWidth = pipeWidth / 2f;

            foreach (Vector2 node in nodes)
            {
                if (hasPreviousNode)
                {
                    bool startNodeExit = i == 1;
                    bool endNodeExit = i == nodes.Count() - 1;

                    Vector2 nextNode = nodes.ElementAtOrDefault(i + 1);

                    MarioClearPipeSolid pipeSolid = MarioClearPipeSolid.FromNodes(previousNode, node, nextNode, startNodeExit, endNodeExit, pipeWidth, texturePath, surfaceSound);

                    pipeSolids.Add(pipeSolid);
                    Scene.Add(pipeSolid);
                }

                hasPreviousNode = true;
                previousNode = node;
                i++;
            }
        }

        private IEnumerator moveBetweenNodes(Entity entity, MarioClearPipeInteraction interaction, Vector2 from, Vector2 to, float? travelSpeed=null, bool firstEntry=false, bool exiting=false)
        {
            // Directions should not be affected by offset
            interaction.DirectionVector = GetPipeExitDirectionVector(to, from);
            interaction.Direction = GetPipeExitDirection(to, from);

            interaction.From = from;
            interaction.To = to;

            if (!exiting)
            {
                interaction.To += interaction.PipeRenderOffset;
            }

            if (!firstEntry)
            {
                interaction.From += interaction.PipeRenderOffset;
            }

            // Travel distance should be the actual distance the entity has to travel
            interaction.Distance = (from - to).Length();
            interaction.TravelSpeed = travelSpeed != null ? travelSpeed.Value : TransportSpeed;

            while (entity != null && interaction.Moved <= interaction.Distance && interaction.Distance != 0f && !interaction.ExitEarly)
            {
                interaction?.OnPipeUpdate(entity, interaction);

                float lerpValue = interaction.Moved / interaction.Distance;

                entity.Position = Vector2.Lerp(interaction.From, interaction.To, lerpValue);
                interaction.Moved += interaction.TravelSpeed * Engine.DeltaTime;

                // Only yield if we are still in the same segment
                if (interaction.Moved <= interaction.Distance || legacyPipeMovementYields)
                {
                    yield return null;
                }
            }

            // Prevent oddities with overshooting
            entity.Position = interaction.To;

            interaction.Moved -= interaction.Distance;
        }

        // Visually update exiting steps
        // Correct for exit overshooting
        private IEnumerator exitPipeMovement(Entity entity, MarioClearPipeInteraction interaction)
        {
            if (entity == null || entity.Scene == null) {
                yield break;
            }
            
            Vector2 previousPosition = entity.Position;
            Vector2 currentPosition = entity.Position;

            bool colliding = entity.CollideFirst<MarioClearPipeSolid>() != null;

            while (colliding && !interaction.ExitEarly)
            {
                interaction?.OnPipeUpdate(entity, interaction);

                entity.Position += interaction.DirectionVector * TransportSpeed * Engine.DeltaTime;

                previousPosition = currentPosition;
                currentPosition = entity.Position;

                colliding = entity.CollideFirst<MarioClearPipeSolid>() != null;

                if (colliding)
                {
                    yield return null;
                }
            }

            if (interaction.ExitEarly)
            {
                yield break;
            }

            // Correct for overshooting the exit, attempt to place entity as close as possible to the pipe

            Vector2 lowerValue = previousPosition;
            Vector2 upperValue = currentPosition;
            Vector2 lastUnblocked = entity.Position;

            while ((lowerValue - upperValue).LengthSquared() > 0.5f)
            {
                entity.Position = Vector2.Lerp(lowerValue, upperValue, 0.5f);

                if (entity.CollideFirst<MarioClearPipeSolid>() != null)
                {
                    lowerValue = entity.Position;
                }
                else
                {
                    upperValue = entity.Position;
                    lastUnblocked = entity.Position;
                }
            }

            entity.Position = lastUnblocked;
        }

        private void ejectFromPipe(Entity entity, MarioClearPipeInteraction interaction)
        {
            // Fix float positions, causes weird collision bugs for entities
            entity.Position = new Vector2((int)Math.Round(entity.Position.X), (int)Math.Round(entity.Position.Y));

            interaction.OnPipeExit?.Invoke(entity, interaction);
            interaction.CurrentClearPipe = null;
            interaction.Moved = 0f;

            CurrentlyTransportedEntities.Remove(entity);
        }

        // TODO - Cleanup and make more generic 
        private IEnumerator pipeMovement(Entity entity, bool fromStart, bool canBounceBack=true, Vector2? forcedStartPosition=null)
        {
            MarioClearPipeInteraction interaction = GetClearPipeInteraction(entity);

            int startIndex = fromStart ? 0 : nodes.Length - 1;
            int lastIndex = fromStart ? nodes.Length - 1 : 0;
            int direction = fromStart ? 1 : -1;

            Direction transportStartDirection = fromStart ? startDirection : endDirection;
            Direction transportEndDirection = fromStart ? endDirection : startDirection;

            if (forcedStartPosition != null || CanTransportEntity(entity, transportStartDirection))
            {
                CurrentlyTransportedEntities.Add(entity);
                interaction.CurrentClearPipe = this;
                interaction.ExitEarly = false;
                interaction?.OnPipeEnter?.Invoke(entity, interaction);

                // Check if we are entering the pipe or bouncing back from a blocked exit
                if (forcedStartPosition != null)
                {
                    entity.Position = forcedStartPosition.Value;
                }
                else
                {
                    bool firstEntry = forcedStartPosition == null;

                    // Gracefully attempt to move to the first node
                    IEnumerator enumerator = moveBetweenNodes(entity, interaction, entity.Position, nodes[startIndex], TransportSpeed * transportSpeedEnterMultiplier, firstEntry);

                    if (legacyPipeMovementYields)
                    {
                        yield return enumerator;
                    }
                    else
                    {
                        while (enumerator.MoveNext()) { yield return null; }
                    }
                }

                // Follow the nodes
                for (int i = startIndex; i != lastIndex && !interaction.ExitEarly; i += direction)
                {
                    IEnumerator enumerator = moveBetweenNodes(entity, interaction, nodes[i], nodes[i + direction]);

                    if (legacyPipeMovementYields)
                    {
                        yield return enumerator;
                    }
                    else
                    {
                        while (enumerator.MoveNext()) { yield return null; }
                    }
                }

                if (interaction.ExitEarly)
                {
                    ejectFromPipe(entity, interaction);

                    yield break;
                }

                // Check if we can exit the pipe
                if (CanExitPipe(entity, interaction.DirectionVector, TransportSpeed))
                {
                    IEnumerator enumerator = exitPipeMovement(entity, interaction);

                    if (legacyPipeMovementYields)
                    {
                        yield return enumerator;
                    }
                    else
                    {
                        while (enumerator.MoveNext()) { yield return null; }
                    }
                }

                // Send back if it gets stuck in a solid
                if (entity != null && entity.Scene != null && entity.CollideCheck<Solid>() && !interaction.ExitEarly)
                {
                    if (canBounceBack)
                    {
                        entity.Position = nodes[lastIndex] + interaction.PipeRenderOffset;

                        IEnumerator enumerator = pipeMovement(entity, !fromStart, false, entity.Position);

                        if (legacyPipeMovementYields)
                        {
                            yield return enumerator;
                        }
                        else
                        {
                            while (enumerator.MoveNext()) { yield return null; }
                        }
                    }
                    else
                    {
                        // TODO: This should try to kill theo etc?
                        ejectFromPipe(entity, interaction);
                    }
                }
                else
                {
                    ejectFromPipe(entity, interaction);
                }
            }
        }

        public override void Update()
        {
            List<Component> components = Scene.Tracker.GetComponents<MarioClearPipeInteraction>();

            foreach (MarioClearPipeInteraction component in components)
            {
                Entity entity = component.Entity;

                if (entity.Collider == null)
                {
                    continue;
                }

                if (entity.Collider.Collide(startCollider))
                {
                    if (CanTransportEntity(entity, startDirection)) {
                        Add(new Coroutine(pipeMovement(entity, true)));
                    }
                }
                else if (entity.Collider.Collide(endCollider))
                {
                    if (CanTransportEntity(entity, endDirection))
                    {
                        Add(new Coroutine(pipeMovement(entity, false)));
                    }
                }
            }

            base.Update();
        }

        public override void Awake(Scene scene)
        {
            if (HasPipeSolids)
            {
                addPipeSolids(pipeWidth);
            }

            foreach (Entity entity in Scene.Entities)
            {
                AddClearPipeInteraction(entity);
            }

            AllowComponentsForList(Scene.Entities);

            base.Awake(scene);
        }

        private static void EntityList_Add_Entity(On.Monocle.EntityList.orig_Add_Entity orig, EntityList self, Entity entity)
        {
            if (ShouldAddComponentsForList(self))
            {
                AddClearPipeInteraction(entity);
            }

            orig(self, entity);
        }

        public static void Load()
        {
            InteractionRegistry.Add(new HoldableInteraction());
            InteractionRegistry.Add(new PlayerInteraction());
            InteractionRegistry.Add(new PufferInteraction());

            InteractionRegistry.Load();

            On.Monocle.EntityList.Add_Entity += EntityList_Add_Entity;
        }

        public static void Unload()
        {
            InteractionRegistry.Unload();

            On.Monocle.EntityList.Add_Entity -= EntityList_Add_Entity;
        }
    }
}
