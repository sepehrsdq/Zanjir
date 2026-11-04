// =========================================================
//  Windscribe Chain — Mini v2 (Glass)
//  شیشهٔ واقعی + حاشیهٔ گرادیانی چرخان + اورب پاور سه‌بعدی
//  WPF + Acrylic | net48 | C#5
// =========================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using IoPath = System.IO.Path;

namespace WindscribeChainMini
{
    public class AppWindow : Window
    {
        // ---------- پالت ----------
        static readonly Color CBase = Color.FromRgb(0x0B, 0x11, 0x20);
        static readonly Color CCyan = Color.FromRgb(0x22, 0xD3, 0xEE);
        static readonly Color CViolet = Color.FromRgb(0x8B, 0x5C, 0xF6);
        static readonly Color CGreen = Color.FromRgb(0x22, 0xC5, 0x5E);
        static readonly Color CGreenLt = Color.FromRgb(0x4A, 0xDE, 0x80);
        static readonly Color CGreenDk = Color.FromRgb(0x15, 0x8A, 0x47);
        static readonly Color CAmber = Color.FromRgb(0xF5, 0x9E, 0x0B);
        static readonly Color CAmberLt = Color.FromRgb(0xFB, 0xBF, 0x24);
        static readonly Color CAmberDk = Color.FromRgb(0xB4, 0x53, 0x06);
        static readonly Color CCyanLt = Color.FromRgb(0x67, 0xE8, 0xF9);
        static readonly Color CCyanDk = Color.FromRgb(0x0E, 0x74, 0x90);
        static readonly Color COffLt = Color.FromRgb(0x3C, 0x47, 0x5C);
        static readonly Color COffDk = Color.FromRgb(0x1F, 0x27, 0x36);
        static readonly Color CMuted = Color.FromRgb(0x8A, 0x97, 0xAD);
        static readonly Color MacRed = Color.FromRgb(0xFF, 0x5F, 0x57);
        static readonly Color MacYellow = Color.FromRgb(0xFE, 0xBC, 0x2E);
        static readonly Color MacGreen = Color.FromRgb(0x28, 0xC8, 0x40);

