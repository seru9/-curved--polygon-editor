using System.ComponentModel;
using System.Windows;

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
		private bool _isSelected;

		// Flaga informuj¹ca, czy u¿ytkownik jawnie zmieni³ ci¹g³oœæ w tym wierzcho³ku
		public bool IsContinuityExplicitlySet { get; set; } = false;

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

		public ContinuityType Continuity
		{
			get => _continuity;
			set
			{
				if (_continuity != value)
				{
					_continuity = value;
					OnPropertyChanged(nameof(Continuity));
					OnPropertyChanged(nameof(ContinuityText));
				}
			}
		}

		public string ContinuityText => _continuity.ToString();

		public double X => _position.X; //helpful for binding
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