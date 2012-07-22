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

namespace Bnoerj.AI.Steering.LowSpeedTurn
{
	class LowSpeedTurnPlugIn : PlugIn
	{
		const int lstCount = 5;
		const float lstLookDownDistance = 18;
		static Vector3 lstViewCenter = new Vector3(7, 0, -2);
		static Vector3 lstPlusZ = new Vector3(0, 0, 1);

		public LowSpeedTurnPlugIn()
			: base()
		{
			all = new List<LowSpeedTurn>();
		}

		public override String Name { get { return "Low Speed Turn"; } }

		public override float SelectionOrderSortKey { get { return 0.05f; } }

		public override void Open()
		{
			// create a given number of agents with stepped inital parameters,
			// store pointers to them in an array.
			LowSpeedTurn.ResetStarts();
			for (int i = 0; i < lstCount; i++) all.Add(new LowSpeedTurn());

			// initial selected vehicle
			Demo.SelectedVehicle = all[0];

			// initialize camera
			Demo.Camera.Mode = Camera.CameraMode.Fixed;
			Demo.Camera.FixedUp = lstPlusZ;
			Demo.Camera.FixedTarget = lstViewCenter;
			Demo.Camera.FixedPosition = lstViewCenter;
			Demo.Camera.FixedPosition.Y += lstLookDownDistance;
			Demo.Camera.LookDownDistance = lstLookDownDistance;
			Demo.Camera.FixedDistanceVerticalOffset = Demo.Camera2dElevation;
			Demo.Camera.FixedDistanceDistance = Demo.CameraTargetDistance;
		}

		public override void Update(float currentTime, float elapsedTime)
		{
			// update, draw and annotate each agent
			for (int i = 0; i < all.Count; i++)
			{
				all[i].Update(currentTime, elapsedTime);
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

			// draw "ground plane"
			Demo.GridUtility(selected.Position);

			// update, draw and annotate each agent
			for (int i = 0; i < all.Count; i++)
			{
				// draw this agent
				LowSpeedTurn agent = all[i];
				agent.Draw();

				// display speed near agent's screen position
				Color textColor = new Color(new Vector3(0.8f, 0.8f, 1.0f));
				Vector3 textOffset = new Vector3(0, 0.25f, 0);
				Vector3 textPosition = agent.Position + textOffset;
				String annote = String.Format("{0:0.00}", agent.Speed);
				Drawing.Draw2dTextAt3dLocation(annote, textPosition, textColor);
			}

			// highlight vehicle nearest mouse
			Demo.HighlightVehicleUtility(nearMouse);
		}

		public override void Close()
		{
			all.Clear();
		}

		public override void Reset()
		{
			// reset each agent
			LowSpeedTurn.ResetStarts();
			for (int i = 0; i < all.Count; i++) all[i].Reset();
		}

		public override List<IVehicle> Vehicles
		{
			get { return all.ConvertAll<IVehicle>(delegate(LowSpeedTurn v) { return (IVehicle)v; }); }
		}

		List<LowSpeedTurn> all; // for allVehicles
	}
}
