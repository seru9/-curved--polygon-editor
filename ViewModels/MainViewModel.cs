using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using _curved__polygon_editor.Models;
using _curved__polygon_editor.Models.Constraints;

namespace _curved__polygon_editor.ViewModels
{
	public class MainViewModel
	{
		public ObservableCollection<Polygon> Polygons { get; set; } = new ObservableCollection<Polygon>();
		public ObservableCollection<Edge> SelectedEdges { get; } = new ObservableCollection<Edge>();
		public ObservableCollection<ConstraintVisual> ConstraintVisuals { get; } = new ObservableCollection<ConstraintVisual>();

		public MainViewModel()
		{
			InitializeDemoScene();
			UpdateConstraintVisuals();
		}

		public void ToggleSelection(Edge edge)
		{
			edge.IsSelected = !edge.IsSelected;
			if (edge.IsSelected)
			{
				if (!SelectedEdges.Contains(edge))
					SelectedEdges.Add(edge);
			}
			else
			{
				SelectedEdges.Remove(edge);
			}
		}

		public void ClearSelection()
		{
			foreach (var edge in SelectedEdges)
			{
				edge.IsSelected = false;
			}
			SelectedEdges.Clear();
		}

		public void AddLengthConstraint(Edge edge, double targetLength)
		{
			var existing = edge.Constraints.OfType<LengthConstraint>().FirstOrDefault();
			if (existing != null)
			{
				edge.Constraints.Remove(existing);
			}

			edge.Constraints.Add(new LengthConstraint(targetLength));
			UpdateConstraintVisuals();
		}

		public void UpdateConstraintVisuals()
		{
			ConstraintVisuals.Clear();
			foreach (var polygon in Polygons)
			{
				foreach (var edge in polygon.Edges)
				{
					var lengthConstraint = edge.Constraints.OfType<LengthConstraint>().FirstOrDefault(c => c.IsActive);
					if (lengthConstraint != null)
					{
						var center = edge.GetCenterPoint();
						ConstraintVisuals.Add(new ConstraintVisual
						{
							X = center.X,
							Y = center.Y,
							Text = $"L={lengthConstraint.TargetLength:0.##}"
						});
					}
				}
			}
		}
		private void InitializeDemoScene()
		{
			// Wielokąt 1 (np. trójkąt)
			var p1 = new Polygon();
			var v1 = new Vertex(new Point(50, 50));
			var v2 = new Vertex(new Point(200, 50));
			var v3 = new Vertex(new Point(125, 180));

			p1.Vertices.Add(v1);
			p1.Vertices.Add(v2);
			p1.Vertices.Add(v3);

			p1.Edges.Add(new Edge(v1, v2, EdgeType.Line));
			p1.Edges.Add(new Edge(v2, v3, EdgeType.Line));
			p1.Edges.Add(new Edge(v3, v1, EdgeType.Line));

			Polygons.Add(p1);

			// Możesz w bardzo prosty i czytelny sposób dodać kolejne wielokąty
			var p2 = new Polygon();
			var v4 = new Vertex(new Point(300, 100));
			var v5 = new Vertex(new Point(450, 100));
			var v6 = new Vertex(new Point(450, 250));
			var v7 = new Vertex(new Point(300, 250));

			p2.Vertices.Add(v4);
			p2.Vertices.Add(v5);
			p2.Vertices.Add(v6);
			p2.Vertices.Add(v7);

			p2.Edges.Add(new Edge(v4, v5, EdgeType.Line));
			p2.Edges.Add(new Edge(v5, v6, EdgeType.Line));
			p2.Edges.Add(new Edge(v6, v7, EdgeType.Line));
			p2.Edges.Add(new Edge(v7, v4, EdgeType.Line));

			Polygons.Add(p2);
		}
	}

	}
