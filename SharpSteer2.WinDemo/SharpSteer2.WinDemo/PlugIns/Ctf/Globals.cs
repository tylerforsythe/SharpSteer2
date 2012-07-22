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

namespace Bnoerj.AI.Steering.Ctf
{
	class Globals
	{
		public static readonly Vector3 HomeBaseCenter = new Vector3(0, 0, 0);
		public const float HomeBaseRadius = 1.5f;

		public const float MinStartRadius = 30;
		public const float MaxStartRadius = 40;

		public const float BrakingRate = 0.75f;

		public static readonly Color EvadeColor = new Color((byte)(255.0f * 0.6f), (byte)(255.0f * 0.6f), (byte)(255.0f * 0.3f)); // annotation
		public static readonly Color SeekColor = new Color((byte)(255.0f * 0.3f), (byte)(255.0f * 0.6f), (byte)(255.0f * 0.6f)); // annotation
		public static readonly Color ClearPathColor = new Color((byte)(255.0f * 0.3f), (byte)(255.0f * 0.6f), (byte)(255.0f * 0.3f)); // annotation

		public const float AvoidancePredictTimeMin = 0.9f;
		public const float AvoidancePredictTimeMax = 2;
		public static float AvoidancePredictTime = AvoidancePredictTimeMin;

		public static bool EnableAttackSeek = true; // for testing (perhaps retain for UI control?)
		public static bool EnableAttackEvade = true; // for testing (perhaps retain for UI control?)

		public static CtfSeeker Seeker = null;

		// count the number of times the simulation has reset (e.g. for overnight runs)
		public static int ResetCount = 0;

		// ----------------------------------------------------------------------------
		// state for OpenSteerDemo PlugIn
		//
		// XXX consider moving this inside CtfPlugIn
		// XXX consider using STL (any advantage? consistency?)

		public static CtfSeeker CtfSeeker = null;
		public static int CtfEnemyCount = 4;
		public static CtfEnemy[] CtfEnemies = new CtfEnemy[CtfEnemyCount];
	}
}
