using Avalonia;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace RemnantOverseer;

internal sealed class Program
{
    // Allow only one instance: https://stackoverflow.com/questions/19147/what-is-the-correct-way-to-create-a-single-instance-wpf-application/522874#522874
    static Mutex mutex = new Mutex(false, @"Global\{RO-F7F5EC79-F2CF-4645-820D-241A4E4E6E1A}");
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (mutex.WaitOne(TimeSpan.Zero, false))
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            mutex.ReleaseMutex();
            Log.Dispose();
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
              NativeMethods.PostMessage(
                  (IntPtr)NativeMethods.HWND_BROADCAST,
                  NativeMethods.RO_WM_SHOWME,
                  IntPtr.Zero,
                  IntPtr.Zero);
            } else {
              Console.WriteLine("The application is already running");
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .ConfigureFonts(fontManager =>
            {
                fontManager.AddFontCollection(new MontserratFontCollection());
            })
            .WithInterFont()
            .LogToTrace();
}
