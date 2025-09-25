using System.Collections.Generic;
using System.Linq;

namespace HayChonGiaDung.Wpf
{
    public class QuickQuestion
    {
        public string Text { get; set; } = string.Empty;

        public List<string> Options { get; set; } = new() { string.Empty, string.Empty, string.Empty, string.Empty };

        public int CorrectIndex { get; set; }
            = 0;

        public string Explanation { get; set; } = string.Empty;

        public QuickQuestion Clone()
        {
            return new QuickQuestion
            {
                Text = Text,
                Options = Options.ToList(),
                CorrectIndex = CorrectIndex,
                Explanation = Explanation
            };
        }
    }
}
