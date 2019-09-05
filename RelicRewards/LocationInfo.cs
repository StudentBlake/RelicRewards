// Use Singleton design to hold values
// Very bare bones implementation
namespace RelicRewards
{
    class LocationInfo
    {
        private static int numPeople;
        private static int part1Loc;
        private static int part2Loc;
        private static int part3Loc;
        private static int part4Loc;

        public static readonly LocationInfo _instance = new LocationInfo();

        public int NumPeople
        {
            get { return numPeople; }
            set { numPeople = value; }
        }

        public int Part1Loc
        {
            get { return part1Loc; }
            set { part1Loc = value; }
        }

        public int Part2Loc
        {
            get { return part2Loc; }
            set { part2Loc = value; }
        }

        public int Part3Loc
        {
            get { return part3Loc; }
            set { part3Loc = value; }
        }

        public int Part4Loc
        {
            get { return part4Loc; }
            set { part4Loc = value; }
        }

        LocationInfo()
        {
            numPeople = 4;
            part1Loc = 638;
            part2Loc = 961;
            part3Loc = 1287;
            part4Loc = 1610;
        }
    }
}