        static Brush Br(Color c, byte a) { var b = new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B)); if (b.CanFreeze) b.Freeze(); return b; }
        static Brush Br(Color c) { var b = new SolidColorBrush(c); if (b.CanFreeze) b.Freeze(); return b; }
        static Color Darken(Color c, double t) { return Color.FromRgb((byte)(c.R * (1 - t)), (byte)(c.G * (1 - t)), (byte)(c.B * (1 - t))); }

        // ---------- مسیرها ----------
        readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        string Cfg, SbExe, Log, AutoCmd, Curl;
        const string TASK = "WSChainAutoStart";
        const string API = "http://127.0.0.1:9090";

        // ---------- وضعیت ----------
        volatile bool _busy = false;
        int _state = 0;
        readonly Dictionary<string, double> _prev = new Dictionary<string, double>();
        readonly HttpClient _http = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) };
        readonly JavaScriptSerializer _ser = new JavaScriptSerializer();
        bool _auto;

        // ---------- عناصر ----------
        Border RootGlass;
        DropShadowEffect GlassShadow;
        Grid OrbHit;
        Ellipse OrbGlow, OrbCore, BusyDot;
        GradientStop G1, G2, G3;
        TextBlock PowerGlyph, StatusBig, StatusSub, TotalsTxt, FooterTxt;
        StackPanel RowsHost;
        Canvas BusyRing;
        Border AutoDot, LiveChip;
        Ellipse LiveDot;
        Button BtnTest, BtnIP;
        readonly DispatcherTimer _tm = new DispatcherTimer();

        // ================================================================
        public AppWindow()
        {
            Cfg = IoPath.Combine(BaseDir, "config.json");
            SbExe = IoPath.Combine(BaseDir, @"bin\sing-box-1.14.4-windows-amd64\sing-box.exe");
            Log = IoPath.Combine(IoPath.GetTempPath(), "wschain.log");
            AutoCmd = IoPath.Combine(BaseDir, "StartChainAuto.cmd");
            Curl = IoPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "curl.exe");

            Width = 362; Height = 508;
            Title = "Zanjir — sepehrsdq";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new FontFamily("Segoe UI");
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            BuildUi();
            SourceInitialized += delegate { EnableBlur(new WindowInteropHelper(this).Handle); };

            _tm.Interval = TimeSpan.FromMilliseconds(1200);
            _tm.Tick += PollTick;
            _tm.Start();

            Loaded += delegate { EnterAnim(); InitAuto(); ApplyStateVisual(); };
            MouseLeftButtonDown += delegate { try { DragMove(); } catch { } };
        }

        // ================================================================
        //  شیشهٔ اکریلیک (تینت کم‌رنگ = بلور واقعی دیده می‌شود)
        // ================================================================
        [StructLayout(LayoutKind.Sequential)]
        struct AccentPolicy { public int State, Flags; public uint Color; public int Anim; }
        [StructLayout(LayoutKind.Sequential)]
        struct WCAdata { public int Attr; public IntPtr Data; public int Size; }
        [DllImport("user32.dll")]
        static extern int SetWindowCompositionAttribute(IntPtr h, ref WCAdata d);

        void EnableBlur(IntPtr h)
        {
            var acc = new AccentPolicy { State = 4, Flags = 0, Color = 0x6E241409, Anim = 0 }; // AABBGGRR — تینت ۴۳٪
            int size = Marshal.SizeOf(acc);
            IntPtr p = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(acc, p, false);
            var d = new WCAdata { Attr = 19, Data = p, Size = size };
            try { SetWindowCompositionAttribute(h, ref d); } finally { Marshal.FreeHGlobal(p); }
        }

        // ================================================================
        //  رابط
        // ================================================================
        void BuildUi()
        {
            var root = new Grid { Margin = new Thickness(10) };

            // حاشیهٔ گرادیانی چرخان
            var rim = new LinearGradientBrush();
            rim.StartPoint = new Point(0, 0); rim.EndPoint = new Point(1, 1);
            rim.GradientStops.Add(new GradientStop(Color.FromArgb(0x90, 0x22, 0xD3, 0xEE), 0));
            rim.GradientStops.Add(new GradientStop(Color.FromArgb(0x90, 0x8B, 0x5C, 0xF6), 0.35));
            rim.GradientStops.Add(new GradientStop(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF), 0.65));
            rim.GradientStops.Add(new GradientStop(Color.FromArgb(0x90, 0x22, 0xC5, 0x5E), 1));
            var rimRot = new RotateTransform(0, 0.5, 0.5);
            rim.RelativeTransform = rimRot;
            var ra = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(16))) { RepeatBehavior = RepeatBehavior.Forever };
            rimRot.BeginAnimation(RotateTransform.AngleProperty, ra);

            GlassShadow = new DropShadowEffect { BlurRadius = 34, ShadowDepth = 0, Opacity = 0.65, Color = Colors.Black };

            RootGlass = new Border
            {
                CornerRadius = new CornerRadius(18),
                Background = new LinearGradientBrush(
                    Color.FromArgb(0x8E, 0x0B, 0x11, 0x20),
                    Color.FromArgb(0x66, 0x0B, 0x11, 0x20),
                    new Point(0, 0), new Point(0, 1)),
                BorderBrush = rim,
                BorderThickness = new Thickness(1.3),
                Effect = GlassShadow
            };
            RootGlass.Child = BuildContent();
            root.Children.Add(RootGlass);
            Content = root;
        }

        Grid BuildContent()
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(52) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(158) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(84) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // ---------- حباب‌های نوری (داخل کلیپِ گوشه‌گرد) ----------
            var blobHost = new Canvas { IsHitTestVisible = false };
            blobHost.Clip = new RectangleGeometry(new Rect(0, 0, 342, 488), 18, 18);
            AddBlob(blobHost, 280, CCyan, 0.13, -80, 40, 11, 9);
            AddBlob(blobHost, 320, CViolet, 0.13, 150, 220, 13, 10);
            AddBlob(blobHost, 230, CGreen, 0.10, 30, 350, 9, 12);
            Grid.SetRowSpan(blobHost, 7);
            g.Children.Add(blobHost);

            // هایلایت شیشه‌ای بالای پنجره
            var sheen = new Border
            {
                Height = 95, VerticalAlignment = VerticalAlignment.Top,
                CornerRadius = new CornerRadius(17, 17, 0, 0), IsHitTestVisible = false,
                Background = new LinearGradientBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF), Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), new Point(0, 0), new Point(0, 1))
            };
            Grid.SetRowSpan(sheen, 2);
            g.Children.Add(sheen);

            // ---------- نوار مک (دقیقاً به سبک اپل) ----------
            var top = new Grid { Margin = new Thickness(15, 12, 15, 0) };
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dots = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, FlowDirection = FlowDirection.LeftToRight };
            dots.Children.Add(MacDot(MacRed, "\uE8BB", delegate { Close(); }));
            dots.Children.Add(MacDot(MacYellow, "\uE921", delegate { WindowState = WindowState.Minimized; }));
            dots.Children.Add(MacDot(MacGreen, "\uE922", delegate { ZoomToggle(); }));
            dots.MouseEnter += delegate
            {
                foreach (UIElement ch in dots.Children) ((TextBlock)((FrameworkElement)ch).Tag).Visibility = Visibility.Visible;
            };
            dots.MouseLeave += delegate
            {
                foreach (UIElement ch in dots.Children) ((TextBlock)((FrameworkElement)ch).Tag).Visibility = Visibility.Collapsed;
            };
            Grid.SetColumn(dots, 0);
            top.Children.Add(dots);

            LiveChip = new Border
            {
                Background = Br(CGreen, 0x1A), BorderBrush = Br(CGreen, 0x46),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9),
                Padding = new Thickness(9, 3, 9, 3), VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed
            };
            var live = new StackPanel { Orientation = Orientation.Horizontal };
            LiveDot = new Ellipse { Width = 7, Height = 7, Fill = Br(CGreenLt), Margin = new Thickness(0, 0, 6, 0) };
            var ld = new DoubleAnimation(0.25, 1, new Duration(TimeSpan.FromSeconds(0.9))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            LiveDot.BeginAnimation(Ellipse.OpacityProperty, ld);
            live.Children.Add(LiveDot);
            live.Children.Add(new TextBlock { Text = "LIVE", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Br(CGreenLt), FontFamily = new FontFamily("Consolas") });
            LiveChip.Child = live;
            Grid.SetColumn(LiveChip, 1);
            top.Children.Add(LiveChip);

            // لوگوی مرکزی
            var logo = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
            var zj = new TextBlock
            {
                Text = "Zanjir",
                FontFamily = new FontFamily("Segoe UI Light"),
                FontSize = 17,
                Foreground = new LinearGradientBrush(Colors.White, Color.FromRgb(0x93, 0xA6, 0xC4), new Point(0, 0), new Point(0, 1)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 0, Opacity = 0.35, Color = Colors.Black }
            };
            var sdq = new TextBlock
            {
                Text = "s\u2009e\u2009p\u2009e\u2009h\u2009r\u2009s\u2009d\u2009q",
                FontSize = 8, Foreground = Br(CMuted, 0xE0),
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0)
            };
            logo.Children.Add(zj);
            logo.Children.Add(sdq);
            Grid.SetColumnSpan(logo, 2);
            top.Children.Add(logo);

            Grid.SetRow(top, 0);
            g.Children.Add(top);

            // ---------- اورب پاور ----------
            OrbHit = new Grid { Width = 128, Height = 128, HorizontalAlignment = HorizontalAlignment.Center, Cursor = Cursors.Hand };

            OrbGlow = new Ellipse { Width = 124, Height = 124, Fill = Br(COffDk), Opacity = 0.35, Effect = new BlurEffect { Radius = 26 } };
            OrbHit.Children.Add(OrbGlow);

            var dashRing = new Ellipse
            {
                Width = 118, Height = 118,
                Stroke = Br(Colors.White, 0x33), StrokeThickness = 1.3,
                StrokeDashArray = new DoubleCollection { 2.2, 7 },
                StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
            };
            var dashRot = new RotateTransform(0, 59, 59);
            dashRing.RenderTransform = dashRot;
            var da = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(22))) { RepeatBehavior = RepeatBehavior.Forever };
            dashRot.BeginAnimation(RotateTransform.AngleProperty, da);
            OrbHit.Children.Add(dashRing);

            G1 = new GradientStop(COffLt, 0);
            G2 = new GradientStop(COffLt, 0.55);
            G3 = new GradientStop(COffDk, 1);
            OrbCore = new Ellipse
            {
                Width = 92, Height = 92,
                Stroke = Br(Colors.White, 0x2A), StrokeThickness = 0.8
            };
            var og = new LinearGradientBrush { StartPoint = new Point(0.25, 0), EndPoint = new Point(0.75, 1) };
            og.GradientStops.Add(G1); og.GradientStops.Add(G2); og.GradientStops.Add(G3);
            OrbCore.Fill = og;
            OrbCore.Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 3, Opacity = 0.5, Color = Colors.Black };
            OrbHit.Children.Add(OrbCore);

            var spec = new Ellipse
            {
                Width = 52, Height = 24, Fill = Br(Colors.White, 0x3A),
                Effect = new BlurEffect { Radius = 9 }, IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 12, 0, 0)
            };
            OrbHit.Children.Add(spec);

            PowerGlyph = new TextBlock
            {
                Text = "\uE7E8",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 36,
                Foreground = Br(Colors.White, 0xE8),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 0, Opacity = 0.55, Color = Colors.White }
            };
            OrbHit.Children.Add(PowerGlyph);

            BusyRing = new Canvas { Width = 128, Height = 128, Visibility = Visibility.Collapsed, IsHitTestVisible = false };
            BusyDot = new Ellipse { Width = 7, Height = 7, Fill = Br(CCyanLt), Effect = new BlurEffect { Radius = 3 } };
            Canvas.SetLeft(BusyDot, 60); Canvas.SetTop(BusyDot, 1);
            var brt = new RotateTransform(0, 64, 64);
            BusyRing.RenderTransform = brt;
            var ba = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(0.85))) { RepeatBehavior = RepeatBehavior.Forever };
            brt.BeginAnimation(RotateTransform.AngleProperty, ba);
            BusyRing.Children.Add(BusyDot);
            OrbHit.Children.Add(BusyRing);

            var orbScale = new ScaleTransform(1, 1);
            OrbHit.RenderTransform = orbScale;
            OrbHit.RenderTransformOrigin = new Point(0.5, 0.5);
            OrbHit.MouseEnter += delegate { SpringTo(orbScale, 1.06); };
            OrbHit.MouseLeave += delegate { SpringTo(orbScale, 1.0); };
            OrbHit.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                SpringTo(orbScale, 0.93);
                ToggleMain();
            };
            g.Children.Add(OrbHit);
            Grid.SetRow(OrbHit, 1);

            // ---------- وضعیت ----------
            var st = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 6, 0, 0) };
            StatusBig = new TextBlock { Text = "خاموش", FontSize = 21, FontWeight = FontWeights.Bold, Foreground = Br(Colors.White, 0xEA), HorizontalAlignment = HorizontalAlignment.Center };
            StatusSub = new TextBlock { Text = "برای اتصال دکمهٔ بالا را بزن", FontSize = 12, Foreground = Br(CMuted), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
            StatusBig.Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 0, Opacity = 0.3, Color = Colors.Black };
            st.Children.Add(StatusBig);
            st.Children.Add(StatusSub);
            Grid.SetRow(st, 2);
            g.Children.Add(st);

            // ---------- آمار کل ----------
            TotalsTxt = new TextBlock
            {
                Text = "—", FontSize = 11.5, Foreground = Br(CCyan, 0xDC),
                FontFamily = new FontFamily("Consolas"),
                FlowDirection = FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(TotalsTxt, 3);
            g.Children.Add(TotalsTxt);

            // ---------- سطرها ----------
            RowsHost = new StackPanel { Margin = new Thickness(20, 4, 20, 0) };
            Grid.SetRow(RowsHost, 4);
            g.Children.Add(RowsHost);

            // ---------- دکمه‌های کوچک ----------
            var row = new Grid { Margin = new Thickness(20, 8, 20, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition());
            BtnTest = Pill("تست", CCyan);
            BtnTest.Click += delegate { Fire(TestChain, "در حال تست مسیر..."); };
            Grid.SetColumn(BtnTest, 0);
            row.Children.Add(BtnTest);
            BtnIP = Pill("بررسی IP", CViolet);
            BtnIP.Click += delegate { Fire(CheckIp, "در حال بررسی IP..."); };
            Grid.SetColumn(BtnIP, 1);
            row.Children.Add(BtnIP);
            var BtnCfg = Pill("کانفیگ", CGreen);
            BtnCfg.Click += delegate { new ConfigWindow(new Action<string>(Say)).ShowDialog(); };
            Grid.SetColumn(BtnCfg, 2);
            row.Children.Add(BtnCfg);

            var autoWrap = new Border
            {
                Background = Br(Colors.White, 0x07),
                CornerRadius = new CornerRadius(11),
                Margin = new Thickness(5, 0, 5, 0),
                Cursor = Cursors.Hand,
                BorderBrush = Br(Colors.White, 0x14),
                BorderThickness = new Thickness(1)
            };
            var aw = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            AutoDot = new Border { Width = 9, Height = 9, CornerRadius = new CornerRadius(4.5), Background = Br(COffDk), Margin = new Thickness(0, 0, 7, 0) };
            aw.Children.Add(AutoDot);
            aw.Children.Add(new TextBlock { Text = "خودکار", FontSize = 12, Foreground = Br(Colors.White, 0xE4) });
            autoWrap.Child = aw;
            var autoScale = new ScaleTransform(1, 1);
            autoWrap.RenderTransform = autoScale; autoWrap.RenderTransformOrigin = new Point(0.5, 0.5);
            autoWrap.MouseEnter += delegate { SpringTo(autoScale, 1.05); };
            autoWrap.MouseLeave += delegate { SpringTo(autoScale, 1.0); };
            autoWrap.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs ev)
            {
                ev.Handled = true;
                ToggleAuto();
            };
            Grid.SetColumn(autoWrap, 3);
            row.Children.Add(autoWrap);
            Grid.SetRow(row, 5);
            g.Children.Add(row);

            // ---------- فوتر ----------
            FooterTxt = new TextBlock
            {
                Text = "آماده", FontSize = 11, Foreground = Br(CMuted, 0xCC),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(16, 0, 16, 0)
            };
            Grid.SetRow(FooterTxt, 6);
            g.Children.Add(FooterTxt);

            return g;
        }

        void AddBlob(Canvas host, double size, Color col, double op, double x, double y, int dx, int dy)
        {
            var e = new Ellipse { Width = size, Height = size, Fill = Br(col), Opacity = op, Effect = new BlurEffect { Radius = 55 }, IsHitTestVisible = false };
            Canvas.SetLeft(e, x); Canvas.SetTop(e, y);
            var tt = new TranslateTransform(0, 0); e.RenderTransform = tt;
            var ax = new DoubleAnimation(-16, 16, new Duration(TimeSpan.FromSeconds(dx))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() };
            var ay = new DoubleAnimation(-13, 13, new Duration(TimeSpan.FromSeconds(dy))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() };
            tt.BeginAnimation(TranslateTransform.XProperty, ax);
            tt.BeginAnimation(TranslateTransform.YProperty, ay);
            host.Children.Add(e);
        }

        UIElement MacDot(Color c, string mdlGlyph, Action click)
        {
            var wrap = new Grid
            {
                Width = 12, Height = 12, Margin = new Thickness(4, 0, 4, 0),
                Cursor = Cursors.Hand, Background = Brushes.Transparent
            };
            var e = new Ellipse { Fill = Br(c), Stroke = Br(Colors.Black, 0x2A), StrokeThickness = 0.75 };
            wrap.Children.Add(e);
            var g = new TextBlock
            {
                Text = mdlGlyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 6.5,
                Foreground = Br(Colors.Black, 0x80),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -0.5, 0, 0),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            wrap.Children.Add(g);
            wrap.Tag = g;
            wrap.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs ev)
            {
                ev.Handled = true;
                if (click != null) { click(); }
            };
            return wrap;
        }

        Rect? _savedRect = null;
        void ZoomToggle()
        {
            if (_savedRect == null)
            {
                _savedRect = new Rect(Left, Top, Width, Height);
                var wa = SystemParameters.WorkArea;
                Left = wa.Left; Top = wa.Top; Width = wa.Width; Height = wa.Height;
                Say("بزرگ‌نمایی — دوباره بزن برای حالت عادی");
            }
            else
            {
                Left = _savedRect.Value.Left; Top = _savedRect.Value.Top;
                Width = _savedRect.Value.Width; Height = _savedRect.Value.Height;
                _savedRect = null;
                Say("حالت عادی");
            }
        }

        Button Pill(string txt, Color accent)
        {
            var b = new Button
            {
                Content = txt, Height = 30, Margin = new Thickness(5, 0, 5, 0),
                Foreground = Br(Colors.White, 0xEC), FontSize = 12,
                Background = Br(Colors.White, 0x08),
                BorderBrush = Br(accent, 0x55), BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            b.Template = FlatTemplate();
            var st = new ScaleTransform(1, 1); b.RenderTransform = st; b.RenderTransformOrigin = new Point(0.5, 0.5);
            var glow = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 0, Opacity = 0.5, Color = accent };
            b.MouseEnter += delegate { SpringTo(st, 1.06); b.Effect = glow; };
            b.MouseLeave += delegate { SpringTo(st, 1.0); b.Effect = null; };
            return b;
        }

        ControlTemplate FlatTemplate()
        {
            var t = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(11));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Border.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Border.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Border.BorderThicknessProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            cp.SetValue(TextElement.ForegroundProperty, new TemplateBindingExtension(TextElement.ForegroundProperty));
            border.AppendChild(cp);
            t.VisualTree = border;
            return t;
        }

        // ================================================================
        //  انیمیشن
        // ================================================================
        static void Anim(IAnimatable el, DependencyProperty p, double from, double to, int ms)
        {
            var a = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut } };
            el.BeginAnimation(p, a);
        }
        static void SpringTo(ScaleTransform st, double to)
        {
            var e = new BackEase { Amplitude = 0.55, EasingMode = EasingMode.EaseOut };
            var a = new DoubleAnimation(st.ScaleX, to, new Duration(TimeSpan.FromMilliseconds(260))) { EasingFunction = e };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            var b = new DoubleAnimation(st.ScaleY, to, new Duration(TimeSpan.FromMilliseconds(260))) { EasingFunction = e };
            st.BeginAnimation(ScaleTransform.ScaleYProperty, b);
        }

        void EnterAnim()
        {
            var tg = new TransformGroup();
            var tt = new TranslateTransform(0, 24);
            var sc = new ScaleTransform(0.95, 0.95);
            tg.Children.Add(tt); tg.Children.Add(sc);
            RootGlass.RenderTransform = tg;
            RootGlass.RenderTransformOrigin = new Point(0.5, 0.5);
            RootGlass.Opacity = 0;
            Anim(RootGlass, UIElement.OpacityProperty, 0, 1, 500);
            var a = new DoubleAnimation(24, 0, new Duration(TimeSpan.FromMilliseconds(520))) { EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut } };
            tt.BeginAnimation(TranslateTransform.YProperty, a);
            var b2 = new DoubleAnimation(0.95, 1, new Duration(TimeSpan.FromMilliseconds(520))) { EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut } };
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, b2);
            var b3 = new DoubleAnimation(0.95, 1, new Duration(TimeSpan.FromMilliseconds(520))) { EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut } };
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, b3);
        }

        void PulseGlow(Color c)
        {
            OrbGlow.Fill = Br(c);
            var a = new DoubleAnimation(0.22, 0.62, new Duration(TimeSpan.FromSeconds(1.25)))
            { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() };
            OrbGlow.BeginAnimation(UIElement.OpacityProperty, a);
        }
        void StopGlow() { OrbGlow.BeginAnimation(UIElement.OpacityProperty, null); OrbGlow.Opacity = 0.35; }

        void ApplyStateVisual()
        {
            Color g1, g2, g3, glow, dot;
            if (_busy) { g1 = CCyanLt; g2 = CCyan; g3 = CCyanDk; glow = CCyan; dot = CCyanLt; }
            else if (_state == 1) { g1 = CGreenLt; g2 = CGreen; g3 = Darken(CGreenDk, 0.25); glow = CGreen; dot = CGreenLt; }
            else if (_state == 2) { g1 = CAmberLt; g2 = CAmber; g3 = Darken(CAmber, 0.45); glow = CAmber; dot = CAmberLt; }
            else { g1 = COffLt; g2 = COffLt; g3 = COffDk; glow = COffDk; dot = COffDk; }

            int ms = 420;
            G1.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(g1, new Duration(TimeSpan.FromMilliseconds(ms))));
            G2.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(g2, new Duration(TimeSpan.FromMilliseconds(ms))));
            G3.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(g3, new Duration(TimeSpan.FromMilliseconds(ms))));

            if (_state == 1 && !_busy) PulseGlow(glow); else if (_busy) { OrbGlow.Fill = Br(glow); } else StopGlow();

            GlassShadow.BeginAnimation(DropShadowEffect.ColorProperty, new ColorAnimation(
                _state == 1 ? Color.FromRgb(0x18, 0x8A, 0x4C) : (_state == 2 ? Color.FromRgb(0xB4, 0x78, 0x08) : Colors.Black),
                new Duration(TimeSpan.FromMilliseconds(600))));

            LiveChip.Visibility = (_state == 1 && !_busy) ? Visibility.Visible : Visibility.Collapsed;

            if (_busy)
            {
                StatusSub.Text = "در حال اجرای فرمان...";
                StatusSub.Foreground = Br(CCyanLt);
            }
            else if (_state == 1)
            {
                StatusBig.Text = "روشن";
                StatusBig.Foreground = new LinearGradientBrush(CGreenLt, CCyan, new Point(0, 0), new Point(1, 0));
                StatusSub.Text = "زنجیره فعال — ترافیک از سرور شما";
                StatusSub.Foreground = Br(CMuted);
            }
            else if (_state == 2)
            {
                StatusBig.Text = "نیمه‌بالا";
                StatusBig.Foreground = new LinearGradientBrush(CAmberLt, CAmber, new Point(0, 0), new Point(1, 0));
                StatusSub.Text = "تانل آماده نیست — چند لحظه صبر کن";
                StatusSub.Foreground = Br(CMuted);
            }
            else
            {
                StatusBig.Text = "خاموش";
                StatusBig.Foreground = Br(Colors.White, 0xEA);
                StatusSub.Text = "برای اتصال دکمهٔ بالا را بزن";
                StatusSub.Foreground = Br(CMuted);
            }
        }

        void SetBusy(bool b)
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(delegate { SetBusy(b); }); return; }
            _busy = b;
            BusyRing.Visibility = b ? Visibility.Visible : Visibility.Collapsed;
            BtnTest.IsEnabled = !b;
            BtnIP.IsEnabled = !b;
            ApplyStateVisual();
        }

        void Say(string t)
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(delegate { Say(t); }); return; }
            FooterTxt.Text = t.Length > 80 ? t.Substring(0, 80) : t;
            Anim(FooterTxt, UIElement.OpacityProperty, 0.35, 1, 350);
        }

        // ================================================================
        //  منطق
        // ================================================================
        void ToggleMain()
        {
            if (_busy) return;
            if (_state == 0) Fire(StartChain, "در حال روشن کردن زنجیره...");
            else Fire(StopChain, "در حال خاموش کردن...");
        }

        void Fire(Action a, string msg)
        {
            if (_busy) return;
            SetBusy(true);
            Say(msg);
            Task.Run(delegate
            {
                try { a(); }
                catch (Exception ex) { Say("[×] " + ex.Message); }
                finally { SetBusy(false); RefreshStateNow(); }
            });
        }

        bool ProcAlive() { return Process.GetProcessesByName("sing-box").Length > 0; }
        bool TunUp()
        {
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    if (ni.Name == "wschain" && ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up) return true;
            }
            catch { }
            return false;
        }

        void StartChain()
        {
            string ifname = "Wi-Fi";
            try
            {
                string raw = File.ReadAllText(Cfg);
                raw = Regex.Replace(raw, "\"default_interface\"\\s*:\\s*\"[^\"]*\"", "\"default_interface\": \"" + ifname + "\"");
                File.WriteAllText(Cfg, raw, new UTF8Encoding(false));
            }
            catch (Exception ex) { Say("[×] خطا در کانفیگ: " + ex.Message); return; }

            Say("... زنجیره راه‌اندازی می‌شود");

            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    if (ni.Name == "throne-tun" && ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    {
                        foreach (var p in Process.GetProcessesByName("ThroneCore")) { try { p.Kill(); } catch { } }
                        Thread.Sleep(2000);
                    }
            }
            catch { }

            bool ok = false;
            for (int attempt = 1; attempt <= 2 && !ok; attempt++)
            {
                foreach (var p in Process.GetProcessesByName("sing-box")) { try { p.Kill(); } catch { } }
                Thread.Sleep(3000);
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"\"" + SbExe + "\" run -c \"" + Cfg + "\" > \"" + Log + "\" 2>&1\"",
                    WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true, CreateNoWindow = true
                };
                Process.Start(psi);
                for (int i = 0; i < 40; i++)
                {
                    Thread.Sleep(500);
                    if (ProcAlive() && TunUp()) { ok = true; break; }
                    if (!ProcAlive()) break;
                }
                if (!ok && attempt == 1) Say("... اداپتر مانده بود — تلاش دوباره");
            }
            if (ok) Say("[✓] روشن شد — حالا ویندسکرایب را با Stealth وصل کن");
            else Say("[×] بالا نیامد — " + TailLog(1));
        }

        string TailLog(int n)
        {
            try
            {
                var lines = File.ReadAllLines(Log);
                if (lines.Length == 0) return "(لاگ خالی)";
                var last = lines[lines.Length - 1].Trim();
                last = Regex.Replace(last, "\\x1B\\[[0-9;]*m", "");
                return last.Length > 60 ? last.Substring(last.Length - 60) : last;
            }
            catch { return "(لاگ موجود نیست)"; }
        }

        void StopChain()
        {
            foreach (var p in Process.GetProcessesByName("sing-box")) { try { p.Kill(); } catch { } }
            Say("[✓] زنجیره خاموش شد");
        }

        string RunCurl(string args, int timeoutSec)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Curl, Arguments = args,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    string o = p.StandardOutput.ReadToEnd();
                    if (!p.WaitForExit(timeoutSec * 1000)) { try { p.Kill(); } catch { } }
                    return (o ?? "").Trim();
                }
            }
            catch (Exception ex) { return "ERR:" + ex.Message; }
        }

        void TestChain()
        {
            string r = RunCurl("-s -m 20 -x socks5h://127.0.0.1:20808 -o NUL -w \"%{http_code}|%{time_total}\" https://api.windscribe.com/", 25);
            string[] p = r.Split('|');
            if (p.Length == 2 && Regex.IsMatch(p[0], "^\\d{3}$"))
                Say("[✓] مسیر سالم — کد " + p[0] + " در " + p[1] + " ثانیه");
            else
                Say("[×] پاسخی نیامد — زنجیره روشن است؟");
        }

        void CheckIp()
        {
            string d = RunCurl("-s -m 12 https://api.ipify.org", 15);
            string px = RunCurl("-s -m 15 -x socks5h://127.0.0.1:20808 https://api.ipify.org", 18);
            Say("IP سیستم: " + (d.StartsWith("ERR") ? "ناموفق" : d) + " | از سرور شما: " + (px.StartsWith("ERR") ? "ناموفق" : px));
        }

        void ToggleAuto()
        {
            _auto = !_auto;
            AutoDot.Background = Br(_auto ? CGreen : COffDk);
            try
            {
                if (_auto)
                {
                    Run("schtasks.exe", "/Create /F /TN " + TASK + " /SC ONLOGON /RL HIGHEST /TR \"" + AutoCmd + "\"");
                    Say("[✓] اجرای خودکار با ورود ویندوز فعال شد");
                }
                else
                {
                    Run("schtasks.exe", "/Delete /F /TN " + TASK);
                    Say("اجرای خودکار حذف شد");
                }
            }
            catch (Exception ex) { Say("[×] " + ex.Message); }
        }

        void InitAuto()
        {
            try
            {
                var psi = new ProcessStartInfo("schtasks.exe", "/Query /TN " + TASK)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using (var p = Process.Start(psi)) { p.WaitForExit(5000); _auto = p.ExitCode == 0; }
            }
            catch { }
            AutoDot.Background = Br(_auto ? CGreen : COffDk);
        }

        static void Run(string file, string args)
        {
            var psi = new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = true };
            using (var p = Process.Start(psi)) p.WaitForExit(8000);
        }

        // ================================================================
        //  نظرسنجی
        // ================================================================
        void RefreshStateNow()
        {
            int s = (ProcAlive() && TunUp()) ? 1 : (ProcAlive() ? 2 : 0);
            if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(delegate { RefreshStateNow(); }); return; }
            if (s != _state) { _state = s; ApplyStateVisual(); }
        }

        async void PollTick(object s, EventArgs e)
        {
            if (_busy) return;
            bool proc = ProcAlive();
            RefreshStateNow();
            if (!proc)
            {
                Dispatcher.Invoke(delegate
                {
                    TotalsTxt.Text = "—";
                    if (RowsHost.Children.Count > 0) RowsHost.Children.Clear();
                });
                return;
            }
            try
            {
                var resp = await _http.GetAsync(API + "/connections");
                string json = await resp.Content.ReadAsStringAsync();
                RenderTraffic(json);
            }
            catch { }
        }

        void RenderTraffic(string json)
        {
            try
            {
                var root = (Dictionary<string, object>)_ser.DeserializeObject(json);
                object[] conns = root.ContainsKey("connections") ? root["connections"] as object[] : new object[0];
                var rows = new List<object[]>();
                double totUp = 0, totDn = 0;
                var now = new Dictionary<string, double>();
                foreach (object oo in conns)
                {
                    var c = oo as Dictionary<string, object>;
                    if (c == null) continue;
                    string id = c.ContainsKey("id") ? c["id"].ToString() : Guid.NewGuid().ToString();
                    double up = c.ContainsKey("upload") ? Convert.ToDouble(c["upload"]) : 0;
                    double dn = c.ContainsKey("download") ? Convert.ToDouble(c["download"]) : 0;
                    totUp += up; totDn += dn;
                    double cur = up + dn;
                    double prev; _prev.TryGetValue(id, out prev);
                    double rate = cur - prev; if (rate < 0) rate = 0;
                    now[id] = cur;

                    string proc = "", dst = ""; bool direct = false;
                    if (c.ContainsKey("metadata"))
                    {
                        var md = c["metadata"] as Dictionary<string, object>;
                        if (md != null)
                        {
                            if (md.ContainsKey("processPath") && md["processPath"] != null)
                                proc = IoPath.GetFileName(md["processPath"].ToString());
                            if (md.ContainsKey("host") && md["host"] != null && md["host"].ToString() != "") dst = md["host"].ToString();
                            else if (md.ContainsKey("destination") && md["destination"] != null) dst = md["destination"].ToString();
                        }
                    }
                    if (c.ContainsKey("chains"))
                    {
                        var ch = c["chains"] as object[];
                        if (ch != null) direct = ch.Cast<object>().Any(x => x.ToString() == "direct");
                    }
                    rows.Add(new object[] { rate, proc, dst, direct });
                }
                _prev.Clear(); foreach (var kv in now) _prev[kv.Key] = kv.Value;

                rows.Sort((a, b) => Convert.ToDouble(b[0]).CompareTo(Convert.ToDouble(a[0])));
                var top = rows.Take(3).ToList();

                Dispatcher.Invoke(delegate
                {
                    TotalsTxt.Text = string.Format("DL {0}  |  UP {1}  |  {2} conns", Fmt(totDn), Fmt(totUp), rows.Count);
                    RowsHost.Children.Clear();
                    foreach (var r in top) RowsHost.Children.Add(MakeRow(r));
                    if (top.Count == 0)
                        RowsHost.Children.Add(new TextBlock { Text = "—", Foreground = Br(CMuted, 0x90), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });
                });
            }
            catch { }
        }

        UIElement MakeRow(object[] r)
        {
            double rate = Convert.ToDouble(r[0]);
            string proc = (string)r[1];
            bool direct = (bool)r[3];
            var b = new Border
            {
                Height = 26, CornerRadius = new CornerRadius(8),
                Background = Br(Colors.White, 0x07),
                Margin = new Thickness(0, 1.5, 0, 1.5)
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // نوار سرعت محو پشت سطر
            var bar = new Border
            {
                Background = new LinearGradientBrush(direct ? CViolet : CCyan, CViolet, new Point(0, 0), new Point(1, 0)),
                Opacity = rate > 0 ? 0.14 : 0.04,
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 20
            };
            var wa = new DoubleAnimation(20, 20 + Math.Min(rate / 40.0, 280), new Duration(TimeSpan.FromMilliseconds(600))) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut } };
            bar.BeginAnimation(FrameworkElement.WidthProperty, wa);
            Grid.SetColumnSpan(bar, 3);
            g.Children.Add(bar);

            var rateT = new TextBlock
            {
                Text = Fmt(rate / 1.2) + "/s", Foreground = Br(Colors.White, 0xE6),
                FontFamily = new FontFamily("Consolas"), FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(9, 0, 0, 0)
            };
            Grid.SetColumn(rateT, 0); g.Children.Add(rateT);

            var badge = new Border
            {
                CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 1, 6, 1),
                Background = direct ? Br(Colors.White, 0x10) : Br(CGreen, 0x26),
                BorderBrush = direct ? Br(Colors.White, 0x18) : Br(CGreen, 0x50),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(4, 0, 0, 0)
            };
            badge.Child = new TextBlock
            {
                Text = direct ? "مستقیم" : "سرور من", FontSize = 9.5,
                Foreground = direct ? Br(CMuted) : Br(CGreenLt)
            };
            Grid.SetColumn(badge, 1); g.Children.Add(badge);

            var procT = new TextBlock
            {
                Text = string.IsNullOrEmpty(proc) ? "—" : proc,
                Foreground = Br(CMuted), FontFamily = new FontFamily("Consolas"), FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0),
                TextTrimming = TextTrimming.CharacterEllipsis, FlowDirection = FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(procT, 2); g.Children.Add(procT);

            b.Child = g;
            return b;
        }

        static string Fmt(double b)
        {
            if (b >= 1024 * 1024) return string.Format("{0:0.0}MB", b / (1024 * 1024));
            if (b >= 1024) return string.Format("{0:0.0}KB", b / 1024);
            return string.Format("{0:0}B", b);
        }
    }

    // =========================================================
    //  پردازش لینک vless و مدیریت کانفیگ‌ها
    // =========================================================
    static class Importer
    {
        static readonly string CfgPath = IoPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        static Dictionary<string, object> Load()
        {
            return (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(File.ReadAllText(CfgPath));
        }

        static void Save(Dictionary<string, object> cfg)
        {
            var sb = new StringBuilder();
            WriteJson(sb, cfg, 0);
            File.WriteAllText(CfgPath, sb.ToString(), new UTF8Encoding(false));
        }

        static string Esc(string s)
        {
            var sb = new StringBuilder();
            foreach (char ch in s)
            {
                if (ch == '"' || ch == '\\') { sb.Append('\\').Append(ch); }
                else if (ch == '\n') { sb.Append("\\n"); }
                else if (ch == '\r') { }
                else { sb.Append(ch); }
            }
            return sb.ToString();
        }

        static void WriteJson(StringBuilder sb, object o, int ind)
        {
            string pad = new string(' ', ind * 2);
            if (o is Dictionary<string, object>)
            {
                var d = (Dictionary<string, object>)o;
                if (d.Count == 0) { sb.Append("{}"); return; }
                sb.Append("{\n");
                bool first = true;
                foreach (var kv in d)
                {
                    if (!first) sb.Append(",\n");
                    first = false;
                    sb.Append(pad).Append("  \"").Append(Esc(kv.Key)).Append("\": ");
                    WriteJson(sb, kv.Value, ind + 1);
                }
                sb.Append('\n').Append(pad).Append('}');
            }
            else if (o is object[])
            {
                var arr = (object[])o;
                if (arr.Length == 0) { sb.Append("[]"); return; }
                bool allPrim = true;
                foreach (var x in arr) if (x is Dictionary<string, object> || x is object[]) { allPrim = false; break; }
                if (allPrim && arr.Length <= 8)
                {
                    sb.Append('[');
                    for (int i = 0; i < arr.Length; i++) { if (i > 0) sb.Append(", "); WriteJson(sb, arr[i], ind); }
                    sb.Append(']');
                }
                else
                {
                    sb.Append("[\n");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (i > 0) sb.Append(",\n");
                        sb.Append(pad).Append("  ");
                        WriteJson(sb, arr[i], ind + 1);
                    }
                    sb.Append('\n').Append(pad).Append(']');
                }
            }
            else if (o is string) { sb.Append('"').Append(Esc((string)o)).Append('"'); }
            else if (o is bool) { sb.Append((bool)o ? "true" : "false"); }
            else if (o == null) { sb.Append("null"); }
            else { sb.Append(Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture)); }
        }

        static Dictionary<string, object> FindSelector(Dictionary<string, object> cfg)
        {
            foreach (var o in (object[])cfg["outbounds"])
            {
                var d = o as Dictionary<string, object>;
                if (d != null && (string)d["type"] == "selector" && (string)d["tag"] == "ws-hub") return d;
            }
            return null;
        }

        static int NextIndex(Dictionary<string, object> cfg)
        {
            int mx = 0;
            foreach (var o in (object[])cfg["outbounds"])
            {
                var d = o as Dictionary<string, object>;
                if (d != null)
                {
                    string t = (string)d["tag"];
                    if (t != null && t.StartsWith("imp-"))
                    {
                        int n;
                        if (int.TryParse(t.Substring(4), out n) && n > mx) mx = n;
                    }
                }
            }
            return mx + 1;
        }

        // خروجی: موفق = نام تگ جدید، ناموفق = متن خطا
        public static string ImportLink(string link)
        {
            link = (link ?? "").Trim().Trim('"');
            if (!link.StartsWith("vless://", StringComparison.OrdinalIgnoreCase)) return "لینک باید با vless:// شروع شود";
            try
            {
                string body = link.Substring(8);
                int hash = body.IndexOf('#');
                body = hash >= 0 ? body.Substring(0, hash) : body;
                int q = body.IndexOf('?');
                var qp = new Dictionary<string, string>();
                if (q >= 0)
                {
                    foreach (string kv in body.Substring(q + 1).Split('&'))
                    {
                        int eq = kv.IndexOf('=');
                        if (eq > 0) qp[kv.Substring(0, eq).ToLower()] = Uri.UnescapeDataString(kv.Substring(eq + 1));
                    }
                    body = body.Substring(0, q);
                }
                int at = body.LastIndexOf('@');
                if (at < 0) return "ساختار لینک نامعتبر است";
                string uuid = body.Substring(0, at);
                string hostport = body.Substring(at + 1);
                int colon = hostport.LastIndexOf(':');
                if (colon < 0) return "پورت در لینک پیدا نشد";
                string host = hostport.Substring(0, colon);
                int port = int.Parse(hostport.Substring(colon + 1));
                string type = qp.ContainsKey("type") ? qp["type"] : "tcp";
                string security = qp.ContainsKey("security") ? qp["security"] : "none";

                var ob = new Dictionary<string, object>();
                var cfg = Load();
                ob["type"] = "vless";
                ob["tag"] = "imp-" + NextIndex(cfg);
                ob["server"] = host;
                ob["server_port"] = port;
                ob["uuid"] = uuid;
                ob["packet_encoding"] = "xudp";
                if (qp.ContainsKey("flow") && qp["flow"] != "none") ob["flow"] = qp["flow"];

                if (security == "reality" || security == "tls")
                {
                    var tls = new Dictionary<string, object>();
                    tls["enabled"] = true;
                    if (qp.ContainsKey("sni")) tls["server_name"] = qp["sni"];
                    var utls = new Dictionary<string, object>();
                    utls["enabled"] = true;
                    utls["fingerprint"] = qp.ContainsKey("fp") ? qp["fp"] : "chrome";
                    tls["utls"] = utls;
                    if (security == "reality")
                    {
                        if (!qp.ContainsKey("pbk")) return "لینک Reality کلید عمومی (pbk) ندارد";
                        var rl = new Dictionary<string, object>();
                        rl["enabled"] = true;
                        rl["public_key"] = qp["pbk"];
                        rl["short_id"] = qp.ContainsKey("sid") ? qp["sid"] : "";
                        tls["reality"] = rl;
                    }
                    ob["tls"] = tls;
                }

                if (type == "ws")
                {
                    var tr = new Dictionary<string, object>();
                    tr["type"] = "ws";
                    tr["path"] = qp.ContainsKey("path") ? qp["path"] : "/";
                    var hd = new Dictionary<string, object>();
                    hd["Host"] = qp.ContainsKey("host") ? qp["host"] : (qp.ContainsKey("sni") ? qp["sni"] : host);
                    tr["headers"] = hd;
                    ob["transport"] = tr;
                }
                else if (type == "grpc")
                {
                    var tr = new Dictionary<string, object>();
                    tr["type"] = "grpc";
                    if (qp.ContainsKey("servicename")) tr["service_name"] = qp["servicename"];
                    ob["transport"] = tr;
                }
                else if (type != "tcp")
                {
                    return "این نوع transport پشتیبانی نمی‌شود: " + type;
                }

                var obs = new List<object>((object[])cfg["outbounds"]);
                var sel = FindSelector(cfg);
                if (sel == null) return "سکشنر ws-hub در کانفیگ پیدا نشد";
                int insertAt = obs.Count;
                for (int i = 0; i < obs.Count; i++)
                {
                    var o = obs[i] as Dictionary<string, object>;
                    if (o != null && (string)o["type"] == "direct") { insertAt = i; break; }
                }
                obs.Insert(insertAt, ob);
                var mem = new List<object>((object[])sel["outbounds"]);
                mem.Add(ob["tag"]);
                sel["outbounds"] = mem.ToArray();
                sel["default"] = ob["tag"];
                cfg["outbounds"] = obs.ToArray();
                Save(cfg);
                return (string)ob["tag"];
            }
            catch (Exception ex)
            {
                return "خطا در پردازش لینک: " + ex.Message;
            }
        }

        public static List<KeyValuePair<string, string>> Members()
        {
            var res = new List<KeyValuePair<string, string>>();
            try
            {
                var cfg = Load();
                var sel = FindSelector(cfg);
                if (sel == null) return res;
                string def = sel.ContainsKey("default") ? (string)sel["default"] : "";
                foreach (var m in (object[])sel["outbounds"])
                    res.Add(new KeyValuePair<string, string>((string)m, (string)m == def ? "active" : ""));
            }
            catch { }
            return res;
        }

        public static bool SetDefault(string tag)
        {
            try
            {
                var cfg = Load();
                var sel = FindSelector(cfg);
                if (sel == null) return false;
                sel["default"] = tag;
                Save(cfg);
                return true;
            }
            catch { return false; }
        }

        public static string LiveSwitch(string tag)
        {
            try
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://127.0.0.1:9090/proxies/ws-hub");
                req.Method = "PUT";
                req.ContentType = "application/json";
                req.Timeout = 2500;
                byte[] bytes = Encoding.UTF8.GetBytes("{\"name\": \"" + tag.Replace("\"", "") + "\"}");
                req.ContentLength = bytes.Length;
                using (System.IO.Stream s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
                using (var r = (System.Net.HttpWebResponse)req.GetResponse()) { }
                return "";
            }
            catch (Exception ex) { return ex.Message; }
        }
    }

    // =========================================================
    //  پنجرهٔ مدیریت کانفیگ‌ها
    // =========================================================
    public class ConfigWindow : Window
    {
        Action<string> _say;
        TextBox LinkBox;
        StackPanel ListHost;

        static Brush Br(Color c, byte a) { var b = new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B)); if (b.CanFreeze) b.Freeze(); return b; }
        static Brush Br(Color c) { var b = new SolidColorBrush(c); if (b.CanFreeze) b.Freeze(); return b; }
        static readonly Color CGreen = Color.FromRgb(0x22, 0xC5, 0x5E);
        static readonly Color CGreenLt = Color.FromRgb(0x4A, 0xDE, 0x80);
        static readonly Color CCyan = Color.FromRgb(0x22, 0xD3, 0xEE);
        static readonly Color CMuted = Color.FromRgb(0x8A, 0x97, 0xAD);

        public ConfigWindow(Action<string> say)
        {
            _say = say;
            Width = 372; Height = 480;
            Title = "کانفیگ‌ها";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new FontFamily("Segoe UI");
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            Build();
            Loaded += delegate { LoadList(); FadeIn(); };
            MouseLeftButtonDown += delegate { try { DragMove(); } catch { } };
        }

        void FadeIn()
        {
            var root = (Grid)Content;
            var glass = (Border)root.Children[0];
            glass.Opacity = 0;
            var a = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
            glass.BeginAnimation(UIElement.OpacityProperty, a);
        }

        void Build()
        {
            var root = new Grid { Margin = new Thickness(10) };
            var glass = new Border
            {
                CornerRadius = new CornerRadius(16),
                Background = Br(Color.FromRgb(0x0D, 0x14, 0x24), 0xE8),
                BorderBrush = Br(Colors.White, 0x22),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 0, Opacity = 0.6, Color = Colors.Black }
            };
            root.Children.Add(glass);

            var g = new Grid { Margin = new Thickness(18) };
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // سربرگ
            var head = new Grid();
            var title = new TextBlock { Text = "کانفیگ‌ها", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Br(Colors.White), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            head.Children.Add(title);
            var close = new Grid
            {
                Width = 12, Height = 12, Background = Brushes.Transparent,
                Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center
            };
            close.Children.Add(new Ellipse { Fill = Br(Color.FromRgb(0xFF, 0x5F, 0x57)), Stroke = Br(Colors.Black, 0x2A), StrokeThickness = 0.75 });
            close.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; Close(); };
            head.Children.Add(close);
            Grid.SetRow(head, 0);
            g.Children.Add(head);

            // ورود لینک
            var lbl = new TextBlock { Text = "لینک vless را اینجا بچسبانید:", FontSize = 12, Foreground = Br(CMuted), Margin = new Thickness(0, 8, 0, 5) };
            Grid.SetRow(lbl, 1);
            g.Children.Add(lbl);

            var linkRow = new Grid();
            linkRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linkRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            LinkBox = new TextBox
            {
                FontSize = 11, FontFamily = new FontFamily("Consolas"),
                Background = Br(Colors.White, 0x08), Foreground = Br(Colors.White),
                BorderBrush = Br(Colors.White, 0x20), BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6), VerticalContentAlignment = VerticalAlignment.Center,
                FlowDirection = FlowDirection.LeftToRight, Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(LinkBox, 0);
            linkRow.Children.Add(LinkBox);
            var addBtn = new Button
            {
                Content = "افزودن", Width = 84, Height = 32,
                Foreground = Br(Colors.White), FontSize = 12,
                Background = Br(CGreen, 0x30), BorderBrush = Br(CGreen, 0x66), BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            var bt = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Border.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Border.BorderBrushProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            bt.VisualTree = bd;
            addBtn.Template = bt;
            addBtn.Click += delegate { AddClicked(); };
            Grid.SetColumn(addBtn, 1);
            linkRow.Children.Add(addBtn);
            Grid.SetRow(linkRow, 2);
            g.Children.Add(linkRow);

            // فهرست
            var lbl2 = new TextBlock { Text = "خروجی‌های فعال (کلیک = انتخاب):", FontSize = 12, Foreground = Br(CMuted), Margin = new Thickness(0, 14, 0, 5) };
            Grid.SetRow(lbl2, 3);
            g.Children.Add(lbl2);

            var sc = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 26, 0, 0) };
            ListHost = new StackPanel();
            sc.Content = ListHost;
            Grid.SetRow(sc, 3);
            g.Children.Add(sc);

            glass.Child = g;
            Content = root;
        }

        void LoadList()
        {
            ListHost.Children.Clear();
            bool chainRunning = Process.GetProcessesByName("sing-box").Length > 0;
            foreach (var m in Importer.Members())
            {
                string tag = m.Key;
                bool active = m.Value == "active";
                var b = new Border
                {
                    Height = 36, CornerRadius = new CornerRadius(9),
                    Background = Br(Colors.White, active ? (byte)0x10 : (byte)0x06),
                    BorderBrush = active ? Br(CGreen, (byte)0x55) : Br(Colors.White, (byte)0x10),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 2, 0, 2), Cursor = Cursors.Hand
                };
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var dot = new Ellipse { Width = 9, Height = 9, Fill = Br(active ? CGreenLt : CMuted), Margin = new Thickness(10, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(dot, 0);
                row.Children.Add(dot);
                var name = new TextBlock
                {
                    Text = tag, FontFamily = new FontFamily("Consolas"), FontSize = 12,
                    Foreground = Br(Colors.White, active ? (byte)0xF0 : (byte)0xB0),
                    VerticalAlignment = VerticalAlignment.Center, FlowDirection = FlowDirection.LeftToRight
                };
                Grid.SetColumn(name, 1);
                row.Children.Add(name);
                if (active)
                {
                    var live = new TextBlock
                    {
                        Text = chainRunning ? "فعال — در حال استفاده" : "فعال (با استارت بعدی)",
                        FontSize = 10, Foreground = Br(active ? CGreenLt : CMuted),
                        VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 10, 0)
                    };
                    Grid.SetColumn(live, 2);
                    row.Children.Add(live);
                }
                b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    SwitchTo(tag);
                };
                ListHost.Children.Add(b);
            }
            if (ListHost.Children.Count == 0)
                ListHost.Children.Add(new TextBlock { Text = "چیزی نیست", Foreground = Br(CMuted), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) });
        }

        void SwitchTo(string tag)
        {
            if (!Importer.SetDefault(tag)) { _say("[×] تغییر کانفیگ ناموفق"); return; }
            if (Process.GetProcessesByName("sing-box").Length > 0)
            {
                string err = Importer.LiveSwitch(tag);
                _say(err == "" ? "[✓] کانفیگ عوض شد: " + tag + " (بدون ری‌استارت)" : "[✓] ثبت شد: " + tag + " — سوییچ زنده نشد (" + err + ")");
            }
            else
            {
                _say("[✓] ثبت شد: " + tag + " — با روشن کردن زنجیره اعمال می‌شود");
            }
            LoadList();
        }

        void AddClicked()
        {
            string text = LinkBox.Text.Trim();
            if (text == "") { _say("[×] اول لینک را وارد کن"); return; }
            int okCount = 0; string lastTag = "";
            foreach (string lineRaw in text.Replace("\r", "").Split('\n'))
            {
                string line = lineRaw.Trim();
                if (!line.StartsWith("vless://", StringComparison.OrdinalIgnoreCase)) continue;
                string r = Importer.ImportLink(line);
                if (r.StartsWith("imp-")) { okCount++; lastTag = r; }
                else { _say("[×] " + r); return; }
            }
            if (okCount == 0) { _say("[×] لینک vless معتبری پیدا نشد"); return; }
            LinkBox.Clear();
            LoadList();
            _say("[✓] " + okCount + " کانفیگ اضافه شد (" + lastTag + ") — زنجیره را خاموش/روشن کن");
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length >= 2 && args[0] == "import")
            {
                string r = Importer.ImportLink(args[1]);
                Environment.ExitCode = r.StartsWith("imp-") ? 0 : 1;
                return;
            }
            bool admin = false;
            try
            {
                using (WindowsIdentity wi = WindowsIdentity.GetCurrent())
                    admin = new WindowsPrincipal(wi).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { }
            if (!admin)
            {
                try
                {
                    var exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var psi = new ProcessStartInfo(exe) { UseShellExecute = true, Verb = "runas" };
                    Process.Start(psi);
                }
                catch { }
                return;
            }
            var app = new Application();
            app.DispatcherUnhandledException += delegate(object s, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            {
                MessageBox.Show(e.Exception.ToString(), "خطای برنامه");
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                try { MessageBox.Show(e.ExceptionObject.ToString(), "خطای بحرانی"); } catch { }
            };
            app.Run(new AppWindow());
        }
    }
}
