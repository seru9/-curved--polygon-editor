using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using _curved__polygon_editor.Models.Constraints;
namespace _curved__polygon_editor.Models
{
	public enum EdgeType
	{
		Line,       
		Bezier,     
		Arc         
	}
	public class Edge
	{
		private Vertex _startVertex;
		private Vertex _endVertex;
		private EdgeType _edgeType;
		
		private Point[] _controlPoints;
		public Vertex StartVertex
		{
			get { return _startVertex; }
			set
			{
				if (_startVertex != value)
				{
					_startVertex = value;
					OnPropertyChanged(nameof(StartVertex));
				}
			}
		}
		public Vertex EndVertex
		{
			get { return _endVertex; }
			set
			{
				if (_endVertex != value)
				{
					_endVertex = value;
					OnPropertyChanged(nameof(EndVertex));
				}
			}
		}
		public EdgeType EdgeType
		{
			get => _edgeType;
			set
			{
				if (_edgeType != value)
				{
					_edgeType = value;
					OnPropertyChanged(nameof(EdgeType));
				}
			}
		}
		public Point[] ControlPosition
		{
			get { return _controlPoints; }
			set
			{
				if (_controlPoints != value)
				{
					_controlPoints = value;
					OnPropertyChanged(nameof(ControlPosition));
				}
			}
		}
		public ObservableCollection<Constraint> Constraints { get; set; } = new ObservableCollection<Constraint>();
		public Edge(Vertex startVertex, Vertex endVertex, EdgeType edgeType = EdgeType.Line)
		{
			StartVertex = startVertex;
			EndVertex = endVertex;
			EdgeType = edgeType;
			ControlPosition = new Point[0];
		}
		public Point GetCenterPoint()
		{
			if (this.EdgeType == EdgeType.Bezier && ControlPosition != null && ControlPosition.Length >= 2)
			{
				// Przybliżony środek krzywej Béziera dla t = 0.5 (De Casteljau / wzór potęgowy)
				double t = 0.5;
				double u = 1 - t;
				double tt = t * t;
				double uu = u * u;
				double uuu = uu * u;
				double ttt = tt * t;

				double x = uuu * StartVertex.Position.X + 3 * uu * t * ControlPosition[0].X + 3 * u * tt * ControlPosition[1].X + ttt * EndVertex.Position.X;
				double y = uuu * StartVertex.Position.Y + 3 * uu * t * ControlPosition[0].Y + 3 * u * tt * ControlPosition[1].Y + ttt * EndVertex.Position.Y;
				return new Point(x, y);
			}
			else
			{
				// Środek zwykłego odcinka
				return new Point(
					(StartVertex.Position.X + EndVertex.Position.X) / 2.0,
					(StartVertex.Position.Y + EndVertex.Position.Y) / 2.0
				);
			}
		}
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
