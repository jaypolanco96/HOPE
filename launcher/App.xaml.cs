using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Hope.Launcher;

public partial class App : Application
{
    private Mutex? instance;
    private EventWaitHandle? homeRequest;
    private System.Windows.Threading.DispatcherTimer? homeMonitor;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        string? Value(string flag) { var i = Array.IndexOf(e.Args, flag); return i >= 0 && i + 1 < e.Args.Length ? e.Args[i + 1] : null; }
        try
        {
            if (e.Args.Contains("--self-test"))
            {
                var report = SelfTests.Run(Value("--links"));
                File.WriteAllText(Value("--test-report") ?? Path.Combine(Path.GetTempPath(), "hope-tests.txt"), report);
                Shutdown(0);
                return;
            }
            var bundleRoot = Value("--root") ?? AppContext.BaseDirectory;
            var window = new MainWindow(bundleRoot);
            if (Value("--render") is { } output)
            {
                // Offline layout export: no native window is shown, no game
                // is launched, and no user desktop is inspected or controlled.
                window.Navigate(Value("--page") ?? "Home");
                var size = (Value("--size") ?? "1360x850").Split('x');
                int width = int.Parse(size[0]), height = int.Parse(size[1]);
                var visual = (FrameworkElement)window.Content;
                visual.Measure(new Size(width, height));
                visual.Arrange(new Rect(0, 0, width, height));
                visual.UpdateLayout();
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
                using (var stream = File.Create(output)) encoder.Save(stream);
                window.Dispose();
                Shutdown(0);
                return;
            }
            var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(bundleRoot).ToUpperInvariant())));
            instance = new Mutex(true, @"Local\HOPE-" + identity, out var created);
            homeRequest = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\HOPE-Home-" + identity);
            if (!created)
            {
                homeRequest.Set();
                window.Dispose();
                Shutdown(0);
                return;
            }
            MainWindow = window;
            window.Show();
            homeMonitor = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            homeMonitor.Tick += (_, _) =>
            {
                if (homeRequest.WaitOne(0))
                {
                    window.Navigate("Home");
                    if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
                    window.Activate();
                }
            };
            homeMonitor.Start();
        }
        catch (Exception error)
        {
            if (e.Args.Contains("--render") || e.Args.Contains("--self-test"))
            {
                File.WriteAllText(Value("--test-report") ?? Path.Combine(Path.GetTempPath(), "hope-error.txt"), error.ToString());
            }
            else MessageBox.Show(error.Message, "HOPE — couldn't start", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e) { homeMonitor?.Stop(); homeRequest?.Dispose(); instance?.Dispose(); base.OnExit(e); }
}
