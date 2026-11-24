using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PKT
{
    class MainForm : Form, IMessageFilter
    {
        readonly Store st;
        Firm cur; bool binding;
        readonly Timer saveT = new Timer { Interval = 500 };

        // kenar çubuğu
        Panel side; KpiTile kAll, kDev, kKus, kTam, kKtm, kIp; DayChips dayc; Card cDonut, cUp; Donut donut; Upcoming up;
        Label lblSave; PillButton bExport, bFolder, bHelp;
        // liste
        Card cList; FieldBox search; Segmented seg; PillButton fStage, fUcret, fSort, fClear, bNew; FirmList list;
        int filterStage = -1, sortMode; string filterUcret;
        static readonly string[] SortNames = { "Son güncellenen", "Firma adı (A–Z)", "Kontrol tarihi", "Keşif tarihi", "İlerleme" };
        // detay
        Card cDet; Scroller sc; Panel dc; Label empty;
        FieldBox fName; ProgressRing ring; PillButton bDel; StatusLine sline;
        Label hDurum, hIlet, hPlan, hAkis, hNot, lGenel, lUcret, lY, lT, lE, lA, lKe, lSo, lKo, planHint, noteHint;
        Segmented sgDurum, sgUcret; Label lKu, kuHint; DateField dKu; FieldBox fY, fT, fE, fA; DateField dKe, dSo, dKo;
        StageCard[] stages; ChipBar chips; FieldBox fNote; PillButton bNoteAdd, bNoteCancel; NoteList notes; Note editing;
        readonly ToolTip tip = new ToolTip();
        public static bool NoPopup;
        // görünümler
        Segmented nav; int view; Card cCal, cRep; CalendarView cal; Scroller repSc; ReportCanvas rep;
        PillButton bRestore, bDonem, bPer, bHat, bOto, bKesifYok;
        readonly Timer autoT = new Timer { Interval = 10 * 60 * 1000 }; Label lYil, yilHint; DateField dYil;
        List<Firm> Live() { return st.Firms.Where(f => !f.arsiv).ToList(); }

        public MainForm(Store store)
        {
            st = store;
            Text = "Periyodik Kontrol Takip";
            AutoScaleMode = AutoScaleMode.None;
            Font = Th.F(9.5f);
            BackColor = Th.Bg;
            StartPosition = FormStartPosition.CenterScreen;
            var wa = Screen.PrimaryScreen.WorkingArea;
            MinimumSize = new Size(Math.Min(Th.D(1180), wa.Width), Math.Min(Th.D(700), wa.Height));
            Size = new Size(Math.Min(Th.D(1440), wa.Width - Th.D(20)), Math.Min(Th.D(900), wa.Height - Th.D(20)));
            if (wa.Width < Th.D(1500) || wa.Height < Th.D(860)) WindowState = FormWindowState.Maximized;
            KeyPreview = true;
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            BuildSide(); BuildList(); BuildDetail(); BuildViews();
            saveT.Tick += (s, e) => { saveT.Stop(); DoSave(); };
            Application.AddMessageFilter(this);
            RefreshAll();
            var first = list.Items.FirstOrDefault();
            if (first != null) SelectFirm(first);
            else ShowDetail(null);
            UpdSaveLabel();
        }

        // ═════════════════════════ KENAR ÇUBUĞU ═════════════════════════
        void BuildSide()
        {
            side = new Panel { BackColor = Th.Bg };
            side.Paint += (s, e) =>
            {
                var g = e.Graphics;
                G.T(g, "Periyodik Kontrol", Th.F(15f, true), new Rectangle(Th.D(20), Th.D(14), side.Width - Th.D(40), Th.D(30)), Th.Text, G.L);
                G.T(g, DateTime.Today.ToString("d MMM", Th.TR), Th.F(9f), new Rectangle(Th.D(20), Th.D(14), side.Width - Th.D(40), Th.D(30)), Th.Sub, G.R);
            };
            Controls.Add(side);
            nav = new Segmented { BackColor = Th.Bg };
            nav.SetItems("Firmalar", "Takvim", "Rapor", "Arşiv");
            nav.Changed += (s, e) => SetView(nav.Sel);
            side.Controls.Add(nav);
            kAll = Tile("Tümü", "list", Th.Blue, 0);
            kDev = Tile("Devam Eden", "clock", Th.Orange, 1);
            kKus = Tile("Kusur Bekleyen", "excl", Th.Red, 2);
            kTam = Tile("Uygun Tamam", "check", Th.Green, 3);
            kKtm = Tile("Kusurlu Tamam", "check", Flow.Brown, 4);
            kIp = Tile("İptal / Olumsuz", "x", Th.GrayDark, 5);

            cDonut = new Card { Title = "Aşama Dağılımı" };
            donut = new Donut();
            donut.SegClick += k => { if (view != 0) SetView(0); filterStage = filterStage == k ? -1 : k; RefreshAll(); };
            cDonut.Controls.Add(donut);
            side.Controls.Add(cDonut);

            cUp = new Card { Title = "Yaklaşan ve Takip" };
            up = new Upcoming();
            up.Pick += f => GoFirm(f);
            cUp.Controls.Add(up);
            side.Controls.Add(cUp);

            lblSave = new Label { AutoSize = false, ForeColor = Th.Sub, Font = Th.F(8.5f), TextAlign = ContentAlignment.MiddleLeft, BackColor = Th.Bg };
            side.Controls.Add(lblSave);
            bExport = new PillButton { Kind = "plain", IconKind = "export", BackColor = Th.Bg };
            bExport.Click += (s, e) => ExportCsv();
            bFolder = new PillButton { Kind = "plain", IconKind = "folder", BackColor = Th.Bg };
            bFolder.Click += (s, e) => { try { Process.Start("explorer.exe", "\"" + st.Dir + "\""); } catch { MessageBox.Show(st.Dir, Text); } };
            bHelp = new PillButton { Kind = "plain", IconKind = "help", BackColor = Th.Bg };
            bHelp.Click += (s, e) => ShowHelp();
            tip.SetToolTip(bExport, "Excel'e aktar (.xlsx)  ·  Ctrl+E");
            tip.SetToolTip(bFolder, "Veri klasörünü aç");
            tip.SetToolTip(bHelp, "Kısayollar  ·  F1");
            side.Controls.AddRange(new Control[] { bExport, bFolder, bHelp });
        }

        KpiTile Tile(string label, string glyph, Color c, int segIndex)
        {
            var t = new KpiTile { Label = label, Glyph = glyph, Accent = c, BackColor = Th.Bg };
            t.Click += (s, e) => { if (view != 0) SetView(0); seg.Sel = seg.Sel == segIndex && segIndex != 0 ? 0 : segIndex; RefreshAll(); };
            side.Controls.Add(t);
            return t;
        }

        // ═════════════════════════ GÖRÜNÜMLER ═════════════════════════
        void BuildViews()
        {
            cCal = new Card { Visible = false };
            cal = new CalendarView { BackColor = Color.White, Source = () => st.Firms };
            cal.Pick += f => GoFirm(f);
            cCal.Controls.Add(cal); Controls.Add(cCal);
            cRep = new Card { Visible = false };
            repSc = new Scroller();
            rep = new ReportCanvas { BackColor = Color.White, Source = () => st.Firms };
            rep.Pick += f => GoFirm(f);
            rep.HeightChanged += (s, e) => LayoutReport();
            repSc.Content.Controls.Add(rep);
            cRep.Controls.Add(repSc); Controls.Add(cRep);
        }
        void SetView(int v)
        {
            bool modeChange = (view == 3) != (v == 3);
            view = v; nav.Sel = v; nav.Invalidate();
            bool firms = v == 0 || v == 3;
            cList.Visible = cDet.Visible = firms;
            cCal.Visible = v == 1; cRep.Visible = v == 2;
            cList.Title = v == 3 ? "Arşiv" : "Firmalar";
            bNew.Visible = v != 3;
            if (modeChange) { search.Value = ""; filterStage = -1; filterUcret = null; seg.Sel = 0; }
            RefreshAll();
            if (firms && (cur == null || cur.arsiv != (v == 3)))
            {
                var first = list.Items.FirstOrDefault();
                if (first != null) SelectFirm(first); else { cur = null; ShowDetail(null); }
            }
            LayoutAll();
            if (v == 1) cal.Invalidate();
            if (v == 2) { LayoutReport(); rep.Invalidate(); }
        }
        void GoFirm(Firm f)
        {
            if (f.arsiv) { if (view != 3) SetView(3); }
            else if (view != 0) SetView(0);
            if (!list.Items.Contains(f)) { search.Value = ""; filterStage = -1; filterUcret = null; seg.Sel = 0; RefreshAll(); }
            SelectFirm(f);
        }
        void LayoutReport()
        {
            if (repSc == null) return;
            int w = repSc.ClientSize.Width - Th.D(10);
            rep.SetBounds(0, 0, w, rep.Calc(w));
            repSc.Content.Height = rep.Height;
            repSc.ContentChanged();
            rep.Invalidate();
        }

        // ═════════════════════════ FİRMA LİSTESİ ═════════════════════════
        void BuildList()
        {
            cList = new Card { Title = "Firmalar" };
            Controls.Add(cList);
            bNew = new PillButton { Text = "Yeni Firma", IconKind = "plus", Kind = "primary" };
            bNew.Click += (s, e) => NewFirm();
            tip.SetToolTip(bNew, "Ctrl+N");
            search = new FieldBox(false, 9.75f) { IconKind = "search", Placeholder = "Firma, kişi veya not ara   (Ctrl+F)" };
            search.Box.TextChanged += (s, e) => RefreshList();
            search.Box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter) { list.Focus(); if (list.Selected == null || !list.Items.Contains(list.Selected)) list.MoveSel(1); e.SuppressKeyPress = true; }
            };
            seg = new Segmented();
            seg.Changed += (s, e) => RefreshAll();
            fStage = new PillButton { Kind = "plain", Chevron = true, Font = Th.F(8.75f, true), Height = Th.D(28) };
            fUcret = new PillButton { Kind = "plain", Chevron = true, Font = Th.F(8.75f, true), Height = Th.D(28) };
            fSort = new PillButton { Kind = "plain", Chevron = true, Font = Th.F(8.75f, true), Height = Th.D(28) };
            fClear = new PillButton { Kind = "ghost", IconKind = "x", Height = Th.D(28) };
            tip.SetToolTip(fClear, "Filtreleri temizle");
            fStage.Click += (s, e) => StageMenu();
            fUcret.Click += (s, e) => UcretMenu();
            fSort.Click += (s, e) => SortMenu();
            fClear.Click += (s, e) => ClearFilters();
            list = new FirmList();
            list.SelChanged += (s, e) => { if (list.Selected != null) ShowDetail(list.Selected); };
            list.RightClick += (f, p) => RowMenu(f, p);
            cList.Controls.AddRange(new Control[] { bNew, search, seg, fStage, fUcret, fSort, fClear, list });
        }

        ContextMenuStrip MkMenu()
        {
            return new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new MenuColors()), Font = Th.F(9.5f), ShowImageMargin = false, ShowCheckMargin = true };
        }
        void StageMenu()
        {
            var m = MkMenu();
            Action<string, int> add = (t, k) => { var it = new ToolStripMenuItem(t) { Checked = filterStage == k }; it.Click += (s, e) => { filterStage = k; RefreshAll(); }; m.Items.Add(it); };
            add("Tüm aşamalar", -1);
            m.Items.Add(new ToolStripSeparator());
            for (int i = 0; i < Flow.S.Length; i++) add((i + 1) + ". " + Flow.S[i].Title, i);
            add("Uygun tamamlandı", Flow.DONE); add("Kusurlu tamamlandı", 7);
            m.Items.Add(new ToolStripSeparator());
            add("Beklemede (" + Flow.IDLE_GUN + "+ gün işlem yok)", 99);
            m.Show(fStage, new Point(0, fStage.Height + Th.D(2)));
        }
        void UcretMenu()
        {
            var m = MkMenu();
            Action<string, string> add = (t, k) => { var it = new ToolStripMenuItem(t) { Checked = filterUcret == k }; it.Click += (s, e) => { filterUcret = k; RefreshAll(); }; m.Items.Add(it); };
            add("Tümü", null); m.Items.Add(new ToolStripSeparator());
            add(Flow.Ucretli, Flow.Ucretli); add(Flow.Ucretsiz, Flow.Ucretsiz); add("Belirtilmemiş", "");
            m.Show(fUcret, new Point(0, fUcret.Height + Th.D(2)));
        }
        void SortMenu()
        {
            var m = MkMenu();
            for (int i = 0; i < SortNames.Length; i++)
            {
                int k = i; var it = new ToolStripMenuItem(SortNames[i]) { Checked = sortMode == i };
                it.Click += (s, e) => { sortMode = k; RefreshList(); };
                m.Items.Add(it);
            }
            m.Show(fSort, new Point(0, fSort.Height + Th.D(2)));
        }
        void RowMenu(Firm f, Point p)
        {
            var m = MkMenu();
            var a = new ToolStripMenuItem("Aktif") { Checked = !Flow.IsIptal(f) };
            a.Click += (s, e) => { f.durum = Flow.Aktif; if (f == cur) Bind(); Changed(); };
            var ip = new ToolStripMenuItem("İptal / Olumsuz") { Checked = Flow.IsIptal(f) };
            ip.Click += (s, e) => { f.durum = Flow.Iptal; if (f == cur) Bind(); Changed(); };
            var del = new ToolStripMenuItem(f.arsiv ? "Kalıcı olarak sil…" : "Arşive taşı") { ForeColor = Th.Red };
            del.Click += (s, e) => DeleteFirm();
            if (f.arsiv)
            {
                var rs = new ToolStripMenuItem("Arşivden çıkar");
                rs.Click += (s, e) => RestoreFirm();
                m.Items.AddRange(new ToolStripItem[] { rs, new ToolStripSeparator(), del });
            }
            else m.Items.AddRange(new ToolStripItem[] { a, ip, new ToolStripSeparator(), del });
            m.Show(list, p);
        }
        void ClearFilters()
        {
            filterStage = -1; filterUcret = null; seg.Sel = 0; search.Value = "";
            RefreshAll();
        }

        // ═════════════════════════ DETAY ═════════════════════════
        Label Head(string t)
        {
            var l = new Label { Text = U.Up(t), AutoSize = false, Font = Th.F(8.25f, true), ForeColor = Th.Sub, BackColor = Color.White, TextAlign = ContentAlignment.BottomLeft, UseMnemonic = false };
            dc.Controls.Add(l); return l;
        }
        Label Cap(string t)
        {
            var l = new Label { Text = t, AutoSize = false, Font = Th.F(8.75f), ForeColor = Th.GrayDark, BackColor = Color.White, TextAlign = ContentAlignment.BottomLeft, UseMnemonic = false };
            dc.Controls.Add(l); return l;
        }
        FieldBox Field(string ph, Action<string> set)
        {
            var f = new FieldBox { Placeholder = ph };
            f.Box.TextChanged += (s, e) => { if (binding || cur == null) return; set(f.Value); Changed(); };
            f.Box.Enter += (s, e) => sc.EnsureVisible(f);
            dc.Controls.Add(f); return f;
        }

        void BuildDetail()
        {
            cDet = new Card();
            Controls.Add(cDet);
            empty = new Label { Text = "Soldan bir firma seçin\nya da Ctrl+N ile yeni firma ekleyin.", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Th.Gray, Font = Th.F(11f), BackColor = Color.White };
            cDet.Controls.Add(empty);
            sc = new Scroller();
            dc = sc.Content;
            cDet.Controls.Add(sc);

            fName = new FieldBox(false, 15f) { Placeholder = "Firma adı" };
            fName.Box.Font = Th.F(15f, true);
            fName.Height = fName.Box.PreferredHeight + Th.D(16);
            fName.Box.TextChanged += (s, e) => { if (binding || cur == null) return; cur.ad = fName.Value; Changed(); };
            dc.Controls.Add(fName);
            ring = new ProgressRing { BackColor = Color.White }; dc.Controls.Add(ring);
            bDel = new PillButton { Kind = "danger", IconKind = "trash" };
            bDel.Click += (s, e) => DeleteFirm();
            tip.SetToolTip(bDel, "Firmayı sil");
            dc.Controls.Add(bDel);
            sline = new StatusLine { BackColor = Color.White }; dc.Controls.Add(sline);

            hDurum = Head("Durum");
            lGenel = Cap("Genel durum"); lUcret = Cap("Kontrol ücreti");
            sgDurum = new Segmented { Colors = new[] { Th.Green, Th.Red } };
            sgDurum.SetItems(Flow.Aktif, Flow.Iptal);
            sgDurum.Changed += (s, e) => { if (binding || cur == null) return; cur.durum = sgDurum.Sel == 0 ? Flow.Aktif : Flow.Iptal; Changed(); };
            sgUcret = new Segmented { Colors = new[] { Th.Indigo, Th.Teal } };
            sgUcret.SetItems(Flow.Ucretli, Flow.Ucretsiz);
            sgUcret.Changed += (s, e) => { if (binding || cur == null) return; cur.ucret = sgUcret.Sel == 0 ? Flow.Ucretli : Flow.Ucretsiz; Changed(); };
            dc.Controls.AddRange(new Control[] { sgDurum, sgUcret });

            hPlan = Head("Planlama");
            lKe = Cap("Keşif tarihi"); lSo = Cap("İSG-KATİP sözleşme tarihi"); lKo = Cap("Kontrol günleri (ekle)");
            dKe = DateBox(v => { cur.kesifTarih = v; if (v != "" && !Flow.Done(cur, 1, 0)) cur.checks["1-0"] = U.Now(); });
            dKe.DisabledText = "Keşif yok";
            bKesifYok = new PillButton { Kind = "plain", Font = Th.F(8.75f, true), BackColor = Color.White, Height = Th.D(30), Accent = Th.Blue };
            bKesifYok.Click += (s, e) =>
            {
                if (cur == null) return;
                cur.kesifYok = !cur.kesifYok;
                if (cur.kesifYok)
                {
                    cur.kesifTarih = ""; binding = true; dKe.SetValue(null, false); binding = false;
                    if (!cur.checks.ContainsKey("1f-0")) cur.checks["1f-0"] = U.Now();
                }
                dKe.Enabled = !cur.kesifYok;
                Changed(); LayoutDetail();
            };
            tip.SetToolTip(bKesifYok, "Firma yapılacak kalemleri kendisi ilettiyse işaretleyin: keşif tarihi pasif olur, 2. aşama “Keşif” yerine “Firma İletti” olarak gösterilir.");
            dc.Controls.Add(bKesifYok);
            dSo = DateBox(v => { cur.sozlesmeTarih = v; if (v != "" && !Flow.Done(cur, 2, 1)) cur.checks["2-1"] = new DateTimeOffset(U.PD(v).Value + DateTime.Now.TimeOfDay).ToString("o"); });
            dKo = DateBox(v => { if (v != "" && !cur.kontrolGunleri.Contains(v)) cur.kontrolGunleri.Add(v); Flow.SyncKontrol(cur); });
            dKo.Placeholder = "gg.aa.yyyy  → ekle";
            dKo.ValueChanged += (s, e) => { if (dKo.Value2.HasValue) BeginInvoke(new Action(() => dKo.SetValue(null, false))); };
            dayc = new DayChips { BackColor = Color.White };
            dayc.Changed += (s, e) => Changed();
            dc.Controls.Add(dayc);
            lKu = Cap("Kusur süresi başlangıcı");
            dKu = DateBox(v => { cur.kusurTarih = v == "" ? "" : new DateTimeOffset(DateTime.ParseExact(v, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)).ToString("o"); });
            kuHint = new Label { AutoSize = false, Font = Th.F(8.75f), BackColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            dc.Controls.Add(kuHint);
            bOto = new PillButton { Kind = "plain", Font = Th.F(8.75f, true), BackColor = Color.White, Height = Th.D(30), Accent = Flow.Brown };
            bOto.Click += (s, e) => { if (cur == null) return; cur.otoKapat = !cur.otoKapat; Changed(); LayoutDetail(); };
            tip.SetToolTip(bOto, Flow.KUSUR_GUN + " gün içinde kusurlar giderilmezse firma otomatik olarak “kusurlu tamamlandı” durumuna geçer ve not eklenir.");
            dc.Controls.Add(bOto);
            lYil = Cap("Sonraki periyodik kontrol");
            dYil = DateBox(v => { cur.sonrakiKontrol = v; });
            bPer = new PillButton { Kind = "plain", Chevron = true, Font = Th.F(8.75f, true), BackColor = Color.White };
            bHat = new PillButton { Kind = "plain", Chevron = true, Font = Th.F(8.75f, true), BackColor = Color.White };
            bPer.Click += (s, e) => NumMenu(bPer, "Kontrol periyodu", new[] { 3, 6, 12, 24 }, cur.periyotAy, Flow.PeriyotText, "ay", v => { cur.periyotAy = v; cur.sonrakiKontrol = ""; });
            bHat.Click += (s, e) => NumMenu(bHat, "Kaç gün önce hatırlatılsın", new[] { 7, 15, 30, 45, 60, 90 }, cur.hatirlatGun, v => v + " gün önce", "gün", v => { cur.hatirlatGun = v; });
            tip.SetToolTip(bPer, "Sonraki kontrol tarihi = kontrolün bittiği gün + periyot");
            tip.SetToolTip(bHat, "“Yaklaşan ve Takip”te kaç gün önceden görünsün");
            dc.Controls.AddRange(new Control[] { bPer, bHat });
            yilHint = new Label { AutoSize = false, Font = Th.F(8.75f), BackColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            bDonem = new PillButton { Kind = "tinted", Text = "Yeni dönem başlat", IconKind = "plus", Accent = Th.Green, BackColor = Color.White };
            bDonem.Click += (s, e) => NewPeriod();
            tip.SetToolTip(bDonem, "Bu yılın işini kapatır; iş akışını gelecek yılın kontrolü için sıfırlar. Notlar ve iletişim bilgileri korunur.");
            dc.Controls.AddRange(new Control[] { yilHint, bDonem });
            bRestore = new PillButton { Kind = "tinted", Text = "Arşivden çıkar", BackColor = Color.White, Visible = false };
            bRestore.Click += (s, e) => RestoreFirm();
            dc.Controls.Add(bRestore);
            planHint = new Label { AutoSize = false, Font = Th.F(8.75f), BackColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            dc.Controls.Add(planHint);

            hAkis = Head("İş Akışı");
            stages = new StageCard[Flow.S.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                var s = new StageCard { S = i, BackColor = Color.White };
                s.Changed += (o, e) => Changed();
                var sc0 = s;
                s.ItemDateClick += (ix, pt) =>
                {
                    string key = Flow.Key(cur, sc0.S, ix);
                    var old = U.PT(cur.checks[key]);
                    PickDate(sc0, pt, old, d =>
                    {
                        var t = old.HasValue ? old.Value.TimeOfDay : DateTime.Now.TimeOfDay;
                        cur.checks[key] = new DateTimeOffset(d.Date + t).ToString("o");
                        Changed();
                    });
                };
                s.KusurDateClick += (o, e) => { LayoutDetail(); sc.EnsureVisible(dKu); dKu.ShowCal(); };
                s.NotesClick += k => { chips.Sel = k; CancelEdit(); RefreshNotes(); LayoutDetail(); sc.SetOff(hNot.Top - Th.D(8)); fNote.FocusBox(); };
                stages[i] = s; dc.Controls.Add(s);
            }

            hNot = Head("Notlar");
            chips = new ChipBar { BackColor = Color.White };
            chips.Changed += (s, e) => { RefreshNotes(); LayoutDetail(); };
            dc.Controls.Add(chips);
            fNote = new FieldBox(true) { Placeholder = "Not yazın…  (Ctrl+Enter ile kaydedilir)" };
            fNote.Box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && e.Control) { AddNote(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape && editing != null) { CancelEdit(); e.SuppressKeyPress = true; }
            };
            fNote.Box.Enter += (s, e) => sc.EnsureVisible(fNote);
            dc.Controls.Add(fNote);
            bNoteAdd = new PillButton { Kind = "primary", IconKind = "plus" };
            bNoteAdd.Click += (s, e) => AddNote();
            bNoteCancel = new PillButton { Kind = "plain", Text = "Vazgeç", Visible = false };
            bNoteCancel.Click += (s, e) => CancelEdit();
            noteHint = new Label { AutoSize = false, Font = Th.F(8.25f), ForeColor = Th.Gray, BackColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, Text = "Konu çipi seçili ise not o aşamaya kaydedilir. Nota tıklayınca düzenlenir." };
            dc.Controls.AddRange(new Control[] { bNoteAdd, bNoteCancel, noteHint });
            notes = new NoteList { BackColor = Color.White };
            notes.EditClick += n => StartEdit(n);
            notes.DeleteClick += n => DeleteNote(n);
            dc.Controls.Add(notes);

            hIlet = Head("İletişim");
            lY = Cap("Yetkili"); lT = Cap("Telefon"); lE = Cap("E-posta"); lA = Cap("Adres");
            fY = Field("Ad Soyad", v => cur.yetkili = v);
            fT = Field("0 5xx xxx xx xx", v => cur.tel = v);
            fE = Field("ornek@firma.com", v => cur.eposta = v);
            fA = Field("Adres / OSB / ilçe", v => cur.adres = v);
        }

        void PickDate(Control owner, Point pt, DateTime? init, Action<DateTime> onPick)
        {
            var mc = new MonthCalendar { MaxSelectionCount = 1, ShowTodayCircle = true };
            if (init.HasValue) mc.SetDate(init.Value.Date);
            var dd = new ToolStripDropDown { Padding = Padding.Empty };
            mc.DateSelected += (s, e) => { dd.Close(); onPick(e.Start.Date); };
            dd.Items.Add(new ToolStripControlHost(mc) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = mc.Size });
            int x = Math.Max(0, Math.Min(pt.X, owner.Width - mc.Width));
            dd.Show(owner, new Point(x, pt.Y + Th.D(2)));
        }

        DateField DateBox(Action<string> set)
        {
            var d = new DateField();
            d.ValueChanged += (s, e) =>
            {
                if (binding || cur == null) return;
                set(d.Value2.HasValue ? d.Value2.Value.ToString("yyyy-MM-dd") : "");
                Changed();
            };
            d.Box.Enter += (s, e) => sc.EnsureVisible(d);
            dc.Controls.Add(d); return d;
        }

        // ═════════════════════════ YERLEŞİM ═════════════════════════
        protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutAll(); }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e); LayoutAll();
            if (!string.IsNullOrEmpty(st.Info) && !NoPopup) MessageBox.Show(this, st.Info, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            RunAutoClose();
            autoT.Tick += (s, a) => RunAutoClose();
            autoT.Start();
        }

        void RunAutoClose()
        {
            var closed = Flow.AutoClose(st.Firms);
            if (closed.Count == 0) return;
            if (cur != null && closed.Contains(cur)) Bind();
            RefreshAll();
            ScheduleSave();
            if (NoPopup) return;
            string list = string.Join("\n", closed.Select(f => "• " + f.ad + "  (son gün: " + U.PT(f.kusurTarih).Value.Date.AddDays(Flow.KUSUR_GUN).ToString("dd.MM.yyyy") + ")"));
            MessageBox.Show(this, Flow.KUSUR_GUN + " gün geçti, kusurlar giderilmedi.\n\nAşağıdaki firma(lar) otomatik olarak “Kusurlu tamamlandı” durumuna geçirildi ve firmaya not eklendi:\n\n" + list +
                "\n\nYeniden açmak için firmanın 6. adımında başka bir seçenek seçebilirsiniz.", "Kusur süresi doldu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void LayoutAll()
        {
            if (side == null) return;
            int W = ClientSize.Width, H = ClientSize.Height, p = Th.D(16);
            int sw = Th.D(288);
            side.SetBounds(0, 0, sw, H);
            int tx = Th.D(16), tw = (sw - Th.D(16) * 2 - Th.D(10)) / 2, th = Th.D(68);
            nav.SetBounds(tx, Th.D(50), sw - Th.D(32), Th.D(30));
            int y = Th.D(92);
            var tl = new[] { kAll, kDev, kKus, kTam, kKtm, kIp };
            for (int i = 0; i < tl.Length; i++)
                tl[i].SetBounds(tx + (i % 2) * (tw + Th.D(10)), y + (i / 2) * (th + Th.D(8)), tw, th);
            y += 3 * th + 2 * Th.D(8) + Th.D(12);
            int cw = sw - Th.D(32);
            cDonut.SetBounds(tx, y, cw, Th.D(196));
            donut.SetBounds(Th.D(14), Th.D(42), cw - Th.D(26), Th.D(146));
            y += Th.D(196) + Th.D(12);
            int bottom = H - Th.D(52);
            cUp.SetBounds(tx, y, cw, Math.Max(Th.D(90), bottom - y - Th.D(4)));
            up.SetBounds(Th.D(16), Th.D(44), cw - Th.D(28), cUp.Height - Th.D(52));
            int bs = Th.D(32);
            bHelp.SetBounds(sw - Th.D(16) - bs, bottom + Th.D(8), bs, bs);
            bFolder.SetBounds(bHelp.Left - bs - Th.D(6), bottom + Th.D(8), bs, bs);
            bExport.SetBounds(bFolder.Left - bs - Th.D(6), bottom + Th.D(8), bs, bs);
            lblSave.SetBounds(Th.D(20), bottom + Th.D(8), bExport.Left - Th.D(24), bs);
            side.Invalidate();

            int lx = sw, lw = Th.D(410);
            cList.SetBounds(lx, p, lw, H - p * 2);
            LayoutList();
            int dx = lx + lw + p;
            cDet.SetBounds(dx, p, Math.Max(Th.D(300), W - dx - p), H - p * 2);
            empty.SetBounds(Th.D(8), Th.D(8), cDet.Width - Th.D(16), cDet.Height - Th.D(16));
            sc.SetBounds(Th.D(6), Th.D(8), cDet.Width - Th.D(12), cDet.Height - Th.D(16));
            LayoutDetail();
            cCal.SetBounds(lx, p, W - lx - p, H - p * 2);
            cal.SetBounds(Th.D(4), Th.D(4), cCal.Width - Th.D(8), cCal.Height - Th.D(8));
            cRep.SetBounds(lx, p, W - lx - p, H - p * 2);
            repSc.SetBounds(Th.D(6), Th.D(8), cRep.Width - Th.D(12), cRep.Height - Th.D(16));
            LayoutReport();
        }

        void LayoutList()
        {
            int w = cList.Width, pad = Th.D(14);
            bNew.Width = bNew.PrefW(); bNew.Location = new Point(w - pad - bNew.Width, Th.D(10));
            search.SetBounds(pad, Th.D(52), w - pad * 2, search.Height);
            seg.SetBounds(pad, search.Bottom + Th.D(10), w - pad * 2, Th.D(32));
            int y = seg.Bottom + Th.D(10), x = pad;
            foreach (var b in new[] { fStage, fUcret })
            {
                b.Width = b.PrefW(); b.Location = new Point(x, y); x = b.Right + Th.D(6);
            }
            fClear.SetBounds(x, y, Th.D(28), Th.D(28));
            fSort.Width = Math.Min(fSort.PrefW(), w - pad - fClear.Right - Th.D(10));
            fSort.Location = new Point(w - pad - fSort.Width, y);
            list.SetBounds(Th.D(4), y + Th.D(38), w - Th.D(8), cList.Height - y - Th.D(46));
        }

        void LayoutDetail()
        {
            if (sc == null || cur == null) return;
            sc.Content.SuspendLayout();
            int W = sc.ClientSize.Width - Th.D(10);
            dc.Width = sc.ClientSize.Width;
            int p = Th.D(22), x = p, w = W - p * 2, y = Th.D(14), gap = Th.D(12);
            int rs = Th.D(54);
            bDel.SetBounds(x + w - Th.D(36), y + Th.D(9), Th.D(36), Th.D(36));
            bRestore.Visible = cur.arsiv;
            tip.SetToolTip(bDel, cur.arsiv ? "Kalıcı olarak sil" : "Arşive taşı");
            int rx = bDel.Left;
            if (cur.arsiv) { bRestore.Width = bRestore.PrefW(); bRestore.Location = new Point(bDel.Left - bRestore.Width - Th.D(8), y + Th.D(11)); rx = bRestore.Left; }
            ring.SetBounds(rx - rs - Th.D(10), y, rs, rs);
            fName.SetBounds(x - Th.D(4), y + (rs - fName.Height) / 2, ring.Left - x - Th.D(10), fName.Height);
            y += rs + Th.D(6);
            sline.SetBounds(x, y, w, Th.D(26)); y += Th.D(26) + Th.D(14);

            // Durum
            y = Section(hDurum, y);
            int half = (w - gap * 2) / 2;
            lGenel.SetBounds(x, y, half, Th.D(20)); lUcret.SetBounds(x + half + gap * 2, y, half, Th.D(20)); y += Th.D(22);
            sgDurum.SetBounds(x, y, half, Th.D(32)); sgUcret.SetBounds(x + half + gap * 2, y, half, Th.D(32)); y += Th.D(32) + Th.D(18);

            // Planlama
            y = Section(hPlan, y);
            int third = (w - gap * 2) / 3;
            bKesifYok.Text = cur.kesifYok ? "✓ Keşif yapılmadı — kalemler firma tarafından iletildi" : "Keşif gerekmedi — kalemler firma tarafından iletildi";
            bKesifYok.Active = cur.kesifYok; bKesifYok.Width = bKesifYok.PrefW(); bKesifYok.Location = new Point(x, y);
            y += bKesifYok.Height + Th.D(10);
            lKe.Text = cur.kesifYok ? "Keşif tarihi (pasif)" : "Keşif tarihi";
            lKe.SetBounds(x, y, third, Th.D(20)); lSo.SetBounds(x + third + gap, y, third, Th.D(20)); lKo.SetBounds(x + (third + gap) * 2, y, third, Th.D(20)); y += Th.D(22);
            dKe.SetBounds(x, y, third, dKe.Height); dSo.SetBounds(x + third + gap, y, third, dSo.Height); dKo.SetBounds(x + (third + gap) * 2, y, third, dKo.Height);
            y += dKe.Height + Th.D(8);
            int dch = dayc.Lay(w); dayc.SetBounds(x, y, w, dch); y += dch + Th.D(4);
            bool yil = Flow.KontrolBitti(cur).HasValue;
            dYil.Visible = bDonem.Visible = yil;
            {
                y += Th.D(6);
                lYil.SetBounds(x, y, w, Th.D(20)); y += Th.D(22);
                int bx = x;
                if (yil) { dYil.SetBounds(x, y, third, dYil.Height); bx = x + third + gap; }
                int by = y + (dYil.Height - Th.D(30)) / 2;
                bPer.Text = "Periyot: " + Flow.PeriyotText(cur.periyotAy); bPer.Height = Th.D(30); bPer.Width = bPer.PrefW(); bPer.Location = new Point(bx, by);
                bHat.Text = "Hatırlat: " + Flow.Hatirlat(cur) + " gün önce"; bHat.Height = Th.D(30); bHat.Width = bHat.PrefW(); bHat.Location = new Point(bPer.Right + Th.D(6), by);
                y += dYil.Height + Th.D(6);
                bDonem.Width = bDonem.PrefW();
                bDonem.Location = new Point(x + w - bDonem.Width, y);
                yilHint.SetBounds(x, y, (yil ? bDonem.Left - Th.D(10) : x + w) - x, bDonem.Height);
                y += bDonem.Height + Th.D(4);
            }
            bool ku = cur.kusur == Flow.KusurVar;
            lKu.Visible = dKu.Visible = kuHint.Visible = bOto.Visible = ku;
            if (ku)
            {
                y += Th.D(6);
                lKu.SetBounds(x, y, third, Th.D(20)); y += Th.D(22);
                dKu.SetBounds(x, y, third, dKu.Height);
                bOto.Text = cur.otoKapat ? "✓ Süre dolunca otomatik kapat" : "Otomatik kapatma kapalı"; bOto.Active = cur.otoKapat;
                bOto.Width = bOto.PrefW(); bOto.Location = new Point(x + w - bOto.Width, y + (dKu.Height - bOto.Height) / 2);
                kuHint.SetBounds(x + third + gap, y, bOto.Left - gap - (x + third + gap), dKu.Height);
                y += dKu.Height + Th.D(4);
            }
            int ph = planHint.Text == "" ? 0 : Th.D(24);
            planHint.SetBounds(x, y, w, ph); planHint.Visible = ph > 0;
            y += ph + Th.D(14);

            // İş akışı
            y = Section(hAkis, y);
            bool two = w >= Th.D(600);
            var vis = stages.ToList();
            foreach (var s in stages) { s.Visible = true; s.Num = s.S + 1; }
            if (two)
            {
                int cwid = (w - gap) / 2;
                for (int r = 0; r < vis.Count; r += 2)
                {
                    var a = vis[r]; var b = r + 1 < vis.Count ? vis[r + 1] : null;
                    int h = Math.Max(a.Calc(cwid), b != null ? b.Calc(cwid) : 0);
                    a.SetBounds(x, y, cwid, h);
                    if (b != null) b.SetBounds(x + cwid + gap, y, cwid, h);
                    y += h + gap;
                }
            }
            else
                foreach (var s in vis) { int h = s.Calc(w); s.SetBounds(x, y, w, h); y += h + gap; }
            y += Th.D(6);

            // Notlar
            y = Section(hNot, y);
            int chH = chips.Lay(w); chips.SetBounds(x, y, w, chH); y += chH + Th.D(10);
            fNote.SetBounds(x, y, w, Th.D(76)); y += Th.D(76) + Th.D(8);
            bNoteAdd.Width = bNoteAdd.PrefW(); bNoteAdd.Location = new Point(x, y);
            bNoteCancel.Width = bNoteCancel.PrefW(); bNoteCancel.Location = new Point(bNoteAdd.Right + Th.D(6), y);
            int hx = (bNoteCancel.Visible ? bNoteCancel.Right : bNoteAdd.Right) + Th.D(12);
            noteHint.SetBounds(hx, y, x + w - hx, Th.D(32));
            y += Th.D(32) + Th.D(12);
            int nh = notes.Calc(w); notes.SetBounds(x, y, w, nh); y += nh + Th.D(22);

            // İletişim
            y = Section(hIlet, y);
            lY.SetBounds(x, y, half, Th.D(20)); lT.SetBounds(x + half + gap * 2, y, half, Th.D(20)); y += Th.D(22);
            fY.SetBounds(x, y, half, fY.Height); fT.SetBounds(x + half + gap * 2, y, half, fT.Height); y += fY.Height + Th.D(10);
            lE.SetBounds(x, y, half, Th.D(20)); lA.SetBounds(x + half + gap * 2, y, half, Th.D(20)); y += Th.D(22);
            fE.SetBounds(x, y, half, fE.Height); fA.SetBounds(x + half + gap * 2, y, half, fA.Height); y += fE.Height + Th.D(28);

            dc.Height = y;
            sc.Content.ResumeLayout();
            sc.ContentChanged();
            foreach (Control c in dc.Controls) c.Invalidate();
        }
        int Section(Label h, int y)
        {
            int p = Th.D(22);
            h.SetBounds(p, y, dc.Width - p * 2, Th.D(22));
            return y + Th.D(28);
        }

        // ═════════════════════════ VERİ → EKRAN ═════════════════════════
        void RefreshAll()
        {
            RefreshCounts();
            RefreshList();
            if (cur != null) RefreshDetailLive();
        }

        void RefreshCounts()
        {
            var fs = Live();
            int dev = fs.Count(f => Flow.Category(f) == Flow.Cat.Devam), kus = fs.Count(f => Flow.Category(f) == Flow.Cat.Kusur),
                tam = fs.Count(f => Flow.Category(f) == Flow.Cat.Tamam), ip = fs.Count(f => Flow.Category(f) == Flow.Cat.Iptal);
            int ktm = fs.Count(f => Flow.Category(f) == Flow.Cat.KusurluTamam);
            kAll.Value = fs.Count; kDev.Value = dev; kKus.Value = kus; kTam.Value = tam; kKtm.Value = ktm; kIp.Value = ip;
            var tl = new[] { kAll, kDev, kKus, kTam, kKtm, kIp };
            for (int i = 0; i < tl.Length; i++) { tl[i].Selected = i > 0 && seg.Sel == i; tl[i].Invalidate(); }
            seg.SetItems("Tümü", "Devam", "Kusur", "Uygun", "Kusurlu", "İptal");
            cList.Sub = null;

            var act = fs.Where(f => !Flow.IsIptal(f)).ToList();
            donut.Segs.Clear();
            for (int i = 0; i <= 7; i++)
            {
                int k = i;
                donut.Segs.Add(new Donut.Seg { Key = i, Label = i == 6 ? "Uygun tamam" : i == 7 ? "Kusurlu tamam" : Flow.StageName(i), Color = Flow.StageColor(i), Count = act.Count(f => Flow.Bucket(f) == k) });
            }
            donut.SelKey = filterStage; donut.Invalidate();

            var items = new List<Upcoming.Item>();
            var today = DateTime.Today;
            foreach (var f in act)
            {
                var cat = Flow.Category(f);
                // Yıllık periyodik kontrol: tarihten 30 gün önce hatırlat
                var ny = Flow.NextYearly(f);
                if (ny.HasValue)
                {
                    int yd = (int)(ny.Value - today).TotalDays;
                    if (yd <= Flow.Hatirlat(f))
                        items.Add(new Upcoming.Item { F = f, Date = ny, Text = yd < 0 ? "Periyodik kontrol zamanı " + (-yd) + " gün geçti" : "Periyodik kontrol (" + Flow.PeriyotText(f.periyotAy) + ") · " + When(yd), Color = yd < 0 ? Th.Red : Th.Green });
                }
                if (Flow.Idle(f))
                    items.Add(new Upcoming.Item { F = f, Date = null, Text = "Beklemede · " + Flow.IdleDays(f) + " gündür işlem yok", Color = Th.H("#C99A00") });
                if (cat == Flow.Cat.Tamam || cat == Flow.Cat.KusurluTamam) continue;
                // Keşif
                var d = U.PD(f.kesifTarih);
                if (d.HasValue && !Flow.Done(f, 1, 1))
                {
                    int dd = (int)(d.Value - today).TotalDays;
                    if (dd >= 0 && dd <= 30) items.Add(new Upcoming.Item { F = f, Date = d, Text = "Keşif · " + When(dd), Color = dd == 0 ? Th.Orange : Th.Blue });
                    else if (dd < 0 && dd >= -14) items.Add(new Upcoming.Item { F = f, Date = d, Text = "Keşif · " + (-dd) + " gün geçti, “yapıldı” işaretlenmedi", Color = Th.GrayDark });
                }
                // Kontrol günleri: geçmiş günler gecikme sayılmaz (kontrol birkaç güne yayılabilir)
                if (!Flow.StageDone(f, 3))
                {
                    var days = Flow.Days(f);
                    var next = days.Where(x => x >= today && x <= today.AddDays(30)).ToList();
                    foreach (var x in next)
                    {
                        int dd = (int)(x - today).TotalDays;
                        string nth = days.Count > 1 ? " (" + (days.IndexOf(x) + 1) + ". gün)" : "";
                        items.Add(new Upcoming.Item { F = f, Date = x, Text = "Kontrol" + nth + " · " + When(dd), Color = Th.Orange });
                    }
                    if (next.Count == 0 && days.Count > 0 && f.kontrolDevam)
                        items.Add(new Upcoming.Item { F = f, Date = null, Text = "Kontrol sürüyor · tarih belirsiz", Color = Th.GrayDark });
                }
                // Kusur giderme süresi (30 gün)
                int? kg = Flow.KusurGun(f);
                if (kg.HasValue)
                {
                    int left = Flow.KUSUR_GUN - kg.Value;
                    var due = U.PT(f.kusurTarih).Value.Date.AddDays(Flow.KUSUR_GUN);
                    items.Add(new Upcoming.Item { F = f, Date = due, Text = left > 0 ? "Kusur süresi · " + kg.Value + ". gün, " + left + " gün kaldı" : (f.otoKapat ? "Kusur süresi doldu · otomatik kapanacak" : "Kusur süresi doldu · kapatılabilir"), Color = left > 15 ? Th.Orange : Th.Red });
                }
            }
            up.Items = items.OrderBy(i => i.Date.HasValue ? 0 : 1).ThenBy(i => i.Date ?? DateTime.MaxValue).ToList(); up.Invalidate();
            cUp.Sub = items.Count > 0 ? items.Count.ToString() : null; cUp.Invalidate();
        }

        static string When(int dd) { return dd == 0 ? "bugün" : dd == 1 ? "yarın" : dd + " gün sonra"; }

        bool Match(Firm f, string q)
        {
            if (q == "") return true;
            Func<string, bool> has = s => U.Low(s).Contains(q);
            return has(f.ad) || has(f.yetkili) || has(f.tel) || has(f.eposta) || has(f.adres) || f.notlar.Any(n => has(n.metin));
        }

        void RefreshList()
        {
            string q = U.Low(search.Value.Trim());
            var baseSet = st.Firms.Where(f => f.arsiv == (view == 3)).ToList();
            IEnumerable<Firm> r = baseSet.Where(f => Match(f, q));
            if (seg.Sel > 0) r = r.Where(f => (int)Flow.Category(f) == seg.Sel);
            if (filterStage == 99) r = r.Where(f => Flow.Idle(f) || (f.arsiv && Flow.Category(f) == Flow.Cat.Devam && Flow.IdleDays(f) >= Flow.IDLE_GUN));
            else if (filterStage >= 0) r = r.Where(f => !Flow.IsIptal(f) && Flow.Bucket(f) == filterStage);
            if (filterUcret != null) r = r.Where(f => f.ucret == filterUcret);
            var cmp = StringComparer.Create(Th.TR, true);
            switch (sortMode)
            {
                case 1: r = r.OrderBy(f => f.ad, cmp); break;
                case 2: r = r.OrderBy(f => { var n = Flow.Days(f).Where(x => x >= DateTime.Today).Select(x => x.ToString("yyyy-MM-dd")).FirstOrDefault(); return n ?? (f.kontrolTarih == "" ? "9999" : "8" + f.kontrolTarih); }).ThenBy(f => f.ad, cmp); break;
                case 3: r = r.OrderBy(f => f.kesifTarih == "" ? "9999" : f.kesifTarih).ThenBy(f => f.ad, cmp); break;
                case 4: r = r.OrderByDescending(f => Flow.Percent(f)).ThenBy(f => f.ad, cmp); break;
                default: r = r.OrderByDescending(f => f.guncelleme, StringComparer.Ordinal); break;
            }
            var l = r.ToList();
            list.EmptyText = view == 3 ? (baseSet.Count == 0 ? "Arşiv boş.\n\nFirmayı sildiğinizde buraya taşınır; buradan geri alabilir veya kalıcı silebilirsiniz." : "Bu filtreye uygun firma yok.") : baseSet.Count == 0 ? "Henüz firma eklenmedi.\n\nBaşlamak için sağ üstteki “Yeni Firma” düğmesine tıklayın." : "Bu filtreye uygun firma yok.";
            list.SetItems(l);
            list.Selected = cur; list.EnsureVisible();

            fStage.Text = filterStage < 0 ? "Aşama" : filterStage == 99 ? "Beklemede" : Flow.StageName(filterStage); fStage.Active = filterStage >= 0;
            fUcret.Text = filterUcret == null ? "Ücret" : (filterUcret == "" ? "Belirtilmemiş" : filterUcret); fUcret.Active = filterUcret != null;
            fSort.Text = SortNames[sortMode];
            bool any = filterStage >= 0 || filterUcret != null || seg.Sel != 0 || search.Value != "";
            fClear.Visible = any;
            cList.Sub = l.Count != baseSet.Count ? l.Count + " / " + baseSet.Count : baseSet.Count.ToString();
            cList.Invalidate();
            LayoutList();
        }

        void SelectFirm(Firm f)
        {
            list.Select(f, false);
            ShowDetail(f);
        }

        void ShowDetail(Firm f)
        {
            if (cur != f) { editing = null; if (chips != null) chips.Sel = "all"; }
            cur = f;
            list.Selected = f; list.Invalidate();
            empty.Visible = f == null; sc.Visible = f != null;
            if (f == null) return;
            Bind();
            sc.SetOff(0);
        }

        void Bind()
        {
            binding = true;
            var f = cur;
            fName.Value = f.ad;
            sgDurum.Sel = Flow.IsIptal(f) ? 1 : 0; sgDurum.Invalidate();
            sgUcret.Sel = f.ucret == Flow.Ucretsiz ? 1 : (f.ucret == Flow.Ucretli ? 0 : -1); sgUcret.Invalidate();
            fY.Value = f.yetkili; fT.Value = f.tel; fE.Value = f.eposta; fA.Value = f.adres;
            dKe.SetValue(U.PD(f.kesifTarih), false); dKe.Enabled = !f.kesifYok; dSo.SetValue(U.PD(f.sozlesmeTarih), false); dKo.SetValue(U.PD(f.kontrolTarih), false);
            foreach (var s in stages) s.F = f;
            dayc.F = f; dKo.SetValue(null, false);
            dYil.SetValue(Flow.NextYearly(f), false);
            var kt0 = U.PT(f.kusurTarih); dKu.SetValue(kt0.HasValue ? kt0.Value.Date : (DateTime?)null, false);
            CancelEdit(false);
            binding = false;
            RefreshDetailLive();
        }

        void RefreshDetailLive()
        {
            if (cur == null) return;
            var f = cur;
            var cat = Flow.Category(f); int c = Flow.Current(f);
            ring.Percent = Flow.Percent(f); ring.Color = Flow.CatColor(cat) == Th.Orange ? Flow.StageColor(c) : Flow.CatColor(cat); ring.Invalidate();
            sline.F = f; sline.Invalidate();
            foreach (var s in stages)
            {
                s.IsCurrent = !Flow.IsIptal(f) && s.S == c;
                s.NoteCount = f.notlar.Count(n => n.asama == Flow.S[s.S].Key);
                s.Invalidate();
            }
            // planlama uyarısı
            var so = U.PD(f.sozlesmeTarih); var ko = U.PD(f.kontrolTarih);
            dayc.Invalidate();
            int dchNew = dayc.Width > 0 ? dayc.Lay(dayc.Width) : 0;
            bool relayDays = dayc.Width > 0 && dchNew != dayc.Height;
            string hint = ""; Color hc = Th.Red;
            if (ko.HasValue && !so.HasValue) hint = "⚠  İSG-KATİP sözleşme tarihi girilmedi. Sözleşme yapılmadan kontrol yapılmaz.";
            else if (ko.HasValue && so.HasValue && ko.Value <= so.Value) hint = "⚠  İlk kontrol günü, İSG-KATİP sözleşmesinden en erken 1 gün sonra olmalı.";
            else if (ko.HasValue && so.HasValue) { hint = "✓  Kontrol günleri uygun (sözleşmeden sonra)."; hc = Th.Green; }
            bool yilVis = Flow.KontrolBitti(f).HasValue;
            bPer.Text = "Periyot: " + Flow.PeriyotText(f.periyotAy); bHat.Text = "Hatırlat: " + Flow.Hatirlat(f) + " gün önce";
            bPer.Width = bPer.PrefW(); bHat.Left = bPer.Right + Th.D(6); bHat.Width = bHat.PrefW();
            if (!yilVis) { yilHint.Text = "Kontrol (elektrik/mekanik) bitince sonraki tarih kendiliğinden hesaplanır: bitiş günü + " + Flow.PeriyotText(f.periyotAy) + "."; yilHint.ForeColor = Th.Sub; }
            if (yilVis)
            {
                var ny = Flow.NextYearly(f).Value; var kb = Flow.KontrolBitti(f).Value;
                if (!dYil.Box.Focused) { binding = true; dYil.SetValue(ny, false); binding = false; }
                int yd = (int)(ny - DateTime.Today).TotalDays;
                bool manual = U.PD(f.sonrakiKontrol).HasValue;
                yilHint.Text = (manual ? "Tarih elle ayarlandı (temizlerseniz otomatiğe döner)" : "Kontrolün bittiği " + kb.ToString("dd.MM.yyyy") + " + " + Flow.PeriyotText(f.periyotAy)) + " · " +
                    (yd < 0 ? (-yd) + " gün geçti" : yd + " gün kaldı") + " · hatırlatma " + ny.AddDays(-Flow.Hatirlat(f)).ToString("dd.MM.yyyy") + " tarihinde başlar";
                yilHint.ForeColor = yd < 0 ? Th.Red : yd <= Flow.Hatirlat(f) ? Th.Orange : Th.GrayDark;
            }
            bool kuVis = f.kusur == Flow.KusurVar;
            if (!dKu.Box.Focused) { var kt1 = U.PT(f.kusurTarih); binding = true; dKu.SetValue(kt1.HasValue ? kt1.Value.Date : (DateTime?)null, false); binding = false; }
            int? kg = Flow.KusurGun(f);
            if (!kuVis) kuHint.Text = "";
            else if (!kg.HasValue) { kuHint.Text = "Tarih girilmezse 30 günlük süre takibi yapılmaz."; kuHint.ForeColor = Th.Sub; }
            else
            {
                var due = U.PT(f.kusurTarih).Value.Date.AddDays(Flow.KUSUR_GUN);
                int left = Flow.KUSUR_GUN - kg.Value;
                kuHint.Text = kg.Value < 0 ? "Süre " + U.PT(f.kusurTarih).Value.ToString("dd.MM.yyyy") + " tarihinde başlayacak · son gün " + due.ToString("dd.MM.yyyy")
                    : left > 0 ? "Bugün " + kg.Value + ". gün · " + left + " gün kaldı · son gün " + due.ToString("dd.MM.yyyy")
                    : Flow.KUSUR_GUN + " günlük süre " + due.ToString("dd.MM.yyyy") + " tarihinde doldu" + (f.otoKapat ? " · otomatik kapanacak" : " · hizmet kapatılabilir");
                kuHint.ForeColor = kg.Value >= Flow.KUSUR_GUN ? Th.Red : kg.Value >= 15 ? Th.Orange : Th.GrayDark;
            }
            bool relayout = (planHint.Text == "") != (hint == "") || relayDays || dKu.Visible != kuVis || dYil.Visible != yilVis || bRestore.Visible != f.arsiv;
            planHint.Text = hint; planHint.ForeColor = hc;
            RefreshNotes();
            if (relayout) LayoutDetail();
        }

        void RefreshNotes()
        {
            var f = cur; if (f == null) return;
            chips.Chips.Clear();
            chips.Chips.Add(new ChipBar.Chip { Key = "all", Text = "Tümü", Count = f.notlar.Count });
            foreach (var t in Flow.Topics)
                chips.Chips.Add(new ChipBar.Chip { Key = t, Text = Flow.TopicName(t), Color = Flow.TopicColor(t), Count = f.notlar.Count(n => n.asama == t) });
            chips.Invalidate();
            var l = f.notlar.Where(n => chips.Sel == "all" || n.asama == chips.Sel).OrderByDescending(n => n.tarih, StringComparer.Ordinal).ToList();
            int oldH = notes.Calc(notes.Width > 0 ? notes.Width : 400);
            notes.Items = l; notes.Editing = editing;
            notes.EmptyText = chips.Sel == "all" ? "Henüz not yok. Bir konu seçip notunuzu yazın." : "“" + Flow.TopicName(chips.Sel) + "” konusunda not yok.";
            notes.Invalidate();
            string topic = editing != null ? (chips.Sel == "all" ? editing.asama : chips.Sel) : (chips.Sel == "all" ? "genel" : chips.Sel);
            bNoteAdd.Text = editing != null ? "Güncelle · " + Flow.TopicName(topic) : "Not ekle · " + Flow.TopicName(topic);
            bNoteAdd.IconKind = editing != null ? "check" : "plus";
            bNoteCancel.Visible = editing != null;
            if (notes.Width > 0 && notes.Calc(notes.Width) != oldH) LayoutDetail();
            else { bNoteAdd.Width = bNoteAdd.PrefW(); bNoteCancel.Left = bNoteAdd.Right + Th.D(6); int hx = (bNoteCancel.Visible ? bNoteCancel.Right : bNoteAdd.Right) + Th.D(12); noteHint.SetBounds(hx, noteHint.Top, notes.Right - hx, noteHint.Height); }
        }

        // ═════════════════════════ NOTLAR ═════════════════════════
        void AddNote()
        {
            if (cur == null) return;
            string t = fNote.Value.Trim();
            if (t == "") { fNote.FocusBox(); return; }
            if (editing != null)
            {
                editing.metin = t;
                if (chips.Sel != "all") editing.asama = chips.Sel;
                editing = null;
            }
            else
                cur.notlar.Add(new Note { id = U.NewId(), asama = chips.Sel == "all" ? "genel" : chips.Sel, metin = t, tarih = U.Now() });
            fNote.Value = "";
            Changed();
            LayoutDetail();
        }
        void StartEdit(Note n)
        {
            editing = n; fNote.Value = n.metin;
            RefreshNotes(); LayoutDetail();
            fNote.FocusBox(); fNote.Box.SelectionStart = fNote.Box.Text.Length;
            sc.EnsureVisible(fNote);
        }
        void CancelEdit(bool refresh = true)
        {
            if (editing != null) fNote.Value = "";
            editing = null;
            if (refresh) { RefreshNotes(); LayoutDetail(); }
        }
        void DeleteNote(Note n)
        {
            if (MessageBox.Show(this, "Bu not silinsin mi?\n\n" + (n.metin.Length > 160 ? n.metin.Substring(0, 160) + "…" : n.metin), "Notu sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            cur.notlar.Remove(n);
            if (editing == n) { editing = null; fNote.Value = ""; }
            Changed(); LayoutDetail();
        }

        // ═════════════════════════ FİRMA İŞLEMLERİ ═════════════════════════
        void NewFirm()
        {
            using (var d = new NewFirmDialog(st.Firms))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                var now = U.Now();
                var f = new Firm { id = U.NewId(), ad = d.FirmName, olusturma = now, guncelleme = now };
                f.checks["0-0"] = now;
                st.Firms.Add(f);
                ClearFilters();
                cur = null;
                SelectFirm(f);
                Changed(false);
                fY.FocusBox();
            }
        }
        void RestoreFirm()
        {
            if (cur == null || !cur.arsiv) return;
            var f = cur;
            f.arsiv = false; f.arsivTarih = ""; f.guncelleme = U.Now();
            ScheduleSave();
            GoFirm(f);
        }
        void NumMenu(PillButton b, string title, int[] opts, int curVal, Func<int, string> fmt, string unit, Action<int> set)
        {
            if (cur == null) return;
            var m = MkMenu();
            m.Items.Add(new ToolStripLabel(title) { Font = Th.F(8.5f, true), ForeColor = Th.Sub });
            foreach (var o in opts)
            {
                int v = o; var it = new ToolStripMenuItem(fmt(v) + (v == (unit == "ay" ? 12 : 30) ? "  (varsayılan)" : "")) { Checked = curVal == v };
                it.Click += (s, e) => { set(v); Changed(); LayoutDetail(); };
                m.Items.Add(it);
            }
            m.Items.Add(new ToolStripSeparator());
            var other = new ToolStripMenuItem("Diğer… (" + unit + ")") { Checked = !opts.Contains(curVal) };
            other.Click += (s, e) =>
            {
                int v;
                if (AskNumber(title + " (" + unit + ")", curVal, out v)) { set(v); Changed(); LayoutDetail(); }
            };
            m.Items.Add(other);
            m.Show(b, new Point(0, b.Height + Th.D(2)));
        }
        bool AskNumber(string title, int init, out int val)
        {
            val = init;
            using (var f = new Form { Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, StartPosition = FormStartPosition.CenterParent, BackColor = Color.White, AutoScaleMode = AutoScaleMode.None, Font = Th.F(9.5f), ClientSize = new Size(Th.D(320), Th.D(130)) })
            {
                var nb = new NumericUpDown { Minimum = 1, Maximum = 999, Value = Math.Max(1, Math.Min(999, init)), Font = Th.F(11f), Location = new Point(Th.D(20), Th.D(24)), Width = Th.D(120) };
                var ok = new Button { Text = "Tamam", DialogResult = DialogResult.OK, Location = new Point(Th.D(130), Th.D(80)), Size = new Size(Th.D(80), Th.D(30)) };
                var no = new Button { Text = "Vazgeç", DialogResult = DialogResult.Cancel, Location = new Point(Th.D(220), Th.D(80)), Size = new Size(Th.D(80), Th.D(30)) };
                f.Controls.AddRange(new Control[] { nb, ok, no }); f.AcceptButton = ok; f.CancelButton = no;
                if (f.ShowDialog(this) != DialogResult.OK) return false;
                val = (int)nb.Value; return true;
            }
        }
        void NewPeriod()
        {
            if (cur == null) return;
            var f = cur; var ny = Flow.NextYearly(f);
            var kb = Flow.KontrolBitti(f);
            string yil = kb.HasValue ? kb.Value.Year.ToString() : DateTime.Today.Year.ToString();
            if (MessageBox.Show(this, "“" + f.ad + "” için " + yil + " dönemi kapatılsın ve iş akışı yeni yıl için sıfırlansın mı?\n\n" +
                "• Notlar, iletişim bilgileri ve ücret bilgisi korunur.\n• Bu dönemin özeti not olarak eklenir.\n" +
                (ny.HasValue ? "• Sonraki kontrol günü olarak " + ny.Value.ToString("dd.MM.yyyy") + " eklenir." : ""),
                "Yeni dönem başlat", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            var ozet = new Dictionary<string, object> {
                { "kapanis", U.Now() }, { "kesifTarih", f.kesifTarih }, { "sozlesmeTarih", f.sozlesmeTarih },
                { "kontrolGunleri", new List<string>(f.kontrolGunleri) }, { "kusur", f.kusur }, { "checks", new Dictionary<string, string>(f.checks) } };
            f.donemler.Add(ozet);
            string metin = yil + " dönemi kapatıldı. Keşif: " + (U.DS(f.kesifTarih) == "" ? "—" : U.DS(f.kesifTarih)) +
                " · İSG-KATİP: " + (U.DS(f.sozlesmeTarih) == "" ? "—" : U.DS(f.sozlesmeTarih)) +
                " · Kontrol: " + (f.kontrolGunleri.Count == 0 ? "—" : string.Join(", ", f.kontrolGunleri.Select(U.DS))) +
                " · Kusur: " + (f.kusur == "" ? "—" : f.kusur) + ".";
            f.notlar.Add(new Note { id = U.NewId(), asama = "genel", metin = metin, tarih = U.Now() });
            var keep = f.checks.Where(kv => kv.Key.StartsWith("0-")).ToDictionary(kv => kv.Key, kv => kv.Value);
            f.checks = keep;
            f.kesifTarih = ""; f.sozlesmeTarih = ""; f.kusur = ""; f.kusurTarih = ""; f.kontrolDevam = false; f.sonrakiKontrol = "";
            f.kontrolGunleri = ny.HasValue ? new List<string> { ny.Value.ToString("yyyy-MM-dd") } : new List<string>();
            Flow.SyncKontrol(f);
            Bind();
            Changed();
            LayoutDetail();
        }
        void DeleteFirm()
        {
            if (cur == null) return;
            if (!cur.arsiv)
            {
                if (MessageBox.Show(this, "“" + cur.ad + "” arşive taşınsın mı?\n\nListelerden, sayımlardan ve takvimden çıkar. Sol menüdeki “Arşiv”den istediğiniz zaman geri alabilirsiniz.", "Arşive taşı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                int j = list.Items.IndexOf(cur);
                cur.arsiv = true; cur.arsivTarih = U.Now();
                cur = null;
                RefreshAll();
                Firm nx = list.Items.Count == 0 ? null : list.Items[Math.Max(0, Math.Min(j, list.Items.Count - 1))];
                if (nx != null) SelectFirm(nx); else ShowDetail(null);
                ScheduleSave();
                return;
            }
            if (MessageBox.Show(this, "“" + cur.ad + "” firması ve tüm bilgileri (notlar dahil) KALICI olarak silinecek. Bu işlem geri alınamaz.\n\nEmin misiniz?", "Kalıcı olarak sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            int i = list.Items.IndexOf(cur);
            st.Firms.Remove(cur);
            cur = null;
            RefreshAll();
            Firm next = list.Items.Count == 0 ? null : list.Items[Math.Max(0, Math.Min(i, list.Items.Count - 1))];
            if (next != null) SelectFirm(next); else ShowDetail(null);
            ScheduleSave();
        }

        void Changed(bool touch = true)
        {
            if (cur != null && touch) cur.guncelleme = U.Now();
            RefreshCounts();
            RefreshList();
            RefreshDetailLive();
            if (cal != null && cCal.Visible) cal.Invalidate();
            if (rep != null && cRep.Visible) LayoutReport();
            ScheduleSave();
        }

        // ═════════════════════════ KAYIT ═════════════════════════
        void ScheduleSave() { saveT.Stop(); saveT.Start(); lblSave.Text = "Kaydediliyor…"; lblSave.ForeColor = Th.Sub; }
        void DoSave() { st.Save(); UpdSaveLabel(); }
        void UpdSaveLabel()
        {
            if (st.LastError != null) { lblSave.Text = "⚠ Kaydedilemedi!"; lblSave.ForeColor = Th.Red; tip.SetToolTip(lblSave, st.LastError + "\nVeri klasörünü kontrol edin."); }
            else if (st.LastSaved.HasValue) { lblSave.Text = "✓ Kaydedildi · " + st.LastSaved.Value.ToString("HH:mm"); lblSave.ForeColor = Th.Green; }
            else { lblSave.Text = "Değişiklikler otomatik kaydedilir"; lblSave.ForeColor = Th.Sub; }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            if (saveT.Enabled) { saveT.Stop(); DoSave(); }
            if (st.LastError != null && MessageBox.Show(this, "Son değişiklikler kaydedilemedi:\n" + st.LastError + "\n\nYine de çıkılsın mı?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            { e.Cancel = true; Application.AddMessageFilter(this); return; }
            base.OnFormClosing(e);
        }

        void ExportCsv()
        {
            using (var d = new SaveFileDialog { Filter = "Excel çalışma kitabı (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv", FileName = "periyodik-kontrol-" + DateTime.Today.ToString("yyyy-MM-dd") + ".xlsx", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    if (d.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) st.ExportCsv(d.FileName);
                    else Xlsx.Export(d.FileName, st.Firms);
                    if (MessageBox.Show(this, "Dışa aktarıldı:\n" + d.FileName + "\n\nŞimdi açılsın mı?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        Process.Start(d.FileName);
                }
                catch (Exception ex) { MessageBox.Show(this, "Dışa aktarılamadı: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        void ShowHelp()
        {
            MessageBox.Show(this,
                "KISAYOLLAR\n\n" +
                "Ctrl+N\tYeni firma\n" +
                "Ctrl+F\tAra\n" +
                "Ctrl+↑ / Ctrl+↓\tÖnceki / sonraki firma\n" +
                "Ctrl+1 … Ctrl+6\tTümü / Devam / Kusur / Uygun / Kusurlu / İptal\n" +
                "Ctrl+0\tTüm filtreleri temizle\n" +
                "F2\tFirma adını düzenle\n" +
                "Ctrl+Enter\tNotu kaydet (not kutusunda)\n" +
                "Ctrl+E\tExcel'e aktar (.xlsx)\n" +
                "Ctrl+L / Ctrl+T / Ctrl+R / Ctrl+H\tFirmalar / Takvim / Rapor / Arşiv\n" +
                "Esc\tNot düzenlemeyi / aramayı iptal et\n\n" +
                "İPUÇLARI\n\n" +
                "• Soldaki kutucuklara ve aşama dağılımına tıklayarak listeyi süzebilirsiniz.\n" +
                "• Aşama kartındaki “Not” çipi, o aşamanın notlarını açar.\n" +
                "• Kontrol birden fazla gün sürebilir: Planlama'dan gün ekleyin. Geçmiş günler “gecikti” sayılmaz; sonraki gün belli değilse “Devam edecek” işaretleyin.\n" +
                "• Kusur giderilmeyecekse 6. aşamada “hizmet kusurlu tamamlandı”yı seçin; istediğinizde başka seçenekle yeniden açabilirsiniz.\n" +
                "• " + Flow.KUSUR_GUN + " günlük kusur süresi dolup kusurlar giderilmediyse firma ertesi gün otomatik “kusurlu tamamlandı” olur, not eklenir ve bilgi verilir (firma bazında kapatılabilir).\n" +
                "• Kontrol aşamasında elektrik veya mekanik gerekmiyorsa satırın üzerine gelip “Gerekmiyor” deyin.\n" +
                "• Firmaya sağ tıklayarak durumunu değiştirebilir ya da arşive taşıyabilirsiniz. Arşivden geri alınabilir.\n" +
                "• " + Flow.IDLE_GUN + " gündür üzerinde işlem yapılmayan devam eden firmalar “Beklemede” olarak işaretlenir.\n" +
                "• Kontrol bitince sonraki periyodik kontrol tarihi kendiliğinden hesaplanır (varsayılan 1 yıl sonra, 30 gün önceden hatırlatma). Periyot ve hatırlatma süresi Planlama'dan her firma için ayrı değiştirilebilir. “Yeni dönem başlat” ile iş akışı yeni yıl için sıfırlanır.\n\n" +
                "Veriler: " + st.FilePath,
                "Periyodik Kontrol Takip", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ═════════════════════════ KLAVYE / FARE ═════════════════════════
        protected override bool ProcessCmdKey(ref Message msg, Keys k)
        {
            switch (k)
            {
                case Keys.Control | Keys.N: NewFirm(); return true;
                case Keys.Control | Keys.F: search.FocusBox(); search.Box.SelectAll(); return true;
                case Keys.Control | Keys.E: ExportCsv(); return true;
                case Keys.Control | Keys.L: SetView(0); return true;
                case Keys.Control | Keys.T: SetView(1); return true;
                case Keys.Control | Keys.R: SetView(2); return true;
                case Keys.Control | Keys.H: SetView(3); return true;
                case Keys.F1: ShowHelp(); return true;
                case Keys.F2: if (cur != null) { sc.SetOff(0); fName.FocusBox(); fName.Box.SelectAll(); } return true;
                case Keys.Control | Keys.Up: list.MoveSel(-1); return true;
                case Keys.Control | Keys.Down: list.MoveSel(1); return true;
                case Keys.Control | Keys.D0: ClearFilters(); return true;
                case Keys.Control | Keys.D1: case Keys.Control | Keys.D2: case Keys.Control | Keys.D3: case Keys.Control | Keys.D4: case Keys.Control | Keys.D5: case Keys.Control | Keys.D6:
                    seg.Sel = (int)(k & Keys.KeyCode) - (int)Keys.D1; RefreshAll(); return true;
                case Keys.Escape:
                    if (editing != null) { CancelEdit(); return true; }
                    if (search.Value != "") { search.Value = ""; RefreshList(); return true; }
                    list.Focus(); return true;
            }
            return base.ProcessCmdKey(ref msg, k);
        }

        // Fare tekerleğini imlecin altındaki alana yönlendir (Windows'ta odak nerede olursa olsun)
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != 0x020A || !Visible) return false;
            int delta = (short)((long)m.WParam >> 16);
            var p = Cursor.Position;
            if (cCal.Visible) return false;
            if (cDet.Visible && sc.Visible && sc.RectangleToScreen(sc.ClientRectangle).Contains(p))
            {
                var c = Control.FromHandle(m.HWnd) as TextBox;
                if (c == fNote.Box && fNote.Box.GetLineFromCharIndex(fNote.Box.TextLength) > 3 && fNote.Box.Focused) return false;
                sc.ScrollBy(-delta / 120 * Th.D(70)); return true;
            }
            if (list.Visible && list.RectangleToScreen(list.ClientRectangle).Contains(p)) { list.ScrollBy(-delta / 120 * Th.D(70)); return true; }
            if (cRep.Visible && repSc.RectangleToScreen(repSc.ClientRectangle).Contains(p)) { repSc.ScrollBy(-delta / 120 * Th.D(70)); return true; }
            return false;
        }

        // ── Ekran görüntüsü modu (geliştirme): PeriyodikKontrolTakip.exe --screenshot <klasör>
        //    Linux/Xvfb altında ImageMagick "import" ile ekranları sırayla kaydeder. Veriyi değiştirmez.
        public void Shot(string dir)
        {
            var t = new Timer { Interval = 1500 }; int step = 0;
            Action<string> snap = n => { try { var pr = Process.Start("import", "-window root " + Path.Combine(dir, n)); pr.WaitForExit(); } catch { } };
            t.Tick += (s, e) =>
            {
                switch (step++)
                {
                    case 0: var k = st.Firms.FirstOrDefault(x => !x.arsiv && x.kusur == Flow.KusurVar) ?? st.Firms.FirstOrDefault(); if (k != null) SelectFirm(k); break;
                    case 1: snap("01-firmalar.png"); sc.SetOff(Th.D(150)); break;
                    case 2: snap("02-planlama.png"); sc.SetOff(Th.D(560)); break;
                    case 3: snap("03-is-akisi.png"); sc.SetOff(100000); break;
                    case 4: snap("04-notlar.png"); SetView(1); break;
                    case 5: snap("05-takvim.png"); SetView(2); break;
                    case 6: snap("06-rapor.png"); repSc.SetOff(Th.D(560)); break;
                    case 7: snap("07-rapor-grafikler.png"); SetView(3); break;
                    case 8: snap("08-arsiv.png"); t.Stop(); saveT.Stop(); Close(); break;
                }
            };
            t.Start();
        }
    }

    // Yeni firma penceresi
    class NewFirmDialog : Form
    {
        readonly FieldBox name; readonly Label err; readonly List<Firm> firms;
        public string FirmName { get { return name.Value.Trim(); } }
        public NewFirmDialog(List<Firm> existing)
        {
            firms = existing;
            Text = "Yeni Firma"; AutoScaleMode = AutoScaleMode.None; Font = Th.F(9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent; BackColor = Color.White;
            ClientSize = new Size(Th.D(440), Th.D(210));
            Paint += (s, e) =>
            {
                G.T(e.Graphics, "Yeni Firma Ekle", Th.F(14f, true), new Rectangle(Th.D(24), Th.D(18), Width, Th.D(30)), Th.Text, G.L);
                G.T(e.Graphics, "Firma adını girin; diğer bilgileri sonra doldurabilirsiniz.", Th.F(9f), new Rectangle(Th.D(24), Th.D(48), Width, Th.D(20)), Th.Sub, G.L);
            };
            name = new FieldBox { Placeholder = "Örn. XYZ Plastik Ltd. Şti." };
            name.SetBounds(Th.D(24), Th.D(80), ClientSize.Width - Th.D(48), name.Height);
            err = new Label { ForeColor = Th.Red, Font = Th.F(8.75f), AutoSize = false, BackColor = Color.White };
            err.SetBounds(Th.D(26), name.Bottom + Th.D(4), ClientSize.Width - Th.D(48), Th.D(20));
            var ok = new PillButton { Text = "Ekle", Kind = "primary", BackColor = Color.White };
            var no = new PillButton { Text = "Vazgeç", Kind = "plain", BackColor = Color.White };
            ok.Width = Math.Max(Th.D(90), ok.PrefW()); no.Width = Math.Max(Th.D(90), no.PrefW());
            ok.Location = new Point(ClientSize.Width - Th.D(24) - ok.Width, ClientSize.Height - Th.D(52));
            no.Location = new Point(ok.Left - Th.D(8) - no.Width, ok.Top);
            ok.Click += (s, e) => Try();
            no.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.AddRange(new Control[] { name, err, ok, no });
            name.Box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { Try(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            };
            Shown += (s, e) => name.FocusBox();
        }
        void Try()
        {
            string n = FirmName;
            if (n == "") { err.Text = "Firma adı boş olamaz."; return; }
            if (firms.Any(f => string.Equals(f.ad.Trim(), n, StringComparison.CurrentCultureIgnoreCase) || U.Low(f.ad.Trim()) == U.Low(n)))
            { err.Text = "Bu isimde bir firma zaten kayıtlı."; return; }
            DialogResult = DialogResult.OK; Close();
        }
    }
}
