// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using Microsoft.Xna.Framework;

namespace Bnoerj.AI.Steering
{
	public struct Vec3
	{
		// names for frequently used vector constants
		public static readonly Vec3 Zero = new Vec3(0, 0, 0);
		public static readonly Vec3 Side = new Vec3(-1, 0, 0);
		public static readonly Vec3 Up = new Vec3(0, 1, 0);
		public static readonly Vec3 Forward = new Vec3(0, 0, 1);

		// generic 3d vector operations

		// three-dimensional Cartesian coordinates
		public float X;
		public float Y;
		public float Z;

		// constructors
		public Vec3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}
		public Vec3(Vec3 other)
		{
			X = other.X;
			Y = other.Y;
			Z = other.Z;
		}

		// vector addition
		public static Vec3 operator +(Vec3 value1, Vec3 value2)
		{
			return new Vec3(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z);
		}

		// vector subtraction
		public static Vec3 operator -(Vec3 value1, Vec3 value2)
		{
			return new Vec3(value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z);
		}

		// unary minus
		public static Vec3 operator -(Vec3 value)
		{
			return new Vec3(-value.X, -value.Y, -value.Z);
		}

		// vector times scalar product(scale length of vector times argument)
		public static Vec3 operator *(float scaleFactor, Vec3 value)
		{
			return new Vec3(value.X * scaleFactor, value.Y * scaleFactor, value.Z * scaleFactor);
		}
		public static Vec3 operator *(Vec3 value, float scaleFactor)
		{
			return new Vec3(value.X * scaleFactor, value.Y * scaleFactor, value.Z * scaleFactor);
		}

		// vector divided by a scalar(divide length of vector by argument)
		public static Vec3 operator /(Vec3 value, float divider)
		{
			return new Vec3(value.X / divider, value.Y / divider, value.Z / divider);
		}

		// dot product
		public float Dot(Vec3 vector2)
		{
			return (X * vector2.X) + (Y * vector2.Y) + (Z * vector2.Z);
		}

		// length
		public float Length()
		{
			return (float)Math.Sqrt(LengthSquared());
		}

		// length squared
		public float LengthSquared()
		{
			return Dot(this);
		}

		// normalize: returns normalized version(parallel to this, length = 1)
		public Vec3 Normalize()
		{
			// skip divide if length is zero
			float len = Length();
			return len > 0 ? this / len : this;
		}

		// cross product (modify "this" to be A x B)
		// [XXX  side effecting -- deprecate this function?  XXX]
		//FIXME: make this a static returning the Cross product
		public void Cross(Vec3 vector1, Vec3 vector2)
		{
			this = new Vec3((vector1.Y * vector2.Z) - (vector1.Z * vector2.Y), (vector1.Z * vector2.X) - (vector1.X * vector2.Z), (vector1.X * vector2.Y) - (vector1.Y * vector2.X));
		}

		// set XYZ coordinates to given three floats
		public Vec3 Set(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
			return this;
		}

		// equality/inequality
		public static bool operator ==(Vec3 value1, Vec3 value2)
		{
			return value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
		}
		public static bool operator !=(Vec3 value1, Vec3 value2)
		{
			return !(value1 == value2);
		}

		public static float Distance(Vec3 vector1, Vec3 vector2)
		{
			return (vector1 - vector2).Length();
		}

		// utility member functions used in OpenSteer

		// return component of vector parallel to a unit basis vector
		// IMPORTANT NOTE: assumes "basis" has unit magnitude (length == 1)
		public Vec3 ParallelComponent(Vec3 unitBasis)
		{
			float projection = Dot(unitBasis);
			return unitBasis * projection;
		}

		// return component of vector perpendicular to a unit basis vector
		// IMPORTANT NOTE: assumes "basis" has unit magnitude(length==1)
		public Vec3 PerpendicularComponent(Vec3 unitBasis)
		{
			return (this) - ParallelComponent(unitBasis);
		}

