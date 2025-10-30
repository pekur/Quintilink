using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Quintilink.Models;

namespace Quintilink.Helpers
{
    public static class LogHelper
    {
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
            var paragraph = new Paragraph { Margin = new Thickness(0) };

         // Add timestamp
   paragraph.Inlines.Add(new Run($"[{timestamp}] {prefix}") { Foreground = Brushes.Black });

      if (isAsciiLine && content.StartsWith("ASCII: "))
  {
                // Add "ASCII: " prefix
        paragraph.Inlines.Add(new Run("ASCII: ") { Foreground = Brushes.Black });

     // Parse and format the ASCII content with macros in gray
                string asciiContent = content.Substring(7); // Remove "ASCII: " prefix
         FormatAsciiContent(paragraph, asciiContent);
        }
 else
            {
       // Regular content
          paragraph.Inlines.Add(new Run(content) { Foreground = Brushes.Black });
     }

            document.Blocks.Add(paragraph);
        }

        private static void FormatAsciiContent(Paragraph paragraph, string asciiContent)
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
      paragraph.Inlines.Add(new Run(macro) { Foreground = Brushes.Gray });
         i = closingIndex + 1;
  continue;
         }
     }
          else if (asciiContent[i] == '[' && i + 2 < asciiContent.Length && asciiContent[i + 3] == ']')
   {
     // Handle hex fallback format [XX]
           string hexFallback = asciiContent.Substring(i, 4);
                 paragraph.Inlines.Add(new Run(hexFallback) { Foreground = Brushes.Gray });
  i += 4;
          continue;
      }

        // Regular printable character
     paragraph.Inlines.Add(new Run(asciiContent[i].ToString()) { Foreground = Brushes.Black });
      i++;
    }
        }
    }
}
