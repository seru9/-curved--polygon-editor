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
			if (edge1.StartVertex == null || edge1.EndVertex == null || edge2.StartVertex == null || edge2.EndVertex == null)
				return;

			// Tutaj w kolejnym kroku dopiszemy matematykę obrotu/korekty kąta prostopadłego z uwzględnieniem pinnedVertex
		}
	}
}