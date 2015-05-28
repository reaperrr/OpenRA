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
	public class WithTurretReloadInfo : WithTurretInfo, Requires<AmmoPoolInfo>, Requires<ArmamentInfo>
	{
		[Desc("AmmoPool to use for ammo-dependent sequences.")]
		public readonly string AmmoPoolName = null;

		[Desc("How many reload stages does this turret have. Defaults to AmmoPool's Ammo.",
		"Adds current reload stage to Sequence as suffix when a matching AmmoPool is present.")]
		public readonly int ReloadStages = -1;

		public override object Create(ActorInitializer init) { return new WithTurretReload(init.Self, this); }
	}

	public class WithTurretReload : WithTurret
	{
		readonly int reloadStages;
		readonly AmmoPool ammoPool;
		string sequence;
		string ammoSuffix = null;

		public WithTurretReload(Actor self, WithTurretReloadInfo info)
			: base(self, info)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>()
				.First(a => a.Info.Name == info.AmmoPoolName);

			sequence = Info.Sequence;
			reloadStages = info.ReloadStages;

			var initialAmmo = ammoPool.Info.InitialAmmo;
			var ammo = ammoPool.Info.Ammo;
			var initialAmmoStage = initialAmmo >= 0 && initialAmmo != ammo ? initialAmmo : ammo;

			if (ammoPool != null && reloadStages < 0)
				ammoSuffix = initialAmmoStage.ToString();
			if (ammoPool != null && reloadStages >= 0)
				ammoSuffix = (initialAmmoStage * reloadStages / ammo).ToString();
		}

		public override void Tick(Actor self)
		{
			if (Info.AimSequence != null)
				sequence = ab.IsAttacking ? Info.AimSequence : sequence;

			if (ammoPool != null && reloadStages < 0)
				ammoSuffix = ammoPool.GetAmmoCount().ToString();
			if (ammoPool != null && reloadStages >= 0)
				ammoSuffix = (ammoPool.GetAmmoCount() * reloadStages / ammoPool.Info.Ammo).ToString();

			anim.ReplaceAnim(sequence + ammoSuffix);
		}
	}
}
