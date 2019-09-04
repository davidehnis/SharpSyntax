using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace SharpSyntax
{
    /// <summary>A set of options liked to each rule.</summary>
    public class RuleOptions
    {
        public RuleOptions(XElement rule)
        {
            var ignoreCaseStr = rule.Element("IgnoreCase")?.Value.Trim();
            var foregroundStr = rule.Element("Foreground")?.Value.Trim();
            var fontWeightStr = rule.Element("FontWeight")?.Value.Trim();
            var fontStyleStr = rule.Element("FontStyle")?.Value.Trim();

            if (ignoreCaseStr != null) IgnoreCase = bool.Parse(ignoreCaseStr);
            Foreground = (Brush)new BrushConverter().ConvertFrom(foregroundStr);
            if (fontWeightStr != null)
            {
                FontWeight = (FontWeight)new FontWeightConverter().ConvertFrom(value: fontWeightStr);
            }

            FontStyle = (FontStyle)new FontStyleConverter().ConvertFrom(fontStyleStr);
        }

        public FontStyle FontStyle { get; private set; }

        public FontWeight FontWeight { get; private set; }

        public Brush Foreground { get; private set; }

        public bool IgnoreCase { get; private set; }
    }
}