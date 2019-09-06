using System.Windows.Forms;

namespace RelicRewards
{
    class InfoContainer
    {
        public TextBox platinum { get; set; }
        public TextBox ducats { get; set; }

        public InfoContainer(TextBox platinum, TextBox ducats)
        {
            this.platinum = platinum;
            this.ducats = ducats;
        }
    }
}
