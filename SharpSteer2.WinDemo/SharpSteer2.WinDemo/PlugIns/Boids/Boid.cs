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
using Microsoft.Xna.Framework;

namespace Bnoerj.AI.Steering.Boids
{
	using ProximityDatabase = IProximityDatabase<IVehicle>;
	using ProximityToken = ITokenForProximityDatabase<IVehicle>;
	using SOG = List<SphericalObstacle>;  // spherical obstacle group

	public class Boid : SimpleVehicle
	{
		public const float AvoidancePredictTimeMin = 0.9f;
		public const float AvoidancePredictTimeMax = 2;
		public static float AvoidancePredictTime = AvoidancePredictTimeMin;

		// a pointer to this boid's interface object for the proximity database
		public ProximityToken proximityToken;

		// allocate one and share amoung instances just to save memory usage
		// (change to per-instance allocation to be more MP-safe)
		public static List<IVehicle> neighbors = new List<IVehicle>();
		public static int boundaryCondition = 0;
		public const float worldRadius = 50;

		// constructor
		public Boid(ProximityDatabase pd)
		{
			// allocate a token for this boid in the proximity database
			proximityToken = null;
			NewPD(pd);

			// reset all boid state
			Reset();
		}

		// reset state
		public override void Reset()
		{
			// reset the vehicle
			base.Reset();

			// steering force is clipped to this magnitude
			MaxForce = 27;

			// velocity is clipped to this magnitude
			MaxSpeed = 9;

			// initial slow speed
			Speed = (MaxSpeed * 0.3f);

			// randomize initial orientation
			//RegenerateOrthonormalBasisUF(Vector3Helpers.RandomUnitVector());
			Vector3 d = Vector3Helpers.RandomUnitVector();
			d.X = Math.Abs(d.X);
			d.Y = 0;
			d.Z = Math.Abs(d.Z);
			RegenerateOrthonormalBasisUF(d);

			// randomize initial position
			Position = Vector3.UnitX * 10 + (Vector3Helpers.RandomVectorInUnitRadiusSphere() * 20);

			// notify proximity database that our position has changed
			//FIXME: SimpleVehicle::SimpleVehicle() calls reset() before proximityToken is set
			if (proximityToken != null) proximityToken.UpdateForNewPosition(Position);
		}

		// draw this boid into the scene
		public void Draw()
		{
			Drawing.DrawBasic3dSphericalVehicle(this, Color.LightGray);
		}

		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			// steer to flock and perhaps to stay within the spherical boundary
			ApplySteeringForce(SteerToFlock() + HandleBoundary(), elapsedTime);

			// notify proximity database that our position has changed
			proximityToken.UpdateForNewPosition(Position);
		}

		// basic flocking
		public Vector3 SteerToFlock()
		{
			const float separationRadius = 5.0f;
			const float separationAngle = -0.707f;
			const float separationWeight = 12.0f;

			const float alignmentRadius = 7.5f;
			const float alignmentAngle = 0.7f;
			const float alignmentWeight = 8.0f;

			const float cohesionRadius = 9.0f;
			const float cohesionAngle = -0.15f;
			const float cohesionWeight = 8.0f;

			float maxRadius = Math.Max(separationRadius, Math.Max(alignmentRadius, cohesionRadius));

			// find all flockmates within maxRadius using proximity database
			neighbors.Clear();
			proximityToken.FindNeighbors(Position, maxRadius, ref neighbors);

			// determine each of the three component behaviors of flocking
			Vector3 separation = SteerForSeparation(separationRadius, separationAngle, neighbors);
			Vector3 alignment = SteerForAlignment(alignmentRadius, alignmentAngle, neighbors);
			Vector3 cohesion = SteerForCohesion(cohesionRadius, cohesionAngle, neighbors);

			// apply weights to components (save in variables for annotation)
			Vector3 separationW = separation * separationWeight;
			Vector3 alignmentW = alignment * alignmentWeight;
			Vector3 cohesionW = cohesion * cohesionWeight;

			Vector3 avoidance = SteerToAvoidObstacles(Boid.AvoidancePredictTimeMin, AllObstacles);

			// saved for annotation
			bool Avoiding = (avoidance != Vector3.Zero);
			Vector3 steer = separationW + alignmentW + cohesionW;
			if (Avoiding)
			{
				steer = avoidance;
				System.Diagnostics.Debug.WriteLine(String.Format("Avoiding: [{0}, {1}, {2}]", avoidance.X, avoidance.Y, avoidance.Z));
			}
#if IGNORED
			// annotation
			const float s = 0.1f;
			AnnotationLine(Position, Position + (separationW * s), Color.Red);
			AnnotationLine(Position, Position + (alignmentW * s), Color.Orange);
			AnnotationLine(Position, Position + (cohesionW * s), Color.Yellow);
#endif
			return steer;
		}

