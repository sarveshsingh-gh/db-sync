using Database.Services;
using Spectre.Console;

namespace Database.Commands;

public static class CompareCommand
{
    public static void Run(AppConfig cfg)
    {
        AnsiConsole.MarkupLine("[bold]── Compare: source vs destination baseline ──[/]");

        if (!File.Exists(cfg.BaselinePath))
        {
            AnsiConsole.MarkupLine("[red]No baseline found. Run --resync first.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold]Step 1/3 — Snapshotting source...[/]");
        AnsiConsole.MarkupLine($"[grey]Source: {cfg.MaskConn(cfg.SourceDb)}[/]");

        var sourceDacpac = Path.Combine(Path.GetTempPath(), $"source_{DateTime.UtcNow:yyyyMMddHHmmss}.dacpac");
        DacService.SnapshotDatabase(cfg.SourceDb, sourceDacpac);

        AnsiConsole.MarkupLine("[bold]Step 2/3 — Generating UP and DOWN scripts...[/]");

        // UP   : bring destination (prod) up to source (UAT) schema
        // DOWN : bring destination back to baseline if rollback needed
        var upScript   = DacService.GenerateMigrationUpScript(sourceDacpac, cfg.DestinationDb);
        var downScript = DacService.GenerateMigrationDownScript(cfg.BaselinePath, cfg.SourceDb);

        File.Delete(sourceDacpac);

        if (!DacService.ScriptHasChanges(upScript))
        {
            AnsiConsole.MarkupLine("[green]✓ No differences found. Destination is already in sync with source.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold]Step 3/3 — Writing migration files...[/]");

        var timestamp   = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var migrDir     = cfg.MigrationsPath;
        var deployedDir = Path.Combine(migrDir, "Deployed");

        Directory.CreateDirectory(migrDir);
        Directory.CreateDirectory(deployedDir);

        var gitkeep = Path.Combine(deployedDir, ".gitkeep");
        if (!File.Exists(gitkeep)) File.WriteAllText(gitkeep, "");

        var upPath   = Path.Combine(migrDir, $"{timestamp}_up.sql");
        var downPath = Path.Combine(migrDir, $"{timestamp}_down.sql");

        File.WriteAllText(upPath,   upScript);
        File.WriteAllText(downPath, downScript);

        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("File")
            .AddColumn("Path");

        table.AddRow("[green]UP[/]",    upPath);
        table.AddRow("[yellow]DOWN[/]", downPath);
        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Next steps:[/]");
        AnsiConsole.MarkupLine("[grey]  1. Review Migrations UP and DOWN scripts[/]");
        AnsiConsole.MarkupLine("[grey]  2. Copy both to your project's migrations folder[/]");
        AnsiConsole.MarkupLine("[grey]  3. After deploying, move both files into Deployed/[/]");
    }
}
