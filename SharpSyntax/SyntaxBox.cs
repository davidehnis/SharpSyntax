using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SharpSyntax
{
    public class SyntaxBox : TextBox
    {
        public static readonly DependencyProperty IsLineNumbersMarginVisibleProperty = DependencyProperty.Register(
            "IsLineNumbersMarginVisible", typeof(bool), typeof(SyntaxBox), new PropertyMetadata(true));

        public SyntaxBox()
        {
            MaxLineCountInBlock = 100;
            LineHeight = FontSize * 1.2;
            TotalLineCount = 1;
            Blocks = new List<InnerTextBlock>();

            Loaded += (s, e) =>
            {
                RenderCanvas = (DrawingControl)Template.FindName("PART_RenderCanvas", this);
                LineNumbersCanvas = (DrawingControl)Template.FindName("PART_LineNumbersCanvas", this);
                ScrollerView = (ScrollViewer)Template.FindName("PART_ContentHost", this);

                LineNumbersCanvas.Width = GetFormattedTextWidth(string.Format("{0:0000}", TotalLineCount)) + 5;

                ScrollerView.ScrollChanged += OnScrollChanged;

                InvalidateBlocks(0);
                InvalidateVisual();
            };

            SizeChanged += (s, e) =>
            {
                if (!e.HeightChanged) return;
                UpdateBlocks();
                InvalidateVisual();
            };

            TextChanged += (s, e) =>
            {
                UpdateTotalLineCount();
                InvalidateBlocks(e.Changes.First().Offset);
                InvalidateVisual();
            };
        }

        public IHighlighter CurrentHighlighter { get; set; }

        public bool IsLineNumbersMarginVisible
        {
            get => (bool)GetValue(IsLineNumbersMarginVisibleProperty);
            set => SetValue(IsLineNumbersMarginVisibleProperty, value);
        }

        public double LineHeight
        {
            get => AdjustedLineHeight;
            set
            {
                if (value == AdjustedLineHeight) return;
                AdjustedLineHeight = value;
                BlockHeight = MaxLineCountInBlock * value;
                TextBlock.SetLineStackingStrategy(this, LineStackingStrategy.BlockLineHeight);
                TextBlock.SetLineHeight(this, AdjustedLineHeight);
            }
        }

        public int MaxLineCountInBlock
        {
            get => MaximumLineCountInBlock;
            set
            {
                MaximumLineCountInBlock = value > 0 ? value : 0;
                BlockHeight = value * LineHeight;
            }
        }

        private double AdjustedLineHeight { get; set; }

        private double BlockHeight { get; set; }

        private List<InnerTextBlock> Blocks { get; set; }

        private DrawingControl LineNumbersCanvas { get; set; }

        private int MaximumLineCountInBlock { get; set; }

        private DrawingControl RenderCanvas { get; set; }

        private ScrollViewer ScrollerView { get; set; }

        private int TotalLineCount { get; set; }

        public int GetIndexOfFirstVisibleLine()
        {
            var line = (int)(VerticalOffset / AdjustedLineHeight);
            return line > TotalLineCount ? TotalLineCount : line;
        }

        public int GetIndexOfLastVisibleLine()
        {
            var height = VerticalOffset + ViewportHeight;
            var line = (int)(height / AdjustedLineHeight);
            return line > TotalLineCount - 1 ? TotalLineCount - 1 : line;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            DrawBlocks();
            base.OnRender(drawingContext);
        }

        private void DrawBlocks()
        {
            if (!IsLoaded || RenderCanvas == null || LineNumbersCanvas == null)
                return;

            var dc = RenderCanvas.GetContext();
            var dc2 = LineNumbersCanvas.GetContext();
            foreach (var block in Blocks)
            {
                var blockPos = block.Position;
                var top = blockPos.Y - VerticalOffset;
                var bottom = top + BlockHeight;
                if ((top >= ActualHeight) || (bottom <= 0)) continue;
                try
                {
                    dc.DrawText(block.FormattedText, new Point(2 - HorizontalOffset, block.Position.Y - VerticalOffset));
                    if (IsLineNumbersMarginVisible)
                    {
                        LineNumbersCanvas.Width = GetFormattedTextWidth(string.Format("{0:0000}", TotalLineCount)) + 5;
                        dc2.DrawText(block.LineNumbers, new Point(LineNumbersCanvas.ActualWidth, 1 + block.Position.Y - VerticalOffset));
                    }
                }
                catch
                {
                    // ignored
                }
            }
            dc.Close();
            dc2.Close();
        }

        private void FormatBlock(InnerTextBlock currentBlock, InnerTextBlock previousBlock)
        {
            currentBlock.FormattedText = GetFormattedText(currentBlock.RawText);
            if (CurrentHighlighter != null)
            {
                ThreadPool.QueueUserWorkItem(p =>
                {
                    var previousCode = previousBlock?.Code
                                       ?? -1;
                    currentBlock.Code = CurrentHighlighter.Highlight(currentBlock.FormattedText, previousCode);
                });
            }
        }

        private FormattedText GetFormattedLineNumbers(int firstIndex, int lastIndex)
        {
            var text = "";
            var builder = new StringBuilder();
            for (var i = firstIndex + 1; i <= lastIndex + 1; i++)
            {
                builder.AppendLine($"{text}{i}");
            }

            text = text.Trim();

            var formattedText = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), 16,
                new SolidColorBrush(Color.FromRgb(0x21, 0xA1, 0xD8)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            {
                Trimming = TextTrimming.None,
                LineHeight = AdjustedLineHeight,
                TextAlignment = TextAlignment.Right
            };

            return formattedText;
        }

        private FormattedText GetFormattedText(string text)
        {
            var formattedText = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), 16,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            {
                Trimming = TextTrimming.None,
                LineHeight = AdjustedLineHeight
            };

            return formattedText;
        }

        private double GetFormattedTextWidth(string text)
        {
            var formattedText = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), 16,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            {
                Trimming = TextTrimming.None,
                LineHeight = AdjustedLineHeight
            };

            formattedText.Trimming = TextTrimming.None;
            formattedText.LineHeight = AdjustedLineHeight;

            return formattedText.Width;
        }

        private void InvalidateBlocks(int changeOffset)
        {
            InnerTextBlock blockChanged = null;
            foreach (var block in Blocks)
            {
                if (block.CharStartIndex > changeOffset || changeOffset > block.CharEndIndex + 1) continue;
                blockChanged = block;
                break;
            }

            if (blockChanged == null && changeOffset > 0)
                blockChanged = Blocks.Last();

            var fvline = blockChanged?.LineStartIndex ?? 0;
            var lvline = GetIndexOfLastVisibleLine();
            var fvchar = blockChanged?.CharStartIndex ?? 0;
            var lvchar = TextUtilities.GetLastCharIndexFromLineIndex(Text, lvline);

            if (blockChanged != null)
                Blocks.RemoveRange(Blocks.IndexOf(blockChanged), Blocks.Count - Blocks.IndexOf(blockChanged));

            var localLineCount = 1;
            var charStart = fvchar;
            var lineStart = fvline;
            for (var i = fvchar; i < Text.Length; i++)
            {
                if (Text[i] == '\n')
                {
                    localLineCount += 1;
                }
                if (i == Text.Length - 1)
                {
                    var blockText = Text.Substring(charStart);
                    var block = new InnerTextBlock(
                        charStart,
                        i, lineStart,
                        lineStart + TextUtilities.GetLineCount(blockText) - 1,
                        LineHeight);
                    block.RawText = block.GetSubString(Text);
                    block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);
                    block.IsLast = true;

                    foreach (var b in Blocks)
                        if (b.LineStartIndex == block.LineStartIndex)
                        {
                            var exception = new Exception();
                            throw exception;
                        }

                    Blocks.Add(block);
                    FormatBlock(block, Blocks.Count > 1 ? Blocks[Blocks.Count - 2] : null);
                    break;
                }
                if (localLineCount > MaximumLineCountInBlock)
                {
                    var block = new InnerTextBlock(
                        charStart,
                        i,
                        lineStart,
                        lineStart + MaximumLineCountInBlock - 1,
                        LineHeight);
                    block.RawText = block.GetSubString(Text);
                    block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);

                    foreach (var b in Blocks)
                        if (b.LineStartIndex == block.LineStartIndex)
                        {
                            var exception1 = new Exception();
                            throw exception1;
                        }

                    Blocks.Add(block);
                    FormatBlock(block, Blocks.Count > 1 ? Blocks[Blocks.Count - 2] : null);

                    charStart = i + 1;
                    lineStart += MaximumLineCountInBlock;
                    localLineCount = 1;

                    if (i > lvchar)
                        break;
                }
            }
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0.00)
                UpdateBlocks();
            InvalidateVisual();
        }

        private void UpdateBlocks()
        {
            if (Blocks.Count == 0)
                return;

            while (!Blocks.Last().IsLast && Blocks.Last().Position.Y + BlockHeight - VerticalOffset < ActualHeight)
            {
                var firstLineIndex = Blocks.Last().LineEndIndex + 1;
                var lastLineIndex = firstLineIndex + MaximumLineCountInBlock - 1;
                lastLineIndex = lastLineIndex <= TotalLineCount - 1 ? lastLineIndex : TotalLineCount - 1;

                var fisrCharIndex = Blocks.Last().CharEndIndex + 1;
                var lastCharIndex = TextUtilities.GetLastCharIndexFromLineIndex(Text, lastLineIndex); // to be optimized (forward search)

                if (lastCharIndex <= fisrCharIndex)
                {
                    Blocks.Last().IsLast = true;
                    return;
                }

                var block = new InnerTextBlock(
                    fisrCharIndex,
                    lastCharIndex,
                    Blocks.Last().LineEndIndex + 1,
                    lastLineIndex,
                    LineHeight);
                block.RawText = block.GetSubString(Text);
                block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);
                Blocks.Add(block);
                FormatBlock(block, Blocks.Count > 1 ? Blocks[Blocks.Count - 2] : null);
            }
        }

        private void UpdateTotalLineCount()
        {
            TotalLineCount = TextUtilities.GetLineCount(Text);
        }

        private class InnerTextBlock
        {
            public InnerTextBlock(int charStart, int charEnd, int lineStart, int lineEnd, double lineHeight)
            {
                CharStartIndex = charStart;
                CharEndIndex = charEnd;
                LineStartIndex = lineStart;
                LineEndIndex = lineEnd;
                LineHeight = lineHeight;
                IsLast = false;
            }

            public int CharEndIndex { get; }

            public int CharStartIndex { get; }

            public int Code { get; set; }

            public FormattedText FormattedText { get; set; }

            public bool IsLast { get; set; }

            public int LineEndIndex { get; }

            public FormattedText LineNumbers { get; set; }

            public int LineStartIndex { get; }

            public Point Position => new Point(0, LineStartIndex * LineHeight);

            public string RawText { get; set; }

            private double LineHeight { get; set; }

            public string GetSubString(string text)
            {
                return text.Substring(CharStartIndex, CharEndIndex - CharStartIndex + 1);
            }

            public override string ToString()
            {
                return $"L:{LineStartIndex}/{LineEndIndex} C:{CharStartIndex}/{CharEndIndex} {FormattedText.Text}";
            }
        }
    }
}