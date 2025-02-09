using CsvHelper;
using CsvHelper.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TezLab_Import;

public class Program
{
    internal static readonly CultureInfo _ciEnUS = new CultureInfo("en-US");

    //Change CarId here (default=1)
    private static readonly int CarId = 1;

    //Change DBConnectionstring here
    //private static readonly string DBConnectionstring = "Server=dockerraspi;Database=teslalogger;Uid=root;Password=teslalogger;CharSet=utf8mb4;";
    private static readonly string DBConnectionstring = "Server=raspberry;Database=teslalogger;Uid=root;Password=teslalogger;CharSet=utf8mb4;";

    //Default ratio for my ModelX, but with the first Entry, it will update it anyway.
    private static double CurrentRangeToBatteryLevel = 4.05;

    public static void Main(string[] args)
    {
        DeleteImportData();
        var dtMax = GetFirstTeslaloggerData();
        var items = new List<DataBase>();
        items.AddRange(GetDrivingData());
        items.AddRange(GetChargingData());
        items = items.Where(it => it.StartTime < dtMax).OrderBy(it => it.StartTime).ToList();
        foreach (var item in items)
        {
            if (item is DrivingData drivingData)
            {
                Console.WriteLine($"Import Driving Data from {drivingData.StartTime}");
                var startPosId = InsertPos(drivingData.StartTime,
                    drivingData.FromLatitude,
                    drivingData.FromLongitude,
                    drivingData.StartOdometer,
                    drivingData.StartRange,
                    null,
                    drivingData.Temperature);
                var endPosId = InsertPos(drivingData.EndTime,
                    drivingData.ToLatitude,
                    drivingData.ToLongitude,
                    drivingData.EndOdometer,
                    drivingData.EndRange,
                    null,
                    drivingData.Temperature);
                InsertDriveState(drivingData.StartTime, startPosId, drivingData.EndTime, endPosId, drivingData.Temperature);
            }
            else if (item is ChargingData chargingData)
            {
                Console.WriteLine($"Import Charging Data from {chargingData.StartTime}");
                int? temp = int.TryParse(chargingData.Temperature, out var t) ? t : null;
                var startChargingId = InsertCharging(chargingData.StartTime,
                    0,
                    chargingData.StartSOC,
                    chargingData.StartRange,
                    temp);
                InsertPos(chargingData.StartTime,
                            chargingData.Latitude,
                            chargingData.Longitude,
                            chargingData.Odometer,
                            chargingData.StartRange,
                            chargingData.StartSOC,
                            temp);
                var socDiff = chargingData.EndSOC - chargingData.StartSOC;
                //Füge einen Chargingpunkt hinzu pro SOC
                if (socDiff > 1)
                {
                    var diffCt = Math.Max(socDiff / 5, 2);
                    var rangeDiff = (chargingData.EndRange - chargingData.StartRange) / socDiff;
                    var timeDiff = (chargingData.EndTime - chargingData.StartTime).TotalSeconds / socDiff;
                    var energyDiff = chargingData.ChargeEnergyAdded / socDiff;
                    for (var i = diffCt; i < socDiff; i += diffCt)
                    {
                        InsertCharging(chargingData.StartTime.AddSeconds(timeDiff * i),
                            energyDiff * i,
                            chargingData.StartSOC + i,
                            chargingData.StartRange + rangeDiff * i,
                            temp);
                    }
                }
                var endPosId = InsertPos(chargingData.EndTime,
                    chargingData.Latitude,
                    chargingData.Longitude,
                    chargingData.Odometer,
                    chargingData.EndRange,
                    chargingData.EndSOC,
                    temp);
                var endChargingId = InsertCharging(chargingData.EndTime,
                    chargingData.ChargeEnergyAdded,
                    chargingData.EndSOC,
                    chargingData.EndRange,
                    temp);
                InsertChargingState(chargingData.StartTime,
                    startChargingId,
                    chargingData.EndTime,
                    endChargingId,
                    endPosId,
                    chargingData.ChargeEnergyAdded,
                    chargingData.FastCharging,
                    chargingData.MaxChargerPower,
                    chargingData.Cost);
            }
        }
        Console.Write("Import completed!");
        Console.ReadKey();
    }

