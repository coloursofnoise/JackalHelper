﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.JackalHelper.Entities
{
	[TrackedAs(typeof(DustStaticSpinner))]
	[CustomEntity("JackalHelper/CustomDustSprite")]
    public class CustomDustSprite : Entity
    {
		public static ParticleType P_Move;

		public const float ParticleInterval = 0.02f;

		private float offset = Calc.Random.NextFloat();
		public CustomDustGraphic Sprite;
        public CustomDustSprite(Vector2 position, bool attachToSolid, string spritePath, bool ignoreSolids = false) : base(position)
        {
			base.Collider = new ColliderList(new Circle(6f), new Hitbox(16f, 4f, -8f, -3f));
			Add(new PlayerCollider(OnPlayer));
			Add(new HoldableCollider(OnHoldable));
			Add(new LedgeBlocker());
			Add(Sprite = new CustomDustGraphic(ignoreSolids, autoControlEyes: true, autoExpandDust: true));
			base.Depth = -50;
			if (attachToSolid)
			{
				Add(new StaticMover
				{
					OnShake = OnShake,
					SolidChecker = IsRiding
				});
			}
		}

        public CustomDustSprite(EntityData data, Vector2 offset) : this(data.Position + offset, data.Bool("attachToSolid"), data.Attr("spritePath"), data.Bool("ignoreSolids", defaultValue:false))
        {

        }


		public void ForceInstantiate()
		{
			Sprite.AddDustNodesIfInCamera();
		}

		public override void Update()
		{
			base.Update();
			if (base.Scene.OnInterval(0.05f, offset) && Sprite.Estableshed)
			{
				Player entity = base.Scene.Tracker.GetEntity<Player>();
				if (entity != null)
				{
					Collidable = Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f;
				}
			}
		}

		private void OnShake(Vector2 pos)
		{
			Sprite.Position = pos;
		}

		private bool IsRiding(Solid solid)
		{
			return CollideCheck(solid);
		}

		private void OnPlayer(Player player)
		{
			player.Die((player.Position - Position).SafeNormalize());
			Sprite.OnHitPlayer();
		}

		private void OnHoldable(Holdable h)
		{
			h.HitSpinner(this);
		}



	}

	[Tracked]
	public class CustomDustGraphic : Component
	{
		public class Node
		{
			public MTexture Base;

			public MTexture Overlay;

			public float Rotation;

			public Vector2 Angle;

			public bool Enabled;
		}

		private class Eyeballs : Entity
		{
			public CustomDustGraphic Dust;

			public Color Color;

			public Eyeballs(CustomDustGraphic dust)
			{
				Dust = dust;
				base.Depth = Dust.Entity.Depth - 1;
			}

			public override void Added(Scene scene)
			{
				base.Added(scene);
				Color = DustStyles.Get(scene).EyeColor;
			}

			public override void Update()
			{
				base.Update();
				if (Dust.Entity == null || Dust.Scene == null)
				{
					RemoveSelf();
				}
			}

			public override void Render()
			{
				if (Dust.Visible && Dust.Entity.Visible)
				{
					Vector2 vector = new Vector2(0f - Dust.EyeDirection.Y, Dust.EyeDirection.X).SafeNormalize();
					if (Dust.leftEyeVisible)
					{
						Dust.eyeTexture.DrawCentered(Dust.RenderPosition + (Dust.EyeDirection * 5f + vector * 3f) * Dust.Scale, Color, Dust.Scale);
					}
					if (Dust.rightEyeVisible)
					{
						Dust.eyeTexture.DrawCentered(Dust.RenderPosition + (Dust.EyeDirection * 5f - vector * 3f) * Dust.Scale, Color, Dust.Scale);
					}
				}
			}
		}

		public Vector2 Position;

		public float Scale = 1f;

		private MTexture center;

		public Action OnEstablish;

		private List<Node> nodes = new List<Node>();

		public List<Node> LeftNodes = new List<Node>();

		public List<Node> RightNodes = new List<Node>();

		public List<Node> TopNodes = new List<Node>();

		public List<Node> BottomNodes = new List<Node>();

		public Vector2 EyeTargetDirection;

		public Vector2 EyeDirection;

		public int EyeFlip = 1;

		private bool eyesExist;

		private int eyeTextureIndex;

		private MTexture eyeTexture;

		private Vector2 eyeLookRange;

		private bool eyesMoveByRotation;

		private bool autoControlEyes;

		private bool eyesFollowPlayer;

		private Coroutine blink;

		private bool leftEyeVisible = true;

		private bool rightEyeVisible = true;

		private Eyeballs eyes;

		private float timer;

		private float offset;

		private bool ignoreSolids;

		private bool autoExpandDust;

		private float shakeTimer;

		private Vector2 shakeValue;

		private int randomSeed;

		public bool Estableshed { get; private set; }

		public Vector2 RenderPosition => base.Entity.Position + Position + shakeValue;

		private bool InView
		{
			get
			{
				Camera camera = (base.Scene as Level).Camera;
				Vector2 position = base.Entity.Position;
				if (!(position.X + 16f < camera.Left) && !(position.Y + 16f < camera.Top) && !(position.X - 16f > camera.Right))
				{
					return !(position.Y - 16f > camera.Bottom);
				}
				return false;
			}
		}

		public string spritePath;

		public CustomDustGraphic(bool ignoreSolids, bool autoControlEyes = false, bool autoExpandDust = false, string spritePath = "danger/customDustSprite/")
			: base(active: true, visible: true)
		{
			this.spritePath = spritePath;
			this.ignoreSolids = ignoreSolids;
			this.autoControlEyes = autoControlEyes;
			this.autoExpandDust = autoExpandDust;
			center = Calc.Random.Choose(GFX.Game.GetAtlasSubtextures(spritePath + "center"));
			offset = Calc.Random.NextFloat() * 4f;
			timer = Calc.Random.NextFloat();
			EyeTargetDirection = (EyeDirection = Calc.AngleToVector(Calc.Random.NextFloat((float)Math.PI * 2f), 1f));
			eyeTextureIndex = Calc.Random.Next(128);
			eyesExist = true;
			if (autoControlEyes)
			{
				eyesExist = Calc.Random.Chance(0.5f);
				eyesFollowPlayer = Calc.Random.Chance(0.3f);
			}
			randomSeed = Calc.Random.Next();
		}

		public override void Added(Entity entity)
		{
			base.Added(entity);
			TransitionListener transitionListener = new TransitionListener();
			transitionListener.OnIn = delegate
			{
				AddDustNodesIfInCamera();
			};
			entity.Add(transitionListener);
			entity.Add(new DustEdge(Render));
		}

		public override void Update()
		{
			timer += Engine.DeltaTime * 0.6f;
			bool inView = InView;
			if (shakeTimer > 0f)
			{
				shakeTimer -= Engine.DeltaTime;
				if (shakeTimer <= 0f)
				{
					shakeValue = Vector2.Zero;
				}
				else if (base.Scene.OnInterval(0.05f))
				{
					shakeValue = Calc.Random.ShakeVector();
				}
			}
			if (eyesExist)
			{
				if (EyeDirection != EyeTargetDirection && inView)
				{
					if (!eyesMoveByRotation)
					{
						EyeDirection = Calc.Approach(EyeDirection, EyeTargetDirection, 12f * Engine.DeltaTime);
					}
					else
					{
						float val = EyeDirection.Angle();
						float num = EyeTargetDirection.Angle();
						val = Calc.AngleApproach(val, num, 8f * Engine.DeltaTime);
						if (val == num)
						{
							EyeDirection = EyeTargetDirection;
						}
						else
						{
							EyeDirection = Calc.AngleToVector(val, 1f);
						}
					}
				}
				if (eyesFollowPlayer && inView)
				{
					Player entity = base.Entity.Scene.Tracker.GetEntity<Player>();
					if (entity != null)
					{
						Vector2 vector = (entity.Position - base.Entity.Position).SafeNormalize();
						if (eyesMoveByRotation)
						{
							float target = vector.Angle();
							float val2 = eyeLookRange.Angle();
							EyeTargetDirection = Calc.AngleToVector(Calc.AngleApproach(val2, target, (float)Math.PI / 4f), 1f);
						}
						else
						{
							EyeTargetDirection = vector;
						}
					}
				}
				if (blink != null)
				{
					blink.Update();
				}
			}
			if (nodes.Count <= 0 && base.Entity.Scene != null && !Estableshed)
			{
				AddDustNodesIfInCamera();
				return;
			}
			foreach (Node node in nodes)
			{
				node.Rotation += Engine.DeltaTime * 0.5f;
			}
		}

		public void OnHitPlayer()
		{
			if (!SaveData.Instance.Assists.Invincible)
			{
				shakeTimer = 0.6f;
				if (eyesExist)
				{
					blink = null;
					leftEyeVisible = true;
					rightEyeVisible = true;
					eyeTexture = GFX.Game[spritePath + "deadEyes"];
				}
			}
		}

		public void AddDustNodesIfInCamera()
		{
			if (nodes.Count > 0 || !InView || DustEdges.DustGraphicEstabledCounter > 25 || Estableshed)
			{
				return;
			}
			Calc.PushRandom(randomSeed);
			int num = (int)base.Entity.X;
			int num2 = (int)base.Entity.Y;
			Vector2 vector = new Vector2(1f, 1f).SafeNormalize();
			AddNode(new Vector2(0f - vector.X, 0f - vector.Y), ignoreSolids || !base.Entity.Scene.CollideCheck<Solid>(new Rectangle(num - 8, num2 - 8, 8, 8)));
			AddNode(new Vector2(vector.X, 0f - vector.Y), ignoreSolids || !base.Entity.Scene.CollideCheck<Solid>(new Rectangle(num, num2 - 8, 8, 8)));
			AddNode(new Vector2(0f - vector.X, vector.Y), ignoreSolids || !base.Entity.Scene.CollideCheck<Solid>(new Rectangle(num - 8, num2, 8, 8)));
			AddNode(new Vector2(vector.X, vector.Y), ignoreSolids || !base.Entity.Scene.CollideCheck<Solid>(new Rectangle(num, num2, 8, 8)));
			if (nodes[0].Enabled || nodes[2].Enabled)
			{
				Position.X -= 1f;
			}
			if (nodes[1].Enabled || nodes[3].Enabled)
			{
				Position.X += 1f;
			}
			if (nodes[0].Enabled || nodes[1].Enabled)
			{
				Position.Y -= 1f;
			}
			if (nodes[2].Enabled || nodes[3].Enabled)
			{
				Position.Y += 1f;
			}
			int num3 = 0;
			foreach (Node node in nodes)
			{
				if (node.Enabled)
				{
					num3++;
				}
			}
			eyesMoveByRotation = num3 < 4;
			if (autoControlEyes && eyesExist && eyesMoveByRotation)
			{
				eyeLookRange = Vector2.Zero;
				if (nodes[0].Enabled)
				{
					eyeLookRange += new Vector2(-1f, -1f).SafeNormalize();
				}
				if (nodes[1].Enabled)
				{
					eyeLookRange += new Vector2(1f, -1f).SafeNormalize();
				}
				if (nodes[2].Enabled)
				{
					eyeLookRange += new Vector2(-1f, 1f).SafeNormalize();
				}
				if (nodes[3].Enabled)
				{
					eyeLookRange += new Vector2(1f, 1f).SafeNormalize();
				}
				if (num3 > 0 && eyeLookRange.Length() > 0f)
				{
					eyeLookRange /= (float)num3;
					eyeLookRange = eyeLookRange.SafeNormalize();
				}
				EyeTargetDirection = (EyeDirection = eyeLookRange);
			}
			if (eyesExist)
			{
				blink = new Coroutine(BlinkRoutine());
				List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(DustStyles.Get(base.Scene).EyeTextures);
				eyeTexture = atlasSubtextures[eyeTextureIndex % atlasSubtextures.Count];
				base.Entity.Scene.Add(eyes = new Eyeballs(this));
			}
			DustEdges.DustGraphicEstabledCounter++;
			Estableshed = true;
			if (OnEstablish != null)
			{
				OnEstablish();
			}
			Calc.PopRandom();
		}

		private void AddNode(Vector2 angle, bool enabled)
		{
			Vector2 vector = new Vector2(1f, 1f);
			if (autoExpandDust)
			{
				int num = Math.Sign(angle.X);
				int num2 = Math.Sign(angle.Y);
				base.Entity.Collidable = false;
				if (base.Scene.CollideCheck<Solid>(new Rectangle((int)(base.Entity.X - 4f + (float)(num * 16)), (int)(base.Entity.Y - 4f + (float)(num2 * 4)), 8, 8)) || base.Scene.CollideCheck<DustStaticSpinner>(new Rectangle((int)(base.Entity.X - 4f + (float)(num * 16)), (int)(base.Entity.Y - 4f + (float)(num2 * 4)), 8, 8)))
				{
					vector.X = 5f;
				}
				if (base.Scene.CollideCheck<Solid>(new Rectangle((int)(base.Entity.X - 4f + (float)(num * 4)), (int)(base.Entity.Y - 4f + (float)(num2 * 16)), 8, 8)) || base.Scene.CollideCheck<DustStaticSpinner>(new Rectangle((int)(base.Entity.X - 4f + (float)(num * 4)), (int)(base.Entity.Y - 4f + (float)(num2 * 16)), 8, 8)))
				{
					vector.Y = 5f;
				}
				base.Entity.Collidable = true;
			}
			Node node = new Node();
			node.Base = Calc.Random.Choose(GFX.Game.GetAtlasSubtextures(spritePath + "base"));
			node.Overlay = Calc.Random.Choose(GFX.Game.GetAtlasSubtextures(spritePath + "overlay"));
			node.Rotation = Calc.Random.NextFloat((float)Math.PI * 2f);
			node.Angle = angle * vector;
			node.Enabled = enabled;
			nodes.Add(node);
			if (angle.X < 0f)
			{
				LeftNodes.Add(node);
			}
			else
			{
				RightNodes.Add(node);
			}
			if (angle.Y < 0f)
			{
				TopNodes.Add(node);
			}
			else
			{
				BottomNodes.Add(node);
			}
		}

		private IEnumerator BlinkRoutine()
		{
			while (true)
			{
				yield return 2f + Calc.Random.NextFloat(1.5f);
				leftEyeVisible = false;
				yield return 0.02f + Calc.Random.NextFloat(0.05f);
				rightEyeVisible = false;
				yield return 0.25f;
				leftEyeVisible = (rightEyeVisible = true);
			}
		}

		public override void Render()
		{
			if (!InView)
			{
				return;
			}
			Vector2 renderPosition = RenderPosition;
			foreach (Node node in nodes)
			{
				if (node.Enabled)
				{
					node.Base.DrawCentered(renderPosition + node.Angle * Scale, Color.White, Scale, node.Rotation);
					node.Overlay.DrawCentered(renderPosition + node.Angle * Scale, Color.White, Scale, 0f - node.Rotation);
				}
			}
			center.DrawCentered(renderPosition, Color.White, Scale, timer);
		}
	}

}
