#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Traits.Render
{
	[Desc("Display the time remaining until the next cash is given by actor's CashTrickler trait.")]
	class CashTricklerBarInfo : ITraitInfo
	{
		[Desc("Defines to which players the bar is to be shown.")]
		public readonly Stance DisplayStances = Stance.Ally;

		public readonly Color Color = Color.Magenta;

		public object Create(ActorInitializer init) { return new CashTricklerBar(init.Self, this); }
	}

	class CashTricklerBar : ISelectionBar
	{
		readonly Actor self;
		readonly CashTricklerBarInfo info;
		readonly IEnumerable<CashTrickler> enabledCashTricklers;

		public CashTricklerBar(Actor self, CashTricklerBarInfo info)
		{
			this.self = self;
			this.info = info;
			enabledCashTricklers = self.TraitsImplementing<CashTrickler>().ToArray().Where(ct => !ct.IsTraitDisabled);
		}

		float ISelectionBar.GetValue()
		{
			if (enabledCashTricklers == null)
				return 0;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (viewer != null && !info.DisplayStances.HasStance(self.Owner.Stances[viewer]))
				return 0;

			var complete = enabledCashTricklers.Max(ct => (float)ct.Ticks / ct.Info.Interval);
			return 1 - complete;
		}

		Color ISelectionBar.GetColor() { return info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}