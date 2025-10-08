using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;
//accent color: #814ac8         rgb is 129, 74, 200

namespace NEC_AI_V1.UI
{
    public class DebugWindow : Window
    {
        private RichTextBox richTextBox;
        private System.Windows.Controls.Button copyButton;
        private System.Windows.Controls.Button closeButton;

        public DebugWindow(string title, string content)
        {
            Title = title;
            Width = 900;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(129, 74, 200));

            // Create main grid layout
            System.Windows.Controls.Grid mainGrid = new System.Windows.Controls.Grid();
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

            // RichTextBox for formatted content
            richTextBox = new RichTextBox
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                FontFamily = new FontFamily("Calibri"),
                FontSize = 13,
                Padding = new Thickness(15),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderThickness = new Thickness(0)
            };

            // Format the content
            FormatContent(content);

            System.Windows.Controls.Grid.SetRow(richTextBox, 0);

            // Button panel
            System.Windows.Controls.StackPanel buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
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
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(closeButton);

            mainGrid.Children.Add(richTextBox);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void FormatContent(string content)
        {
            FlowDocument doc = new FlowDocument();
            doc.PagePadding = new Thickness(0);

            string[] lines = content.Split('\n');

            foreach (string line in lines)
            {
                Paragraph para = new Paragraph();
                para.Margin = new Thickness(0, 3, 0, 3);

                // Main headers (===)
                if (line.StartsWith("==="))
                {
                    para.Inlines.Add(new Run(line)
                    {
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(129, 74, 200))  // Your accent color
                    });
                }
                // Section headers (---)
                else if (line.StartsWith("---"))
                {
                    para.Inlines.Add(new Run(line)
                    {
                        FontSize = 15,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(150, 200, 100))
                    });
                }
                // List items
                else if (line.TrimStart().StartsWith("•") || line.TrimStart().StartsWith("-") ||
                         (line.TrimStart().Length > 0 && char.IsDigit(line.TrimStart()[0]) && line.Contains(".")))
                {
                    para.Inlines.Add(new Run(line)
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                        FontFamily = new FontFamily("Calibri")
                    });
                    para.Margin = new Thickness(20, 2, 0, 2);
                }
                // Room/label lines (contain ":")
                else if (line.Contains(":") && !line.StartsWith(" "))
                {
                    string[] parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        para.Inlines.Add(new Run(parts[0] + ": ")
                        {
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 100))
                        });
                        para.Inlines.Add(new Run(parts[1])
                        {
                            Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
                        });
                    }
                    else
                    {
                        para.Inlines.Add(new Run(line));
                    }
                }
                // Normal text
                else
                {
                    para.Inlines.Add(new Run(line)
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
                    });
                }

                doc.Blocks.Add(para);
            }

            richTextBox.Document = doc;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new TextRange(
                richTextBox.Document.ContentStart,
                richTextBox.Document.ContentEnd
            );
            System.Windows.Clipboard.SetText(textRange.Text);

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
            Paragraph para = new Paragraph(new Run(text));
            richTextBox.Document.Blocks.Add(para);
            richTextBox.ScrollToEnd();
        }

        public void ClearText()
        {
            richTextBox.Document.Blocks.Clear();
        }
    }
}