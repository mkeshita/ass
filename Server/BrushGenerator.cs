using System.Drawing;
using Devcorner.NIdenticon.BrushGenerators;

namespace norsu.ass.Server
{
    class BrushGenerator : IBrushGenerator
    {
        public Brush GetBrush(uint seed)
        {
            return Brushes.Red;
        }
    }
}
