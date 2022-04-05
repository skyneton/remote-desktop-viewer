using System.Collections.Generic;

namespace RemoteDesktopViewer.Utils.Image
{
    public class Palette
    {
        private readonly List<short> _palette = new();
        private readonly Dictionary<short, int> _inversePalette = new();

        public int GetOrCreatePaletteIndex(short pixel)
        {
            var index = _inversePalette.GetValueOrDefault(pixel, -1);
            if (index != -1) return index;
            index = _palette.Count;
            _palette.Add(pixel);
            _inversePalette.Add(pixel, index);

            return index;
        }
    }
}