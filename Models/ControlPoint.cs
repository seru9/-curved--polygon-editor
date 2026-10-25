using System.ComponentModel;
using System.Windows;

namespace _curved__polygon_editor.Models
{
	public class ControlPoint : INotifyPropertyChanged
	{
		private Point _position;
		private bool _isSelected;

		public Point Position
		{
			get => _position;
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

		public double X => _position.X;
		public double Y => _position.Y;

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

		public Edge ParentEdge { get; }
		public int Index { get; } // 0 dla CP1 (przy StartVertex), 1 dla CP2 (przy EndVertex)

		public ControlPoint(Point position, Edge parentEdge, int index)
		{
			_position = position;
			ParentEdge = parentEdge;
			Index = index;
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
