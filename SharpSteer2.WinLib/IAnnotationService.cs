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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	/// <summary>
	/// Provides methods to annotate the steering behaviors.
	/// </summary>
	public interface IAnnotationService
	{
		/// <summary>
		/// Indicates whether annotation is enabled.
		/// </summary>
		bool IsEnabled { get; set; }

		/// <summary>
		/// Adds a Trail.
		/// </summary>
		/// <param name="trail"></param>
		void AddTrail(Trail trail);

		/// <summary>
		/// Removes the specified Trail.
		/// </summary>
		/// <param name="trail"></param>
		void RemoveTrail(Trail trail);

		/// <summary>
		/// Draws all registered Trails.
		/// </summary>
		void DrawTrails(IDraw drawer);

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

		/// <summary>
		/// Draws an opaque colored line segment between two locations in space.
		/// </summary>
		/// <param name="startPoint">The start point of the line.</param>
		/// <param name="endPoint">The end point of the line.</param>
		/// <param name="color">The color of the line.</param>
		void Line(Vector3 startPoint, Vector3 endPoint, Color color);

		/// <summary>
		/// Draws a circle on the XZ plane.
		/// </summary>
		/// <param name="radius">The radius of the circle.</param>
		/// <param name="center">The center of the circle.</param>
		/// <param name="color">The color of the circle.</param>
		/// <param name="segments">The number of segments to use to draw the circle.</param>
		void CircleXZ(float radius, Vector3 center, Color color, int segments);

		/// <summary>
		/// Draws a disk on the XZ plane.
		/// </summary>
		/// <param name="radius">The radius of the disk.</param>
		/// <param name="center">The center of the disk.</param>
		/// <param name="color">The color of the disk.</param>
		/// <param name="segments">The number of segments to use to draw the disk.</param>
		void DiskXZ(float radius, Vector3 center, Color color, int segments);

		/// <summary>
		/// Draws a circle perpendicular to the given axis.
		/// </summary>
		/// <param name="radius">The radius of the circle.</param>
		/// <param name="center">The center of the circle.</param>
		/// <param name="axis">The axis of the circle.</param>
		/// <param name="color">The color of the circle.</param>
		/// <param name="segments">The number of segments to use to draw the circle.</param>
		void Circle3D(float radius, Vector3 center, Vector3 axis, Color color, int segments);

		/// <summary>
		/// Draws a disk perpendicular to the given axis.
		/// </summary>
		/// <param name="radius">The radius of the disk.</param>
		/// <param name="center">The center of the disk.</param>
		/// <param name="axis">The axis of the disk.</param>
		/// <param name="color">The color of the disk.</param>
		/// <param name="segments">The number of segments to use to draw the disk.</param>
		void Disk3D(float radius, Vector3 center, Vector3 axis, Color color, int segments);

		/// <summary>
		/// Draws a circle (not filled) or disk (filled) on the XZ plane.
		/// </summary>
		/// <param name="radius">The radius of the circle/disk.</param>
		/// <param name="center">The center of the circle/disk.</param>
		/// <param name="color">The color of the circle/disk.</param>
		/// <param name="segments">The number of segments to use to draw the circle/disk.</param>
		/// <param name="filled">Flag indicating whether to draw a disk or circle.</param>
		void CircleOrDiskXZ(float radius, Vector3 center, Color color, int segments, bool filled);

		/// <summary>
		/// Draws a circle (not filled) or disk (filled) perpendicular to the given axis.
		/// </summary>
		/// <param name="radius">The radius of the circle/disk.</param>
		/// <param name="center">The center of the circle/disk.</param>
		/// <param name="axis">The axis of the circle/disk.</param>
		/// <param name="color">The color of the circle/disk.</param>
		/// <param name="segments">The number of segments to use to draw the circle/disk.</param>
		/// <param name="filled">Flag indicating whether to draw a disk or circle.</param>
		void CircleOrDisk3D(float radius, Vector3 center, Vector3 axis, Color color, int segments, bool filled);

		/// <summary>
		/// Draws a circle (not filled) or disk (filled) perpendicular to the given axis.
		/// </summary>
		/// <param name="radius">The radius of the circle/disk.</param>
		/// <param name="axis">The axis of the circle/disk.</param>
		/// <param name="center">The center of the circle/disk.</param>
		/// <param name="color">The color of the circle/disk.</param>
		/// <param name="segments">The number of segments to use to draw the circle/disk.</param>
		/// <param name="filled">Flag indicating whether to draw a disk or circle.</param>
		/// <param name="in3d">Flag indicating whether to draw the disk/circle in 3D or the XZ plane.</param>
		void CircleOrDisk(float radius, Vector3 axis, Vector3 center, Color color, int segments, bool filled, bool in3d);

		/// <summary>
		/// Called when steerToAvoidObstacles decides steering is required.
		/// </summary>
		/// <param name="minDistanceToCollision"></param>
		void AvoidObstacle(float minDistanceToCollision);

		/// <summary>
		/// Called when steerToFollowPath decides steering is required.
		/// </summary>
		/// <param name="future"></param>
		/// <param name="onPath"></param>
		/// <param name="target"></param>
		/// <param name="outside"></param>
		void PathFollowing(Vector3 future, Vector3 onPath, Vector3 target, float outside);

		/// <summary>
		/// Called when steerToAvoidCloseNeighbors decides steering is required.
		/// </summary>
		/// <param name="other"></param>
		/// <param name="additionalDistance"></param>
		void AvoidCloseNeighbor(IVehicle other, float additionalDistance);

		/// <summary>
		/// Called when steerToAvoidNeighbors decides steering is required.
		/// </summary>
		/// <param name="threat"></param>
		/// <param name="steer"></param>
		/// <param name="ourFuture"></param>
		/// <param name="threatFuture"></param>
		void AvoidNeighbor(IVehicle threat, float steer, Vector3 ourFuture, Vector3 threatFuture);

		/// <summary>
		/// Draws lines from the vehicle's position showing its velocity and acceleration.
		/// </summary>
		/// <param name="vehicle">The vehicle to annotate.</param>
		void VelocityAcceleration(IVehicle vehicle);

		/// <summary>
		/// Draws lines from the vehicle's position showing its velocity and acceleration.
		/// </summary>
		/// <param name="vehicle">The vehicle to annotate.</param>
		/// <param name="maxLength">The maximum length for the acceleration and velocity lines.</param>
		void VelocityAcceleration(IVehicle vehicle, float maxLength);

		/// <summary>
		/// Draws lines from the vehicle's position showing its velocity and acceleration.
		/// </summary>
		/// <param name="vehicle">The vehicle to annotate.</param>
		/// <param name="maxLengthAcceleration">The maximum length for the acceleration line.</param>
		/// <param name="maxLengthVelocity">The maximum length for the velocity line.</param>
		void VelocityAcceleration(IVehicle vehicle, float maxLengthAcceleration, float maxLengthVelocity);
	}
}
