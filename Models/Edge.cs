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
	public class Edge : INotifyPropertyChanged
	{
		private Vertex _startVertex;
		private Vertex _endVertex;
		private EdgeType _edgeType;
		public Vertex StartVertex
		{
			get { return _startVertex; }
			set
			{
				if (_startVertex != value)
				{
					if (_startVertex != null)
						_startVertex.PropertyChanged -= Vertex_PropertyChanged;
					_startVertex = value;
					if (_startVertex != null)
						_startVertex.PropertyChanged += Vertex_PropertyChanged;
					OnPropertyChanged(nameof(StartVertex));
					OnPropertyChanged(string.Empty);
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
					if (_endVertex != null)
						_endVertex.PropertyChanged -= Vertex_PropertyChanged;
					_endVertex = value;
					if (_endVertex != null)
						_endVertex.PropertyChanged += Vertex_PropertyChanged;
					OnPropertyChanged(nameof(EndVertex));
					OnPropertyChanged(string.Empty);
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
					if (_edgeType == EdgeType.Bezier)
					{
						InitializeBezierControlPoints();
					}
					else if (_edgeType == EdgeType.Line)
					{
						ClearControlPoints();
					}
					OnPropertyChanged(nameof(EdgeType));
					OnPropertyChanged(nameof(Self));
					OnPropertyChanged(string.Empty);
				}
			}
		}

		public ObservableCollection<ControlPoint> ControlPoints { get; } = new ObservableCollection<ControlPoint>();

		public Point[] ControlPosition
		{
			get
			{
				return ControlPoints.Select(cp => cp.Position).ToArray();
			}
			set
			{
				if (value != null && value.Length >= 2)
				{
					if (ControlPoints.Count == 0)
					{
						var cp1 = new ControlPoint(value[0], this, 0);
						var cp2 = new ControlPoint(value[1], this, 1);
						cp1.PropertyChanged += ControlPoint_PropertyChanged;
						cp2.PropertyChanged += ControlPoint_PropertyChanged;
						ControlPoints.Add(cp1);
						ControlPoints.Add(cp2);
					}
					else
					{
						ControlPoints[0].Position = value[0];
						ControlPoints[1].Position = value[1];
					}
					OnPropertyChanged(nameof(ControlPosition));
					OnPropertyChanged(nameof(Self));
					OnPropertyChanged(string.Empty);
				}
			}
		}

		public void InitializeBezierControlPoints()
		{
			if (ControlPoints.Count == 0 && StartVertex != null && EndVertex != null)
			{
				Point v0 = StartVertex.Position;
				Point v3 = EndVertex.Position;
				Point cp1Pos = new Point(v0.X + (v3.X - v0.X) / 3.0, v0.Y + (v3.Y - v0.Y) / 3.0);
				Point cp2Pos = new Point(v0.X + 2.0 * (v3.X - v0.X) / 3.0, v0.Y + 2.0 * (v3.Y - v0.Y) / 3.0);

				var cp1 = new ControlPoint(cp1Pos, this, 0);
				var cp2 = new ControlPoint(cp2Pos, this, 1);

				cp1.PropertyChanged += ControlPoint_PropertyChanged;
				cp2.PropertyChanged += ControlPoint_PropertyChanged;

				ControlPoints.Add(cp1);
				ControlPoints.Add(cp2);

				OnPropertyChanged(nameof(ControlPosition));
				OnPropertyChanged(nameof(Self));
				OnPropertyChanged(string.Empty);
			}
		}

		public void ClearControlPoints()
		{
			foreach (var cp in ControlPoints)
			{
				cp.PropertyChanged -= ControlPoint_PropertyChanged;
			}
			ControlPoints.Clear();
			OnPropertyChanged(nameof(ControlPosition));
			OnPropertyChanged(nameof(Self));
			OnPropertyChanged(string.Empty);
		}

		private void ControlPoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(ControlPosition));
			OnPropertyChanged(nameof(Self));
			OnPropertyChanged(string.Empty);
		}

		public ObservableCollection<Constraint> Constraints { get; set; } = new ObservableCollection<Constraint>();
		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					OnPropertyChanged(nameof(IsSelected));
				}
			}
		}
		public Edge Self => this;

		public Edge(Vertex startVertex, Vertex endVertex, EdgeType edgeType = EdgeType.Line)
		{
			_startVertex = startVertex;
			_endVertex = endVertex;
			_edgeType = edgeType;

			_startVertex.PropertyChanged += Vertex_PropertyChanged;
			_endVertex.PropertyChanged += Vertex_PropertyChanged;

			if (_edgeType == EdgeType.Bezier)
			{
				InitializeBezierControlPoints();
			}
		}

		private void Vertex_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Vertex.Position) || e.PropertyName == nameof(Vertex.X) || e.PropertyName == nameof(Vertex.Y))
			{
				OnPropertyChanged(nameof(Self));
				OnPropertyChanged(string.Empty);
			}
		}

		public Point GetCenterPoint()
		{
			if (this.EdgeType == EdgeType.Bezier && ControlPoints.Count >= 2)
			{
				// Przybliżony środek krzywej Béziera dla t = 0.5 (De Casteljau / wzór potęgowy)
				double t = 0.5;
				double u = 1 - t;
				double tt = t * t;
				double uu = u * u;
				double uuu = uu * u;
				double ttt = tt * t;

				Point p1 = ControlPoints[0].Position;
				Point p2 = ControlPoints[1].Position;

				double x = uuu * StartVertex.Position.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * EndVertex.Position.X;
				double y = uuu * StartVertex.Position.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * EndVertex.Position.Y;
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
		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
