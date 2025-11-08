using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.PandorasBox
{
	public class PandorasBoxPostcard : Scene
	{
		private static FieldInfo engineScene = typeof(Engine).GetField("scene", BindingFlags.Instance | BindingFlags.NonPublic);

		private Postcard postcard;
		private bool completeChapter;
		private Scene oldScene;

		public PandorasBoxPostcard(Postcard postcard, Scene oldScene, Boolean completeChapter)
		{
			Audio.SetMusic(null);
			Audio.SetAmbience(null);

			this.postcard = postcard;
			this.completeChapter = completeChapter;
			this.oldScene = oldScene;

			Add(new Entity {
				new Coroutine(postcardRoutine())
			});
			Add(new HudRenderer());
		}
		
        private IEnumerator postcardRoutine()
        {
			yield return 0.25f;

			Add(postcard);

			yield return postcard.DisplayRoutine();

            if (!completeChapter)
            {
				Engine.Scene = new OverworldLoader(Overworld.StartMode.MainMenu);
			}
			else
            {
				Engine engine = Engine.Instance;
				Scene previousScene = Engine.Scene;

				engineScene.SetValue(engine, oldScene);
				(oldScene as Level).CompleteArea(false, true, true);
				engineScene.SetValue(engine, previousScene);
			}
        }

        public override void BeforeRender()
		{
			base.BeforeRender();

			if (postcard != null)
			{
				postcard.BeforeRender();
			}
		}
	}
}
