namespace TezLab_Import
{
    public class DrivingData : DataBase
    {
        public double DistanceTraveled { get; set; }
        public double EndOdometer { get; set; }
        public string From { get; set; }
        public string FromCoord { get; set; }
        public double FromLatitude => double.Parse(FromCoord.Split(',')[0], Program.ciEnUS);
        public double FromLongitude => double.Parse(FromCoord.Split(',')[1], Program.ciEnUS);
        public override bool IsChargeState => false;
        public override bool IsDrivingState => true;
        public double RangeUsed { get; set; }
        public double StartOdometer { get; set; }
        public int Temperature { get; set; }
        public string To { get; set; }
        public string ToCoord { get; set; }
        public double ToLatitude => double.Parse(ToCoord.Split(',')[0], Program.ciEnUS);
        public double ToLongitude => double.Parse(ToCoord.Split(',')[1], Program.ciEnUS);
    }
}