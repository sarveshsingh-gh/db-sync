using Database.Services;
using Spectre.Console;

namespace Database.Commands;

public static class MigrateCommand
{
    public static void Run(AppConfig cfg)
    {
        AnsiConsole.MarkupLine("[bold]── Migrate (resync + compare) ──[/]");
        AnsiConsole.WriteLine();
        ResyncCommand.Run(cfg);
        AnsiConsole.WriteLine();
        CompareCommand.Run(cfg);
    }
}
