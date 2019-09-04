using System;

namespace SharpSyntax
{
    public class TextUtilities
    {
        protected TextUtilities()
        {
        }

        public static int GetFirstCharIndexFromLineIndex(string text, int lineIndex)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (lineIndex <= 0)
                return 0;

            var currentLineIndex = 0;
            for (var i = 0; i < text.Length - 1; i++)
            {
                if (text[i] != '\n') continue;
                currentLineIndex += 1;
                if (currentLineIndex == lineIndex)
                    return Math.Min(i + 1, text.Length - 1);
            }

            return Math.Max(text.Length - 1, 0);
        }

        public static int GetLastCharIndexFromLineIndex(string text, int lineIndex)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (lineIndex < 0)
                return 0;

            var currentLineIndex = 0;
            for (var i = 0; i < text.Length - 1; i++)
            {
                if (text[i] != '\n') continue;
                if (currentLineIndex == lineIndex)
                    return i;
                currentLineIndex += 1;
            }

            return Math.Max(text.Length - 1, 0);
        }

        public static int GetLineCount(String text)
        {
            var lineCount = 1;
            foreach (var t in text)
            {
                if (t == '\n')
                    lineCount += 1;
            }
            return lineCount;
        }
    }
}