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

namespace Bnoerj.AI.Steering.MapDrive
{
	public class MapDriver : SimpleVehicle
	{
		Trail trail;

		// constructor
		public MapDriver()
		{
			map = MakeMap();
			path = MakePath();

			Reset();

			// to compute mean time between collisions
			sumOfCollisionFreeTimes = 0;
			countOfCollisionFreeTimes = 0;

			// keep track for reliability statistics
			collisionLastTime = false;
			timeOfLastCollision = Demo.Clock.TotalSimulationTime;

			// keep track of average speed
			totalDistance = 0;
			totalTime = 0;

			// keep track of path following failure rate
			pathFollowTime = 0;
			pathFollowOffTime = 0;

			// innitialize counters for various performance data
			stuckCount = 0;
			stuckCycleCount = 0;
			stuckOffPathCount = 0;
			lapsStarted = 0;
			lapsFinished = 0;
			hintGivenCount = 0;
			hintTakenCount = 0;

			// follow the path "upstream or downstream" (+1/-1)
			pathFollowDirection = 1;

			// use curved prediction and incremental steering:
			curvedSteering = true;
			incrementalSteering = true;
		}

		// reset state
		public override void Reset()
		{
			// reset the underlying vehicle class
			base.Reset();

			// initially stopped
			Speed = 0;

			// Assume top speed is 20 meters per second (44.7 miles per hour).
			// This value will eventually be supplied by a higher level module.
			MaxSpeed = 20;

			// steering force is clipped to this magnitude
			MaxForce = MaxSpeed * 0.4f;

			// vehicle is 2 meters wide and 3 meters long
			halfWidth = 1.0f;
			halfLength = 1.5f;

			// init dynamically controlled radius
			AdjustVehicleRadiusForSpeed();

			// not previously avoiding
			annotateAvoid = Vector3.Zero;

			// 10 seconds with 200 points along the trail
			if (trail == null) trail = new Trail(10, 200);

			// prevent long streaks due to teleportation 
			trail.Clear();

			// first pass at detecting "stuck" state
			stuck = false;

			// QQQ need to clean up this hack
			qqqLastNearestObstacle = Vector3.Zero;

			// master look ahead (prediction) time
			baseLookAheadTime = 3;

			if (demoSelect == 2)
			{
				lapsStarted++;
				float s = worldSize;
				float d = (float)pathFollowDirection;
				Position = (new Vector3(s * d * 0.6f, 0, s * -0.4f));
				RegenerateOrthonormalBasisUF(Vector3.Right * d);
			}

			// reset bookeeping to detect stuck cycles
			ResetStuckCycleDetection();

			// assume no previous steering
			currentSteering = Vector3.Zero;

			// assume normal running state
			dtZero = false;

			// QQQ temporary global QQQoaJustScraping
			QQQoaJustScraping = false;

			// state saved for speedometer
			annoteMaxRelSpeed = annoteMaxRelSpeedCurve = annoteMaxRelSpeedPath = 0;
			annoteMaxRelSpeed = annoteMaxRelSpeedCurve = annoteMaxRelSpeedPath = 1;
		}


		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			// take note when current dt is zero (as in paused) for stat counters
			dtZero = (elapsedTime == 0);

			// pretend we are bigger when going fast
			AdjustVehicleRadiusForSpeed();

			// state saved for speedometer
			//      annoteMaxRelSpeed = annoteMaxRelSpeedCurve = annoteMaxRelSpeedPath = 0;
			annoteMaxRelSpeed = annoteMaxRelSpeedCurve = annoteMaxRelSpeedPath = 1;

			// determine combined steering
			Vector3 steering = Vector3.Zero;
			bool offPath = !IsBodyInsidePath();
			if (stuck || offPath || DetectImminentCollision())
			{
				// bring vehicle to a stop if we are stuck (newly or previously
				// stuck, because off path or collision seemed imminent)
				// (QQQ combine with stuckCycleCount code at end of this function?)
				//ApplyBrakingForce (curvedSteering ? 3 : 2, elapsedTime); // QQQ
				ApplyBrakingForce((curvedSteering ? 3.0f : 2.0f), elapsedTime); // QQQ
				// count "off path" events
				if (offPath && !stuck && (demoSelect == 2)) stuckOffPathCount++;
				stuck = true;

				// QQQ trying to prevent "creep" during emergency stops
				ResetAcceleration();
				currentSteering = Vector3.Zero;
			}
			else
			{
				// determine steering for obstacle avoidance (save for annotation)
				Vector3 avoid = annotateAvoid = SteerToAvoidObstaclesOnMap(LookAheadTimeOA(), map, HintForObstacleAvoidance());
				bool needToAvoid = avoid != Vector3.Zero;

				// any obstacles to avoid?
				if (needToAvoid)
				{
					// slow down and turn to avoid the obstacles
					float targetSpeed = ((curvedSteering && QQQoaJustScraping) ? MaxSpeedForCurvature() : 0);
					annoteMaxRelSpeed = targetSpeed / MaxSpeed;
					float avoidWeight = 3 + (3 * RelativeSpeed()); // ad hoc
					steering = avoid * avoidWeight;
					steering += SteerForTargetSpeed(targetSpeed);
				}
				else
				{
					// otherwise speed up and...
					steering = SteerForTargetSpeed(MaxSpeedForCurvature());

					// wander for demo 1
					if (demoSelect == 1)
					{
						Vector3 wander = SteerForWander(elapsedTime);
						wander.Y = 0;
						Vector3 flat = wander;
						Vector3 weighted = Vector3Helpers.TruncateLength(flat, MaxForce) * 6;
						Vector3 a = Position + new Vector3(0, 0.2f, 0);
						annotation.Line(a, a + (weighted * 0.3f), Color.White);
						steering += weighted;
					}

					// follow the path in demo 2
					if (demoSelect == 2)
					{
						Vector3 pf = SteerToFollowPath(pathFollowDirection, LookAheadTimePF(), path);
						if (pf != Vector3.Zero)
						{
							// steer to remain on path
							if (Vector3.Dot(pf, Forward) < 0)
								steering = pf;
							else
								steering = pf + steering;
						}
						else
						{
							// path aligment: when neither obstacle avoidance nor
							// path following is required, align with path segment
							Vector3 pathHeading = path.TangentAt(Position, pathFollowDirection);
							{
								Vector3 b = (Position + (Up * 0.2f) + (Forward * halfLength * 1.4f));
								float l = 2;
								annotation.Line(b, b + (Forward * l), Color.Cyan);
								annotation.Line(b, b + (pathHeading * l), Color.Cyan);
							}
							steering += (SteerTowardHeading(pathHeading) *
										 (path.NearWaypoint(Position) ?
										  0.5f : 0.1f));
						}
					}
				}
			}

			if (!stuck)
			{
				// convert from absolute to incremental steering signal
				if (incrementalSteering)
					steering = ConvertAbsoluteToIncrementalSteering(steering, elapsedTime);
				// enforce minimum turning radius
				steering = AdjustSteeringForMinimumTurningRadius(steering);
			}

			// apply selected steering force to vehicle, record data
			ApplySteeringForce(steering, elapsedTime);
			CollectReliabilityStatistics(currentTime, elapsedTime);

			// detect getting stuck in cycles -- we are moving but not
			// making progress down the route (annotate smoothedPosition)
			if (demoSelect == 2)
			{
				bool circles = WeAreGoingInCircles();
				if (circles && !stuck) stuckCycleCount++;
				if (circles) stuck = true;
				annotation.CircleOrDisk(0.5f, Up, SmoothedPosition, Color.White, 12, circles, false);
			}

			// annotation
			PerFrameAnnotation();
			trail.Record(currentTime, Position);
		}

