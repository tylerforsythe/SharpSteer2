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
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	public struct TextEntry
	{
		public Color Color;
		public Vector2 Position;
		public String Text;
	}

	public class Drawing : IDraw
	{
		public static Demo game = null;
		static CullMode cullMode;
		static Color curColor;
		static PrimitiveType curMode;
		static List<VertexPositionColor> vertices = new List<VertexPositionColor>();
		static LocalSpace localSpace = new LocalSpace();

		static void SetColor(Color color)
		{
			curColor = color;
		}

        static void drawBegin(PrimitiveType mode)
		{
			curMode = mode;
		}

		static void drawEnd()
		{
			int primitiveCount = 0;

			switch (curMode)
			{
			case PrimitiveType.LineList:
	
				primitiveCount = vertices.Count / 2;
				break;
			case PrimitiveType.LineStrip:
				vertices.Add(vertices[0]);
				primitiveCount = vertices.Count - 1;
				break;
            case PrimitiveType.TriangleList:
				primitiveCount = vertices.Count / 3;
				break;
            case PrimitiveType.TriangleStrip://.TriangleFan:
				primitiveCount = vertices.Count - 2;
				break;
			}

            game.graphics.GraphicsDevice.DrawUserPrimitives(curMode, vertices.ToArray(), 0, primitiveCount);

			vertices.Clear();
		}

		static void AddVertex(Vector3 v)
		{
			vertices.Add(new VertexPositionColor(v, curColor));
		}

		static void BeginDoubleSidedDrawing()
		{
            //HACK
            //cullMode = game.graphics.GraphicsDevice.RasterizerState.CullMode;
            //game.graphics.GraphicsDevice.RasterizerState.CullMode = CullMode.None;
		}

		static void EndDoubleSidedDrawing()
		{
            //game.graphics.GraphicsDevice.RasterizerState.CullMode = cullMode;
		}

		public static void iDrawLine(Vector3 startPoint, Vector3 endPoint, Color color)
		{
            SetColor(color);
			drawBegin(PrimitiveType.LineList);
			AddVertex(startPoint);
			AddVertex(endPoint);
            drawEnd();
		}

		static void iDrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
		{
            SetColor(color);
			drawBegin(PrimitiveType.TriangleList);
			{
				AddVertex(a);
                AddVertex(b);
                AddVertex(c);
			}
			drawEnd();
		}

		// Draw a single OpenGL quadrangle given four Vector3 vertices, and color.
		static void iDrawQuadrangle(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
		{
            SetColor(color);
            drawBegin(PrimitiveType.TriangleStrip);
			{
                AddVertex(a);
                AddVertex(b);
                AddVertex(c);
                AddVertex(d);
			}
			drawEnd();
		}

		public void Line(Vector3 startPoint, Vector3 endPoint, Color color)
		{
			DrawLine(startPoint, endPoint, color);
		}

		// draw a line with alpha blending
		public void LineAlpha(Vector3 startPoint, Vector3 endPoint, Color color, float alpha)
		{
			DrawLineAlpha(startPoint, endPoint, color, alpha);
		}

		public void CircleOrDisk(float radius, Vector3 axis, Vector3 center, Color color, int segments, bool filled, bool in3d)
		{
			DrawCircleOrDisk(radius, axis, center, color, segments, filled, in3d);
		}

		public static void DrawLine(Vector3 startPoint, Vector3 endPoint, Color color)
		{
			if (Demo.IsDrawPhase == true)
			{
				iDrawLine(startPoint, endPoint, color);
			}
			else
			{
				DeferredLine.AddToBuffer(startPoint, endPoint, color);
			}
		}

		// draw a line with alpha blending
		public static void DrawLineAlpha(Vector3 startPoint, Vector3 endPoint, Color color, float alpha)
		{
			Color c = new Color(color.R, color.G, color.B, (byte)(255.0f * alpha));
			if (Demo.IsDrawPhase == true)
			{
				iDrawLine(startPoint, endPoint, c);
			}
			else
			{
				DeferredLine.AddToBuffer(startPoint, endPoint, c);
			}
		}

		// draw 2d lines in screen space: x and y are the relevant coordinates
		public static void Draw2dLine(Vector3 startPoint, Vector3 endPoint, Color color)
		{
			iDrawLine(startPoint, endPoint, color);
		}

		// draws a "wide line segment": a rectangle of the given width and color
		// whose mid-line connects two given endpoints
		public static void DrawXZWideLine(Vector3 startPoint, Vector3 endPoint, Color color, float width)
		{
			Vector3 offset = endPoint - startPoint;
			offset.Normalize();
            Vector3 perp = localSpace.LocalRotateForwardToSide(offset);
			Vector3 radius = perp * width / 2;

			Vector3 a = startPoint + radius;
			Vector3 b = endPoint + radius;
			Vector3 c = endPoint - radius;
			Vector3 d = startPoint - radius;

			iDrawQuadrangle(a, b, c, d, color);
		}

		// draw a (filled-in, polygon-based) square checkerboard grid on the XZ
		// (horizontal) plane.
		//
		// ("size" is the length of a side of the overall grid, "subsquares" is the
		// number of subsquares along each edge (for example a standard checkboard
		// has eight), "center" is the 3d position of the center of the grid,
		// color1 and color2 are used for alternating subsquares.)
		public static void DrawXZCheckerboardGrid(float size, int subsquares, Vector3 center, Color color1, Color color2)
		{
			float half = size / 2;
			float spacing = size / subsquares;

			BeginDoubleSidedDrawing();
			{
				bool flag1 = false;
				float p = -half;
				Vector3 corner = new Vector3();
				for (int i = 0; i < subsquares; i++)
				{
					bool flag2 = flag1;
					float q = -half;
					for (int j = 0; j < subsquares; j++)
					{
						corner.X = p;
                        corner.Y = 0;
                        corner.Z = q;

						corner += center;
						iDrawQuadrangle(corner,
										 corner + new Vector3(spacing, 0, 0),
										 corner + new Vector3(spacing, 0, spacing),
										 corner + new Vector3(0, 0, spacing),
										 flag2 ? color1 : color2);
						flag2 = !flag2;
						q += spacing;
					}
					flag1 = !flag1;
					p += spacing;
				}
			}
			EndDoubleSidedDrawing();
		}

		// draw a square grid of lines on the XZ (horizontal) plane.
		//
		// ("size" is the length of a side of the overall grid, "subsquares" is the
		// number of subsquares along each edge (for example a standard checkboard
		// has eight), "center" is the 3d position of the center of the grid, lines
		// are drawn in the specified "color".)
		public static void DrawXZLineGrid(float size, int subsquares, Vector3 center, Color color)
		{
			float half = size / 2;
			float spacing = size / subsquares;

			// set grid drawing color
            SetColor(color);

			// draw a square XZ grid with the given size and line count
			drawBegin(PrimitiveType.LineList);
			float q = -half;
			for (int i = 0; i < (subsquares + 1); i++)
			{
				Vector3 x1 = new Vector3(q, 0, +half); // along X parallel to Z
				Vector3 x2 = new Vector3(q, 0, -half);
				Vector3 z1 = new Vector3(+half, 0, q); // along Z parallel to X
				Vector3 z2 = new Vector3(-half, 0, q);

                AddVertex(x1 + center);
                AddVertex(x2 + center);
                AddVertex(z1 + center);
                AddVertex(z2 + center);

				q += spacing;
			}
			drawEnd();
		}

		// draw the three axes of a LocalSpace: three lines parallel to the
		// basis vectors of the space, centered at its origin, of lengths
		// given by the coordinates of "size".
		public static void DrawAxes(ILocalSpace ls, Vector3 size, Color color)
		{
			Vector3 x = new Vector3(size.X / 2, 0, 0);
			Vector3 y = new Vector3(0, size.Y / 2, 0);
			Vector3 z = new Vector3(0, 0, size.Z / 2);

			iDrawLine(ls.GlobalizePosition(x), ls.GlobalizePosition(x * -1), color);
			iDrawLine(ls.GlobalizePosition(y), ls.GlobalizePosition(y * -1), color);
			iDrawLine(ls.GlobalizePosition(z), ls.GlobalizePosition(z * -1), color);
		}

		public static void DrawQuadrangle(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
		{
			iDrawQuadrangle(a, b, c, d, color);
		}

		// draw the edges of a box with a given position, orientation, size
		// and color.  The box edges are aligned with the axes of the given
		// LocalSpace, and it is centered at the origin of that LocalSpace.
		// "size" is the main diagonal of the box.
		//
		// use gGlobalSpace to draw a box aligned with global space
		public static void DrawBoxOutline(ILocalSpace localSpace, Vector3 size, Color color)
		{
			Vector3 s = size / 2.0f;  // half of main diagonal

			Vector3 a = new Vector3(+s.X, +s.Y, +s.Z);
			Vector3 b = new Vector3(+s.X, -s.Y, +s.Z);
			Vector3 c = new Vector3(-s.X, -s.Y, +s.Z);
			Vector3 d = new Vector3(-s.X, +s.Y, +s.Z);

			Vector3 e = new Vector3(+s.X, +s.Y, -s.Z);
			Vector3 f = new Vector3(+s.X, -s.Y, -s.Z);
			Vector3 g = new Vector3(-s.X, -s.Y, -s.Z);
			Vector3 h = new Vector3(-s.X, +s.Y, -s.Z);

			Vector3 A = localSpace.GlobalizePosition(a);
			Vector3 B = localSpace.GlobalizePosition(b);
			Vector3 C = localSpace.GlobalizePosition(c);
			Vector3 D = localSpace.GlobalizePosition(d);

			Vector3 E = localSpace.GlobalizePosition(e);
			Vector3 F = localSpace.GlobalizePosition(f);
			Vector3 G = localSpace.GlobalizePosition(g);
			Vector3 H = localSpace.GlobalizePosition(h);

			iDrawLine(A, B, color);
			iDrawLine(B, C, color);
			iDrawLine(C, D, color);
			iDrawLine(D, A, color);

			iDrawLine(A, E, color);
			iDrawLine(B, F, color);
			iDrawLine(C, G, color);
			iDrawLine(D, H, color);

			iDrawLine(E, F, color);
			iDrawLine(F, G, color);
			iDrawLine(G, H, color);
			iDrawLine(H, E, color);
		}

		public static void DrawXZCircle(float radius, Vector3 center, Color color, int segments)
		{
			DrawXZCircleOrDisk(radius, center, color, segments, false);
		}

		public static void DrawXZDisk(float radius, Vector3 center, Color color, int segments)
		{
			DrawXZCircleOrDisk(radius, center, color, segments, true);
		}

		// drawing utility used by both drawXZCircle and drawXZDisk
		public static void DrawXZCircleOrDisk(float radius, Vector3 center, Color color, int segments, bool filled)
		{
			// draw a circle-or-disk on the XZ plane
			DrawCircleOrDisk(radius, Vector3.Zero, center, color, segments, filled, false);
		}

		// a simple 2d vehicle on the XZ plane
		public static void DrawBasic2dCircularVehicle(IVehicle vehicle, Color color)
		{
			// "aspect ratio" of body (as seen from above)
			float x = 0.5f;
			float y = (float)Math.Sqrt(1 - (x * x));

			// radius and position of vehicle
			float r = vehicle.Radius;
			Vector3 p = vehicle.Position;

			// shape of triangular body
			Vector3 u = new Vector3(0, 1, 0) * r * 0.05f; // slightly up
			Vector3 f = vehicle.Forward * r;
			Vector3 s = vehicle.Side * x * r;
			Vector3 b = vehicle.Forward * -y * r;

			// draw double-sided triangle (that is: no (back) face culling)
			BeginDoubleSidedDrawing();
			iDrawTriangle(p + f + u,
						   p + b - s + u,
						   p + b + s + u,
						   color);
			EndDoubleSidedDrawing();

			// draw the circular collision boundary
			DrawXZCircle(r, p + u, Color.White, 20);
		}

		// a simple 3d vehicle
		public static void DrawBasic3dSphericalVehicle(IVehicle vehicle, Color color)
		{
			Vector3 vColor = new Vector3((float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f);

			// "aspect ratio" of body (as seen from above)
			const float x = 0.5f;
			float y = (float)Math.Sqrt(1 - (x * x));

			// radius and position of vehicle
			float r = vehicle.Radius;
			Vector3 p = vehicle.Position;

			// body shape parameters
			Vector3 f = vehicle.Forward * r;
			Vector3 s = vehicle.Side * r * x;
			Vector3 u = vehicle.Up * r * x * 0.5f;
			Vector3 b = vehicle.Forward * r * -y;

			// vertex positions
			Vector3 nose = p + f;
			Vector3 side1 = p + b - s;
			Vector3 side2 = p + b + s;
			Vector3 top = p + b + u;
			Vector3 bottom = p + b - u;

			// colors
			const float j = +0.05f;
			const float k = -0.05f;
			Color color1 = new Color(vColor + new Vector3(j, j, k));
			Color color2 = new Color(vColor + new Vector3(j, k, j));
			Color color3 = new Color(vColor + new Vector3(k, j, j));
			Color color4 = new Color(vColor + new Vector3(k, j, k));
			Color color5 = new Color(vColor + new Vector3(k, k, j));

			// draw body
			iDrawTriangle(nose, side1, top, color1);  // top, side 1
			iDrawTriangle(nose, top, side2, color2);  // top, side 2
			iDrawTriangle(nose, bottom, side1, color3);  // bottom, side 1
			iDrawTriangle(nose, side2, bottom, color4);  // bottom, side 2
			iDrawTriangle(side1, side2, top, color5);  // top back
			iDrawTriangle(side2, side1, bottom, color5);  // bottom back
		}

		// a simple sphere
		public static void DrawBasic3dSphere(Vector3 position, float radius, Color color)
		{
			Vector3 vColor = new Vector3((float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f);

			// "aspect ratio" of body (as seen from above)
			const float x = 0.5f;
			float y = (float)Math.Sqrt(1 - (x * x));

			// radius and position of vehicle
			float r = radius;
			Vector3 p = position;

			// body shape parameters
			Vector3 f = Vector3.Forward * r;
			Vector3 s = Vector3.Left * r * x;
			Vector3 u = Vector3.Up * r * x;
			Vector3 b = Vector3.Forward * r * -y;

			// vertex positions
			Vector3 nose = p + f;
			Vector3 side1 = p + b - s;
			Vector3 side2 = p + b + s;
			Vector3 top = p + b + u;
			Vector3 bottom = p + b - u;

			// colors
			const float j = +0.05f;
			const float k = -0.05f;
			Color color1 = new Color(vColor + new Vector3(j, j, k));
			Color color2 = new Color(vColor + new Vector3(j, k, j));
			Color color3 = new Color(vColor + new Vector3(k, j, j));
			Color color4 = new Color(vColor + new Vector3(k, j, k));
			Color color5 = new Color(vColor + new Vector3(k, k, j));

			// draw body
			iDrawTriangle(nose, side1, top, color1);  // top, side 1
			iDrawTriangle(nose, top, side2, color2);  // top, side 2
			iDrawTriangle(nose, bottom, side1, color3);  // bottom, side 1
			iDrawTriangle(nose, side2, bottom, color4);  // bottom, side 2
			iDrawTriangle(side1, side2, top, color5);  // top back
			iDrawTriangle(side2, side1, bottom, color5);  // bottom back
		}

		// General purpose circle/disk drawing routine.  Draws circles or disks (as
		// specified by "filled" argument) and handles both special case 2d circles
		// on the XZ plane or arbitrary circles in 3d space (as specified by "in3d"
		// argument)
		public static void DrawCircleOrDisk(float radius, Vector3 axis, Vector3 center, Color color, int segments, bool filled, bool in3d)
		{
			if (Demo.IsDrawPhase == true)
			{
				LocalSpace ls = new LocalSpace();
				if (in3d)
				{
					// define a local space with "axis" as the Y/up direction
					// (XXX should this be a method on  LocalSpace?)
					Vector3 unitAxis = axis;
                    unitAxis.Normalize();
					Vector3 unitPerp = Vector3Helpers.FindPerpendicularIn3d(axis);
                    unitPerp.Normalize();
					ls.Up = unitAxis;
					ls.Forward = unitPerp;
					ls.Position = (center);
					ls.SetUnitSideFromForwardAndUp();
				}

				// make disks visible (not culled) from both sides 
				if (filled) BeginDoubleSidedDrawing();

				// point to be rotated about the (local) Y axis, angular step size
				Vector3 pointOnCircle = new Vector3(radius, 0, 0);
				float step = (float)(2 * Math.PI) / (float)segments;

				// set drawing color
                SetColor(color);

				// begin drawing a triangle fan (for disk) or line loop (for circle)
				drawBegin(filled ? PrimitiveType.TriangleStrip : PrimitiveType.LineStrip);

				// for the filled case, first emit the center point
                if (filled) AddVertex(in3d ? ls.Position : center);

				// rotate p around the circle in "segments" steps
				float sin = 0, cos = 0;
				int vertexCount = filled ? segments + 1 : segments;
				for (int i = 0; i < vertexCount; i++)
				{
					// emit next point on circle, either in 3d (globalized out
					// of the local space), or in 2d (offset from the center)
                    AddVertex(in3d ? ls.GlobalizePosition(pointOnCircle) : pointOnCircle + center);

					// rotate point one more step around circle
                    pointOnCircle = Vector3Helpers.RotateAboutGlobalY(pointOnCircle, step, ref sin, ref cos);
				}

				// close drawing operation
				drawEnd();
				if (filled) EndDoubleSidedDrawing();
			}
			else
			{
				DeferredCircle.AddToBuffer(radius, axis, center, color, segments, filled, in3d);
			}
		}

		public static void Draw3dCircleOrDisk(float radius, Vector3 center, Vector3 axis, Color color, int segments, bool filled)
		{
			// draw a circle-or-disk in the given local space
			DrawCircleOrDisk(radius, axis, center, color, segments, filled, true);
		}

		public static void Draw3dCircle(float radius, Vector3 center, Vector3 axis, Color color, int segments)
		{
			Draw3dCircleOrDisk(radius, center, axis, color, segments, false);
		}

		public static void AllDeferredLines()
		{
			DeferredLine.DrawAll();
		}

		public static void AllDeferredCirclesOrDisks()
		{
			DeferredCircle.DrawAll();
		}

		public static void Draw2dTextAt3dLocation(String text, Vector3 location, Color color)
		{
			// XXX NOTE: "it would be nice if" this had a 2d screenspace offset for
			// the origin of the text relative to the screen space projection of
			// the 3d point.

			// set text color and raster position
			Vector3 p = game.graphics.GraphicsDevice.Viewport.Project(location, game.projectionMatrix, game.viewMatrix, game.worldMatrix);
			TextEntry textEntry = new TextEntry();
			textEntry.Color = color;
			textEntry.Position = new Vector2(p.X, p.Y);
			textEntry.Text = text;
			game.AddText(textEntry);
		}

		public static void Draw2dTextAt2dLocation(String text, Vector3 location, Color color)
		{
			// set text color and raster position
			TextEntry textEntry = new TextEntry();
			textEntry.Color = color;
			textEntry.Position = new Vector2(location.X, location.Y);
			textEntry.Text = text;
			game.AddText(textEntry);
		}

		public static float GetWindowWidth()
		{
			return 1024;
		}

		public static float GetWindowHeight()
		{
			return 640;
		}
	}
}
