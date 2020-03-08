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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common
{
	public interface IVideoLoader
	{
		// TODO: Not implemented yet
	}

	public interface IVideoStream
	{
		ushort Frames { get; }
		byte Framerate { get; }
		ushort Width { get; }
		ushort Height { get; }
		int CurrentFrame { get; }
		uint[,] FrameData { get; }

		bool HasAudio { get; }
		byte[] AudioData { get; }
		int SampleRate { get; }
		int SampleBits { get; }
		int AudioChannels { get; }

		void AdvanceFrame();
		void Reset();
	}
}
