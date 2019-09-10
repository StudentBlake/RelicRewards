// Use Singleton design to hold values
// Very bare bones implementation
namespace RelicRewards
{
    class PartLocation
    {
        private static int numPeople;
        private static int partLoc1;
        private static int partLoc2;
        private static int partLoc3;
        private static int partLoc4;

        public static readonly PartLocation _instance = new PartLocation();

        public int NumPeople
        {
            get { return numPeople; }
        }

        public int PartLoc1
        {
            get { return partLoc1; }
        }

        public int PartLoc2
        {
            get { return partLoc2; }
        }

        public int PartLoc3
        {
            get { return partLoc3; }
        }

        public int PartLoc4
        {
            get { return partLoc4; }
        }

        public void SetPeople2()
        {
            numPeople = 2;
            // Needs updated numbers
            partLoc1 = 725;
            partLoc2 = 1300;
        }

        public void SetPeople3()
        {
            numPeople = 3;
            // Needs updated numbers
            partLoc1 = 435;
            partLoc2 = 1011;
            partLoc3 = 1590;
        }

        public void SetPeople4()
        {
            numPeople = 4;

            partLoc1 = 638;
            partLoc2 = 961;
            partLoc3 = 1287;
            partLoc4 = 1610;
        }

        PartLocation()
        {
            SetPeople4();
        }
    }
}
