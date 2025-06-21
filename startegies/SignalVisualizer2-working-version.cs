using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui.Chart;
using System.Runtime.InteropServices;





namespace NinjaTrader.NinjaScript.Indicators
{
    public enum StrategyPreset
    {
        DayTrading15Min,
        SwingTrading30Min60Min,
        Custom
    }

    public class SignalVisualizer2 : Indicator
    {

        private EMA ema9;
        private EMA ema21;
        private DateTime lastResetDate;
        private DateTime lastNotificationTime = DateTime.MinValue;

        // Track previous signals to avoid duplicate alerts
        private bool previousLongSignal = false;
        private bool previousShortSignal = false;

        // Track daily alert tracking
        private HashSet<string> todaysAlertsSent = new HashSet<string>();

        // HTTP client for Discord webhooks
        private static readonly HttpClient httpClient = new HttpClient();

        // Risk management variables
        private int dailyTradeCount;
        private int consecutiveLosses;
        private double dailyPnL;
        private bool isTradingBlocked;

        private const int MaxDailyTrades = 10;
        private const int MaxConsecutiveLosses = 3;
        private const double MaxDailyLoss = -100.0;
        private const double MaxDailyProfit = 150.0;

        // Simplified logging and rate limiting
        private bool enableDetailedLogging = false;
        private int logCounter = 0;

        // SIMPLIFIED DISCORD RATE LIMITING
        private DateTime lastDiscordNotification = DateTime.MinValue;
        private const int DiscordCooldownMinutes = 1; // Reduced to 1 minute for 15min charts

        // REAL-TIME ONLY NOTIFICATIONS
        private bool indicatorFullyLoaded = false;
        private DateTime realTimeStartTime = DateTime.MinValue;

