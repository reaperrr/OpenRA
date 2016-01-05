#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public class ShpSSILoader : ISpriteLoader
	{
		bool isMechCommanderSSI = false;
		int additionalByteOffset;
		uint[] frameOffsets;

		bool IsShpSSI(Stream s)
		{
			var start = s.Position;

			if (s.ReadASCII(4) != "1.10")
			{
				// MechCommander SHPs add 2 bytes of unknown purpose + a copy of frame count (4 bytes) before the string,
				// otherwise they're identical so we can ignore those 6 bytes
				s.Position = 6;
				if (s.ReadASCII(4) != "1.10")
				{
					s.Position = start;
					return false;
				}

				isMechCommanderSSI = true;
			}

			// First 4 bytes after string is the image count.
			var imageCount = s.ReadInt16();
			if (imageCount == 0)
			{
				s.Position = start;
				return false;
			}

			s.Position = start;
			return true;
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames)
		{
			if (!IsShpSSI(s))
			{
				frames = null;
				return false;
			}

			additionalByteOffset = isMechCommanderSSI ? 6 : 0;
			frames = ParseFrames(s);
			return true;
		}

		ShpSSIFrame[] ParseFrames(Stream s)
		{
			var start = s.Position;
			s.Position = 4 + additionalByteOffset;

			// Read frame count and offsets in the file.
			var frameCount = s.ReadUInt32();
			frameOffsets = new uint[frameCount];
			for (var i = 0; i < frameCount; i++)
			{
				frameOffsets[i] = s.ReadUInt32();
				/*framePalette[i] =*/ s.ReadUInt32();
			}

			// Current stream position should be 152 + additionalByteOffset
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			var size = new Size(width, height);

			var frames = new ShpSSIFrame[frameCount];
			for (var i = 0; i < frames.Length; i++)
			{
				s.Position = frameOffsets[i] + additionalByteOffset;
				frames[i] = new ShpSSIFrame(s, size);
			}

			s.Position = start;
			return frames;
		}

		class ShpSSIFrame : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get; private set; }
			public float2 Offset { get; private set; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public readonly uint FileOffset;

			public ShpSSIFrame(Stream s, Size frameSize)
			{
				var xSize = s.ReadUInt16();
				var ySize = s.ReadUInt16();
				// These are the same for all SSI SHPs and can probably be ignored
				var xOffset = s.ReadUInt16();
				var yOffset = s.ReadUInt16();
				var left = s.ReadInt32();
				var top = s.ReadInt32();
				var right = s.ReadInt32();
				var bottom = s.ReadInt32();

				var width = right - left;
				var height = bottom - top;

				// Pad the dimensions to an even number to avoid issues with half-integer offsets
				var dataWidth = width;
				var dataHeight = height;
				if (dataWidth % 2 == 1)
					dataWidth += 1;

				if (dataHeight % 2 == 1)
					dataHeight += 1;

				Offset = new int2(0 + (dataWidth) / 2, 0 + (dataHeight) / 2);
				Size = new Size(dataWidth, dataHeight);
				FrameSize = frameSize;

				s.Position += 11;
				FileOffset = s.ReadUInt32();

				if (FileOffset == 0)
					return;

				// Parse the frame data as we go (but remember to jump back to the header before returning!)
				var start = s.Position;
				s.Position = FileOffset;

				Data = new byte[dataWidth * dataHeight];

				s.Position = start;
			}
		}
	}
}
