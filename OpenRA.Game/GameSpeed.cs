#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA
{
	public class GameSpeed
	{
		public readonly string Name;
		public readonly int Timestep;
		public readonly int OrderLatency;
	}

	public class GameSpeeds : IGlobalModData
	{
		[FieldLoader.LoadUsing(nameof(LoadSpeeds))]
		public readonly Dictionary<string, GameSpeed> Speeds;

		static object LoadSpeeds(MiniYaml y)
		{
			var ret = new Dictionary<string, GameSpeed>();
			var defaultExists = false;
			foreach (var node in y.Nodes)
			{
				var gs = FieldLoader.Load<GameSpeed>(node.Value);
				if (string.IsNullOrEmpty(gs.Name))
					throw new YamlException("At least one gamespeed doesn't define a Name!");
				else if (gs.Timestep < 1)
					throw new YamlException("Gamespeed {0} doesn't define a valid Timestep!".F(gs.Name));
				else if (gs.OrderLatency < 1)
					throw new YamlException("Gamespeed {0} doesn't define a valid OrderLatency!".F(gs.Name));

				if (node.Key == "default")
					defaultExists = true;

				ret.Add(node.Key, gs);
			}

			if (!defaultExists)
				throw new YamlException("Mod doesn't define a 'default:' gamespeed! (Note: Name can still be anything).");

			return ret;
		}
	}
}