		public void AdjustVehicleRadiusForSpeed()
		{
			float minRadius = (float)Math.Sqrt(Utilities.Square(halfWidth) + Utilities.Square(halfLength));
			float safetyMargin = (curvedSteering ? Utilities.Interpolate(RelativeSpeed(), 0.0f, 1.5f) : 0.0f);
			Radius = (minRadius + safetyMargin);
		}

		public void CollectReliabilityStatistics(float currentTime, float elapsedTime)
		{
			// detect obstacle avoidance failure and keep statistics
			collisionDetected = map.ScanLocalXZRectangle(this,
														   -halfWidth, halfWidth,
														   -halfLength, halfLength);

			// record stats to compute mean time between collisions
			float timeSinceLastCollision = currentTime - timeOfLastCollision;
			if (collisionDetected && !collisionLastTime && timeSinceLastCollision > 1)
			{
				sumOfCollisionFreeTimes += timeSinceLastCollision;
				countOfCollisionFreeTimes++;
				timeOfLastCollision = currentTime;
			}
			collisionLastTime = collisionDetected;

			// keep track of average speed
			totalDistance += Speed * elapsedTime;
			totalTime += elapsedTime;

			// keep track of path following failure rate
			// QQQ for now, duplicating this code from the draw method:
			// if we are following a path but are off the path,
			// draw a red line to where we should be
			if (demoSelect == 2)
			{
				pathFollowTime += elapsedTime;
				if (!IsBodyInsidePath()) pathFollowOffTime += elapsedTime;
			}
		}

		public Vector3 HintForObstacleAvoidance()
		{
			// used only when path following, return zero ("no hint") otherwise
			if (demoSelect != 2) return Vector3.Zero;

			// are we heading roughly parallel to the current path segment?
			Vector3 p = Position;
			Vector3 pathHeading = path.TangentAt(p, pathFollowDirection);
			if (Vector3.Dot(pathHeading, Forward) < 0.8f)
			{
				// if not, the "hint" is to turn to align with path heading
				Vector3 s = Side * halfWidth;
				float f = halfLength * 2;
				annotation.Line(p + s, p + s + (Forward * f), Color.Black);
				annotation.Line(p - s, p - s + (Forward * f), Color.Black);
				annotation.Line(p, p + (pathHeading * 5), Color.Magenta);
				return pathHeading;
			}
			else
			{
				// when there is a valid nearest obstacle position
				Vector3 obstacle = qqqLastNearestObstacle;
				Vector3 o = obstacle + (Up * 0.1f);
				if (obstacle != Vector3.Zero)
				{
					// get offset, distance from obstacle to its image on path
					float outside;
					Vector3 onPath = path.MapPointToPath(obstacle, out outside);
					Vector3 offset = onPath - obstacle;
					float offsetDistance = offset.Length();

					// when the obstacle is inside the path tube
					if (outside < 0)
					{
						// when near the outer edge of a sufficiently wide tube
						int segmentIndex = path.IndexOfNearestSegment(onPath);
						float segmentRadius = path.radii[segmentIndex];
						float w = halfWidth * 6;
						bool nearEdge = offsetDistance > w;
						bool wideEnough = segmentRadius > (w * 2);
						if (nearEdge && wideEnough)
						{
							float obstacleDistance = (obstacle - p).Length();
							float range = Speed * LookAheadTimeOA();
							float farThreshold = range * 0.8f;
							bool usableHint = obstacleDistance > farThreshold;
							if (usableHint)
							{
								Vector3 temp = offset;
								temp.Normalize();
								Vector3 q = p + (temp * 5);
								annotation.Line(p, q, Color.Magenta);
								annotation.CircleOrDisk(0.4f, Up, o, Color.White, 12, false, false);
								return offset;
							}
						}
					}
					annotation.CircleOrDisk(0.4f, Up, o, Color.Black, 12, false, false);
				}
			}
			// otherwise, no hint
			return Vector3.Zero;
		}

		// like steerToAvoidObstacles, but based on a BinaryTerrainMap indicating
		// the possitions of impassible regions
		//
		public Vector3 SteerToAvoidObstaclesOnMap(float minTimeToCollision, TerrainMap map)
		{
			return SteerToAvoidObstaclesOnMap(minTimeToCollision, map, Vector3.Zero); // no steer hint
		}

