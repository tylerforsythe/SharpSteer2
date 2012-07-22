// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering.MultiplePursuit
{
	public class MpPlugIn : PlugIn
	{
		public MpPlugIn()
		{
			allMP = new List<MpBase>();
		}

		public override String Name { get { return "Multiple Pursuit"; } }

		public override float SelectionOrderSortKey { get { return 0.04f; } }

		public override void Open()
		{
			// create the wanderer, saving a pointer to it
			wanderer = new MpWanderer();
			allMP.Add(wanderer);

			// create the specified number of pursuers, save pointers to them
			const int pursuerCount = 30;
			for (int i = 0; i < pursuerCount; i++)
				allMP.Add(new MpPursuer(wanderer));
			//pBegin = allMP.begin() + 1;  // iterator pointing to first pursuer
			//pEnd = allMP.end();          // iterator pointing to last pursuer

			// initialize camera
			Demo.SelectedVehicle = wanderer;
			Demo.Camera.Mode = Camera.CameraMode.StraightDown;
			Demo.Camera.FixedDistanceDistance = Demo.CameraTargetDistance;
			Demo.Camera.FixedDistanceVerticalOffset = Demo.Camera2dElevation;
		}

		public override void Update(float currentTime, float elapsedTime)
		{
			// update the wanderer
			wanderer.Update(currentTime, elapsedTime);

			// update each pursuer
			for (int i = 1; i < allMP.Count; i++)
			{
				((MpPursuer)allMP[i]).Update(currentTime, elapsedTime);
			}
		}

		public override void Redraw(float currentTime, float elapsedTime)
		{
			// selected vehicle (user can mouse click to select another)
			IVehicle selected = Demo.SelectedVehicle;

			// vehicle nearest mouse (to be highlighted)
			IVehicle nearMouse = null;//Demo.vehicleNearestToMouse ();

			// update camera
			Demo.UpdateCamera(currentTime, elapsedTime, selected);

			// draw "ground plane"
			Demo.GridUtility(selected.Position);

			// draw each vehicles
			for (int i = 0; i < allMP.Count; i++) allMP[i].Draw();

			// highlight vehicle nearest mouse
			Demo.HighlightVehicleUtility(nearMouse);
			Demo.CircleHighlightVehicleUtility(selected);
		}

		public override void Close()
		{
			// delete wanderer, all pursuers, and clear list
			allMP.Clear();
		}

		public override void Reset()
		{
			// reset wanderer and pursuers
			wanderer.Reset();
			for (int i = 1; i < allMP.Count; i++) ((MpPursuer)allMP[i]).Reset();

			// immediately jump to default camera position
			Demo.Camera.DoNotSmoothNextMove();
			Demo.Camera.ResetLocalSpace();
		}

		//const AVGroup& allVehicles () {return (const AVGroup&) allMP;}
		public override List<IVehicle> Vehicles
		{
			get { return allMP.ConvertAll<IVehicle>(delegate(MpBase m) { return (IVehicle)m; }); }
		}

		// a group (STL vector) of all vehicles
		List<MpBase> allMP;

		MpWanderer wanderer;
	}
}
