using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
		public Edge(Vertex startVertex, Vertex endVertex, EdgeType edgeType = EdgeType.Line)
		{
			StartVertex = startVertex;
			EndVertex = endVertex;
			EdgeType = edgeType;
			ControlPosition = new Point[0];
		}
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
