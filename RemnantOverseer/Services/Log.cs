using Avalonia.Controls;
using Serilog;
using Serilog.Core;
using Serilog.Templates;
using System;
using System.IO;
using System.Reflection;

namespace RemnantOverseer.Services;
internal class Log
{
    private static Logger? _disposableLogger;
    private static ILogger? _instance;
    public static ILogger Instance
    {
        get
        {
            return _instance is null ? throw new NullReferenceException("Logger was called before initialization") : _instance;
        }
    }

    public static string GetLogFilePath()
    {
# if REMNANTOVERSEER_USER_DIRECTORIES
        string userdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
        return Path.Combine(userdir, "remnant-two-overseer", "log.txt");
# else
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
# endif
    }

    public static void Initialize()
    {
        ExpressionTemplate template = new("{@t:dd MMM yyyy HH:mm:ss} {@l:u3} [{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] {@m}\n");
        if (File.Exists(GetLogFilePath()))
        {
            File.Delete(GetLogFilePath());
        }

        LoggerConfiguration config;
        if (Design.IsDesignMode)
        {
            config = new LoggerConfiguration();
        }
        else
        {
            config = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(template, GetLogFilePath());
        }

        _disposableLogger = config.CreateLogger();
        _instance = _disposableLogger.ForContext<Program>();
        Instance.Information($"Version {Assembly.GetExecutingAssembly().GetName().Version}");
        lib.remnant2.analyzer.Log.Logger = Instance;
        lib.remnant2.saves.Log.Logger = Instance;
    }

    public static void Dispose()
    {
        _disposableLogger?.Dispose();
    }
}
