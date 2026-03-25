using Database.Services;
using Spectre.Console;

namespace Database.Commands;

public static class ResyncCommand
{
    public static void Run(AppConfig cfg)
    {
        AnsiConsole.MarkupLine("[bold]── Resync: snapshotting destination baseline ──[/]");
        AnsiConsole.MarkupLine($"[grey]Destination: {cfg.MaskConn(cfg.DestinationDb)}[/]");

        AnsiConsole.MarkupLine("[bold]Step 1/2 — Extracting schema...[/]");
        DacpacService.SnapshotDatabase(cfg.DestinationDb, cfg.BaselinePath);

        AnsiConsole.MarkupLine("[bold]Step 2/2 — Writing table schema files...[/]");
        var tables = DacpacService.ExtractTableScripts(cfg.BaselinePath);

        Directory.CreateDirectory(cfg.SchemaPath);

        foreach (var existing in Directory.GetFiles(cfg.SchemaPath, "*.sql"))
            File.Delete(existing);

        foreach (var (name, sql) in tables)
            File.WriteAllText(Path.Combine(cfg.SchemaPath, $"{name}.sql"), sql);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]✓ Baseline saved   → {cfg.BaselinePath}[/]");
        AnsiConsole.MarkupLine($"[green]✓ Schema files     → {cfg.SchemaPath}/ ({tables.Count} tables)[/]");
        AnsiConsole.MarkupLine("[grey]Commit db/schema/ to git — git diff will show table changes.[/]");
    }
}
