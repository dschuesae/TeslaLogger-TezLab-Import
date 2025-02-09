using System;

namespace TezLab_Import
{
    public abstract class DataBase
    {
        public int Duration { get; set; }
        public double EndRange { get; set; }
        public DateTime EndTime { get; set; }
        public abstract bool IsChargeState { get; }
        public abstract bool IsDrivingState { get; }
        public double StartRange { get; set; }
        public DateTime StartTime { get; set; }
        public string VehicleName { get; set; }
    }
}