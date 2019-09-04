using System.Xml.Linq;

namespace SharpSyntax
{
    public class HighlightLineRule : Rule
    {
        public HighlightLineRule(XElement rule)
        {
            LineStart = rule.Element("LineStart")?.Value.Trim();
            Options = new RuleOptions(rule);
        }

        public string LineStart { get; }
    }
}