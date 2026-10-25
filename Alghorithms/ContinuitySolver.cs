using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using _curved__polygon_editor.Models;

namespace _curved__polygon_editor.Algorithms
{
	public static class ContinuitySolver
	{
		/// <summary>
		/// Pobiera dwie krawędzie incydentne do danego wierzchołka w wielokącie.
		/// </summary>
		public static (Edge? e1, Edge? e2) GetIncidentEdges(Polygon polygon, Vertex vertex)
		{
			var incident = polygon.Edges
				.Where(e => e.StartVertex == vertex || e.EndVertex == vertex)
				.Take(2)
				.ToList();

			if (incident.Count == 2)
				return (incident[0], incident[1]);
			if (incident.Count == 1)
				return (incident[0], null);

			return (null, null);
		}

		/// <summary>
		/// Zwraca punkt kontrolny incydentny do wierzchołka na krawędzi Béziera.
		/// </summary>
		public static ControlPoint? GetAdjacentControlPoint(Edge edge, Vertex vertex)
		{
			if (edge.EdgeType != EdgeType.Bezier || edge.ControlPoints.Count < 2)
				return null;

			if (edge.StartVertex == vertex)
				return edge.ControlPoints[0];
			if (edge.EndVertex == vertex)
				return edge.ControlPoints[1];

			return null;
		}

		/// <summary>
		/// Zwraca drugi wierzchołek krawędzi (inny niż podany).
		/// </summary>
		public static Vertex? GetOppositeVertex(Edge edge, Vertex vertex)
		{
			if (edge.StartVertex == vertex)
				return edge.EndVertex;
			if (edge.EndVertex == vertex)
				return edge.StartVertex;

			return null;
		}

		/// <summary>
		/// Obsługa przesuwania punktu kontrolnego myszą z uwzględnieniem klasy ciągłości w sąsiednim wierzchołku.
		/// </summary>
		public static void OnControlPointMoved(Polygon polygon, Edge edge, ControlPoint movedCp, Point targetPos)
		{
			Vertex vertex = (movedCp.Index == 0) ? edge.StartVertex : edge.EndVertex;
			if (vertex == null)
			{
				movedCp.Position = targetPos;
				return;
			}

			if (vertex.Continuity == ContinuityType.G0)
			{
				movedCp.Position = targetPos;
				return;
			}

			var (e1, e2) = GetIncidentEdges(polygon, vertex);
			Edge? otherEdge = (e1 == edge) ? e2 : e1;

			if (otherEdge == null)
			{
				movedCp.Position = targetPos;
				return;
			}

			// Przypadek 1: Po drugiej stronie jest krawędź Béziera
			if (otherEdge.EdgeType == EdgeType.Bezier)
			{
				var otherCp = GetAdjacentControlPoint(otherEdge, vertex);
				if (otherCp == null)
				{
					movedCp.Position = targetPos;
					return;
				}

				if (vertex.Continuity == ContinuityType.C1)
				{
					// C1: wektor pochodnej jest równy, punkt wierzchołka jest środkiem odcinka między punktami kontrolnymi
					// C_other = 2*V - C_moved
					movedCp.Position = targetPos;
					otherCp.Position = new Point(2.0 * vertex.Position.X - targetPos.X, 2.0 * vertex.Position.Y - targetPos.Y);
				}
				else if (vertex.Continuity == ContinuityType.G1)
				{
					// G1: wektory leżą na jednej prostej i mają przeciwne zwroty względem V
					movedCp.Position = targetPos;
					Vector u = targetPos - vertex.Position;
					if (u.Length > 0.001)
					{
						u.Normalize();
						Vector vOther = otherCp.Position - vertex.Position;
						double currentLen = Math.Max(5.0, vOther.Length);
						otherCp.Position = new Point(vertex.Position.X - u.X * currentLen, vertex.Position.Y - u.Y * currentLen);
					}
				}
			}
			// Przypadek 2: Po drugiej stronie jest zwykły odcinek prosty
			else
			{
				Vertex? lineEnd = GetOppositeVertex(otherEdge, vertex);
				if (lineEnd == null)
				{
					movedCp.Position = targetPos;
					return;
				}

				// Wektor styczny odcinka wychodzący przez wierzchołek V na zewnątrz (w kierunku krzywej)
				Vector tangentDir = vertex.Position - lineEnd.Position;
				double lineLen = tangentDir.Length;
				if (lineLen < 0.001)
				{
					movedCp.Position = targetPos;
					return;
				}

				if (vertex.Continuity == ContinuityType.C1)
				{
					// Dla C1: 3 * (C - V) = (V - V_line) => C = V + 1/3 * (V - V_line)
					// Punkt kontrolny jest w pełni determinowany przez geometrię odcinka
					movedCp.Position = new Point(
						vertex.Position.X + tangentDir.X / 3.0,
						vertex.Position.Y + tangentDir.Y / 3.0
					);
				}
				else if (vertex.Continuity == ContinuityType.G1)
				{
					// Dla G1: C musi leżeć na prostej wyznaczonej przez wektor tangentDir
					tangentDir.Normalize();
					Vector p = targetPos - vertex.Position;
					double proj = p.X * tangentDir.X + p.Y * tangentDir.Y;
					if (proj < 5.0) proj = 5.0; // Utrzymujemy zwrot na zewnątrz

					movedCp.Position = new Point(
						vertex.Position.X + tangentDir.X * proj,
						vertex.Position.Y + tangentDir.Y * proj
					);
				}
			}
		}