        [NinjaScriptProperty]
        [Display(Name = "Show Long Signals", Order = 1, GroupName = "Signal Settings")]
        public bool ShowLongSignals { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Short Signals", Order = 2, GroupName = "Signal Settings")]
        public bool ShowShortSignals { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Long Signal Color", Order = 3, GroupName = "Visual Settings")]
        public Brush LongSignalColor { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Short Signal Color", Order = 4, GroupName = "Visual Settings")]
        public Brush ShortSignalColor { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Signal Size", Order = 5, GroupName = "Visual Settings")]
        public int SignalSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Play Sound Alerts", Order = 6, GroupName = "Alert Settings")]
        public bool PlaySoundAlerts { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Long Alert Sound", Order = 7, GroupName = "Alert Settings")]
        public string LongAlertSound { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Short Alert Sound", Order = 8, GroupName = "Alert Settings")]
        public string ShortAlertSound { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable Discord Notifications", GroupName = "Notification Settings")]
        public bool EnableDiscordNotifications { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Send Chart Screenshot to Discord", GroupName = "Notification Settings")]
        public bool SendChartScreenshot { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Discord Webhook URL", GroupName = "Notification Settings")]
        public string DiscordWebhookUrl { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Discord Username", GroupName = "Notification Settings")]
        public string DiscordUsername { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Valid Trading Hours Only", GroupName = "Time Settings")]
        public bool ValidTradingHoursOnly { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Start Time (CST)", GroupName = "Time Settings")]
        public TimeSpan StartTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "End Time (CST)", GroupName = "Time Settings")]
        public TimeSpan EndTime { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Lookback Period", Order = 9, GroupName = "Signal Settings")]
        public int LookbackPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 1.0)]
        [Display(Name = "Range Position Threshold", Order = 10, GroupName = "Signal Settings")]
        public double RangeThreshold { get; set; }

        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name = "EMA Fast Period", Order = 11, GroupName = "Signal Settings")]
        public int EmaFastPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(10, 100)]
        [Display(Name = "EMA Slow Period", Order = 12, GroupName = "Signal Settings")]
        public int EmaSlowPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Strategy Preset", Order = 13, GroupName = "Signal Settings")]
        public StrategyPreset Preset { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable Detailed Logging", Order = 14, GroupName = "Debug Settings")]
        public bool EnableDetailedLogging { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Test Chart Capture", Order = 15, GroupName = "Debug Settings")]
        public bool TriggerTest { get; set; }

        // Add these Windows API declarations inside your SignalVisualizer2 class
[DllImport("user32.dll")]
private static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

[DllImport("user32.dll")]
private static extern IntPtr GetWindowDC(IntPtr hWnd);

[DllImport("user32.dll")]
private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

[DllImport("gdi32.dll")]
private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

[DllImport("gdi32.dll")]
private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

[DllImport("gdi32.dll")]
private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

[DllImport("gdi32.dll")]
private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

[DllImport("gdi32.dll")]
private static extern bool DeleteObject(IntPtr hObject);

[DllImport("gdi32.dll")]
private static extern bool DeleteDC(IntPtr hdc);

[DllImport("user32.dll")]
private static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

// PrintWindow flags
private const uint PW_CLIENTONLY = 0x00000001;
private const uint PW_RENDERFULLCONTENT = 0x00000002;

private const int SRCCOPY = 0x00CC0020;

[StructLayout(LayoutKind.Sequential)]
private struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}


// GDI+ capture method that captures exactly what's on screen
private byte[] CaptureWindowGDI(IntPtr hWnd)
{
    try
    {
        // Get window dimensions
        RECT rect = new RECT();
        GetWindowRect(hWnd, ref rect);

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        Print($"📐 Window dimensions: {width}x{height}");

        // Get device contexts
        IntPtr hdcSrc = GetWindowDC(hWnd);
        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
        IntPtr hOld = SelectObject(hdcDest, hBitmap);

        // Try PrintWindow first (better for hardware accelerated content)
        bool success = PrintWindow(hWnd, hdcDest, PW_RENDERFULLCONTENT);

        if (!success)
        {
            Print("PrintWindow with full content failed, trying BitBlt...");
            // Fallback to BitBlt
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
        }

        SelectObject(hdcDest, hOld);

        // Convert to WPF BitmapSource (no System.Drawing needed!)
        var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            hBitmap,
            IntPtr.Zero,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        // Cleanup GDI objects
        DeleteObject(hBitmap);
        DeleteDC(hdcDest);
        ReleaseDC(hWnd, hdcSrc);

        // Convert BitmapSource to PNG bytes
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using (var stream = new MemoryStream())
        {
            encoder.Save(stream);
            return stream.ToArray();
        }
    }
    catch (Exception ex)
    {
        Print($"❌ WPF capture error: {ex.Message}");
        return null;
    }
}

// Alternative: Capture specific region of screen (if window capture still fails)
private byte[] CaptureScreenRegion(System.Windows.Window window)
{
    try
    {
        Print("📸 Attempting WPF screen region capture...");

        // Force window to foreground
        var helper = new System.Windows.Interop.WindowInteropHelper(window);
        SetForegroundWindow(helper.Handle);
        System.Threading.Thread.Sleep(100);

        // Get window bounds
        RECT rect = new RECT();
        GetWindowRect(helper.Handle, ref rect);

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        // Use RenderTargetBitmap with the actual window
        var renderTarget = new RenderTargetBitmap(
            width, height, 96, 96, PixelFormats.Pbgra32);

        // Try to render the window
        try
        {
            renderTarget.Render(window);
        }
        catch
        {
            // If direct render fails, try with visual
            var visual = window as System.Windows.Media.Visual;
            if (visual != null)
            {
                renderTarget.Render(visual);
            }
        }

        // Convert to PNG
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        using (var stream = new MemoryStream())
        {
            encoder.Save(stream);
            return stream.ToArray();
        }
    }
    catch (Exception ex)
    {
        Print($"❌ Screen region error: {ex.Message}");
        return null;
    }
}
[DllImport("user32.dll")]
private static extern bool SetForegroundWindow(IntPtr hWnd);

// Alternative: Direct screen capture using Windows Desktop Duplication API
private byte[] CaptureDesktopRegion(IntPtr hWnd)
{
    try
    {
        RECT rect = new RECT();
        GetWindowRect(hWnd, ref rect);

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        // Create compatible DC and bitmap
        IntPtr hdcScreen = GetWindowDC(IntPtr.Zero); // Get entire screen DC
        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
        IntPtr hOld = SelectObject(hdcMem, hBitmap);

        // Copy screen region where window is located
        BitBlt(hdcMem, 0, 0, width, height, hdcScreen, rect.Left, rect.Top, SRCCOPY);

        SelectObject(hdcMem, hOld);

        // Convert to BitmapSource
        var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            hBitmap,
            IntPtr.Zero,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        // Cleanup
        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        // Convert to PNG
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using (var stream = new MemoryStream())
        {
            encoder.Save(stream);
            return stream.ToArray();
        }
    }
    catch (Exception ex)
    {
        Print($"❌ Desktop capture error: {ex.Message}");
        return null;
    }
}
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Discord alerts with chart screenshots for trading setups";
                Name = "SignalVisualizer2";
                IsOverlay = true;
                Calculate = Calculate.OnBarClose;

                Print("*** DISCORD ALERT INDICATOR - OnBarClose Mode ***");

                ShowLongSignals = true;
                ShowShortSignals = true;
                LongSignalColor = Brushes.LimeGreen;
                ShortSignalColor = Brushes.Red;
                SignalSize = 3;
                PlaySoundAlerts = true;
                LongAlertSound = "Alert1.wav";
                ShortAlertSound = "Alert2.wav";

                EnableDiscordNotifications = true;
                SendChartScreenshot = true;
                DiscordWebhookUrl = "https://discord.com/api/webhooks/1382051062252572723/LfEeLa14h1J9yYO_gbHDINoFkUG58iH6m3VV0UIGtWM3F0SeUM1BCydOfhwzkXsea4uY"; // User must configure
                DiscordUsername = "NinjaTrader Bot";

                ValidTradingHoursOnly = true;
                StartTime = new TimeSpan(1, 30, 0);
                EndTime = new TimeSpan(23, 0, 0);

                // 15-minute optimized settings
                LookbackPeriod = 20;
                RangeThreshold = 0.6;
                EmaFastPeriod = 9;
                EmaSlowPeriod = 21;
                Preset = StrategyPreset.DayTrading15Min;
                EnableDetailedLogging = false;
                TriggerTest = false;

                ResetDaily();
            }
            else if (State == State.Configure)
            {
                ApplyPresetSettings();
                try
                {
                    ema9 = EMA(EmaFastPeriod);
                    ema21 = EMA(EmaSlowPeriod);
                }
                catch (Exception ex)
                {
                    Print($"Error initializing EMAs: {ex.Message}");
                }
            }
            else if (State == State.DataLoaded)
            {
                lastResetDate = Time[0].Date;
                enableDetailedLogging = EnableDetailedLogging;
                Print($"*** LOADING HISTORICAL DATA - NO NOTIFICATIONS ***");
                indicatorFullyLoaded = false;
                realTimeStartTime = DateTime.MinValue;
            }
            else if (State == State.Realtime)
            {
                indicatorFullyLoaded = true;
                realTimeStartTime = DateTime.Now;
                Print($"*** REAL-TIME MODE - DISCORD NOTIFICATIONS ENABLED ***");
                Print($"Real-time start: {realTimeStartTime:HH:mm:ss}");
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(EmaSlowPeriod, LookbackPeriod))
                return;

            // Test capture trigger
            if (TriggerTest && State == State.Realtime)
            {
                TriggerTest = false;
                Print("🧪 Manual test triggered from NinjaTrader button!");
                TestChartCapture();
                return;
            }

            try
            {
                if (ema9 == null || ema21 == null)
                {
                    Print("Error: EMAs not initialized");
                    return;
                }

                // Reset daily counters
                if (Time[0].Date != lastResetDate)
                {
                    ResetDaily();
                    lastResetDate = Time[0].Date;
                    RemoveDrawObjects();
                    previousLongSignal = false;
                    previousShortSignal = false;
                    todaysAlertsSent.Clear();
                    Print($"New trading day: {Time[0].Date:yyyy-MM-dd}");
                }

                // Calculate setups
                var (longSignal, shortSignal) = CalculateSetups();

                // Log analysis
                if (enableDetailedLogging && (logCounter % 10 == 0 || longSignal || shortSignal))
                {
                    LogTradeAnalysis(longSignal, shortSignal);
                }
                logCounter++;

                // Check trading hours
                bool inTradingHours = IsInTradingHours();

                // Process signals
                if (!isTradingBlocked && inTradingHours)
                    ProcessSignals(longSignal, shortSignal);

                // Update displays
                UpdateRiskDisplay();
                UpdatePermissionDisplay(longSignal, shortSignal, inTradingHours);

                // Update previous states
                previousLongSignal = longSignal;
                previousShortSignal = shortSignal;
            }
            catch (Exception ex)
            {
                Print($"Error in OnBarUpdate: {ex.Message}");
            }
        }

        private void UpdatePermissionDisplay(bool longSignal, bool shortSignal, bool inTradingHours)
        {
            bool anySetup = longSignal || shortSignal;
            if (!isTradingBlocked && anySetup && inTradingHours)
            {
                Draw.TextFixed(this, "PermissionText", "✅ OK TO MANUAL TRADE", TextPosition.TopRight,
                    Brushes.LimeGreen, new SimpleFont("Arial", 16), Brushes.Transparent, Brushes.Transparent, 5);
            }
            else
            {
                string reason = !inTradingHours ? " (Outside Hours)" :
                               isTradingBlocked ? " (Risk Blocked)" : " (No Setup)";
                Draw.TextFixed(this, "PermissionText", $"❌ DO NOT TRADE{reason}", TextPosition.TopRight,
                    Brushes.Red, new SimpleFont("Arial", 16), Brushes.Transparent, Brushes.Transparent, 5);
            }
        }

        private bool IsInTradingHours()
        {
            if (!ValidTradingHoursOnly) return true;
            try
            {
                TimeSpan currentTime = DateTime.Now.TimeOfDay;
                return currentTime >= StartTime && currentTime <= EndTime;
            }
            catch
            {
                return true;
            }
        }

        private void ApplyPresetSettings()
        {
            if (Preset == StrategyPreset.DayTrading15Min)
            {
                LookbackPeriod = 20;
                RangeThreshold = 0.6;
                EmaFastPeriod = 9;
                EmaSlowPeriod = 21;
                Print("Applied DAY TRADING 15-MIN preset");
            }
        }

        private (bool, bool) CalculateSetups()
        {
            try
            {
                double range = High[0] - Low[0];
                if (range <= 0) return (false, false);

                // 15-minute day trading setup
                bool hasVolumeData = CurrentBar >= 10;
                double avgVolume = hasVolumeData ? SMA(Volume, 10)[0] : Volume[0];

                bool longSignal = ShowLongSignals &&
                                  Close[0] > ema9[0] &&
                                  Close[0] > ema21[0] &&
                                  ema9[0] > ema21[0] &&
                                  Close[0] >= Low[0] + RangeThreshold * range &&
                                  Volume[0] > avgVolume &&
                                  High[0] > MAX(High, LookbackPeriod)[1];

                bool shortSignal = ShowShortSignals &&
                                   Close[0] < ema9[0] &&
                                   Close[0] < ema21[0] &&
                                   ema9[0] < ema21[0] &&
                                   Close[0] <= High[0] - RangeThreshold * range &&
                                   Volume[0] > avgVolume &&
                                   Low[0] < MIN(Low, LookbackPeriod)[1];

                return (longSignal, shortSignal);
            }
            catch (Exception ex)
            {
                Print($"Error calculating setups: {ex.Message}");
                return (false, false);
            }
        }

        private void ProcessSignals(bool longSignal, bool shortSignal)
        {
            try
            {
                // Prevent conflicting signals
                if (longSignal && shortSignal)
                {
                    Print($"WARNING: Both signals detected - skipping");
                    return;
                }

                // Only process NEW signals
                bool isNewLongSignal = longSignal && !previousLongSignal;
                bool isNewShortSignal = shortSignal && !previousShortSignal;

                if (isNewLongSignal)
                {
                    ProcessLongSignal();
                }

                if (isNewShortSignal)
                {
                    ProcessShortSignal();
                }
            }
            catch (Exception ex)
            {
                Print($"Error processing signals: {ex.Message}");
            }
        }

        private void ProcessLongSignal()
{
    // Capture price and time IMMEDIATELY
    double signalPrice = Close[0];
    DateTime signalTime = DateTime.Now;

    // Draw visual signal
    string tag = "LongArrow" + CurrentBar;
    double y = Low[0] - SignalSize * TickSize;
    Draw.TriangleUp(this, tag, true, 0, y, LongSignalColor);
    Draw.Text(this, "ManualLong" + CurrentBar, "📈 Long Entry", 0, y - 2 * TickSize, Brushes.Green);

    Print($"🟢 NEW LONG SIGNAL: Price={signalPrice:F2}, Bar={CurrentBar}, State={State}");

    // Play sound
    if (PlaySoundAlerts)
    {
        try
        {
            Alert("LongAlert", Priority.Medium, "Long Setup!", LongAlertSound, 10, LongSignalColor, Brushes.Black);
        }
        catch (Exception ex)
        {
            Print($"Sound alert error: {ex.Message}");
        }
    }

    // Send Discord notification for REAL-TIME signals only
    if (State == State.Realtime && indicatorFullyLoaded)
    {
        Print($"📤 Preparing Discord alert with CAPTURED price: {signalPrice:F2}");

        // CAPTURE SCREENSHOT ON UI THREAD FIRST!
        if (SendChartScreenshot && ChartControl != null)
        {
            ChartControl.Dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    // Capture screenshot on UI thread
                    byte[] screenshot = await CaptureChartScreenshotOnUIThread();

                    // Now send to Discord on background thread
                    Task.Run(async () =>
                    {
                        await SendDiscordAlertWithScreenshot("LONG SIGNAL", true, signalPrice, screenshot);
                    });
                }
                catch (Exception ex)
                {
                    Print($"Screenshot capture error: {ex.Message}");
                    // Fallback: send without screenshot
                    Task.Run(async () =>
                    {
                        await SendDiscordAlertWithScreenshot("LONG SIGNAL", true, signalPrice, null);
                    });
                }
            }));
        }
        else
        {
            // No screenshot requested, just send text
            Task.Run(async () =>
            {
                await SendDiscordAlertWithScreenshot("LONG SIGNAL", true, signalPrice, null);
            });
        }
    }
    else
    {
        Print($"SIGNAL DETECTED - Not sending Discord (State: {State}, Loaded: {indicatorFullyLoaded})");
    }
}


      private void ProcessShortSignal()
{
    // Capture price and time IMMEDIATELY
    double signalPrice = Close[0];
    DateTime signalTime = DateTime.Now;

    // Draw visual signal
    string tag = "ShortArrow" + CurrentBar;
    double y = High[0] + SignalSize * TickSize;
    Draw.TriangleDown(this, tag, true, 0, y, ShortSignalColor);
    Draw.Text(this, "ManualShort" + CurrentBar, "📉 SHORT Entry", 0, y + 2 * TickSize, Brushes.Red);

    Print($"🔴 NEW SHORT SIGNAL: Price={signalPrice:F2}, Bar={CurrentBar}, State={State}");

    // Play sound
    if (PlaySoundAlerts)
    {
        try
        {
            Alert("ShortAlert", Priority.Medium, "Short Setup!", ShortAlertSound, 10, ShortSignalColor, Brushes.Black);
        }
        catch (Exception ex)
        {
            Print($"Sound alert error: {ex.Message}");
        }
    }

    // Send Discord notification for REAL-TIME signals only
    if (State == State.Realtime && indicatorFullyLoaded)
    {
        Print($"📤 Preparing Discord alert with CAPTURED price: {signalPrice:F2}");

        // CAPTURE SCREENSHOT ON UI THREAD FIRST!
        if (SendChartScreenshot && ChartControl != null)
        {
            ChartControl.Dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    // Capture screenshot on UI thread
                    byte[] screenshot = await CaptureChartScreenshotOnUIThread();

                    // Now send to Discord on background thread
                    Task.Run(async () =>
                    {
                        await SendDiscordAlertWithScreenshot("SHORT SIGNAL", false, signalPrice, screenshot);
                    });
                }
                catch (Exception ex)
                {
                    Print($"Screenshot capture error: {ex.Message}");
                    // Fallback: send without screenshot
                    Task.Run(async () =>
                    {
                        await SendDiscordAlertWithScreenshot("SHORT SIGNAL", false, signalPrice, null);
                    });
                }
            }));
        }
        else
        {
            // No screenshot requested, just send text
            Task.Run(async () =>
            {
                await SendDiscordAlertWithScreenshot("SHORT SIGNAL", false, signalPrice, null);
            });
        }
    }
    else
    {
        Print($"SIGNAL DETECTED - Not sending Discord (State: {State}, Loaded: {indicatorFullyLoaded})");
    }
}
private async Task<byte[]> CaptureChartScreenshotOnUIThread()
{
    try
    {
        Print("📸 Taking screenshot on UI thread...");

        // Get the chart window
        var window = System.Windows.Window.GetWindow(ChartControl);
        if (window == null)
        {
            Print("❌ No window found");
            return null;
        }

        // Get window handle
        var helper = new System.Windows.Interop.WindowInteropHelper(window);
        IntPtr windowHandle = helper.Handle;

        if (windowHandle == IntPtr.Zero)
        {
            Print("❌ No window handle");
            return null;
        }

        // Small delay to ensure chart is rendered
        await Task.Delay(500);

        // Capture using the desktop region method (already on UI thread)
        byte[] screenshot = CaptureDesktopRegion(windowHandle);

        if (screenshot != null)
        {
            Print($"✅ Screenshot captured: {screenshot.Length:N0} bytes");
        }

        return screenshot;
    }
    catch (Exception ex)
    {
        Print($"❌ Screenshot error: {ex.Message}");
        return null;
    }
}
private async Task SendDiscordAlertWithScreenshot(string signalType, bool isLong, double signalPrice, byte[] screenshotBytes)
{
    try
    {
        // Rate limiting check
        double timeSinceLastDiscord = (DateTime.Now - lastDiscordNotification).TotalMinutes;
        if (timeSinceLastDiscord < DiscordCooldownMinutes)
        {
            Print($"DISCORD RATE LIMITED: {timeSinceLastDiscord:F1} minutes since last (need {DiscordCooldownMinutes})");
            return;
        }

        // Create unique alert key
        DateTime now = DateTime.Now;
        string alertKey = $"{now.Date:yyyy-MM-dd}_{now:HH:mm}_{signalType}_{signalPrice:F2}";

        if (todaysAlertsSent.Contains(alertKey))
        {
            Print($"DUPLICATE BLOCKED: {alertKey}");
            return;
        }

        if (!EnableDiscordNotifications || string.IsNullOrWhiteSpace(DiscordWebhookUrl))
        {
            Print("Discord notifications disabled or no webhook URL");
            return;
        }

        Print($"🚀 SENDING DISCORD ALERT: {signalType} at {signalPrice:F2}");

        // Send to Discord with retries
        int maxRetries = 3;
        bool success = false;

        for (int attempt = 1; attempt <= maxRetries && !success; attempt++)
        {
            Print($"Discord attempt {attempt}/{maxRetries}");
            success = await SendDiscordMessageWithScreenshot(signalType, isLong, signalPrice, now, screenshotBytes);

            if (!success && attempt < maxRetries)
            {
                Print($"Attempt {attempt} failed, waiting before retry...");
                await Task.Delay(2000);
            }
        }

        if (success)
        {
            lastDiscordNotification = DateTime.Now;
            todaysAlertsSent.Add(alertKey);
            Print($"✅ Discord alert sent successfully!");
        }
        else
        {
            Print($"❌ Discord alert failed after {maxRetries} attempts");
        }
    }
    catch (Exception ex)
    {
        Print($"Discord alert error: {ex.Message}");
    }
}

      private async Task SendDiscordAlert(string signalType, bool isLong, double signalPrice)
{
    try
    {
        // CRITICAL: Only send for real-time mode
        if (!indicatorFullyLoaded || State != State.Realtime)
        {
            Print($"SKIPPING: Not real-time (State: {State}, Loaded: {indicatorFullyLoaded})");
            return;
        }

        // Simple rate limiting - 1 minute minimum between Discord alerts
        double timeSinceLastDiscord = (DateTime.Now - lastDiscordNotification).TotalMinutes;
        if (timeSinceLastDiscord < DiscordCooldownMinutes)
        {
            Print($"DISCORD RATE LIMITED: {timeSinceLastDiscord:F1} minutes since last (need {DiscordCooldownMinutes})");
            return;
        }

        // Create unique alert key for today
        DateTime now = DateTime.Now;
        string alertKey = $"{now.Date:yyyy-MM-dd}_{now:HH:mm}_{signalType}_{signalPrice:F2}";

        if (todaysAlertsSent.Contains(alertKey))
        {
            Print($"DUPLICATE BLOCKED: {alertKey}");
            return;
        }

        if (!EnableDiscordNotifications || string.IsNullOrWhiteSpace(DiscordWebhookUrl))
        {
            Print("Discord notifications disabled or no webhook URL");
            return;
        }

        Print($"🚀 SENDING DISCORD ALERT: {signalType} at {signalPrice:F2}");

        // CAPTURE SCREENSHOT ON UI THREAD FIRST
        byte[] screenshotBytes = null;
        if (SendChartScreenshot)
        {
            screenshotBytes = await CaptureChartScreenshot();
        }

        // Now send to Discord on background thread
        Task.Run(async () =>
        {
            // Try multiple times if Discord send fails
            int maxRetries = 3;
            bool success = false;
            int attempt = 1;

            for (attempt = 1; attempt <= maxRetries && !success; attempt++)
            {
                Print($"Discord attempt {attempt}/{maxRetries}");
                success = await SendDiscordMessageWithScreenshot(signalType, isLong, signalPrice, DateTime.Now, screenshotBytes);

                if (!success && attempt < maxRetries)
                {
                    Print($"Attempt {attempt} failed, waiting before retry...");
                    await Task.Delay(2000); // Wait 2 seconds before retry
                }
            }

            if (success)
            {
                lastDiscordNotification = DateTime.Now;
                todaysAlertsSent.Add(alertKey);
                Print($"✅ Discord alert sent successfully after {attempt-1} attempts!");
            }
            else
            {
                Print($"❌ Discord alert failed after {maxRetries} attempts");
            }
        });
    }
    catch (Exception ex)
    {
        Print($"Discord alert error: {ex.Message}");
    }
}

      // Replace your SendDiscordMessage method with this fixed version:
