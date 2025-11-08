using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    [CustomEntity("pandorasBox/dashToggleBlock")]
    class DashToggleBlock : Solid
    {
        public bool active;
        public int divisor;
        public List<int> indices;
        public int counter;
        public Color renderColor;

        public DashToggleBlock(Vector2 position, float width, float height) : base(position, width, height, false)
        {
            
        }

        public DashToggleBlock(EntityData data, Vector2 offset) : this(data.Position + offset, data.Width, data.Height)
        {
            indices = new List<int>();
            divisor = int.Parse(data.Attr("divisor", "2"));

            String rawIndices = data.Attr("index", "0");

            foreach (String s in rawIndices.Split(','))
            {
                indices.Add(int.Parse(s));
            }

            active = indices.Contains(counter);

            this.Add(new DashListener()
                {
                    OnDash = new Action<Vector2>(this.OnDash)
                }
            );
        }

        public void UpdateState()
        {
            bool shouldBeActive = indices.Contains(counter);

            if (shouldBeActive)
            {
                TheoCrystal theoCrystal = this.CollideFirst<TheoCrystal>();
                Player player = this.CollideFirst<Player>();
                
                if (player == null && theoCrystal == null)
                {
                    Depth = -9990;
                    Collidable = true;
                    active = true;

                    renderColor = Color.Red;
                }
                else
                {
                    renderColor = Color.Yellow;
                }
            }
            else
            {
                Depth = 8990;
                Collidable = false;
                active = false;

                renderColor = Color.Green;
            }
        }

        public override void Update()
        {
            base.Update();
            UpdateState();
        }

        private void OnDash(Vector2 direction)
        {
            counter = (counter + 1) % divisor;
            UpdateState();
        }

        public override void Render()
        {
            base.Render();
            Draw.Rect(this.Collider, renderColor);
        }
    }
}
