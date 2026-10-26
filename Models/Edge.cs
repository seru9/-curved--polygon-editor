using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
		private Vertex? _controlPoint1;
		private Vertex? _controlPoint2;
		private EdgeType _edgeType;
		private bool _isBezier;
		public Polygon? OwnerPolygon { get; set; }
		public bool IsArc => _edgeType == EdgeType.Arc;

		public void EnableArc(Polygon? ownerPolygon)
		{
			if (ownerPolygon != null)
				OwnerPolygon = ownerPolygon;

			DisableBezier();

			if (!StartVertex.IsContinuityExplicitlySet || StartVertex.Continuity == ContinuityType.C1)
				StartVertex.Continuity = ContinuityType.G0;

			if (!EndVertex.IsContinuityExplicitlySet || EndVertex.Continuity == ContinuityType.C1)
				EndVertex.Continuity = ContinuityType.G0;

			if (StartVertex.Continuity == ContinuityType.G1 && EndVertex.Continuity == ContinuityType.G1)
				EndVertex.Continuity = ContinuityType.G0;

			EdgeType = EdgeType.Arc;
			OnPropertyChanged(nameof(EdgeType));
			OnPropertyChanged(nameof(IsArc));
			OnPropertyChanged(nameof(Self));
			OnPropertyChanged(string.Empty);
		}

		public void DisableArc()
		{
			if (EdgeType == EdgeType.Arc)
			{
				EdgeType = EdgeType.Line;
				OnPropertyChanged(nameof(EdgeType));
				OnPropertyChanged(nameof(IsArc));
				OnPropertyChanged(nameof(Self));
				OnPropertyChanged(string.Empty);
			}
		}

		public void NotifyArcChanged()
		{
			OnPropertyChanged(nameof(Self));
			OnPropertyChanged(string.Empty);
		}

		public bool IsBezier
		{
			get => _isBezier;
			set
			{
				if (_isBezier != value)
				{
					_isBezier = value;
					OnPropertyChanged(nameof(IsBezier));
					OnPropertyChanged(nameof(Self));
					OnPropertyChanged(string.Empty);
				}
			}
		}

		public Vertex? ControlPoint1
		{
			get => _controlPoint1;
			set
			{
				if (_controlPoint1 != value)
				{
					if (_controlPoint1 != null)
						_controlPoint1.PropertyChanged -= Vertex_PropertyChanged;
					_controlPoint1 = value;
					if (_controlPoint1 != null)
						_controlPoint1.PropertyChanged += Vertex_PropertyChanged;
					OnPropertyChanged(nameof(ControlPoint1));
					OnPropertyChanged(nameof(Self));
					OnPropertyChanged(string.Empty);
				}
			}
		}

		public Vertex? ControlPoint2
		{
			get => _controlPoint2;
			set
			{
				if (_controlPoint2 != value)
				{
					if (_controlPoint2 != null)
						_controlPoint2.PropertyChanged -= Vertex_PropertyChanged;
					_controlPoint2 = value;
					if (_controlPoint2 != null)
						_controlPoint2.PropertyChanged += Vertex_PropertyChanged;
					OnPropertyChanged(nameof(ControlPoint2));
					OnPropertyChanged(nameof(Self));
					OnPropertyChanged(string.Empty);
				}
			}
		}

		public Vertex StartVertex
		{
			get => _startVertex;
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
					OnPropertyChanged(nameof(Self));
					OnPropertyChanged(string.Empty);
				}
			}
		}

		public Vertex EndVertex
		{
			get => _endVertex;
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
					OnPropertyChanged(nameof(Self));
					OnPropertyChanged(string.Empty);
				}
			}
		}

		public void EnableBezier()
		{
			if (!StartVertex.IsContinuityExplicitlySet)
				StartVertex.Continuity = ContinuityType.C1;

			if (!EndVertex.IsContinuityExplicitlySet)
				EndVertex.Continuity = ContinuityType.C1;

			double dx = (EndVertex.X - StartVertex.X) / 3.0;
			double dy = (EndVertex.Y - StartVertex.Y) / 3.0;

			double perpX = -dy * 0.3;
			double perpY = dx * 0.3;

			ControlPoint1 = new Vertex(new Point(StartVertex.X + dx + perpX, StartVertex.Y + dy + perpY), ContinuityType.G0);
			ControlPoint2 = new Vertex(new Point(StartVertex.X + 2 * dx + perpX, StartVertex.Y + 2 * dy + perpY), ContinuityType.G0);

			EdgeType = EdgeType.Bezier;
			IsBezier = true;
		}

		public void DisableBezier()
		{
			IsBezier = false;
			EdgeType = EdgeType.Line;
			ControlPoint1 = null;
			ControlPoint2 = null;
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
					OnPropertyChanged(nameof(Self));
					OnPropertyChanged(string.Empty);
				}
			}
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
			return new Point(
				(StartVertex.Position.X + EndVertex.Position.X) / 2.0,
				(StartVertex.Position.Y + EndVertex.Position.Y) / 2.0
			);
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
