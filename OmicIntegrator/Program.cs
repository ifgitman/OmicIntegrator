using Microsoft.EntityFrameworkCore;
using OmicIntegrator;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;
using System.Text.Json;

var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");

if (File.Exists(settingsPath))
{
    var settsFile = File.ReadAllText(settingsPath);
    Settings.Current = JsonSerializer.Deserialize<Settings>(settsFile);
}

if (String.IsNullOrEmpty(Settings.Current?.DatabaseFile)
    || !File.Exists(Settings.Current.DatabaseFile))
{
    if (Settings.Current == null)
        Settings.Current = new();

    Settings.Current.DatabaseFile = ConsoleInput.AskFileName("Enter database file path:", false);

    File.WriteAllText(settingsPath, JsonSerializer.Serialize(Settings.Current));
}

var ctx = new BaseCtx();
await ctx.Database.MigrateAsync();
Console.WriteLine("Database schema update complete");

await Menu.Program();
