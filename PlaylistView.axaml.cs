using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace EchoBranch
{
    public partial class PlaylistView : UserControl
    {
        public PlaylistView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void PlaylistNameTextBox_GotFocus(object sender, GotFocusEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox.Text == "Enter playlist name")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.White;
            }
        }

        private void PlaylistNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "Enter playlist name";
                textBox.Foreground = Brushes.Gray;
            }
        }
    }
}