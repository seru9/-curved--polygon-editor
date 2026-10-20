using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using _curved__polygon_editor.Algorithms;
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

		private Vertex? _selectedVertex;
		public Vertex? SelectedVertex
		{
			get => _selectedVertex;
			set
			{
				if (_selectedVertex != value)
				{
					if (_selectedVertex != null)
						_selectedVertex.IsSelected = false;
					_selectedVertex = value;
					if (_selectedVertex != null)
						_selectedVertex.IsSelected = true;
				}
			}
		}

		public void SelectVertex(Vertex? vertex)
		{
			SelectedVertex = vertex;
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
			SelectVertex(null);
		}

		private int _nextPerpendicularPairId = 1;

		public bool AddPerpendicularConstraint(Edge e1, Edge e2, out string? errorMessage)
		{
			if (e1 == e2)
			{
				errorMessage = "Wybierz dwie różne krawędzie.";
				return false;
			}

			// Usuń ewentualne poprzednie ograniczenia prostopadłości z obu krawędzi (i ich partnerów)
			RemovePerpendicularConstraintOnly(e1);
			RemovePerpendicularConstraintOnly(e2);

			int pairId = _nextPerpendicularPairId++;
			e1.Constraints.Add(new PerpendicularConstraint(e2, pairId));
			e2.Constraints.Add(new PerpendicularConstraint(e1, pairId));

			ConstraintSolver.ApplyPerpendicularConstraint(e1, e2);
			UpdateConstraintVisuals();
			errorMessage = null;
			return true;
		}

		private void RemovePerpendicularConstraintOnly(Edge edge)
		{
			var perps = edge.Constraints.OfType<PerpendicularConstraint>().ToList();
			foreach (var pc in perps)
			{
				edge.Constraints.Remove(pc);
				if (pc.TargetEdge != null)
				{
					var partnerPcs = pc.TargetEdge.Constraints.OfType<PerpendicularConstraint>()
						.Where(p => p.TargetEdge == edge || p.PairId == pc.PairId).ToList();
					foreach (var ppc in partnerPcs)
					{
						pc.TargetEdge.Constraints.Remove(ppc);
					}
				}
			}
		}

		private void RemoveEdgeConstraintsAndNotifyPartners(Edge edge)
		{
			RemovePerpendicularConstraintOnly(edge);
			edge.Constraints.Clear();
		}

		public bool DeleteSelectedVertex(out string? errorMessage)
		{
			if (SelectedVertex == null)
			{
				errorMessage = "Zaznacz wierzchołek, który chcesz usunąć.";
				return false;
			}

			var polygon = Polygons.FirstOrDefault(p => p.Vertices.Contains(SelectedVertex));
			if (polygon == null)
			{
				errorMessage = "Nie znaleziono wielokąta powiązanego z zaznaczonym wierzchołkiem.";
				return false;
			}

			if (polygon.Vertices.Count <= 3)
			{
				errorMessage = "Wielokąt musi posiadać co najmniej 3 wierzchołki.";
				return false;
			}

			var incident = polygon.Edges.Where(e => e.StartVertex == SelectedVertex || e.EndVertex == SelectedVertex).ToList();
			if (incident.Count != 2)
			{
				errorMessage = "Nie można jednoznacznie zidentyfikować krawędzi incydentnych.";
				return false;
			}

			var e1 = incident[0];
			var e2 = incident[1];

			Vertex vPrev = (e1.StartVertex == SelectedVertex) ? e1.EndVertex : e1.StartVertex;
			Vertex vNext = (e2.StartVertex == SelectedVertex) ? e2.EndVertex : e2.StartVertex;

			int insertIndex = polygon.Edges.IndexOf(e1);

			// Czyszczenie relacji z usuwanych krawędzi (w tym powiadomienie krawędzi partnerskich)
			RemoveEdgeConstraintsAndNotifyPartners(e1);
			RemoveEdgeConstraintsAndNotifyPartners(e2);

			polygon.Edges.Remove(e1);
			polygon.Edges.Remove(e2);
			SelectedEdges.Remove(e1);
			SelectedEdges.Remove(e2);

			Edge newEdge = new Edge(vPrev, vNext, EdgeType.Line);
			if (insertIndex >= 0 && insertIndex <= polygon.Edges.Count)
			{
				polygon.Edges.Insert(insertIndex, newEdge);
			}
			else
			{
				polygon.Edges.Add(newEdge);
			}

			polygon.Vertices.Remove(SelectedVertex);
			SelectedVertex = null;

			UpdateConstraintVisuals();
			errorMessage = null;
			return true;
		}

		public bool AddVertexInMiddleOfSelectedEdge(out string? errorMessage)
		{
			if (SelectedEdges.Count != 1)
			{
				errorMessage = "Zaznacz dokładnie jedną krawędź, w której chcesz dodać wierzchołek.";
				return false;
			}

			var edge = SelectedEdges[0];
			var polygon = Polygons.FirstOrDefault(p => p.Edges.Contains(edge));
			if (polygon == null)
			{
				errorMessage = "Nie znaleziono wielokąta zawierającego wybraną krawędź.";
				return false;
			}

			Point mid = edge.GetCenterPoint();
			Vertex newVertex = new Vertex(mid);

			Vertex v1 = edge.StartVertex;
			Vertex v2 = edge.EndVertex;

			int v1Index = polygon.Vertices.IndexOf(v1);
			int v2Index = polygon.Vertices.IndexOf(v2);

			if (v1Index >= 0 && v2Index == 0 && v1Index == polygon.Vertices.Count - 1)
			{
				polygon.Vertices.Add(newVertex);
			}
			else if (v2Index >= 0)
			{
				polygon.Vertices.Insert(v2Index, newVertex);
			}
			else if (v1Index >= 0)
			{
				polygon.Vertices.Insert(v1Index + 1, newVertex);
			}
			else
			{
				polygon.Vertices.Add(newVertex);
			}

			Edge e1 = new Edge(v1, newVertex, EdgeType.Line);
			Edge e2 = new Edge(newVertex, v2, EdgeType.Line);

			int edgeIndex = polygon.Edges.IndexOf(edge);

			// Czyszczenie relacji z dzielonej krawędzi
			RemoveEdgeConstraintsAndNotifyPartners(edge);

			polygon.Edges.Remove(edge);
			SelectedEdges.Remove(edge);

			if (edgeIndex >= 0)
			{
				polygon.Edges.Insert(edgeIndex, e1);
				polygon.Edges.Insert(edgeIndex + 1, e2);
			}
			else
			{
				polygon.Edges.Add(e1);
				polygon.Edges.Add(e2);
			}

			SelectVertex(newVertex);
			UpdateConstraintVisuals();
			errorMessage = null;
			return true;
		}

		public void AddLengthConstraint(Edge edge, double targetLength)
		{
			var existing = edge.Constraints.OfType<LengthConstraint>().FirstOrDefault();
			if (existing != null)
			{
				edge.Constraints.Remove(existing);
			}

			edge.Constraints.Add(new LengthConstraint(targetLength));
			ConstraintSolver.ApplyLengthConstraint(edge, targetLength);
			UpdateConstraintVisuals();
		}

		public void RemoveConstraintsFromSelected()
		{
			foreach (var edge in SelectedEdges)
			{
				RemoveEdgeConstraintsAndNotifyPartners(edge);
			}
			UpdateConstraintVisuals();
		}

		public void UpdateConstraintVisuals()
		{
			ConstraintVisuals.Clear();
			foreach (var polygon in Polygons)
			{
				foreach (var edge in polygon.Edges)
				{
					var activeConstraints = new List<ConstraintVisual>();

					var lengthConstraint = edge.Constraints.OfType<LengthConstraint>().FirstOrDefault(c => c.IsActive);
					if (lengthConstraint != null)
					{
						activeConstraints.Add(new ConstraintVisual
						{
							Text = $"L={lengthConstraint.TargetLength:0.##}",
							Background = "#FFFFF3B0",
							BorderBrush = "Orange",
							Foreground = "#B8860B"
						});
					}

					var perpConstraint = edge.Constraints.OfType<PerpendicularConstraint>().FirstOrDefault(c => c.IsActive);
					if (perpConstraint != null)
					{
						activeConstraints.Add(new ConstraintVisual
						{
							Text = $"⟂{perpConstraint.PairId}",
							Background = "#E1F5FE",
							BorderBrush = "#03A9F4",
							Foreground = "#0277BD"
						});
					}

					if (activeConstraints.Count == 0) continue;

					Point center = edge.GetCenterPoint();
					Vector dir = edge.EndVertex.Position - edge.StartVertex.Position;
					if (dir.Length > 0.0001)
					{
						dir.Normalize();
					}
					else
					{
						dir = new Vector(1, 0);
					}

					if (activeConstraints.Count == 1)
					{
						var visual = activeConstraints[0];
						visual.X = center.X;
						visual.Y = center.Y;
						ConstraintVisuals.Add(visual);
					}
					else
					{
						// Gdy jest kilka ograniczeń na krawędzi, rozsuwamy je wzdłuż krawędzi
						double spacing = 22.0;
						double startOffset = -(activeConstraints.Count - 1) * spacing / 2.0;

						for (int i = 0; i < activeConstraints.Count; i++)
						{
							var visual = activeConstraints[i];
							double offset = startOffset + i * spacing;
							visual.X = center.X + dir.X * offset;
							visual.Y = center.Y + dir.Y * offset;
							ConstraintVisuals.Add(visual);
						}
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
