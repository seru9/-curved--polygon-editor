using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using _curved__polygon_editor.Models;
namespace _curved__polygon_editor.ViewModels
{
	public class MainViewModel
	{
		public ObservableCollection<Polygon> Polygons { get; set; } = new ObservableCollection<Polygon>();
		public MainViewModel()
		{
			InitializeDemoScene();
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

			// Krawędź z Bézierem (Wymaganie nr 3 i 8)
			//var bezierEdge = new Edge(v3, v1, EdgeType.Bezier);
			//bezierEdge.ControlPoints = new Point[] { new Point(80, 220), new Point(20, 120) };
			//p1.Edges.Add(bezierEdge);

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
