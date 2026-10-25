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
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Edge edge)
            {
                if (edge.EdgeType == EdgeType.Bezier && edge.ControlPoints.Count >= 2)
                {
                    var figure = new PathFigure
                    {
                        StartPoint = edge.StartVertex.Position,
                        IsClosed = false,
                        IsFilled = false
                    };
                    figure.Segments.Add(new BezierSegment(
                        edge.ControlPoints[0].Position,
                        edge.ControlPoints[1].Position,
                        edge.EndVertex.Position,
                        isStroked: true
                    ));
                    var geometry = new PathGeometry();
                    geometry.Figures.Add(figure);
                    return geometry;
                }

                return new LineGeometry
                {
                    StartPoint = new System.Windows.Point(edge.StartVertex.X, edge.StartVertex.Y),
                    EndPoint = new System.Windows.Point(edge.EndVertex.X, edge.EndVertex.Y)
                };
            }
            return Geometry.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}