		// given a map of obstacles (currently a global, binary map) steer so as
		// to avoid collisions within the next minTimeToCollision seconds.
		//
		public Vector3 SteerToAvoidObstaclesOnMap(float minTimeToCollision, TerrainMap map, Vector3 steerHint)
		{
			float spacing = map.MinSpacing() / 2;
			float maxSide = Radius;
			float maxForward = minTimeToCollision * Speed;
			int maxSamples = (int)(maxForward / spacing);
			Vector3 step = Forward * spacing;
			Vector3 fOffset = Position;
			Vector3 sOffset = Vector3.Zero;
			float s = spacing / 2;

			int infinity = 9999; // qqq
			int nearestL = infinity;
			int nearestR = infinity;
			int nearestWL = infinity;
			int nearestWR = infinity;
			Vector3 nearestO = Vector3.Zero;
			wingDrawFlagL = false;
			wingDrawFlagR = false;

			bool hintGiven = steerHint != Vector3.Zero;
			if (hintGiven && !dtZero)
				hintGivenCount++;
			if (hintGiven)
				annotation.CircleOrDisk(halfWidth * 0.9f, Up, Position + (Up * 0.2f), Color.White, 12, false, false);

			// QQQ temporary global QQQoaJustScraping
			QQQoaJustScraping = true;

			float signedRadius = 1 / NonZeroCurvatureQQQ();
			Vector3 localCenterOfCurvature = Side * signedRadius;
			Vector3 center = Position + localCenterOfCurvature;
			float sign = signedRadius < 0 ? 1.0f : -1.0f;
			float arcRadius = signedRadius * -sign;
			float twoPi = 2 * (float)Math.PI;
			float circumference = twoPi * arcRadius;
			float rawLength = Speed * minTimeToCollision * sign;
			float fracLimit = 1.0f / 6.0f;
			float distLimit = circumference * fracLimit;
			float arcLength = ArcLengthLimit(rawLength, distLimit);
			float arcAngle = twoPi * arcLength / circumference;

			// XXX temp annotation to show limit on arc angle
			if (curvedSteering)
			{
				if ((Speed * minTimeToCollision) > (circumference * fracLimit))
				{
					float q = twoPi * fracLimit;
					Vector3 fooz = Position - center;
					Vector3 booz = Vector3Helpers.RotateAboutGlobalY(fooz, sign * q);
					annotation.Line(center, center + fooz, Color.Red);
					annotation.Line(center, center + booz, Color.Red);
				}
			}

			// assert loops will terminate
			System.Diagnostics.Debug.Assert(spacing > 0);

			// scan corridor straight ahead of vehicle,
			// keep track of nearest obstacle on left and right sides
			while (s < maxSide)
			{
				sOffset = Side * s;
				s += spacing;
				Vector3 lOffset = fOffset + sOffset;
				Vector3 rOffset = fOffset - sOffset;

				Vector3 lObsPos = Vector3.Zero, rObsPos = Vector3.Zero;

				int L = (curvedSteering ?
							   (int)(ScanObstacleMap(lOffset,
													   center,
													   arcAngle,
													   maxSamples,
													   0,
													   Color.Yellow,
													   Color.Red,
													   out lObsPos)
									  / spacing) :
							   map.ScanXZray(lOffset, step, maxSamples));
				int R = (curvedSteering ?
							   (int)(ScanObstacleMap(rOffset,
														center,
													   arcAngle,
													   maxSamples,
													   0,
													   Color.Yellow,
													   Color.Red,
													   out rObsPos)
									  / spacing) :
							   map.ScanXZray(rOffset, step, maxSamples));

				if ((L > 0) && (L < nearestL))
				{
					nearestL = L;
					if (L < nearestR) nearestO = ((curvedSteering) ?
												  lObsPos :
												  lOffset + (step * (float)L));
				}
				if ((R > 0) && (R < nearestR))
				{
					nearestR = R;
					if (R < nearestL) nearestO = ((curvedSteering) ?
												  rObsPos :
												  rOffset + (step * (float)R));
				}

				if (!curvedSteering)
				{
					AnnotateAvoidObstaclesOnMap(lOffset, L, step);
					AnnotateAvoidObstaclesOnMap(rOffset, R, step);
				}

				if (curvedSteering)
				{
					// QQQ temporary global QQQoaJustScraping
					bool outermost = s >= maxSide;
					bool eitherSide = (L > 0) || (R > 0);
					if (!outermost && eitherSide) QQQoaJustScraping = false;
				}
			}
			qqqLastNearestObstacle = nearestO;

			// scan "wings"
			{
				int wingScans = 4;
				// see duplicated code at: QQQ draw sensing "wings"
				// QQQ should be a parameter of this method
				Vector3 wingWidth = Side * WingSlope() * maxForward;

				Color beforeColor = new Color((byte)(255.0f * 0.75f), (byte)(255.0f * 0.9f), (byte)(255.0f * 0.0f));  // for annotation
				Color afterColor = new Color((byte)(255.0f * 0.9f), (byte)(255.0f * 0.5f), (byte)(255.0f * 0.0f));  // for annotation

				for (int i = 1; i <= wingScans; i++)
				{
					float fraction = (float)i / (float)wingScans;
					Vector3 endside = sOffset + (wingWidth * fraction);
					Vector3 corridorFront = Forward * maxForward;

					// "loop" from -1 to 1
					for (int j = -1; j < 2; j += 2)
					{
						float k = (float)j; // prevent VC7.1 warning
						Vector3 start = fOffset + (sOffset * k);
						Vector3 end = fOffset + corridorFront + (endside * k);
						Vector3 ray = end - start;
						float rayLength = ray.Length();
						Vector3 step2 = ray * spacing / rayLength;
						int raySamples = (int)(rayLength / spacing);
						float endRadius =
							WingSlope() * maxForward * fraction *
							(signedRadius < 0 ? 1 : -1) * (j == 1 ? 1 : -1);
						Vector3 ignore;
						int scan = (curvedSteering ?
										  (int)(ScanObstacleMap(start,
																  center,
																  arcAngle,
																  raySamples,
																  endRadius,
																  beforeColor,
																  afterColor,
																  out ignore)
												 / spacing) :
										  map.ScanXZray(start, step2, raySamples));

						if (!curvedSteering)
							AnnotateAvoidObstaclesOnMap(start, scan, step2);

						if (j == 1)
						{
							if ((scan > 0) && (scan < nearestWL)) nearestWL = scan;
						}
						else
						{
							if ((scan > 0) && (scan < nearestWR)) nearestWR = scan;
						}
					}
				}
				wingDrawFlagL = nearestWL != infinity;
				wingDrawFlagR = nearestWR != infinity;
			}

			// for annotation
			savedNearestWR = (float)nearestWR;
			savedNearestR = (float)nearestR;
			savedNearestL = (float)nearestL;
			savedNearestWL = (float)nearestWL;

			// flags for compound conditions, used below
			bool obstacleFreeC = nearestL == infinity && nearestR == infinity;
			bool obstacleFreeL = nearestL == infinity && nearestWL == infinity;
			bool obstacleFreeR = nearestR == infinity && nearestWR == infinity;
			bool obstacleFreeWL = nearestWL == infinity;
			bool obstacleFreeWR = nearestWR == infinity;
			bool obstacleFreeW = obstacleFreeWL && obstacleFreeWR;

			// when doing curved steering and we have already detected "just
			// scarping" but neither wing is free, recind the "just scarping"
			// QQQ temporary global QQQoaJustScraping
			bool JS = curvedSteering && QQQoaJustScraping;
			bool cancelJS = !obstacleFreeWL && !obstacleFreeWR;
			if (JS && cancelJS) QQQoaJustScraping = false;


			// ----------------------------------------------------------
			// now we have measured everything, decide which way to steer
			// ----------------------------------------------------------


			// no obstacles found on path, return zero steering
			if (obstacleFreeC)
			{
				qqqLastNearestObstacle = Vector3.Zero;
				AnnotationNoteOAClauseName("obstacleFreeC");

				// qqq  this may be in the wrong place (what would be the right
				// qqq  place?!) but I'm trying to say "even if the path is
				// qqq  clear, don't go too fast when driving between obstacles
				if (obstacleFreeWL || obstacleFreeWR || RelativeSpeed() < 0.7f)
					return Vector3.Zero;
				else
					return -Forward;
			}

			// if the nearest obstacle is way out there, take hint if any
			//      if (hintGiven && (Math.Min (nearestL, nearestR) > (maxSamples * 0.8f)))
			if (hintGiven && (Math.Min((float)nearestL, (float)nearestR) > (maxSamples * 0.8f)))
			{
				AnnotationNoteOAClauseName("nearest obstacle is way out there");
				AnnotationHintWasTaken();
				if (Vector3.Dot(steerHint, Side) > 0)
					return Side;
				else
					return -Side;
			}

			// QQQ experiment 3-9-04
			//
			// since there are obstacles ahead, if we are already near
			// maximum curvature, we MUST turn in opposite direction
			//
			// are we turning more sharply than the minimum turning radius?
			// (code from adjustSteeringForMinimumTurningRadius)
			float maxCurvature = 1 / (MinimumTurningRadius() * 1.2f);
			if (Math.Abs(Curvature) > maxCurvature)
			{
				Color blue = new Color(0, 0, (byte)(255.0f * 0.8f));
				AnnotationNoteOAClauseName("min turn radius");
				annotation.CircleOrDisk(MinimumTurningRadius() * 1.2f, Up,
										center, blue, 40, false, false);
				return Side * sign;
			}

			// if either side is obstacle-free, turn in that direction
			if (obstacleFreeL || obstacleFreeR)
				AnnotationNoteOAClauseName("obstacle-free side");

			if (obstacleFreeL) return Side;
			if (obstacleFreeR) return -Side;

			// if wings are clear, turn away from nearest obstacle straight ahead
			if (obstacleFreeW)
			{
				AnnotationNoteOAClauseName("obstacleFreeW");
				// distance to obs on L and R side of corridor roughtly the same
				bool same = Math.Abs(nearestL - nearestR) < 5; // within 5
				// if they are about the same and a hint is given, use hint
				if (same && hintGiven)
				{
					AnnotationHintWasTaken();
					if (Vector3.Dot(steerHint, Side) > 0)
						return Side;
					else
						return -Side;
				}
				else
				{
					// otherwise steer toward the less cluttered side
					if (nearestL > nearestR)
						return Side;
					else
						return -Side;
				}
			}

			// if the two wings are about equally clear and a steering hint is
			// provided, use it
			bool equallyClear = Math.Abs(nearestWL - nearestWR) < 2; // within 2
			if (equallyClear && hintGiven)
			{
				AnnotationNoteOAClauseName("equallyClear");
				AnnotationHintWasTaken();
				if (Vector3.Dot(steerHint, Side) > 0) return Side; else return -Side;
			}

			// turn towards the side whose "wing" region is less cluttered
			// (the wing whose nearest obstacle is furthest away)
			AnnotationNoteOAClauseName("wing less cluttered");
			if (nearestWL > nearestWR) return Side; else return -Side;
		}

		// QQQ reconsider calling sequence
		// called when steerToAvoidObstaclesOnMap decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public void AnnotateAvoidObstaclesOnMap(Vector3 scanOrigin, int scanIndex, Vector3 scanStep)
		{
			if (scanIndex > 0)
			{
				Vector3 hit = scanOrigin + (scanStep * (float)scanIndex);
				annotation.Line(scanOrigin, hit, new Color((byte)(255.0f * 0.7f), (byte)(255.0f * 0.3f), (byte)(255.0f * 0.3f)));
			}
		}