		// Take action to stay within sphereical boundary.  Returns steering
		// value (which is normally zero) and may take other side-effecting
		// actions such as kinematically changing the Boid's position.
		public Vector3 HandleBoundary()
		{
			// while inside the sphere do noting
			if (Position.Length() < worldRadius)
				return Vector3.Zero;

			// once outside, select strategy
			switch (boundaryCondition)
			{
			case 0:
				{
					// steer back when outside
					Vector3 seek = xxxSteerForSeek(Vector3.Zero);
                    Vector3 lateral = Vector3Helpers.PerpendicularComponent(seek, Forward);
					return lateral;
				}
			case 1:
				{
					// wrap around (teleport)
                    Position = (Vector3Helpers.SphericalWrapAround(Position, Vector3.Zero, worldRadius));
					return Vector3.Zero;
				}
			}
			return Vector3.Zero; // should not reach here
		}

		// make boids "bank" as they fly
		public override void RegenerateLocalSpace(Vector3 newVelocity, float elapsedTime)
		{
			RegenerateLocalSpaceForBanking(newVelocity, elapsedTime);
		}

		// switch to new proximity database -- just for demo purposes
		public void NewPD(ProximityDatabase pd)
		{
			// delete this boid's token in the old proximity database
			if (proximityToken != null)
			{
				proximityToken.Dispose();
				proximityToken = null;
			}

			// allocate a token for this boid in the proximity database
			proximityToken = pd.AllocateToken(this);
		}

		// cycle through various boundary conditions
		public static void NextBoundaryCondition()
		{
			const int max = 2;
			boundaryCondition = (boundaryCondition + 1) % max;
		}

		// dynamic obstacle registry
		public static void InitializeObstacles()
		{
			// start with 40% of possible obstacles
			if (obstacleCount == -1)
			{
				obstacleCount = 0;
				for (int i = 0; i < (maxObstacleCount * 1.0); i++)
					AddOneObstacle();
			}
		}

		public static void AddOneObstacle()
		{
			if (obstacleCount < maxObstacleCount)
			{
				// pick a random center and radius,
				// loop until no overlap with other obstacles and the home base
				//float r = 15;
				//Vector3 c = Vector3.Up * r * (-0.5f * maxObstacleCount + obstacleCount);
				float r = Utilities.Random(0.5f, 2);
				Vector3 c = Vector3Helpers.RandomVectorInUnitRadiusSphere() * worldRadius * 1.1f;

				// add new non-overlapping obstacle to registry
				AllObstacles.Add(new SphericalObstacle(r, c));
				obstacleCount++;
			}
		}

		public static void RemoveOneObstacle()
		{
			if (obstacleCount > 0)
			{
				obstacleCount--;
				AllObstacles.RemoveAt(obstacleCount);
			}
		}

		public float MinDistanceToObstacle(Vector3 point)
		{
			float r = 0;
			Vector3 c = point;
			float minClearance = float.MaxValue;
			for (int so = 0; so < AllObstacles.Count; so++)
			{
				minClearance = TestOneObstacleOverlap(minClearance, r, AllObstacles[so].Radius, c, AllObstacles[so].Center);
			}
			return minClearance;
		}

		static float TestOneObstacleOverlap(float minClearance, float r, float radius, Vector3 c, Vector3 center)
		{
			float d = Vector3.Distance(c, center);
			float clearance = d - (r + radius);
			if (minClearance > clearance)
				minClearance = clearance;
			return minClearance;
		}

		protected static int obstacleCount = -1;
		protected const int maxObstacleCount = 100;
		public static SOG AllObstacles = new SOG();
	}
}
