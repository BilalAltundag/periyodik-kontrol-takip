using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace PKT
{
    // Harici kütüphane gerektirmeyen basit, biçimli .xlsx yazıcı
    class XlsxSheet
    {
        public string Name;
        public List<string> Headers = new List<string>();
        public List<double> Widths = new List<double>();
        public List<object[]> Rows = new List<object[]>();
    }

    static class Xlsx
    {
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        static string E(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char c in s)
                if (c == '\t' || c == '\n' || c == '\r' || c >= 0x20) sb.Append(c);
            return SecurityElement.Escape(sb.ToString());
        }
        static string Col(int i) { string s = ""; i++; while (i > 0) { int m = (i - 1) % 26; s = (char)('A' + m) + s; i = (i - 1) / 26; } return s; }

        public static void Write(string path, List<XlsxSheet> sheets)
        {
            if (File.Exists(path)) File.Delete(path);
            using (var fs = new FileStream(path, FileMode.CreateNew))
            using (var z = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                Add(z, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                    string.Concat(sheets.Select((s, i) => "<Override PartName=\"/xl/worksheets/sheet" + (i + 1) + ".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>")) +
                    "</Types>");
                Add(z, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                Add(z, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>" +
                    string.Concat(sheets.Select((s, i) => "<sheet name=\"" + E(s.Name) + "\" sheetId=\"" + (i + 1) + "\" r:id=\"rId" + (i + 1) + "\"/>")) +
                    "</sheets><definedNames>" +
                    string.Concat(sheets.Select((s, i) => "<definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"" + i + "\" hidden=\"1\">'" + E(s.Name) + "'!$A$1:$" + Col(Math.Max(0, s.Headers.Count - 1)) + "$" + (s.Rows.Count + 1) + "</definedName>")) +
                    "</definedNames></workbook>");
                Add(z, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    string.Concat(sheets.Select((s, i) => "<Relationship Id=\"rId" + (i + 1) + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet" + (i + 1) + ".xml\"/>")) +
                    "<Relationship Id=\"rId" + (sheets.Count + 1) + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                // stiller: 0 normal, 1 başlık, 2 tarih, 3 kaydırmalı metin, 4 sayı
                Add(z, "xl/styles.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                    "<numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"dd.mm.yyyy\"/></numFmts>" +
                    "<fonts count=\"2\"><font><sz val=\"10\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"10\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/></font></fonts>" +
                    "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF007AFF\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
                    "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border><border><left/><right/><top/><bottom style=\"thin\"><color rgb=\"FFE5E5EA\"/></bottom><diagonal/></border></borders>" +
                    "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                    "<cellXfs count=\"5\">" +
                    "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"><alignment vertical=\"top\"/></xf>" +
                    "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
                    "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"><alignment horizontal=\"center\" vertical=\"top\"/></xf>" +
                    "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>" +
                    "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"><alignment horizontal=\"center\" vertical=\"top\"/></xf>" +
                    "</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");
                for (int i = 0; i < sheets.Count; i++) Add(z, "xl/worksheets/sheet" + (i + 1) + ".xml", Sheet(sheets[i]));
            }
        }

        static string Sheet(XlsxSheet s)
        {
            var sb = new StringBuilder();
            int nc = s.Headers.Count;
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/><cols>");
            for (int c = 0; c < nc; c++)
                sb.Append("<col min=\"" + (c + 1) + "\" max=\"" + (c + 1) + "\" width=\"" + (c < s.Widths.Count ? s.Widths[c] : 14).ToString(CultureInfo.InvariantCulture) + "\" customWidth=\"1\"/>");
            sb.Append("</cols><sheetData>");
            sb.Append("<row r=\"1\" ht=\"30\" customHeight=\"1\">");
            for (int c = 0; c < nc; c++) sb.Append("<c r=\"" + Col(c) + "1\" s=\"1\" t=\"inlineStr\"><is><t>" + E(s.Headers[c]) + "</t></is></c>");
            sb.Append("</row>");
            for (int r = 0; r < s.Rows.Count; r++)
            {
                int rn = r + 2;
                sb.Append("<row r=\"" + rn + "\">");
                var row = s.Rows[r];
                for (int c = 0; c < row.Length && c < nc; c++)
                {
                    object v = row[c]; string rf = Col(c) + rn;
                    if (v == null) continue;
                    if (v is DateTime)
                        sb.Append("<c r=\"" + rf + "\" s=\"2\"><v>" + ((DateTime)v - new DateTime(1899, 12, 30)).TotalDays.ToString(CultureInfo.InvariantCulture) + "</v></c>");
                    else if (v is int || v is double)
                        sb.Append("<c r=\"" + rf + "\" s=\"4\"><v>" + Convert.ToDouble(v).ToString(CultureInfo.InvariantCulture) + "</v></c>");
                    else
                    {
                        string t = v.ToString();
                        if (t == "") continue;
                        sb.Append("<c r=\"" + rf + "\" s=\"3\" t=\"inlineStr\"><is><t xml:space=\"preserve\">" + E(t) + "</t></is></c>");
                    }
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData>");
            if (nc > 0) sb.Append("<autoFilter ref=\"A1:" + Col(nc - 1) + (s.Rows.Count + 1) + "\"/>");
            sb.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.6\" bottom=\"0.6\" header=\"0.3\" footer=\"0.3\"/><pageSetup orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"0\"/></worksheet>");
            return sb.ToString();
        }

        static void Add(ZipArchive z, string name, string content)
        {
            var e = z.CreateEntry(name, CompressionLevel.Optimal);
            using (var w = new StreamWriter(e.Open(), Utf8)) w.Write(content);
        }

        // ── Uygulama verisinden çalışma kitabı ──
        public static void Export(string path, List<Firm> all)
        {
            var cmp = StringComparer.Create(Th.TR, true);
            var fs = new XlsxSheet { Name = "Firmalar" };
            fs.Headers.AddRange(new[] { "Firma", "Genel durum", "Durum", "Aşama", "İlerleme %", "Kontrol ücreti", "Keşif tarihi", "İSG-KATİP sözleşme tarihi",
                "Kontrol günleri", "Kontrol devam ediyor", "Sonraki periyodik kontrol", "Kusur durumu", "Kusur süresi başlangıcı", "Yetkili", "Telefon", "E-posta", "Adres",
                "Son güncelleme", "İşlem yapılmayan gün", "Arşiv" });
            fs.Widths.AddRange(new double[] { 30, 13, 18, 18, 10, 12, 12, 14, 24, 11, 14, 30, 13, 20, 16, 24, 28, 13, 11, 9 });
            Func<DateTime?, object> D = d => d.HasValue ? (object)d.Value.Date : null;
            foreach (var f in all.OrderBy(x => x.arsiv).ThenBy(x => x.ad, cmp))
            {
                var cat = Flow.Category(f);
                fs.Rows.Add(new object[] { f.ad, f.durum, Flow.CatName(cat), Flow.StageName(Flow.Bucket(f)), Flow.Percent(f), f.ucret,
                    f.kesifYok ? (object)"Keşif yok (kalemler iletildi)" : D(U.PD(f.kesifTarih)), D(U.PD(f.sozlesmeTarih)), string.Join(", ", Flow.Days(f).Select(d => d.ToString("dd.MM.yyyy"))),
                    f.kontrolDevam ? "Evet" : "", D(Flow.NextYearly(f)), f.kusur, f.kusur == Flow.KusurVar ? D(U.PT(f.kusurTarih)) : null,
                    f.yetkili, f.tel, f.eposta, f.adres, D(U.PT(f.guncelleme)), Flow.IdleDays(f), f.arsiv ? "Arşivde" : "" });
            }
            var ns = new XlsxSheet { Name = "Notlar" };
            ns.Headers.AddRange(new[] { "Firma", "Konu", "Tarih", "Not" });
            ns.Widths.AddRange(new double[] { 30, 14, 12, 90 });
            foreach (var f in all.OrderBy(x => x.ad, cmp))
                foreach (var n in f.notlar.OrderByDescending(n => n.tarih, StringComparer.Ordinal))
                    ns.Rows.Add(new object[] { f.ad, Flow.TopicName(n.asama), D(U.PT(n.tarih)), n.metin });

            var ys = new XlsxSheet { Name = "Periyodik Kontrol" };
            ys.Headers.AddRange(new[] { "Firma", "Kontrolün bittiği gün", "Sonraki periyodik kontrol", "Kalan gün", "Periyot", "Hatırlatma (gün önce)", "Yetkili", "Telefon" });
            ys.Widths.AddRange(new double[] { 30, 14, 14, 10, 10, 12, 20, 16 });
            foreach (var f in all.Where(x => !x.arsiv && !Flow.IsIptal(x) && Flow.NextYearly(x).HasValue).OrderBy(x => Flow.NextYearly(x)))
            {
                var nx = Flow.NextYearly(f).Value;
                ys.Rows.Add(new object[] { f.ad, D(Flow.KontrolBitti(f)), nx, (int)(nx - DateTime.Today).TotalDays, Flow.PeriyotText(f.periyotAy), Flow.Hatirlat(f), f.yetkili, f.tel });
            }

            var ms = new XlsxSheet { Name = "Aylık Özet" };
            ms.Headers.AddRange(new[] { "Ay", "Yeni firma", "Keşif yapıldı", "Sözleşme yapıldı", "Kontrol yapılan firma", "Kontrol günü", "Rapor gönderildi", "Ort. keşif→sözleşme (gün)" });
            ms.Widths.AddRange(new double[] { 16, 11, 11, 13, 14, 11, 13, 16 });
            var t = DateTime.Today;
            for (int i = 0; i < 12; i++)
            {
                var m = new DateTime(t.Year, t.Month, 1).AddMonths(-i);
                var st = MonthStats.Of(all, m.Year, m.Month);
                ms.Rows.Add(new object[] { st.Label, st.YeniFirma, st.Kesif, st.Sozlesme, st.KontrolFirma, st.KontrolGunu, st.Rapor,
                    st.OrtKesifSozlesme.HasValue ? (object)Math.Round(st.OrtKesifSozlesme.Value, 1) : null });
            }
            Write(path, new List<XlsxSheet> { fs, ns, ys, ms });
        }
    }
}
