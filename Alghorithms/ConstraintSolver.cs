using System.Collections.Generic;
using System.Windows;
using _curved__polygon_editor.Models;
using _curved__polygon_editor.Models.Constraints;

namespace _curved__polygon_editor.Algorithms
{
	public static class ConstraintSolver
	{
		// Dodajemy opcjonalny parametr pinnedVertex i zwiększamy domyślną liczbę iteracji dla lepszej zbieżności
		public static void Solve(IEnumerable<Polygon> polygons, Vertex? pinnedVertex = null, int iterations = 15)
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
								ApplyLengthConstraint(edge, lenConstraint.TargetLength, pinnedVertex);
							}
							else if (constraint is PerpendicularConstraint perpConstraint)
							{
								ApplyPerpendicularConstraint(edge, perpConstraint.TargetEdge, pinnedVertex);
							}
						}
					}
				}
			}
		}

		// Przeciążenie dla pojedynczego wielokąta (wygodne przy przeciąganiu wierzchołka)
		public static void Solve(Polygon? polygon, Vertex? pinnedVertex = null, int iterations = 15)
		{
			if (polygon == null) return;
			Solve(new[] { polygon }, pinnedVertex, iterations);
		}

		public static void ApplyLengthConstraint(Edge edge, double targetLength, Vertex? pinnedVertex = null)
		{
			if (edge.StartVertex == null || edge.EndVertex == null) return;

			var p1 = edge.StartVertex.Position;
			var p2 = edge.EndVertex.Position;

			Vector v = new Vector(p2.X - p1.X, p2.Y - p1.Y);
			double currentLength = v.Length;
			if (currentLength < 0.0001) return;

			v.Normalize();
			Vector targetVector = v * targetLength;

			// pinnedVertex's one of the edge (where pinnedVertex is StartVertex) other vertex has to follow other vertex
			if (edge.StartVertex == pinnedVertex)
			{
				edge.EndVertex.Position = new Point(p1.X + targetVector.X, p1.Y + targetVector.Y);
			}
			// EndVertex jest przeciągany (zablokowany w miejscu kursora) -> przesuwamy tylko StartVertex
			else if (edge.EndVertex == pinnedVertex)
			{
				edge.StartVertex.Position = new Point(p2.X - targetVector.X, p2.Y - targetVector.Y);
			}
			// vertexes that are not adjacent to adjacent edge of the vertex
			else
			{
				Point center = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);
				edge.StartVertex.Position = new Point(center.X - targetVector.X / 2.0, center.Y - targetVector.Y / 2.0);
				edge.EndVertex.Position = new Point(center.X + targetVector.X / 2.0, center.Y + targetVector.Y / 2.0);
			}
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

		private static void ApplyPerpendicularConstraint(Edge edge1, Edge edge2, Vertex? pinnedVertex = null)
		{
			if (edge1.StartVertex == null || edge1.EndVertex == null ||
				edge2.StartVertex == null || edge2.EndVertex == null)
				return;

			if (edge1 == edge2) return;
			// direction vectors
			Vector v1 = edge1.EndVertex.Position - edge1.StartVertex.Position;
			Vector v2 = edge2.EndVertex.Position - edge2.StartVertex.Position;

			if (v1.LengthSquared < 0.0001 || v2.LengthSquared < 0.0001)
				return;

			// AngleBetween the direction vectors
			double angle = Vector.AngleBetween(v1, v2);

			// Choosing 90 degrees or -90 degrees
			double targetAngle = angle >= 0 ? 90.0 : -90.0;
			double diff = targetAngle - angle; // diff - how much degrees to rotate
			if (System.Math.Abs(diff) < 0.01)
				return;

			// checking if any of adjacent vertex is being rotated
			bool edge1HasPinned = (edge1.StartVertex == pinnedVertex || edge1.EndVertex == pinnedVertex) && pinnedVertex != null;
			bool edge2HasPinned = (edge2.StartVertex == pinnedVertex || edge2.EndVertex == pinnedVertex) && pinnedVertex != null;

			if (edge1HasPinned && !edge2HasPinned)
			{
				// if edge 1 is being rotated - we rotate edge 2
				RotateEdge(edge2, diff, pinnedVertex, sharedWith: edge1);
			}
			else if (edge2HasPinned && !edge1HasPinned)
			{
				// if edge 2 is being rotated - we rotate edge 1
				RotateEdge(edge1, -diff, pinnedVertex, sharedWith: edge2);
			}
			else
			{
				// if both are rotated then we need to rotate both (if we drag the vertex that is adjacent to perpendicular edges
				RotateEdge(edge1, -diff / 2.0, pinnedVertex, sharedWith: edge2);
				RotateEdge(edge2, diff / 2.0, pinnedVertex, sharedWith: edge1);
			}
		}

		private static void RotateEdge(Edge edge, double angleDegrees, Vertex? pinnedVertex, Edge? sharedWith = null)
		{
			Point p1 = edge.StartVertex.Position;
			Point p2 = edge.EndVertex.Position;

			double rad = angleDegrees * System.Math.PI / 180.0;
			double cos = System.Math.Cos(rad);
			double sin = System.Math.Sin(rad);

			// edge can be rotated around StartVertex (like an anchor)
			if (edge.StartVertex == pinnedVertex)
			{
				edge.EndVertex.Position = RotatePoint(p2, p1, cos, sin);
				return;
			}

			// or can be rotated around EdgeVertex
			if (edge.EndVertex == pinnedVertex)
			{
				edge.StartVertex.Position = RotatePoint(p1, p2, cos, sin);
				return;
			}

			if (sharedWith != null)
			{
				if (edge.StartVertex == sharedWith.StartVertex || edge.StartVertex == sharedWith.EndVertex)
				{
					edge.EndVertex.Position = RotatePoint(p2, p1, cos, sin);
					return;
				}
				if (edge.EndVertex == sharedWith.StartVertex || edge.EndVertex == sharedWith.EndVertex)
				{
					edge.StartVertex.Position = RotatePoint(p1, p2, cos, sin);
					return;
				}
			}

			Point center = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);
			edge.StartVertex.Position = RotatePoint(p1, center, cos, sin);
			edge.EndVertex.Position = RotatePoint(p2, center, cos, sin);
		}

		private static Point RotatePoint(Point point, Point pivot, double cos, double sin)
		{
			double dx = point.X - pivot.X;
			double dy = point.Y - pivot.Y;

			return new Point(
				pivot.X + (dx * cos - dy * sin),
				pivot.Y + (dx * sin + dy * cos)
			);
		}
	}
}