		// clamps the length of a given vector to maxLength.  If the vector is
		// shorter its value is returned unaltered, if the vector is longer
		// the value returned has length of maxLength and is paralle to the
		// original input.
		public Vec3 TruncateLength(float maxLength)
		{
			float maxLengthSquared = maxLength * maxLength;
			float vecLengthSquared = LengthSquared();
			if (vecLengthSquared <= maxLengthSquared)
				return this;
			else
				return (this) * (maxLength / (float)Math.Sqrt(vecLengthSquared));
		}

		// forces a 3d position onto the XZ (aka y=0) plane
		//FIXME: Misleading name
		public Vec3 SetYToZero()
		{
			return new Vec3(X, 0, Z);
		}

		// rotate this vector about the global Y (up) axis by the given angle
		public Vec3 RotateAboutGlobalY(float angle)
		{
			float s = (float)Math.Sin(angle);
			float c = (float)Math.Cos(angle);
			return new Vec3((this.X * c) + (this.Z * s), (this.Y), (this.Z * c) - (this.X * s));
		}

		// version for caching sin/cos computation
		public Vec3 RotateAboutGlobalY(float angle, ref float sin, ref float cos)
		{
			// is both are zero, they have not be initialized yet
			if (sin == 0 && cos == 0)
			{
				sin = (float)Math.Sin(angle);
				cos = (float)Math.Cos(angle);
			}
			return new Vec3((X * cos) + (Z * sin), Y, (Z * cos) - (X * sin));
		}

		// if this position is outside sphere, push it back in by one diameter
		public Vec3 SphericalWraparound(Vec3 center, float radius)
		{
			Vec3 offset = this - center;
			float r = offset.Length();
			if (r > radius)
				return this + ((offset / r) * radius * -2);
			else
				return this;
		}

		public Vector3 ToVector3()
		{
			return new Vector3(X, Y, Z);
		}

