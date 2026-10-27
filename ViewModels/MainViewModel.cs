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
		public void RefreshAllEdges()
		{
			foreach (var polygon in Polygons)
			{
				foreach (var edge in polygon.Edges)
				{
					edge.NotifyArcChanged();
				}
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
			RemovePerpendicularConstraintsForEdge(e1);
			RemovePerpendicularConstraintsForEdge(e2);
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
			RemovePerpendicularConstraintsForEdge(edge);
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
				// removing constrains from select
				var existingLen = edge.Constraints.OfType<LengthConstraint>().ToList();
				foreach (var c in existingLen)
				{
					edge.Constraints.Remove(c);
				}

				// removing the PerpendicularEdge for the edge's target edge
				RemovePerpendicularConstraintsForEdge(edge);
			}
			UpdateConstraintVisuals();
		}

		public void UpdateConstraintVisuals()
		{
			ConstraintVisuals.Clear();

			var perpLabels = new Dictionary<Edge, List<string>>();
			int pairIndex = 1;

			foreach (var polygon in Polygons)
			{
				foreach (var edge in polygon.Edges)
				{
					foreach (var perp in edge.Constraints.OfType<PerpendicularConstraint>().Where(c => c.IsActive))
					{
						string tag = $"⊥{pairIndex}";
						if (!perpLabels.ContainsKey(edge)) perpLabels[edge] = new List<string>();
						if (!perpLabels.ContainsKey(perp.TargetEdge)) perpLabels[perp.TargetEdge] = new List<string>();

						perpLabels[edge].Add(tag);
						perpLabels[perp.TargetEdge].Add(tag);
						pairIndex++;
					}
				}
			}

			foreach (var polygon in Polygons)
			{
				foreach (var edge in polygon.Edges)
				{
					var parts = new List<string>();

					var lengthConstraint = edge.Constraints.OfType<LengthConstraint>().FirstOrDefault(c => c.IsActive);
					if (lengthConstraint != null)
					{
						parts.Add($"L={lengthConstraint.TargetLength:0.##}");
					}

					if (perpLabels.TryGetValue(edge, out var tags))
					{
						parts.AddRange(tags);
					}

					if (parts.Count > 0)
					{
						var center = edge.GetCenterPoint();
						ConstraintVisuals.Add(new ConstraintVisual
						{
							X = center.X,
							Y = center.Y,
							Text = string.Join(" | ", parts)
						});
					}
				}
			}
		}
		private void InitializeDemoScene()
		{
			// 1. Wielokąt nr 1 (czworokąt z ograniczeniami długości, prostopadłości i segmentem Béziera)
			var p1 = CreatePolygon(
				new Point(60, 80),
				new Point(220, 80),
				new Point(220, 240),
				new Point(60, 240)
			);

			// Krawędź 0 (górna): stała długość L = 160
			p1.Edges[0].Constraints.Add(new LengthConstraint(160));

			// Krawędź 0 (górna) i Krawędź 3 (lewa): więzy prostopadłości (⊥)
			p1.Edges[0].Constraints.Add(new PerpendicularConstraint(p1.Edges[3]));

			// Krawędź 1 (prawa): zamieniona na krzywą Béziera z ciągłościami C1 i G1 na końcach
			p1.Edges[1].EnableBezier();
			p1.Vertices[1].Continuity = ContinuityType.C1;
			p1.Vertices[1].IsContinuityExplicitlySet = true;
			p1.Vertices[2].Continuity = ContinuityType.G1;
			p1.Vertices[2].IsContinuityExplicitlySet = true;


			// 2. Wielokąt nr 2 (pięciokąt prezentujący m.in. łuk okręgu, Beziera oraz więzy geometryczne)
			var p2 = CreatePolygon(
				new Point(360, 80),
				new Point(520, 80),
				new Point(570, 190),
				new Point(440, 280),
				new Point(340, 190)
			);

			// Krawędź 0 (górna): stała długość L = 160
			p2.Edges[0].Constraints.Add(new LengthConstraint(160));

			// Krawędź 1 (prawa górna): łuk okręgu z ciągłością G1 w wierzchołku początkowym
			p2.Edges[1].EnableArc(p2);
			p2.Vertices[1].Continuity = ContinuityType.G1;
			p2.Vertices[1].IsContinuityExplicitlySet = true;

			// Krawędź 3 (lewa dolna): segment Béziera
			p2.Edges[3].EnableBezier();

			// Krawędź 0 i Krawędź 4: prostopadłość
			p2.Edges[0].Constraints.Add(new PerpendicularConstraint(p2.Edges[4]));


			// --- Dodanie kolejnego wielokąta w kodzie to tylko jedno wywołanie: ---
			// var p3 = CreatePolygon(new Point(620, 80), new Point(740, 80), new Point(680, 200));

			// Przeliczenie solvera dla całej sceny na starcie, aby dopasować punkty kontrolne i więzy
			ConstraintSolver.Solve(Polygons);
		}

		private Polygon CreatePolygon(params Point[] points)
		{
			if (points == null || points.Length < 3)
				throw new ArgumentException("Wielokąt musi mieć co najmniej 3 wierzchołki.");

			var polygon = new Polygon();

			foreach (var pt in points)
			{
				polygon.Vertices.Add(new Vertex(pt));
			}

			for (int i = 0; i < polygon.Vertices.Count; i++)
			{
				var vStart = polygon.Vertices[i];
				var vEnd = polygon.Vertices[(i + 1) % polygon.Vertices.Count];
				polygon.Edges.Add(new Edge(vStart, vEnd, EdgeType.Line) { OwnerPolygon = polygon });
			}

			Polygons.Add(polygon);
			return polygon;
		}
		public bool AddPerpendicularConstraint(Edge edge1, Edge edge2, out string? errorMessage)
		{
			if (edge1 == edge2)
			{
				errorMessage = "Wybierz dwie różne krawędzie.";
				return false;
			}

			// optional, the perpendicular contraint has to be on edges from the same polygon
			var poly1 = Polygons.FirstOrDefault(p => p.Edges.Contains(edge1));
			var poly2 = Polygons.FirstOrDefault(p => p.Edges.Contains(edge2));
			if (poly1 == null || poly1 != poly2)
			{
				errorMessage = "Wybrane krawędzie muszą należeć do tego samego wielokąta.";
				return false;
			}

		
			RemovePerpendicularConstraintsForEdge(edge1);
			RemovePerpendicularConstraintsForEdge(edge2);

			// dodajemy ograniczenie (wystarczy dodać do edge1 ze wskazaniem na edge2, 
			// lub symetrycznie do obu - jeśli dodasz tylko do edge1, solver w każdej iteracji wykona obrót raz na parę)
			edge1.Constraints.Add(new PerpendicularConstraint(edge2));

			// solver for edges to be in their new positions
			ConstraintSolver.Solve(Polygons);
			UpdateConstraintVisuals();

			errorMessage = null;
			return true;
		}

		private void RemovePerpendicularConstraintsForEdge(Edge targetEdge)
		{
			// remove the Perpendicular constraint directly from the edge
			var ownPerp = targetEdge.Constraints.OfType<PerpendicularConstraint>().ToList();
			foreach (var c in ownPerp)
			{
				targetEdge.Constraints.Remove(c);
			}

			// remove the p. constraint from the targetEdge
			foreach (var polygon in Polygons)
			{
				foreach (var edge in polygon.Edges)
				{
					var pointingToTarget = edge.Constraints
						.OfType<PerpendicularConstraint>()
						.Where(c => c.TargetEdge == targetEdge)
						.ToList();

					foreach (var c in pointingToTarget)
					{
						edge.Constraints.Remove(c);
					}
				}
			}
		}
	}

	}
