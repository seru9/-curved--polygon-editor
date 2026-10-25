using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using _curved__polygon_editor.Models;

namespace _curved__polygon_editor.Helpers
{
    public class EdgeToBezierPolygonGeometryConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Edge edge && edge.EdgeType == EdgeType.Bezier && edge.ControlPoints.Count >= 2)
            {
                var figure = new PathFigure
                {
                    StartPoint = edge.StartVertex.Position,
                    IsClosed = false,
                    IsFilled = false
                };

                figure.Segments.Add(new LineSegment(edge.ControlPoints[0].Position, isStroked: true));
                figure.Segments.Add(new LineSegment(edge.ControlPoints[1].Position, isStroked: true));
                figure.Segments.Add(new LineSegment(edge.EndVertex.Position, isStroked: true));

                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                return geometry;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
