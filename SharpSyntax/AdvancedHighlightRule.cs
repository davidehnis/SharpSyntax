using System.Xml.Linq;

namespace SharpSyntax
{
    public class AdvancedHighlightRule
    {
        public AdvancedHighlightRule(XElement rule)
        {
            Expression = rule.Element("Expression")?.Value.Trim();
            Options = new RuleOptions(rule);
        }

        public string Expression { get; }

        public RuleOptions Options { get; }
    }
}