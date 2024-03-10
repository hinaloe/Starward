using Dapper;
using Microsoft.Data.Sqlite;
using Starward.Dashboard.Server.Services;
using System.Diagnostics;
using System.Reflection;

internal class Program
{

    public static string? AppVersion { get; } = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    public static string? Language { get; private set; }

    public static string? UserDataFolder { get; private set; }

    public static string? DatabasePath { get; private set; }

    public static int DatabaseVersion { get; set; }




    private static void Main(string[] args)
    {
        Console.WriteLine($"Starward Dashboard Server - {AppVersion}");
        ReadConfig();
        if (!CheckDatabase())
        {
            return;
        }

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddSingleton(sp => new DatabaseService($"DataSource={DatabasePath};", sp));

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }




    private static void ReadConfig()
    {
        string? baseDir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\'));
        string exe = Path.Join(baseDir, "Starward.exe");
        if (!File.Exists(exe))
        {
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
        }
        string? iniPath = Path.Join(baseDir, "config.ini");
        IConfigurationBuilder configBuilder = new ConfigurationBuilder().AddCommandLine(Environment.GetCommandLineArgs());
        if (File.Exists(iniPath))
        {
            configBuilder.AddIniFile(iniPath);
        }
        IConfigurationRoot config = configBuilder.Build();

        Language = config.GetValue<string>(nameof(Language));
        string? dir = config.GetValue<string>(nameof(UserDataFolder));
        if (!string.IsNullOrWhiteSpace(dir))
        {
            string folder;
            if (Path.IsPathFullyQualified(dir))
            {
                folder = dir;
            }
            else
            {
                folder = Path.Join(baseDir, dir);
            }
            if (Directory.Exists(folder))
            {
                UserDataFolder = Path.GetFullPath(folder);
            }
        }
        string? database = Path.Join(UserDataFolder, "StarwardDatabase.db");
        if (File.Exists(database))
        {
            DatabasePath = database;
        }
    }



    private static bool CheckDatabase()
    {
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            if (Process.GetCurrentProcess().MainWindowHandle == 0)
            {
                Console.WriteLine("Cannot find the file 'StarwardDatabase.db'.");
                Environment.Exit(-1);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cannot find the file 'StarwardDatabase.db'.");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Press enter key to exit...");
                Console.ResetColor();
                Console.ReadLine();
                return false;
            }
        }

        var con = new SqliteConnection($"DataSource={DatabasePath};");
        con.Open();
        DatabaseVersion = con.QueryFirstOrDefault<int>("PRAGMA USER_VERSION;");
        if (DatabaseVersion < 8)
        {
            if (Process.GetCurrentProcess().MainWindowHandle == 0)
            {
                Console.WriteLine("Database version is less than 8.");
                Environment.Exit(-1);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Database version is less than 8.");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Press enter key to exit...");
                Console.ResetColor();
                Console.ReadLine();
                return false;
            }
        }
        return true;
    }




}