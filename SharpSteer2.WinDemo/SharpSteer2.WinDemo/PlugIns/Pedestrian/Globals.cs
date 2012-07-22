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

namespace Bnoerj.AI.Steering.Pedestrian
{
	using ObstacleGroup = List<IObstacle>;

	class Globals
	{
		// create path for PlugIn 
		//
		//
		//        | gap |
		//
		//        f      b
		//        |\    /\        -
		//        | \  /  \       ^
		//        |  \/    \      |
		//        |  /\     \     |
		//        | /  \     c   top
		//        |/    \g  /     |
		//        /        /      |
		//       /|       /       V      z     y=0
		//      / |______/        -      ^
		//     /  e      d               |
		//   a/                          |
		//    |<---out-->|               o----> x
		//
		public static PolylinePathway GetTestPath()
		{
			if (TestPath == null)
			{
				const float pathRadius = 2;

				const int pathPointCount = 7;
				const float size = 30;
				float top = 2 * size;
				float gap = 1.2f * size;
				float outter = 2 * size;
				float h = 0.5f;
				Vector3[] pathPoints = new Vector3[pathPointCount]
					{
						new Vector3 (h+gap-outter,  0,  h+top-outter), // 0 a
						new Vector3 (h+gap,         0,  h+top),        // 1 b
						new Vector3 (h+gap+(top/2), 0,  h+top/2),      // 2 c
						new Vector3 (h+gap,         0,  h),            // 3 d
						new Vector3 (h,             0,  h),            // 4 e
						new Vector3 (h,             0,  h+top),        // 5 f
						new Vector3 (h+gap,         0,  h+top/2)       // 6 g
					};

				Obstacle1.Center = Utilities.Interpolate(0.2f, pathPoints[0], pathPoints[1]);
				Obstacle2.Center = Utilities.Interpolate(0.5f, pathPoints[2], pathPoints[3]);
				Obstacle1.Radius = 3;
				Obstacle2.Radius = 5;
				Obstacles.Add(Obstacle1);
				Obstacles.Add(Obstacle2);

				Endpoint0 = pathPoints[0];
				Endpoint1 = pathPoints[pathPointCount - 1];

				TestPath = new PolylinePathway(pathPointCount,
												 pathPoints,
												 pathRadius,
												 false);
			}
			return TestPath;
		}

		public static PolylinePathway TestPath = null;
		public static SphericalObstacle Obstacle1 = new SphericalObstacle();
		public static SphericalObstacle Obstacle2 = new SphericalObstacle();
		public static ObstacleGroup Obstacles = new ObstacleGroup();
		public static Vector3 Endpoint0 = Vector3.Zero;
		public static Vector3 Endpoint1 = Vector3.Zero;
		public static bool UseDirectedPathFollowing = true;

		// this was added for debugging tool, but I might as well leave it in
		public static bool WanderSwitch = true;
	}
}