private async Task<bool> SendDiscordMessageWithScreenshot(string signalType, bool isLong, double price, DateTime time, byte[] chartImageBytes)
{
    try
    {
        Print($"Building Discord message - Signal: {signalType}, Price: {price:F2}, Time: {time:HH:mm:ss}");

        if (chartImageBytes != null && chartImageBytes.Length > 0)
        {
            Print($"✅ Using pre-captured screenshot: {chartImageBytes.Length:N0} bytes");

            // SAVE A COPY FOR DEBUGGING
            try
            {
                string debugFile = Path.Combine(Path.GetTempPath(), $"discord_debug_{DateTime.Now:HHmmss}.png");
                File.WriteAllBytes(debugFile, chartImageBytes);
                Print($"📁 Debug copy saved: {debugFile}");
            }
            catch { }
        }
        else if (SendChartScreenshot)
        {
            Print("❌ No screenshot available - sending text-only message");
        }

        // Build Discord message
        string color = isLong ? "65280" : "16711680"; // Green or Red
        string emoji = isLong ? "📈" : "📉";

        var jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{");
        jsonBuilder.Append($"\"username\":\"{EscapeJson(DiscordUsername)}\",");
        jsonBuilder.Append("\"embeds\":[{");
        jsonBuilder.Append($"\"title\":\"{emoji} {EscapeJson(signalType)}\",");
        jsonBuilder.Append($"\"description\":\"**{EscapeJson(Instrument.FullName)}** trading signal detected\",");
        jsonBuilder.Append($"\"color\":{color},");
        jsonBuilder.Append("\"fields\":[");
        jsonBuilder.Append($"{{\"name\":\"📊 Symbol\",\"value\":\"{EscapeJson(Instrument.FullName)}\",\"inline\":true}},");
        jsonBuilder.Append($"{{\"name\":\"💰 Price\",\"value\":\"{price:F2}\",\"inline\":true}},");
        jsonBuilder.Append($"{{\"name\":\"🕐 Time\",\"value\":\"{time:HH:mm:ss}\",\"inline\":true}},");
        jsonBuilder.Append($"{{\"name\":\"📍 Direction\",\"value\":\"{(isLong ? "🟢 LONG" : "🔴 SHORT")}\",\"inline\":true}},");
        jsonBuilder.Append($"{{\"name\":\"📈 Bar\",\"value\":\"{CurrentBar}\",\"inline\":true}},");
        jsonBuilder.Append($"{{\"name\":\"⚡ State\",\"value\":\"{State}\",\"inline\":true}}");
        jsonBuilder.Append("]");

        // ONLY add image reference if we actually have image bytes
        if (chartImageBytes != null && chartImageBytes.Length > 0)
        {
            jsonBuilder.Append(",\"image\":{\"url\":\"attachment://chart.png\"}");
            Print("📸 Image reference added to embed");
        }

        jsonBuilder.Append(",\"footer\":{\"text\":\"NinjaTrader SignalVisualizer2\"}");
        jsonBuilder.Append($",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"");
        jsonBuilder.Append("}]}");

        string jsonPayload = jsonBuilder.ToString();
        Print($"Discord JSON payload length: {jsonPayload.Length} characters");

        // Create multipart content with proper ordering
        using (var content = new MultipartFormDataContent())
        {
            // IMPORTANT: Add file FIRST, then JSON
            if (chartImageBytes != null && chartImageBytes.Length > 0)
            {
                var imageContent = new ByteArrayContent(chartImageBytes);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                imageContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"file\"",
                    FileName = "\"chart.png\""
                };
                content.Add(imageContent);
                Print($"📎 Added image to multipart: {chartImageBytes.Length:N0} bytes");
            }

            // Add JSON payload AFTER the file
            var jsonContent = new StringContent(jsonPayload, Encoding.UTF8);
            jsonContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            jsonContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = "\"payload_json\""
            };
            content.Add(jsonContent);

            Print($"🚀 Sending Discord webhook with {content.Count()} parts...");

            // Send with extended timeout
            using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                var response = await httpClient.PostAsync(DiscordWebhookUrl, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    Print("✅ Discord webhook sent successfully!");
                    return true;
                }
                else
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    Print($"❌ Discord webhook failed: {response.StatusCode}");
                    Print($"Response: {responseText}");

                    // Log response headers for debugging
                    Print("Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Print($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    return false;
                }
            }
        }
    }
    catch (Exception ex)
    {
        Print($"❌ Discord send error: {ex.Message}");
        Print($"Stack trace: {ex.StackTrace}");
        return false;
    }
}

   // REPLACE your current CaptureChartScreenshot method with this:
