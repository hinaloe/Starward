using Dapper;
using Microsoft.Data.Sqlite;
using Starward.Core.GameRecord.Genshin.TravelersDiary;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;
using System.ComponentModel;
using System.Data;
using System.Text.Json;

namespace Starward.Dashboard.Server.Services;

internal class DatabaseService
{

    private readonly ILogger<DatabaseService> _logger;


    private readonly string _connectionString;



    static DatabaseService()
    {
        SqlMapper.AddTypeHandler(new DapperSqlMapper.DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.TravelersDiaryPrimogemsMonthGroupStatsListHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.TrailblazeCalendarMonthDataGroupByListHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.StringListHandler());
    }



    public DatabaseService(string connectionString, IServiceProvider serviceProvider)
    {
        _connectionString = connectionString;
        _logger = serviceProvider.GetRequiredService<ILogger<DatabaseService>>();
    }




    public SqliteConnection CreateConnection()
    {
        var con = new SqliteConnection(_connectionString);
        con.Open();
        return con;
    }





    #region Setting & KVT




    private T? GetSetting<T>(string key, T? defaultValue = default)
    {
        try
        {
            using var dapper = CreateConnection();
            string? value = dapper.QueryFirstOrDefault<string>("SELECT Value FROM Setting WHERE Key=@key LIMIT 1;", new { key });
            return ConvertFromString(value, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }


    private void SetSetting<T>(string key, T? value)
    {
        try
        {
            string? val = value?.ToString();
            using var dapper = CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO Setting (Key, Value) VALUES (@key, @val);", new { key, val });
        }
        catch { }
    }



    private static T? ConvertFromString<T>(string? value, T? defaultValue = default)
    {
        if (value is null)
        {
            return defaultValue;
        }
        var converter = TypeDescriptor.GetConverter(typeof(T));
        if (converter == null)
        {
            return defaultValue;
        }
        return (T?)converter.ConvertFromString(value);
    }


    public T? GetKVT<T>(string key, out DateTime time, T? defaultValue = default)
    {
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                return ConvertFromString<T>(kvt.Value, defaultValue);
            }
            else
            {
                return defaultValue;
            }
        }
        catch
        {
            return defaultValue;
        }
    }



    public bool TryGetKVT<T>(string key, out T? result, out DateTime time, T? defaultValue = default)
    {
        result = defaultValue;
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                result = ConvertFromString<T>(kvt.Value, defaultValue);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }



    public void SetKVT<T>(string key, T value, DateTime? time = null)
    {
        try
        {
            using var con = CreateConnection();
            con.Execute("INSERT OR REPLACE INTO KVT (Key, Value, Time) VALUES (@Key, @Value, @Time);", new KVT(key, value?.ToString() ?? "", time ?? DateTime.Now));

        }
        catch { }
    }




    private class KVT
    {

        public KVT() { }


        public KVT(string key, string value, DateTime time)
        {
            Key = key;
            Value = value;
            Time = time;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime Time { get; set; }
    }





    #endregion




    #region Sql Mapper



    internal class DapperSqlMapper
    {

        private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true };

        public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
        {
            public override DateTimeOffset Parse(object value)
            {
                if (value is string str)
                {
                    return DateTimeOffset.Parse(str);
                }
                else
                {
                    return new DateTimeOffset();
                }
            }

            public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
            {
                parameter.Value = value.ToString();
            }
        }



        public class TravelersDiaryPrimogemsMonthGroupStatsListHandler : SqlMapper.TypeHandler<List<TravelersDiaryPrimogemsMonthGroupStats>>
        {
            public override List<TravelersDiaryPrimogemsMonthGroupStats> Parse(object value)
            {
                if (value is string str)
                {
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        return JsonSerializer.Deserialize<List<TravelersDiaryPrimogemsMonthGroupStats>>(str, JsonSerializerOptions)!;
                    }
                }
                return new();
            }

            public override void SetValue(IDbDataParameter parameter, List<TravelersDiaryPrimogemsMonthGroupStats>? value)
            {
                parameter.Value = JsonSerializer.Serialize(value, JsonSerializerOptions);
            }
        }


        public class TrailblazeCalendarMonthDataGroupByListHandler : SqlMapper.TypeHandler<List<TrailblazeCalendarMonthDataGroupBy>>
        {
            public override List<TrailblazeCalendarMonthDataGroupBy> Parse(object value)
            {
                if (value is string str)
                {
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        return JsonSerializer.Deserialize<List<TrailblazeCalendarMonthDataGroupBy>>(str, JsonSerializerOptions)!;
                    }
                }
                return new();
            }

            public override void SetValue(IDbDataParameter parameter, List<TrailblazeCalendarMonthDataGroupBy>? value)
            {
                parameter.Value = JsonSerializer.Serialize(value, JsonSerializerOptions);
            }
        }


        public class StringListHandler : SqlMapper.TypeHandler<List<string>>
        {
            public override List<string> Parse(object value)
            {
                if (value is string str)
                {
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        return JsonSerializer.Deserialize<List<string>>(str)!;
                    }
                }
                return new();
            }

            public override void SetValue(IDbDataParameter parameter, List<string>? value)
            {
                parameter.Value = JsonSerializer.Serialize(value, JsonSerializerOptions);
            }
        }


    }


    #endregion



}
