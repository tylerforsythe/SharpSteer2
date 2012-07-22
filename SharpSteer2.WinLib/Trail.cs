using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	/// <summary>
	/// Provides support to visualize the recent path of a vehicle.
	/// </summary>
	public class Trail
	{
		int currentIndex;			// Array index of most recently recorded point
		float duration;				// Duration (in seconds) of entire trail
		float sampleInterval;		// Desired interval between taking samples
		float lastSampleTime;		// Global time when lat sample was taken
		int dottedPhase;			// Dotted line: draw segment or not
		Vector3 currentPosition;	// Last reported position of vehicle
		Vector3[] vertices;			// Array (ring) of recent points along trail
		byte[] flags;				// Array (ring) of flag bits for trail points
		Color trailColor;			// Color of the trail
		Color tickColor;			// Color of the ticks

		/// <summary>
		/// Initializes a new instance of Trail.
		/// </summary>
		public Trail()
			: this(5, 100)
		{
		}

		/// <summary>
		/// Initializes a new instance of Trail.
		/// </summary>
		/// <param name="duration">The amount of time the trail represents.</param>
		/// <param name="vertexCount">The number of smaples along the trails length.</param>
		public Trail(float duration, int vertexCount)
		{
			this.duration = duration;

			// Set internal trail state
			this.currentIndex = 0;
			this.lastSampleTime = 0;
			this.sampleInterval = this.duration / vertexCount;
			this.dottedPhase = 1;

			// Initialize ring buffers
			this.vertices = new Vector3[vertexCount];
			this.flags = new byte[vertexCount];

			trailColor = Color.LightGray;
			tickColor = Color.White;
		}

		/// <summary>
		/// Gets or sets the color of the trail.
		/// </summary>
		public Color TrailColor
		{
			get { return trailColor; }
			set { trailColor = value; }
		}

		/// <summary>
		/// Gets or sets the color of the ticks.
		/// </summary>
		public Color TickColor
		{
			get { return tickColor; }
			set { tickColor = value; }
		}

		/// <summary>
		/// Records a position for the current time, called once per update.
		/// </summary>
		/// <param name="currentTime"></param>
		/// <param name="position"></param>
		public void Record(float currentTime, Vector3 position)
		{
			float timeSinceLastTrailSample = currentTime - lastSampleTime;
			if (timeSinceLastTrailSample > sampleInterval)
			{
				currentIndex = (currentIndex + 1) % vertices.Length;
				vertices[currentIndex] = position;
				dottedPhase = (dottedPhase + 1) % 2;
				bool tick = (Math.Floor(currentTime) > Math.Floor(lastSampleTime));
				flags[currentIndex] = (byte)(dottedPhase | (tick ? 2 : 0));
				lastSampleTime = currentTime;
			}
			currentPosition = position;
		}

		/// <summary>
		/// Draws the trail as a dotted line, fading away with age.
		/// </summary>
		public void Draw(IDraw drawer)
		{
			int index = currentIndex;
			for (int j = 0; j < vertices.Length; j++)
			{
				// index of the next vertex (mod around ring buffer)
				int next = (index + 1) % vertices.Length;

				// "tick mark": every second, draw a segment in a different color
				bool tick = ((flags[index] & 2) != 0 || (flags[next] & 2) != 0);
				Color color = tick ? tickColor : trailColor;

				// draw every other segment
				if ((flags[index] & 1) != 0)
				{
					if (j == 0)
					{
						// draw segment from current position to first trail point
						drawer.LineAlpha(currentPosition, vertices[index], color, 1);
					}
					else
					{
						// draw trail segments with opacity decreasing with age
						const float minO = 0.05f; // minimum opacity
						float fraction = (float)j / vertices.Length;
						float opacity = (fraction * (1 - minO)) + minO;
						drawer.LineAlpha(vertices[index], vertices[next], color, opacity);
					}
				}
				index = next;
			}
		}

		/// <summary>
		/// Clear trail history. Used to prevent long streaks due to teleportation.
		/// </summary>
		public void Clear()
		{
			currentIndex = 0;
			lastSampleTime = 0;
			dottedPhase = 1;

			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i] = Vector3.Zero;
				flags[i] = 0;
			}
		}
	}
}
