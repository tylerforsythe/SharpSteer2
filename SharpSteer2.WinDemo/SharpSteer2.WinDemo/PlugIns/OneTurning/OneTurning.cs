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
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Bnoerj.AI.Steering.OneTurning
{
	public class OneTurning : SimpleVehicle
	{
		Trail trail;

		// constructor
		public OneTurning()
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset(); // reset the vehicle 
			Speed = 1.5f;         // speed along Forward direction.
			MaxForce = 0.3f;      // steering force is clipped to this magnitude
			MaxSpeed = 5;         // velocity is clipped to this magnitude
			trail = new Trail();
			trail.Clear();    // prevent long streaks due to teleportation 
		}

		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			ApplySteeringForce(new Vector3(-2, 0, -3), elapsedTime);
			annotation.VelocityAcceleration(this);
			trail.Record(currentTime, Position);
		}

		// draw this character/vehicle into the scene
		public void Draw()
		{
			Drawing.DrawBasic2dCircularVehicle(this, Color.Gray);
			trail.Draw(Annotation.drawer);
		}
	}
}