    private static void DeleteImportData()
    {
        Console.WriteLine("Deleting Import data");
        var tables = new string[] { "charging", "chargingstate", "drivestate", "pos" };
        foreach (var table in tables)
        {
            try
            {
                using (var con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    var cmd1 = new MySqlCommand($"alter table {table} ADD column IF NOT EXISTS import TINYINT(1) NULL", con);
                    cmd1.CommandTimeout = 300;
                    cmd1.ExecuteNonQuery();
                    var cmd2 = new MySqlCommand($"delete from {table} where import=1", con);
                    cmd2.CommandTimeout = 300;
                    cmd2.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    private static List<ChargingData> GetChargingData()
    {
        Console.WriteLine("Reading Charging data");
        if (!Directory.Exists("ChargingCsvFiles"))
        {
            Console.WriteLine("Directory ChargingCsvFiles not found");
            return new List<ChargingData>();
        }
        var ret = new List<ChargingData>();
        foreach (var filePath in Directory.GetFiles("ChargingCsvFiles", "*.csv"))
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = GetHeader,
            };
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                ret.AddRange(csv.GetRecords<ChargingData>());
            }
        }
        return ret;
    }

    private static List<DrivingData> GetDrivingData()
    {
        Console.WriteLine("Reading Driving data");
        if (!Directory.Exists("DrivingCsvFiles"))
        {
            Console.WriteLine("Directory DrivingCsvFiles not found");
            return new List<DrivingData>();
        }
        var ret = new List<DrivingData>();
        foreach (var filePath in Directory.GetFiles("DrivingCsvFiles", "*.csv"))
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = GetHeader,
            };
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                ret.AddRange(csv.GetRecords<DrivingData>());
            }
        }
        return ret;
    }

    private static DateTime GetFirstTeslaloggerData()
    {
        DateTime dtMin = DateTime.Now;

        using (var con = new MySqlConnection(DBConnectionstring))
        {
            con.Open();
            var cmd = new MySqlCommand("SELECT MIN(StartDate) FROM drivestate where import is null", con);
            var dtDrivestate = cmd.ExecuteScalar();
            if (dtDrivestate != DBNull.Value)
                dtMin = (DateTime)dtDrivestate;

            cmd = new MySqlCommand("SELECT MIN(StartDate) FROM chargingstate where import is null", con);
            var dtChargestate = cmd.ExecuteScalar();
            if (dtChargestate != DBNull.Value && (DateTime)dtChargestate < dtMin)
                dtMin = (DateTime)dtChargestate;
        }

        return dtMin;
    }

    private static string GetHeader(PrepareHeaderForMatchArgs args)
    {
        var header = args.Header;
        var i = header.IndexOf('(');
        if (i >= 0)
            header = header.Substring(0, i);
        return header.Replace(" ", "").Trim();
    }

    private static int InsertCharging(DateTime Date, double charge_energy_added, int battery_level, double range, double? outside_temp)
    {
        range = range / 1.248661055853099;

        using (var con = new MySqlConnection(DBConnectionstring))
        {
            con.Open();
            var cmd = new MySqlCommand("insert charging (import, Datum, battery_level, charge_energy_added, ideal_battery_range_km, outside_temp, charger_power, carid) values (1, @Datum, @battery_level, @charge_energy_added, @ideal_battery_range_km, @outside_temp, 0, @carid)", con);
            cmd.Parameters.AddWithValue("@carid", CarId);
            cmd.Parameters.AddWithValue("@Datum", Date.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@battery_level", battery_level);
            cmd.Parameters.AddWithValue("@charge_energy_added", charge_energy_added.ToString(_ciEnUS));
            cmd.Parameters.AddWithValue("@ideal_battery_range_km", range.ToString(_ciEnUS));
            if (outside_temp == null)
                cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@outside_temp", ((double)outside_temp).ToString(_ciEnUS));

            cmd.ExecuteNonQuery();

            cmd = new MySqlCommand("SELECT LAST_INSERT_ID();", con);
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    private static void InsertChargingState(DateTime startDate, int startChargingId, DateTime endDate, int endChargingId, int posId, double charge_energy_added, bool fast_charger_present, int maxPower, double cost)
    {
        using (var con = new MySqlConnection(DBConnectionstring))
        {
            con.Open();
            var cmd = new MySqlCommand("insert chargingstate (import, StartDate, EndDate, Pos, StartChargingID, EndChargingID, fast_charger_present, max_charger_power, carid, charge_energy_added, cost_total ) values (1, @StartDate, @EndDate, @Pos, @StartChargingID, @EndChargingID, @fast_charger_present, @max_charger_power, @carid, @charge_energy_added, @cost_total)", con);
            cmd.Parameters.AddWithValue("@carid", CarId);
            cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@Pos", posId);
            cmd.Parameters.AddWithValue("@StartChargingID", startChargingId);
            cmd.Parameters.AddWithValue("@EndChargingID", endChargingId);
            cmd.Parameters.AddWithValue("@charge_energy_added", charge_energy_added.ToString(_ciEnUS));
            cmd.Parameters.AddWithValue("@fast_charger_present", fast_charger_present);
            cmd.Parameters.AddWithValue("@max_charger_power", maxPower.ToString(_ciEnUS));
            cmd.Parameters.AddWithValue("@cost_total", cost.ToString(_ciEnUS));
            cmd.ExecuteNonQuery();
        }
    }

    private static void InsertDriveState(DateTime startDate, int startPosId, DateTime endDate, int endPosId, int? outside_temp)
    {
        using (var con = new MySqlConnection(DBConnectionstring))
        {
            con.Open();
            var cmd = new MySqlCommand("insert drivestate (import, StartDate, StartPos, EndDate, EndPos, outside_temp_avg, carid) values (1, @StartDate, @StartPosId, @EndDate, @EndPosId, @outside_temp, @carid)", con);
            cmd.Parameters.AddWithValue("@carid", CarId);
            cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@StartPosId", startPosId);
            cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@EndPosId", endPosId);
            if (outside_temp == null)
                cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@outside_temp", outside_temp.Value.ToString(_ciEnUS));
            cmd.ExecuteNonQuery();
        }
    }

    private static int InsertPos(DateTime date, double latitude, double longitude, double odometer, double range, int? battery_level, double? outside_temp)
    {
        range = range / 1.248661055853099;
        using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
        {
            con.Open();
            MySqlCommand cmd = new MySqlCommand("insert pos (import, Datum, lat, lng, odometer, ideal_battery_range_km, outside_temp, battery_level, carid) values (1, @Datum, @lat, @lng, @odometer, @ideal_battery_range_km, @outside_temp, @battery_level, @carid )", con);
            cmd.Parameters.AddWithValue("@carid", CarId);
            cmd.Parameters.AddWithValue("@Datum", date.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@lat", latitude.ToString(_ciEnUS));
            cmd.Parameters.AddWithValue("@lng", longitude.ToString(_ciEnUS));
            cmd.Parameters.AddWithValue("@odometer", odometer.ToString(_ciEnUS));
            cmd.Parameters.AddWithValue("@ideal_battery_range_km", range.ToString(_ciEnUS));

            if (outside_temp == null)
                cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@outside_temp", ((double)outside_temp).ToString(_ciEnUS));

            if (battery_level == null)
                cmd.Parameters.AddWithValue("@battery_level", Convert.ToInt32(range / CurrentRangeToBatteryLevel));
            else
            {
                if (battery_level.Value > 0)
                    CurrentRangeToBatteryLevel = range / battery_level.Value;
                cmd.Parameters.AddWithValue("@battery_level", battery_level.ToString());
            }

            cmd.ExecuteNonQuery();

            cmd = new MySqlCommand("SELECT LAST_INSERT_ID();", con);
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    private static int InsertState(DateTime startDate, int startPos, DateTime endDate, int endPos, string state)
    {
        using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
        {
            con.Open();
            MySqlCommand cmd = new MySqlCommand("insert state (import, StartDate, EndDate, state, StartPos, EndPos, carid) values (1, @StartDate, @EndDate, @state, @StartPos, @EndPos, @carid)", con);
            cmd.Parameters.AddWithValue("@carid", CarId);
            cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@state", state);
            cmd.Parameters.AddWithValue("@StartPos", startPos);
            cmd.Parameters.AddWithValue("@EndPos", endPos);
            cmd.ExecuteNonQuery();

            cmd = new MySqlCommand("SELECT LAST_INSERT_ID();", con);
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}