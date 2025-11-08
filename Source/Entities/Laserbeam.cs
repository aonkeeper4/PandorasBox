using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Celeste.Mod.PandorasBox
{
    [Tracked]
    class Laserbeam : Entity
    {
        private MTexture beamTexture;
        private float length;
        private Vector2 previousParentPosition;

        public Color Color;
        public string Direction;
        public float Rotation;
        public int TTL;
        public Entity Parent;

        public float Length
        {
            get => length;
            set
            {
                length = value;
                updateCollider();
            }
        }

        public Laserbeam(Vector2 position, string direction, Color color, int ttl=-1, Entity parent=null) : base(position)
        {
            beamTexture = GFX.Game["objects/pandorasBox/laser/beam/beam"];

            Color = color;
            Direction = direction;
            TTL = ttl;

            Parent = parent;

            Rotation = LaserHelper.DirectionRotations[direction];
            Length = 8;

            Depth = 100;

            if (Parent != null)
            {
                previousParentPosition = Parent.Position;
            }
        }

        private void updateCollider()
        {
            Vector2 offset = new Vector2(Length, 0f).Rotate(Rotation);
            Vector2 height = new Vector2(0f, beamTexture.Height).Rotate(Rotation);

            Collider = new Hitbox(offset.X + height.X, offset.Y + height.Y, -height.X / 2, -height.Y / 2);
            Collider.Position.X = Collider.Width < 0 ? Collider.Position.X + Collider.Width : Collider.Position.X;
            Collider.Position.Y = Collider.Height < 0 ? Collider.Position.Y + Collider.Height : Collider.Position.Y;
            Collider.Width = Math.Abs(Collider.Width);
            Collider.Height = Math.Abs(Collider.Height);
        }

        public override void Render()
        {
            beamTexture.Draw(Position, new Vector2(0f, beamTexture.Height / 2f), Color, new Vector2(Length / beamTexture.Width, 1f), Rotation);

            base.Render();
        }

        public override void Update()
        {
            if (Parent != null)
            {
                Position += Parent.Position - previousParentPosition;
                previousParentPosition = Parent.Position;
            }

            if (TTL == 0)
            {
                RemoveSelf();
            }

            LaserHelper.SetLaserLength(Scene, this);

            TTL--;

            base.Update();
        }
    }
}