		public void AnnotationNoteOAClauseName(String clauseName)
		{
			// does noting now, idea was that it might draw 2d text near vehicle
			// with this state information
			//

			// print version:
			//
			// if (!dtZero) std.cout << clauseName << std.endl;

			// was had been in caller:
			//
			//if (!dtZero)
			//{
			//    int WR = nearestWR; debugPrint (WR);
			//    int R  = nearestR;  debugPrint (R);
			//    int L  = nearestL;  debugPrint (L);
			//    int WL = nearestWL; debugPrint (WL);
			//} 
		}

		public void AnnotationHintWasTaken()
		{
			if (!dtZero) hintTakenCount++;

			float r = halfWidth * 0.9f;
			Vector3 ff = Forward * r;
			Vector3 ss = Side * r;
			Vector3 pp = Position + (Up * 0.2f);
			annotation.Line(pp + ff + ss, pp - ff + ss, Color.White);
			annotation.Line(pp - ff - ss, pp - ff + ss, Color.White);
			annotation.Line(pp - ff - ss, pp + ff - ss, Color.White);
			annotation.Line(pp + ff + ss, pp + ff - ss, Color.White);

			//OpenSteerDemo.clock.setPausedState (true);
		}

		// scan across the obstacle map along a given arc
		// (possibly with radius adjustment ramp)
		// returns approximate distance to first obstacle found
		//
		// QQQ 1: this calling sequence does not allow for zero curvature case
		// QQQ 2: in library version of this, "map" should be a parameter
		// QQQ 3: instead of passing in colors, call virtual annotation function?
		// QQQ 4: need flag saying to continue after a hit, for annotation
		// QQQ 5: I needed to return both distance-to and position-of the first
		//        obstacle. I added returnObstaclePosition but maybe this should
		//        return a "scan results object" with a flag for obstacle found,
		//        plus distant and position if so.
		//
		public float ScanObstacleMap(Vector3 start, Vector3 center, float arcAngle, int segments, float endRadiusChange, Color beforeColor, Color afterColor, out Vector3 returnObstaclePosition)
		{
			// "spoke" is initially the vector from center to start,
			// which is then rotated step by step around center
			Vector3 spoke = start - center;
			// determine the angular step per segment
			float step = arcAngle / segments;
			// store distance to, and position of first obstacle
			float obstacleDistance = 0;
			returnObstaclePosition = Vector3.Zero;
			// for spiral "ramps" of changing radius
			float startRadius = (endRadiusChange == 0) ? 0 : spoke.Length();

			// traverse each segment along arc
			float sin = 0, cos = 0;
			Vector3 oldPoint = start;
			bool obstacleFound = false;
			for (int i = 0; i < segments; i++)
			{
				// rotate "spoke" to next step around circle
				// (sin and cos values get filled in on first call)
				spoke = Vector3Helpers.RotateAboutGlobalY(spoke, step, ref sin, ref cos);

				// for spiral "ramps" of changing radius
				float adjust = ((endRadiusChange == 0) ?
									  1.0f :
									  Utilities.Interpolate((float)(i + 1) / (float)segments,
												   1.0f,
												   (Math.Max(0,
															(startRadius +
															 endRadiusChange))
													/ startRadius)));

				// construct new scan point: center point, offset by rotated
				// spoke (possibly adjusting the radius if endRadiusChange!=0)
				Vector3 newPoint = center + (spoke * adjust);

				// once an obstacle if found "our work here is done" -- continue
				// to loop only for the sake of annotation (make that optional?)
				if (obstacleFound)
				{
					annotation.Line(oldPoint, newPoint, afterColor);
				}
				else
				{
					// no obstacle found on this scan so far,
					// scan map along current segment (a chord of the arc)
					Vector3 offset = newPoint - oldPoint;
					float d2 = offset.Length() * 2;

					// when obstacle found: set flag, save distance and position
					if (!map.IsPassable(newPoint))
					{
						obstacleFound = true;
						obstacleDistance = d2 * 0.5f * (i + 1);
						returnObstaclePosition = newPoint;
					}
					annotation.Line(oldPoint, newPoint, beforeColor);
				}
				// save new point for next time around loop
				oldPoint = newPoint;
			}
			// return distance to first obstacle (or zero if none found)
			return obstacleDistance;
		}

		public bool DetectImminentCollision()
		{
			// QQQ  this should be integrated into steerToAvoidObstaclesOnMap
			// QQQ  since it shares so much infrastructure
			// QQQ  less so after changes on 3-16-04
			bool returnFlag = false;
			float spacing = map.MinSpacing() / 2;
			float maxSide = halfWidth + spacing;
			float minDistance = curvedSteering ? 2.0f : 2.5f; // meters
			float predictTime = curvedSteering ? .75f : 1.3f; // seconds
			float maxForward = Speed * CombinedLookAheadTime(predictTime, minDistance);
			Vector3 step = Forward * spacing;
			float s = curvedSteering ? (spacing / 4) : (spacing / 2);

			float signedRadius = 1 / NonZeroCurvatureQQQ();
			Vector3 localCenterOfCurvature = Side * signedRadius;
			Vector3 center = Position + localCenterOfCurvature;
			float sign = signedRadius < 0 ? 1.0f : -1.0f;
			float arcRadius = signedRadius * -sign;
			float twoPi = 2 * (float)Math.PI;
			float circumference = twoPi * arcRadius;
			Vector3 qqqLift = new Vector3(0, 0.2f, 0);
			Vector3 ignore;

			// scan region ahead of vehicle
			while (s < maxSide)
			{
				Vector3 sOffset = Side * s;
				Vector3 lOffset = Position + sOffset;
				Vector3 rOffset = Position - sOffset;
				float bevel = 0.3f;
				float fraction = s / maxSide;
				float scanDist = (halfLength +
										Utilities.Interpolate(fraction,
													 maxForward,
													 maxForward * bevel));
				float angle = (scanDist * twoPi * sign) / circumference;
				int samples = (int)(scanDist / spacing);
				int L = (curvedSteering ?
							   (int)(ScanObstacleMap(lOffset + qqqLift,
													   center,
													   angle,
													   samples,
													   0,
													   Color.Magenta,
													   Color.Cyan,
													   out ignore)
									  / spacing) :
							   map.ScanXZray(lOffset, step, samples));
				int R = (curvedSteering ?
							   (int)(ScanObstacleMap(rOffset + qqqLift,
													   center,
													   angle,
													   samples,
													   0,
													   Color.Magenta,
													   Color.Cyan,
													   out ignore)
									  / spacing) :
							   map.ScanXZray(rOffset, step, samples));

				returnFlag = returnFlag || (L > 0);
				returnFlag = returnFlag || (R > 0);

				// annotation
				if (!curvedSteering)
				{
					Vector3 d = step * (float)samples;
					annotation.Line(lOffset, lOffset + d, Color.White);
					annotation.Line(rOffset, rOffset + d, Color.White);
				}

				// increment sideways displacement of scan line
				s += spacing;
			}
			return returnFlag;
		}

