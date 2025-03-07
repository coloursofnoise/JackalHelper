﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.JackalHelper.Entities
{

    [CustomEntity("JackalHelper/TracerRefill")]
    [Tracked]
    public class StopwatchRefill : Entity
    {

			public static ParticleType P_Shatter;

			public static ParticleType P_Regen;

			public static ParticleType P_Glow;

			public static ParticleType P_ShatterTwo;

			public static ParticleType P_RegenTwo;

			public static ParticleType P_GlowTwo;

			private Sprite sprite;

			private Sprite flash;

			private Image outline;

			private Wiggler wiggler;

			private BloomPoint bloom;

			private VertexLight light;

			private Level level;

			private SineWave sine;

			private bool oneUse;

			private ParticleType p_shatter;

			private ParticleType p_regen;

			private ParticleType p_glow;

			private float respawnTimer;

			public bool timed;

			public float recallTime;

			public float recallTimer = 0f;

		private bool refillDash;

		public StopwatchRefill(Vector2 position, bool oneUse, bool refillDash, float time)
				: base(position)
			{
			this.refillDash = refillDash;
			this.recallTime = time;
			base.Collider = new Hitbox(16f, 16f, -8f, -8f);
			Add(new PlayerCollider(OnPlayer));
			this.oneUse = oneUse;
			string str = "objects/stopwatch/";
			p_shatter = Refill.P_Shatter;
			p_regen = Refill.P_Regen;
			p_glow = Refill.P_Glow;
			Add(outline = new Image(GFX.Game[str + "outline"]));
			outline.CenterOrigin();
			outline.Visible = false;
			Add(sprite = new Sprite(GFX.Game, str + "idle"));
			sprite.AddLoop("idle", "", 0.1f);
			sprite.Play("idle");
			sprite.CenterOrigin();
			Add(flash = new Sprite(GFX.Game, str + "flash"));
			flash.Add("flash", "", 0.05f);
			flash.OnFinish = delegate
			{
				flash.Visible = false;
			};
			flash.CenterOrigin();
			Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v)
			{
				sprite.Scale = (flash.Scale = Vector2.One * (1f + v * 0.2f));
			}));
			Add(new MirrorReflection());
			Add(bloom = new BloomPoint(0.8f, 16f));
			Add(light = new VertexLight(Color.White, 1f, 16, 48));
			Add(sine = new SineWave(0.6f));
			sine.Randomize();
			//UpdateY();
			base.Depth = -100;
		}

			public StopwatchRefill(EntityData data, Vector2 offset)
				: this(data.Position + offset, data.Bool("oneUse"), data.Bool("RefillDashOnUse", defaultValue: true), data.Float("time"))
			{
			}

			public override void Added(Scene scene)
			{
				base.Added(scene);
				level = SceneAs<Level>();
			}

			public override void Update()
			{
				base.Update();
				if (respawnTimer > 0f)
				{
					respawnTimer -= Engine.DeltaTime;
					if (respawnTimer <= 0f)
					{
						Respawn();
					}
				}
				if(recallTimer > 0f && recallTimer < recallTime)
            {
				recallTimer += Engine.DeltaTime;
				timed = true;
            }
				else if (recallTimer >= recallTime)
            {
				if (JackalModule.GetPlayer() != null)
				{
					JackalModule.GetPlayer().Position = (Position + new Vector2(0f, 8f));
				}
				timed = false;
				recallTimer = 0f;
			}


				/*
				else if (base.Scene.OnInterval(0.1f))
				{
					level.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);
				}*/
				UpdateY();
				light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
				bloom.Alpha = light.Alpha * 0.8f;
				if (base.Scene.OnInterval(2f) && sprite.Visible)
				{
					flash.Play("flash", restart: true);
					flash.Visible = true;
				}
			}

			private void Respawn()
			{
				if (!Collidable)
				{
					Collidable = true;
					sprite.Visible = true;
					outline.Visible = false;
					base.Depth = -100;
					wiggler.Start();
					Audio.Play("event:/new_content/game/10_farewell/pinkdiamond_return", Position);
					//level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
				}
			}

			private void UpdateY()
			{
				Sprite obj = flash;
				Sprite obj2 = sprite;
				float num2 = (bloom.Y = sine.Value * 2f);
				float num5 = (obj.Y = (obj2.Y = num2));
			}

			public override void Render()
			{
				if (sprite.Visible)
				{
					sprite.DrawOutline();
				}
				base.Render();
			}

			private void OnPlayer(Player player)
			{
					Audio.Play("event:/new_content/game/10_farewell/pinkdiamond_touch", Position);
					Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
					Collidable = false;
					timed = true;
					recallTimer += Engine.DeltaTime;
					
					Add(new Coroutine(RefillRoutine(player)));
					//Add(new Coroutine(RecallRoutine(player)));
					respawnTimer = 2f + (float)recallTime;
            if (refillDash){
				player.RefillDash();
				player.RefillStamina();
            }
			}

		private IEnumerator RecallRoutine(Player player)
        {
			timed = true;
			yield return recallTime;
			player.Position = (Position + new Vector2(0f, 8f));
			timed = false;
        }

			private IEnumerator RefillRoutine(Player player)
			{
				Celeste.Freeze(0.05f);
				yield return null;
				level.Shake();
				sprite.Visible = (flash.Visible = false);
				if (!oneUse)
				{
					outline.Visible = true;
				}
				Depth = 8999;
				yield return 0.05f;
				float angle = player.Speed.Angle();
				//level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle - (float)Math.PI / 2f);
				//level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle + (float)Math.PI / 2f);
				SlashFx.Burst(Position, angle);
				if (oneUse)
				{
					RemoveSelf();
				}
			}
		}

	}