using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
namespace _curved__polygon_editor.Models
{
	public enum ContinuityType
	{
		G0,
		G1,
		C1
	};
	public class Vertex : INotifyPropertyChanged
	{
		private Point _position;
		private ContinuityType _continuity;
		
		public Point Position
		{
			get { return _position; }
			set
			{
				if (_position != value)
				{
					_position = value;
					OnPropertyChanged(nameof(Position));
					OnPropertyChanged(nameof(X));
					OnPropertyChanged(nameof(Y));
				}
			}
		}
		public ContinuityType Continuity
		{
			get => _continuity;
			set
			{
				if (_continuity != value)
				{
					_continuity = value;
					OnPropertyChanged(nameof(Continuity));
				}
			}
		}
		public double X => _position.X; // helpful for binding
		public double Y => _position.Y;
		public Vertex(Point position, ContinuityType continuity = ContinuityType.G0)
		{
			Position = position;
			Continuity = continuity;
		}
		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
