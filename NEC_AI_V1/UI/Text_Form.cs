using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NEC_AI_V1.UI
{
    public class text_form : Window
    {
        private System.Windows.Controls.TextBox rulesTextBox;
        public string form_input { get; private set; }

        public text_form()
        {
            Title = "Custom Electrical Code Rules";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

            var mainGrid = new System.Windows.Controls.Grid();
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

            // Header label
            var headerLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Enter your custom electrical code requirements:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(15, 15, 15, 10)
            };
            System.Windows.Controls.Grid.SetRow(headerLabel, 0);

            // Text box for rules
            rulesTextBox = new System.Windows.Controls.TextBox
            {
                Text = "Input custom codes and preferences, i.e. our local codes require gfci protection in every room",
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Calibri"),
                FontSize = 12,
                Padding = new Thickness(10),
                Margin = new Thickness(15, 5, 15, 10),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1)
            };
            System.Windows.Controls.Grid.SetRow(rulesTextBox, 1);

            // Button panel
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15, 10, 15, 15)
            };
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

            // OK button
            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 100,
                Height = 32,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            okButton.Click += OkButton_Click;

            // Cancel button
            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 32,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);

            mainGrid.Children.Add(headerLabel);
            mainGrid.Children.Add(rulesTextBox);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            form_input = rulesTextBox.Text;
            DialogResult = true;
            Close();
        }
    }
}