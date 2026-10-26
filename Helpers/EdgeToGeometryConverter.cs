using _curved__polygon_editor.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace _curved__polygon_editor.Helpers
{
	public class EdgeToGeometryConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Edge edge)
			{
				if (edge.StartVertex == null || edge.EndVertex == null)
					return null;

				// normal line
				if (!edge.IsBezier || edge.ControlPoint1 == null || edge.ControlPoint2 == null)
				{
					return new LineGeometry
					{
						StartPoint = new Point(edge.StartVertex.X, edge.StartVertex.Y),
						EndPoint = new Point(edge.EndVertex.X, edge.EndVertex.Y)
					};
				}

				// Bezier Curve
				return CreateBezierGeometry(
					edge.StartVertex.Position,
					edge.ControlPoint1.Position,
					edge.ControlPoint2.Position,
					edge.EndVertex.Position);
			}
			return null;
		}

		private Geometry CreateBezierGeometry(Point v0, Point v1, Point v2, Point v3)
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

			// Krok przyrostu d (np. 50 podziałów daje bardzo gładką krzywą)
			int steps = 50;
			double d = 1.0 / steps;
			double d2 = d * d;
			double d3 = d2 * d;

			// Inicjalizacja różnic progresywnych dla t = 0
			Vector currentP = a0;
			Vector deltaP = a3 * d3 + a2 * d2 + a1 * d;
			Vector delta2P = 6.0 * a3 * d3 + 2.0 * a2 * d2;
			Vector delta3P = 6.0 * a3 * d3; // Stała trzecia różnica

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

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}