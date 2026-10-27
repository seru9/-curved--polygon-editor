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
		public static bool UseBresenham { get; set; } = false;
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

			if (UseBresenham && !edge.IsArc && !edge.IsBezier)
			{
				return CreateBresenhamLineGeometry(edge.StartVertex.Position, edge.EndVertex.Position);
			}

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

		private static Geometry CreateBresenhamLineGeometry(Point start, Point end)
		{
			int x0 = (int)Math.Round(start.X);
			int y0 = (int)Math.Round(start.Y);
			int x1 = (int)Math.Round(end.X);
			int y1 = (int)Math.Round(end.Y);

			int dx = Math.Abs(x1 - x0);
			int dy = Math.Abs(y1 - y0);
			// direction of the line, different approach would be steep 
			int sx = x0 < x1 ? 1 : -1;
			int sy = y0 < y1 ? 1 : -1;
			int err = dx - dy;

			var geometry = new StreamGeometry();
			using (StreamGeometryContext ctx = geometry.Open())
			{
				ctx.BeginFigure(new Point(x0, y0), false, false);

				while (true)
				{
					if (x0 == x1 && y0 == y1)
						break;

					int e2 = 2 * err;
					if (e2 > -dy)
					{
						err -= dy;
						x0 += sx;
						// krok poziomy
						ctx.LineTo(new Point(x0, y0), true, false);
					}
					if (e2 < dx)
					{
						err += dx;
						y0 += sy;
						// krok pionowy
						ctx.LineTo(new Point(x0, y0), true, false);
					}
				}
			}

			geometry.Freeze();
			return geometry;
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

			// default parameters if the continuity type is g0
			double radius = Math.Max(d / 2.0, 0.001);
			bool isLargeArc = false;
			SweepDirection sweepDir = SweepDirection.Clockwise;

			// if one of the continuity type was g1
			if (startG1 || endG1)
			{
				Vector tangent = GetArcTangent(arcEdge, startG1);
				// tangent vector
				if (tangent.LengthSquared < 0.0001)
					return new LineGeometry(pStart, pEnd);
				tangent.Normalize();
				// calculating cross product to on which side is the center 
				double cp = Vector.CrossProduct(tangent, chord);
				if (Math.Abs(cp) < 0.0001)
					return new LineGeometry(pStart, pEnd);

				radius = Math.Max(chord.LengthSquared / (2.0 * Math.Abs(cp)), d / 2.0);
				isLargeArc = (tangent * chord) < 0; 
				sweepDir = (cp > 0) == startG1 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
			}

			var figure = new PathFigure { StartPoint = pStart, IsClosed = false };
			figure.Segments.Add(new ArcSegment
			{
				Point = pEnd,
				Size = new Size(radius, radius),
				IsLargeArc = isLargeArc,
				SweepDirection = sweepDir,
				IsStroked = true
			});

			return new PathGeometry(new[] { figure });
		}

		// Uniwersalna metoda wyznaczająca kierunek stycznej na początku (atStart = true) lub końcu (atStart = false) łuku
		private static Vector GetArcTangent(Edge arcEdge, bool atStart)
		{
			Vertex sharedVertex = atStart ? arcEdge.StartVertex : arcEdge.EndVertex;
			Edge? adj = arcEdge.OwnerPolygon?.Edges.FirstOrDefault(
				e => e != arcEdge && (e.StartVertex == sharedVertex || e.EndVertex == sharedVertex));

			if (adj == null)
				return arcEdge.EndVertex.Position - arcEdge.StartVertex.Position;

			bool sharedIsAdjEnd = (adj.EndVertex == sharedVertex);
			Point pShared = sharedVertex.Position;
			Point pOther = sharedIsAdjEnd ? adj.StartVertex.Position : adj.EndVertex.Position;
			Vector dirFromAdj;
			if (adj.IsBezier)
			{
				Vertex? cp = sharedIsAdjEnd ? adj.ControlPoint2 : adj.ControlPoint1;
				dirFromAdj = cp != null ? (pShared - cp.Position) : (pShared - pOther);
			}
			else if (adj.IsArc)
			{
				Vector adjChord = adj.EndVertex.Position - adj.StartVertex.Position;
				dirFromAdj = new Vector(-adjChord.Y, adjChord.X) * (sharedIsAdjEnd ? 1.0 : -1.0);
			}
			else
			{
				dirFromAdj = pShared - pOther;
			}

			return atStart ? dirFromAdj : -dirFromAdj;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}