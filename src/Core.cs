using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PKT
{
    // ───────────────────────── Tema ─────────────────────────
    static class Th
    {
        public static float K = 1f;
        public static int D(float v) { return (int)Math.Round(v * K); }
        public static Color H(string h) { return ColorTranslator.FromHtml(h); }

        public static readonly Color Bg = H("#F2F2F7"), Text = H("#1C1C1E"), Sub = H("#8A8A8E"),
            Sep = H("#E5E5EA"), Field = H("#F2F2F7"), Soft = H("#F7F7FA"), Track = H("#EBEBF0"),
            Blue = H("#007AFF"), Green = H("#34C759"), Orange = H("#FF9500"), Red = H("#FF3B30"),
            Gray = H("#AEAEB2"), Indigo = H("#5856D6"), Purple = H("#AF52DE"), Teal = H("#30B0C7"),
            GrayDark = H("#8E8E93");

        public static readonly CultureInfo TR = new CultureInfo("tr-TR");

        static readonly Dictionary<string, Font> cache = new Dictionary<string, Font>();
        static string fam, semi;
        static void Init()
        {
            if (fam != null) return;
            var names = new HashSet<string>(new InstalledFontCollection().Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
            fam = names.Contains("Segoe UI") ? "Segoe UI" : SystemFonts.MessageBoxFont.FontFamily.Name;
            semi = names.Contains("Segoe UI Semibold") ? "Segoe UI Semibold" : null;
        }
        public static Font F(float size, bool bold = false)
        {
            Init();
            string key = size + "|" + bold;
            Font f;
            if (cache.TryGetValue(key, out f)) return f;
            if (bold) f = semi != null ? new Font(semi, size, FontStyle.Regular) : new Font(fam, size, FontStyle.Bold);
            else f = new Font(fam, size, FontStyle.Regular);
            cache[key] = f;
            return f;
        }
        public static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }
        public static Color Tint(Color c, float t) { return Mix(Color.White, c, t); }
        public static Color Dark(Color c, float t) { return Mix(c, Color.Black, t); }
    }

    // ───────────────────────── Çizim yardımcıları ─────────────────────────
    static class G
    {
        public const TextFormatFlags L = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        public const TextFormatFlags C = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        public const TextFormatFlags R = TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        public const TextFormatFlags W = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl;

        public static GraphicsPath RR(RectangleF r, float rad)
        {
            var p = new GraphicsPath();
            float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            if (d <= 1f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        public static void Fill(Graphics g, RectangleF r, float rad, Color c)
        {
            using (var p = RR(r, rad)) using (var b = new SolidBrush(c)) g.FillPath(b, p);
        }
        public static void Stroke(Graphics g, RectangleF r, float rad, Color c, float w)
        {
            using (var p = RR(r, rad)) using (var pen = new Pen(c, w)) g.DrawPath(pen, p);
        }
        public static void Circle(Graphics g, RectangleF r, Color c)
        {
            using (var b = new SolidBrush(c)) g.FillEllipse(b, r);
        }
        public static void Ring(Graphics g, RectangleF r, Color c, float w)
        {
            using (var p = new Pen(c, w)) g.DrawEllipse(p, r);
        }
        public static void AA(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
        public static void T(Graphics g, string s, Font f, Rectangle r, Color c, TextFormatFlags fl)
        {
            TextRenderer.DrawText(g, s ?? "", f, r, c, fl);
        }
        public static Size M(string s, Font f, int w, TextFormatFlags fl)
        {
            return TextRenderer.MeasureText(string.IsNullOrEmpty(s) ? " " : s, f, new Size(Math.Max(10, w), 100000), fl);
        }
        public static int TW(string s, Font f) { return TextRenderer.MeasureText(s ?? "", f, new Size(10000, 1000), L).Width; }

        public static void Icon(Graphics g, string k, RectangleF r, Color c, float w)
        {
            var old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(c, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                float x = r.X, y = r.Y, s = Math.Min(r.Width, r.Height);
                Func<float, float, PointF> P = (a, b) => new PointF(x + s * a, y + s * b);
                switch (k)
                {
                    case "search":
                        g.DrawEllipse(pen, x + s * .1f, y + s * .1f, s * .56f, s * .56f);
                        g.DrawLine(pen, P(.62f, .62f), P(.9f, .9f)); break;
                    case "plus":
                        g.DrawLine(pen, P(.5f, .15f), P(.5f, .85f)); g.DrawLine(pen, P(.15f, .5f), P(.85f, .5f)); break;
                    case "x":
                        g.DrawLine(pen, P(.25f, .25f), P(.75f, .75f)); g.DrawLine(pen, P(.75f, .25f), P(.25f, .75f)); break;
                    case "check":
                        g.DrawLines(pen, new[] { P(.2f, .52f), P(.42f, .73f), P(.8f, .3f) }); break;
                    case "minus":
                        g.DrawLine(pen, P(.28f, .5f), P(.72f, .5f)); break;
                    case "cal":
                        using (var p = RR(new RectangleF(x + s * .1f, y + s * .2f, s * .8f, s * .7f), s * .12f)) g.DrawPath(pen, p);
                        g.DrawLine(pen, P(.1f, .42f), P(.9f, .42f));
                        g.DrawLine(pen, P(.32f, .08f), P(.32f, .28f)); g.DrawLine(pen, P(.68f, .08f), P(.68f, .28f)); break;
                    case "trash":
                        g.DrawLine(pen, P(.14f, .26f), P(.86f, .26f));
                        g.DrawLine(pen, P(.38f, .26f), P(.42f, .12f)); g.DrawLine(pen, P(.42f, .12f), P(.58f, .12f)); g.DrawLine(pen, P(.58f, .12f), P(.62f, .26f));
                        g.DrawLines(pen, new[] { P(.24f, .3f), P(.3f, .9f), P(.7f, .9f), P(.76f, .3f) });
                        g.DrawLine(pen, P(.44f, .44f), P(.44f, .76f)); g.DrawLine(pen, P(.56f, .44f), P(.56f, .76f)); break;
                    case "chev":
                        g.DrawLines(pen, new[] { P(.25f, .38f), P(.5f, .63f), P(.75f, .38f) }); break;
                    case "export":
                        g.DrawLine(pen, P(.5f, .12f), P(.5f, .6f)); g.DrawLines(pen, new[] { P(.3f, .3f), P(.5f, .12f), P(.7f, .3f) });
                        g.DrawLines(pen, new[] { P(.16f, .5f), P(.16f, .88f), P(.84f, .88f), P(.84f, .5f) }); break;
                    case "folder":
                        g.DrawPolygon(pen, new[] { P(.1f, .22f), P(.4f, .22f), P(.48f, .34f), P(.9f, .34f), P(.9f, .84f), P(.1f, .84f) }); break;
                    case "list":
                        for (int i = 0; i < 3; i++) g.DrawLine(pen, P(.22f, .3f + i * .2f), P(.78f, .3f + i * .2f)); break;
                    case "clock":
                        g.DrawEllipse(pen, x + s * .14f, y + s * .14f, s * .72f, s * .72f);
                        g.DrawLines(pen, new[] { P(.5f, .3f), P(.5f, .5f), P(.64f, .6f) }); break;
                    case "excl":
                        g.DrawLine(pen, P(.5f, .2f), P(.5f, .58f));
                        using (var b = new SolidBrush(c)) g.FillEllipse(b, x + s * .5f - w * .7f, y + s * .78f - w * .7f, w * 1.4f, w * 1.4f); break;
                    case "note":
                        using (var p = RR(new RectangleF(x + s * .18f, y + s * .12f, s * .64f, s * .76f), s * .1f)) g.DrawPath(pen, p);
                        g.DrawLine(pen, P(.34f, .38f), P(.66f, .38f)); g.DrawLine(pen, P(.34f, .56f), P(.58f, .56f)); break;
                    case "help":
                        g.DrawEllipse(pen, x + s * .1f, y + s * .1f, s * .8f, s * .8f);
                        g.DrawArc(pen, x + s * .36f, y + s * .26f, s * .28f, s * .24f, 180, 250);
                        g.DrawLine(pen, P(.5f, .5f), P(.5f, .58f));
                        using (var b = new SolidBrush(c)) g.FillEllipse(b, x + s * .5f - w * .6f, y + s * .72f - w * .6f, w * 1.2f, w * 1.2f); break;
                    case "edit":
                        g.DrawLine(pen, P(.2f, .8f), P(.75f, .25f)); g.DrawLine(pen, P(.2f, .8f), P(.2f, .8f)); break;
                }
            }
            g.SmoothingMode = old;
        }
    }

    // ───────────────────────── Veri modeli ─────────────────────────
    class Note
    {
        public string id = "", asama = "genel", metin = "", tarih = "";
    }

    class Firm
    {
        public string id = "", ad = "", yetkili = "", tel = "", eposta = "", adres = "",
            durum = Flow.Aktif, ucret = "", kesifTarih = "", sozlesmeTarih = "", kontrolTarih = "",
            kusur = "", kusurTarih = "", olusturma = "", guncelleme = "";
        public List<string> kontrolGunleri = new List<string>();
        public bool kontrolDevam;
        public bool arsiv; public string arsivTarih = "", sonrakiKontrol = "";
        public int periyotAy = 12, hatirlatGun = 30;
        public bool otoKapat = true;
        public List<Dictionary<string, object>> donemler = new List<Dictionary<string, object>>();
        public Dictionary<string, string> checks = new Dictionary<string, string>();
        public List<Note> notlar = new List<Note>();
    }

    static class U
    {
        public static string Now() { return DateTimeOffset.Now.ToString("o"); }
        public static string NewId() { return Guid.NewGuid().ToString("N").Substring(0, 12); }
        public static DateTime? PD(string s)
        {
            DateTime d;
            if (!string.IsNullOrEmpty(s) && DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
            return null;
        }
        public static DateTime? PT(string s)
        {
            DateTimeOffset d;
            if (!string.IsNullOrEmpty(s) && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d.LocalDateTime;
            return null;
        }
        public static string DS(string iso) { var d = PD(iso); return d.HasValue ? d.Value.ToString("dd.MM.yyyy") : ""; }
        public static string TS(string iso) { var d = PT(iso); return d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm") : ""; }
        public static string Up(string s) { return (s ?? "").ToUpper(Th.TR); }
        public static string Low(string s) { return (s ?? "").ToLower(Th.TR); }
    }

    // ───────────────────────── İş akışı ─────────────────────────
    static class Flow
    {
        public const string Aktif = "Aktif", Iptal = "İptal / Olumsuz", Ucretli = "Ücretli", Ucretsiz = "Ücretsiz";
        public const string KusurYok = "Kusur yok", KusurVar = "Kusur var – giderilmesi bekleniyor", KusurGid = "Kusurlar giderildi",
            KusurluTam = "Kusurlar giderilmedi – hizmet kusurlu tamamlandı";
        public static readonly Color Brown = Th.H("#A2845E");
        public const int KUSUR_GUN = 30;

        public class Stage
        {
            public string Key, Title, Short; public string[] Items; public Color Color; public bool Radio;
            public Stage(string k, string t, string sh, Color c, bool radio, params string[] items)
            { Key = k; Title = t; Short = sh; Color = c; Radio = radio; Items = items; }
        }

        public static readonly Stage[] S = {
            new Stage("bulma", "Firma Bulma", "Firma Bulma", Th.Indigo, false,
                "Uygun firma bulundu", "İletişim ve gerekli bilgiler kaydedildi"),
            new Stage("kesif", "Keşif", "Keşif", Th.Blue, false,
                "Keşif / kontrol günü kaydedildi", "Keşif yapıldı", "Keşif dosyası teklif için pazarlama departmanına gönderildi"),
            new Stage("sozlesme", "Sözleşme", "Sözleşme", Th.Purple, false,
                "Teklif firma tarafından kabul edildi", "Firma yetkilisi ile İSG-KATİP sözleşmesi yapıldı"),
            new Stage("kontrol", "Kontrol", "Kontrol", Th.Orange, false,
                "Elektrik — sahaya gidilip kontrol yapıldı", "Mekanik — sahaya gidilip kontrol yapıldı"),
            new Stage("rapor", "E-İmza ve Rapor", "Rapor", Th.Teal, false,
                "Periyodik kontrol raporları hazırlandı", "Rapor e-imza ile imzalandı", "İmzalı rapor, şirkete gönderilmesi için pazarlama ekibine iletildi"),
            new Stage("kusur", "Kusur / Eksiklik", "Kusur", Th.Red, true,
                KusurYok, KusurVar, KusurGid, KusurluTam),
        };
        public const int DONE = 6;
        public static readonly string[] Topics = { "genel", "bulma", "kesif", "sozlesme", "kontrol", "rapor", "kusur" };

        public static string TopicName(string k)
        {
            if (k == "genel") return "Genel";
            foreach (var s in S) if (s.Key == k) return s.Short;
            return "Genel";
        }
        public static Color TopicColor(string k)
        {
            foreach (var s in S) if (s.Key == k) return s.Color;
            return Th.GrayDark;
        }
        // 0-5 aşamalar, 6 = uygun tamamlandı, 7 = kusurlu tamamlandı
        public static string StageName(int i) { return i == 7 ? "Kusurlu tamamlandı" : i >= DONE ? "Uygun tamamlandı" : S[i].Short; }
        public static Color StageColor(int i) { return i == 7 ? Brown : i >= DONE ? Th.Green : S[i].Color; }
        public static int Bucket(Firm f) { int c = Current(f); return c < DONE ? c : (f.kusur == KusurluTam ? 7 : DONE); }

        public static string Key(int s, int i) { return s + "-" + i; }
        public static bool Done(Firm f, int s, int i) { string v; return f.checks.TryGetValue(Key(s, i), out v) && !string.IsNullOrEmpty(v); }
        public static bool NA(Firm f, int s, int i) { string v; return f.checks.TryGetValue(Key(s, i), out v) && v == "na"; }
        public static bool StageDone(Firm f, int s)
        {
            if (S[s].Radio) return f.kusur == KusurYok || f.kusur == KusurGid || f.kusur == KusurluTam;
            for (int i = 0; i < S[s].Items.Length; i++) if (!Done(f, s, i)) return false;
            return true;
        }
        public static int StageCount(Firm f, int s)
        {
            if (S[s].Radio) return StageDone(f, s) ? 1 : 0;
            int n = 0; for (int i = 0; i < S[s].Items.Length; i++) if (Done(f, s, i)) n++;
            return n;
        }
        public static int StageTotal(int s) { return S[s].Radio ? 1 : S[s].Items.Length; }
        public static int Current(Firm f) { if (f.kusur == KusurluTam) return DONE; for (int s = 0; s < S.Length; s++) if (!StageDone(f, s)) return s; return DONE; }
        public static int Percent(Firm f)
        {
            int t = 0, d = 0;
            for (int s = 0; s < S.Length; s++) { t += StageTotal(s); d += StageCount(f, s); }
            return t == 0 ? 0 : (int)Math.Round(100.0 * d / t);
        }
        public enum Cat { Devam = 1, Kusur = 2, Tamam = 3, KusurluTamam = 4, Iptal = 5 }
        public static bool IsIptal(Firm f) { return f.durum == Iptal; }
        public static Cat Category(Firm f)
        {
            if (IsIptal(f)) return Cat.Iptal;
            if (f.kusur == KusurluTam) return Cat.KusurluTamam;
            if (Current(f) == DONE) return Cat.Tamam;
            if (f.kusur == KusurVar) return Cat.Kusur;
            return Cat.Devam;
        }
        public static List<DateTime> Days(Firm f)
        {
            return f.kontrolGunleri.Select(U.PD).Where(d => d.HasValue).Select(d => d.Value).Distinct().OrderBy(d => d).ToList();
        }
        public static void SyncKontrol(Firm f)
        {
            var d = Days(f);
            f.kontrolGunleri = d.Select(x => x.ToString("yyyy-MM-dd")).ToList();
            f.kontrolTarih = d.Count > 0 ? f.kontrolGunleri[0] : "";
        }
        public const int IDLE_GUN = 14, YIL_HATIRLAT = 30;
        public static int IdleDays(Firm f) { var t = U.PT(f.guncelleme); return t.HasValue ? (int)(DateTime.Today - t.Value.Date).TotalDays : 0; }
        public static bool Idle(Firm f) { return !f.arsiv && Category(f) == Cat.Devam && IdleDays(f) >= IDLE_GUN; }
        static DateTime? Stamp(Firm f, int s, int i)
        {
            string v; if (!f.checks.TryGetValue(Key(s, i), out v) || v == "na") return null;
            var t = U.PT(v); return t.HasValue ? t.Value.Date : (DateTime?)null;
        }
        // Kontrolün bittiği gün: geçmiş son kontrol günü, yoksa elektrik/mekanik işaret tarihi
        public static DateTime? KontrolBitti(Firm f)
        {
            if (!StageDone(f, 3)) return null;
            var past = Days(f).Where(d => d <= DateTime.Today).ToList();
            if (past.Count > 0) return past.Max();
            var st = new[] { Stamp(f, 3, 0), Stamp(f, 3, 1) }.Where(x => x.HasValue).Select(x => x.Value).ToList();
            return st.Count > 0 ? st.Max() : (DateTime?)null;
        }
        public static DateTime? AutoYearly(Firm f) { var b = KontrolBitti(f); return b.HasValue ? b.Value.AddMonths(Math.Max(1, f.periyotAy)) : (DateTime?)null; }
        public static int Hatirlat(Firm f) { return Math.Max(0, f.hatirlatGun); }
        public static string PeriyotText(int ay) { return ay % 12 == 0 ? (ay / 12) + " yıl" : ay + " ay"; }
        public static DateTime? NextYearly(Firm f)
        {
            var o = U.PD(f.sonrakiKontrol);
            if (o.HasValue && StageDone(f, 3)) return o;
            return AutoYearly(f);
        }
        public static DateTime? KesifYapildi(Firm f) { if (!Done(f, 1, 1) || NA(f, 1, 1)) return null; return U.PD(f.kesifTarih) ?? Stamp(f, 1, 1); }
        public static DateTime? SozlesmeYapildi(Firm f) { if (!Done(f, 2, 1)) return null; return U.PD(f.sozlesmeTarih) ?? Stamp(f, 2, 1); }
        public static DateTime? RaporGonderildi(Firm f) { return Stamp(f, 4, 2); }

        // Kusur süresi (30 gün) dolmuş ve giderilmemiş firmaları otomatik "kusurlu tamamlandı" yapar
        public static List<Firm> AutoClose(IEnumerable<Firm> firms)
        {
            var closed = new List<Firm>();
            foreach (var f in firms)
            {
                if (f.arsiv || IsIptal(f) || !f.otoKapat || f.kusur != KusurVar) continue;
                int? kg = KusurGun(f);
                if (!kg.HasValue || kg.Value <= KUSUR_GUN) continue;
                var start = U.PT(f.kusurTarih).Value.Date; var due = start.AddDays(KUSUR_GUN);
                f.kusur = KusurluTam;
                f.notlar.Add(new Note { id = U.NewId(), asama = "kusur", tarih = U.Now(),
                    metin = KUSUR_GUN + " gün geçti, kusurlar giderilmedi. Hizmet otomatik olarak “kusurlu tamamlandı” durumuna geçirildi. (Kusur başlangıcı: " + start.ToString("dd.MM.yyyy") + " · son gün: " + due.ToString("dd.MM.yyyy") + ")" });
                f.guncelleme = U.Now();
                closed.Add(f);
            }
            return closed;
        }

        public static int? KusurGun(Firm f)
        {
            if (f.kusur != KusurVar) return null;
            var t = U.PT(f.kusurTarih);
            return t.HasValue ? (int)(DateTime.Today - t.Value.Date).TotalDays : (int?)null;
        }
        public static string CatName(Cat c)
        {
            switch (c) { case Cat.Devam: return "Devam ediyor"; case Cat.Kusur: return "Kusur bekleniyor"; case Cat.Tamam: return "Uygun tamamlandı"; case Cat.KusurluTamam: return "Kusurlu tamamlandı"; default: return "İptal / Olumsuz"; }
        }
        public static string CatShort(Cat c)
        {
            switch (c) { case Cat.Devam: return "Devam"; case Cat.Kusur: return "Kusur"; case Cat.Tamam: return "Uygun"; case Cat.KusurluTamam: return "Kusurlu"; default: return "İptal"; }
        }
        public static Color CatColor(Cat c)
        {
            switch (c) { case Cat.Devam: return Th.Orange; case Cat.Kusur: return Th.Red; case Cat.Tamam: return Th.Green; case Cat.KusurluTamam: return Brown; default: return Th.GrayDark; }
        }
    }

    // ───────────────────────── Kayıt / yükleme ─────────────────────────
    class Store
    {
        public List<Firm> Firms = new List<Firm>();
        public string Dir, FilePath, BackupDir, Info;
        public string LastError;
        public DateTime? LastSaved;
        const int SURUM = 3;

        JavaScriptSerializer Js() { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 }; }

        public void Init()
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            Dir = Path.Combine(exeDir, "veri");
            if (!CanWrite(Dir))
                Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PeriyodikKontrolTakip", "veri");
            Directory.CreateDirectory(Dir);
            FilePath = Path.Combine(Dir, "firmalar.json");
            BackupDir = Path.Combine(Dir, "yedekler");
            Directory.CreateDirectory(BackupDir);
        }

        static bool CanWrite(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string t = Path.Combine(dir, ".test");
                File.WriteAllText(t, "ok"); File.Delete(t);
                return true;
            }
            catch { return false; }
        }

        public void Load()
        {
            Firms = new List<Firm>();
            if (!File.Exists(FilePath)) return;
            try
            {
                bool migrated;
                Firms = Parse(File.ReadAllText(FilePath, Encoding.UTF8), out migrated);
                if (migrated)
                {
                    string b = Path.Combine(BackupDir, "firmalar-v2-oncesi-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                    File.Copy(FilePath, b, true);
                    Info = "Eski sürüm verileri yeni iş akışına aktarıldı. Orijinal dosyanın kopyası: veri\\yedekler\\" + Path.GetFileName(b);
                    Save();
                }
            }
            catch (Exception ex)
            {
                string bad = Path.Combine(Dir, "firmalar.bozuk-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                try { File.Copy(FilePath, bad, true); } catch { }
                Info = "Veri dosyası okunamadı (" + ex.Message + "). Dosyanın kopyası alındı: " + Path.GetFileName(bad);
                // en yeni yedekten dene
                foreach (var f in Directory.GetFiles(BackupDir, "firmalar-????-??-??.json").OrderByDescending(x => x))
                {
                    try
                    {
                        bool m; Firms = Parse(File.ReadAllText(f, Encoding.UTF8), out m);
                        Info += "\nSon yedek yüklendi: " + Path.GetFileName(f);
                        break;
                    }
                    catch { }
                }
            }
        }

        public List<Firm> Parse(string json, out bool migrated)
        {
            var root = Js().Deserialize<Dictionary<string, object>>(json);
            int surum = 2;
            object o;
            if (root.TryGetValue("surum", out o)) { try { surum = Convert.ToInt32(o); } catch { } }
            migrated = surum < SURUM;
            var list = new List<Firm>();
            if (root.TryGetValue("firms", out o) && o is ArrayList)
                foreach (var it in (ArrayList)o)
                {
                    var d = it as Dictionary<string, object>;
                    if (d != null) list.Add(FromDict(d, migrated));
                }
            return list;
        }

        static string S(Dictionary<string, object> d, string k)
        {
            object o; return d.TryGetValue(k, out o) && o != null ? Convert.ToString(o, CultureInfo.InvariantCulture) : "";
        }

        static Firm FromDict(Dictionary<string, object> d, bool v2)
        {
            var f = new Firm();
            f.id = S(d, "id"); if (f.id == "") f.id = U.NewId();
            f.ad = S(d, "ad"); f.yetkili = S(d, "yetkili"); f.tel = S(d, "tel"); f.eposta = S(d, "eposta"); f.adres = S(d, "adres");
            string du = S(d, "durum");
            f.durum = du.Contains("ptal") || du.Contains("Olumsuz") ? Flow.Iptal : Flow.Aktif;
            f.ucret = S(d, "ucret");
            f.kesifTarih = S(d, "kesifTarih"); f.sozlesmeTarih = S(d, "sozlesmeTarih"); f.kontrolTarih = S(d, "kontrolTarih");
            f.kusur = S(d, "kusur");
            if (f.kusur != "" && f.kusur != Flow.KusurYok && f.kusur != Flow.KusurGid && f.kusur != Flow.KusurluTam && f.kusur != Flow.KusurVar)
                f.kusur = f.kusur.Contains("giderildi") ? Flow.KusurGid : (f.kusur.Contains("var") ? Flow.KusurVar : "");
            if (f.kusur == "" && S(d, "kusur").Contains("kusurlu")) f.kusur = Flow.KusurluTam;
            f.kusurTarih = S(d, "kusurTarih");
            f.olusturma = S(d, "olusturma"); f.guncelleme = S(d, "guncelleme");
            if (f.olusturma == "") f.olusturma = U.Now();
            if (f.guncelleme == "") f.guncelleme = f.olusturma;
            if (f.kusur == Flow.KusurVar && f.kusurTarih == "") f.kusurTarih = f.guncelleme;
            object kg;
            if (d.TryGetValue("kontrolGunleri", out kg) && kg is ArrayList)
                foreach (var x in (ArrayList)kg) if (x != null) f.kontrolGunleri.Add(Convert.ToString(x, CultureInfo.InvariantCulture));
            if (f.kontrolGunleri.Count == 0 && f.kontrolTarih != "") f.kontrolGunleri.Add(f.kontrolTarih);
            Flow.SyncKontrol(f);
            if (d.TryGetValue("kontrolDevam", out kg) && kg is bool) f.kontrolDevam = (bool)kg;
            if (d.TryGetValue("arsiv", out kg) && kg is bool) f.arsiv = (bool)kg;
            f.arsivTarih = S(d, "arsivTarih"); f.sonrakiKontrol = S(d, "sonrakiKontrol");
            int iv;
            if (int.TryParse(S(d, "periyotAy"), out iv) && iv > 0) f.periyotAy = iv;
            if (int.TryParse(S(d, "hatirlatGun"), out iv) && iv >= 0) f.hatirlatGun = iv;
            if (d.TryGetValue("otoKapat", out kg) && kg is bool) f.otoKapat = (bool)kg;
            if (d.TryGetValue("donemler", out kg) && kg is ArrayList)
                foreach (var x in (ArrayList)kg) { var dd = x as Dictionary<string, object>; if (dd != null) f.donemler.Add(dd); }

            var ch = new Dictionary<string, string>();
            object o;
            if (d.TryGetValue("checks", out o) && o is Dictionary<string, object>)
                foreach (var kv in (Dictionary<string, object>)o)
                    if (kv.Value != null) ch[kv.Key] = Convert.ToString(kv.Value, CultureInfo.InvariantCulture);

            if (d.TryGetValue("notlar", out o) && o is ArrayList)
                foreach (var it in (ArrayList)o)
                {
                    var nd = it as Dictionary<string, object>;
                    if (nd == null) continue;
                    var n = new Note { id = S(nd, "id"), asama = S(nd, "asama"), metin = S(nd, "metin"), tarih = S(nd, "tarih") };
                    if (n.id == "") n.id = U.NewId();
                    if (!Flow.Topics.Contains(n.asama)) n.asama = "genel";
                    if (n.metin.Trim() != "") f.notlar.Add(n);
                }

            if (!v2) { f.checks = ch; return f; }

            // ── v2 → v3 göçü ──
            f.checks = MigrateChecks(ch);
            string t = f.guncelleme;
            Action<string, string> addNote = (asama, metin) =>
            {
                if (!string.IsNullOrWhiteSpace(metin))
                    f.notlar.Add(new Note { id = U.NewId(), asama = asama, metin = metin.Trim(), tarih = t });
            };
            addNote("genel", S(d, "not"));
            if (S(d, "elektrik") != "") addNote("kesif", "Elektrik nokta sayısı: " + S(d, "elektrik"));
            if (S(d, "mekanik") != "") addNote("kesif", "Mekanik nokta sayısı: " + S(d, "mekanik"));
            if (S(d, "kontrolSaat") != "") addNote("kontrol", "Kontrol saati: " + S(d, "kontrolSaat"));
            if (S(d, "personel") != "") addNote("genel", "Personel: " + S(d, "personel"));
            if (S(d, "katipSor") != "") addNote("sozlesme", "İSG-KATİP sözleşme sorumlusu: " + S(d, "katipSor"));
            if (S(d, "imzaSor") != "") addNote("rapor", "E-imza / gönderim sorumlusu: " + S(d, "imzaSor"));
            return f;
        }

        // Eski (v2, 12 aşamalı) kontrol listesini yeni 6 aşamalı yapıya eşler.
        static Dictionary<string, string> MigrateChecks(Dictionary<string, string> o)
        {
            var n = new Dictionary<string, string>();
            Action<string, string[]> m = (nk, olds) =>
            {
                foreach (var k in olds)
                {
                    string v;
                    if (o.TryGetValue(k, out v) && !string.IsNullOrEmpty(v)) { n[nk] = v; return; }
                }
            };
            m("0-0", new[] { "0-0" });          // Uygun firma bulundu
            m("0-1", new[] { "0-1" });          // İletişim / bilgiler
            m("1-0", new[] { "1-0" });          // Keşif/kontrol günleri planlandı
            m("1-1", new[] { "3-0", "3-1" });   // Keşif dosyası oluşturuldu ⇒ keşif yapıldı
            m("1-2", new[] { "3-1" });          // Pazarlamaya gönderildi
            m("2-0", new[] { "4-0" });          // Teklif kabul
            m("2-1", new[] { "5-0" });          // İSG-KATİP sözleşmesi yapıldı
            m("3-0", new[] { "8-0" });          // Periyodik kontrol yapıldı → elektrik
            m("3-1", new[] { "8-0" });          //                          → mekanik
            m("4-0", new[] { "8-1" });          // Raporlar tamamlandı
            m("4-1", new[] { "10-0" });         // E-imza
            m("4-2", new[] { "10-1", "11-0" }); // Gönderildi
            return n;
        }

        Dictionary<string, object> ToDict(Firm f)
        {
            var d = new Dictionary<string, object>();
            d["id"] = f.id; d["ad"] = f.ad; d["yetkili"] = f.yetkili; d["tel"] = f.tel; d["eposta"] = f.eposta; d["adres"] = f.adres;
            d["durum"] = f.durum; d["ucret"] = f.ucret;
            d["kesifTarih"] = f.kesifTarih; d["sozlesmeTarih"] = f.sozlesmeTarih; d["kontrolTarih"] = f.kontrolTarih;
            d["kontrolGunleri"] = f.kontrolGunleri; d["kontrolDevam"] = f.kontrolDevam;
            d["sonrakiKontrol"] = f.sonrakiKontrol; d["periyotAy"] = f.periyotAy; d["hatirlatGun"] = f.hatirlatGun; d["otoKapat"] = f.otoKapat; d["arsiv"] = f.arsiv; d["arsivTarih"] = f.arsivTarih; d["donemler"] = f.donemler;
            d["kusur"] = f.kusur; d["kusurTarih"] = f.kusurTarih; d["olusturma"] = f.olusturma; d["guncelleme"] = f.guncelleme;
            d["checks"] = f.checks;
            d["notlar"] = f.notlar.Select(n => new Dictionary<string, object> { { "id", n.id }, { "asama", n.asama }, { "metin", n.metin }, { "tarih", n.tarih } }).ToList();
            return d;
        }

        public bool Save()
        {
            try
            {
                var root = new Dictionary<string, object>();
                root["surum"] = SURUM;
                root["firms"] = Firms.Select(ToDict).ToList();
                root["savedAt"] = DateTime.UtcNow.ToString("o");
                string json = Js().Serialize(root);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json, new UTF8Encoding(false));
                if (File.Exists(FilePath))
                {
                    try { File.Replace(tmp, FilePath, null); }
                    catch { File.Copy(tmp, FilePath, true); File.Delete(tmp); }
                }
                else File.Move(tmp, FilePath);
                DailyBackup();
                LastSaved = DateTime.Now; LastError = null;
                return true;
            }
            catch (Exception ex) { LastError = ex.Message; return false; }
        }

        void DailyBackup()
        {
            try
            {
                string b = Path.Combine(BackupDir, "firmalar-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json");
                File.Copy(FilePath, b, true);
                var all = Directory.GetFiles(BackupDir, "firmalar-????-??-??.json").OrderByDescending(x => x).ToList();
                for (int i = 30; i < all.Count; i++) File.Delete(all[i]);
            }
            catch { }
        }

        public void ExportCsv(string path)
        {
            var sb = new StringBuilder();
            Func<string, string> q = s => "\"" + (s ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
            sb.AppendLine(string.Join(";", new[] { "Firma", "Genel durum", "Durum", "Aşama", "İlerleme %", "Kontrol ücreti", "Keşif tarihi",
                "İSG-KATİP sözleşme tarihi", "Kontrol günleri", "Kusur durumu", "Yetkili", "Telefon", "E-posta", "Adres", "Notlar", "Son güncelleme" }.Select(q)));
            foreach (var f in Firms.OrderBy(x => x.ad, StringComparer.Create(Th.TR, true)))
            {
                var notes = string.Join(" | ", f.notlar.OrderBy(n => n.tarih).Select(n => "[" + Flow.TopicName(n.asama) + "] " + n.metin));
                sb.AppendLine(string.Join(";", new[] { f.ad, f.durum, Flow.CatName(Flow.Category(f)), Flow.StageName(Flow.Bucket(f)),
                    Flow.Percent(f).ToString(), f.ucret, U.DS(f.kesifTarih), U.DS(f.sozlesmeTarih), string.Join(", ", f.kontrolGunleri.Select(U.DS)) + (f.kontrolDevam ? " (devam edecek)" : ""), f.kusur,
                    f.yetkili, f.tel, f.eposta, f.adres, notes, U.TS(f.guncelleme) }.Select(q)));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }
    }
}

namespace PKT
{
    // Aylık özet hesapları (rapor ekranı ve Excel için)
    class MonthStats
    {
        public int Yil, Ay, YeniFirma, Kesif, Sozlesme, KontrolFirma, KontrolGunu, Rapor;
        public double? OrtKesifSozlesme; public int OrtN;
        public string Label { get { return new DateTime(Yil, Ay, 1).ToString("MMMM yyyy", Th.TR); } }
        public string Short { get { return new DateTime(Yil, Ay, 1).ToString("MMM yy", Th.TR); } }

        public static MonthStats Of(IEnumerable<Firm> firms, int y, int m)
        {
            var r = new MonthStats { Yil = y, Ay = m };
            Func<DateTime?, bool> inM = d => d.HasValue && d.Value.Year == y && d.Value.Month == m;
            var gaps = new List<double>();
            foreach (var f in firms)
            {
                if (inM(U.PT(f.olusturma))) r.YeniFirma++;
                if (inM(Flow.KesifYapildi(f))) r.Kesif++;
                var so = Flow.SozlesmeYapildi(f);
                if (inM(so))
                {
                    r.Sozlesme++;
                    var ke = Flow.KesifYapildi(f);
                    if (ke.HasValue && so.Value >= ke.Value) gaps.Add((so.Value - ke.Value).TotalDays);
                }
                var days = Flow.Days(f).Where(d => d <= DateTime.Today && d.Year == y && d.Month == m).ToList();
                if (days.Count > 0) { r.KontrolFirma++; r.KontrolGunu += days.Count; }
                if (inM(Flow.RaporGonderildi(f))) r.Rapor++;
            }
            if (gaps.Count > 0) { r.OrtKesifSozlesme = gaps.Average(); r.OrtN = gaps.Count; }
            return r;
        }
        public static double? OverallKesifSozlesme(IEnumerable<Firm> firms, out int n)
        {
            var gaps = new List<double>();
            foreach (var f in firms)
            {
                var so = Flow.SozlesmeYapildi(f); var ke = Flow.KesifYapildi(f);
                if (so.HasValue && ke.HasValue && so.Value >= ke.Value) gaps.Add((so.Value - ke.Value).TotalDays);
            }
            n = gaps.Count;
            return gaps.Count > 0 ? gaps.Average() : (double?)null;
        }
    }
}
