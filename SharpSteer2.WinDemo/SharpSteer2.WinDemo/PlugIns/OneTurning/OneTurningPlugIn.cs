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
	public class OneTurningPlugIn : PlugIn
	{
		public OneTurningPlugIn()
		{
			theVehicle = new List<OneTurning>();
		}

		public override String Name { get { return "One Turning Away"; } }

		public override float SelectionOrderSortKey { get { return 0.06f; } }

		public override void Open()
		{
			oneTurning = new OneTurning();
			Demo.SelectedVehicle = oneTurning;
			theVehicle.Add(oneTurning);

			// initialize camera
			Demo.Init2dCamera(oneTurning);
			Demo.Camera.SetPosition(10, Demo.Camera2dElevation, 10);
			Demo.Camera.FixedPosition = new Vector3(40);
		}

		public override void Update(float currentTime, float elapsedTime)
		{
			// update simulation of test vehicle
			oneTurning.Update(currentTime, elapsedTime);
		}

		public override void Redraw(float currentTime, float elapsedTime)
		{
			// draw "ground plane"
			Demo.GridUtility(oneTurning.Position);

			// draw test vehicle
			oneTurning.Draw();

			// textual annotation (following the test vehicle's screen position)
			String annote = String.Format("      speed: {0:0.00}", oneTurning.Speed);
			Drawing.Draw2dTextAt3dLocation(annote, oneTurning.Position, Color.Red);
			Drawing.Draw2dTextAt3dLocation("start", Vector3.Zero, Color.Green);

			// update camera, tracking test vehicle
			Demo.UpdateCamera(currentTime, elapsedTime, oneTurning);
		}

		public override void Close()
		{
			theVehicle.Clear();
			oneTurning = null;
		}

		public override void Reset()
		{
			// reset vehicle
			oneTurning.Reset();
		}

		public override List<IVehicle> Vehicles
		{
			get { return theVehicle.ConvertAll<IVehicle>(delegate(OneTurning v) { return (IVehicle)v; }); }
		}

		OneTurning oneTurning;
		List<OneTurning> theVehicle; // for allVehicles
	}
}
