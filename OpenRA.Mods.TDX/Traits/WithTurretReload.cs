#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.TDX.Traits
{
	[Desc("Renders ammo-dependent turret graphics for units with the Turreted trait.")]
	public class WithTurretReloadInfo : UpgradableTraitInfo, ITraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>,
		Requires<TurretedInfo>, Requires<IBodyOrientationInfo>, Requires<AmmoPoolInfo>, Requires<ArmamentInfo>
	{
		[Desc("Sequence name to use.")]
		public readonly string Sequence = "turret";

		[Desc("Sequence name to use when prepared to fire")]
		public readonly string AimSequence = null;

		[Desc("AmmoPool to use for ammo-dependent sequences.")]
		public readonly string AmmoPoolName = null;

		[Desc("How many reload stages does this turret have. Defaults to AmmoPool's Ammo.",
		"Adds current reload stage to Sequence as suffix when a matching AmmoPool is present.")]
		public readonly int ReloadStages = -1;

		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		[Desc("Render recoil")]
		public readonly bool Recoils = true;

		public object Create(ActorInitializer init) { return new WithTurretReload(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			var body = init.Actor.Traits.Get<BodyOrientationInfo>();
			var t = init.Actor.Traits.WithInterface<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var ifacing = init.Actor.Traits.GetOrDefault<IFacingInfo>();
			var bodyFacing = ifacing != null ? init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : ifacing.GetInitialFacing() : 0;
			var turretFacing = init.Contains<TurretFacingInit>() ? init.Get<TurretFacingInit, int>() : t.InitialFacing;

			var anim = new Animation(init.World, image, () => turretFacing);
			anim.Play(Sequence);

			var orientation = body.QuantizeOrientation(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(bodyFacing)), facings);
			var offset = body.LocalToWorld(t.Offset.Rotate(orientation));
			yield return new SpriteActorPreview(anim, offset, offset.Y + offset.Z + 1, p, rs.Scale);
		}
	}

	public class WithTurretReload : UpgradableTrait<WithTurretReloadInfo>, ITick
	{
		readonly AmmoPool ammoPool;
		readonly int reloadStages;
		string sequence;
		string ammoSuffix = null;
		RenderSprites rs;
		IBodyOrientation body;
		AttackBase ab;
		Turreted t;
		IEnumerable<Armament> arms;
		Animation anim;

		public WithTurretReload(Actor self, WithTurretReloadInfo info)
			: base(info)
		{
			rs = self.Trait<RenderSprites>();
			body = self.Trait<IBodyOrientation>();

			ab = self.TraitOrDefault<AttackBase>();
			t = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == info.Turret);
			arms = self.TraitsImplementing<Armament>()
				.Where(w => w.Info.Turret == info.Turret);
			ammoPool = self.TraitsImplementing<AmmoPool>()
				.First(a => a.Info.Name == info.AmmoPoolName);

			sequence = info.Sequence;
			reloadStages = info.ReloadStages;

			var initialAmmo = ammoPool.Info.InitialAmmo;
			var ammo = ammoPool.Info.Ammo;
			var initialAmmoStage = initialAmmo >= 0 && initialAmmo != ammo ? initialAmmo : ammo;

			if (ammoPool != null && reloadStages < 0)
				ammoSuffix = initialAmmoStage.ToString();
			if (ammoPool != null && reloadStages >= 0)
				ammoSuffix = (initialAmmoStage * reloadStages / ammo).ToString();

			anim = new Animation(self.World, rs.GetImage(self), () => t.TurretFacing);
			anim.Play(sequence + ammoSuffix);
			rs.Add("turret_{0}_{1}".F(info.Turret, sequence + ammoSuffix), new AnimationWithOffset(
				anim, () => TurretOffset(self), () => IsTraitDisabled, () => false, p => ZOffsetFromCenter(self, p, 1)));

			// Restrict turret facings to match the sprite
			t.QuantizedFacings = anim.CurrentSequence.Facings;
		}

		WVec TurretOffset(Actor self)
		{
			if (!Info.Recoils)
				return t.Position(self);

			var recoil = arms.Aggregate(WRange.Zero, (a, b) => a + b.Recoil);
			var localOffset = new WVec(-recoil, WRange.Zero, WRange.Zero);
			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			var turretOrientation = body.QuantizeOrientation(self, t.LocalOrientation(self));
			return t.Position(self) + body.LocalToWorld(localOffset.Rotate(turretOrientation).Rotate(bodyOrientation));
		}

		public void Tick(Actor self)
		{
			if (Info.AimSequence != null)
				sequence = ab.IsAttacking ? Info.AimSequence : sequence;

			if (ammoPool != null && reloadStages < 0)
				ammoSuffix = ammoPool.GetAmmoCount().ToString();
			if (ammoPool != null && reloadStages >= 0)
				ammoSuffix = (ammoPool.GetAmmoCount() * Info.ReloadStages / ammoPool.Info.Ammo).ToString();

			anim.ReplaceAnim(sequence + ammoSuffix);
		}

		public static int ZOffsetFromCenter(Actor self, WPos pos, int offset)
		{
			var delta = self.CenterPosition - pos;
			return delta.Y + delta.Z + offset;
		}
	}
}
