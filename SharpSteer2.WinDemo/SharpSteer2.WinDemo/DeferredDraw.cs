// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// Copyright (C) 2007 Michael Coles <michael@digini.com>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Bnoerj.AI.Steering
{
	public class DeferredLine
	{
		static DeferredLine()
		{
			deferredLineArray = new DeferredLine[size];
			for (int i = 0; i < size; i++)
			{
				deferredLineArray[i] = new DeferredLine();
			}
		}

		public static void AddToBuffer(Vector3 s, Vector3 e, Color c)
		{
			if (index < size)
			{
				deferredLineArray[index].startPoint = s;
				deferredLineArray[index].endPoint = e;
				deferredLineArray[index].color = c;
				index++;
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("overflow in deferredDrawLine buffer");
			}
		}

		public static void DrawAll()
		{
			// draw all lines in the buffer
			for (int i = 0; i < index; i++)
			{
				DeferredLine dl = deferredLineArray[i];
				Drawing.iDrawLine(dl.startPoint, dl.endPoint, dl.color);
			}

			// reset buffer index
			index = 0;
		}

		Vector3 startPoint;
		Vector3 endPoint;
		Color color;

		static int index = 0;
		const int size = 3000;
		static DeferredLine[] deferredLineArray;
	}

	public class DeferredCircle
	{
		static DeferredCircle()
		{
			deferredCircleArray = new DeferredCircle[size];
			for (int i = 0; i < size; i++)
			{
				deferredCircleArray[i] = new DeferredCircle();
			}
		}

		public static void AddToBuffer(float radius, Vector3 axis, Vector3 center, Color color, int segments, bool filled, bool in3d)
		{
			if (index < size)
			{
				deferredCircleArray[index].radius = radius;
				deferredCircleArray[index].axis = axis;
				deferredCircleArray[index].center = center;
				deferredCircleArray[index].color = color;
				deferredCircleArray[index].segments = segments;
				deferredCircleArray[index].filled = filled;
				deferredCircleArray[index].in3d = in3d;
				index++;
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("overflow in deferredDrawCircle buffer");
			}
		}

		public static void DrawAll()
		{
			// draw all circles in the buffer
			for (int i = 0; i < index; i++)
			{
				DeferredCircle dc = deferredCircleArray[i];
				Drawing.DrawCircleOrDisk(dc.radius, dc.axis, dc.center, dc.color, dc.segments, dc.filled, dc.in3d);
			}

			// reset buffer index
			index = 0;
		}

		float radius;
		Vector3 axis;
		Vector3 center;
		Color color;
		int segments;
		bool filled;
		bool in3d;

		static int index = 0;
		const int size = 500;
		static DeferredCircle[] deferredCircleArray;
	}
}
