// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// Copyright (C) 2007 Michael Coles <michael@digini.com>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	public interface IDraw
	{
        void Line(Vector3 startPoint, Vector3 endPoint, Color color);
        void LineAlpha(Vector3 startPoint, Vector3 endPoint, Color color, float alpha);
        void CircleOrDisk(float radius, Vector3 axis, Vector3 center, Color color, int segments, bool filled, bool in3d);
	}
}
