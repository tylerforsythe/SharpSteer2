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
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Bnoerj.AI.Steering.Ctf
{
	using SOG = List<SphericalObstacle>;  // spherical obstacle group

	// Capture the Flag   (a portion of the traditional game)
	//
	// The "Capture the Flag" sample steering problem, proposed by Marcin
	// Chady of the Working Group on Steering of the IGDA's AI Interface
	// Standards Committee (http://www.igda.org/Committees/ai.htm) in this
	// message (http://sourceforge.net/forum/message.php?msg_id=1642243):
	//
	//     "An agent is trying to reach a physical location while trying
	//     to stay clear of a group of enemies who are actively seeking
	//     him. The environment is littered with obstacles, so collision
	//     avoidance is also necessary."
	//
	// Note that the enemies do not make use of their knowledge of the 
	// seeker's goal by "guarding" it.  
	//
	// XXX hmm, rename them "attacker" and "defender"?
	//
	// 08-12-02 cwr: created 
	
	public class CtfPlugIn : PlugIn
	{
		public CtfPlugIn()
			: base()
		{
			all = new List<CtfBase>();
		}

		public override String Name { get { return "Capture the Flag"; } }

		public override float SelectionOrderSortKey { get { return 0.01f; } }

		public override void Open()
		{
			// create the seeker ("hero"/"attacker")
			Globals.CtfSeeker = new CtfSeeker();
			all.Add(Globals.CtfSeeker);

			// create the specified number of enemies, 
			// storing pointers to them in an array.
			for (int i = 0; i < Globals.CtfEnemyCount; i++)
			{
				Globals.CtfEnemies[i] = new CtfEnemy();
				all.Add(Globals.CtfEnemies[i]);
			}

			// initialize camera
			Demo.Init2dCamera(Globals.CtfSeeker);
			Demo.Camera.Mode = Camera.CameraMode.FixedDistanceOffset;
			Demo.Camera.FixedTarget = Vector3.Zero;
            Demo.Camera.FixedTarget.X = 15;
			Demo.Camera.FixedPosition.X = 80;
            Demo.Camera.FixedPosition.Y = 60;
            Demo.Camera.FixedPosition.Z = 0;

			CtfBase.InitializeObstacles();
		}

		public override void Update(float currentTime, float elapsedTime)
		{
			// update the seeker
			Globals.CtfSeeker.Update(currentTime, elapsedTime);

			// update each enemy
			for (int i = 0; i < Globals.CtfEnemyCount; i++)
			{
				Globals.CtfEnemies[i].Update(currentTime, elapsedTime);
			}
		}

		public override void Redraw(float currentTime, float elapsedTime)
		{
			// selected vehicle (user can mouse click to select another)
			IVehicle selected = Demo.SelectedVehicle;

			// vehicle nearest mouse (to be highlighted)
			IVehicle nearMouse = null;//FIXME: Demo.vehicleNearestToMouse ();

			// update camera
			Demo.UpdateCamera(currentTime, elapsedTime, selected);

			// draw "ground plane" centered between base and selected vehicle
			Vector3 goalOffset = Globals.HomeBaseCenter - Demo.Camera.Position;
			Vector3 goalDirection = goalOffset;
            goalDirection.Normalize();
			Vector3 cameraForward = Demo.Camera.xxxls().Forward;
            float goalDot = Vector3.Dot(cameraForward, goalDirection);
			float blend = Utilities.RemapIntervalClip(goalDot, 1, 0, 0.5f, 0);
			Vector3 gridCenter = Utilities.Interpolate(blend, selected.Position, Globals.HomeBaseCenter);
			Demo.GridUtility(gridCenter);

			// draw the seeker, obstacles and home base
			Globals.CtfSeeker.Draw();
			DrawObstacles();
			DrawHomeBase();

			// draw each enemy
			for (int i = 0; i < Globals.CtfEnemyCount; i++) Globals.CtfEnemies[i].Draw();

			// highlight vehicle nearest mouse
			Demo.HighlightVehicleUtility(nearMouse);
		}

		public override void Close()
		{
			// delete seeker
			Globals.CtfSeeker = null;

			// delete each enemy
			for (int i = 0; i < Globals.CtfEnemyCount; i++)
			{
				Globals.CtfEnemies[i] = null;
			}

			// clear the group of all vehicles
			all.Clear();
		}

		public override void Reset()
		{
			// count resets
			Globals.ResetCount++;

			// reset the seeker ("hero"/"attacker") and enemies
			Globals.CtfSeeker.Reset();
			for (int i = 0; i < Globals.CtfEnemyCount; i++) Globals.CtfEnemies[i].Reset();

			// reset camera position
			Demo.Position2dCamera(Globals.CtfSeeker);

			// make camera jump immediately to new position
			Demo.Camera.DoNotSmoothNextMove();
		}

		public override void HandleFunctionKeys(Keys key)
		{
			switch (key)
			{
			case Keys.F1: CtfBase.AddOneObstacle(); break;
			case Keys.F2: CtfBase.RemoveOneObstacle(); break;
			}
		}

		public override void PrintMiniHelpForFunctionKeys()
		{
#if TODO
			std.ostringstream message;
			message << "Function keys handled by ";
			message << '"' << name() << '"' << ':' << std.ends;
			Demo.printMessage (message);
			Demo.printMessage ("  F1     add one obstacle.");
			Demo.printMessage ("  F2     remove one obstacle.");
			Demo.printMessage ("");
#endif
		}

		public override List<IVehicle> Vehicles
		{
			get { return all.ConvertAll<IVehicle>(delegate(CtfBase v) { return (IVehicle)v; }); }
		}

		public void DrawHomeBase()
		{
			Vector3 up = new Vector3(0, 0.01f, 0);
			Color atColor = new Color((byte)(255.0f * 0.3f), (byte)(255.0f * 0.3f), (byte)(255.0f * 0.5f));
			Color noColor = Color.Gray;
			bool reached = Globals.CtfSeeker.State == CtfSeeker.SeekerState.AtGoal;
			Color baseColor = (reached ? atColor : noColor);
			Drawing.DrawXZDisk(Globals.HomeBaseRadius, Globals.HomeBaseCenter, baseColor, 40);
			Drawing.DrawXZDisk(Globals.HomeBaseRadius / 15, Globals.HomeBaseCenter + up, Color.Black, 20);
		}

		public void DrawObstacles()
		{
			Color color = new Color((byte)(255.0f * 0.8f), (byte)(255.0f * 0.6f), (byte)(255.0f * 0.4f));
			SOG allSO = CtfBase.AllObstacles;
			for (int so = 0; so < allSO.Count; so++)
			{
				Drawing.DrawXZCircle(allSO[so].Radius, allSO[so].Center, color, 40);
			}
		}

		// a group (STL vector) of all vehicles in the PlugIn
		List<CtfBase> all;
	}
}
