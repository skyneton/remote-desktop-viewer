using System.Collections.Generic;

namespace RemoteDesktopViewer.Utils.Image
{
    public class Palette
    {
        private readonly List<short> _palette;
        private readonly Dictionary<short, int> _inversePalette;

        public int Length => _palette.Count;

        public Palette() : this(6000) { }
        public Palette(int capacity)
        {
            _palette = new List<short>(capacity);
            _inversePalette = new Dictionary<short, int>(capacity);
        }


        public int GetOrCreatePaletteIndex(short pixel)
        {
            //var index = _inversePalette.GetValueOrDefault(pixel, -1);
            if(_inversePalette.TryGetValue(pixel, out var value))
                return value;
            
            value = _palette.Count;
            _palette.Add(pixel);
            _inversePalette.Add(pixel, value);

            return value;
        }

        public short this[int index] => _palette[index];
    }
}