		// see comments at SimpleVehicle.predictFuturePosition, in this instance
		// I just need the future position (not a LocalSpace), so I'll keep the
		// calling sequence and just conditionalize its body
		//
		// this should be const, but easier for now to ignore that
		public override Vector3 PredictFuturePosition(float predictionTime)
		{
			if (curvedSteering)
			{
				// QQQ this chunk of code is repeated in far too many places,
				// QQQ it has to be moved inside some utility
				// QQQ 
				// QQQ and now, worse, I rearranged it to try the "limit arc
				// QQQ angle" trick
				float signedRadius = 1 / NonZeroCurvatureQQQ();
				Vector3 localCenterOfCurvature = Side * signedRadius;
				Vector3 center = Position + localCenterOfCurvature;
				float sign = signedRadius < 0 ? 1.0f : -1.0f;
				float arcRadius = signedRadius * -sign;
				float twoPi = 2 * (float)Math.PI;
				float circumference = twoPi * arcRadius;
				float rawLength = Speed * predictionTime * sign;
				float arcLength = ArcLengthLimit(rawLength, circumference * 0.25f);
				float arcAngle = twoPi * arcLength / circumference;

				Vector3 spoke = Position - center;
				Vector3 newSpoke = Vector3Helpers.RotateAboutGlobalY(spoke, arcAngle);
				Vector3 prediction = newSpoke + center;

				// QQQ unify with annotatePathFollowing
				Color futurePositionColor = new Color((byte)(255.0f * 0.5f), (byte)(255.0f * 0.5f), (byte)(255.0f * 0.6f));
				AnnotationXZArc(Position, center, arcLength, 20, futurePositionColor);
				return prediction;
			}
			else
			{
				return Position + (Velocity * predictionTime);
			}
		}

		// QQQ experimental fix for arcLength limit in predictFuturePosition
		// QQQ and steerToAvoidObstaclesOnMap.
		//
		// args are the intended arc length (signed!), and the limit which is
		// a given (positive!) fraction of the arc's (circle's) circumference
		//
		public float ArcLengthLimit(float length, float limit)
		{
			if (length > 0)
				return Math.Min(length, limit);
			else
				return -Math.Min(-length, limit);
		}

		// this is a version of the one in SteerLibrary.h modified for "slow when
		// heading off path".  I put it here because the changes were not
		// compatible with Pedestrians.cpp.  It needs to be merged back after
		// things settle down.
		//
		// its been modified in other ways too (such as "reduce the offset if
		// facing in the wrong direction" and "increase the target offset to
		// compensate the fold back") plus I changed the type of "path" from
		// Pathway to GCRoute to use methods like indexOfNearestSegment and
		// dotSegmentUnitTangents
		//
		// and now its been modified again for curvature-based prediction
		//
		public Vector3 SteerToFollowPath(int direction, float predictionTime, GCRoute path)
		{
			if (curvedSteering)
				return SteerToFollowPathCurve(direction, predictionTime, path);
			else
				return SteerToFollowPathLinear(direction, predictionTime, path);
		}

		public Vector3 SteerToFollowPathLinear(int direction, float predictionTime, GCRoute path)
		{
			// our goal will be offset from our path distance by this amount
			float pathDistanceOffset = direction * predictionTime * Speed;

			// predict our future position
			Vector3 futurePosition = PredictFuturePosition(predictionTime);

			// measure distance along path of our current and predicted positions
			float nowPathDistance =
				path.MapPointToPathDistance(Position);

			// are we facing in the correction direction?
			Vector3 pathHeading = path.TangentAt(Position) * (float)direction;
			bool correctDirection = Vector3.Dot(pathHeading, Forward) > 0;

			// find the point on the path nearest the predicted future position
			// XXX need to improve calling sequence, maybe change to return a
			// XXX special path-defined object which includes two Vector3s and a 
			// XXX bool (onPath,tangent (ignored), withinPath)
			float futureOutside;
			Vector3 onPath = path.MapPointToPath(futurePosition, out futureOutside);

			// determine if we are currently inside the path tube
			float nowOutside;
			Vector3 nowOnPath = path.MapPointToPath(Position, out nowOutside);

			// no steering is required if our present and future positions are
			// inside the path tube and we are facing in the correct direction
			float m = -Radius;
			bool whollyInside = (futureOutside < m) && (nowOutside < m);
			if (whollyInside && correctDirection)
			{
				// all is well, return zero steering
				return Vector3.Zero;
			}
			else
			{
				// otherwise we need to steer towards a target point obtained
				// by adding pathDistanceOffset to our current path position
				// (reduce the offset if facing in the wrong direction)
				float targetPathDistance = (nowPathDistance +
												  (pathDistanceOffset *
												   (correctDirection ? 1 : 0.1f)));
				Vector3 target = path.MapPathDistanceToPoint(targetPathDistance);


				// if we are on one segment and target is on the next segment and
				// the dot of the tangents of the two segments is negative --
				// increase the target offset to compensate the fold back
				int ip = path.IndexOfNearestSegment(Position);
				int it = path.IndexOfNearestSegment(target);
				if (((ip + direction) == it) &&
					(path.DotSegmentUnitTangents(it, ip) < -0.1f))
				{
					float newTargetPathDistance =
						nowPathDistance + (pathDistanceOffset * 2);
					target = path.MapPathDistanceToPoint(newTargetPathDistance);
				}

				AnnotatePathFollowing(futurePosition, onPath, target, futureOutside);

				// if we are currently outside head directly in
				// (QQQ new, experimental, makes it turn in more sharply)
				if (nowOutside > 0) return SteerForSeek(nowOnPath);

				// steering to seek target on path
				Vector3 seek = Vector3Helpers.TruncateLength(SteerForSeek(target), MaxForce);

				// return that seek steering -- except when we are heading off
				// the path (currently on path and future position is off path)
				// in which case we put on the brakes.
				if ((nowOutside < 0) && (futureOutside > 0))
					return (Vector3Helpers.PerpendicularComponent(seek, Forward) - (Forward * MaxForce));
				else
					return seek;
			}
		}

		// Path following case for curved prediction and incremental steering
		// (called from steerToFollowPath for the curvedSteering case)
		//
		// QQQ this does not handle the case when we AND futurePosition
		// QQQ are outside, say when approach the path from far away
		//
		public Vector3 SteerToFollowPathCurve(int direction, float predictionTime, GCRoute path)
		{
			// predict our future position (based on current curvature and speed)
			Vector3 futurePosition = PredictFuturePosition(predictionTime);
			// find the point on the path nearest the predicted future position
			float futureOutside;
			Vector3 onPath = path.MapPointToPath(futurePosition, out futureOutside);
			Vector3 pathHeading = path.TangentAt(onPath, direction);
			Vector3 rawBraking = Forward * MaxForce * -1;
			Vector3 braking = ((futureOutside < 0) ? Vector3.Zero : rawBraking);
			//qqq experimental wrong-way-fixer
			float nowOutside;
			Vector3 nowTangent = Vector3.Zero;
			Vector3 p = Position;
			Vector3 nowOnPath = path.MapPointToPath(p, out nowTangent, out nowOutside);
			nowTangent *= (float)direction;
			float alignedness = Vector3.Dot(nowTangent, Forward);

			// facing the wrong way?
			if (alignedness < 0)
			{
				annotation.Line(p, p + (nowTangent * 10), Color.Cyan);

				// if nearly anti-parallel
				if (alignedness < -0.707f)
				{
					Vector3 towardCenter = nowOnPath - p;
					Vector3 turn = (Vector3.Dot(towardCenter, Side) > 0 ?
									   Side * MaxForce :
									   Side * MaxForce * -1);
					return (turn + rawBraking);
				}
				else
				{
					return (Vector3Helpers.PerpendicularComponent(SteerTowardHeading(pathHeading), Forward) + braking);
				}
			}

			// is the predicted future position(+radius+margin) inside the path?
			if (futureOutside < -(Radius + 1.0f)) //QQQ
			{
				// then no steering is required
				return Vector3.Zero;
			}
			else
			{
				// otherwise determine corrective steering (including braking)
				annotation.Line(futurePosition, futurePosition + pathHeading, Color.Red);
				AnnotatePathFollowing(futurePosition, onPath,
									   Position, futureOutside);

				// two cases, if entering a turn (a waypoint between path segments)
				if (path.NearWaypoint(onPath) && (futureOutside > 0))
				{
					// steer to align with next path segment
					annotation.Circle3D(0.5f, futurePosition, Up, Color.Red, 8);
					return SteerTowardHeading(pathHeading) + braking;
				}
				else
				{
					// otherwise steer away from the side of the path we
					// are heading for
					Vector3 pathSide = LocalRotateForwardToSide(pathHeading);
					Vector3 towardFP = futurePosition - onPath;
					float whichSide = (Vector3.Dot(pathSide, towardFP) < 0) ? 1.0f : -1.0f;
					return (Side * MaxForce * whichSide) + braking;
				}
			}
		}

