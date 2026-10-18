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
            // Tutaj zaimplementuj logikę konwersji Edge -> Geometry
            // Przykład dla linii prostej:
            if (value is Edge edge)
            {
                var geometry = new LineGeometry
                {
                    StartPoint = new System.Windows.Point(edge.StartVertex.X, edge.StartVertex.Y),
                    EndPoint = new System.Windows.Point(edge.EndVertex.X, edge.EndVertex.Y)
                };
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