		public override bool Equals(Object obj)
		{
			return this == (Vec3)obj;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// ----------------------------------------------------------------------------
		// Returns a position randomly distributed on a disk of unit radius
		// on the XZ (Y=0) plane, centered at the origin.  Orientation will be
		// random and length will range between 0 and 1
		public static Vec3 RandomVectorOnUnitRadiusXZDisk()
		{
			Vec3 v = new Vec3();
			do
			{
				v.Set((Utilities.Random() * 2) - 1, 0, (Utilities.Random() * 2) - 1);
			}
			while (v.Length() >= 1);

			return v;
		}

		// Returns a position randomly distributed inside a sphere of unit radius
		// centered at the origin.  Orientation will be random and length will range
		// between 0 and 1
		public static Vec3 RandomVectorInUnitRadiusSphere()
		{
			Vec3 v = new Vec3();
			do
			{
				v.Set((Utilities.Random() * 2) - 1, (Utilities.Random() * 2) - 1, (Utilities.Random() * 2) - 1);
			}
			while (v.Length() >= 1);

			return v;
		}

		// ----------------------------------------------------------------------------
		// Returns a position randomly distributed on the surface of a sphere
		// of unit radius centered at the origin.  Orientation will be random
		// and length will be 1
		public static Vec3 RandomUnitVector()
		{
			return RandomVectorInUnitRadiusSphere().Normalize();
		}

		// ----------------------------------------------------------------------------
		// Returns a position randomly distributed on a circle of unit radius
		// on the XZ (Y=0) plane, centered at the origin.  Orientation will be
		// random and length will be 1
		public static Vec3 RandomUnitVectorOnXZPlane()
		{
			return RandomVectorInUnitRadiusSphere().SetYToZero().Normalize();
		}

		// ----------------------------------------------------------------------------
		// used by limitMaxDeviationAngle / limitMinDeviationAngle below
		public static Vec3 LimitDeviationAngleUtility(bool insideOrOutside, Vec3 source, float cosineOfConeAngle, Vec3 basis)
		{
			// immediately return zero length input vectors
			float sourceLength = source.Length();
			if (sourceLength == 0) return source;

			// measure the angular diviation of "source" from "basis"
			Vec3 direction = source / sourceLength;
			float cosineOfSourceAngle = direction.Dot(basis);

			// Simply return "source" if it already meets the angle criteria.
			// (note: we hope this top "if" gets compiled out since the flag
			// is a constant when the function is inlined into its caller)
			if (insideOrOutside)
			{
				// source vector is already inside the cone, just return it
				if (cosineOfSourceAngle >= cosineOfConeAngle) return source;
			}
			else
			{
				// source vector is already outside the cone, just return it
				if (cosineOfSourceAngle <= cosineOfConeAngle) return source;
			}

			// find the portion of "source" that is perpendicular to "basis"
			Vec3 perp = source.PerpendicularComponent(basis);

			// normalize that perpendicular
			Vec3 unitPerp = perp.Normalize();

			// construct a new vector whose length equals the source vector,
			// and lies on the intersection of a plane (formed the source and
			// basis vectors) and a cone (whose axis is "basis" and whose
			// angle corresponds to cosineOfConeAngle)
			float perpDist = (float)Math.Sqrt(1 - (cosineOfConeAngle * cosineOfConeAngle));
			Vec3 c0 = basis * cosineOfConeAngle;
			Vec3 c1 = unitPerp * perpDist;
			return (c0 + c1) * sourceLength;
		}

		// ----------------------------------------------------------------------------
		// Enforce an upper bound on the angle by which a given arbitrary vector
		// diviates from a given reference direction (specified by a unit basis
		// vector).  The effect is to clip the "source" vector to be inside a cone
		// defined by the basis and an angle.
		public static Vec3 LimitMaxDeviationAngle(Vec3 source, float cosineOfConeAngle, Vec3 basis)
		{
			return LimitDeviationAngleUtility(true, // force source INSIDE cone
				source, cosineOfConeAngle, basis);
		}

		// ----------------------------------------------------------------------------
		// Enforce a lower bound on the angle by which a given arbitrary vector
		// diviates from a given reference direction (specified by a unit basis
		// vector).  The effect is to clip the "source" vector to be outside a cone
		// defined by the basis and an angle.
		public static Vec3 LimitMinDeviationAngle(Vec3 source, float cosineOfConeAngle, Vec3 basis)
		{
			return LimitDeviationAngleUtility(false, // force source OUTSIDE cone
				source, cosineOfConeAngle, basis);
		}

		// ----------------------------------------------------------------------------
		// Returns the distance between a point and a line.  The line is defined in
		// terms of a point on the line ("lineOrigin") and a UNIT vector parallel to
		// the line ("lineUnitTangent")
		public static float DistanceFromLine(Vec3 point, Vec3 lineOrigin, Vec3 lineUnitTangent)
		{
			Vec3 offset = point - lineOrigin;
			Vec3 perp = offset.PerpendicularComponent(lineUnitTangent);
			return perp.Length();
		}

		// ----------------------------------------------------------------------------
		// given a vector, return a vector perpendicular to it (note that this
		// arbitrarily selects one of the infinitude of perpendicular vectors)
		public static Vec3 FindPerpendicularIn3d(Vec3 direction)
		{
			// to be filled in:
			Vec3 quasiPerp;  // a direction which is "almost perpendicular"
			Vec3 result = new Vec3();     // the computed perpendicular to be returned

			// three mutually perpendicular basis vectors
			Vec3 i = new Vec3(1, 0, 0);
			Vec3 j = new Vec3(0, 1, 0);
			Vec3 k = new Vec3(0, 0, 1);

			// measure the projection of "direction" onto each of the axes
			float id = i.Dot(direction);
			float jd = j.Dot(direction);
			float kd = k.Dot(direction);

			// set quasiPerp to the basis which is least parallel to "direction"
			if ((id <= jd) && (id <= kd))
			{
				quasiPerp = i;               // projection onto i was the smallest
			}
			else
			{
				if ((jd <= id) && (jd <= kd))
					quasiPerp = j;           // projection onto j was the smallest
				else
					quasiPerp = k;           // projection onto k was the smallest
			}

			// return the cross product (direction x quasiPerp)
			// which is guaranteed to be perpendicular to both of them
			result.Cross(direction, quasiPerp);
			return result;
		}
	}
}
