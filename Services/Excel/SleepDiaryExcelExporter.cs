using System.Globalization;
using ClosedXML.Excel;
using HomeMedications.Formatting;
using HomeMedications.Models;

namespace HomeMedications.Services.Excel;

/// <summary>Экспорт дневника сна в .xlsx: один лист на календарный год, оформление как в эталонной таблице.</summary>
public sealed class SleepDiaryExcelExporter
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#2D5A43");
    private static readonly XLColor HeaderFont = XLColor.White;
    private static readonly XLColor RowAlt = XLColor.FromHtml("#F3F4F6");
    private static readonly XLColor YesNoNo = XLColor.FromHtml("#C6EFCE");
    private static readonly XLColor YesNoYes = XLColor.FromHtml("#FFC7CE");
    private static readonly XLColor FatigueStrong = XLColor.FromHtml("#375623");
    private static readonly XLColor FatigueMedium = XLColor.FromHtml("#FFEB9C");
    private static readonly XLColor FatigueWeak = XLColor.FromHtml("#FFFFCC");
    private static readonly XLColor FatigueNone = XLColor.FromHtml("#d8f3dc");
    private static readonly XLColor SleepOk = XLColor.FromHtml("#C6EFCE");
    private static readonly XLColor SleepWarn = XLColor.FromHtml("#FFEB9C");
    private static readonly XLColor SleepBad = XLColor.FromHtml("#FFC7CE");
    private static readonly XLColor SleepShort = XLColor.FromHtml("#E4D4F4");

    private static readonly string[] Headers =
    [
        "Дата записи",
        "Время засыпания\nвчера",
        "Время окончательного\nпробуждения",
        "Количество пробуждений\nпосреди сна",
        "Время пробуждений",
        "Попыток уснуть",
        "Еда перед сном",
        "Усталость перед сном",
        "Лежание после\nпробуждения",
        "Волнение/мысли\nперед сном",
        "Удовлетворение сном",
        "Удовлетворение сном\nспустя пол дня",
        "Время сна",
        "Время бодрствования",
        "Комментарии"
    ];

    public byte[] BuildWorkbook(IReadOnlyList<SleepEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var byDate = entries
            .GroupBy(e => e.EntryDate)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

        SleepEntry? ResolveNext(SleepEntry e) =>
            byDate.TryGetValue(e.EntryDate.AddDays(1), out var n) ? n : null;

        using var workbook = new XLWorkbook();
        var groups = entries
            .GroupBy(e => e.EntryDate.Year)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in groups)
        {
            var ordered = group
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.Id)
                .ToList();

            var sheetName = group.Key.ToString(CultureInfo.InvariantCulture);
            var worksheet = workbook.Worksheets.Add(sheetName);
            WriteSheet(worksheet, ordered, ResolveNext);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSheet(IXLWorksheet ws, IReadOnlyList<SleepEntry> rows, Func<SleepEntry, SleepEntry?> nextOf)
    {
        for (var c = 0; c < Headers.Length; c++)
            ws.Cell(1, c + 1).Value = Headers[c];

        StyleHeaderRow(ws, 1, Headers.Length);
        ws.Row(1).Height = 48;

        for (var i = 0; i < rows.Count; i++)
        {
            var r = i + 2;
            var e = rows[i];
            var next = nextOf(e);
            var sleep = SleepEntryMetrics.ComputeSleepDuration(e);
            var wake = SleepEntryMetrics.ComputeWakeDuration(e, next);
            WriteRowValues(ws, r, e, sleep, wake);
            if (i % 2 == 1)
                ApplyAlternatingNeutralColumns(ws, r);
            ApplyRowPresentation(ws, r, e, sleep);
        }

        ws.Range(1, 1, Math.Max(1, rows.Count + 1), Headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(1, 1, Math.Max(1, rows.Count + 1), Headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        if (rows.Count > 0)
            ws.Range(1, 1, rows.Count + 1, Headers.Length).SetAutoFilter();

        ws.SheetView.FreezeRows(1);
        SetColumnWidths(ws);
    }

    private static void WriteRowValues(IXLWorksheet ws, int row, SleepEntry e, TimeSpan? sleepComputed, TimeSpan? wakeComputed)
    {
        ws.Cell(row, 1).Value = SleepEntryFormatting.Date(e.EntryDate);
        ws.Cell(row, 2).Value = SleepEntryFormatting.Time(e.FallAsleepTimeYesterday);
        ws.Cell(row, 3).Value = SleepEntryFormatting.Time(e.FinalWakeTime);
        ws.Cell(row, 4).Value = e.NightAwakeningsCount;
        ws.Cell(row, 5).Value = SleepEntryFormatting.AwakeningTimesExcel(e.AwakeningTimes);
        ws.Cell(row, 6).Value = e.FallAsleepAttempts;
        ws.Cell(row, 7).Value = SleepEntryFormatting.YesNo(e.FoodBeforeSleep);
        ws.Cell(row, 8).Value = SleepEntryFormatting.Fatigue(e.FatigueBeforeSleep);
        ws.Cell(row, 9).Value = SleepEntryFormatting.YesNo(e.LyingAfterWake);
        ws.Cell(row, 10).Value = SleepEntryFormatting.YesNo(e.AnxietyBeforeSleep);
        ws.Cell(row, 11).Value = SleepEntryFormatting.YesNo(e.SleepSatisfaction);
        ws.Cell(row, 12).Value = SleepEntryFormatting.YesNo(e.SleepSatisfactionHalfDay);
        ws.Cell(row, 13).Value = SleepEntryFormatting.Duration(sleepComputed);
        ws.Cell(row, 14).Value = SleepEntryFormatting.Duration(wakeComputed);
        ws.Cell(row, 15).Value = e.Comments ?? string.Empty;

        ws.Range(row, 1, row, Headers.Length).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        ws.Range(row, 1, row, Headers.Length).Style.Alignment.WrapText = true;
    }

    private static void ApplyRowPresentation(IXLWorksheet ws, int row, SleepEntry e, TimeSpan? sleepComputed)
    {
        StyleYesNoFood(ws.Cell(row, 7), e.FoodBeforeSleep);
        StyleYesNoFood(ws.Cell(row, 9), e.LyingAfterWake);
        StyleYesNoAnxiety(ws.Cell(row, 10), e.AnxietyBeforeSleep);
        StyleSatisfactionPrimary(ws.Cell(row, 11), e.SleepSatisfaction);
        StyleSatisfactionHalfDay(ws.Cell(row, 12), e.SleepSatisfactionHalfDay);
        StyleFatigue(ws.Cell(row, 8), e.FatigueBeforeSleep);
        StyleSleepDuration(ws.Cell(row, 13), sleepComputed);
    }

    private static void StyleHeaderRow(IXLWorksheet ws, int row, int lastCol)
    {
        var header = ws.Range(row, 1, row, lastCol);
        header.Style.Fill.BackgroundColor = HeaderFill;
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = HeaderFont;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Alignment.WrapText = true;
    }

    private static void ApplyAlternatingNeutralColumns(IXLWorksheet ws, int row)
    {
        ReadOnlySpan<int> cols = stackalloc int[] { 1, 2, 3, 4, 5, 6, 15 };
        foreach (var c in cols)
            ws.Cell(row, c).Style.Fill.BackgroundColor = RowAlt;
    }

    private static void StyleYesNoFood(IXLCell cell, bool? value)
    {
        ResetCellColors(cell);
        switch (value)
        {
            case false:
                cell.Style.Fill.BackgroundColor = YesNoNo;
                break;
            case true:
                cell.Style.Fill.BackgroundColor = YesNoYes;
                break;
        }
    }

    private static void StyleYesNoAnxiety(IXLCell cell, bool? value) => StyleYesNoFood(cell, value);

    private static void StyleSatisfactionPrimary(IXLCell cell, bool? value)
    {
        ResetCellColors(cell);
        switch (value)
        {
            case true:
                cell.Style.Fill.BackgroundColor = YesNoNo;
                break;
            case false:
                cell.Style.Fill.BackgroundColor = FatigueMedium;
                break;
        }
    }

    private static void StyleSatisfactionHalfDay(IXLCell cell, bool? value)
    {
        ResetCellColors(cell);
        switch (value)
        {
            case false:
                cell.Style.Fill.BackgroundColor = YesNoYes;
                break;
            case true:
                cell.Style.Fill.BackgroundColor = YesNoNo;
                break;
        }
    }

    private static void StyleFatigue(IXLCell cell, FatigueBeforeSleepLevel? level)
    {
        ResetCellColors(cell);
        cell.Style.Font.FontColor = XLColor.Black;
        switch (level)
        {
            case FatigueBeforeSleepLevel.None:
                cell.Style.Fill.BackgroundColor = FatigueNone;
                break;
            case FatigueBeforeSleepLevel.Strong:
                cell.Style.Fill.BackgroundColor = FatigueStrong;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
                break;
            case FatigueBeforeSleepLevel.Medium:
                cell.Style.Fill.BackgroundColor = FatigueMedium;
                break;
            case FatigueBeforeSleepLevel.Weak:
                cell.Style.Fill.BackgroundColor = FatigueWeak;
                break;
        }
    }

    private static void StyleSleepDuration(IXLCell cell, TimeSpan? duration)
    {
        ResetCellColors(cell);
        if (duration is null)
            return;

        var hours = duration.Value.TotalHours;
        if (hours >= 9)
            cell.Style.Fill.BackgroundColor = SleepOk;
        else if (hours >= 8)
            cell.Style.Fill.BackgroundColor = SleepWarn;
        else if (hours >= 7)
            cell.Style.Fill.BackgroundColor = SleepBad;
        else
            cell.Style.Fill.BackgroundColor = SleepShort;
    }

    private static void ResetCellColors(IXLCell cell)
    {
        cell.Style.Fill.BackgroundColor = XLColor.NoColor;
        cell.Style.Font.FontColor = XLColor.Black;
        cell.Style.Font.Bold = false;
    }

    private static void SetColumnWidths(IXLWorksheet ws)
    {
        ws.Column(1).Width = 9.5;
        ws.Column(2).Width = 10;
        ws.Column(3).Width = 11;
        ws.Column(4).Width = 9;
        ws.Column(5).Width = 11;
        ws.Column(6).Width = 8;
        ws.Column(7).Width = 9;
        ws.Column(8).Width = 11;
        ws.Column(9).Width = 10;
        ws.Column(10).Width = 10;
        ws.Column(11).Width = 10;
        ws.Column(12).Width = 12;
        ws.Column(13).Width = 8.5;
        ws.Column(14).Width = 10;
        ws.Column(15).Width = 18;
    }
}