		public void PerFrameAnnotation()
		{
			Vector3 p = Position;

			// draw the circular collision boundary
			annotation.CircleOrDisk(Radius, Up, p, Color.Black, 32, false, false);

			// draw forward sensing corridor and wings ( for non-curved case)
			if (!curvedSteering)
			{
				float corLength = Speed * LookAheadTimeOA();
				if (corLength > halfLength)
				{
					Vector3 corFront = Forward * corLength;
					Vector3 corBack = Vector3.Zero; // (was bbFront)
					Vector3 corSide = Side * Radius;
					Vector3 c1 = p + corSide + corBack;
					Vector3 c2 = p + corSide + corFront;
					Vector3 c3 = p - corSide + corFront;
					Vector3 c4 = p - corSide + corBack;
					Color color = ((annotateAvoid != Vector3.Zero) ? Color.Red : Color.Yellow);
					annotation.Line(c1, c2, color);
					annotation.Line(c2, c3, color);
					annotation.Line(c3, c4, color);

					// draw sensing "wings"
					Vector3 wingWidth = Side * WingSlope() * corLength;
					Vector3 wingTipL = c2 + wingWidth;
					Vector3 wingTipR = c3 - wingWidth;
					Color wingColor = Color.Orange;
					if (wingDrawFlagL) annotation.Line(c2, wingTipL, wingColor);
					if (wingDrawFlagL) annotation.Line(c1, wingTipL, wingColor);
					if (wingDrawFlagR) annotation.Line(c3, wingTipR, wingColor);
					if (wingDrawFlagR) annotation.Line(c4, wingTipR, wingColor);
				}
			}

			// annotate steering acceleration
			Vector3 above = Position + new Vector3(0, 0.2f, 0);
			Vector3 accel = Acceleration * 5 / MaxForce;
			Color aColor = new Color((byte)(255.0f * 0.4f), (byte)(255.0f * 0.4f), (byte)(255.0f * 0.8f));
			annotation.Line(above, above + accel, aColor);
		}

		// draw vehicle's body and annotation
		public void Draw()
		{
			// for now: draw as a 2d bounding box on the ground
			Color bodyColor = Color.Black;
			if (stuck) bodyColor = Color.Yellow;
			if (!IsBodyInsidePath()) bodyColor = Color.Orange;
			if (collisionDetected) bodyColor = Color.Red;

			// draw vehicle's bounding box on gound plane (its "shadow")
			Vector3 p = Position;
			Vector3 bbSide = Side * halfWidth;
			Vector3 bbFront = Forward * halfLength;
			Vector3 bbHeight = new Vector3(0, 0.1f, 0);
			Drawing.DrawQuadrangle(p - bbFront + bbSide + bbHeight,
							p + bbFront + bbSide + bbHeight,
							p + bbFront - bbSide + bbHeight,
							p - bbFront - bbSide + bbHeight,
							bodyColor);

			// annotate trail
			Color darkGreen = new Color(0, (byte)(255.0f * 0.6f), 0);
			trail.TrailColor = darkGreen;
			trail.TickColor = Color.Black;
			trail.Draw(Annotation.drawer);
		}

		// called when steerToFollowPath decides steering is required
		public void AnnotatePathFollowing(Vector3 future, Vector3 onPath, Vector3 target, float outside)
		{
			Color toTargetColor = new Color(0, (byte)(255.0f * 0.6f), 0);
			Color insidePathColor = new Color((byte)(255.0f * 0.6f), (byte)(255.0f * 0.6f), 0);
			Color outsidePathColor = new Color(0, 0, (byte)(255.0f * 0.6f));
			Color futurePositionColor = new Color((byte)(255.0f * 0.5f), (byte)(255.0f * 0.5f), (byte)(255.0f * 0.6f));

			// draw line from our position to our predicted future position
			if (!curvedSteering)
				annotation.Line(Position, future, futurePositionColor);

			// draw line from our position to our steering target on the path
			annotation.Line(Position, target, toTargetColor);

			// draw a two-toned line between the future test point and its
			// projection onto the path, the change from dark to light color
			// indicates the boundary of the tube.

			float o = outside + Radius + (curvedSteering ? 1.0f : 0.0f);
			Vector3 boundaryOffset = (onPath - future);
			boundaryOffset.Normalize();
			boundaryOffset *= o;

			Vector3 onPathBoundary = future + boundaryOffset;
			annotation.Line(onPath, onPathBoundary, insidePathColor);
			annotation.Line(onPathBoundary, future, outsidePathColor);
		}

		public void DrawMap()
		{
			float xs = map.xSize / (float)map.resolution;
			float zs = map.zSize / (float)map.resolution;
			Vector3 alongRow = new Vector3(xs, 0, 0);
			Vector3 nextRow = new Vector3(-map.xSize, 0, zs);
			Vector3 g = new Vector3((map.xSize - xs) / -2, 0, (map.zSize - zs) / -2);
			g += map.center;
			for (int j = 0; j < map.resolution; j++)
			{
				for (int i = 0; i < map.resolution; i++)
				{
					if (map.GetMapBit(i, j))
					{
						// spikes
						// Vector3 spikeTop (0, 5.0f, 0);
						// drawLine (g, g+spikeTop, Color.White);

						// squares
						float rockHeight = 0;
						Vector3 v1 = new Vector3(+xs / 2, rockHeight, +zs / 2);
						Vector3 v2 = new Vector3(+xs / 2, rockHeight, -zs / 2);
						Vector3 v3 = new Vector3(-xs / 2, rockHeight, -zs / 2);
						Vector3 v4 = new Vector3(-xs / 2, rockHeight, +zs / 2);
						// Vector3 redRockColor (0.6f, 0.1f, 0.0f);
						Color orangeRockColor = new Color((byte)(255.0f * 0.5f), (byte)(255.0f * 0.2f), (byte)(255.0f * 0.0f));
						Drawing.DrawQuadrangle(g + v1, g + v2, g + v3, g + v4, orangeRockColor);

						// pyramids
						// Vector3 top (0, xs/2, 0);
						// Vector3 redRockColor (0.6f, 0.1f, 0.0f);
						// Vector3 orangeRockColor (0.5f, 0.2f, 0.0f);
						// drawTriangle (g+v1, g+v2, g+top, redRockColor);
						// drawTriangle (g+v2, g+v3, g+top, orangeRockColor);
						// drawTriangle (g+v3, g+v4, g+top, redRockColor);
						// drawTriangle (g+v4, g+v1, g+top, orangeRockColor);
					}
					g += alongRow;
				}
				g += nextRow;
			}
		}

		// draw the GCRoute as a series of circles and "wide lines"
		// (QQQ this should probably be a method of Path (or a
		// closely-related utility function) in which case should pass
		// color in, certainly shouldn't be recomputing it each draw)
		public void DrawPath()
		{
			Vector3 pathColor = new Vector3(0, 0.5f, 0.5f);
			Vector3 sandColor = new Vector3(0.8f, 0.7f, 0.5f);
			Vector3 vColor = Utilities.Interpolate(0.1f, sandColor, pathColor);
			Color color = new Color(vColor);

			Vector3 down = new Vector3(0, -0.1f, 0);
			for (int i = 0; i < path.pointCount; i++)
			{
				Vector3 endPoint0 = path.points[i] + down;
				if (i > 0)
				{
					Vector3 endPoint1 = path.points[i - 1] + down;

					float legWidth = path.radii[i];

					Drawing.DrawXZWideLine(endPoint0, endPoint1, color, legWidth * 2);
					Drawing.DrawLine(path.points[i], path.points[i - 1], new Color(pathColor));
					Drawing.DrawXZDisk(legWidth, endPoint0, color, 24);
					Drawing.DrawXZDisk(legWidth, endPoint1, color, 24);
				}
			}
		}

