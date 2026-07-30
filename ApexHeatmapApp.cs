using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("Apex Pro Live Heatmap")]
[assembly: AssemblyDescription("Live key-usage heatmap for SteelSeries Apex Pro keyboards")]
[assembly: AssemblyCompany("Community project")]
[assembly: AssemblyProduct("Apex Pro Live Heatmap")]
[assembly: AssemblyCopyright("Copyright (c) 2026 OldManLoki")]
[assembly: AssemblyVersion("0.1.3.0")]
[assembly: AssemblyFileVersion("0.1.3.0")]

namespace ApexProHeatmap
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, @"Local\ApexProLiveHeatmap", out created))
            {
                if (!created)
                {
                    MessageBox.Show("Apex Pro Live Heatmap läuft bereits.", "Apex Pro Live Heatmap",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log"), ex.ToString());
                    MessageBox.Show(ex.Message, "Apex Pro Live Heatmap – Fehler",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    internal sealed class AppConfig
    {
        public int updateIntervalMs = 250;
        public int autosaveSeconds = 30;
        public double heatHalfLifeMinutes = 15.0;
        public bool countAutoRepeat = false;
        public bool persistStatistics = true;
        public bool startAutomatically = true;
        public bool minimizeToTray = true;
        public string normalization = "logarithmic";
    }

    internal sealed class KeyDef
    {
        public string Label;
        public int Id;
        public int Row;
        public int[] Cells;
        public KeyDef(string label, int scan, bool extended, int row, params int[] cells)
        {
            Label = label; Id = scan | (extended ? 0x100 : 0); Row = row; Cells = cells;
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();
        private readonly AppConfig config;
        private readonly List<KeyDef> layout;
        private readonly Dictionary<int, long> totals = new Dictionary<int, long>();
        private readonly Dictionary<int, double> heat = new Dictionary<int, double>();
        private readonly Dictionary<int, Label> cells = new Dictionary<int, Label>();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly Button startButton = new Button();
        private readonly Label status = new Label();
        private readonly Label counter = new Label();
        private readonly NumericUpDown halfLife = new NumericUpDown();
        private readonly CheckBox repeat = new CheckBox();
        private readonly CheckBox persist = new CheckBox();
        private readonly CheckBox minimizeToTray = new CheckBox();
        private readonly NotifyIcon trayIcon = new NotifyIcon();
        private ToolStripMenuItem trayStartStop;
        private bool running;
        private bool trayHintShown;
        private DateTime lastTick = DateTime.UtcNow;
        private DateTime lastSave = DateTime.UtcNow;

        public MainForm()
        {
            config = LoadConfig();
            layout = BuildLayout();
            LoadStats();
            BuildUi();

            timer.Interval = Math.Max(100, config.updateIntervalMs);
            timer.Tick += Tick;
            timer.Start();
            FormClosing += OnFormClosing;
            Resize += delegate {
                if (WindowState == FormWindowState.Minimized && minimizeToTray.Checked)
                    HideToTray();
            };
            if (config.startAutomatically) StartCapture();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Properties such as ShowInTaskbar can recreate the Win32 window.
            // Raw Input targets a specific handle and must follow that change.
            RawKeyboardCounter.Register(Handle);
        }

        private void BuildUi()
        {
            Text = "Apex Pro Live Heatmap";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1160, 565);
            MinimumSize = new Size(960, 540);
            BackColor = Color.FromArgb(17, 22, 34);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9f);

            Controls.Add(new Label {
                Text = "APEX PRO  ·  LIVE HEATMAP", AutoSize = true,
                Font = new Font("Segoe UI Semibold", 16f), Location = new Point(22, 16)
            });
            Controls.Add(new Label {
                Text = "Lokal: nur Zähler physischer Tasten – keine Texte, Reihenfolgen oder Programmnamen.",
                AutoSize = true, ForeColor = Color.FromArgb(170,185,210), Location = new Point(24, 50)
            });

            var trayButton = new Button {
                Text = "In Infobereich", Size = new Size(145, 34), Location = new Point(987, 14)
            };
            trayButton.Click += delegate { HideToTray(); };
            Controls.Add(trayButton);

            minimizeToTray.Text = "Beim Minimieren ausblenden";
            minimizeToTray.AutoSize = true;
            minimizeToTray.Checked = config.minimizeToTray;
            minimizeToTray.Location = new Point(958, 52);
            minimizeToTray.CheckedChanged += delegate {
                config.minimizeToTray = minimizeToTray.Checked; SaveConfig();
            };
            Controls.Add(minimizeToTray);

            var grid = new TableLayoutPanel {
                RowCount = 6, ColumnCount = 22, Location = new Point(22, 82),
                Size = new Size(1110, 330), BackColor = Color.FromArgb(9,13,22),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            for (int i=0; i<22; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f/22f));
            for (int i=0; i<6; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f/6f));
            Controls.Add(grid);

            foreach (var key in layout)
            {
                foreach (int cell in key.Cells)
                {
                    int index = key.Row * 22 + cell;
                    if (cells.ContainsKey(index)) continue;
                    var label = new Label {
                        Text = key.Label, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill,
                        Margin = new Padding(1), BackColor = Color.FromArgb(7,12,24),
                        ForeColor = Color.White, Tag = key.Id
                    };
                    cells[index] = label;
                    grid.Controls.Add(label, cell, key.Row);
                }
            }

            startButton.Text = "Start";
            startButton.Size = new Size(105,38);
            startButton.Location = new Point(22,430);
            startButton.Click += delegate { if (running) StopCapture(); else StartCapture(); };
            Controls.Add(startButton);

            var resetLive = new Button { Text="Live-Heatmap leeren", Size=new Size(145,38), Location=new Point(138,430) };
            resetLive.Click += delegate { heat.Clear(); };
            Controls.Add(resetLive);

            var resetAll = new Button { Text="Alle Zähler löschen", Size=new Size(135,38), Location=new Point(294,430) };
            resetAll.Click += delegate {
                if (MessageBox.Show("Wirklich alle Zähler löschen?", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    heat.Clear(); totals.Clear();
                    string path = Path.Combine(baseDir, "stats.json");
                    if (File.Exists(path)) File.Delete(path);
                }
            };
            Controls.Add(resetAll);

            Controls.Add(new Label { Text="Halbwertszeit (Min.)", AutoSize=true, Location=new Point(455,440) });
            halfLife.Minimum=0; halfLife.Maximum=1440; halfLife.DecimalPlaces=1; halfLife.Increment=0.5m;
            halfLife.Value=(decimal)Math.Max(0, Math.Min(1440, config.heatHalfLifeMinutes));
            halfLife.Size=new Size(75,28); halfLife.Location=new Point(580,436);
            halfLife.ValueChanged += delegate { config.heatHalfLifeMinutes=(double)halfLife.Value; SaveConfig(); };
            Controls.Add(halfLife);

            repeat.Text="Gedrückthalten mehrfach zählen"; repeat.AutoSize=true;
            repeat.Checked=config.countAutoRepeat; repeat.Location=new Point(680,439);
            repeat.CheckedChanged += delegate { config.countAutoRepeat=repeat.Checked; RawKeyboardCounter.CountAutoRepeat=repeat.Checked; SaveConfig(); };
            Controls.Add(repeat);

            persist.Text="Langzeit-Zähler lokal speichern"; persist.AutoSize=true;
            persist.Checked=config.persistStatistics; persist.Location=new Point(900,439);
            persist.CheckedChanged += delegate { config.persistStatistics=persist.Checked; SaveConfig(); if (persist.Checked) SaveStats(); };
            Controls.Add(persist);

            status.AutoSize=true; status.Location=new Point(24,490); status.ForeColor=Color.FromArgb(120,210,170);
            counter.AutoSize=true; counter.Location=new Point(24,516); counter.ForeColor=Color.FromArgb(170,185,210);
            Controls.Add(status); Controls.Add(counter);

            BuildTrayIcon();
        }

        private void BuildTrayIcon()
        {
            var menu = new ContextMenuStrip();
            var open = new ToolStripMenuItem("Öffnen");
            open.Font = new Font(open.Font, FontStyle.Bold);
            open.Click += delegate { ShowFromTray(); };
            trayStartStop = new ToolStripMenuItem("Erfassung stoppen");
            trayStartStop.Click += delegate { if (running) StopCapture(); else StartCapture(); };
            var exit = new ToolStripMenuItem("Beenden");
            exit.Click += delegate { Close(); };
            menu.Items.Add(open);
            menu.Items.Add(trayStartStop);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exit);
            menu.Opening += delegate {
                trayStartStop.Text=running ? "Erfassung stoppen" : "Erfassung starten";
            };

            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = "Apex Pro Live Heatmap";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += delegate { ShowFromTray(); };
            trayIcon.Visible = true;
        }

        private void HideToTray()
        {
            Hide();
            if (!trayHintShown)
            {
                trayIcon.BalloonTipTitle="Apex Pro Live Heatmap";
                trayIcon.BalloonTipText="Die Heatmap läuft im Infobereich weiter. Doppelklick öffnet das Fenster.";
                trayIcon.ShowBalloonTip(2500);
                trayHintShown=true;
            }
        }

        private void ShowFromTray()
        {
            Show();
            WindowState=FormWindowState.Normal;
            Activate();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            timer.Stop();
            StopCapture();
            SaveConfig();
            trayIcon.Visible=false;
            trayIcon.Dispose();
        }

        private void StartCapture()
        {
            RawKeyboardCounter.CountAutoRepeat = repeat.Checked;
            RawKeyboardCounter.Enabled = true;
            running = true; lastTick = DateTime.UtcNow; startButton.Text = "Stop";
            status.Text = "Raw-Input-Erfassung läuft – Verbindung zu GG wird aufgebaut";
        }

        private void StopCapture()
        {
            if (!running) return;
            RawKeyboardCounter.Enabled = false; GameSenseClient.StopGame();
            running=false; startButton.Text="Start"; status.Text="Gestoppt"; SaveStats();
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == RawKeyboardCounter.WM_INPUT)
                RawKeyboardCounter.Process(message.LParam);
            base.WndProc(ref message);
        }

        private void Tick(object sender, EventArgs e)
        {
            DateTime now=DateTime.UtcNow;
            double elapsed=(now-lastTick).TotalMinutes;
            lastTick=now;
            if (config.heatHalfLifeMinutes > 0 && elapsed > 0)
            {
                double decay=Math.Pow(0.5, elapsed/config.heatHalfLifeMinutes);
                foreach (int id in new List<int>(heat.Keys))
                {
                    heat[id]*=decay;
                    if (heat[id]<0.001) heat.Remove(id);
                }
            }
            if (running)
            {
                foreach (var pair in RawKeyboardCounter.Drain())
                {
                    if (!totals.ContainsKey(pair.Key)) totals[pair.Key]=0;
                    if (!heat.ContainsKey(pair.Key)) heat[pair.Key]=0;
                    totals[pair.Key]+=pair.Value; heat[pair.Key]+=pair.Value;
                }
            }

            double max=0;
            foreach (double v in heat.Values) if (v>max) max=v;
            foreach (var key in layout)
            {
                double v=heat.ContainsKey(key.Id) ? heat[key.Id] : 0;
                Color c=HeatColor(v,max,config.normalization);
                long total=totals.ContainsKey(key.Id) ? totals[key.Id] : 0;
                foreach (int cell in key.Cells)
                {
                    int index=key.Row*22+cell;
                    Label label;
                    if (cells.TryGetValue(index,out label)) { label.BackColor=c; label.Text=key.Label+"\r\n"+total; }
                }
            }
            if (running) GameSenseClient.QueueFrame(CreateBitmap());
            if (running) status.Text=GameSenseClient.LastStatus;
            long sum=0; foreach (long v in totals.Values) sum+=v;
            counter.Text="Gezählte Tastendrücke: "+sum+"  ·  GG-App: Apex Pro Live Heatmap";
            if (config.persistStatistics && (now-lastSave).TotalSeconds>=config.autosaveSeconds) SaveStats();
        }

        private int[][] CreateBitmap()
        {
            double max=0; foreach(double v in heat.Values) if(v>max) max=v;
            var bitmap=new int[132][];
            for(int i=0;i<132;i++) bitmap[i]=new[]{7,12,24};
            foreach(var key in layout)
            {
                double v=heat.ContainsKey(key.Id)?heat[key.Id]:0;
                Color c=HeatColor(v,max,config.normalization);
                foreach(int cell in key.Cells) bitmap[key.Row*22+cell]=new[]{(int)c.R,(int)c.G,(int)c.B};
            }
            return bitmap;
        }

        private static Color HeatColor(double value,double maximum,string mode)
        {
            if(maximum<=0 || value<=0) return Color.FromArgb(7,12,24);
            double r=Math.Min(1,value/maximum);
            if(mode=="logarithmic") r=Math.Log(1+9*r)/Math.Log(10);
            double[][] s={
                new[]{0.0,12.0,28.0,70.0},new[]{0.25,0.0,150.0,255.0},
                new[]{0.5,0.0,220.0,130.0},new[]{0.75,255.0,210.0,0.0},
                new[]{1.0,255.0,35.0,20.0}
            };
            for(int i=0;i<s.Length-1;i++) if(r<=s[i+1][0])
            {
                double t=(r-s[i][0])/(s[i+1][0]-s[i][0]);
                return Color.FromArgb((int)(s[i][1]+(s[i+1][1]-s[i][1])*t),
                    (int)(s[i][2]+(s[i+1][2]-s[i][2])*t),
                    (int)(s[i][3]+(s[i+1][3]-s[i][3])*t));
            }
            return Color.FromArgb(255,35,20);
        }

        private AppConfig LoadConfig()
        {
            string path=Path.Combine(baseDir,"config.json");
            try { return File.Exists(path) ? json.Deserialize<AppConfig>(File.ReadAllText(path)) : new AppConfig(); }
            catch { return new AppConfig(); }
        }
        private void SaveConfig()
        {
            File.WriteAllText(Path.Combine(baseDir,"config.json"), json.Serialize(config), Encoding.UTF8);
        }
        private void LoadStats()
        {
            if(!config.persistStatistics) return;
            string path=Path.Combine(baseDir,"stats.json");
            try
            {
                if(!File.Exists(path)) return;
                foreach(var p in json.Deserialize<Dictionary<string,long>>(File.ReadAllText(path)))
                    totals[int.Parse(p.Key)]=p.Value;
            } catch { }
        }
        private void SaveStats()
        {
            if(!config.persistStatistics) return;
            var output=new Dictionary<string,long>();
            foreach(var p in totals) output[p.Key.ToString()]=p.Value;
            File.WriteAllText(Path.Combine(baseDir,"stats.json"),json.Serialize(output),Encoding.UTF8);
            lastSave=DateTime.UtcNow;
        }

        private static List<KeyDef> BuildLayout()
        {
            var k=new List<KeyDef>();
            Action<string,int,bool,int,int[]> add=(l,s,e,r,c)=>k.Add(new KeyDef(l,s,e,r,c));
            add("Esc",1,false,0,new[]{0});
            int[] fscan={59,60,61,62,63,64,65,66,67,68,87,88}; int[] fx={2,3,4,5,7,8,9,10,12,13,14,15};
            for(int i=0;i<12;i++) add("F"+(i+1),fscan[i],false,0,new[]{fx[i]});
            add("Druck",55,true,0,new[]{17}); add("Rollen",70,false,0,new[]{18}); add("Pause",69,false,0,new[]{19});
            string[] r1={"^","1","2","3","4","5","6","7","8","9","0","ß","´"};
            int[] s1={41,2,3,4,5,6,7,8,9,10,11,12,13}; for(int i=0;i<r1.Length;i++) add(r1[i],s1[i],false,1,new[]{i});
            add("⌫",14,false,1,new[]{13,14}); add("Einfg",82,true,1,new[]{15}); add("Pos1",71,true,1,new[]{16});
            add("Bild↑",73,true,1,new[]{17}); add("Num",69,true,1,new[]{18}); add("/",53,true,1,new[]{19});
            add("*",55,false,1,new[]{20}); add("−",74,false,1,new[]{21});
            add("Tab",15,false,2,new[]{0});
            string[] r2={"Q","W","E","R","T","Z","U","I","O","P","Ü","+"}; int[] s2={16,17,18,19,20,21,22,23,24,25,26,27};
            for(int i=0;i<r2.Length;i++) add(r2[i],s2[i],false,2,new[]{i+1});
            add("Enter",28,false,2,new[]{13,14}); add("Entf",83,true,2,new[]{15}); add("Ende",79,true,2,new[]{16});
            add("Bild↓",81,true,2,new[]{17}); add("7",71,false,2,new[]{18}); add("8",72,false,2,new[]{19});
            add("9",73,false,2,new[]{20}); add("+",78,false,2,new[]{21});
            add("Caps",58,false,3,new[]{0});
            string[] r3={"A","S","D","F","G","H","J","K","L","Ö","Ä","#"}; int[] s3={30,31,32,33,34,35,36,37,38,39,40,43};
            for(int i=0;i<r3.Length;i++) add(r3[i],s3[i],false,3,new[]{i+1});
            add("Enter",28,false,3,new[]{13,14}); add("4",75,false,3,new[]{18}); add("5",76,false,3,new[]{19});
            add("6",77,false,3,new[]{20}); add("+",78,false,3,new[]{21});
            add("Shift",42,false,4,new[]{0}); add("<>",86,false,4,new[]{1});
            string[] r4={"Y","X","C","V","B","N","M",",",".","-"}; int[] s4={44,45,46,47,48,49,50,51,52,53};
            for(int i=0;i<r4.Length;i++) add(r4[i],s4[i],false,4,new[]{i+2});
            add("Shift",54,false,4,new[]{12,13}); add("↑",72,true,4,new[]{16});
            add("1",79,false,4,new[]{18}); add("2",80,false,4,new[]{19}); add("3",81,false,4,new[]{20}); add("Enter",28,true,4,new[]{21});
            add("Strg",29,false,5,new[]{0}); add("Win",91,true,5,new[]{1}); add("Alt",56,false,5,new[]{2});
            add("Leertaste",57,false,5,new[]{4,5,6,7,8,9}); add("Alt Gr",56,true,5,new[]{10});
            add("Menü",93,true,5,new[]{12}); add("Strg",29,true,5,new[]{13});
            add("←",75,true,5,new[]{15}); add("↓",80,true,5,new[]{16}); add("→",77,true,5,new[]{17});
            add("0",82,false,5,new[]{18,19}); add(",",83,false,5,new[]{20}); add("Enter",28,true,5,new[]{21});
            return k;
        }
    }

    internal static class RawKeyboardCounter
    {
        public const int WM_INPUT=0x00FF;
        private const uint RID_INPUT=0x10000003, RIDEV_INPUTSINK=0x00000100;
        private const ushort RI_KEY_BREAK=0x0001, RI_KEY_E0=0x0002, RI_KEY_E1=0x0004;
        private static readonly object sync=new object();
        private static readonly Dictionary<int,long> pending=new Dictionary<int,long>();
        private static readonly HashSet<int> held=new HashSet<int>();
        public static bool Enabled;
        public static bool CountAutoRepeat;

        public static void Register(IntPtr windowHandle)
        {
            var devices=new[]{new RawInputDevice{UsagePage=0x01,Usage=0x06,Flags=RIDEV_INPUTSINK,Target=windowHandle}};
            if(!RegisterRawInputDevices(devices,1,(uint)Marshal.SizeOf(typeof(RawInputDevice))))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public static Dictionary<int,long> Drain(){lock(sync){var d=new Dictionary<int,long>(pending);pending.Clear();return d;}}

        public static void Process(IntPtr rawInputHandle)
        {
            uint size=0;
            uint headerSize=(uint)Marshal.SizeOf(typeof(RawInputHeader));
            if(GetRawInputData(rawInputHandle,RID_INPUT,IntPtr.Zero,ref size,headerSize)!=0 || size==0)return;
            IntPtr buffer=Marshal.AllocHGlobal((int)size);
            try
            {
                if(GetRawInputData(rawInputHandle,RID_INPUT,buffer,ref size,headerSize)!=size)return;
                var header=(RawInputHeader)Marshal.PtrToStructure(buffer,typeof(RawInputHeader));
                if(header.Type!=1)return;
                IntPtr keyboardPtr=IntPtr.Add(buffer,Marshal.SizeOf(typeof(RawInputHeader)));
                var data=(RawKeyboard)Marshal.PtrToStructure(keyboardPtr,typeof(RawKeyboard));
                if(data.VKey==255 || data.MakeCode==0)return;
                bool isBreak=(data.Flags&RI_KEY_BREAK)!=0;
                bool extended=(data.Flags&(RI_KEY_E0|RI_KEY_E1))!=0;
                int id=data.MakeCode|(extended?0x100:0);
                lock(sync)
                {
                    if(!Enabled){held.Clear();return;}
                    if(!isBreak)
                    {
                        if(CountAutoRepeat||held.Add(id)){if(!pending.ContainsKey(id))pending[id]=0;pending[id]++;}
                    }
                    else held.Remove(id);
                }
            }
            finally{Marshal.FreeHGlobal(buffer);}
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDevice{public ushort UsagePage,Usage;public uint Flags;public IntPtr Target;}
        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputHeader{public uint Type,Size;public IntPtr Device;public IntPtr WParam;}
        [StructLayout(LayoutKind.Sequential)]
        private struct RawKeyboard
        {
            public ushort MakeCode,Flags,Reserved,VKey;
            public uint Message,ExtraInformation;
        }
        [DllImport("user32.dll",SetLastError=true)]
        private static extern bool RegisterRawInputDevices(RawInputDevice[] devices,uint number,uint size);
        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr raw,uint command,IntPtr data,ref uint size,uint headerSize);
    }

    internal static class GameSenseClient
    {
        private static int busy; private static string address; private static bool registered;
        public static string LastStatus="Noch nicht verbunden";
        public static void QueueFrame(int[][] bitmap)
        {
            if(Interlocked.Exchange(ref busy,1)!=0)return;
            ThreadPool.QueueUserWorkItem(delegate {
                try
                {
                    EnsureRegistered(); var b=new StringBuilder(2400);
                    b.Append("{\"game\":\"APEX_HEATMAP\",\"event\":\"HEATMAP\",\"data\":{\"value\":1,\"frame\":{\"bitmap\":[");
                    for(int i=0;i<bitmap.Length;i++){if(i>0)b.Append(',');b.Append('[').Append(bitmap[i][0]).Append(',').Append(bitmap[i][1]).Append(',').Append(bitmap[i][2]).Append(']');}
                    b.Append("]}}}"); Post("/game_event",b.ToString()); LastStatus="Verbunden – Heatmap wird übertragen";
                }catch(Exception ex){registered=false;LastStatus="GG: "+ex.Message;}finally{Interlocked.Exchange(ref busy,0);}
            });
        }
        public static void StopGame(){try{Discover();Post("/stop_game","{\"game\":\"APEX_HEATMAP\"}");}catch{}}
        private static void EnsureRegistered()
        {
            Discover(); if(registered)return;
            Post("/game_metadata","{\"game\":\"APEX_HEATMAP\",\"game_display_name\":\"Apex Pro Live Heatmap\",\"developer\":\"Lokales Tool\",\"deinitialize_timer_length_ms\":3000}");
            Post("/bind_game_event","{\"game\":\"APEX_HEATMAP\",\"event\":\"HEATMAP\",\"min_value\":0,\"max_value\":1,\"handlers\":[{\"device-type\":\"rgb-per-key-zones\",\"mode\":\"bitmap\"}]}");
            registered=true;
        }
        private static void Discover()
        {
            string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"SteelSeries","SteelSeries Engine 3","coreProps.json");
            if(!File.Exists(path))throw new FileNotFoundException("SteelSeries GG läuft nicht.");
            Match m=Regex.Match(File.ReadAllText(path),"\"address\"\\s*:\\s*\"([^\"]+)\"");
            if(!m.Success)throw new InvalidDataException("GameSense-Adresse fehlt.");
            address="http://"+m.Groups[1].Value;
        }
        private static void Post(string endpoint,string body)
        {
            byte[] bytes=Encoding.UTF8.GetBytes(body); var r=(HttpWebRequest)WebRequest.Create(address+endpoint);
            r.Method="POST";r.ContentType="application/json";r.ContentLength=bytes.Length;r.Timeout=1800;
            using(Stream s=r.GetRequestStream())s.Write(bytes,0,bytes.Length);
            using(var response=(HttpWebResponse)r.GetResponse())if((int)response.StatusCode!=200)throw new WebException("HTTP "+(int)response.StatusCode);
        }
    }
}
