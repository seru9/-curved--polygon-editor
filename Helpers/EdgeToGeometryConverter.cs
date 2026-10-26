using _curved__polygon_editor.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace _curved__polygon_editor.Helpers
{
	public class EdgeToGeometryConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Edge edge)
			{
				return GetGeometry(edge);
			}
			return null;
		}

		public static Geometry? GetGeometry(Edge edge)
		{
			if (edge.StartVertex == null || edge.EndVertex == null)
				return null;

			// Arc edge
			if (edge.IsArc)
			{
				return CreateArcGeometry(edge);
			}

			// Bezier Curve
			if (edge.IsBezier && edge.ControlPoint1 != null && edge.ControlPoint2 != null)
			{
				return CreateBezierGeometry(
					edge.StartVertex.Position,
					edge.ControlPoint1.Position,
					edge.ControlPoint2.Position,
					edge.EndVertex.Position);
			}

			// Normal line
			return new LineGeometry
			{
				StartPoint = new Point(edge.StartVertex.X, edge.StartVertex.Y),
				EndPoint = new Point(edge.EndVertex.X, edge.EndVertex.Y)
			};
		}

		private static Geometry CreateBezierGeometry(Point v0, Point v1, Point v2, Point v3)
		{
			// from Bernstein base to polynomial base
			Vector p0 = (Vector)v0;
			Vector p1 = (Vector)v1;
			Vector p2 = (Vector)v2;
			Vector p3 = (Vector)v3;

			Vector a0 = p0;
			Vector a1 = 3.0 * (p1 - p0);
			Vector a2 = 3.0 * (p2 - 2.0 * p1 + p0);
			Vector a3 = p3 - 3.0 * p2 + 3.0 * p1 - p0;

			// Krok przyrostu d (50 podziałów daje bardzo gładką krzywą)
			int steps = 50;
			double d = 1.0 / steps;
			double d2 = d * d;
			double d3 = d2 * d;

			// Inicjalizacja różnic progresywnych dla t = 0
			Vector currentP = a0;
			Vector deltaP = a3 * d3 + a2 * d2 + a1 * d;
			Vector delta2P = 6.0 * a3 * d3 + 2.0 * a2 * d2;
			Vector delta3P = 6.0 * a3 * d3;

			var polyLineSegment = new PolyLineSegment();

			// Pętla przyrostowa - wyznaczanie kolejnych punktów wyłącznie przez dodawanie
			for (int i = 0; i < steps; i++)
			{
				currentP += deltaP;
				deltaP += delta2P;
				delta2P += delta3P;

				polyLineSegment.Points.Add(new Point(currentP.X, currentP.Y));
			}

			// Gwarancja dokładnego dociągnięcia do punktu końcowego V3
			polyLineSegment.Points[polyLineSegment.Points.Count - 1] = v3;

			var pathFigure = new PathFigure
			{
				StartPoint = v0,
				IsClosed = false
			};
			pathFigure.Segments.Add(polyLineSegment);

			var pathGeometry = new PathGeometry();
			pathGeometry.Figures.Add(pathFigure);

			return pathGeometry;
		}

		private static Geometry CreateArcGeometry(Edge arcEdge)
		{
			Point pStart = arcEdge.StartVertex.Position;
			Point pEnd = arcEdge.EndVertex.Position;
			Vector chord = pEnd - pStart;
			double d = chord.Length;

			if (d < 0.001)
				return new LineGeometry(pStart, pEnd);

			bool startG1 = arcEdge.StartVertex.Continuity == ContinuityType.G1;
			bool endG1 = arcEdge.EndVertex.Continuity == ContinuityType.G1;

			// Przypadek 1: Oba końce mają G0 -> promień R = d / 2 (półokrąg)
			if (!startG1 && !endG1)
			{
				double r = Math.Max(d / 2.0, 0.001);
				var figure = new PathFigure
				{
					StartPoint = pStart,
					IsClosed = false
				};
				figure.Segments.Add(new ArcSegment
				{
					Point = pEnd,
					Size = new Size(r, r),
					RotationAngle = 0,
					IsLargeArc = false,
					SweepDirection = SweepDirection.Clockwise,
					IsStroked = true
				});
				var pathGeometry = new PathGeometry();
				pathGeometry.Figures.Add(figure);
				return pathGeometry;
			}
			// Przypadek 2: Jeden z końców ma ciągłość G1
			else
			{
				bool anchoredAtStart = startG1;
				Vector tangent = anchoredAtStart
					? GetIncomingTangentAtStart(arcEdge)
					: GetOutgoingTangentAtEnd(arcEdge);

				if (tangent.LengthSquared < 0.0001)
					return new LineGeometry(pStart, pEnd);

				tangent.Normalize();

				if (anchoredAtStart)
				{
					Vector U = pEnd - pStart;
					double cp = tangent.X * U.Y - tangent.Y * U.X;
					if (Math.Abs(cp) < 0.0001)
						return new LineGeometry(pStart, pEnd);

					double radius = Math.Max(U.LengthSquared / (2.0 * Math.Abs(cp)), d / 2.0);
					bool isLargeArc = (tangent.X * U.X + tangent.Y * U.Y) < 0;
					SweepDirection sweepDir = cp > 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;

					var figure = new PathFigure
					{
						StartPoint = pStart,
						IsClosed = false
					};
					figure.Segments.Add(new ArcSegment
					{
						Point = pEnd,
						Size = new Size(radius, radius),
						RotationAngle = 0,
						IsLargeArc = isLargeArc,
						SweepDirection = sweepDir,
						IsStroked = true
					});
					var pathGeometry = new PathGeometry();
					pathGeometry.Figures.Add(figure);
					return pathGeometry;
				}
				else
				{
					Vector U = pEnd - pStart;
					double cp = tangent.X * U.Y - tangent.Y * U.X;
					if (Math.Abs(cp) < 0.0001)
						return new LineGeometry(pStart, pEnd);

					double radius = Math.Max(U.LengthSquared / (2.0 * Math.Abs(cp)), d / 2.0);
					bool isLargeArc = (tangent.X * U.X + tangent.Y * U.Y) < 0;
					SweepDirection sweepDir = cp > 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

					var figure = new PathFigure
					{
						StartPoint = pStart,
						IsClosed = false
					};
					figure.Segments.Add(new ArcSegment
					{
						Point = pEnd,
						Size = new Size(radius, radius),
						RotationAngle = 0,
						IsLargeArc = isLargeArc,
						SweepDirection = sweepDir,
						IsStroked = true
					});
					var pathGeometry = new PathGeometry();
					pathGeometry.Figures.Add(figure);
					return pathGeometry;
				}
			}
		}

		// Wyznacza kierunek stycznej wchodzącej do StartVertex z poprzedniej krawędzi
		private static Vector GetIncomingTangentAtStart(Edge arcEdge)
		{
			var poly = arcEdge.OwnerPolygon;
			if (poly == null) return arcEdge.EndVertex.Position - arcEdge.StartVertex.Position;

			Edge? prevEdge = poly.Edges.FirstOrDefault(e => e != arcEdge && (e.EndVertex == arcEdge.StartVertex || e.StartVertex == arcEdge.StartVertex));
			if (prevEdge == null) return arcEdge.EndVertex.Position - arcEdge.StartVertex.Position;

			if (prevEdge.EndVertex == arcEdge.StartVertex)
			{
				if (prevEdge.IsBezier && prevEdge.ControlPoint2 != null)
					return prevEdge.EndVertex.Position - prevEdge.ControlPoint2.Position;
				if (prevEdge.IsArc)
					return new Vector(-(prevEdge.EndVertex.Y - prevEdge.StartVertex.Y), prevEdge.EndVertex.X - prevEdge.StartVertex.X);

				return prevEdge.EndVertex.Position - prevEdge.StartVertex.Position;
			}
			else
			{
				if (prevEdge.IsBezier && prevEdge.ControlPoint1 != null)
					return prevEdge.StartVertex.Position - prevEdge.ControlPoint1.Position;
				if (prevEdge.IsArc)
					return new Vector(prevEdge.StartVertex.Y - prevEdge.EndVertex.Y, -(prevEdge.StartVertex.X - prevEdge.EndVertex.X));

				return prevEdge.StartVertex.Position - prevEdge.EndVertex.Position;
			}
		}

		// Wyznacza kierunek stycznej wychodzącej z EndVertex w następną krawędź
		private static Vector GetOutgoingTangentAtEnd(Edge arcEdge)
		{
			var poly = arcEdge.OwnerPolygon;
			if (poly == null) return arcEdge.EndVertex.Position - arcEdge.StartVertex.Position;

			Edge? nextEdge = poly.Edges.FirstOrDefault(e => e != arcEdge && (e.StartVertex == arcEdge.EndVertex || e.EndVertex == arcEdge.EndVertex));
			if (nextEdge == null) return arcEdge.EndVertex.Position - arcEdge.StartVertex.Position;

			if (nextEdge.StartVertex == arcEdge.EndVertex)
			{
				if (nextEdge.IsBezier && nextEdge.ControlPoint1 != null)
					return nextEdge.ControlPoint1.Position - nextEdge.StartVertex.Position;
				if (nextEdge.IsArc)
					return new Vector(-(nextEdge.EndVertex.Y - nextEdge.StartVertex.Y), nextEdge.EndVertex.X - nextEdge.StartVertex.X);

				return nextEdge.EndVertex.Position - nextEdge.StartVertex.Position;
			}
			else
			{
				if (nextEdge.IsBezier && nextEdge.ControlPoint2 != null)
					return nextEdge.ControlPoint2.Position - nextEdge.EndVertex.Position;
				if (nextEdge.IsArc)
					return new Vector(nextEdge.StartVertex.Y - nextEdge.EndVertex.Y, -(nextEdge.StartVertex.X - nextEdge.EndVertex.X));

				return nextEdge.StartVertex.Position - nextEdge.EndVertex.Position;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}