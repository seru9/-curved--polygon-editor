using System;
using System.Collections.Generic;
using System.Windows;

namespace _curved__polygon_editor.Algorithms
{
	/// <summary>
	/// Implementacja obliczania punktów i różnic progresywnych krzywej Béziera 3. stopnia
	/// zgodnie ze specyfikacją w graphics.md
	/// </summary>
	public static class BezierAlgorithm
	{
		/// <summary>
		/// Wyznacza punkty krzywej Béziera za pomocą metody różnic progresywnych (Forward Differences).
		/// Przejście z bazy Bernsteina {V0, V1, V2, V3} do bazy wielomianowej {A0, A1, A2, A3}:
		/// A0 = V0
		/// A1 = 3*(V1 - V0)
		/// A2 = 3*(V2 - 2*V1 + V0)
		/// A3 = V3 - 3*V2 + 3*V1 - V0
		/// </summary>
		public static List<Point> ComputePointsForwardDifferences(Point v0, Point v1, Point v2, Point v3, int segments = 50)
		{
			var points = new List<Point>(segments + 1);
			if (segments <= 0) segments = 1;

			double d = 1.0 / segments;
			double d2 = d * d;
			double d3 = d2 * d;

			// Współczynniki bazy wielomianowej dla współrzędnej X
			double a0x = v0.X;
			double a1x = 3.0 * (v1.X - v0.X);
			double a2x = 3.0 * (v2.X - 2.0 * v1.X + v0.X);
			double a3x = v3.X - 3.0 * v2.X + 3.0 * v1.X - v0.X;

			// Współczynniki bazy wielomianowej dla współrzędnej Y
			double a0y = v0.Y;
			double a1y = 3.0 * (v1.Y - v0.Y);
			double a2y = 3.0 * (v2.Y - 2.0 * v1.Y + v0.Y);
			double a3y = v3.Y - 3.0 * v2.Y + 3.0 * v1.Y - v0.Y;

			// Różnice progresywne dla t = 0
			// P(0) = A0
			double px = a0x;
			double py = a0y;

			// Delta P(0) = A3*d^3 + A2*d^2 + A1*d
			double dpx = a3x * d3 + a2x * d2 + a1x * d;
			double dpy = a3y * d3 + a2y * d2 + a1y * d;

			// Delta^2 P(0) = 6*A3*d^3 + 2*A2*d^2
			double d2px = 6.0 * a3x * d3 + 2.0 * a2x * d2;
			double d2py = 6.0 * a3y * d3 + 2.0 * a2y * d2;

			// Delta^3 P = 6*A3*d^3 (wartość stała)
			double d3px = 6.0 * a3x * d3;
			double d3py = 6.0 * a3y * d3;

			points.Add(new Point(px, py));

			for (int i = 0; i < segments; i++)
			{
				px += dpx;
				py += dpy;

				dpx += d2px;
				dpy += d2py;

				d2px += d3px;
				d2py += d3py;

				points.Add(new Point(px, py));
			}

			// Ostatni punkt dokładnie równy v3 (aby uniknąć akumulacji błędów zmiennoprzecinkowych)
			points[points.Count - 1] = v3;

			return points;
		}

		/// <summary>
		/// Oblicza punkt P(t) na krzywej Béziera za pomocą wzoru Bernsteina dla t w [0, 1]
		/// </summary>
		public static Point Evaluate(Point v0, Point v1, Point v2, Point v3, double t)
		{
			t = Math.Clamp(t, 0.0, 1.0);
			double u = 1.0 - t;
			double tt = t * t;
			double uu = u * u;
			double uuu = uu * u;
			double ttt = tt * t;

			double x = uuu * v0.X + 3 * uu * t * v1.X + 3 * u * tt * v2.X + ttt * v3.X;
			double y = uuu * v0.Y + 3 * uu * t * v1.Y + 3 * u * tt * v2.Y + ttt * v3.Y;

			return new Point(x, y);
		}

		/// <summary>
		/// Wektor pierwszej pochodnej P'(t)
		/// P'(0) = 3*(V1 - V0)
		/// P'(1) = 3*(V3 - V2)
		/// </summary>
		public static Vector EvaluateDerivative(Point v0, Point v1, Point v2, Point v3, double t)
		{
			t = Math.Clamp(t, 0.0, 1.0);
			double u = 1.0 - t;

			// P'(t) = 3*(1-t)^2 * (V1 - V0) + 6*(1-t)*t * (V2 - V1) + 3*t^2 * (V3 - V2)
			Vector d1 = v1 - v0;
			Vector d2 = v2 - v1;
			Vector d3 = v3 - v2;

			double c1 = 3.0 * u * u;
			double c2 = 6.0 * u * t;
			double c3 = 3.0 * t * t;

			return new Vector(
				c1 * d1.X + c2 * d2.X + c3 * d3.X,
				c1 * d1.Y + c2 * d2.Y + c3 * d3.Y
			);
		}
	}
}
