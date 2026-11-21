using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PKT
{
    static class Chart
    {
        // Gruplu / tekli sütun grafiği. values[grup][seri]
        public static List<Rectangle> Bars(Graphics g, Rectangle r, string[] labels, int[][] values, Color[] colors, int sel, Point hov)
        {
            var hits = new List<Rectangle>();
            int n = labels.Length; if (n == 0) return hits;
            int max = Math.Max(1, values.SelectMany(v => v).DefaultIfEmpty(0).Max());
            int step = max <= 4 ? 1 : max <= 10 ? 2 : max <= 25 ? 5 : max <= 50 ? 10 : (int)Math.Ceiling(max / 5.0 / 10) * 10;
            int top = (int)Math.Ceiling(max / (double)step) * step;
            int axisW = Th.D(28), labH = Th.D(22);
            var plot = new Rectangle(r.X + axisW, r.Y + Th.D(16), r.Width - axisW, r.Height - labH - Th.D(16));
            using (var p = new Pen(Th.Sep))
                for (int v = 0; v <= top; v += step)
                {
                    int y = plot.Bottom - (int)(plot.Height * v / (double)top);
                    g.DrawLine(p, plot.X, y, plot.Right, y);
                    G.T(g, v.ToString(), Th.F(7.5f), new Rectangle(r.X, y - Th.D(8), axisW - Th.D(6), Th.D(16)), Th.Gray, G.R);
                }
            float gw = plot.Width / (float)n;
            int ns = values[0].Length;
            float inner = gw * (ns == 1 ? .56f : .74f);
            float bw = inner / ns;
            for (int i = 0; i < n; i++)
            {
                var gr = new Rectangle((int)(plot.X + i * gw), r.Y, (int)gw, r.Height);
                hits.Add(gr);
                bool h = gr.Contains(hov);
                if (i == sel) G.Fill(g, new RectangleF(gr.X + 2, plot.Y - Th.D(12), gr.Width - 4, plot.Height + Th.D(12) + labH), Th.D(8), Th.Tint(Th.Blue, .08f));
                else if (h) G.Fill(g, new RectangleF(gr.X + 2, plot.Y - Th.D(12), gr.Width - 4, plot.Height + Th.D(12) + labH), Th.D(8), Th.Soft);
                float x0 = plot.X + i * gw + (gw - inner) / 2;
                for (int s = 0; s < ns; s++)
                {
                    int v = values[i][s];
                    float bh = plot.Height * v / (float)top;
                    var br = new RectangleF(x0 + s * bw + 1, plot.Bottom - bh, Math.Max(2, bw - 2), bh);
                    if (v > 0)
                    {
                        G.Fill(g, br, Math.Min(Th.D(4), br.Width / 2), colors[s]);
                        G.T(g, v.ToString(), Th.F(7.5f, true), new Rectangle((int)br.X - Th.D(10), (int)br.Y - Th.D(15), (int)br.Width + Th.D(20), Th.D(14)), Th.Dark(colors[s], .2f), G.C);
                    }
                }
                G.T(g, labels[i], i == sel ? Th.F(8f, true) : Th.F(8f), new Rectangle(gr.X - Th.D(4), plot.Bottom + Th.D(4), gr.Width + Th.D(8), labH - Th.D(4)), i == sel ? Th.Blue : Th.Sub, G.C);
            }
            using (var p = new Pen(Th.H("#D1D1D6"))) g.DrawLine(p, plot.X, plot.Bottom, plot.Right, plot.Bottom);
            return hits;
        }

        public static void Legend(Graphics g, int x, int y, string[] names, Color[] colors)
        {
            for (int i = 0; i < names.Length; i++)
            {
                float d = Th.D(9);
                G.Fill(g, new RectangleF(x, y + Th.D(4), d, d), Th.D(2), colors[i]);
                int w = G.TW(names[i], Th.F(8.5f));
                G.T(g, names[i], Th.F(8.5f), new Rectangle(x + Th.D(14), y, w + 4, Th.D(18)), Th.Sub, G.L);
                x += w + Th.D(30);
            }
        }
    }

    // ───────── Aylık takvim ─────────
    class CalendarView : BaseCtl
    {
        public DateTime Month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public Func<IEnumerable<Firm>> Source;
        public event Action<Firm> Pick;
        class Ev { public DateTime D; public Firm F; public string Kind; public Color C; public int Order; }
        readonly List<KeyValuePair<Rectangle, Firm>> chipHits = new List<KeyValuePair<Rectangle, Firm>>();
        readonly List<KeyValuePair<Rectangle, DateTime>> moreHits = new List<KeyValuePair<Rectangle, DateTime>>();
        Rectangle prevR, nextR, todayR;
        static readonly string[] Wd = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        public static readonly string[] Kinds = { "Keşif", "Kontrol günü", "Periyodik kontrol (sonraki)", "Kusur süresi son gün" };
        public static readonly Color[] KindColors = { Th.Blue, Th.Orange, Th.Green, Th.Red };

        public CalendarView() { Cursor = Cursors.Default; }

        List<Ev> GetEvents()
        {
            var l = new List<Ev>();
            if (Source == null) return l;
            foreach (var f in Source())
            {
                if (f.arsiv || Flow.IsIptal(f)) continue;
                var k = U.PD(f.kesifTarih);
                if (k.HasValue) l.Add(new Ev { D = k.Value, F = f, Kind = Kinds[0], C = KindColors[0], Order = 0 });
                var days = Flow.Days(f);
                for (int i = 0; i < days.Count; i++)
                    l.Add(new Ev { D = days[i], F = f, Kind = Kinds[1] + (days.Count > 1 ? " (" + (i + 1) + "/" + days.Count + ")" : ""), C = KindColors[1], Order = 1 });
                var y = Flow.NextYearly(f);
                if (y.HasValue) l.Add(new Ev { D = y.Value, F = f, Kind = Kinds[2], C = KindColors[2], Order = 2 });
                var kt = U.PT(f.kusurTarih);
                if (f.kusur == Flow.KusurVar && kt.HasValue) l.Add(new Ev { D = kt.Value.Date.AddDays(Flow.KUSUR_GUN), F = f, Kind = Kinds[3], C = KindColors[3], Order = 3 });
            }
            return l;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            chipHits.Clear(); moreHits.Clear();
            int pad = Th.D(20);
            G.T(g, Month.ToString("MMMM yyyy", Th.TR), Th.F(17f, true), new Rectangle(pad, Th.D(14), Width / 2, Th.D(34)), Th.Text, G.L);
            // gezinme
            int bh = Th.D(30), x = Width - pad;
            todayR = new Rectangle(x - Th.D(64), Th.D(16), Th.D(64), bh); x = todayR.X - Th.D(8);
            nextR = new Rectangle(x - bh, Th.D(16), bh, bh); x = nextR.X - Th.D(6);
            prevR = new Rectangle(x - bh, Th.D(16), bh, bh);
            foreach (var b in new[] { prevR, nextR, todayR })
                G.Fill(g, b, bh / 2f, b.Contains(hov) ? Th.H("#E5E5EA") : Th.Field);
            G.T(g, "Bugün", Th.F(9f, true), todayR, Th.Blue, G.C);
            using (var p = new Pen(Th.Text, Th.K * 1.8f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
            {
                int cy = prevR.Y + bh / 2, s = Th.D(5);
                g.DrawLines(p, new[] { new Point(prevR.X + bh / 2 + s / 2, cy - s), new Point(prevR.X + bh / 2 - s / 2, cy), new Point(prevR.X + bh / 2 + s / 2, cy + s) });
                g.DrawLines(p, new[] { new Point(nextR.X + bh / 2 - s / 2, cy - s), new Point(nextR.X + bh / 2 + s / 2, cy), new Point(nextR.X + bh / 2 - s / 2, cy + s) });
            }
            Chart.Legend(g, pad, Th.D(54), Kinds, KindColors);

            var evs = GetEvents();
            int top = Th.D(84);
            int cw = (Width - pad * 2) / 7;
            for (int i = 0; i < 7; i++)
                G.T(g, Wd[i], Th.F(8.5f, true), new Rectangle(pad + i * cw, top, cw, Th.D(22)), i >= 5 ? Th.Gray : Th.Sub, G.C);
            top += Th.D(24);
            int ch = (Height - top - Th.D(14)) / 6;
            int off = ((int)Month.DayOfWeek + 6) % 7;
            var start = Month.AddDays(-off);
            using (var pen = new Pen(Th.Sep))
            {
                for (int r = 0; r <= 6; r++) g.DrawLine(pen, pad, top + r * ch, pad + 7 * cw, top + r * ch);
                for (int c = 0; c <= 7; c++) g.DrawLine(pen, pad + c * cw, top, pad + c * cw, top + 6 * ch);
            }
            int chipH = Th.D(19);
            for (int i = 0; i < 42; i++)
            {
                var d = start.AddDays(i);
                var cell = new Rectangle(pad + (i % 7) * cw, top + (i / 7) * ch, cw, ch);
                bool inM = d.Month == Month.Month;
                if (!inM) g.FillRectangle(new SolidBrush(Th.Soft), cell.X + 1, cell.Y + 1, cell.Width - 1, cell.Height - 1);
                var nr = new Rectangle(cell.X + Th.D(6), cell.Y + Th.D(4), Th.D(24), Th.D(22));
                if (d == DateTime.Today)
                {
                    G.Circle(g, new RectangleF(nr.X, nr.Y, nr.Width, nr.Width - Th.D(2)), Th.Red);
                    G.T(g, d.Day.ToString(), Th.F(9f, true), new Rectangle(nr.X, nr.Y, nr.Width, nr.Width - Th.D(2)), Color.White, G.C);
                }
                else G.T(g, d.Day.ToString(), Th.F(9f, inM), nr, inM ? (i % 7 >= 5 ? Th.Sub : Th.Text) : Th.Gray, G.C);

                var de = evs.Where(v => v.D == d).OrderBy(v => v.Order).ThenBy(v => v.F.ad).ToList();
                int y = cell.Y + Th.D(28);
                int cap = Math.Max(0, (cell.Bottom - y - Th.D(2)) / (chipH + Th.D(2)));
                int shown = de.Count > cap ? Math.Max(0, cap - 1) : de.Count;
                for (int k = 0; k < shown; k++)
                {
                    var ev = de[k];
                    var cr = new Rectangle(cell.X + Th.D(4), y, cell.Width - Th.D(8), chipH);
                    bool h = cr.Contains(hov);
                    G.Fill(g, cr, Th.D(5), Th.Tint(ev.C, h ? .3f : .16f));
                    G.Fill(g, new RectangleF(cr.X, cr.Y, Th.D(3), cr.Height), Th.D(1.5f), ev.C);
                    G.T(g, ev.F.ad, Th.F(8f, true), new Rectangle(cr.X + Th.D(7), cr.Y, cr.Width - Th.D(9), cr.Height), Th.Dark(ev.C, .3f), G.L);
                    chipHits.Add(new KeyValuePair<Rectangle, Firm>(cr, ev.F));
                    y += chipH + Th.D(2);
                }
                if (de.Count > shown)
                {
                    var mr = new Rectangle(cell.X + Th.D(6), y, cell.Width - Th.D(10), chipH);
                    G.T(g, "+" + (de.Count - shown) + " daha", Th.F(8f, true), mr, mr.Contains(hov) ? Th.Blue : Th.Sub, G.L);
                    moreHits.Add(new KeyValuePair<Rectangle, DateTime>(mr, d));
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hand = chipHits.Any(c => c.Key.Contains(e.Location)) || moreHits.Any(c => c.Key.Contains(e.Location)) || prevR.Contains(e.Location) || nextR.Contains(e.Location) || todayR.Contains(e.Location);
            Cursor = hand ? Cursors.Hand : Cursors.Default;
            var c0 = chipHits.FirstOrDefault(c => c.Key.Contains(e.Location));
            if (c0.Value != null)
            {
                var ev = GetEvents().Where(v => v.F == c0.Value).Select(v => v.D.ToString("dd.MM") + " " + v.Kind);
                tip.SetToolTip(this, c0.Value.ad + "\n" + string.Join("\n", ev));
            }
            else tip.SetToolTip(this, null);
        }
        readonly ToolTip tip = new ToolTip();

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (prevR.Contains(e.Location)) { Month = Month.AddMonths(-1); Invalidate(); return; }
            if (nextR.Contains(e.Location)) { Month = Month.AddMonths(1); Invalidate(); return; }
            if (todayR.Contains(e.Location)) { Month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); Invalidate(); return; }
            foreach (var c in chipHits) if (c.Key.Contains(e.Location)) { if (Pick != null) Pick(c.Value); return; }
            foreach (var m in moreHits)
                if (m.Key.Contains(e.Location))
                {
                    var menu = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new MenuColors()), Font = Th.F(9.5f), ShowImageMargin = false };
                    menu.Items.Add(new ToolStripLabel(m.Value.ToString("d MMMM yyyy, dddd", Th.TR)) { Font = Th.F(9f, true), ForeColor = Th.Sub });
                    foreach (var ev in GetEvents().Where(v => v.D == m.Value).OrderBy(v => v.Order))
                    {
                        var f = ev.F; var it = new ToolStripMenuItem(ev.F.ad + "  ·  " + ev.Kind) { ForeColor = Th.Dark(ev.C, .25f) };
                        it.Click += (s, a) => { if (Pick != null) Pick(f); };
                        menu.Items.Add(it);
                    }
                    menu.Show(this, new Point(m.Key.X, m.Key.Bottom));
                    return;
                }
        }
        protected override void OnMouseWheel(MouseEventArgs e) { Month = Month.AddMonths(e.Delta > 0 ? -1 : 1); Invalidate(); }
    }

    // ───────── Raporlar ─────────
    class ReportCanvas : BaseCtl
    {
        public Func<List<Firm>> Source;
        public DateTime Month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public int YSel = -2;
        public event Action<Firm> Pick;
        public event EventHandler HeightChanged;
        Rectangle prevR, nextR, todayR;
        List<Rectangle> yHits = new List<Rectangle>();
        readonly List<KeyValuePair<Rectangle, Firm>> nameHits = new List<KeyValuePair<Rectangle, Firm>>();

        public ReportCanvas() { }

        // Yıllık kontrol sütunları: 0 = geciken, 1..12 = bu aydan itibaren
        List<List<Firm>> YearCols(out string[] labels)
        {
            var firms = Source().Where(f => !f.arsiv && !Flow.IsIptal(f) && Flow.NextYearly(f).HasValue).ToList();
            var cols = new List<List<Firm>>(); var lab = new List<string>();
            var m0 = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            cols.Add(firms.Where(f => Flow.NextYearly(f).Value < DateTime.Today).ToList()); lab.Add("Geciken");
            for (int i = 0; i < 12; i++)
            {
                var m = m0.AddMonths(i);
                cols.Add(firms.Where(f => { var d = Flow.NextYearly(f).Value; return d >= DateTime.Today && d.Year == m.Year && d.Month == m.Month; }).ToList());
                lab.Add(m.ToString("MMM", Th.TR) + (m.Month == 1 || i == 0 ? " " + m.ToString("yy") : ""));
            }
            labels = lab.ToArray();
            return cols;
        }
        int SelCol(List<List<Firm>> cols)
        {
            if (YSel >= 0) return YSel;
            if (cols[0].Count > 0) return 0;
            for (int i = 1; i < cols.Count; i++) if (cols[i].Count > 0) return i;
            return 1;
        }
        int TileCols(int w) { return w >= Th.D(860) ? 6 : 3; }
        public int Calc(int w)
        {
            if (Source == null) return 100;
            string[] lab; var cols = YearCols(out lab);
            int n = cols[SelCol(cols)].Count;
            int rows = 6 / TileCols(w);
            return Th.D(70) + rows * Th.D(96) + (rows - 1) * Th.D(12) + Th.D(20) + Th.D(84) + Th.D(24)
                + Th.D(300) + Th.D(24) + Th.D(300) + Th.D(16) + Math.Max(1, n) * Th.D(34) + Th.D(40);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            if (Source == null) return;
            var all = Source();
            nameHits.Clear();
            int pad = Th.D(24), w = Width - pad * 2, y = Th.D(18);

            // başlık + ay seçimi
            G.T(g, "Aylık Özet", Th.F(17f, true), new Rectangle(pad, y, w, Th.D(34)), Th.Text, G.L);
            int bh = Th.D(30), x = pad + w;
            todayR = new Rectangle(x - Th.D(72), y + Th.D(2), Th.D(72), bh); x = todayR.X - Th.D(8);
            nextR = new Rectangle(x - bh, y + Th.D(2), bh, bh); x = nextR.X - Th.D(8);
            int lw = Th.D(150);
            var labR = new Rectangle(x - lw, y + Th.D(2), lw, bh); x = labR.X - Th.D(8);
            prevR = new Rectangle(x - bh, y + Th.D(2), bh, bh);
            foreach (var b in new[] { prevR, nextR, todayR }) G.Fill(g, b, bh / 2f, b.Contains(hov) ? Th.H("#E5E5EA") : Th.Field);
            G.T(g, "Bu ay", Th.F(9f, true), todayR, Th.Blue, G.C);
            G.T(g, Month.ToString("MMMM yyyy", Th.TR), Th.F(10.5f, true), labR, Th.Text, G.C);
            using (var p = new Pen(Th.Text, Th.K * 1.8f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
            {
                int cy = prevR.Y + bh / 2, s = Th.D(5);
                g.DrawLines(p, new[] { new Point(prevR.X + bh / 2 + s / 2, cy - s), new Point(prevR.X + bh / 2 - s / 2, cy), new Point(prevR.X + bh / 2 + s / 2, cy + s) });
                g.DrawLines(p, new[] { new Point(nextR.X + bh / 2 - s / 2, cy - s), new Point(nextR.X + bh / 2 + s / 2, cy), new Point(nextR.X + bh / 2 - s / 2, cy + s) });
            }
            y += Th.D(52);

            // kutucuklar
            var st = MonthStats.Of(all, Month.Year, Month.Month);
            var pm = Month.AddMonths(-1); var ps = MonthStats.Of(all, pm.Year, pm.Month);
            var tiles = new[] {
                new { L = "Yeni firma", V = st.YeniFirma, P = ps.YeniFirma, C = Th.Indigo },
                new { L = "Keşif yapıldı", V = st.Kesif, P = ps.Kesif, C = Th.Blue },
                new { L = "Sözleşme yapıldı", V = st.Sozlesme, P = ps.Sozlesme, C = Th.Purple },
                new { L = "Kontrol yapılan firma", V = st.KontrolFirma, P = ps.KontrolFirma, C = Th.Orange },
                new { L = "Kontrol günü", V = st.KontrolGunu, P = ps.KontrolGunu, C = Th.H("#E08600") },
                new { L = "Rapor gönderildi", V = st.Rapor, P = ps.Rapor, C = Th.Teal } };
            int tc = TileCols(Width), gap = Th.D(12);
            int tw = (w - gap * (tc - 1)) / tc, th = Th.D(96);
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                var r = new Rectangle(pad + (i % tc) * (tw + gap), y + (i / tc) * (th + gap), tw, th);
                G.Fill(g, r, Th.D(12), Th.Soft);
                G.Fill(g, new RectangleF(r.X + Th.D(14), r.Y + Th.D(16), Th.D(8), Th.D(8)), Th.D(4), t.C);
                G.T(g, t.L, Th.F(8.75f, true), new Rectangle(r.X + Th.D(28), r.Y + Th.D(10), r.Width - Th.D(34), Th.D(20)), Th.Sub, G.L);
                G.T(g, t.V.ToString(), Th.F(22f, true), new Rectangle(r.X + Th.D(12), r.Y + Th.D(30), r.Width, Th.D(40)), Th.Text, G.L);
                int diff = t.V - t.P;
                string ds = "geçen ay " + t.P + (diff != 0 ? (diff > 0 ? "  ▲" + diff : "  ▼" + (-diff)) : "");
                G.T(g, ds, Th.F(8f), new Rectangle(r.X + Th.D(14), r.Bottom - Th.D(24), r.Width - Th.D(20), Th.D(18)), diff > 0 ? Th.Green : diff < 0 ? Th.Red : Th.Gray, G.L);
            }
            int rows = tiles.Length / tc;
            y += rows * th + (rows - 1) * gap + Th.D(20);

            // keşif → sözleşme süresi
            int on; var ov = MonthStats.OverallKesifSozlesme(all, out on);
            var ar = new Rectangle(pad, y, w, Th.D(84));
            G.Fill(g, ar, Th.D(12), Th.Tint(Th.Purple, .08f));
            G.T(g, "Keşiften sözleşmeye ortalama süre", Th.F(9.5f, true), new Rectangle(ar.X + Th.D(16), ar.Y + Th.D(10), w, Th.D(22)), Th.Text, G.L);
            int half = w / 2;
            G.T(g, st.OrtKesifSozlesme.HasValue ? Math.Round(st.OrtKesifSozlesme.Value) + " gün" : "—", Th.F(18f, true), new Rectangle(ar.X + Th.D(16), ar.Y + Th.D(34), half, Th.D(32)), Th.Purple, G.L);
            G.T(g, "bu ay sözleşme yapılan " + st.OrtN + " firma", Th.F(8.25f), new Rectangle(ar.X + Th.D(16), ar.Y + Th.D(64), half, Th.D(16)), Th.Sub, G.L);
            G.T(g, ov.HasValue ? Math.Round(ov.Value) + " gün" : "—", Th.F(18f, true), new Rectangle(ar.X + half, ar.Y + Th.D(34), half, Th.D(32)), Th.Text, G.L);
            G.T(g, "tüm zamanlar · " + on + " firma", Th.F(8.25f), new Rectangle(ar.X + half, ar.Y + Th.D(64), half, Th.D(16)), Th.Sub, G.L);
            y += Th.D(84) + Th.D(24);

            // son 6 ay
            G.T(g, "Son 6 ay", Th.F(12f, true), new Rectangle(pad, y, w, Th.D(26)), Th.Text, G.L);
            var sc = new[] { Th.Blue, Th.Purple, Th.Orange };
            Chart.Legend(g, pad + Th.D(100), y + Th.D(4), new[] { "Keşif", "Sözleşme", "Kontrol yapılan firma" }, sc);
            var labs = new string[6]; var vals = new int[6][];
            for (int i = 0; i < 6; i++)
            {
                var m = Month.AddMonths(i - 5); var s = MonthStats.Of(all, m.Year, m.Month);
                labs[i] = m.ToString("MMM yy", Th.TR); vals[i] = new[] { s.Kesif, s.Sozlesme, s.KontrolFirma };
            }
            Chart.Bars(g, new Rectangle(pad, y + Th.D(34), w, Th.D(260)), labs, vals, sc, 5, new Point(-1, -1));
            y += Th.D(300) + Th.D(24);

            // yıllık periyodik kontroller
            string[] ylab; var cols = YearCols(out ylab);
            int sel = SelCol(cols);
            G.T(g, "Periyodik kontroller · önümüzdeki 12 ay", Th.F(12f, true), new Rectangle(pad, y, w, Th.D(26)), Th.Text, G.L);
            G.T(g, "Sütuna tıklayın: o aydaki firmalar aşağıda listelenir. Periyot ve hatırlatma süresi her firmada ayrı ayarlanır (varsayılan 1 yıl / 30 gün).", Th.F(8.5f), new Rectangle(pad, y + Th.D(24), w, Th.D(18)), Th.Sub, G.L);
            var yv = cols.Select(c => new[] { c.Count }).ToArray();
            yHits = Chart.Bars(g, new Rectangle(pad, y + Th.D(44), w, Th.D(250)), ylab, yv, new[] { Th.Green }, sel, hov);
            y += Th.D(300) + Th.D(16);

            var list = cols[sel].OrderBy(f => Flow.NextYearly(f)).ToList();
            G.T(g, (sel == 0 ? "Geciken periyodik kontroller" : ylab[sel] + " ayında periyodik kontrolü olan firmalar") + " (" + list.Count + ")", Th.F(9.5f, true), new Rectangle(pad, y, w, Th.D(24)), Th.Text, G.L);
            y += Th.D(30);
            if (list.Count == 0) G.T(g, "Bu dönemde periyodik kontrol yok.", Th.F(9f), new Rectangle(pad, y, w, Th.D(30)), Th.Gray, G.L);
            foreach (var f in list)
            {
                var r = new Rectangle(pad, y, w, Th.D(30));
                bool h = r.Contains(hov);
                if (h) G.Fill(g, r, Th.D(8), Th.Soft);
                var d = Flow.NextYearly(f).Value; int left = (int)(d - DateTime.Today).TotalDays;
                G.T(g, d.ToString("dd.MM.yyyy"), Th.F(9f, true), new Rectangle(r.X + Th.D(10), r.Y, Th.D(90), r.Height), left < 0 ? Th.Red : left <= Flow.Hatirlat(f) ? Th.Orange : Th.Green, G.L);
                G.T(g, f.ad, Th.F(9.5f), new Rectangle(r.X + Th.D(106), r.Y, w - Th.D(320), r.Height), h ? Th.Blue : Th.Text, G.L);
                string ls = (left < 0 ? (-left) + " gün geçti" : left == 0 ? "bugün" : left + " gün kaldı") + "  ·  " + Flow.PeriyotText(f.periyotAy) + "lık";
                G.T(g, ls + (f.tel != "" ? "  ·  " + f.tel : ""), Th.F(8.5f), new Rectangle(r.Right - Th.D(220), r.Y, Th.D(210), r.Height), Th.Sub, G.R);
                nameHits.Add(new KeyValuePair<Rectangle, Firm>(r, f));
                y += Th.D(34);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hand = prevR.Contains(e.Location) || nextR.Contains(e.Location) || todayR.Contains(e.Location) || yHits.Any(r => r.Contains(e.Location)) || nameHits.Any(n => n.Key.Contains(e.Location));
            Cursor = hand ? Cursors.Hand : Cursors.Default;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (prevR.Contains(e.Location)) { Month = Month.AddMonths(-1); Invalidate(); return; }
            if (nextR.Contains(e.Location)) { Month = Month.AddMonths(1); Invalidate(); return; }
            if (todayR.Contains(e.Location)) { Month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); Invalidate(); return; }
            for (int i = 0; i < yHits.Count; i++)
                if (yHits[i].Contains(e.Location)) { YSel = i; if (HeightChanged != null) HeightChanged(this, EventArgs.Empty); Invalidate(); return; }
            foreach (var n in nameHits) if (n.Key.Contains(e.Location)) { if (Pick != null) Pick(n.Value); return; }
        }
    }
}
