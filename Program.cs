using HomeMedications.Components;
using HomeMedications.Services;
using HomeMedications.Services.Excel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<MedicationStore>();
builder.Services.AddSingleton<SleepDiaryStore>();
builder.Services.AddSingleton<SleepDiaryExcelExporter>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/api/sleep-diary/export", (SleepDiaryStore store, SleepDiaryExcelExporter exporter) =>
{
    var all = store.GetAll();
    if (all.Count == 0)
        return Results.NotFound("Нет записей для экспорта.");

    var bytes = exporter.BuildWorkbook(all);
    var fileName = $"sleep-diary-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
    return Results.File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
