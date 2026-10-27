using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using _curved__polygon_editor.Algorithms;
using _curved__polygon_editor.Models;
using _curved__polygon_editor.ViewModels;

namespace _curved__polygon_editor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainViewModel _vm;
		private Vertex? _draggedVertex = null;
		private Polygon? _draggedPolygon = null;
		private Polygon? _draggedWholePolygon = null;
		private Point _lastDragPosition;

		public MainWindow()
		{
			InitializeComponent();
			_vm = new MainViewModel();
			DataContext = _vm;
		}

		private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var pos = e.GetPosition((IInputElement)sender);

			// 1a. Sprawdź, czy użytkownik kliknął w główny wierzchołek wielokąta (promień 10px)
			const double vertexHitRadius = 10.0;
			foreach (var polygon in _vm.Polygons)
			{
				foreach (var vertex in polygon.Vertices)
				{
					double dist = (pos - vertex.Position).Length;
					if (dist <= vertexHitRadius)
					{
						_vm.ClearSelection();
						_vm.SelectVertex(vertex);
						_draggedVertex = vertex;
						_draggedPolygon = polygon;
						((IInputElement)sender).CaptureMouse();
						e.Handled = true;
						return;
					}
				}
			}

			// 1b. Sprawdź, czy użytkownik kliknął w punkt kontrolny krzywej Béziera (V1 lub V2)
			foreach (var polygon in _vm.Polygons)
			{
				foreach (var edge in polygon.Edges)
				{
					if (!edge.IsBezier) continue;

					foreach (var cp in new[] { edge.ControlPoint1, edge.ControlPoint2 })
					{
						if (cp != null && (pos - cp.Position).Length <= vertexHitRadius)
						{
							_vm.ClearSelection();
							_vm.SelectVertex(null);
							_draggedVertex = cp;
							_draggedPolygon = polygon;
							((IInputElement)sender).CaptureMouse();
							e.Handled = true;
							return;
						}
					}
				}
			}

			// 2. Sprawdź, czy kliknięto w krawędź
			Edge? nearest = null;
			double best = double.MaxValue;

			foreach (var polygon in _vm.Polygons)
			{
				foreach (var edge in polygon.Edges)
				{
					double distance = DistanceToEdge(edge, pos);
					if (distance < best)
					{
						best = distance;
						nearest = edge;
					}
				}
			}

			if (nearest != null && best < 8)
			{
				_vm.SelectVertex(null);
				_vm.ToggleSelection(nearest);
				e.Handled = true;
				return;
			}

			// 3. Sprawdź, czy kliknięto we wnętrzu wielokąta
			foreach (var polygon in _vm.Polygons)
			{
				if (ConstraintSolver.IsPointInPolygon(pos, polygon))
				{
					_draggedWholePolygon = polygon;
					_lastDragPosition = pos;
					_vm.ClearSelection();
					_vm.SelectVertex(null);
					((IInputElement)sender).CaptureMouse();
					e.Handled = true;
					return;
				}
			}

			// 4. Kliknięcie w pustą przestrzeń = odznaczenie
			_vm.ClearSelection();
			_vm.SelectVertex(null);
		}

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (_draggedVertex != null && e.LeftButton == MouseButtonState.Pressed)
			{
				var pos = e.GetPosition((IInputElement)sender);

				// Przeciągany wierzchołek (lub punkt kontrolny V1/V2) podąża za kursorem
				_draggedVertex.Position = pos;

				// Solver dopasowuje pozostałe wierzchołki wielokąta
				ConstraintSolver.Solve(_draggedPolygon, _draggedVertex);

				// Aktualizacja ikon ograniczeń
				_vm.UpdateConstraintVisuals();
			}
			else if (_draggedWholePolygon != null && e.LeftButton == MouseButtonState.Pressed)
			{
				var pos = e.GetPosition((IInputElement)sender);
				Vector delta = pos - _lastDragPosition;

				foreach (var v in _draggedWholePolygon.Vertices)
				{
					v.Position = new Point(v.Position.X + delta.X, v.Position.Y + delta.Y);
				}

				// Przesunięcie punktów kontrolnych krzywych Béziera oraz odświeżenie łuków
				foreach (var edge in _draggedWholePolygon.Edges)
				{
					if (edge.IsBezier)
					{
						if (edge.ControlPoint1 != null)
							edge.ControlPoint1.Position = new Point(edge.ControlPoint1.Position.X + delta.X, edge.ControlPoint1.Position.Y + delta.Y);
						if (edge.ControlPoint2 != null)
							edge.ControlPoint2.Position = new Point(edge.ControlPoint2.Position.X + delta.X, edge.ControlPoint2.Position.Y + delta.Y);
					}
					else if (edge.IsArc)
					{
						edge.NotifyArcChanged();
					}
				}

				_lastDragPosition = pos;
				_vm.UpdateConstraintVisuals();
			}
		}

		private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_draggedVertex != null || _draggedWholePolygon != null)
			{
				_draggedVertex = null;
				_draggedPolygon = null;
				_draggedWholePolygon = null;
				((IInputElement)sender).ReleaseMouseCapture();
			}
		}

		private static double DistanceToEdge(Edge edge, Point pos)
		{
			if (edge.IsArc || edge.IsBezier)
			{
				var geom = _curved__polygon_editor.Helpers.EdgeToGeometryConverter.GetGeometry(edge);
				if (geom != null && geom.StrokeContains(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 16), pos))
				{
					return 0;
				}
			}

			return DistanceToSegment(pos, edge.StartVertex.Position, edge.EndVertex.Position);
		}

		private static double DistanceToSegment(Point point, Point a, Point b)
		{
			double dx = b.X - a.X;
			double dy = b.Y - a.Y;

			if (dx == 0 && dy == 0)
				return (point - a).Length;

			double t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy);
			t = Math.Clamp(t, 0, 1);

			var projection = new Point(a.X + t * dx, a.Y + t * dy);
			return (point - projection).Length;
		}

		private void AddVertexButton_Click(object sender, RoutedEventArgs e)
		{
			if (!_vm.AddVertexInMiddleOfSelectedEdge(out string? error))
			{
				MessageBox.Show(error, "Dodawanie wierzchołka", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void DeleteVertexButton_Click(object sender, RoutedEventArgs e)
		{
			if (!_vm.DeleteSelectedVertex(out string? error))
			{
				MessageBox.Show(error, "Usuwanie wierzchołka", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				if (_vm.SelectedVertex != null)
				{
					if (!_vm.DeleteSelectedVertex(out string? error))
					{
						MessageBox.Show(error, "Usuwanie wierzchołka", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					e.Handled = true;
				}
			}
		}

		private void LengthButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.SelectedEdges.Count != 1)
			{
				MessageBox.Show("Zaznacz dokładnie jedną krawędź.", "Długość krawędzi", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var edge = _vm.SelectedEdges[0];
			if (edge.IsBezier || edge.IsArc)
			{
				MessageBox.Show("Ograniczenie długości można nałożyć tylko na zwykły odcinek.", "Długość krawędzi", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			double current = GetEdgeLength(edge);
			double? result = PromptLength(current);
			if (result.HasValue)
				_vm.AddLengthConstraint(edge, result.Value);
		}

		private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.ClearSelection();
		}

		private void RemoveConstraintButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.SelectedEdges.Count == 0)
			{
				MessageBox.Show("Zaznacz krawędź, z której chcesz usunąć ograniczenie.", "Usuń ograniczenie", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			_vm.RemoveConstraintsFromSelected();
		}

		private static double GetEdgeLength(Edge edge)
		{
			var start = edge.StartVertex.Position;
			var end = edge.EndVertex.Position;
			return new Vector(end.X - start.X, end.Y - start.Y).Length;
		}

		private double? PromptLength(double defaultValue)
		{
			var win = new Window
			{
				Title = "Długość krawędzi",
				Width = 320,
				SizeToContent = SizeToContent.Height,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Owner = this,
				ResizeMode = ResizeMode.NoResize
			};

			var textBox = new TextBox
			{
				Text = defaultValue.ToString("0.##", CultureInfo.InvariantCulture),
				Margin = new Thickness(10),
				VerticalContentAlignment = VerticalAlignment.Center
			};

			var ok = new Button { Content = "OK", Width = 80, Margin = new Thickness(10, 0, 5, 0), IsDefault = true };
			var cancel = new Button { Content = "Anuluj", Width = 80, Margin = new Thickness(5, 0, 10, 0), IsCancel = true };

			var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 0, 10) };
			buttons.Children.Add(ok);
			buttons.Children.Add(cancel);

			var root = new StackPanel();
			root.Children.Add(textBox);
			root.Children.Add(buttons);
			win.Content = root;

			ok.Click += (s, e) => win.DialogResult = true;
			win.Loaded += (s, e) => { textBox.Focus(); textBox.SelectAll(); };

			if (win.ShowDialog() == true &&
				double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) &&
				value > 0)
				return value;

			return null;
		}

		private void PerpendicularButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.SelectedEdges.Count != 2)
			{
				MessageBox.Show("Zaznacz dokładnie dwie krawędzie, aby ustawić prostopadłość.",
								"Prostopadłość krawędzi",
								MessageBoxButton.OK,
								MessageBoxImage.Information);
				return;
			}

			var edge1 = _vm.SelectedEdges[0];
			var edge2 = _vm.SelectedEdges[1];

			if (edge1.IsBezier || edge1.IsArc || edge2.IsBezier || edge2.IsArc)
			{
				MessageBox.Show("Więzy prostopadłości można nakładać tylko na zwykłe odcinki.",
								"Prostopadłość krawędzi",
								MessageBoxButton.OK,
								MessageBoxImage.Information);
				return;
			}

			if (!_vm.AddPerpendicularConstraint(edge1, edge2, out string? error))
			{
				MessageBox.Show(error, "Prostopadłość krawędzi", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void BezierButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.SelectedEdges.Count == 0)
			{
				MessageBox.Show("Zaznacz krawędź, aby przełączyć tryb krzywej Béziera.",
								"Krzywa Béziera",
								MessageBoxButton.OK,
								MessageBoxImage.Information);
				return;
			}

			_vm.RemoveConstraintsFromSelected();

			foreach (var edge in _vm.SelectedEdges)
			{
				if (edge.IsBezier)
				{
					edge.DisableBezier();
				}
				else
				{
					edge.DisableArc();
					edge.EnableBezier();
				}
			}

			ConstraintSolver.Solve(_vm.Polygons);
			_vm.UpdateConstraintVisuals();
		}

		private void ArcButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.SelectedEdges.Count == 0)
			{
				MessageBox.Show("Zaznacz krawędź, aby przełączyć tryb łuku okręgu.",
								"Łuk okręgu",
								MessageBoxButton.OK,
								MessageBoxImage.Information);
				return;
			}

			_vm.RemoveConstraintsFromSelected();

			foreach (var edge in _vm.SelectedEdges)
			{
				if (edge.IsArc)
				{
					edge.DisableArc();
				}
				else
				{
					Polygon? owner = _vm.Polygons.FirstOrDefault(p => p.Edges.Contains(edge));
					edge.EnableArc(owner);
				}
			}

			ConstraintSolver.Solve(_vm.Polygons);
			_vm.UpdateConstraintVisuals();
		}

		private void SetG0Button_Click(object sender, RoutedEventArgs e) => SetSelectedVertexContinuity(ContinuityType.G0);
		private void SetG1Button_Click(object sender, RoutedEventArgs e) => SetSelectedVertexContinuity(ContinuityType.G1);
		private void SetC1Button_Click(object sender, RoutedEventArgs e) => SetSelectedVertexContinuity(ContinuityType.C1);

		private void SetSelectedVertexContinuity(ContinuityType continuity)
		{
			var vertex = _vm.SelectedVertex;
			if (vertex == null)
			{
				MessageBox.Show("Zaznacz wierzchołek, aby określić jego klasę ciągłości.",
								"Ciągłość wierzchołka",
								MessageBoxButton.OK,
								MessageBoxImage.Information);
				return;
			}

			Polygon? ownerPolygon = null;
			bool touchesBezier = false;
			var adjacentArcs = new System.Collections.Generic.List<Edge>();

			foreach (var poly in _vm.Polygons)
			{
				if (poly.Vertices.Contains(vertex))
				{
					ownerPolygon = poly;
					foreach (var edge in poly.Edges)
					{
						if (edge.StartVertex == vertex || edge.EndVertex == vertex)
						{
							if (edge.IsBezier) touchesBezier = true;
							if (edge.IsArc) adjacentArcs.Add(edge);
						}
					}
					break;
				}
			}

			if (!touchesBezier && adjacentArcs.Count == 0)
			{
				MessageBox.Show("Wybrany wierzchołek nie sąsiaduje z segmentem Béziera ani z łukiem okręgu.",
								"Ciągłość wierzchołka",
								MessageBoxButton.OK,
								MessageBoxImage.Information);
				return;
			}

			// Walidacja wymagań dla łuku okręgu
			if (adjacentArcs.Count > 0)
			{
				if (continuity == ContinuityType.C1)
				{
					MessageBox.Show("Dla wierzchołka sąsiadującego z łukiem okręgu dostępne są tylko klasy ciągłości G0 oraz G1.",
									"Ciągłość łuku",
									MessageBoxButton.OK,
									MessageBoxImage.Warning);
					return;
				}

				if (continuity == ContinuityType.G1)
				{
					foreach (var arc in adjacentArcs)
					{
						// Tylko jeden koniec łuku może mieć ustawioną ciągłość G1
						Vertex otherEnd = (arc.StartVertex == vertex) ? arc.EndVertex : arc.StartVertex;
						if (otherEnd.Continuity == ContinuityType.G1)
						{
							otherEnd.Continuity = ContinuityType.G0;
							MessageBox.Show("Tylko jeden koniec łuku może mieć ustawioną ciągłość G1.\nDrugi koniec łuku został automatycznie przełączony na G0.",
											"Ciągłość łuku",
											MessageBoxButton.OK,
											MessageBoxImage.Information);
						}
					}
				}
			}

			vertex.IsContinuityExplicitlySet = true;
			vertex.Continuity = continuity;

			ConstraintSolver.Solve(ownerPolygon, vertex);
			foreach (var arc in adjacentArcs)
			{
				arc.NotifyArcChanged();
			}
			_vm.UpdateConstraintVisuals();
		}
		private void LineDrawingMode_Checked(object sender, RoutedEventArgs e)
		{
			if (_vm == null) return; 

			_curved__polygon_editor.Helpers.EdgeToGeometryConverter.UseBresenham = (BresenhamLineRadio.IsChecked == true);
			_vm.RefreshAllEdges();
		}

		private void DocumentationButton_Click(object sender, RoutedEventArgs e)
		{
			var docWindow = new DocumentationWindow
			{
				Owner = this
			};
			docWindow.ShowDialog();
		}
	}
}