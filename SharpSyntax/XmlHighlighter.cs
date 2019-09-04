using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Linq;

namespace SharpSyntax
{
    public class XmlHighlighter : IHighlighter
    {
        public XmlHighlighter(XElement root)
        {
            WordsRules = new List<HighlightWordsRule>();
            LineRules = new List<HighlightLineRule>();
            RegexRules = new List<AdvancedHighlightRule>();

            foreach (var elem in root.Elements())
            {
                switch (elem.Name.ToString())
                {
                    case "HighlightWordsRule": WordsRules.Add(new HighlightWordsRule(elem)); break;
                    case "HighlightLineRule": LineRules.Add(new HighlightLineRule(elem)); break;
                    case "AdvancedHighlightRule": RegexRules.Add(new AdvancedHighlightRule(elem)); break;
                }
            }
        }

        private List<HighlightLineRule> LineRules { get; set; }

        private List<AdvancedHighlightRule> RegexRules { get; set; }

        private List<HighlightWordsRule> WordsRules { get; set; }

        public int Highlight(FormattedText text, int previousBlockCode)
        {
            // words
            var wordsRgx = new Regex("[a-zA-Z_][a-zA-Z0-9_]*");
            foreach (Match m in wordsRgx.Matches(text.Text))
            {
                foreach (var rule in WordsRules)
                {
                    foreach (var word in rule.Words)
                    {
                        if (rule.Options.IgnoreCase)
                        {
                            if (!m.Value.Equals(word, StringComparison.InvariantCultureIgnoreCase)) continue;
                            text.SetForegroundBrush(rule.Options.Foreground, m.Index, m.Length);
                            text.SetFontWeight(rule.Options.FontWeight, m.Index, m.Length);
                            text.SetFontStyle(rule.Options.FontStyle, m.Index, m.Length);
                        }
                        else
                        {
                            if (m.Value != word) continue;
                            text.SetForegroundBrush(rule.Options.Foreground, m.Index, m.Length);
                            text.SetFontWeight(rule.Options.FontWeight, m.Index, m.Length);
                            text.SetFontStyle(rule.Options.FontStyle, m.Index, m.Length);
                        }
                    }
                }
            }

            // regex
            foreach (var rule in RegexRules)
            {
                var regexRgx = new Regex(rule.Expression);
                foreach (Match m in regexRgx.Matches(text.Text))
                {
                    text.SetForegroundBrush(rule.Options.Foreground, m.Index, m.Length);
                    text.SetFontWeight(rule.Options.FontWeight, m.Index, m.Length);
                    text.SetFontStyle(rule.Options.FontStyle, m.Index, m.Length);
                }
            }

            // lines
            foreach (var rule in LineRules)
            {
                var lineRgx = new Regex(Regex.Escape(rule.LineStart) + ".*");
                foreach (Match m in lineRgx.Matches(text.Text))
                {
                    text.SetForegroundBrush(rule.Options.Foreground, m.Index, m.Length);
                    text.SetFontWeight(rule.Options.FontWeight, m.Index, m.Length);
                    text.SetFontStyle(rule.Options.FontStyle, m.Index, m.Length);
                }
            }

            return -1;
        }
    }
}