		public GCRoute MakePath()
		{
			// a few constants based on world size
			float m = worldSize * 0.4f; // main diamond size
			float n = worldSize / 8;    // notch size
			float o = worldSize * 2;    // outside of the sand

			// construction vectors
			Vector3 p = new Vector3(0, 0, m);
			Vector3 q = new Vector3(0, 0, m - n);
			Vector3 r = new Vector3(-m, 0, 0);
			Vector3 s = new Vector3(2 * n, 0, 0);
			Vector3 t = new Vector3(o, 0, 0);
			Vector3 u = new Vector3(-o, 0, 0);
			Vector3 v = new Vector3(n, 0, 0);
			Vector3 w = new Vector3(0, 0, 0);


			// path vertices
			Vector3 a = t - p;
			Vector3 b = s + v - p;
			Vector3 c = s - q;
			Vector3 d = s + q;
			Vector3 e = s - v + p;
			Vector3 f = p - w;
			Vector3 g = r - w;
			Vector3 h = -p - w;
			Vector3 i = u - p;

			// return Path object
			int pathPointCount = 9;
			Vector3[] pathPoints = new Vector3[] { a, b, c, d, e, f, g, h, i };
			float k = 10.0f;
			float[] pathRadii = new float[] { k, k, k, k, k, k, k, k, k };
			return new GCRoute(pathPointCount, pathPoints, pathRadii, false);
		}

		public TerrainMap MakeMap()
		{
			return new TerrainMap(Vector3.Zero, worldSize, worldSize, (int)worldSize + 1);
		}

		public bool HandleExitFromMap()
		{
			if (demoSelect == 2)
			{
				// for path following, do wrap-around (teleport) and make new map
				float px = Position.X;
				float fx = Forward.X;
				float ws = worldSize * 0.51f; // slightly past edge
				if (((fx > 0) && (px > ws)) || ((fx < 0) && (px < -ws)))
				{
					// bump counters
					lapsStarted++;
					lapsFinished++;

					Vector3 camOffsetBefore = Demo.Camera.Position - Position;

					// set position on other side of the map (set new X coordinate)
					SetPosition((((px < 0) ? 1 : -1) *
								  ((worldSize * 0.5f) +
								   (Speed * LookAheadTimePF()))),
								 Position.Y,
								 Position.Z);

					// reset bookeeping to detect stuck cycles
					ResetStuckCycleDetection();

					// new camera position and aimpoint to compensate for teleport
					Demo.Camera.Target = Position;
					Demo.Camera.Position = (Position + camOffsetBefore);

					// make camera jump immediately to new position
					Demo.Camera.DoNotSmoothNextMove();

					// prevent long streaks due to teleportation 
					trail.Clear();

					return true;
				}
			}
			else
			{
				// for the non-path-following demos:
				// reset simulation if the vehicle drives through the fence
				if (Position.Length() > worldDiag) Reset();
			}
			return false;
		}


		// QQQ move this utility to SimpleVehicle?
		public float RelativeSpeed() { return Speed / MaxSpeed; }

		public float WingSlope()
		{
			return Utilities.Interpolate(RelativeSpeed(),
								(curvedSteering ? 0.3f : 0.35f),
								0.06f);
		}

		public void ResetStuckCycleDetection()
		{
			ResetSmoothedPosition(Position + (Forward * -80)); // qqq
		}

		// QQQ just a stop gap, not quite right
		// (say for example we were going around a circle with radius > 10)
		public bool WeAreGoingInCircles()
		{
			Vector3 offset = SmoothedPosition - Position;
			return offset.Length() < 10;
		}

		public float LookAheadTimeOA()
		{
			float minTime = (baseLookAheadTime *
								   (curvedSteering ?
									Utilities.Interpolate(RelativeSpeed(), 0.4f, 0.7f) :
									0.66f));
			return CombinedLookAheadTime(minTime, 3);
		}

		public float LookAheadTimePF()
		{
			return CombinedLookAheadTime(baseLookAheadTime, 3);
		}

		// QQQ maybe move to SimpleVehicle ?
		// compute a "look ahead time" with two components, one based on
		// minimum time to (say) a collision and one based on minimum distance
		// arg 1 is "seconds into the future", arg 2 is "meters ahead"
		public float CombinedLookAheadTime(float minTime, float minDistance)
		{
			if (Speed == 0) return 0;
			return Math.Max(minTime, minDistance / Speed);
		}

		// is vehicle body inside the path?
		// (actually tests if all four corners of the bounbding box are inside)
		//
		public bool IsBodyInsidePath()
		{
			if (demoSelect == 2)
			{
				Vector3 bbSide = Side * halfWidth;
				Vector3 bbFront = Forward * halfLength;
				return (path.IsInsidePath(Position - bbFront + bbSide) &&
						path.IsInsidePath(Position + bbFront + bbSide) &&
						path.IsInsidePath(Position + bbFront - bbSide) &&
						path.IsInsidePath(Position - bbFront - bbSide));
			}
			return true;
		}

		public Vector3 ConvertAbsoluteToIncrementalSteering(Vector3 absolute, float elapsedTime)
		{
			Vector3 curved = ConvertLinearToCurvedSpaceGlobal(absolute);
			Utilities.BlendIntoAccumulator(elapsedTime * 8.0f, curved, ref currentSteering);
			{
				// annotation
				Vector3 u = new Vector3(0, 0.5f, 0);
				Vector3 p = Position;
				annotation.Line(p + u, p + u + absolute, Color.Red);
				annotation.Line(p + u, p + u + curved, Color.Yellow);
				annotation.Line(p + u * 2, p + u * 2 + currentSteering, Color.Green);
			}
			return currentSteering;
		}

		// QQQ new utility 2-25-04 -- may replace inline code elsewhere
		//
		// Given a location in this vehicle's linear local space, convert it into
		// the curved space defined by the vehicle's current path curvature.  For
		// example, forward() gets mapped on a point 1 unit along the circle
		// centered on the current center of curvature and passing through the
		// vehicle's position().
		//
		public Vector3 ConvertLinearToCurvedSpaceGlobal(Vector3 linear)
		{
			Vector3 trimmedLinear = Vector3Helpers.TruncateLength(linear, MaxForce);

			// ---------- this block imported from steerToAvoidObstaclesOnMap
			float signedRadius = 1 / (NonZeroCurvatureQQQ() /*QQQ*/ * 1);
			Vector3 localCenterOfCurvature = Side * signedRadius;
			Vector3 center = Position + localCenterOfCurvature;
			float sign = signedRadius < 0 ? 1.0f : -1.0f;
			float arcLength = Vector3.Dot(trimmedLinear, Forward);
			//
			float arcRadius = signedRadius * -sign;
			float twoPi = 2 * (float)Math.PI;
			float circumference = twoPi * arcRadius;
			float arcAngle = twoPi * arcLength / circumference;
			// ---------- this block imported from steerToAvoidObstaclesOnMap

			// ---------- this block imported from scanObstacleMap
			// vector from center of curvature to position of vehicle
			Vector3 initialSpoke = Position - center;
			// rotate by signed arc angle
			Vector3 spoke = Vector3Helpers.RotateAboutGlobalY(initialSpoke, arcAngle * sign);
			// ---------- this block imported from scanObstacleMap

			Vector3 fromCenter = -localCenterOfCurvature;
			fromCenter.Normalize();
			float dRadius = Vector3.Dot(trimmedLinear, fromCenter);
			float radiusChangeFactor = (dRadius + arcRadius) / arcRadius;
			Vector3 resultLocation = center + (spoke * radiusChangeFactor);
			{
				Vector3 center2 = Position + localCenterOfCurvature;
				AnnotationXZArc(Position, center2, Speed * sign * -3, 20, Color.White);
			}
			// return the vector from vehicle position to the coimputed location
			// of the curved image of the original linear offset
			return resultLocation - Position;
		}

