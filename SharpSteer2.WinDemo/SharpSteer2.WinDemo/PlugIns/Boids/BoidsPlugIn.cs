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
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Bnoerj.AI.Steering.Boids
{
	using ProximityDatabase = IProximityDatabase<IVehicle>;
	using ProximityToken = ITokenForProximityDatabase<IVehicle>;
	using SOG = List<SphericalObstacle>;  // spherical obstacle group

	public class BoidsPlugIn : PlugIn
	{
		public BoidsPlugIn()
			: base()
		{
			flock = new List<Boid>();
		}

		public override String Name { get { return "Boids"; } }

		public override float SelectionOrderSortKey
		{
			get { return -0.03f; }
		}

		public override void Open()
		{
			// make the database used to accelerate proximity queries
			cyclePD = -1;
			NextPD();

			// make default-sized flock
			population = 0;
			for (int i = 0; i < 200; i++) AddBoidToFlock();

			// initialize camera
			Bnoerj.AI.Steering.Demo.Init3dCamera(Bnoerj.AI.Steering.Demo.SelectedVehicle);
			Bnoerj.AI.Steering.Demo.Camera.Mode = Camera.CameraMode.Fixed;
			Bnoerj.AI.Steering.Demo.Camera.FixedDistanceDistance = Bnoerj.AI.Steering.Demo.CameraTargetDistance;
			Bnoerj.AI.Steering.Demo.Camera.FixedDistanceVerticalOffset = 0;
			Bnoerj.AI.Steering.Demo.Camera.LookDownDistance = 20;
			Bnoerj.AI.Steering.Demo.Camera.AimLeadTime = 0.5f;
			Bnoerj.AI.Steering.Demo.Camera.PovOffset.X =0;
            Bnoerj.AI.Steering.Demo.Camera.PovOffset.Y = 0.5f;
            Bnoerj.AI.Steering.Demo.Camera.PovOffset.Z = -2;

			Boid.InitializeObstacles();
		}

		public override void Update(float currentTime, float elapsedTime)
		{
			// update flock simulation for each boid
			for (int i = 0; i < flock.Count; i++)
			{
				flock[i].Update(currentTime, elapsedTime);
			}
		}

		public override void Redraw(float currentTime, float elapsedTime)
		{
			// selected vehicle (user can mouse click to select another)
			IVehicle selected = Demo.SelectedVehicle;

			// vehicle nearest mouse (to be highlighted)
			IVehicle nearMouse = null;// Demo.vehicleNearestToMouse();

			// update camera
			Demo.UpdateCamera(currentTime, elapsedTime, selected);

			DrawObstacles();

			// draw each boid in flock
			for (int i = 0; i < flock.Count; i++) flock[i].Draw();

			// highlight vehicle nearest mouse
			Demo.DrawCircleHighlightOnVehicle(nearMouse, 1, Color.LightGray);

			// highlight selected vehicle
			Demo.DrawCircleHighlightOnVehicle(selected, 1, Color.Gray);

			// display status in the upper left corner of the window
			StringBuilder status = new StringBuilder();
			status.AppendFormat("[F1/F2] {0} boids", population);
			status.Append("\n[F3]    PD type: ");
			switch (cyclePD)
			{
			case 0: status.Append("LQ bin lattice"); break;
			case 1: status.Append("brute force"); break;
			}
			status.Append("\n[F4]    Boundary: ");
			switch (Boid.boundaryCondition)
			{
			case 0: status.Append("steer back when outside"); break;
			case 1: status.Append("wrap around (teleport)"); break;
			}
			Vector3 screenLocation = new Vector3(15, 50, 0);
			Drawing.Draw2dTextAt2dLocation(status.ToString(), screenLocation, Color.LightGray);
		}

		public override void Close()
		{
			// delete each member of the flock
			while (population > 0) RemoveBoidFromFlock();

			// delete the proximity database
			pd = null;
		}

		public override void Reset()
		{
			// reset each boid in flock
			for (int i = 0; i < flock.Count; i++) flock[i].Reset();

			// reset camera position
			Demo.Position3dCamera(Demo.SelectedVehicle);

			// make camera jump immediately to new position
			Demo.Camera.DoNotSmoothNextMove();
		}

		// for purposes of demonstration, allow cycling through various
		// types of proximity databases.  this routine is called when the
		// Demo user pushes a function key.
		public void NextPD()
		{
			// save pointer to old PD
			ProximityDatabase oldPD = pd;

			// allocate new PD
			const int totalPD = 2;
			switch (cyclePD = (cyclePD + 1) % totalPD)
			{
			case 0:
				{
					Vector3 center = Vector3.Zero;
					const float div = 10.0f;
					Vector3 divisions = new Vector3(div, div, div);
					float diameter = Boid.worldRadius * 1.1f * 2;
					Vector3 dimensions = new Vector3(diameter, diameter, diameter);
					pd = new LocalityQueryProximityDatabase<IVehicle>(center, dimensions, divisions);
					break;
				}
			case 1:
				{
					pd = new BruteForceProximityDatabase<IVehicle>();
					break;
				}
			}

			// switch each boid to new PD
			for (int i = 0; i < flock.Count; i++) flock[i].NewPD(pd);

			// delete old PD (if any)
			oldPD = null;
		}

		public override void HandleFunctionKeys(Keys key)
		{
			switch (key)
			{
			case Keys.F1: AddBoidToFlock(); break;
			case Keys.F2: RemoveBoidFromFlock(); break;
			case Keys.F3: NextPD(); break;
			case Keys.F4: Boid.NextBoundaryCondition(); break;
			}
		}

		public override void PrintMiniHelpForFunctionKeys()
		{
#if IGNORED
        std.ostringstream message;
        message << "Function keys handled by ";
        message << '"' << name() << '"' << ':' << std.ends;
        Demo.printMessage (message);
        Demo.printMessage ("  F1     add a boid to the flock.");
        Demo.printMessage ("  F2     remove a boid from the flock.");
        Demo.printMessage ("  F3     use next proximity database.");
        Demo.printMessage ("  F4     next flock boundary condition.");
        Demo.printMessage ("");
#endif
		}

		public void AddBoidToFlock()
		{
			population++;
			Boid boid = new Boid(pd);
			flock.Add(boid);
			if (population == 1) Demo.SelectedVehicle = boid;
		}

		public void RemoveBoidFromFlock()
		{
			if (population > 0)
			{
				// save a pointer to the last boid, then remove it from the flock
				population--;
				Boid boid = flock[population];
				flock.RemoveAt(population);

				// if it is Demo's selected vehicle, unselect it
				if (boid == Demo.SelectedVehicle)
					Demo.SelectedVehicle = null;

				// delete the Boid
				boid = null;
			}
		}

		// return an AVGroup containing each boid of the flock
		public override List<IVehicle> Vehicles
		{
			get { return flock.ConvertAll<IVehicle>(delegate(Boid v) { return (IVehicle)v; }); }
		}

		// flock: a group (STL vector) of pointers to all boids
		public List<Boid> flock;

		// pointer to database used to accelerate proximity queries
		public ProximityDatabase pd;

		// keep track of current flock size
		public int population;

		// which of the various proximity databases is currently in use
		public int cyclePD;

		public void DrawObstacles()
		{
			//Color color = new Color((byte)(255.0f * 0.8f), (byte)(255.0f * 0.6f), (byte)(255.0f * 0.4f));
			SOG allSO = Boid.AllObstacles;
			for (int so = 0; so < allSO.Count; so++)
			{
				//Drawing.DrawBasic3dSphere(allSO[so].Center, allSO[so].Radius, Color.Red);
				Drawing.Draw3dCircleOrDisk(allSO[so].Radius, allSO[so].Center, Vector3.UnitY, Color.Red, 10, true);
				Drawing.Draw3dCircleOrDisk(allSO[so].Radius, allSO[so].Center, Vector3.UnitX, Color.Red, 10, true);
				Drawing.Draw3dCircleOrDisk(allSO[so].Radius, allSO[so].Center, Vector3.UnitZ, Color.Red, 10, true);
			}
		}
	}
}
