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

namespace Bnoerj.AI.Steering.Pedestrian
{
	using ProximityDatabase = IProximityDatabase<IVehicle>;
	using ProximityToken = ITokenForProximityDatabase<IVehicle>;

	public class PedestrianPlugIn : PlugIn
	{
		public PedestrianPlugIn()
			: base()
		{
			crowd = new List<Pedestrian>();
		}

		public override String Name { get { return "Pedestrians"; } }

		public override float SelectionOrderSortKey { get { return 0.02f; } }

		public override void Open()
		{
			// make the database used to accelerate proximity queries
			cyclePD = -1;
			NextPD();

			// create the specified number of Pedestrians
			population = 0;
			for (int i = 0; i < 100; i++) AddPedestrianToCrowd();

			// initialize camera and selectedVehicle
			Pedestrian firstPedestrian = crowd[0];
			Demo.Init3dCamera(firstPedestrian);
			Demo.Camera.Mode = Camera.CameraMode.FixedDistanceOffset;

			Demo.Camera.FixedTarget.X = 15;
            Demo.Camera.FixedTarget.Y = 0;
            Demo.Camera.FixedTarget.Z = 30;

			Demo.Camera.FixedPosition.X = 15;
            Demo.Camera.FixedPosition.Y = 70;
            Demo.Camera.FixedPosition.Z = -70;
		}

		public override void Update(float currentTime, float elapsedTime)
		{
			// update each Pedestrian
			for (int i = 0; i < crowd.Count; i++)
			{
				crowd[i].Update(currentTime, elapsedTime);
			}
		}

		public override void Redraw(float currentTime, float elapsedTime)
		{
			// selected Pedestrian (user can mouse click to select another)
			IVehicle selected = Demo.SelectedVehicle;

			// Pedestrian nearest mouse (to be highlighted)
			IVehicle nearMouse = Demo.VehicleNearestToMouse();

			// update camera
			Demo.UpdateCamera(currentTime, elapsedTime, selected);

			// draw "ground plane"
			if (Demo.SelectedVehicle != null) gridCenter = selected.Position;
			Demo.GridUtility(gridCenter);

			// draw and annotate each Pedestrian
			for (int i = 0; i < crowd.Count; i++) crowd[i].Draw();

			// draw the path they follow and obstacles they avoid
			DrawPathAndObstacles();

			// highlight Pedestrian nearest mouse
			Demo.HighlightVehicleUtility(nearMouse);

			// textual annotation (at the vehicle's screen position)
			SerialNumberAnnotationUtility(selected, nearMouse);

			// textual annotation for selected Pedestrian
			if (Demo.SelectedVehicle != null)//FIXME: && annotation.IsEnabled)
			{
				Color color = new Color((byte)(255.0f * 0.8f), (byte)(255.0f * 0.8f), (byte)(255.0f * 1.0f));
				Vector3 textOffset = new Vector3(0, 0.25f, 0);
				Vector3 textPosition = selected.Position + textOffset;
				Vector3 camPosition = Demo.Camera.Position;
				float camDistance = Vector3.Distance(selected.Position, camPosition);

				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("1: speed: {0:0.00}\n", selected.Speed);
				sb.AppendFormat("2: cam dist: {0:0.0}\n", camDistance);
				Drawing.Draw2dTextAt3dLocation(sb.ToString(), textPosition, color);
			}

			// display status in the upper left corner of the window
			StringBuilder status = new StringBuilder();
			status.AppendFormat("[F1/F2] Crowd size: {0}\n", population);
			status.Append("[F3] PD type: ");
			switch (cyclePD)
			{
			case 0: status.Append("LQ bin lattice"); break;
			case 1: status.Append("brute force"); break;
			}
			status.Append("\n[F4] ");
			if (Globals.UseDirectedPathFollowing)
				status.Append("Directed path following.");
			else
				status.Append("Stay on the path.");
			status.Append("\n[F5] Wander: ");
			if (Globals.WanderSwitch) status.Append("yes");
			else status.Append("no");
			status.Append("\n");
			Vector3 screenLocation = new Vector3(15, 50, 0);
			Drawing.Draw2dTextAt2dLocation(status.ToString(), screenLocation, Color.LightGray);
		}

