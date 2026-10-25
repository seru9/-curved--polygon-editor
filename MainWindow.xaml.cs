using System;
using System.Globalization;
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
        private ControlPoint? _draggedControlPoint = null;
        private Edge? _draggedEdge = null;
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

            // 1. Sprawdź, czy użytkownik kliknął w punkt kontrolny Béziera (tolerancja promienia 10px)
            const double cpHitRadius = 10.0;
            foreach (var polygon in _vm.Polygons)
            {
                foreach (var edge in polygon.Edges)
                {
                    if (edge.EdgeType == EdgeType.Bezier)
                    {
                        foreach (var cp in edge.ControlPoints)
                        {
                            double dist = (pos - cp.Position).Length;
                            if (dist <= cpHitRadius)
                            {
                                _vm.ClearSelection();
                                _vm.SelectControlPoint(cp);
                                _draggedControlPoint = cp;
                                _draggedEdge = edge;
                                _draggedPolygon = polygon;
                                ((IInputElement)sender).CaptureMouse();
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                }
            }

            // 2. Sprawdź, czy użytkownik kliknął w wierzchołek (tolerancja promienia 10px)
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

            // 3. Jeśli nie wierzchołek/punkt kontrolny, sprawdź czy kliknięto w krawędź
            Edge? nearest = null;
            double best = double.MaxValue;

            foreach (var polygon in _vm.Polygons)
            {
                foreach (var edge in polygon.Edges)
                {
                    double distance = DistanceToEdge(pos, edge);
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
                _vm.SelectControlPoint(null);
                _vm.ToggleSelection(nearest);
                e.Handled = true;
                return;
            }

            // 4. Sprawdź, czy kliknięto we wnętrzu wielokąta (przesuwanie całego wielokąta)
            foreach (var polygon in _vm.Polygons)
            {
                if (ConstraintSolver.IsPointInPolygon(pos, polygon))
                {
                    _draggedWholePolygon = polygon;
                    _lastDragPosition = pos;
                    _vm.ClearSelection();
                    _vm.SelectVertex(null);
                    _vm.SelectControlPoint(null);
                    ((IInputElement)sender).CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }

            // 5. Kliknięcie w puste tło - wyczyszczenie zaznaczenia
            _vm.ClearSelection();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedControlPoint != null && e.LeftButton == MouseButtonState.Pressed && _draggedPolygon != null && _draggedEdge != null)
            {
                var pos = e.GetPosition((IInputElement)sender);
                ContinuitySolver.OnControlPointMoved(_draggedPolygon, _draggedEdge, _draggedControlPoint, pos);
                _vm.UpdateConstraintVisuals();
            }
            else if (_draggedVertex != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition((IInputElement)sender);

                // Wyznaczenie pozycji wierzchołka z uwzględnieniem ograniczeń (ruch po łuku)
                Point constrainedPos = ConstraintSolver.ConstrainVertexPosition(_draggedPolygon, _draggedVertex, pos);
                Vector delta = constrainedPos - _draggedVertex.Position;
                _draggedVertex.Position = constrainedPos;

                if (_draggedPolygon != null)
                {
                    ContinuitySolver.OnVertexMoved(_draggedPolygon, _draggedVertex, delta);
                }

                // Aktualizacja plakietek ograniczeń (jeśli istnieją)
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
                foreach (var ed in _draggedWholePolygon.Edges)
                {
                    foreach (var cp in ed.ControlPoints)
                    {
                        cp.Position = new Point(cp.Position.X + delta.X, cp.Position.Y + delta.Y);
                    }
                }

                _lastDragPosition = pos;
                _vm.UpdateConstraintVisuals();
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedControlPoint != null || _draggedVertex != null || _draggedWholePolygon != null)
            {
                _draggedControlPoint = null;
                _draggedEdge = null;
                _draggedVertex = null;
                _draggedPolygon = null;
                _draggedWholePolygon = null;
                ((IInputElement)sender).ReleaseMouseCapture();
            }
        }

        private static double DistanceToEdge(Point point, Edge edge)
        {
            if (edge.EdgeType == EdgeType.Bezier && edge.ControlPoints.Count >= 2)
            {
                var pts = BezierAlgorithm.ComputePointsForwardDifferences(
                    edge.StartVertex.Position,
                    edge.ControlPoints[0].Position,
                    edge.ControlPoints[1].Position,
                    edge.EndVertex.Position,
                    20
                );
                double minD = double.MaxValue;
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double d = DistanceToSegment(point, pts[i], pts[i + 1]);
                    if (d < minD) minD = d;
                }
                return minD;
            }

            return DistanceToSegment(point, edge.StartVertex.Position, edge.EndVertex.Position);
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
            double current = GetEdgeLength(edge);
            double? result = PromptLength(current);
            if (result.HasValue)
                _vm.AddLengthConstraint(edge, result.Value);
        }

        private void PerpendicularButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedEdges.Count != 2)
            {
                MessageBox.Show("Zaznacz dokładnie dwie krawędzie, aby ustawić między nimi prostopadłość.", "Prostopadłość", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_vm.AddPerpendicularConstraint(_vm.SelectedEdges[0], _vm.SelectedEdges[1], out string? error))
            {
                MessageBox.Show(error, "Prostopadłość", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        private void ToggleBezierButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.ToggleBezierForSelectedEdge(out string? error))
            {
                MessageBox.Show(error, "Przełączanie Bézier", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ContinuityG0Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.SetContinuityForSelectedVertex(ContinuityType.G0, out string? error))
            {
                MessageBox.Show(error, "Ciągłość G0", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ContinuityG1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.SetContinuityForSelectedVertex(ContinuityType.G1, out string? error))
            {
                MessageBox.Show(error, "Ciągłość G1", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ContinuityC1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.SetContinuityForSelectedVertex(ContinuityType.C1, out string? error))
            {
                MessageBox.Show(error, "Ciągłość C1", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
