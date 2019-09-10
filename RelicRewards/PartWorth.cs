using System.Windows.Forms;

namespace RelicRewards
{
    class PartWorth
    {
        public TextBox platinum { get; set; }
        public TextBox ducats { get; set; }

        public PartWorth(TextBox platinum, TextBox ducats)
        {
            this.platinum = platinum;
            this.ducats = ducats;
        }
    }
}
