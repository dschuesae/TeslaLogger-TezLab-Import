namespace TezLab_Import
{
    public class ChargingData : DataBase
    {
        public double ChargeEnergyAdded { get; set; }
        public string ChargerName { get; set; }
        public string ConnectorType { get; set; }
        public string Coordinate { get; set; }
        public double Cost { get; set; }
        public int EndSOC { get; set; }
        public string EnergyDrawn { get; set; }

        public bool FastCharging { get; set; }
        public override bool IsChargeState => true;
        public override bool IsDrivingState => false;
        public double Latitude => double.Parse(Coordinate.Split(',')[0], Program.ciEnUS);
        public string Location { get; set; }
        public double Longitude => double.Parse(Coordinate.Split(',')[1], Program.ciEnUS);
        public int MaxChargerPower { get; set; }
        public double Odometer { get; set; }
        public double RangeAdded { get; set; }
        public int StartSOC { get; set; }
        public string Temperature { get; set; }
    }
}