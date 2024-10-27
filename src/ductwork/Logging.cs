using NLog;
using NLog.Config;
using NLog.Targets;

namespace ductwork;

public static class Logging
{
    public const string DefaultLogFormat = "${longdate} ${level:uppercase=true}: ${message} ${exception}";
    private const string LogNamePrefix = "ductwork_";

    private static bool _isInitialized;
    private static readonly object InitializeLock = new();

    private static void Initialize()
    {
        lock (InitializeLock)
        {
            if (_isInitialized)
            {
                return;
            }
            
            AddRule($"{LogNamePrefix}*", new ColoredConsoleTarget { Layout = DefaultLogFormat });
            _isInitialized = true;
        }
    }

    private static LoggingConfiguration GetConfiguration()
    {
        return LogManager.Configuration ?? new LoggingConfiguration();
    }

    public static Logger GetLogger(string name)
    {
        Initialize();
        return LogManager.GetLogger($"{LogNamePrefix}{name}");
    }

    public static void AddRule(string name, Target target, LogLevel? minLevel = null, LogLevel? maxLevel = null)
    {
        var configuration = GetConfiguration();
        configuration.AddRule(
            minLevel ?? LogLevel.Trace,
            maxLevel ?? LogLevel.Fatal,
            target,
            name);
        LogManager.Configuration = configuration;
    }
}