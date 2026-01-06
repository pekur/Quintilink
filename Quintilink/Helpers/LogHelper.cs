using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using Quintilink.Models;

namespace Quintilink.Helpers
{
    public static class LogHelper
    {
        // Define color brushes
        private static readonly Brush GreenBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x87, 0x3a));
        private static readonly Brush DimGreenBrush = new SolidColorBrush(Color.FromRgb(0x87, 0xdb, 0x9c));
        private static readonly Brush BlueBrush = new SolidColorBrush(Color.FromRgb(0x04, 0x51, 0xb5));
        private static readonly Brush DimBlueBrush = new SolidColorBrush(Color.FromRgb(0x87, 0xbf, 0xde));
        private static readonly Brush BookmarkBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x14, 0x00));

        public static FlowDocument CreateLogDocument()
        {
            return new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                PagePadding = new Thickness(5)
            };
        }

        public static void AppendLogEntry(FlowDocument document, string timestamp, string prefix, string content, bool isAsciiLine = false)
        {
            AppendLogEntry(document, timestamp, prefix, content, isAsciiLine, isBookmarked: false);
        }

        public static void AppendLogEntry(FlowDocument document, string timestamp, string prefix, string content, bool isAsciiLine, bool isBookmarked)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0) };

            const double gutterWidth = 18;
            const double dotSize = 12;

            // Fixed-width gutter so the rest of the text never shifts.
            var gutter = new Grid
            {
                Width = gutterWidth,
                Height = dotSize,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (isBookmarked)
            {
                gutter.Children.Add(new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = BookmarkBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            var gutterInline = new InlineUIContainer(gutter)
            {
                BaselineAlignment = BaselineAlignment.Center
            };
            paragraph.Inlines.Add(gutterInline);

            // Add timestamp in gray
            paragraph.Inlines.Add(new Run($"[{timestamp}] ") { Foreground = Brushes.Gray });

            // Determine prefix color and dimmed color for macros
            Brush prefixColor = Brushes.Black;
            Brush dimmedColor = Brushes.Gray;

            if (prefix.StartsWith("[RX]"))
            {
                prefixColor = GreenBrush;
                dimmedColor = DimGreenBrush;
            }
            else if (prefix.StartsWith("[TX]"))
            {
                prefixColor = BlueBrush;
                dimmedColor = DimBlueBrush;
            }

            // Add prefix with appropriate color
            if (!string.IsNullOrEmpty(prefix))
                paragraph.Inlines.Add(new Run(prefix) { Foreground = prefixColor });

            // Handle ASCII: prefix
            if (isAsciiLine && content.StartsWith("ASCII: "))
            {
                // Add "ASCII: " prefix with same color as [RX]/[TX]
                paragraph.Inlines.Add(new Run("ASCII: ") { Foreground = prefixColor });

                // Parse and format the ASCII content with macros in dimmed color
                string asciiContent = content.Substring(7); // Remove "ASCII: " prefix
                FormatAsciiContent(paragraph, asciiContent, prefixColor, dimmedColor);
            }
            // Handle HEX: prefix (including "HEX  : " with extra spaces)
            else if (content.StartsWith("HEX") && content.Contains(": "))
            {
                int colonIndex = content.IndexOf(": ");
                string hexPrefix = content.Substring(0, colonIndex + 2); // e.g., "HEX  : "
                string hexContent = content.Substring(colonIndex + 2);

                // Add "HEX  : " prefix with same color as [RX]/[TX]
                paragraph.Inlines.Add(new Run(hexPrefix) { Foreground = prefixColor });

                // Add the hex content in the same color as prefix
                paragraph.Inlines.Add(new Run(hexContent) { Foreground = prefixColor });
            }
            else
            {
                // Regular content in black for non-RX/TX lines, or in prefix color for RX/TX
                Brush contentColor = (prefixColor == GreenBrush || prefixColor == BlueBrush) ? prefixColor : Brushes.Black;
                paragraph.Inlines.Add(new Run(content) { Foreground = contentColor });
            }

            document.Blocks.Add(paragraph);
        }

        private static void FormatAsciiContent(Paragraph paragraph, string asciiContent, Brush normalColor, Brush dimmedColor)
        {
            int i = 0;
            while (i < asciiContent.Length)
            {
                if (asciiContent[i] == '<')
                {
                    // Find the closing '>'
                    int closingIndex = asciiContent.IndexOf('>', i);
                    if (closingIndex != -1)
                    {
                        // Extract macro (including < and >)
                        string macro = asciiContent.Substring(i, closingIndex - i + 1);
                        paragraph.Inlines.Add(new Run(macro) { Foreground = dimmedColor });
                        i = closingIndex + 1;
                        continue;
                    }
                }
                else if (asciiContent[i] == '[' && i + 2 < asciiContent.Length && asciiContent[i + 3] == ']')
                {
                    // Handle hex fallback format [XX]
                    string hexFallback = asciiContent.Substring(i, 4);
                    paragraph.Inlines.Add(new Run(hexFallback) { Foreground = dimmedColor });
                    i += 4;
                    continue;
                }

                // Regular printable character in normal color
                paragraph.Inlines.Add(new Run(asciiContent[i].ToString()) { Foreground = normalColor });
                i++;
            }
        }
    }
}