		// approximate value for the Polaris Ranger 6x6: 16 feet, 5 meters
		public float MinimumTurningRadius() { return 5.0f; }

		public Vector3 AdjustSteeringForMinimumTurningRadius(Vector3 steering)
		{
			float maxCurvature = 1 / (MinimumTurningRadius() * 1.1f);

			// are we turning more sharply than the minimum turning radius?
			if (Math.Abs(Curvature) > maxCurvature)
			{
				// remove the tangential (non-thrust) component of the steering
				// force, replace it with a force pointing away from the center
				// of curvature, causing us to "widen out" easing off from the
				// minimum turing radius
				float signedRadius = 1 / NonZeroCurvatureQQQ();
				float sign = signedRadius < 0 ? 1.0f : -1.0f;
				Vector3 thrust = Vector3Helpers.ParallelComponent(steering, Forward);
				Vector3 trimmed = Vector3Helpers.TruncateLength(thrust, MaxForce);
				Vector3 widenOut = Side * MaxForce * sign;
				{
					// annotation
					Vector3 localCenterOfCurvature = Side * signedRadius;
					Vector3 center = Position + localCenterOfCurvature;
					annotation.CircleOrDisk(MinimumTurningRadius(), Up,
											center, Color.Blue, 40, false, false);
				}
				return trimmed + widenOut;
			}

			// otherwise just return unmodified input
			return steering;
		}

		// QQQ This is to work around the bug that scanObstacleMap's current
		// QQQ arguments preclude the driving straight [curvature()==0] case.
		// QQQ This routine returns the current vehicle path curvature, unless it
		// QQQ is *very* close to zero, in which case a small positive number is
		// QQQ returned (corresponding to a radius of 100,000 meters).  
		// QQQ
		// QQQ Presumably it would be better to get rid of this routine and
		// QQQ redesign the arguments of scanObstacleMap
		//
		public float NonZeroCurvatureQQQ()
		{
			float c = Curvature;
			float minCurvature = 1.0f / 100000.0f; // 100,000 meter radius
			bool tooSmall = (c < minCurvature) && (c > -minCurvature);
			return (tooSmall ? minCurvature : c);
		}

		// QQQ ad hoc speed limitation based on path orientation...
		// QQQ should be renamed since it is based on more than curvature
		//
		public float MaxSpeedForCurvature()
		{
			float maxRelativeSpeed = 1;

			if (curvedSteering)
			{
				// compute an ad hoc "relative curvature"
				float absC = Math.Abs(Curvature);
				float maxC = 1 / MinimumTurningRadius();
				float relativeCurvature = (float)Math.Sqrt(Utilities.Clip(absC / maxC, 0, 1));

				// map from full throttle when straight to 10% at max curvature
				float curveSpeed = Utilities.Interpolate(relativeCurvature, 1.0f, 0.1f);
				annoteMaxRelSpeedCurve = curveSpeed;

				if (demoSelect != 2)
				{
					maxRelativeSpeed = curveSpeed;
				}
				else
				{
					// heading (unit tangent) of the path segment of interest
					Vector3 pathHeading = path.TangentAt(Position, pathFollowDirection);
					// measure how parallel we are to the path
					float parallelness = Vector3.Dot(pathHeading, Forward);

					// determine relative speed for this heading
					float mw = 0.2f;
					float headingSpeed = ((parallelness < 0) ? mw :
												Utilities.Interpolate(parallelness, mw, 1.0f));
					maxRelativeSpeed = Math.Min(curveSpeed, headingSpeed);
					annoteMaxRelSpeedPath = headingSpeed;
				}
			}
			annoteMaxRelSpeed = maxRelativeSpeed;
			return MaxSpeed * maxRelativeSpeed;
		}

		// xxx library candidate
		// xxx assumes (but does not check or enforce) heading is unit length
		//
		public Vector3 SteerTowardHeading(Vector3 desiredGlobalHeading)
		{
			Vector3 headingError = desiredGlobalHeading - Forward;
			headingError.Normalize();
			headingError *= MaxForce;

			return headingError;
		}

		// XXX this should eventually be in a library, make it a first
		// XXX class annotation queue, tie in with drawXZArc
		public void AnnotationXZArc(Vector3 start, Vector3 center, float arcLength, int segments, Color color)
		{
			// "spoke" is initially the vector from center to start,
			// it is then rotated around its tail
			Vector3 spoke = start - center;

			// determine the angular step per segment
			float radius = spoke.Length();
			float twoPi = 2 * (float)Math.PI;
			float circumference = twoPi * radius;
			float arcAngle = twoPi * arcLength / circumference;
			float step = arcAngle / segments;

			// draw each segment along arc
			float sin = 0, cos = 0;
			for (int i = 0; i < segments; i++)
			{
				Vector3 old = spoke + center;

				// rotate point to next step around circle
				spoke = Vector3Helpers.RotateAboutGlobalY(spoke, step, ref sin, ref cos);

				annotation.Line(spoke + center, old, color);
			}
		}

		// map of obstacles
		public TerrainMap map;

		// route for path following (waypoints and legs)
		public GCRoute path;

		// follow the path "upstream or downstream" (+1/-1)
		public int pathFollowDirection;

		// master look ahead (prediction) time
		public float baseLookAheadTime;

		// vehicle dimentions in meters
		public float halfWidth;
		public float halfLength;

		// keep track of failure rate (when vehicle is on top of obstacle)
		public bool collisionDetected;
		public bool collisionLastTime;
		public float timeOfLastCollision;
		public float sumOfCollisionFreeTimes;
		public int countOfCollisionFreeTimes;

		// keep track of average speed
		public float totalDistance;
		public float totalTime;

		// keep track of path following failure rate
		// (these are probably obsolete now, replaced by stuckOffPathCount)
		public float pathFollowTime;
		public float pathFollowOffTime;

		// take note when current dt is zero (as in paused) for stat counters
		public bool dtZero;

		// state saved for annotation
		public Vector3 annotateAvoid;
		public bool wingDrawFlagL, wingDrawFlagR;

		// QQQ first pass at detecting "stuck" state
		public bool stuck;
		public int stuckCount;
		public int stuckCycleCount;
		public int stuckOffPathCount;

		public Vector3 qqqLastNearestObstacle;

		public int lapsStarted;
		public int lapsFinished;

		// QQQ temporary global QQQoaJustScraping
		// QQQ replace this global flag with a cleaner mechanism
		public bool QQQoaJustScraping;

		public int hintGivenCount;
		public int hintTakenCount;

		// for "curvature-based incremental steering" -- contains the current
		// steering into which new incremental steering is blended
		public Vector3 currentSteering;

		// use curved prediction and incremental steering:
		public bool curvedSteering;
		public bool incrementalSteering;

		// save obstacle avoidance stats for annotation
		// (nearest obstacle in each of the four zones)
		public static float savedNearestWR = 0;
		public static float savedNearestR = 0;
		public static float savedNearestL = 0;
		public static float savedNearestWL = 0;

		public float annoteMaxRelSpeed;
		public float annoteMaxRelSpeedCurve;
		public float annoteMaxRelSpeedPath;

		// which of the three demo modes is selected
		public static int demoSelect = 2;

		// size of the world (the map actually)
		public static float worldSize = 200;
		public static float worldDiag = (float)Math.Sqrt(Utilities.Square(worldSize) / 2);
	}
}
