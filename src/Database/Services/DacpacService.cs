using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Spectre.Console;

namespace Database.Services;

public static class DacpacService
{
    public static void SnapshotDatabase(string connectionString, string dacpacOutputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dacpacOutputPath))!);

        var dacServices = new DacServices(connectionString);
        dacServices.Message += PrintProgress;

        dacServices.Extract(dacpacOutputPath, GetDatabaseName(connectionString), "Database", new Version(1, 0));
    }

    public static Dictionary<string, string> ExtractTableScripts(string dacpacPath)
    {
        var tableScripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var model = TSqlModel.LoadFromDacpac(dacpacPath, new ModelLoadOptions
        {
            LoadAsScriptBackedModel = false,
        });

        foreach (var table in model.GetObjects(DacQueryScopes.UserDefined, ModelSchema.Table))
        {
            var fullName = table.Name.ToString();
            var fileName = fullName.Replace("[", "").Replace("]", "").Replace(".", "_");

            table.TryGetScript(out var createScript);

            tableScripts[fileName] = createScript ?? string.Empty;
        }

        return tableScripts;
    }

    public static string GenerateMigrationUpScript(string sourceDacpac, string destinationConnectionString)
    {
        var sourceSchema = DacPackage.Load(sourceDacpac);
        var dacServices  = new DacServices(destinationConnectionString);

        return dacServices.GenerateDeployScript(sourceSchema, GetDatabaseName(destinationConnectionString), DeployOptions());
    }

    public static string GenerateMigrationDownScript(string baselineDacpac, string sourceConnectionString)
    {
        var baselineSchema = DacPackage.Load(baselineDacpac);
        var dacServices    = new DacServices(sourceConnectionString);

        return dacServices.GenerateDeployScript(baselineSchema, GetDatabaseName(sourceConnectionString), DeployOptions());
    }

    public static bool ScriptHasChanges(string script) =>
        script.Split('\n')
              .Select(line => line.Trim())
              .Where(line => !line.StartsWith("--") && !string.IsNullOrWhiteSpace(line))
              .Any(line => line.StartsWith("ALTER",  StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("DROP",   StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("EXEC",   StringComparison.OrdinalIgnoreCase));

    private static void PrintProgress(object? sender, DacMessageEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Message.Message))
            AnsiConsole.MarkupLine($"[grey]  {e.Message.Message}[/]");
    }

    private static DacDeployOptions DeployOptions() => new()
    {
        BlockOnPossibleDataLoss = false,
        DropObjectsNotInSource  = false,
        IgnorePermissions       = true,
        IgnoreRoleMembership    = true,
        GenerateSmartDefaults   = true,
        ExcludeObjectTypes      =
        [
            ObjectType.StoredProcedures,
            ObjectType.Views,
            ObjectType.ScalarValuedFunctions,
            ObjectType.TableValuedFunctions,
            ObjectType.DatabaseTriggers,
            ObjectType.ServerTriggers,
            ObjectType.Synonyms,
            ObjectType.UserDefinedDataTypes,
            ObjectType.UserDefinedTableTypes,
            ObjectType.ClrUserDefinedTypes,
        ],
    };

    private static string GetDatabaseName(string connectionString) =>
        new SqlConnectionStringBuilder(connectionString).InitialCatalog;
}
