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
        DacService.SnapshotDatabase(cfg.DestinationDb, cfg.BaselinePath);

        AnsiConsole.MarkupLine("[bold]Step 2/2 — Writing schema files...[/]");

        var totalFiles = 0;

        foreach (var (folder, schema) in DacService.SchemaObjectTypes)
        {
            var scripts = DacService.ExtractSchemaScripts(cfg.BaselinePath, schema);
            var dir     = Path.Combine(cfg.SchemaPath, folder);

            Directory.CreateDirectory(dir);

            foreach (var existing in Directory.GetFiles(dir, "*.sql"))
                File.Delete(existing);

            foreach (var (name, sql) in scripts)
                File.WriteAllText(Path.Combine(dir, $"{name}.sql"), sql);

            totalFiles += scripts.Count;
            AnsiConsole.MarkupLine($"[grey]  {folder}: {scripts.Count} files[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]✓ Baseline saved   → {cfg.BaselinePath}[/]");
        AnsiConsole.MarkupLine($"[green]✓ Schema files     → {cfg.SchemaPath}/ ({totalFiles} total)[/]");
        AnsiConsole.MarkupLine("[grey]Commit db/schema/ to git — git diff will show changes.[/]");
    }
}
