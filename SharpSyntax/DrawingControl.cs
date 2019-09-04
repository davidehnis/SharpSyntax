using System;
using System.Windows;
using System.Windows.Media;

namespace SharpSyntax
{
    public class DrawingControl : FrameworkElement
    {
        public DrawingControl()
        {
            Visual = new DrawingVisual();
            Visuals = new VisualCollection(this) { Visual };
        }

        protected override int VisualChildrenCount => Visuals.Count;

        private DrawingVisual Visual { get; }

        private VisualCollection Visuals { get; }

        public DrawingContext GetContext()
        {
            return Visual.RenderOpen();
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= Visuals.Count)
                throw new ArgumentOutOfRangeException();
            return Visuals[index];
        }
    }
}