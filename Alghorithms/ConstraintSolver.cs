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

			// Pobierz krawędzie incydentne do przeciąganego wierzchołka
			var incidentEdges = polygon.Edges
				.Where(e => e.StartVertex == draggedVertex || e.EndVertex == draggedVertex)
				.ToList();

			var lenConstraints = new List<(Edge edge, LengthConstraint constraint, Vertex anchor)>();
			var perpConstraints = new List<(Edge edge, PerpendicularConstraint constraint, Vertex anchor)>();

			foreach (var edge in incidentEdges)
			{
				Vertex anchor = (edge.StartVertex == draggedVertex) ? edge.EndVertex : edge.StartVertex;

				foreach (var c in edge.Constraints)
				{
					if (c is LengthConstraint lc && lc.IsActive)
					{
						lenConstraints.Add((edge, lc, anchor));
					}
					else if (c is PerpendicularConstraint pc && pc.IsActive && pc.TargetEdge != null)
					{
						perpConstraints.Add((edge, pc, anchor));
					}
				}
			}

			// KROK 1: Przeciągany wierzchołek łączy dwie wzajemnie prostopadłe krawędzie (Twierdzenie Talesa)
			var mutualP1 = perpConstraints
				.FirstOrDefault(p1 => perpConstraints.Any(p2 => p2.edge != p1.edge && p1.edge == p2.constraint.TargetEdge && p2.edge == p1.constraint.TargetEdge));

			if (mutualP1.constraint != null)
			{
				var p1 = perpConstraints[0];
				var p2 = perpConstraints[1];
				Point anchorA = p1.anchor.Position;
				Point anchorB = p2.anchor.Position;

				// Środek i promień okręgu Talesa opartego na średnicy AB
				Point centerC = new Point((anchorA.X + anchorB.X) / 2.0, (anchorA.Y + anchorB.Y) / 2.0);
				double radiusR = (anchorB - anchorA).Length / 2.0;

				if (radiusR < 0.0001) return draggedVertex.Position;

				// Sprawdź czy któraś z krawędzi ma dodatkowo LengthConstraint
				var lenA = lenConstraints.FirstOrDefault(l => l.edge == p1.edge).constraint;
				var lenB = lenConstraints.FirstOrDefault(l => l.edge == p2.edge).constraint;

				if (lenA != null || lenB != null)
				{
					double rA = lenA != null ? lenA.TargetLength : System.Math.Sqrt(System.Math.Max(0, 4 * radiusR * radiusR - (lenB!.TargetLength * lenB.TargetLength)));
					double rB = lenB != null ? lenB.TargetLength : System.Math.Sqrt(System.Math.Max(0, 4 * radiusR * radiusR - (lenA!.TargetLength * lenA.TargetLength)));

					Vector dVec = anchorB - anchorA;
					double d = dVec.Length;
					if (d < 0.0001 || d > rA + rB || d < System.Math.Abs(rA - rB)) return draggedVertex.Position;

					double a = (rA * rA - rB * rB + d * d) / (2 * d);
					double hSq = rA * rA - a * a;
					double h = hSq > 0 ? System.Math.Sqrt(hSq) : 0;
					Point p0 = new Point(anchorA.X + (a / d) * dVec.X, anchorA.Y + (a / d) * dVec.Y);
					Vector perpV = new Vector(-dVec.Y / d, dVec.X / d);

					Point cand1 = new Point(p0.X + h * perpV.X, p0.Y + h * perpV.Y);
					Point cand2 = new Point(p0.X - h * perpV.X, p0.Y - h * perpV.Y);
					return (targetPos - cand1).Length <= (targetPos - cand2).Length ? cand1 : cand2;
				}

				// Swobodny kąt prosty -> rzut na okrąg Talesa (kąt wpisany oparty na średnicy wynosi dokładnie 90°)
				Vector toMouse = targetPos - centerC;
				if (toMouse.Length < 0.0001) return draggedVertex.Position;
				toMouse.Normalize();
				return new Point(centerC.X + toMouse.X * radiusR, centerC.Y + toMouse.Y * radiusR);
			}

			// KROK 2: Krawędź ma ZARÓWNO LengthConstraint, JAK I PerpendicularConstraint
			foreach (var perp in perpConstraints)
			{
				var matchingLen = lenConstraints.FirstOrDefault(l => l.edge == perp.edge);
				if (matchingLen.constraint != null)
				{
					var targetEdge = perp.constraint.TargetEdge;
					Vector vT = targetEdge.EndVertex.Position - targetEdge.StartVertex.Position;
					if (vT.Length > 0.0001)
					{
						vT.Normalize();
						Vector n = new Vector(-vT.Y, vT.X);
						double l = matchingLen.constraint.TargetLength;
						Point pA = new Point(perp.anchor.Position.X + n.X * l, perp.anchor.Position.Y + n.Y * l);
						Point pB = new Point(perp.anchor.Position.X - n.X * l, perp.anchor.Position.Y - n.Y * l);

						return (targetPos - pA).Length <= (targetPos - pB).Length ? pA : pB;
					}
				}
			}

			// KROK 3: Dwa więzy długości na dwóch krawędziach incydentnych -> przecięcie dwóch okręgów
			if (lenConstraints.Count >= 2)
			{
				var c1 = lenConstraints[0];
				var c2 = lenConstraints[1];

				Point pAnchor1 = c1.anchor.Position;
				Point pAnchor2 = c2.anchor.Position;
				double r1 = c1.constraint.TargetLength;
				double r2 = c2.constraint.TargetLength;

				Vector dVec = new Vector(pAnchor2.X - pAnchor1.X, pAnchor2.Y - pAnchor1.Y);
				double d = dVec.Length;

				if (d < 0.0001 || d > r1 + r2 || d < System.Math.Abs(r1 - r2))
				{
					return draggedVertex.Position;
				}

				double a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
				double hSquared = r1 * r1 - a * a;
				double h = hSquared > 0 ? System.Math.Sqrt(hSquared) : 0.0;

				Point p0 = new Point(pAnchor1.X + (a / d) * dVec.X, pAnchor1.Y + (a / d) * dVec.Y);
				Vector perpVec = new Vector(-dVec.Y / d, dVec.X / d);

				Point i1 = new Point(p0.X + h * perpVec.X, p0.Y + h * perpVec.Y);
				Point i2 = new Point(p0.X - h * perpVec.X, p0.Y - h * perpVec.Y);

				return (targetPos - i1).Length <= (targetPos - i2).Length ? i1 : i2;
			}

			// KROK 3: Dokładnie 1 więz długości i brak prostopadłości -> rzut na okrąg
			if (lenConstraints.Count == 1 && perpConstraints.Count == 0)
			{
				var item = lenConstraints[0];
				Point anchorPos = item.anchor.Position;
				Vector u = new Vector(targetPos.X - anchorPos.X, targetPos.Y - anchorPos.Y);
				if (u.Length < 0.0001) return draggedVertex.Position;

				u.Normalize();
				double l = item.constraint.TargetLength;
				return new Point(anchorPos.X + u.X * l, anchorPos.Y + u.Y * l);
			}

			// KROK 4: Dokładnie 1 więz prostopadłości i brak długości -> rzut na prostą
			if (perpConstraints.Count == 1 && lenConstraints.Count == 0)
			{
				var perp = perpConstraints[0];
				var targetEdge = perp.constraint.TargetEdge;
				Vector vT = targetEdge.EndVertex.Position - targetEdge.StartVertex.Position;
				if (vT.Length > 0.0001)
				{
					vT.Normalize();
					Vector n = new Vector(-vT.Y, vT.X);
					Point anchorPos = perp.anchor.Position;

					Vector w = targetPos - anchorPos;
					double t = w.X * n.X + w.Y * n.Y;
					return new Point(anchorPos.X + n.X * t, anchorPos.Y + n.Y * t);
				}
			}

			return targetPos;
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

		public static void ApplyPerpendicularConstraint(Edge edge1, Edge edge2)
		{
			if (edge1.StartVertex == null || edge1.EndVertex == null || edge2.StartVertex == null || edge2.EndVertex == null)
				return;

			var p1 = edge1.StartVertex.Position;
			var p2 = edge1.EndVertex.Position;
			var p3 = edge2.StartVertex.Position;
			var p4 = edge2.EndVertex.Position;

			Vector v1 = p2 - p1;
			Vector v2 = p4 - p3;

			double l1 = v1.Length;
			double l2 = v2.Length;
			if (l1 < 0.0001 || l2 < 0.0001) return;

			double theta1 = System.Math.Atan2(v1.Y, v1.X);
			double theta2 = System.Math.Atan2(v2.Y, v2.X);

			double diff = theta1 - theta2;
			while (diff > System.Math.PI) diff -= 2 * System.Math.PI;
			while (diff < -System.Math.PI) diff += 2 * System.Math.PI;

			double targetDiff = (diff >= 0) ? System.Math.PI / 2 : -System.Math.PI / 2;
			double err = diff - targetDiff;

			// Sprawdź czy krawędzie dzielą wspólny wierzchołek
			Vertex? shared = null;
			Vertex? other1 = null;
			Vertex? other2 = null;

			if (edge1.StartVertex == edge2.StartVertex)
			{
				shared = edge1.StartVertex;
				other1 = edge1.EndVertex;
				other2 = edge2.EndVertex;
			}
			else if (edge1.StartVertex == edge2.EndVertex)
			{
				shared = edge1.StartVertex;
				other1 = edge1.EndVertex;
				other2 = edge2.StartVertex;
			}
			else if (edge1.EndVertex == edge2.StartVertex)
			{
				shared = edge1.EndVertex;
				other1 = edge1.StartVertex;
				other2 = edge2.EndVertex;
			}
			else if (edge1.EndVertex == edge2.EndVertex)
			{
				shared = edge1.EndVertex;
				other1 = edge1.StartVertex;
				other2 = edge2.EndVertex;
			}

			if (shared != null && other1 != null && other2 != null)
			{
				Point sPos = shared.Position;
				Vector arm1 = other1.Position - sPos;
				Vector arm2 = other2.Position - sPos;
				double arm1Len = arm1.Length;
				double arm2Len = arm2.Length;

				double a1 = System.Math.Atan2(arm1.Y, arm1.X);
				double a2 = System.Math.Atan2(arm2.Y, arm2.X);

				double armDiff = a1 - a2;
				while (armDiff > System.Math.PI) armDiff -= 2 * System.Math.PI;
				while (armDiff < -System.Math.PI) armDiff += 2 * System.Math.PI;

				double armTarget = (armDiff >= 0) ? System.Math.PI / 2 : -System.Math.PI / 2;
				double armErr = armDiff - armTarget;

				double newA1 = a1 - armErr / 2.0;
				double newA2 = a2 + armErr / 2.0;

				other1.Position = new Point(sPos.X + System.Math.Cos(newA1) * arm1Len, sPos.Y + System.Math.Sin(newA1) * arm1Len);
				other2.Position = new Point(sPos.X + System.Math.Cos(newA2) * arm2Len, sPos.Y + System.Math.Sin(newA2) * arm2Len);
			}
			else
			{
				// Krawędzie rozłączne - obracamy wokół ich środków
				double newTheta1 = theta1 - err / 2.0;
				double newTheta2 = theta2 + err / 2.0;

				Point c1 = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);
				Point c2 = new Point((p3.X + p4.X) / 2.0, (p3.Y + p4.Y) / 2.0);

				Vector half1 = new Vector(System.Math.Cos(newTheta1) * (l1 / 2.0), System.Math.Sin(newTheta1) * (l1 / 2.0));
				Vector half2 = new Vector(System.Math.Cos(newTheta2) * (l2 / 2.0), System.Math.Sin(newTheta2) * (l2 / 2.0));

				edge1.StartVertex.Position = new Point(c1.X - half1.X, c1.Y - half1.Y);
				edge1.EndVertex.Position = new Point(c1.X + half1.X, c1.Y + half1.Y);

				edge2.StartVertex.Position = new Point(c2.X - half2.X, c2.Y - half2.Y);
				edge2.EndVertex.Position = new Point(c2.X + half2.X, c2.Y + half2.Y);
			}
		}
	}
}