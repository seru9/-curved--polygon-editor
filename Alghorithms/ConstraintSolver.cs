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

		private static void ApplyLengthConstraint(Edge edge, double targetLength)
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

		private static void ApplyPerpendicularConstraint(Edge edge1, Edge edge2)
		{
			// Logika wymuszenia prostopadłości (iloczyn skalarny wektorów obu krawędzi = 0)
			if (edge1.StartVertex == null || edge1.EndVertex == null || edge2.StartVertex == null || edge2.EndVertex == null)
				return;

			// Tutaj w kolejnym kroku dopiszemy matematykę obrotu/korekty kąta prostopadłego
		}
	}
}