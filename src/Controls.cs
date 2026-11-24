using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace PKT
{
    class BaseCtl : Control
    {
        protected Point hov = new Point(-1, -1);
        public BaseCtl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
        }
        protected override void OnMouseMove(MouseEventArgs e) { hov = e.Location; Invalidate(); base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { hov = new Point(-1, -1); Invalidate(); base.OnMouseLeave(e); }
        protected Graphics Begin(PaintEventArgs e) { var g = e.Graphics; g.Clear(BackColor); G.AA(g); return g; }
    }

    // Beyaz, köşeleri yuvarlatılmış kart
    class Card : Panel
    {
        public int R; public string Title; public string Sub;
        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White; R = Th.D(14);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Th.Bg);
            G.AA(g);
            G.Fill(g, new RectangleF(0, 1, Width - 1, Height - 1), R, Color.FromArgb(10, 0, 0, 0));
            G.Fill(g, new RectangleF(0, 0, Width - 1, Height - 1.5f), R, Color.White);
            if (!string.IsNullOrEmpty(Title))
            {
                int w = G.TW(Title, Th.F(11f, true));
                G.T(g, Title, Th.F(11f, true), new Rectangle(Th.D(16), Th.D(12), Width - Th.D(32), Th.D(24)), Th.Text, G.L);
                if (!string.IsNullOrEmpty(Sub))
                    G.T(g, Sub, Th.F(9f), new Rectangle(Th.D(16) + w + Th.D(6), Th.D(13), Width, Th.D(24)), Th.Sub, G.L);
            }
        }
        protected override void OnPaint(PaintEventArgs e) { }
    }

    // iOS segment kontrolü
    class Segmented : BaseCtl
    {
        public string[] Items = new string[0];
        public Color[] Colors;
        public int Sel;
        public event EventHandler Changed;
        public Segmented() { Height = Th.D(32); Cursor = Cursors.Hand; }
        public void SetItems(params string[] items) { Items = items; Invalidate(); }
        float CellW { get { return (Width - Th.D(4)) / (float)Math.Max(1, Items.Length); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            G.Fill(g, new RectangleF(0, 0, Width - 1, Height - 1), Th.D(9), Th.Track);
            int n = Items.Length; if (n == 0) return;
            float w = CellW;
            for (int i = 0; i < n; i++)
            {
                var c = new RectangleF(Th.D(2) + i * w, Th.D(2), w, Height - Th.D(4) - 1);
                if (i == Sel)
                {
                    G.Fill(g, new RectangleF(c.X, c.Y + 1, c.Width, c.Height), Th.D(7), Color.FromArgb(22, 0, 0, 0));
                    G.Fill(g, c, Th.D(7), Color.White);
                }
                else if (i > 0 && i - 1 != Sel)
                    using (var p = new Pen(Th.H("#D1D1D6"))) g.DrawLine(p, c.X, c.Y + c.Height * .28f, c.X, c.Y + c.Height * .72f);
                Color fg = Th.Text;
                if (i == Sel && Colors != null && i < Colors.Length && Colors[i] != Color.Empty) fg = Colors[i];
                G.T(g, Items[i], i == Sel ? Th.F(9f, true) : Th.F(9f), Rectangle.Round(c), fg, G.C);
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Items.Length == 0) return;
            int i = (int)((e.X - Th.D(2)) / CellW);
            i = Math.Max(0, Math.Min(Items.Length - 1, i));
            if (i != Sel) { Sel = i; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); }
        }
    }

    // Hapı andıran düğme
    class PillButton : BaseCtl
    {
        public string Kind = "primary"; // primary, tinted, plain, danger, ghost
        public string IconKind; public bool Chevron; public bool Active; public Color Accent = Th.Blue;
        public PillButton() { Cursor = Cursors.Hand; Height = Th.D(32); Font = Th.F(9f, true); }
        public int PrefW()
        {
            int w = string.IsNullOrEmpty(Text) ? 0 : G.TW(Text, Font);
            int pad = Th.D(14) * 2;
            if (IconKind != null) w += Th.D(string.IsNullOrEmpty(Text) ? 16 : 22);
            if (Chevron) w += Th.D(16);
            if (string.IsNullOrEmpty(Text)) return Height;
            return w + pad;
        }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            bool h = ClientRectangle.Contains(hov);
            Color bg, fg;
            string k = Active ? "tinted" : Kind;
            switch (k)
            {
                case "primary": bg = h ? Th.Dark(Accent, .1f) : Accent; fg = Color.White; break;
                case "tinted": bg = Th.Tint(Accent, h ? .2f : .13f); fg = Accent; break;
                case "danger": bg = Th.Tint(Th.Red, h ? .18f : .1f); fg = Th.Red; break;
                case "ghost": bg = h ? Th.Field : BackColor; fg = Th.GrayDark; break;
                default: bg = h ? Th.H("#E8E8ED") : Th.Field; fg = Th.Text; break;
            }
            G.Fill(g, new RectangleF(0, 0, Width - 1, Height - 1), (Height - 1) / 2f, bg);
            int x = Th.D(14);
            bool iconOnly = string.IsNullOrEmpty(Text);
            if (IconKind != null)
            {
                float s = Th.D(15);
                float ix = iconOnly ? (Width - s) / 2f : x - Th.D(2);
                G.Icon(g, IconKind, new RectangleF(ix, (Height - s) / 2f, s, s), fg, Th.K * 1.7f);
                x += Th.D(20);
            }
            if (!iconOnly)
            {
                int right = Chevron ? Th.D(28) : Th.D(12);
                G.T(g, Text, Font, new Rectangle(x, 0, Width - x - right + Th.D(2), Height), fg, G.L);
                if (Chevron) G.Icon(g, "chev", new RectangleF(Width - Th.D(24), (Height - Th.D(12)) / 2f, Th.D(12), Th.D(12)), fg, Th.K * 1.6f);
            }
        }
    }

    // Genel bakış kutucuğu (iOS Anımsatıcılar tarzı)
    class KpiTile : BaseCtl
    {
        public string Label, Glyph; public int Value; public Color Accent; public bool Selected;
        public KpiTile() { Cursor = Cursors.Hand; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            var r = new RectangleF(0, 0, Width - 1, Height - 2);
            bool h = ClientRectangle.Contains(hov);
            G.Fill(g, new RectangleF(0, 1, Width - 1, Height - 1), Th.D(12), Color.FromArgb(10, 0, 0, 0));
            G.Fill(g, r, Th.D(12), Selected ? Th.Tint(Accent, .1f) : (h ? Th.H("#FBFBFD") : Color.White));
            if (Selected) G.Stroke(g, new RectangleF(1, 1, Width - 3, Height - 4), Th.D(11), Accent, Th.K * 1.6f);
            float cs = Th.D(28);
            var c = new RectangleF(Th.D(12), Th.D(11), cs, cs);
            G.Circle(g, c, Accent);
            G.Icon(g, Glyph, new RectangleF(c.X + cs * .2f, c.Y + cs * .2f, cs * .6f, cs * .6f), Color.White, Th.K * 1.8f);
            G.T(g, Value.ToString(), Th.F(19f, true), new Rectangle(0, Th.D(6), Width - Th.D(12), Th.D(36)), Th.Text, G.R);
            G.T(g, Label, Th.F(9f, true), new Rectangle(Th.D(12), Height - Th.D(28), Width - Th.D(16), Th.D(20)), Th.Sub, G.L);
        }
    }

    // Etiket çipleri (notlar için konu seçimi)
    class ChipBar : BaseCtl
    {
        public class Chip { public string Key, Text; public Color Color; public int Count; }
        public List<Chip> Chips = new List<Chip>();
        public string Sel = "all";
        public event EventHandler Changed;
        readonly List<Rectangle> rects = new List<Rectangle>();
        public ChipBar() { Cursor = Cursors.Hand; }
        public int Lay(int width)
        {
            rects.Clear();
            int x = 0, y = 0, h = Th.D(28), gap = Th.D(6);
            foreach (var c in Chips)
            {
                string t = c.Text + (c.Count > 0 ? "  " + c.Count : "");
                int w = G.TW(t, Th.F(9f, true)) + Th.D(c.Key == "all" ? 24 : 34);
                if (x + w > width && x > 0) { x = 0; y += h + gap; }
                rects.Add(new Rectangle(x, y, w, h));
                x += w + gap;
            }
            return y + h + 1;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            Lay(Width);
            for (int i = 0; i < Chips.Count; i++)
            {
                var c = Chips[i]; var r = rects[i];
                bool sel = c.Key == Sel, h = r.Contains(hov);
                Color col = c.Key == "all" ? Th.Text : c.Color;
                G.Fill(g, new RectangleF(r.X, r.Y, r.Width - 1, r.Height - 1), r.Height / 2f, sel ? col : (h ? Th.H("#E8E8ED") : Th.Field));
                int x = r.X + Th.D(12);
                if (c.Key != "all")
                {
                    float d = Th.D(8);
                    G.Circle(g, new RectangleF(x, r.Y + (r.Height - d) / 2f, d, d), sel ? Color.White : col);
                    x += Th.D(14);
                }
                string t = c.Text + (c.Count > 0 ? "  " + c.Count : "");
                G.T(g, t, Th.F(9f, true), new Rectangle(x, r.Y, r.Right - x, r.Height), sel ? Color.White : Th.Text, G.L);
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            Lay(Width);
            for (int i = 0; i < rects.Count; i++)
                if (rects[i].Contains(e.Location))
                {
                    Sel = Chips[i].Key; Invalidate();
                    if (Changed != null) Changed(this, EventArgs.Empty);
                    return;
                }
        }
    }

    // Yuvarlatılmış metin kutusu (yer tutucu yazılı)
    class FieldBox : Control
    {
        public TextBox Box; public string IconKind; public bool Error; protected int RightPad;
        public string Placeholder = "";
        public string DisabledText = "";
        readonly bool multi;
        public FieldBox(bool multiline = false, float size = 9.75f)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            multi = multiline; Cursor = Cursors.IBeam;
            Box = new TextBox { BorderStyle = BorderStyle.None, BackColor = Th.Field, ForeColor = Th.Text, Font = Th.F(size), Multiline = multiline };
            if (multiline) { Box.AcceptsReturn = true; Box.ScrollBars = ScrollBars.None; Box.WordWrap = true; }
            Controls.Add(Box);
            Box.GotFocus += (s, e) => { Invalidate(); };
            Box.LostFocus += (s, e) => { UpdPh(); Invalidate(); };
            Box.TextChanged += (s, e) => { if (!Box.Focused) UpdPh(); };
            Height = multiline ? Th.D(76) : Box.PreferredHeight + Th.D(16);
            UpdPh();
        }
        public string Value { get { return Box.Text; } set { Box.Text = value ?? ""; UpdPh(); } }
        void UpdPh()
        {
            bool show = (Box.Text.Length == 0 && !Box.Focused) || !Enabled;
            Box.Visible = !show; TabStop = show; Invalidate();
        }
        public void FocusBox() { Box.Visible = true; TabStop = false; Box.Focus(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); if (Enabled) FocusBox(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); UpdPh(); }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            Box.Bounds = BoxRect();
        }
        protected Rectangle BoxRect()
        {
            int lp = Th.D(IconKind != null ? 34 : 12);
            int bh = multi ? Height - Th.D(18) : Box.PreferredHeight;
            int tp = multi ? Th.D(9) : (Height - bh) / 2;
            return new Rectangle(lp, tp, Math.Max(10, Width - lp - Th.D(10) - RightPad), bh);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.Clear(BackColor); G.AA(g);
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            if (!Enabled)
            {
                G.Fill(g, r, Th.D(10), Th.Soft);
                using (var p = new System.Drawing.Pen(Th.Sep) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }) using (var gp = G.RR(new RectangleF(.5f, .5f, Width - 2, Height - 2), Th.D(10))) g.DrawPath(p, gp);
                G.T(g, DisabledText != "" ? DisabledText : "—", Box.Font, new Rectangle(Th.D(12), 0, Width - Th.D(20), Height), Th.Gray, G.L);
                return;
            }
            G.Fill(g, r, Th.D(10), Th.Field);
            if (Error) G.Stroke(g, new RectangleF(.75f, .75f, Width - 2.5f, Height - 2.5f), Th.D(10), Th.Red, Th.K * 1.5f);
            else if (Box.Focused) G.Stroke(g, new RectangleF(.75f, .75f, Width - 2.5f, Height - 2.5f), Th.D(10), Th.Tint(Th.Blue, .55f), Th.K * 1.5f);
            if (IconKind != null)
                G.Icon(g, IconKind, new RectangleF(Th.D(12), (Height - Th.D(15)) / 2f, Th.D(15), Th.D(15)), Th.GrayDark, Th.K * 1.7f);
            if (!Box.Visible && Placeholder != "")
            {
                var br = BoxRect();
                if (multi) G.T(g, Placeholder, Box.Font, br, Th.Gray, G.W);
                else G.T(g, Placeholder, Box.Font, new Rectangle(br.X, 0, br.Width, Height), Th.Gray, G.L);
            }
        }
        protected override void OnMouseDown(MouseEventArgs e) { FocusBox(); base.OnMouseDown(e); }
    }

    // Tarih alanı: elle yazılabilir (gg.aa.yyyy) + takvim + temizle
    class DateField : FieldBox
    {
        public DateTime? Value2 { get; private set; }
        public event EventHandler ValueChanged;
        Rectangle calR, clrR;
        ToolStripDropDown dd;
        public DateField() : base(false)
        {
            RightPad = Th.D(46); Placeholder = "gg.aa.yyyy"; Cursor = Cursors.Hand;
            Box.Leave += (s, e) => Commit();
            Box.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Commit(); e.SuppressKeyPress = true; } };
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int s = Th.D(18);
            calR = new Rectangle(Width - Th.D(28), (Height - s) / 2, s, s);
            clrR = new Rectangle(Width - Th.D(50), (Height - s) / 2, s, s);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!Enabled) return;
            var g = e.Graphics;
            G.Icon(g, "cal", calR, Th.Blue, Th.K * 1.6f);
            if (Value2.HasValue)
            {
                G.Circle(g, new RectangleF(clrR.X + 1, clrR.Y + 1, clrR.Width - 2, clrR.Height - 2), Th.H("#C7C7CC"));
                G.Icon(g, "x", new RectangleF(clrR.X + clrR.Width * .2f, clrR.Y + clrR.Height * .2f, clrR.Width * .6f, clrR.Height * .6f), Color.White, Th.K * 1.5f);
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (clrR.Contains(e.Location) && Value2.HasValue) { SetValue(null, true); return; }
            if (calR.Contains(e.Location) || e.X > Width - Th.D(60)) { ShowCal(); return; }
            base.OnMouseDown(e);
        }
        void Commit()
        {
            string t = Box.Text.Trim();
            if (t == "") { SetValue(null, true); return; }
            DateTime d;
            if (TryParse(t, out d)) SetValue(d, true);
            else { Error = true; Invalidate(); }
        }
        public void SetValue(DateTime? v, bool raise)
        {
            bool ch = v != Value2;
            Value2 = v;
            Value = v.HasValue ? v.Value.ToString("dd.MM.yyyy") : "";
            Error = false; Invalidate();
            if (raise && ch && ValueChanged != null) ValueChanged(this, EventArgs.Empty);
        }
        static bool TryParse(string t, out DateTime d)
        {
            t = t.Replace(",", ".").Replace(" ", "");
            string[] f = { "d.M.yyyy", "dd.MM.yyyy", "d/M/yyyy", "d-M-yyyy", "ddMMyyyy", "d.M.yy", "yyyy-MM-dd", "ddMMyy" };
            if (DateTime.TryParseExact(t, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return true;
            if (DateTime.TryParseExact(t, new[] { "d.M", "dd.MM" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            { d = new DateTime(DateTime.Today.Year, d.Month, d.Day); return true; }
            return false;
        }
        public void ShowCal()
        {
            var mc = new MonthCalendar { MaxSelectionCount = 1, ShowTodayCircle = true, ShowWeekNumbers = false };
            if (Value2.HasValue) mc.SetDate(Value2.Value);
            dd = new ToolStripDropDown { Padding = Padding.Empty, DropShadowEnabled = true };
            mc.DateSelected += (s, e) => { SetValue(e.Start.Date, true); dd.Close(); };
            var host = new ToolStripControlHost(mc) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = mc.Size };
            dd.Items.Add(host);
            dd.Show(this, new Point(0, Height + Th.D(4)));
        }
    }

    // Halka şeklinde ilerleme göstergesi
    class ProgressRing : BaseCtl
    {
        public int Percent; public Color Color = Th.Blue;
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            float w = Th.D(5), s = Math.Min(Width, Height) - w - 2;
            var r = new RectangleF((Width - s) / 2f, (Height - s) / 2f, s, s);
            using (var p = new Pen(Th.Track, w)) g.DrawEllipse(p, r);
            if (Percent > 0)
                using (var p = new Pen(Color, w) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(p, r, -90, Math.Max(1, 360f * Percent / 100f));
            G.T(g, "%" + Percent, Th.F(9.5f, true), ClientRectangle, Th.Text, G.C);
        }
    }

    // Seçili firmanın üst özet satırı
    class StatusLine : BaseCtl
    {
        public Firm F;
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            if (F == null) return;
            var cat = Flow.Category(F); int cur = Flow.Current(F);
            int x = 0;
            if (F.arsiv) x = Pill(g, x, "Arşivde · " + U.TS(F.arsivTarih).Split(' ')[0], Th.GrayDark) + Th.D(6);
            x = Pill(g, x, Flow.CatName(cat), Flow.CatColor(cat)) + Th.D(6);
            if (Flow.Idle(F)) x = Pill(g, x, "Beklemede · " + Flow.IdleDays(F) + " gündür işlem yok", Th.H("#C99A00")) + Th.D(6);
            if (cur < Flow.DONE) x = Pill(g, x, "Şu an: " + Flow.ViewOf(F, cur).Short, Flow.S[cur].Color) + Th.D(6);
            if (F.ucret != "") x = Pill(g, x, F.ucret, F.ucret == Flow.Ucretsiz ? Th.Teal : Th.Indigo) + Th.D(10);
            string t = "Güncelleme: " + U.TS(F.guncelleme);
            G.T(g, t, Th.F(8.5f), new Rectangle(x, 0, Width - x, Height), Th.Sub, G.L);
        }
        int Pill(Graphics g, int x, string t, Color c)
        {
            int w = G.TW(t, Th.F(8.5f, true)) + Th.D(18);
            var r = new RectangleF(x, (Height - Th.D(22)) / 2f, w, Th.D(22));
            G.Fill(g, r, r.Height / 2, Th.Tint(c, .14f));
            G.T(g, t, Th.F(8.5f, true), Rectangle.Round(r), Th.Dark(c, .15f), G.C);
            return x + w;
        }
    }

    // Tek bir iş akışı aşaması (kart)
    class StageCard : BaseCtl
    {
        public Firm F; public int S; public int NoteCount; public bool IsCurrent; public int Num;
        public event EventHandler Changed;
        public event Action<string> NotesClick;
        public event EventHandler KusurDateClick;
        Rectangle kusurDateR;
        readonly List<Rectangle> rows = new List<Rectangle>();
        readonly List<Rectangle> naLinks = new List<Rectangle>();
        readonly List<Rectangle> dateLinks = new List<Rectangle>();
        public event Action<int, Point> ItemDateClick;
        Rectangle noteChip;
        const TextFormatFlags WF = G.W;

        public StageCard() { Cursor = Cursors.Hand; }

        public int Calc(int width)
        {
            rows.Clear(); naLinks.Clear(); dateLinks.Clear();
            var st = Flow.ViewOf(F, S);
            int pad = Th.D(12), y = Th.D(46);
            int textW = width - pad * 2 - Th.D(30) - Th.D(S == 3 ? 86 : 70);
            for (int i = 0; i < st.Items.Length; i++)
            {
                int th = G.M(st.Items[i], Th.F(9.25f), textW, WF).Height;
                int h = Math.Max(Th.D(34), th + Th.D(14));
                rows.Add(new Rectangle(Th.D(6), y, width - Th.D(12), h));
                naLinks.Add(Rectangle.Empty); dateLinks.Add(Rectangle.Empty);
                y += h;
            }
            return y + Th.D(8);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            if (F == null) return;
            Calc(Width);
            var st = Flow.ViewOf(F, S);
            bool done = Flow.StageDone(F, S);
            var bg = new RectangleF(0, 0, Width - 1, Height - 1);
            G.Fill(g, bg, Th.D(12), Th.Soft);
            if (IsCurrent) G.Stroke(g, new RectangleF(.75f, .75f, Width - 2.5f, Height - 2.5f), Th.D(12), Th.Tint(st.Color, .55f), Th.K * 1.5f);

            // başlık
            int pad = Th.D(12); float cs = Th.D(24);
            var c = new RectangleF(pad, Th.D(11), cs, cs);
            if (done) { G.Circle(g, c, Th.Green); G.Icon(g, "check", new RectangleF(c.X + cs * .18f, c.Y + cs * .18f, cs * .64f, cs * .64f), Color.White, Th.K * 2f); }
            else if (IsCurrent) { G.Circle(g, c, st.Color); G.T(g, (Num > 0 ? Num : S + 1).ToString(), Th.F(9f, true), Rectangle.Round(c), Color.White, G.C); }
            else { G.Ring(g, new RectangleF(c.X + 1, c.Y + 1, cs - 2, cs - 2), Th.H("#C7C7CC"), Th.K * 1.4f); G.T(g, (Num > 0 ? Num : S + 1).ToString(), Th.F(9f, true), Rectangle.Round(c), Th.Sub, G.C); }

            // not çipi
            string nt = NoteCount > 0 ? NoteCount.ToString() : "Not";
            int nw = G.TW(nt, Th.F(8.5f, true)) + Th.D(34);
            noteChip = new Rectangle(Width - pad - nw, Th.D(12), nw, Th.D(22));
            bool nh = noteChip.Contains(hov);
            G.Fill(g, noteChip, noteChip.Height / 2f, NoteCount > 0 ? Th.Tint(st.Color, nh ? .24f : .14f) : (nh ? Th.H("#E5E5EA") : Th.H("#EDEDF2")));
            Color nfg = NoteCount > 0 ? Th.Dark(st.Color, .1f) : Th.GrayDark;
            G.Icon(g, "note", new RectangleF(noteChip.X + Th.D(8), noteChip.Y + Th.D(4), Th.D(14), Th.D(14)), nfg, Th.K * 1.4f);
            G.T(g, nt, Th.F(8.5f, true), new Rectangle(noteChip.X + Th.D(26), noteChip.Y, nw, noteChip.Height), nfg, G.L);

            string cnt;
            if (st.Radio) cnt = F.kusur == Flow.KusurluTam ? "Kusurlu kapandı" : done ? "Tamam" : (F.kusur == Flow.KusurVar ? "Bekleniyor" : "Seçilmedi");
            else cnt = Flow.StageCount(F, S) + "/" + Flow.StageTotal(F, S);
            int cw = G.TW(cnt, Th.F(8.5f));
            G.T(g, cnt, Th.F(8.5f), new Rectangle(noteChip.X - cw - Th.D(8), Th.D(11), cw + 2, (int)cs), Th.Sub, G.L);
            G.T(g, st.Title, Th.F(10f, true), new Rectangle((int)(c.Right + Th.D(10)), Th.D(11), noteChip.X - cw - Th.D(14) - (int)(c.Right + Th.D(10)), (int)cs), Th.Text, G.L);

            // satırlar
            int textW = Width - pad * 2 - Th.D(30) - Th.D(S == 3 ? 86 : 70);
            for (int i = 0; i < st.Items.Length; i++)
            {
                var r = rows[i];
                bool h = r.Contains(hov);
                if (h) G.Fill(g, r, Th.D(8), Th.H("#EFEFF4"));
                float d = Th.D(20);
                var cr = new RectangleF(pad, r.Y + (r.Height - d) / 2f, d, d);
                bool on, na = false;
                Color onCol = st.Color;
                if (st.Radio)
                {
                    on = F.kusur == st.Items[i];
                    onCol = i == 1 ? Th.Orange : (i == 3 ? Flow.Brown : Th.Green);
                    if (on) { G.Circle(g, cr, onCol); G.Circle(g, new RectangleF(cr.X + d * .3f, cr.Y + d * .3f, d * .4f, d * .4f), Color.White); }
                    else G.Ring(g, new RectangleF(cr.X + .75f, cr.Y + .75f, d - 1.5f, d - 1.5f), Th.H("#C7C7CC"), Th.K * 1.5f);
                }
                else
                {
                    on = Flow.Done(F, S, i); na = Flow.NA(F, S, i);
                    if (na) { G.Circle(g, cr, Th.H("#C7C7CC")); G.Icon(g, "minus", new RectangleF(cr.X + d * .15f, cr.Y + d * .15f, d * .7f, d * .7f), Color.White, Th.K * 2f); }
                    else if (on) { G.Circle(g, cr, onCol); G.Icon(g, "check", new RectangleF(cr.X + d * .17f, cr.Y + d * .17f, d * .66f, d * .66f), Color.White, Th.K * 2f); }
                    else G.Ring(g, new RectangleF(cr.X + .75f, cr.Y + .75f, d - 1.5f, d - 1.5f), Th.H("#C7C7CC"), Th.K * 1.5f);
                }
                string txt = st.Items[i];
                var ts = G.M(txt, Th.F(9.25f), textW, WF);
                var tr = new Rectangle(pad + Th.D(30), r.Y + (r.Height - ts.Height) / 2, textW, ts.Height + 2);
                G.T(g, txt, Th.F(9.25f), tr, na ? Th.Gray : (on && !st.Radio ? Th.Sub : Th.Text), WF);

                // sağ taraf: tarih / kapsam dışı bağlantısı
                var rr = new Rectangle(r.Right - Th.D(S == 3 ? 86 : 72), r.Y, Th.D(S == 3 ? 80 : 66), r.Height);
                if (S == 3 && (h || na) && !(on && !na))
                {
                    string lt = na ? "Gerekli yap" : "Gerekmiyor";
                    int lw = G.TW(lt, Th.F(8.25f, true));
                    var lr = new Rectangle(rr.Right - lw, r.Y + (r.Height - Th.D(18)) / 2, lw, Th.D(18));
                    naLinks[i] = new Rectangle(lr.X - Th.D(4), lr.Y - Th.D(2), lr.Width + Th.D(8), lr.Height + Th.D(4));
                    G.T(g, lt, Th.F(8.25f, true), lr, Th.Blue, G.R);
                }
                else
                {
                    naLinks[i] = Rectangle.Empty;
                    if (st.Radio && on && i == 1)
                    {
                        int? kg = Flow.KusurGun(F);
                        string kt = !kg.HasValue ? "Tarih gir" : kg.Value < 0 ? "Başlamadı" : kg.Value + ". gün";
                        Color kc = !kg.HasValue || kg.Value < 0 ? Th.Blue : kg.Value >= Flow.KUSUR_GUN ? Th.Red : (kg.Value >= 15 ? Th.Orange : Th.Blue);
                        int kw = G.TW(kt, Th.F(8.25f, true)) + Th.D(22);
                        kusurDateR = new Rectangle(rr.Right - kw, r.Y + (r.Height - Th.D(22)) / 2, kw, Th.D(22));
                        G.Fill(g, kusurDateR, kusurDateR.Height / 2f, Th.Tint(kc, kusurDateR.Contains(hov) ? .22f : .12f));
                        G.Icon(g, "cal", new RectangleF(kusurDateR.X + Th.D(6), kusurDateR.Y + Th.D(5), Th.D(12), Th.D(12)), kc, Th.K * 1.3f);
                        G.T(g, kt, Th.F(8.25f, true), new Rectangle(kusurDateR.X + Th.D(19), kusurDateR.Y, kw, kusurDateR.Height), Th.Dark(kc, .1f), G.L);
                    }
                    else if (st.Radio && i == 1) kusurDateR = Rectangle.Empty;
                    if (!st.Radio && on && !na)
                    {
                        var dt = U.PT(F.checks[Flow.Key(F, S, i)]);
                        string ds = dt.HasValue ? dt.Value.ToString("dd.MM.yyyy") : "Tarih gir";
                        int dw = G.TW(ds, Th.F(8f)) + Th.D(12);
                        var dr = new Rectangle(rr.Right - dw + Th.D(6), r.Y + (r.Height - Th.D(20)) / 2, dw, Th.D(20));
                        dateLinks[i] = dr;
                        bool dh = dr.Contains(hov);
                        if (dh) G.Fill(g, dr, dr.Height / 2f, Th.Tint(Th.Blue, .14f));
                        G.T(g, ds, Th.F(8f), dr, dh ? Th.Blue : Th.Gray, G.C);
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (F == null || e.Button != MouseButtons.Left) return;
            var st = Flow.ViewOf(F, S);
            if (noteChip.Contains(e.Location)) { if (NotesClick != null) NotesClick(st.Key); return; }
            if (st.Radio && F.kusur == Flow.KusurVar && kusurDateR.Contains(e.Location)) { if (KusurDateClick != null) KusurDateClick(this, EventArgs.Empty); return; }
            for (int i = 0; i < rows.Count; i++)
            {
                string k = Flow.Key(F, S, i);
                if (!st.Radio && i < dateLinks.Count && dateLinks[i] != Rectangle.Empty && dateLinks[i].Contains(e.Location) && Flow.Done(F, S, i) && !Flow.NA(F, S, i))
                { if (ItemDateClick != null) ItemDateClick(i, new Point(dateLinks[i].Left, dateLinks[i].Bottom)); return; }
                if (S == 3 && i < naLinks.Count && naLinks[i] != Rectangle.Empty && naLinks[i].Contains(e.Location))
                {
                    if (Flow.NA(F, S, i)) F.checks.Remove(k); else F.checks[k] = "na";
                    Fire(); return;
                }
                if (!rows[i].Contains(e.Location)) continue;
                if (st.Radio)
                {
                    F.kusur = F.kusur == st.Items[i] ? "" : st.Items[i];
                    if (F.kusur == Flow.KusurVar && (i == 1)) F.kusurTarih = U.Now();
                }
                else if (Flow.Done(F, S, i)) F.checks.Remove(k);
                else F.checks[k] = U.Now();
                Fire(); return;
            }
        }
        void Fire() { Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); }
    }

    // Not listesi
    class NoteList : BaseCtl
    {
        public List<Note> Items = new List<Note>();
        public Note Editing;
        public string EmptyText = "Henüz not yok.";
        public event Action<Note> EditClick, DeleteClick;
        readonly List<Rectangle> boxes = new List<Rectangle>(), trash = new List<Rectangle>();
        public NoteList() { Cursor = Cursors.Hand; }

        public int Calc(int width)
        {
            boxes.Clear(); trash.Clear();
            if (Items.Count == 0) return Th.D(54);
            int y = 0, pad = Th.D(12);
            foreach (var n in Items)
            {
                int th = G.M(n.metin, Th.F(9.5f), width - pad * 2, G.W).Height;
                int h = pad + Th.D(22) + Th.D(6) + th + pad;
                boxes.Add(new Rectangle(0, y, width - 1, h));
                trash.Add(new Rectangle(width - pad - Th.D(24), y + Th.D(8), Th.D(26), Th.D(26)));
                y += h + Th.D(8);
            }
            return y;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            Calc(Width);
            if (Items.Count == 0)
            {
                G.Fill(g, new RectangleF(0, 0, Width - 1, Height - 1), Th.D(12), Th.Soft);
                G.T(g, EmptyText, Th.F(9f), ClientRectangle, Th.Gray, G.C);
                return;
            }
            int pad = Th.D(12);
            for (int i = 0; i < Items.Count; i++)
            {
                var n = Items[i]; var b = boxes[i];
                bool h = b.Contains(hov);
                G.Fill(g, b, Th.D(12), Th.Soft);
                if (n == Editing) G.Stroke(g, new RectangleF(b.X + .75f, b.Y + .75f, b.Width - 1.5f, b.Height - 1.5f), Th.D(12), Th.Blue, Th.K * 1.5f);
                else if (h) G.Stroke(g, new RectangleF(b.X + .5f, b.Y + .5f, b.Width - 1, b.Height - 1), Th.D(12), Th.H("#DCDCE2"), 1);
                Color col = Flow.TopicColor(n.asama);
                string tn = Flow.TopicName(n.asama);
                int pw = G.TW(tn, Th.F(8.25f, true)) + Th.D(16);
                var pr = new RectangleF(b.X + pad, b.Y + pad, pw, Th.D(22));
                G.Fill(g, pr, pr.Height / 2, Th.Tint(col, .15f));
                G.T(g, tn, Th.F(8.25f, true), Rectangle.Round(pr), Th.Dark(col, .15f), G.C);
                string dt = U.TS(n.tarih) + (n == Editing ? "  ·  düzenleniyor" : "");
                G.T(g, dt, Th.F(8.25f), new Rectangle((int)pr.Right + Th.D(8), (int)pr.Y, b.Width - pw - Th.D(70), (int)pr.Height), n == Editing ? Th.Blue : Th.Gray, G.L);
                if (h || n == Editing)
                {
                    var tr = trash[i];
                    if (tr.Contains(hov)) G.Circle(g, tr, Th.Tint(Th.Red, .14f));
                    G.Icon(g, "trash", new RectangleF(tr.X + Th.D(5), tr.Y + Th.D(5), Th.D(16), Th.D(16)), Th.Red, Th.K * 1.5f);
                }
                var txr = new Rectangle(b.X + pad, (int)pr.Bottom + Th.D(6), b.Width - pad * 2, b.Height - Th.D(22) - pad * 2 - Th.D(6) + 2);
                G.T(g, n.metin, Th.F(9.5f), txr, Th.Text, G.W);
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            for (int i = 0; i < boxes.Count; i++)
            {
                if (trash[i].Contains(e.Location)) { if (DeleteClick != null) DeleteClick(Items[i]); return; }
                if (boxes[i].Contains(e.Location)) { if (EditClick != null) EditClick(Items[i]); return; }
            }
        }
    }

    // Firma listesi (kendi çizimli, kaydırılabilir)
    class FirmList : BaseCtl
    {
        public List<Firm> Items = new List<Firm>();
        public Firm Selected;
        public string EmptyText = "";
        public event EventHandler SelChanged;
        public event Action<Firm, Point> RightClick;
        int off; bool drag; int dragY, dragOff;
        int RowH { get { return Th.D(70); } }
        int Top0 { get { return Th.D(2); } }
        public FirmList() { SetStyle(ControlStyles.Selectable, true); TabStop = true; }
        int ContentH { get { return Items.Count * RowH + Top0 * 2; } }
        int MaxOff { get { return Math.Max(0, ContentH - Height); } }
        public void SetItems(List<Firm> l) { Items = l; off = Math.Max(0, Math.Min(off, MaxOff)); Invalidate(); }
        public void ScrollBy(int d) { off = Math.Max(0, Math.Min(MaxOff, off + d)); Invalidate(); }
        public void EnsureVisible()
        {
            int i = Items.IndexOf(Selected); if (i < 0) return;
            int t = Top0 + i * RowH;
            if (t < off) off = t; else if (t + RowH > off + Height) off = t + RowH - Height + Top0;
            off = Math.Max(0, Math.Min(off, MaxOff)); Invalidate();
        }
        public void Select(Firm f, bool raise)
        {
            if (Selected == f) return;
            Selected = f; EnsureVisible(); Invalidate();
            if (raise && SelChanged != null) SelChanged(this, EventArgs.Empty);
        }
        public void MoveSel(int d)
        {
            if (Items.Count == 0) return;
            int i = Items.IndexOf(Selected);
            i = i < 0 ? (d > 0 ? 0 : Items.Count - 1) : Math.Max(0, Math.Min(Items.Count - 1, i + d));
            Select(Items[i], true);
        }
        Rectangle Thumb()
        {
            if (ContentH <= Height) return Rectangle.Empty;
            int th = Math.Max(Th.D(36), Height * Height / ContentH);
            int ty = (int)((Height - th) * (off / (float)MaxOff));
            return new Rectangle(Width - Th.D(7), ty, Th.D(5), th);
        }
        protected override bool IsInputKey(Keys k)
        {
            switch (k & Keys.KeyCode) { case Keys.Up: case Keys.Down: case Keys.PageUp: case Keys.PageDown: case Keys.Home: case Keys.End: return true; }
            return base.IsInputKey(k);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            int page = Math.Max(1, Height / RowH - 1);
            switch (e.KeyCode)
            {
                case Keys.Up: MoveSel(-1); break; case Keys.Down: MoveSel(1); break;
                case Keys.PageUp: MoveSel(-page); break; case Keys.PageDown: MoveSel(page); break;
                case Keys.Home: MoveSel(-100000); break; case Keys.End: MoveSel(100000); break;
            }
            base.OnKeyDown(e);
        }
        protected override void OnMouseWheel(MouseEventArgs e) { ScrollBy(-e.Delta / 120 * RowH); base.OnMouseWheel(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            var t = Thumb();
            if (t != Rectangle.Empty && e.X >= Width - Th.D(12)) { drag = true; dragY = e.Y; dragOff = off; return; }
            int i = (e.Y + off - Top0) / RowH;
            if (i >= 0 && i < Items.Count && e.Y + off - Top0 >= 0)
            {
                Select(Items[i], true);
                if (e.Button == MouseButtons.Right && RightClick != null) RightClick(Items[i], e.Location);
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (drag && MaxOff > 0)
            {
                var t = Thumb();
                float ratio = MaxOff / (float)Math.Max(1, Height - t.Height);
                off = Math.Max(0, Math.Min(MaxOff, dragOff + (int)((e.Y - dragY) * ratio)));
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e) { drag = false; base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            if (Items.Count == 0)
            {
                G.T(g, EmptyText, Th.F(9.5f), new Rectangle(Th.D(20), 0, Width - Th.D(40), Th.D(120)), Th.Gray, G.C | TextFormatFlags.WordBreak);
                return;
            }
            int first = Math.Max(0, (off - Top0) / RowH);
            for (int i = first; i < Items.Count; i++)
            {
                int y = Top0 + i * RowH - off;
                if (y > Height) break;
                DrawRow(g, Items[i], new Rectangle(Th.D(6), y, Width - Th.D(14), RowH), i);
            }
            var t = Thumb();
            if (t != Rectangle.Empty && (ClientRectangle.Contains(hov) || drag))
                G.Fill(g, t, t.Width / 2f, Color.FromArgb(drag ? 120 : 80, 0, 0, 0));
        }

        void DrawRow(Graphics g, Firm f, Rectangle r, int idx)
        {
            bool sel = f == Selected, h = r.Contains(hov) && !drag;
            var cat = Flow.Category(f); int cur = Flow.Bucket(f); int pct = Flow.Percent(f);
            if (sel) G.Fill(g, new RectangleF(r.X, r.Y + 2, r.Width, r.Height - 4), Th.D(12), Focused ? Th.Tint(Th.Blue, .13f) : Th.Tint(Th.Blue, .09f));
            else if (h) G.Fill(g, new RectangleF(r.X, r.Y + 2, r.Width, r.Height - 4), Th.D(12), Th.Soft);
            else if (idx > 0 && Items[idx - 1] != Selected)
                using (var p = new Pen(Th.Sep)) g.DrawLine(p, r.X + Th.D(60), r.Y, r.Right - Th.D(8), r.Y);

            // avatar
            Color sc = cat == Flow.Cat.Iptal ? Th.Gray : Flow.StageColor(cur);
            float a = Th.D(40);
            var ar = new RectangleF(r.X + Th.D(10), r.Y + (r.Height - a) / 2f, a, a);
            G.Circle(g, ar, Th.Tint(sc, .16f));
            G.T(g, Initials(f.ad), Th.F(10f, true), Rectangle.Round(ar), Th.Dark(sc, .1f), G.C);

            // sağ: durum hapı + tarih
            string ps = Flow.CatShort(cat); Color pc = Flow.CatColor(cat);
            int? kgun = Flow.KusurGun(f);
            if (Flow.Idle(f)) { ps = "Beklemede · " + Flow.IdleDays(f) + " g"; pc = Th.H("#C99A00"); }
            if (cat == Flow.Cat.Kusur && kgun.HasValue && kgun.Value >= 0) ps = "Kusur · " + kgun.Value + ". gün";
            int pw = G.TW(ps, Th.F(8f, true)) + Th.D(16);
            var pr = new RectangleF(r.Right - Th.D(10) - pw, r.Y + Th.D(14), pw, Th.D(20));
            G.Fill(g, pr, pr.Height / 2, Th.Tint(pc, .15f));
            G.T(g, ps, Th.F(8f, true), Rectangle.Round(pr), Th.Dark(pc, .15f), G.C);
            string dt = "";
            var days = Flow.Days(f); var ed = U.PD(f.kesifTarih);
            var next = days.FirstOrDefault(q => q >= DateTime.Today);
            if (next != default(DateTime)) dt = "Kontrol " + next.ToString("dd.MM") + (days.Count > 1 ? " (" + days.Count + " gün)" : "");
            else if (days.Count > 0 && f.kontrolDevam && !Flow.StageDone(f, 3)) dt = "Kontrol sürüyor";
            else if (days.Count > 0) dt = "Kontrol " + days[days.Count - 1].ToString("dd.MM") + (days.Count > 1 ? " (" + days.Count + " gün)" : "");
            else if (ed.HasValue) dt = "Keşif " + ed.Value.ToString("dd.MM");
            if (f.ucret == Flow.Ucretsiz) dt = dt == "" ? "Ücretsiz" : "Ücretsiz · " + dt;
            int dw = G.TW(dt, Th.F(8.25f));
            G.T(g, dt, Th.F(8.25f), new Rectangle(r.Right - Th.D(10) - dw - 2, r.Y + Th.D(38), dw + 4, Th.D(18)), Th.Sub, G.R);

            // orta: ad + aşama + ilerleme
            int x = (int)ar.Right + Th.D(12);
            int wName = (int)pr.X - x - Th.D(8);
            G.T(g, string.IsNullOrWhiteSpace(f.ad) ? "(adsız firma)" : f.ad, Th.F(10f, true), new Rectangle(x, r.Y + Th.D(12), wName, Th.D(22)), cat == Flow.Cat.Iptal ? Th.Sub : Th.Text, G.L);
            string sub = cat == Flow.Cat.Iptal ? "İptal / olumsuz" : (cur == 7 ? "Kusurlu olarak kapatıldı" : cur == Flow.DONE ? "Tüm aşamalar tamam" : Flow.ViewOf(f, cur).Short);
            int wSub = r.Right - Th.D(10) - dw - Th.D(12) - x;
            int bw = Math.Min(Th.D(90), Math.Max(Th.D(40), wSub - Th.D(110)));
            G.T(g, sub, Th.F(8.5f), new Rectangle(x, r.Y + Th.D(37), wSub - bw - Th.D(10), Th.D(18)), Th.Sub, G.L);
            var bar = new RectangleF(x + wSub - bw, r.Y + Th.D(44), bw, Th.D(5));
            if (wSub > Th.D(130))
            {
                G.Fill(g, bar, bar.Height / 2, Th.Track);
                if (pct > 0) G.Fill(g, new RectangleF(bar.X, bar.Y, Math.Max(bar.Height, bar.Width * pct / 100f), bar.Height), bar.Height / 2, cat == Flow.Cat.Iptal ? Th.Gray : sc);
            }
        }
        static string Initials(string ad)
        {
            var p = (ad ?? "").Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 0) return "?";
            string s = p[0].Substring(0, 1) + (p.Length > 1 && char.IsLetter(p[1][0]) ? p[1].Substring(0, 1) : (p[0].Length > 1 ? p[0].Substring(1, 1) : ""));
            return U.Up(s);
        }
    }

    // Aşama dağılımı – halka grafik + tıklanabilir açıklama
    class Donut : BaseCtl
    {
        public class Seg { public int Key; public string Label; public Color Color; public int Count; }
        public List<Seg> Segs = new List<Seg>();
        public int SelKey = -1;
        public event Action<int> SegClick;
        readonly List<Rectangle> legend = new List<Rectangle>();
        public Donut() { Cursor = Cursors.Hand; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            int total = Segs.Sum(s => s.Count);
            float size = Math.Min(Th.D(100), Height - Th.D(4));
            var r = new RectangleF(Th.D(2), (Height - size) / 2f, size, size);
            float w = Th.D(13);
            var rr = new RectangleF(r.X + w / 2, r.Y + w / 2, r.Width - w, r.Height - w);
            using (var p = new Pen(Th.Track, w)) g.DrawEllipse(p, rr);
            if (total > 0)
            {
                float a = -90; float gap = Segs.Count(s => s.Count > 0) > 1 ? 2.5f : 0;
                foreach (var s in Segs)
                {
                    if (s.Count == 0) continue;
                    float sw = 360f * s.Count / total;
                    Color c = SelKey >= 0 && SelKey != s.Key ? Th.Tint(s.Color, .35f) : s.Color;
                    using (var p = new Pen(c, w)) g.DrawArc(p, rr, a + gap / 2, Math.Max(.5f, sw - gap));
                    a += sw;
                }
            }
            G.T(g, total.ToString(), Th.F(16f, true), new Rectangle((int)r.X, (int)(r.Y + size / 2 - Th.D(22)), (int)size, Th.D(28)), Th.Text, G.C);
            G.T(g, "aktif firma", Th.F(8f), new Rectangle((int)r.X, (int)(r.Y + size / 2 + Th.D(4)), (int)size, Th.D(16)), Th.Sub, G.C);

            legend.Clear();
            int lx = (int)r.Right + Th.D(14), lw = Width - lx;
            int lh = Th.D(19); int ly = (Height - lh * Segs.Count) / 2;
            foreach (var s in Segs)
            {
                var lr = new Rectangle(lx - Th.D(6), ly, lw + Th.D(6), lh);
                legend.Add(lr);
                bool sel = SelKey == s.Key, h = lr.Contains(hov);
                if (sel || h) G.Fill(g, lr, Th.D(6), sel ? Th.Tint(s.Color, .14f) : Th.Soft);
                float d = Th.D(8);
                G.Circle(g, new RectangleF(lx, ly + (lh - d) / 2f, d, d), s.Color);
                G.T(g, s.Label, sel ? Th.F(8.5f, true) : Th.F(8.5f), new Rectangle(lx + Th.D(13), ly, lw - Th.D(34), lh), Th.Text, G.L);
                G.T(g, s.Count.ToString(), Th.F(8.5f, true), new Rectangle(lx, ly, lw - Th.D(6), lh), s.Count > 0 ? Th.Text : Th.Gray, G.R);
                ly += lh;
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            for (int i = 0; i < legend.Count; i++)
                if (legend[i].Contains(e.Location)) { if (SegClick != null) SegClick(Segs[i].Key); return; }
            base.OnMouseUp(e);
        }
    }

    // Yaklaşan tarihler ve takip hatırlatmaları
    class Upcoming : BaseCtl
    {
        public class Item { public Firm F; public DateTime? Date; public string Text; public Color Color; }
        public List<Item> Items = new List<Item>();
        public event Action<Firm> Pick;
        readonly List<Rectangle> rows = new List<Rectangle>();
        static readonly string[] Ay = { "OCA", "ŞUB", "MAR", "NİS", "MAY", "HAZ", "TEM", "AĞU", "EYL", "EKİ", "KAS", "ARA" };
        public Upcoming() { Cursor = Cursors.Hand; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            rows.Clear();
            if (Items.Count == 0) { G.T(g, "Yaklaşan tarih veya takip yok.", Th.F(8.75f), new Rectangle(0, 0, Width, Th.D(40)), Th.Gray, G.L); return; }
            int y = 0, rh = Th.D(46);
            foreach (var it in Items)
            {
                if (y + rh > Height + 2) break;
                var r = new Rectangle(0, y, Width, rh); rows.Add(r);
                if (r.Contains(hov)) G.Fill(g, new RectangleF(r.X - Th.D(4), r.Y, r.Width + Th.D(4), r.Height - Th.D(2)), Th.D(8), Th.Soft);
                Color c = it.Color;
                var b = new RectangleF(0, y + Th.D(5), Th.D(36), Th.D(36));
                G.Fill(g, b, Th.D(9), Th.Tint(c, .14f));
                if (it.Date.HasValue)
                {
                    G.T(g, Ay[it.Date.Value.Month - 1], Th.F(6.75f, true), new Rectangle((int)b.X, (int)b.Y + Th.D(3), (int)b.Width, Th.D(12)), Th.Dark(c, .1f), G.C);
                    G.T(g, it.Date.Value.Day.ToString(), Th.F(11f, true), new Rectangle((int)b.X, (int)b.Y + Th.D(13), (int)b.Width, Th.D(20)), Th.Dark(c, .15f), G.C);
                }
                else G.T(g, "?", Th.F(13f, true), Rectangle.Round(b), Th.Dark(c, .1f), G.C);
                int x = (int)b.Right + Th.D(10);
                G.T(g, it.F.ad, Th.F(9f, true), new Rectangle(x, y + Th.D(5), Width - x, Th.D(18)), Th.Text, G.L);
                G.T(g, it.Text, Th.F(8.25f), new Rectangle(x, y + Th.D(23), Width - x, Th.D(16)), c == Th.Red ? Th.Red : Th.Sub, G.L);
                y += rh;
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Contains(e.Location)) { if (Pick != null) Pick(Items[i].F); return; }
            base.OnMouseUp(e);
        }
    }

    // Kontrol günleri çipleri + "devam edecek" anahtarı
    class DayChips : BaseCtl
    {
        public Firm F;
        public event EventHandler Changed;
        readonly List<Rectangle> xs = new List<Rectangle>(); Rectangle tog;
        List<Rectangle> rects = new List<Rectangle>();
        public DayChips() { Cursor = Cursors.Hand; }
        const string TogText = "Devam edecek – sonraki gün belirsiz";
        public int Lay(int width)
        {
            rects = new List<Rectangle>();
            int x = 0, y = 0, h = Th.D(28), gap = Th.D(6);
            if (F != null)
                foreach (var d in Flow.Days(F))
                {
                    int w = G.TW(d.ToString("dd.MM.yyyy"), Th.F(8.75f, true)) + Th.D(46);
                    if (x + w > width && x > 0) { x = 0; y += h + gap; }
                    rects.Add(new Rectangle(x, y, w, h)); x += w + gap;
                }
            int tw = G.TW(TogText, Th.F(8.75f, true)) + Th.D(40);
            if (x + tw > width && x > 0) { x = 0; y += h + gap; }
            tog = new Rectangle(x, y, tw, h);
            return y + h + 1;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            if (F == null) return;
            Lay(Width); xs.Clear();
            var days = Flow.Days(F);
            for (int i = 0; i < days.Count; i++)
            {
                var r = rects[i]; bool past = days[i] < DateTime.Today, today = days[i] == DateTime.Today;
                Color c = today ? Th.Orange : (past ? Th.GrayDark : Th.Orange);
                G.Fill(g, new RectangleF(r.X, r.Y, r.Width - 1, r.Height - 1), r.Height / 2f, past ? Th.Field : Th.Tint(c, .14f));
                if (past) G.Icon(g, "check", new RectangleF(r.X + Th.D(9), r.Y + Th.D(7), Th.D(14), Th.D(14)), Th.Green, Th.K * 1.8f);
                else G.Icon(g, "cal", new RectangleF(r.X + Th.D(9), r.Y + Th.D(7), Th.D(14), Th.D(14)), Th.Dark(c, .1f), Th.K * 1.5f);
                G.T(g, days[i].ToString("dd.MM.yyyy"), Th.F(8.75f, true), new Rectangle(r.X + Th.D(27), r.Y, r.Width, r.Height), past ? Th.Sub : Th.Dark(c, .2f), G.L);
                var xr = new Rectangle(r.Right - Th.D(22), r.Y + Th.D(6), Th.D(16), Th.D(16)); xs.Add(xr);
                if (xr.Contains(hov)) G.Circle(g, xr, Th.Tint(Th.Red, .2f));
                G.Icon(g, "x", new RectangleF(xr.X + Th.D(2), xr.Y + Th.D(2), Th.D(12), Th.D(12)), xr.Contains(hov) ? Th.Red : Th.GrayDark, Th.K * 1.5f);
            }
            bool on = F.kontrolDevam, h = tog.Contains(hov);
            G.Fill(g, new RectangleF(tog.X, tog.Y, tog.Width - 1, tog.Height - 1), tog.Height / 2f, on ? Th.Tint(Th.Blue, .14f) : (h ? Th.H("#E8E8ED") : Th.Field));
            var cb = new RectangleF(tog.X + Th.D(9), tog.Y + Th.D(7), Th.D(14), Th.D(14));
            if (on) { G.Fill(g, cb, Th.D(4), Th.Blue); G.Icon(g, "check", new RectangleF(cb.X + 1, cb.Y + 1, cb.Width - 2, cb.Height - 2), Color.White, Th.K * 1.8f); }
            else G.Stroke(g, cb, Th.D(4), Th.H("#C7C7CC"), Th.K * 1.4f);
            G.T(g, TogText, Th.F(8.75f, true), new Rectangle(tog.X + Th.D(29), tog.Y, tog.Width, tog.Height), on ? Th.Blue : Th.Text, G.L);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (F == null || e.Button != MouseButtons.Left) return;
            var days = Flow.Days(F);
            for (int i = 0; i < xs.Count && i < days.Count; i++)
                if (xs[i].Contains(e.Location))
                {
                    F.kontrolGunleri.Remove(days[i].ToString("yyyy-MM-dd")); Flow.SyncKontrol(F);
                    Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); return;
                }
            if (tog.Contains(e.Location)) { F.kontrolDevam = !F.kontrolDevam; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); }
        }
    }

    // Beyaz menü görünümü
    class MenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get { return Th.Tint(Th.Blue, .12f); } }
        public override Color MenuItemBorder { get { return Th.Tint(Th.Blue, .12f); } }
        public override Color MenuBorder { get { return Th.Sep; } }
        public override Color ToolStripDropDownBackground { get { return Color.White; } }
        public override Color ImageMarginGradientBegin { get { return Color.White; } }
        public override Color ImageMarginGradientMiddle { get { return Color.White; } }
        public override Color ImageMarginGradientEnd { get { return Color.White; } }
        public override Color SeparatorDark { get { return Th.Sep; } }
        public override Color SeparatorLight { get { return Color.White; } }
        public override Color CheckBackground { get { return Th.Tint(Th.Blue, .15f); } }
        public override Color CheckSelectedBackground { get { return Th.Tint(Th.Blue, .25f); } }
        public override Color CheckPressedBackground { get { return Th.Tint(Th.Blue, .25f); } }
    }

    // İçeriği kaydırılan panel, ince iOS tarzı kaydırma çubuğu ile
    class Scroller : Panel
    {
        public Panel Content = new Panel { BackColor = Color.White, Location = Point.Empty };
        readonly ScrollThumb thumb;
        int off;
        public Scroller()
        {
            BackColor = Color.White; DoubleBuffered = true;
            Controls.Add(Content);
            thumb = new ScrollThumb(this); Controls.Add(thumb); thumb.BringToFront();
        }
        public int Off { get { return off; } }
        public int MaxOff { get { return Math.Max(0, Content.Height - Height); } }
        public void SetOff(int v)
        {
            off = Math.Max(0, Math.Min(MaxOff, v));
            Content.Top = -off; thumb.Upd();
        }
        public void ScrollBy(int d) { SetOff(off + d); }
        public void EnsureVisible(Control c)
        {
            var p = Content.PointToClient(c.Parent.PointToScreen(c.Location));
            int top = p.Y, bot = p.Y + c.Height;
            if (top - Th.D(20) < off) SetOff(top - Th.D(20));
            else if (bot + Th.D(20) > off + Height) SetOff(bot + Th.D(20) - Height);
        }
        public void ContentChanged() { Content.Width = ClientSize.Width; SetOff(off); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); thumb.SetBounds(Width - Th.D(9), 0, Th.D(9), Height); SetOff(off); }
        protected override void OnMouseWheel(MouseEventArgs e) { ScrollBy(-e.Delta / 120 * Th.D(70)); }

        class ScrollThumb : BaseCtl
        {
            readonly Scroller s; bool drag; int dy, doff;
            public ScrollThumb(Scroller sc) { s = sc; BackColor = Color.White; Cursor = Cursors.Default; }
            public void Upd() { Visible = s.MaxOff > 0; Invalidate(); }
            Rectangle T()
            {
                if (s.MaxOff <= 0) return Rectangle.Empty;
                int h = Math.Max(Th.D(40), Height * Height / Math.Max(1, s.Content.Height));
                int y = (int)((Height - h) * (s.Off / (float)s.MaxOff));
                return new Rectangle(Th.D(2), y, Th.D(5), h);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = Begin(e); var t = T();
                if (t != Rectangle.Empty) G.Fill(g, t, t.Width / 2f, Color.FromArgb(drag || ClientRectangle.Contains(hov) ? 110 : 55, 0, 0, 0));
            }
            protected override void OnMouseDown(MouseEventArgs e) { drag = true; dy = e.Y; doff = s.Off; }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (drag) { var t = T(); float ratio = s.MaxOff / (float)Math.Max(1, Height - t.Height); s.SetOff(doff + (int)((e.Y - dy) * ratio)); }
                base.OnMouseMove(e);
            }
            protected override void OnMouseUp(MouseEventArgs e) { drag = false; Invalidate(); }
        }
    }
}
