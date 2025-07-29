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
    public const string LogFileName = "log.txt";
    public static string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFileName);

    private static Logger? _disposableLogger;
    private static ILogger? _instance;
    public static ILogger Instance
    {
        get
        {
            return _instance is null ? throw new NullReferenceException("Logger was called before initialization") : _instance;
        }
    }

    public static void Initialize()
    {
        ExpressionTemplate template = new("{@t:dd MMM yyyy HH:mm:ss} {@l:u3} [{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] {@m}\n");
        if (File.Exists(LogFilePath))
        {
            File.Delete(LogFilePath);
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
                .WriteTo.File(template, LogFilePath);
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
