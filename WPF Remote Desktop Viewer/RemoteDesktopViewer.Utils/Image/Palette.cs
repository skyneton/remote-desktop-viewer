using System.Collections.Generic;

namespace RemoteDesktopViewer.Utils.Image
{
    public class Palette
    {
        private readonly List<short> _palette;
        private readonly Dictionary<short, int> _inversePalette;

        public int Length => _palette.Count;

        public Palette() : this(2000) { }
        public Palette(int capacity)
        {
            _palette = new List<short>(capacity);
            _inversePalette = new Dictionary<short, int>(capacity);
        }


        public int GetOrCreatePaletteIndex(short pixel)
        {
            var index = _inversePalette.GetValueOrDefault(pixel, -1);
            if (index != -1) return index;
            index = _palette.Count;
            _palette.Add(pixel);
            _inversePalette.Add(pixel, index);

            return index;
        }

        public short this[int index] => _palette[index];
    }
}