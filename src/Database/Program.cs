using Database.Commands;
using Database.Services;
using Spectre.Console;

AnsiConsole.Write(new FigletText("Database Sync").Color(Color.SteelBlue1));
AnsiConsole.MarkupLine("[grey]SQL Server schema sync & migration generator[/]");
AnsiConsole.WriteLine();

try
{
    var cfg = AppConfig.Load();

    AnsiConsole.MarkupLine($"[grey]Source      : {cfg.MaskConn(cfg.SourceDb)}[/]");
    AnsiConsole.MarkupLine($"[grey]Destination : {cfg.MaskConn(cfg.DestinationDb)}[/]");
    AnsiConsole.WriteLine();

    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What do you want to do?")
            .AddChoices(
                "Resync   — Generates baseline schema files",
                "Compare  — Generates migration scripts against base schema",
                "Migrate  — resync then compare in one step",
                "Exit"));

    AnsiConsole.WriteLine();

    switch (choice.Split(' ')[0].ToLowerInvariant())
    {
        case "resync":
            ResyncCommand.Run(cfg);
            break;

        case "compare":
            CompareCommand.Run(cfg);
            break;

        case "migrate":
            MigrateCommand.Run(cfg);
            break;

        case "exit":
            return 0;
    }
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
    return 1;
}

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
Console.ReadKey(intercept: true);
return 0;
