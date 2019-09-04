using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SharpSyntax
{
    public class HighlightWordsRule
    {
        public HighlightWordsRule(XElement rule)
        {
            Words = new List<string>();
            Options = new RuleOptions(rule);

            var wordsStr = rule.Element("Words")?.Value;
            if (wordsStr == null) return;
            var words = Regex.Split(wordsStr, "\\s+");

            foreach (var word in words)
                if (!string.IsNullOrWhiteSpace(word))
                    Words.Add(word.Trim());
        }

        public RuleOptions Options { get; }

        public List<string> Words { get; }
    }
}