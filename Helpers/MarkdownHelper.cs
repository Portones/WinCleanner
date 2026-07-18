using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WinCleaner.Helpers
{
    public static class MarkdownHelper
    {
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.RegisterAttached(
                "Markdown",
                typeof(string),
                typeof(MarkdownHelper),
                new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public static string GetMarkdown(DependencyObject obj) => (string)obj.GetValue(MarkdownProperty);
        public static void SetMarkdown(DependencyObject obj, string value) => obj.SetValue(MarkdownProperty, value);

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowDocumentScrollViewer viewer)
            {
                string markdownText = e.NewValue as string ?? string.Empty;
                viewer.Document = ConvertMarkdownToFlowDocument(markdownText);
            }
        }

        private static FlowDocument ConvertMarkdownToFlowDocument(string markdown)
        {
            var doc = new FlowDocument
            {
                TextAlignment = TextAlignment.Left,
                PagePadding = new Thickness(0)
            };

            if (string.IsNullOrEmpty(markdown))
                return doc;

            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                if (trimmed.StartsWith("###"))
                {
                    var p = new Paragraph(new Run(trimmed.Substring(3).Trim()))
                    {
                        FontSize = 12.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                        Margin = new Thickness(0, 8, 0, 4)
                    };
                    doc.Blocks.Add(p);
                }
                else if (trimmed.StartsWith("##"))
                {
                    var p = new Paragraph(new Run(trimmed.Substring(2).Trim()))
                    {
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                        Margin = new Thickness(0, 10, 0, 5)
                    };
                    doc.Blocks.Add(p);
                }
                else if (trimmed.StartsWith("#"))
                {
                    var p = new Paragraph(new Run(trimmed.Substring(1).Trim()))
                    {
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                        Margin = new Thickness(0, 12, 0, 6)
                    };
                    doc.Blocks.Add(p);
                }
                else if (trimmed.StartsWith("*") || trimmed.StartsWith("-"))
                {
                    string itemText = trimmed.Substring(1).Trim();
                    var p = new Paragraph
                    {
                        Margin = new Thickness(15, 2, 0, 2),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                        FontSize = 11
                    };
                    p.Inlines.Add(new Run("•  "));
                    ParseInlineFormatting(p, itemText);
                    doc.Blocks.Add(p);
                }
                else
                {
                    var p = new Paragraph
                    {
                        Margin = new Thickness(0, 3, 0, 3),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                        FontSize = 11
                    };
                    ParseInlineFormatting(p, trimmed);
                    doc.Blocks.Add(p);
                }
            }

            return doc;
        }

        private static void ParseInlineFormatting(Paragraph p, string text)
        {
            int index = 0;
            while (index < text.Length)
            {
                int boldStart = text.IndexOf("**", index, StringComparison.Ordinal);
                if (boldStart == -1)
                {
                    p.Inlines.Add(new Run(text.Substring(index)));
                    break;
                }

                if (boldStart > index)
                {
                    p.Inlines.Add(new Run(text.Substring(index, boldStart - index)));
                }

                int boldEnd = text.IndexOf("**", boldStart + 2, StringComparison.Ordinal);
                if (boldEnd == -1)
                {
                    p.Inlines.Add(new Run(text.Substring(boldStart)));
                    break;
                }

                var boldRun = new Run(text.Substring(boldStart + 2, boldEnd - (boldStart + 2)))
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
                };
                p.Inlines.Add(boldRun);
                index = boldEnd + 2;
            }
        }
    }
}
