﻿using System;
using Celeste;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;



namespace Celeste.Mod.JackalHelper.Triggers
{
	[CustomEntity("JackalHelper/BlizzardRemovalTrigger.cs")]

	public class BlizzardRemovalTrigger : Trigger
	{

		Level level;

		public BlizzardRemovalTrigger(EntityData data, Vector2 offset)
			: base(data, offset)
		{
		}


		public void SetColorGrade(String str, float speed)
		{
			level.NextColorGrade(str, speed);
		}

		public void SetColorGrade(String str)
		{
			level.NextColorGrade(str);
		}

		public override void OnEnter(Player player)
		{
			level = base.Scene as Level;
			level.Wind.X = 0f;
			level.Wind.Y = 0f;
			WindController windController = base.Scene.Entities.FindFirst<WindController>();
			base.Scene.Remove(windController);
			WindController calm = new WindController(WindController.Patterns.None);
			base.Scene.Add(calm);
			SetColorGrade("cryoBase");
		}

	}

}