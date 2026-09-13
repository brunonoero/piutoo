using Piootoo.Core.Services.Compare;

// ============================================================================================
// Confronto fra un run cBot cTrader e un backtest interno sullo stesso feed di broker.
//
// Uso:  dotnet run -c Release -- <cartella compare> [cartella output] [broker del feed]
//
// La logica sta in Piootoo.Core/Services/Compare/CompareRunner.cs, la stessa che usa il server
// per i confronti avviati dalla console. Qui restano solo gli argomenti e il codice di uscita.
// ============================================================================================

if (args.Length == 0 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Uso: strumento-confronto <cartella compare> [cartella output] [broker del feed]");
    return 2;
}

try
{
    var outDir = CompareRunner.Run(
        args[0],
        args.Length > 1 ? args[1] : null,
        args.Length > 2 ? args[2] : null,
        Console.Out);
    Console.WriteLine("fatto: " + outDir);
    return 0;
}
catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}