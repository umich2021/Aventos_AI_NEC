using System.Windows;
using System.Windows.Media;
// DON'T use: using System.Windows.Controls;

namespace NEC_AI_V1.UI
{
    public class DebugWindow : Window
    {
        private System.Windows.Controls.TextBox textBox;  // Specify WPF TextBox
        private System.Windows.Controls.Button copyButton;
        private System.Windows.Controls.Button closeButton;

        public DebugWindow(string title, string content)
        {
            Title = title;
            Width = 900;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

            // Create main grid layout
            System.Windows.Controls.Grid mainGrid = new System.Windows.Controls.Grid();  // Specify WPF Grid
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

            // Text box for content
            textBox = new System.Windows.Controls.TextBox
            {
                Text = content,
                TextWrapping = TextWrapping.Wrap,
                //VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                //HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Padding = new Thickness(15),
                Background = new SolidColorBrush(Color.FromRgb(56, 173, 45)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(0)
            };
            System.Windows.Controls.Grid.SetRow(textBox, 0);

            // Button panel
            System.Windows.Controls.StackPanel buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48))
            };
            System.Windows.Controls.Grid.SetRow(buttonPanel, 1);

            // Copy button
            copyButton = new System.Windows.Controls.Button
            {
                Content = "Copy to Clipboard",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            copyButton.Click += CopyButton_Click;

            // Close button
            closeButton = new System.Windows.Controls.Button
            {
                Content = "Close",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(237, 14, 14)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(closeButton);

            mainGrid.Children.Add(textBox);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(textBox.Text);
            copyButton.Content = "✓ Copied!";

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, args) =>
            {
                copyButton.Content = "Copy to Clipboard";
                timer.Stop();
            };
            timer.Start();
        }

        public void AppendText(string text)
        {
            textBox.AppendText("\n" + text);
            textBox.ScrollToEnd();
        }

        public void ClearText()
        {
            textBox.Clear();
        }
    }
}