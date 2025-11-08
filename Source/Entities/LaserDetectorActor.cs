using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    [Tracked(true)]
    class LaserDetectorActor : Actor
    {
        private int detectionDelay = 0;
        private HashSet<Laserbeam> handledBeams;

        private float deltaTimeAcc = 0f;

        private static float oneFrame = 1f / 60f;

        public int DetectionDelay {
            get {
                return detectionDelay;
            }
            set
            {
                detectionDelay = value;
                laserbeamPaq = new PeekAQueue<List<Laserbeam>>(detectionDelay + 1);
            }
        }

        public Collider LaserBlockingCollider = null;
        public Collider LaserDetectionCollider = null;

        protected PeekAQueue<List<Laserbeam>> laserbeamPaq;

        public LaserDetectorActor(Vector2 position) : base(position)
        {
            DetectionDelay = 0;
            handledBeams = new HashSet<Laserbeam>();
        }

        public virtual void OnNewLaserbeam(Laserbeam beam)
        {

        }

        public virtual void OnDeadLaserbeam(Laserbeam beam)
        {
            
        }

        public virtual void OnLaserbeams(List<Laserbeam> laserbeams)
        {

        }

        public virtual Collider GetLaserBlockingCollider(Laserbeam beam)
        {
            return LaserBlockingCollider;
        }

        public virtual bool LaserBlockingCheck(Laserbeam beam)
        {
            Collider origCollider = Collider;
            Collider = GetLaserBlockingCollider(beam);

            bool colliding = Collide.Check(beam, this);

            Collider = origCollider;

            return colliding;
        }

        private void UpdateLaserQueue()
        {
            if (laserbeamPaq.Count > DetectionDelay)
            {
                List<Laserbeam> laserbeams = laserbeamPaq.Peek(DetectionDelay);
                OnLaserbeams(laserbeams);
                HashSet<Laserbeam> handledThisUpdate = new HashSet<Laserbeam>();

                foreach (Laserbeam beam in laserbeams)
                {
                    if (!handledBeams.Contains(beam))
                    {
                        handledBeams.Add(beam);
                        OnNewLaserbeam(beam);
                    }

                    handledThisUpdate.Add(beam);
                }

                foreach (Laserbeam beam in handledBeams.Except(handledThisUpdate).ToArray())
                {
                    handledBeams.Remove(beam);
                    OnDeadLaserbeam(beam);
                }
            }

            laserbeamPaq.Enqueue(LaserHelper.ConnectedLasers(Scene, this));
        }

        public override void Update()
        {
            // Update based on when frames should have passed time wise, not actual render frames.

            bool approxOneFrame = Math.Abs(Engine.DeltaTime - oneFrame) < 0.001f;

            if (approxOneFrame)
            {
                UpdateLaserQueue();
            }
            else
            {
                deltaTimeAcc += Engine.DeltaTime;

                while (deltaTimeAcc >= oneFrame)
                {
                    UpdateLaserQueue();

                    deltaTimeAcc -= oneFrame;
                }
            }

            base.Update();
        }
    }
}
