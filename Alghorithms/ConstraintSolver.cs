using System.Collections.Generic;
using System.Windows;
using _curved__polygon_editor.Models;
using _curved__polygon_editor.Models.Constraints;

namespace _curved__polygon_editor.Algorithms
{
	public static class ConstraintSolver
	{
		public static void Solve(IEnumerable<Polygon> polygons, int iterations = 5)
		{
			for (int it = 0; it < iterations; it++)
			{
				foreach (var polygon in polygons)
				{
					foreach (var edge in polygon.Edges)
					{
						foreach (var constraint in edge.Constraints)
						{
							if (!constraint.IsActive) continue;

							if (constraint is LengthConstraint lenConstraint)
							{
								ApplyLengthConstraint(edge, lenConstraint.TargetLength);
							}
							else if (constraint is PerpendicularConstraint perpConstraint)
							{
								ApplyPerpendicularConstraint(edge, perpConstraint.TargetEdge);
							}
						}
					}
				}
			}
		}

		public static void ApplyLengthConstraint(Edge edge, double targetLength)
		{
			if (edge.StartVertex == null || edge.EndVertex == null) return;

			var p1 = edge.StartVertex.Position;
			var p2 = edge.EndVertex.Position;

			Vector v = new Vector(p2.X - p1.X, p2.Y - p1.Y);
			double currentLength = v.Length;
			if (currentLength < 0.0001) return;

			v.Normalize();
			Vector targetVector = v * targetLength;

			Point center = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);

			// Dzielimy zmianę równomiernie na oba końce krawędzi
			edge.StartVertex.Position = new Point(center.X - targetVector.X / 2.0, center.Y - targetVector.Y / 2.0);
			edge.EndVertex.Position = new Point(center.X + targetVector.X / 2.0, center.Y + targetVector.Y / 2.0);
		}

		public static Point ConstrainVertexPosition(Polygon? polygon, Vertex draggedVertex, Point targetPos)
		{
			if (polygon == null) return targetPos;

			// Znajdź krawędzie incydentne do przeciąganego wierzchołka posiadające aktywny LengthConstraint
			var lengthConstraints = new List<(Edge edge, LengthConstraint constraint, Vertex anchor)>();
			foreach (var edge in polygon.Edges)
			{
				if (edge.StartVertex == draggedVertex || edge.EndVertex == draggedVertex)
				{
					foreach (var c in edge.Constraints)
					{
						if (c is LengthConstraint lc && lc.IsActive)
						{
							Vertex anchor = (edge.StartVertex == draggedVertex) ? edge.EndVertex : edge.StartVertex;
							lengthConstraints.Add((edge, lc, anchor));
							break;
						}
					}
				}
			}

			// Przypadek 0: Brak ograniczeń długości na incydentnych krawędziach
			if (lengthConstraints.Count == 0)
			{
				return targetPos;
			}

			// Przypadek 1: Dokładnie 1 ograniczenie długości -> ruch po łuku (okręgu o promieniu L wokół anchor)
			if (lengthConstraints.Count == 1)
			{
				var item = lengthConstraints[0];
				Point anchorPos = item.anchor.Position;
				Vector u = new Vector(targetPos.X - anchorPos.X, targetPos.Y - anchorPos.Y);
				if (u.Length < 0.0001)
				{
					return draggedVertex.Position;
				}

				u.Normalize();
				double l = item.constraint.TargetLength;
				return new Point(anchorPos.X + u.X * l, anchorPos.Y + u.Y * l);
			}

			// Przypadek 2: 2 ograniczenia długości -> przecięcie dwóch okręgów
			var c1 = lengthConstraints[0];
			var c2 = lengthConstraints[1];

			Point pAnchor1 = c1.anchor.Position;
			Point pAnchor2 = c2.anchor.Position;
			double r1 = c1.constraint.TargetLength;
			double r2 = c2.constraint.TargetLength;

			Vector dVec = new Vector(pAnchor2.X - pAnchor1.X, pAnchor2.Y - pAnchor1.Y);
			double d = dVec.Length;

			if (d < 0.0001)
			{
				return draggedVertex.Position;
			}

			// Sprawdź czy okręgi się przecinają
			if (d > r1 + r2 || d < System.Math.Abs(r1 - r2))
			{
				// Okręgi się nie przecinają - zachowaj aktualną pozycję, by nie łamać więzów
				return draggedVertex.Position;
			}

			double a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
			double hSquared = r1 * r1 - a * a;
			double h = hSquared > 0 ? System.Math.Sqrt(hSquared) : 0.0;

			Point p0 = new Point(pAnchor1.X + (a / d) * dVec.X, pAnchor1.Y + (a / d) * dVec.Y);

			// Wektor prostopadły
			Vector perp = new Vector(-dVec.Y / d, dVec.X / d);

			Point i1 = new Point(p0.X + h * perp.X, p0.Y + h * perp.Y);
			Point i2 = new Point(p0.X - h * perp.X, p0.Y - h * perp.Y);

			double dist1 = (targetPos - i1).Length;
			double dist2 = (targetPos - i2).Length;

			return dist1 <= dist2 ? i1 : i2;
		}

		public static bool IsPointInPolygon(Point p, Polygon polygon)
		{
			var verts = polygon.Vertices;
			int count = verts.Count;
			if (count < 3) return false;

			bool inside = false;
			for (int i = 0, j = count - 1; i < count; j = i++)
			{
				double xi = verts[i].X, yi = verts[i].Y;
				double xj = verts[j].X, yj = verts[j].Y;

				bool intersect = ((yi > p.Y) != (yj > p.Y)) &&
					(p.X < (xj - xi) * (p.Y - yi) / (yj - yi) + xi);

				if (intersect)
					inside = !inside;
			}
			return inside;
		}

		private static void ApplyPerpendicularConstraint(Edge edge1, Edge edge2)
		{
			// Logika wymuszenia prostopadłości (iloczyn skalarny wektorów obu krawędzi = 0)
			if (edge1.StartVertex == null || edge1.EndVertex == null || edge2.StartVertex == null || edge2.EndVertex == null)
				return;

			// Tutaj w kolejnym kroku dopiszemy matematykę obrotu/korekty kąta prostopadłego
		}
	}
}