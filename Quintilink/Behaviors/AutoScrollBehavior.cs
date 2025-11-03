using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Quintilink.Behaviors
{
    public static class AutoScrollBehavior
    {
        public static readonly DependencyProperty AutoScrollProperty =
          DependencyProperty.RegisterAttached(
      "AutoScroll",
  typeof(bool),
       typeof(AutoScrollBehavior),
           new PropertyMetadata(false, OnAutoScrollChanged));

        public static bool GetAutoScroll(DependencyObject obj)
      {
          return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
obj.SetValue(AutoScrollProperty, value);
        }

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            bool enable = (bool)e.NewValue;

            if (d is TextBox textBox)
            {
      if (enable)
        textBox.TextChanged += TextBox_TextChanged;
            else
    textBox.TextChanged -= TextBox_TextChanged;
   }
            else if (d is RichTextBox richTextBox)
  {
         if (enable)
               richTextBox.TextChanged += RichTextBox_TextChanged;
   else
     richTextBox.TextChanged -= RichTextBox_TextChanged;
          }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
  if (sender is TextBox textBox)
            {
        textBox.ScrollToEnd();
         }
        }

        private static void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        if (sender is RichTextBox richTextBox)
            {
        richTextBox.ScrollToEnd();
            }
        }
    }
}