private async Task<byte[]> CaptureChartScreenshot()
{
    try
    {
        Print("📸 Taking desktop region screen capture...");

        // Get the chart window
        var window = System.Windows.Window.GetWindow(ChartControl);
        if (window == null)
        {
            Print("❌ No window found");
            return null;
        }

        // Wait for any pending renders
        await Task.Delay(1000); // Give chart time to render

        // Capture on UI thread
        byte[] screenshot = null;
        await ChartControl.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // Get window handle
                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                IntPtr windowHandle = helper.Handle;

                if (windowHandle == IntPtr.Zero)
                {
                    Print("❌ No window handle");
                    return;
                }

                // USE THE WORKING DESKTOP REGION METHOD!
                screenshot = CaptureDesktopRegion(windowHandle);
            }
            catch (Exception ex)
            {
                Print($"❌ UI thread error: {ex.Message}");
            }
        });

        if (screenshot != null)
        {
            Print($"✅ Desktop region screenshot captured: {screenshot.Length:N0} bytes");

            // Check for proper file size (desktop captures are usually 100KB+)
            if (screenshot.Length < 50000)
            {
                Print("⚠️ Screenshot might be incomplete - file size is small");
            }
        }

        return screenshot;
    }
    catch (Exception ex)
    {
        Print($"❌ Screenshot error: {ex.Message}");
        return null;
    }
}

        // Simple window capture method
     private byte[] CaptureWindowContent(System.Windows.Window window)
{
    try
    {
        // Try to capture the chart control directly first
        System.Windows.FrameworkElement targetElement = ChartControl;

        // If ChartControl has a parent that contains the full chart, use that
        if (ChartControl.Parent is System.Windows.FrameworkElement parent)
        {
            targetElement = parent;
            Print($"📊 Using parent element for capture: {parent.GetType().Name}");
        }

        // Ensure the element is visible and loaded
        if (!targetElement.IsLoaded || !targetElement.IsVisible)
        {
            Print("⚠️ Target element not ready, falling back to window capture");
            targetElement = window;
        }

        // Get the actual size
        var size = new System.Windows.Size(targetElement.ActualWidth, targetElement.ActualHeight);

        // Ensure valid size
        if (size.Width <= 0 || size.Height <= 0)
        {
            Print($"⚠️ Invalid size detected, using window size");
            size = new System.Windows.Size(window.ActualWidth, window.ActualHeight);
        }

        Print($"📐 Capture size: {size.Width}x{size.Height}");

        // Measure and arrange to ensure layout is complete
        targetElement.Measure(size);
        targetElement.Arrange(new System.Windows.Rect(size));

        // Create bitmap with proper DPI
        var dpiScale = System.Windows.Media.VisualTreeHelper.GetDpi(targetElement);
        var bitmap = new RenderTargetBitmap(
            (int)size.Width,
            (int)size.Height,
            dpiScale.PixelsPerInchX,
            dpiScale.PixelsPerInchY,
            PixelFormats.Pbgra32);

        // Create visual brush to ensure all layers are captured
        var drawingVisual = new System.Windows.Media.DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            var visualBrush = new System.Windows.Media.VisualBrush(targetElement);
            drawingContext.DrawRectangle(
                visualBrush,
                null,
                new System.Windows.Rect(new System.Windows.Point(), size));
        }

        // Render the visual
        bitmap.Render(drawingVisual);

        // Convert to PNG
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using (var stream = new MemoryStream())
        {
            encoder.Save(stream);
            return stream.ToArray();
        }
    }
    catch (Exception ex)
    {
        Print($"❌ Window capture error: {ex.Message}");

        // Fallback: Try simple window capture
        try
        {
            Print("🔄 Attempting fallback capture method...");
            return SimpleFallbackCapture(window);
        }
        catch (Exception fallbackEx)
        {
            Print($"❌ Fallback also failed: {fallbackEx.Message}");
            return null;
        }
    }
}