		/// <summary>
		/// Obsługa przemieszczenia wierzchołka wielokąta o dany wektor przesunięcia delta.
		/// </summary>
		public static void OnVertexMoved(Polygon polygon, Vertex vertex, Vector delta)
		{
			// 1. Przesuń powiązane punkty kontrolne incydentnych krawędzi Béziera o ten sam wektor delta
			var (e1, e2) = GetIncidentEdges(polygon, vertex);
			if (e1 != null && e1.EdgeType == EdgeType.Bezier)
			{
				var cp = GetAdjacentControlPoint(e1, vertex);
				if (cp != null)
				{
					cp.Position = new Point(cp.Position.X + delta.X, cp.Position.Y + delta.Y);
				}
			}
			if (e2 != null && e2.EdgeType == EdgeType.Bezier)
			{
				var cp = GetAdjacentControlPoint(e2, vertex);
				if (cp != null)
				{
					cp.Position = new Point(cp.Position.X + delta.X, cp.Position.Y + delta.Y);
				}
			}

			// 2. Wymuś ciągłość w przesuniętym wierzchołku
			EnforceContinuityAtVertex(polygon, vertex);

			// 3. Jeśli sąsiadujące krawędzie to odcinki proste, ich zmiana kierunku mogła wpłynąć
			// na ciągłość w ich przeciwnych wierzchołkach
			if (e1 != null && e1.EdgeType == EdgeType.Line)
			{
				var opp = GetOppositeVertex(e1, vertex);
				if (opp != null && opp.Continuity != ContinuityType.G0)
				{
					EnforceContinuityAtVertex(polygon, opp);
				}
			}
			if (e2 != null && e2.EdgeType == EdgeType.Line)
			{
				var opp = GetOppositeVertex(e2, vertex);
				if (opp != null && opp.Continuity != ContinuityType.G0)
				{
					EnforceContinuityAtVertex(polygon, opp);
				}
			}
		}

		/// <summary>
		/// Wymusza zadaną w wierzchołku klasę ciągłości (G0, G1, C1).
		/// </summary>
		public static void EnforceContinuityAtVertex(Polygon polygon, Vertex vertex)
		{
			if (vertex.Continuity == ContinuityType.G0)
				return;

			var (e1, e2) = GetIncidentEdges(polygon, vertex);
			if (e1 == null || e2 == null)
				return;

			// Sprawdźmy, czy przynajmniej jedna krawędź to Bézier
			bool e1Bezier = e1.EdgeType == EdgeType.Bezier;
			bool e2Bezier = e2.EdgeType == EdgeType.Bezier;

			if (!e1Bezier && !e2Bezier)
				return;

			// Przypadek A: Obie krawędzie to Bézier
			if (e1Bezier && e2Bezier)
			{
				var cp1 = GetAdjacentControlPoint(e1, vertex);
				var cp2 = GetAdjacentControlPoint(e2, vertex);
				if (cp1 == null || cp2 == null) return;

				Vector v1 = cp1.Position - vertex.Position;
				Vector v2 = cp2.Position - vertex.Position;

				if (vertex.Continuity == ContinuityType.C1)
				{
					// Uśredniamy lub dopasowujemy względem cp1
					if (v1.Length > 0.001)
					{
						cp2.Position = new Point(2.0 * vertex.Position.X - cp1.Position.X, 2.0 * vertex.Position.Y - cp1.Position.Y);
					}
					else if (v2.Length > 0.001)
					{
						cp1.Position = new Point(2.0 * vertex.Position.X - cp2.Position.X, 2.0 * vertex.Position.Y - cp2.Position.Y);
					}
				}
				else if (vertex.Continuity == ContinuityType.G1)
				{
					if (v1.Length > 0.001 && v2.Length > 0.001)
					{
						Vector dir1 = v1;
						dir1.Normalize();
						double len2 = v2.Length;
						cp2.Position = new Point(vertex.Position.X - dir1.X * len2, vertex.Position.Y - dir1.Y * len2);
					}
				}
			}
			// Przypadek B: Jedna krawędź to Bézier, druga to odcinek prosty
			else
			{
				Edge bezierEdge = e1Bezier ? e1 : e2;
				Edge lineEdge = e1Bezier ? e2 : e1;

				var cp = GetAdjacentControlPoint(bezierEdge, vertex);
				var lineOpposite = GetOppositeVertex(lineEdge, vertex);
				if (cp == null || lineOpposite == null) return;

				Vector tangentDir = vertex.Position - lineOpposite.Position;
				double lineLen = tangentDir.Length;
				if (lineLen < 0.001) return;

				if (vertex.Continuity == ContinuityType.C1)
				{
					cp.Position = new Point(
						vertex.Position.X + tangentDir.X / 3.0,
						vertex.Position.Y + tangentDir.Y / 3.0
					);
				}
				else if (vertex.Continuity == ContinuityType.G1)
				{
					double cpDist = (cp.Position - vertex.Position).Length;
					if (cpDist < 5.0) cpDist = lineLen / 3.0;
					tangentDir.Normalize();

					cp.Position = new Point(
						vertex.Position.X + tangentDir.X * cpDist,
						vertex.Position.Y + tangentDir.Y * cpDist
					);
				}
			}
		}
	}
}
