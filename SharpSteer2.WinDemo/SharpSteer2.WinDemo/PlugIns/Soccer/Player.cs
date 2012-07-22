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

namespace Bnoerj.AI.Steering.Soccer
{
	public class Player : SimpleVehicle
	{
		Trail trail;

		// constructor
		public Player(List<Player> others, List<Player> allplayers, Ball ball, bool isTeamA, int id)
		{
			m_others = others;
			m_AllPlayers = allplayers;
			m_Ball = ball;
			b_ImTeamA = isTeamA;
			m_MyID = id;

			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset(); // reset the vehicle 
			Speed = 0.0f;         // speed along Forward direction.
			MaxForce = 3000.7f;      // steering force is clipped to this magnitude
			MaxSpeed = 10;         // velocity is clipped to this magnitude

			// Place me on my part of the field, looking at oponnents goal
			SetPosition(b_ImTeamA ? Utilities.Random() * 20 : -Utilities.Random() * 20, 0, (Utilities.Random() - 0.5f) * 20);
			if (m_MyID < 9)
			{
				if (b_ImTeamA)
					Position = (Globals.PlayerPosition[m_MyID]);
				else
					Position = (new Vector3(-Globals.PlayerPosition[m_MyID].X, Globals.PlayerPosition[m_MyID].Y, Globals.PlayerPosition[m_MyID].Z));
			}
			m_home = Position;

			if (trail == null) trail = new Trail(10, 60);
			trail.Clear();    // prevent long streaks due to teleportation 
		}

		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			// if I hit the ball, kick it.
			float distToBall = Vector3.Distance(Position, m_Ball.Position);
			float sumOfRadii = Radius + m_Ball.Radius;
			if (distToBall < sumOfRadii)
				m_Ball.Kick((m_Ball.Position - Position) * 50, elapsedTime);

			// otherwise consider avoiding collisions with others
			Vector3 collisionAvoidance = SteerToAvoidNeighbors(1, m_AllPlayers);
			if (collisionAvoidance != Vector3.Zero)
				ApplySteeringForce(collisionAvoidance, elapsedTime);
			else
			{
				float distHomeToBall = Vector3.Distance(m_home, m_Ball.Position);
				if (distHomeToBall < 12)
				{
					// go for ball if I'm on the 'right' side of the ball
					if (b_ImTeamA ? Position.X > m_Ball.Position.X : Position.X < m_Ball.Position.X)
					{
						Vector3 seekTarget = xxxSteerForSeek(m_Ball.Position);
						ApplySteeringForce(seekTarget, elapsedTime);
					}
					else
					{
						if (distHomeToBall < 12)
						{
							float Z = m_Ball.Position.Z - Position.Z > 0 ? -1.0f : 1.0f;
							Vector3 behindBall = m_Ball.Position + (b_ImTeamA ? new Vector3(2, 0, Z) : new Vector3(-2, 0, Z));
							Vector3 behindBallForce = xxxSteerForSeek(behindBall);
							annotation.Line(Position, behindBall, Color.Green);
							Vector3 evadeTarget = xxxSteerForFlee(m_Ball.Position);
							ApplySteeringForce(behindBallForce * 10 + evadeTarget, elapsedTime);
						}
					}
				}
				else	// Go home
				{
					Vector3 seekTarget = xxxSteerForSeek(m_home);
					Vector3 seekHome = xxxSteerForSeek(m_home);
					ApplySteeringForce(seekTarget + seekHome, elapsedTime);
				}

			}
		}

		// draw this character/vehicle into the scene
		public void Draw()
		{
			Drawing.DrawBasic2dCircularVehicle(this, b_ImTeamA ? Color.Red : Color.Blue);
			trail.Draw(Annotation.drawer);
		}

		// per-instance reference to its group
		List<Player> m_others;
		List<Player> m_AllPlayers;
		Ball m_Ball;
		bool b_ImTeamA;
		int m_MyID;
		Vector3 m_home;
	}
}
