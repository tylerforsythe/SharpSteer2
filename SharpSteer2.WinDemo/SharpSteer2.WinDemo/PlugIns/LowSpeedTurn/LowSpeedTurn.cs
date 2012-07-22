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

namespace Bnoerj.AI.Steering.LowSpeedTurn
{
	public class LowSpeedTurn : SimpleVehicle
	{
		Trail trail;

		// constructor
		public LowSpeedTurn()
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			// reset vehicle state
			base.Reset();

			// speed along Forward direction.
			Speed = startSpeed;

			// initial position along X axis
			SetPosition(startX, 0, 0);

			// steering force clip magnitude
			MaxForce = 0.3f;

			// velocity  clip magnitude
			MaxSpeed = 1.5f;

			// for next instance: step starting location
			startX += 2;

			// for next instance: step speed
			startSpeed += 0.15f;

			// 15 seconds and 150 points along the trail
			trail = new Trail(15, 150);
		}

		// draw into the scene
		public void Draw()
		{
			Drawing.DrawBasic2dCircularVehicle(this, Color.Gray);
			trail.Draw(Annotation.drawer);
		}

		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			ApplySteeringForce(Steering, elapsedTime);

			// annotation
			annotation.VelocityAcceleration(this);
			trail.Record(currentTime, Position);
		}

		// reset starting positions
		public static void ResetStarts()
		{
			startX = 0;
			startSpeed = 0;
		}

		// constant steering force
		public Vector3 Steering
		{
			get { return new Vector3(1, 0, -1); }
		}

		// for stepping the starting conditions for next vehicle
		static float startX;
		static float startSpeed;
	}
}
