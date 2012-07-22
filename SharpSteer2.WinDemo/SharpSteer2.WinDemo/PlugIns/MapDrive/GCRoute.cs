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
using Microsoft.Xna.Framework;

namespace Bnoerj.AI.Steering.MapDrive
{
	// A variation on PolylinePathway (whose path tube radius is constant)
	// GCRoute (Grand Challenge Route) has an array of radii-per-segment
	//
	// XXX The OpenSteer path classes are long overdue for a rewrite.  When
	// XXX that happens, support should be provided for constant-radius,
	// XXX radius-per-segment (as in GCRoute), and radius-per-vertex.
	public class GCRoute : PolylinePathway
	{
		// construct a GCRoute given the number of points (vertices), an
		// array of points, an array of per-segment path radii, and a flag
		// indiating if the path is connected at the end.
		public GCRoute(int _pointCount, Vector3[] _points, float[] _radii, bool _cyclic)
		{
			Initialize(_pointCount, _points, _radii[0], _cyclic);

			radii = new float[pointCount];

			// loop over all points
			for (int i = 0; i < pointCount; i++)
			{
				// copy in point locations, closing cycle when appropriate
				bool closeCycle = cyclic && (i == pointCount - 1);
				int j = closeCycle ? 0 : i;
				points[i] = _points[j];
				radii[i] = _radii[i];
			}
		}

		// override the PolylinePathway method to allow for GCRoute-style
		// per-leg radii

		// Given an arbitrary point ("A"), returns the nearest point ("P") on
		// this path.  Also returns, via output arguments, the path tangent at
		// P and a measure of how far A is outside the Pathway's "tube".  Note
		// that a negative distance indicates A is inside the Pathway.
		public override Vector3 MapPointToPath(Vector3 point, out Vector3 tangent, out float outside)
		{
			Vector3 onPath = Vector3.Zero;
			tangent = Vector3.Zero;
			outside = float.MaxValue;

			// loop over all segments, find the one nearest to the given point
			for (int i = 1; i < pointCount; i++)
			{
				// QQQ note bizarre calling sequence of pointToSegmentDistance
				segmentLength = lengths[i];
				segmentNormal = normals[i];
				float d = PointToSegmentDistance(point, points[i - 1], points[i]);

				// measure how far original point is outside the Pathway's "tube"
				// (negative values (from 0 to -radius) measure "insideness")
				float o = d - radii[i];

				// when this is the smallest "outsideness" seen so far, take
				// note and save the corresponding point-on-path and tangent
				if (o < outside)
				{
					outside = o;
					onPath = chosen;
					tangent = segmentNormal;
				}
			}

			// return point on path
			return onPath;
		}

		// ignore that "tangent" output argument which is never used
		// XXX eventually move this to Pathway class
		public Vector3 MapPointToPath(Vector3 point, out float outside)
		{
			Vector3 tangent;
			return MapPointToPath(point, out tangent, out outside);
		}

		// get the index number of the path segment nearest the given point
		// XXX consider moving this to path class
		public int IndexOfNearestSegment(Vector3 point)
		{
			int index = 0;
			float minDistance = float.MaxValue;

			// loop over all segments, find the one nearest the given point
			for (int i = 1; i < pointCount; i++)
			{
				segmentLength = lengths[i];
				segmentNormal = normals[i];
				float d = PointToSegmentDistance(point, points[i - 1], points[i]);
				if (d < minDistance)
				{
					minDistance = d;
					index = i;
				}
			}
			return index;
		}

		// returns the dot product of the tangents of two path segments, 
		// used to measure the "angle" at a path vertex: how sharp is the turn?
		public float DotSegmentUnitTangents(int segmentIndex0, int segmentIndex1)
		{
			return Vector3.Dot(normals[segmentIndex0], normals[segmentIndex1]);
		}

		// return path tangent at given point (its projection on path)
		public Vector3 TangentAt(Vector3 point)
		{
			return normals[IndexOfNearestSegment(point)];
		}

		// return path tangent at given point (its projection on path),
		// multiplied by the given pathfollowing direction (+1/-1 =
		// upstream/downstream).  Near path vertices (waypoints) use the
		// tangent of the "next segment" in the given direction
		public Vector3 TangentAt(Vector3 point, int pathFollowDirection)
		{
			int segmentIndex = IndexOfNearestSegment(point);
			int nextIndex = segmentIndex + pathFollowDirection;
			bool insideNextSegment = IsInsidePathSegment(point, nextIndex);
			int i = (segmentIndex + (insideNextSegment ? pathFollowDirection : 0));
			return normals[i] * (float)pathFollowDirection;
		}

		// is the given point "near" a waypoint of this path?  ("near" == closer
		// to the waypoint than the max of radii of two adjacent segments)
		public bool NearWaypoint(Vector3 point)
		{
			// loop over all waypoints
			for (int i = 1; i < pointCount; i++)
			{
				// return true if near enough to this waypoint
				float r = Math.Max(radii[i], radii[(i + 1) % pointCount]);
				float d = (point - points[i]).Length();
				if (d < r) return true;
			}
			return false;
		}

		// is the given point inside the path tube of the given segment
		// number?  (currently not used. this seemed like a useful utility,
		// but wasn't right for the problem I was trying to solve)
		public bool IsInsidePathSegment(Vector3 point, int segmentIndex)
		{
			if (segmentIndex < 1 || segmentIndex >= pointCount) return false;

			int i = segmentIndex;

			// QQQ note bizarre calling sequence of pointToSegmentDistance
			segmentLength = lengths[i];
			segmentNormal = normals[i];
			float d = PointToSegmentDistance(point, points[i - 1], points[i]);

			// measure how far original point is outside the Pathway's "tube"
			// (negative values (from 0 to -radius) measure "insideness")
			float o = d - radii[i];

			// return true if point is inside the tube
			return o < 0;
		}

		// per-segment radius (width) array
		public float[] radii;
	}
}
