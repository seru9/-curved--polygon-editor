using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _curved__polygon_editor.Models
{
	public class Polygon : INotifyPropertyChanged
	{
		private ObservableCollection<Vertex> _vertices;
		private ObservableCollection<Edge> _edges;

		public ObservableCollection<Vertex> Vertices
		{
			get { return _vertices; }
			set
			{
				_vertices = value;
				OnPropertyChanged(nameof(Vertices));
			}
		}

		public ObservableCollection<Edge> Edges
		{
			get { return _edges; }
			set
			{
				_edges = value;
				if (_edges != null)
				{
					HookEdgesCollection(_edges);
				}
				OnPropertyChanged(nameof(Edges));
			}
		}

		public Polygon()
		{
			_vertices = new ObservableCollection<Vertex>();
			_edges = new ObservableCollection<Edge>();
			HookEdgesCollection(_edges);
		}

		private void HookEdgesCollection(ObservableCollection<Edge> edges)
		{
			foreach (var edge in edges)
			{
				edge.OwnerPolygon = this;
			}
			edges.CollectionChanged += (s, e) =>
			{
				if (e.NewItems != null)
				{
					foreach (Edge edge in e.NewItems)
					{
						edge.OwnerPolygon = this;
					}
				}
			};
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
