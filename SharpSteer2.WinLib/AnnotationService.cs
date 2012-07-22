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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	public class Annotation : IAnnotationService
	{
		bool isEnabled;
		List<Trail> trails;

		//HACK: change the IDraw to a IDrawService
		public static IDraw drawer;

		// constructor
		public Annotation()
		{
			isEnabled = true;
			trails = new List<Trail>();
		}

		/// <summary>
		/// Indicates whether annotation is enabled.
		/// </summary>
		public bool IsEnabled
		{
			get { return isEnabled; }
			set { isEnabled = value; }
		}

		/// <summary>
		/// Adds a Trail.
		/// </summary>
		/// <param name="trail">The trail to add.</param>
		public virtual void AddTrail(Trail trail)
		{
			trails.Add(trail);
		}

		/// <summary>
		/// Removes the specified Trail.
		/// </summary>
		/// <param name="trail">The trail to remove.</param>
		public virtual void RemoveTrail(Trail trail)
		{
			trails.Remove(trail);
		}

		/// <summary>
		/// Draws all registered Trails.
		/// </summary>
		public virtual void DrawTrails(IDraw drawer)
		{
			for (int i = 0; i < trails.Count; i++)
			{
				trails[i].Draw(drawer);
			}
		}

		// ------------------------------------------------------------------------
		// drawing of lines, circles and (filled) disks to annotate steering
		// behaviors.  When called during OpenSteerDemo's simulation update phase,
		// these functions call a "deferred draw" routine which buffer the
		// arguments for use during the redraw phase.
		//
		// note: "circle" means unfilled
		//       "disk" means filled
		//       "XZ" means on a plane parallel to the X and Z axes (perp to Y)
		//       "3d" means the circle is perpendicular to the given "axis"
		//       "segments" is the number of line segments used to draw the circle

		// draw an opaque colored line segment between two locations in space
		public virtual void Line(Vector3 startPoint, Vector3 endPoint, Color color)
		{
			if (isEnabled == true && drawer != null)
			{
				drawer.Line(startPoint, endPoint, color);
			}
		}

		// draw a circle on the XZ plane
		public virtual void CircleXZ(float radius, Vector3 center, Color color, int segments)
		{
			CircleOrDiskXZ(radius, center, color, segments, false);
		}

		// draw a disk on the XZ plane
		public virtual void DiskXZ(float radius, Vector3 center, Color color, int segments)
		{
			CircleOrDiskXZ(radius, center, color, segments, true);
		}

		// draw a circle perpendicular to the given axis
		public virtual void Circle3D(float radius, Vector3 center, Vector3 axis, Color color, int segments)
		{
			CircleOrDisk3D(radius, center, axis, color, segments, false);
		}

		// draw a disk perpendicular to the given axis
		public virtual void Disk3D(float radius, Vector3 center, Vector3 axis, Color color, int segments)
		{
			CircleOrDisk3D(radius, center, axis, color, segments, true);
		}

		// ------------------------------------------------------------------------
		// support for annotation circles
		public virtual void CircleOrDiskXZ(float radius, Vector3 center, Color color, int segments, bool filled)
		{
			CircleOrDisk(radius, Vector3.Zero, center, color, segments, filled, false);
		}

		public virtual void CircleOrDisk3D(float radius, Vector3 center, Vector3 axis, Color color, int segments, bool filled)
		{
			CircleOrDisk(radius, axis, center, color, segments, filled, true);
		}

		public virtual void CircleOrDisk(float radius, Vector3 axis, Vector3 center, Color color, int segments, bool filled, bool in3d)
		{
			if (isEnabled == true && drawer != null)
			{
				drawer.CircleOrDisk(radius, axis, center, color, segments, filled, in3d);
			}
		}

		// called when steerToAvoidObstacles decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public virtual void AvoidObstacle(float minDistanceToCollision)
		{
		}

		// called when steerToFollowPath decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public virtual void PathFollowing(Vector3 future, Vector3 onPath, Vector3 target, float outside)
		{
		}

		// called when steerToAvoidCloseNeighbors decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public virtual void AvoidCloseNeighbor(IVehicle other, float additionalDistance)
		{
		}

		// called when steerToAvoidNeighbors decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public virtual void AvoidNeighbor(IVehicle threat, float steer, Vector3 ourFuture, Vector3 threatFuture)
		{
		}

		public virtual void VelocityAcceleration(IVehicle vehicle)
		{
			VelocityAcceleration(vehicle, 3, 3);
		}

		public virtual void VelocityAcceleration(IVehicle vehicle, float maxLength)
		{
			VelocityAcceleration(vehicle, maxLength, maxLength);
		}

		public virtual void VelocityAcceleration(IVehicle vehicle, float maxLengthAcceleration, float maxLengthVelocity)
		{
			const byte desat = 102;
			Color vColor = new Color(255, desat, 255); // pinkish
			Color aColor = new Color(desat, desat, 255); // bluish

			float aScale = maxLengthAcceleration / vehicle.MaxForce;
			float vScale = maxLengthVelocity / vehicle.MaxSpeed;
			Vector3 p = vehicle.Position;

			Line(p, p + (vehicle.Velocity * vScale), vColor);
			Line(p, p + (vehicle.Acceleration * aScale), aColor);
		}
	}
}
