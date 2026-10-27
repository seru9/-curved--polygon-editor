using System.Windows;

namespace _curved__polygon_editor
{
    /// <summary>
    /// Logika interakcji dla okna DocumentationWindow.xaml
    /// </summary>
    public partial class DocumentationWindow : Window
    {
        public DocumentationWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