		public void SerialNumberAnnotationUtility(IVehicle selected, IVehicle nearMouse)
		{
			// display a Pedestrian's serial number as a text label near its
			// screen position when it is near the selected vehicle or mouse.
			if (selected != null)//FIXME: && IsAnnotationEnabled)
			{
				for (int i = 0; i < crowd.Count; i++)
				{
					IVehicle vehicle = crowd[i];
					const float nearDistance = 6;
					Vector3 vp = vehicle.Position;
					//Vector3 np = nearMouse.Position;
					if ((Vector3.Distance(vp, selected.Position) < nearDistance)/* ||
						(nearMouse != null && (Vector3.Distance(vp, np) < nearDistance))*/)
					{
						String sn = String.Format("#{0}", ((Pedestrian)vehicle).SerialNumber);
						Color textColor = new Color((byte)(255.0f * 0.8f), (byte)(255.0f * 1), (byte)(255.0f * 0.8f));
						Vector3 textOffset = new Vector3(0, 0.25f, 0);
						Vector3 textPos = vehicle.Position + textOffset;
						Drawing.Draw2dTextAt3dLocation(sn, textPos, textColor);
					}
				}
			}
		}

		public void DrawPathAndObstacles()
		{
			// draw a line along each segment of path
			PolylinePathway path = Globals.GetTestPath();
			for (int i = 0; i < path.pointCount; i++)
				if (i > 0) Drawing.DrawLine(path.points[i], path.points[i - 1], Color.Red);

			// draw obstacles
			Drawing.DrawXZCircle(Globals.Obstacle1.Radius, Globals.Obstacle1.Center, Color.White, 40);
			Drawing.DrawXZCircle(Globals.Obstacle2.Radius, Globals.Obstacle2.Center, Color.White, 40);
		}

		public override void Close()
		{
			// delete all Pedestrians
			while (population > 0) RemovePedestrianFromCrowd();
		}

		public override void Reset()
		{
			// reset each Pedestrian
			for (int i = 0; i < crowd.Count; i++) crowd[i].Reset();

			// reset camera position
			Demo.Position2dCamera(Demo.SelectedVehicle);

			// make camera jump immediately to new position
			Demo.Camera.DoNotSmoothNextMove();
		}

		public override void HandleFunctionKeys(Keys key)
		{
			switch (key)
			{
			case Keys.F1: AddPedestrianToCrowd(); break;
			case Keys.F2: RemovePedestrianFromCrowd(); break;
			case Keys.F3: NextPD(); break;
			case Keys.F4: Globals.UseDirectedPathFollowing = !Globals.UseDirectedPathFollowing; break;
			case Keys.F5: Globals.WanderSwitch = !Globals.WanderSwitch; break;
			}
		}

		public override void PrintMiniHelpForFunctionKeys()
		{
#if TODO
			std::ostringstream message;
			message << "Function keys handled by ";
			message << '"' << name() << '"' << ':' << std::ends;
			Demo.printMessage (message);
			Demo.printMessage (message);
			Demo.printMessage ("  F1     add a pedestrian to the crowd.");
			Demo.printMessage ("  F2     remove a pedestrian from crowd.");
			Demo.printMessage ("  F3     use next proximity database.");
			Demo.printMessage ("  F4     toggle directed path follow.");
			Demo.printMessage ("  F5     toggle wander component on/off.");
			Demo.printMessage ("");
#endif
		}

		void AddPedestrianToCrowd()
		{
			population++;
			Pedestrian pedestrian = new Pedestrian(pd);
			crowd.Add(pedestrian);
			if (population == 1) Demo.SelectedVehicle = pedestrian;
		}

		void RemovePedestrianFromCrowd()
		{
			if (population > 0)
			{
				// save pointer to last pedestrian, then remove it from the crowd
				population--;
				Pedestrian pedestrian = crowd[population];
				crowd.RemoveAt(population);

				// if it is OpenSteerDemo's selected vehicle, unselect it
				if (pedestrian == Demo.SelectedVehicle)
					Demo.SelectedVehicle = null;

				// delete the Pedestrian
				pedestrian = null;
			}
		}

		// for purposes of demonstration, allow cycling through various
		// types of proximity databases.  this routine is called when the
		// OpenSteerDemo user pushes a function key.
		void NextPD()
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
					float div = 20.0f;
					Vector3 divisions = new Vector3(div, 1.0f, div);
					float diameter = 80.0f; //XXX need better way to get this
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
			for (int i = 0; i < crowd.Count; i++) crowd[i].NewPD(pd);

			// delete old PD (if any)
			oldPD = null;
		}

		public override List<IVehicle> Vehicles
		{
			get { return crowd.ConvertAll<IVehicle>(delegate(Pedestrian p) { return (IVehicle)p; }); }
		}

		// crowd: a group (STL vector) of all Pedestrians
		List<Pedestrian> crowd;

		Vector3 gridCenter;

		// pointer to database used to accelerate proximity queries
		ProximityDatabase pd;

		// keep track of current flock size
		int population;

		// which of the various proximity databases is currently in use
		int cyclePD;
	}
}