private byte[] SimpleFallbackCapture(System.Windows.Window window)
{
    var bitmap = new RenderTargetBitmap(
        (int)window.ActualWidth,
        (int)window.ActualHeight,
        96, 96,
        PixelFormats.Pbgra32);

    bitmap.Render(window);

    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));

    using (var stream = new MemoryStream())
    {
        encoder.Save(stream);
        return stream.ToArray();
    }
}
        // Simple test method
// Replace your TestChartCapture() method with this simplified version:
public void TestChartCapture()
{
    Print("🧪 Starting chart capture test...");

    if (ChartControl == null)
    {
        Print("❌ ChartControl is null - cannot capture");
        return;
    }

    ChartControl.Dispatcher.BeginInvoke(new Action(async () =>
    {
        try
        {
            Print("📊 Testing desktop region capture (the working method)...");

            // Give chart time to stabilize
            await Task.Delay(1000);

            // Use the same method that Discord alerts will use
            var bytes = await CaptureChartScreenshot();

            if (bytes != null && bytes.Length > 0)
            {
                string fileName = $"ninjatrader_test_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePath = Path.Combine(documentsPath, fileName);

                File.WriteAllBytes(filePath, bytes);
                Print($"✅ Test capture saved: {filePath} ({bytes.Length:N0} bytes)");

                // Check file size
                if (bytes.Length < 50000)
                {
                    Print($"⚠️ Small file size - image might be incomplete");
                }
                else
                {
                    Print($"✅ Good file size - capture successful!");
                }

                // Open the image
                try
                {
                    System.Diagnostics.Process.Start(filePath);
                }
                catch { }
            }
            else
            {
                Print($"❌ Test capture failed - no data");
            }
        }
        catch (Exception ex)
        {
            Print($"❌ Test error: {ex.Message}");
        }
    }));
}
private void SaveTestImage(byte[] bytes, string method)
{
    if (bytes != null && bytes.Length > 0)
    {
        string fileName = $"ninjatrader_{method}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string filePath = Path.Combine(documentsPath, fileName);

        File.WriteAllBytes(filePath, bytes);
        Print($"✅ {method} saved: {filePath} ({bytes.Length:N0} bytes)");

        // Check file size
        if (bytes.Length < 100000) // Less than 100KB might indicate issue
        {
            Print($"⚠️ Small file size - image might be blank or partial");
        }

        try
        {
            System.Diagnostics.Process.Start(filePath);
        }
        catch { }
    }
    else
    {
        Print($"❌ {method} capture failed");
    }
}
        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }

        private void UpdateRiskDisplay()
        {
            try
            {
                string status = isTradingBlocked ? "BLOCKED" : "OK";
                Brush color = isTradingBlocked ? Brushes.Red : Brushes.LimeGreen;

                Draw.TextFixed(this, "RiskStatus",
                    $"RISK: {status} | Trades: {dailyTradeCount}/{MaxDailyTrades} | PnL: ${dailyPnL:F2}",
                    TextPosition.TopLeft, color, new SimpleFont("Arial", 12),
                    Brushes.Transparent, Brushes.Transparent, 10);
            }
            catch (Exception ex)
            {
                Print($"Risk display error: {ex.Message}");
            }
        }

        private void ResetDaily()
        {
            dailyTradeCount = 0;
            consecutiveLosses = 0;
            dailyPnL = 0;
            isTradingBlocked = false;
            todaysAlertsSent.Clear();
            logCounter = 0;
            lastNotificationTime = DateTime.MinValue;
            lastDiscordNotification = DateTime.MinValue;
            previousLongSignal = false;
            previousShortSignal = false;

            Print($"=== DAILY RESET COMPLETE === {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            Print($"Risk counters reset, Discord notifications enabled");
        }

        private void LogTradeAnalysis(bool longSignal, bool shortSignal)
        {
            if (!enableDetailedLogging) return;

            try
            {
                DateTime currentTime = DateTime.Now;
                double currentPrice = Close[0];
                double range = High[0] - Low[0];

                string timestamp = $"[{currentTime:HH:mm:ss}]";

                Print($"\n{timestamp} === TRADE ANALYSIS ({Preset}) ===");
                Print($"{timestamp} Price: {currentPrice:F2} | H: {High[0]:F2} | L: {Low[0]:F2} | Range: {range:F2}");
                Print($"{timestamp} EMA{EmaFastPeriod}: {ema9[0]:F2} | EMA{EmaSlowPeriod}: {ema21[0]:F2}");

                if (longSignal && !previousLongSignal)
                {
                    Print($"{timestamp} 🟢 NEW LONG SIGNAL DETECTED!");
                }
                if (shortSignal && !previousShortSignal)
                {
                    Print($"{timestamp} 🔴 NEW SHORT SIGNAL DETECTED!");
                }

                Print($"{timestamp} Risk: Trades={dailyTradeCount}/{MaxDailyTrades}, Losses={consecutiveLosses}/{MaxConsecutiveLosses}, PnL=${dailyPnL:F2}, Blocked={isTradingBlocked}");
                Print($"{timestamp} State: {State}, Loaded: {indicatorFullyLoaded}");
                Print($"{timestamp} ==========================================");
            }
            catch (Exception ex)
            {
                Print($"Logging error: {ex.Message}");
            }
        }

        public void RecordTrade(double pnl)
        {
            try
            {
                dailyTradeCount++;
                dailyPnL += pnl;

                if (pnl < 0)
                    consecutiveLosses++;
                else
                    consecutiveLosses = 0;

                if (dailyTradeCount >= MaxDailyTrades ||
                    consecutiveLosses >= MaxConsecutiveLosses ||
                    dailyPnL <= MaxDailyLoss ||
                    dailyPnL >= MaxDailyProfit)
                {
                    isTradingBlocked = true;
                    Print($"Trading blocked: Trades={dailyTradeCount}, Losses={consecutiveLosses}, PnL={dailyPnL:C2}");
                }
            }
            catch (Exception ex)
            {
                Print($"Error recording trade: {ex.Message}");
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SignalVisualizer2[] cacheSignalVisualizer2;
		public SignalVisualizer2 SignalVisualizer2(bool showLongSignals, bool showShortSignals, Brush longSignalColor, Brush shortSignalColor, int signalSize, bool playSoundAlerts, string longAlertSound, string shortAlertSound, bool enableDiscordNotifications, bool sendChartScreenshot, string discordWebhookUrl, string discordUsername, bool validTradingHoursOnly, TimeSpan startTime, TimeSpan endTime, int lookbackPeriod, double rangeThreshold, int emaFastPeriod, int emaSlowPeriod, StrategyPreset preset, bool enableDetailedLogging, bool triggerTest)
		{
			return SignalVisualizer2(Input, showLongSignals, showShortSignals, longSignalColor, shortSignalColor, signalSize, playSoundAlerts, longAlertSound, shortAlertSound, enableDiscordNotifications, sendChartScreenshot, discordWebhookUrl, discordUsername, validTradingHoursOnly, startTime, endTime, lookbackPeriod, rangeThreshold, emaFastPeriod, emaSlowPeriod, preset, enableDetailedLogging, triggerTest);
		}

		public SignalVisualizer2 SignalVisualizer2(ISeries<double> input, bool showLongSignals, bool showShortSignals, Brush longSignalColor, Brush shortSignalColor, int signalSize, bool playSoundAlerts, string longAlertSound, string shortAlertSound, bool enableDiscordNotifications, bool sendChartScreenshot, string discordWebhookUrl, string discordUsername, bool validTradingHoursOnly, TimeSpan startTime, TimeSpan endTime, int lookbackPeriod, double rangeThreshold, int emaFastPeriod, int emaSlowPeriod, StrategyPreset preset, bool enableDetailedLogging, bool triggerTest)
		{
			if (cacheSignalVisualizer2 != null)
				for (int idx = 0; idx < cacheSignalVisualizer2.Length; idx++)
					if (cacheSignalVisualizer2[idx] != null && cacheSignalVisualizer2[idx].ShowLongSignals == showLongSignals && cacheSignalVisualizer2[idx].ShowShortSignals == showShortSignals && cacheSignalVisualizer2[idx].LongSignalColor == longSignalColor && cacheSignalVisualizer2[idx].ShortSignalColor == shortSignalColor && cacheSignalVisualizer2[idx].SignalSize == signalSize && cacheSignalVisualizer2[idx].PlaySoundAlerts == playSoundAlerts && cacheSignalVisualizer2[idx].LongAlertSound == longAlertSound && cacheSignalVisualizer2[idx].ShortAlertSound == shortAlertSound && cacheSignalVisualizer2[idx].EnableDiscordNotifications == enableDiscordNotifications && cacheSignalVisualizer2[idx].SendChartScreenshot == sendChartScreenshot && cacheSignalVisualizer2[idx].DiscordWebhookUrl == discordWebhookUrl && cacheSignalVisualizer2[idx].DiscordUsername == discordUsername && cacheSignalVisualizer2[idx].ValidTradingHoursOnly == validTradingHoursOnly && cacheSignalVisualizer2[idx].StartTime == startTime && cacheSignalVisualizer2[idx].EndTime == endTime && cacheSignalVisualizer2[idx].LookbackPeriod == lookbackPeriod && cacheSignalVisualizer2[idx].RangeThreshold == rangeThreshold && cacheSignalVisualizer2[idx].EmaFastPeriod == emaFastPeriod && cacheSignalVisualizer2[idx].EmaSlowPeriod == emaSlowPeriod && cacheSignalVisualizer2[idx].Preset == preset && cacheSignalVisualizer2[idx].EnableDetailedLogging == enableDetailedLogging && cacheSignalVisualizer2[idx].TriggerTest == triggerTest && cacheSignalVisualizer2[idx].EqualsInput(input))
						return cacheSignalVisualizer2[idx];
			return CacheIndicator<SignalVisualizer2>(new SignalVisualizer2(){ ShowLongSignals = showLongSignals, ShowShortSignals = showShortSignals, LongSignalColor = longSignalColor, ShortSignalColor = shortSignalColor, SignalSize = signalSize, PlaySoundAlerts = playSoundAlerts, LongAlertSound = longAlertSound, ShortAlertSound = shortAlertSound, EnableDiscordNotifications = enableDiscordNotifications, SendChartScreenshot = sendChartScreenshot, DiscordWebhookUrl = discordWebhookUrl, DiscordUsername = discordUsername, ValidTradingHoursOnly = validTradingHoursOnly, StartTime = startTime, EndTime = endTime, LookbackPeriod = lookbackPeriod, RangeThreshold = rangeThreshold, EmaFastPeriod = emaFastPeriod, EmaSlowPeriod = emaSlowPeriod, Preset = preset, EnableDetailedLogging = enableDetailedLogging, TriggerTest = triggerTest }, input, ref cacheSignalVisualizer2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SignalVisualizer2 SignalVisualizer2(bool showLongSignals, bool showShortSignals, Brush longSignalColor, Brush shortSignalColor, int signalSize, bool playSoundAlerts, string longAlertSound, string shortAlertSound, bool enableDiscordNotifications, bool sendChartScreenshot, string discordWebhookUrl, string discordUsername, bool validTradingHoursOnly, TimeSpan startTime, TimeSpan endTime, int lookbackPeriod, double rangeThreshold, int emaFastPeriod, int emaSlowPeriod, StrategyPreset preset, bool enableDetailedLogging, bool triggerTest)
		{
			return indicator.SignalVisualizer2(Input, showLongSignals, showShortSignals, longSignalColor, shortSignalColor, signalSize, playSoundAlerts, longAlertSound, shortAlertSound, enableDiscordNotifications, sendChartScreenshot, discordWebhookUrl, discordUsername, validTradingHoursOnly, startTime, endTime, lookbackPeriod, rangeThreshold, emaFastPeriod, emaSlowPeriod, preset, enableDetailedLogging, triggerTest);
		}

		public Indicators.SignalVisualizer2 SignalVisualizer2(ISeries<double> input , bool showLongSignals, bool showShortSignals, Brush longSignalColor, Brush shortSignalColor, int signalSize, bool playSoundAlerts, string longAlertSound, string shortAlertSound, bool enableDiscordNotifications, bool sendChartScreenshot, string discordWebhookUrl, string discordUsername, bool validTradingHoursOnly, TimeSpan startTime, TimeSpan endTime, int lookbackPeriod, double rangeThreshold, int emaFastPeriod, int emaSlowPeriod, StrategyPreset preset, bool enableDetailedLogging, bool triggerTest)
		{
			return indicator.SignalVisualizer2(input, showLongSignals, showShortSignals, longSignalColor, shortSignalColor, signalSize, playSoundAlerts, longAlertSound, shortAlertSound, enableDiscordNotifications, sendChartScreenshot, discordWebhookUrl, discordUsername, validTradingHoursOnly, startTime, endTime, lookbackPeriod, rangeThreshold, emaFastPeriod, emaSlowPeriod, preset, enableDetailedLogging, triggerTest);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SignalVisualizer2 SignalVisualizer2(bool showLongSignals, bool showShortSignals, Brush longSignalColor, Brush shortSignalColor, int signalSize, bool playSoundAlerts, string longAlertSound, string shortAlertSound, bool enableDiscordNotifications, bool sendChartScreenshot, string discordWebhookUrl, string discordUsername, bool validTradingHoursOnly, TimeSpan startTime, TimeSpan endTime, int lookbackPeriod, double rangeThreshold, int emaFastPeriod, int emaSlowPeriod, StrategyPreset preset, bool enableDetailedLogging, bool triggerTest)
		{
			return indicator.SignalVisualizer2(Input, showLongSignals, showShortSignals, longSignalColor, shortSignalColor, signalSize, playSoundAlerts, longAlertSound, shortAlertSound, enableDiscordNotifications, sendChartScreenshot, discordWebhookUrl, discordUsername, validTradingHoursOnly, startTime, endTime, lookbackPeriod, rangeThreshold, emaFastPeriod, emaSlowPeriod, preset, enableDetailedLogging, triggerTest);
		}

		public Indicators.SignalVisualizer2 SignalVisualizer2(ISeries<double> input , bool showLongSignals, bool showShortSignals, Brush longSignalColor, Brush shortSignalColor, int signalSize, bool playSoundAlerts, string longAlertSound, string shortAlertSound, bool enableDiscordNotifications, bool sendChartScreenshot, string discordWebhookUrl, string discordUsername, bool validTradingHoursOnly, TimeSpan startTime, TimeSpan endTime, int lookbackPeriod, double rangeThreshold, int emaFastPeriod, int emaSlowPeriod, StrategyPreset preset, bool enableDetailedLogging, bool triggerTest)
		{
			return indicator.SignalVisualizer2(input, showLongSignals, showShortSignals, longSignalColor, shortSignalColor, signalSize, playSoundAlerts, longAlertSound, shortAlertSound, enableDiscordNotifications, sendChartScreenshot, discordWebhookUrl, discordUsername, validTradingHoursOnly, startTime, endTime, lookbackPeriod, rangeThreshold, emaFastPeriod, emaSlowPeriod, preset, enableDetailedLogging, triggerTest);
		}
	}
}

#endregion
