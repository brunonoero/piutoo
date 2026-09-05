using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using Piootoo.Shared;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy;

namespace Piootoo.Core.Services;

/// <summary>
/// Costruisce la scheda completa di una strategia — <see cref="StrategyExport"/> — mettendo in un
/// file solo le tre cose che servono a verificare una conversione: i <b>numeri</b> con cui è stata
/// tradotta, i <b>commenti</b> che spiegano perché, e la <b>sorgente</b> da cui viene.
///
/// <para><b>Da dove viene ogni pezzo.</b> I parametri sono letti per riflessione dall'istanza
/// appena costruita: sono quindi ciò che il costruttore imposta davvero, non ciò che un commento
/// dichiara. Il sorgente C# della classe e del motore è incorporato nell'assembly delle strategie
/// (vedi <c>Piootoo.Strategies.csproj</c>), quindi descrive il codice in esecuzione anche su un
/// server senza checkout. Il motore Python e la scheda del dossier vengono invece dal repository
/// dati e sono marcati come tali: possono essere stati rigenerati dopo la traduzione.</para>
///
/// <para><b>Niente silenzi.</b> Ogni pezzo mancante finisce in <see cref="StrategyExport.Warnings"/>
/// con il motivo. Un export che tace su ciò che non ha trovato si legge come un export completo, e
/// chi lo riceve non ha modo di accorgersene.</para>
/// </summary>
public sealed class StrategyExportService
{
    private readonly PiootooSettings _settings;

    public StrategyExportService(PiootooSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Il dossier del paniere corrente, relativo alla radice del repository dati. È il file da cui
    /// si estrae la scheda di ricerca da cui la strategia è stata tradotta.
    ///
    /// <para>⚠ Va spostato quando esce una nuova edizione del dossier, insieme a
    /// <c>docs/domini/mappa-strategie-pts.md</c>, <c>tools/dossier-extract.py</c> e
    /// <c>tools/dossier-diff.py</c>, che puntano allo stesso file — <c>StrategyExportTests</c>
    /// fallisce se questo resta indietro. Se il file non c'è, l'export lo dichiara in
    /// <c>warnings</c> invece di fallire: la scheda è un allegato, non la strategia.</para>
    /// </summary>
    public const string DossierRelativePath = @"run-engine\run-08-settembre\DOSSIER_PANIERE (1).md";

    /// <summary>Cartella dei motori di ricerca Python, relativa alla radice del repository dati.</summary>
    public const string PythonEnginesRelativeDirectory = "easy_engine_py";

    /// <summary>
    /// La corrispondenza fra il motore C# e il motore Python da cui è stato portato, con la sigla
    /// che il report di sweep e il dossier usano per lo stesso motore.
    ///
    /// <para>È scritta a mano perché la corrispondenza è una <b>scelta di traduzione</b>, non un
    /// fatto deducibile dai nomi: <c>SessionBreakoutEngine</c> è <c>BO</c> e non "SBO", e i due
    /// motori TF stanno in un file C# solo mentre in Python sono due. Le voci con motore Python
    /// <c>null</c> sono motori nati qui, senza controparte di ricerca.</para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, EngineOrigin> EngineOrigins =
        new Dictionary<string, EngineOrigin>(StringComparer.Ordinal)
        {
            ["TfMirroredEngine"] = new("TF_M", "tf_mirrored.py"),
            ["TfUnmirroredEngine"] = new("TF_U", "tf_unmirrored.py"),
            ["BiasBarCountEngine"] = new("BIAS", "bias.py"),
            ["BiasWeeklyEngine"] = new("BIASW", "bias_weekly.py"),
            ["RbbMirroredEngine"] = new("RBB_M", "reversal_bb_mirrored.py"),
            ["RbbUnmirroredEngine"] = new("RBB_U", "reversal_bb_unmirrored.py"),
            ["SessionBreakoutEngine"] = new("BO", "breakout.py"),
            ["LevelFaderEngine"] = new("LF", "level_fader.py"),
            ["PriceChannelEngine"] = new("PC", "price_channel.py"),
            ["VolatilityBreakoutEngine"] = new("VBO", "volatility_breakout.py"),
            ["RhlEngine"] = new("RHL", "reversal_hl.py"),
            ["MovingAverageCrossoverEngine"] = new("MAC", "ma_crossover.py"),
            // Motori senza controparte nel motore di ricerca Python.
            ["AroonCrossoverEngine"] = new(null, null),
            ["TrendDeveloperEngine"] = new(null, null)
        };

    /// <summary>Sigla del motore e file Python corrispondente; <c>null</c> quando non esiste.</summary>
    private sealed record EngineOrigin(string? Code, string? PythonFile);

    /// <summary>
    /// Membri che non sono taratura e non vanno fra i parametri: l'identità della strategia, che sta
    /// già in <see cref="StrategyExport.Identity"/>, e gli orologi derivati. Ripeterli sposterebbe
    /// l'attenzione dai numeri che contano.
    /// </summary>
    private static readonly HashSet<string> NonParameterMembers = new(StringComparer.Ordinal)
    {
        nameof(ITradingStrategy.Name),
        nameof(ITradingStrategy.Description),
        nameof(ITradingStrategy.Symbol),
        nameof(ITradingStrategy.TimeframeMinutes),
        // Non sono taratura ma orologi memoizzati, derivati dal fuso di Session e TradingWindow che
        // l'export gia' porta. Elencati qui e non lasciati allo scarto per tipo perche' altrimenti
        // ogni export uscirebbe con lo stesso warning, e un avviso che c'e' sempre non si legge piu'.
        "Clock",
        "WindowClock"
    };

    /// <summary>
    /// Costruisce la scheda della strategia identificata dal suo <b>Id di classe</b> o dal suo
    /// codice di esecuzione — <see cref="StrategyFactory.CreateStrategy"/> accetta entrambi.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Se l'identificativo non corrisponde a nessuna strategia.</exception>
    public StrategyExport Build(string strategyId)
    {
        if (string.IsNullOrWhiteSpace(strategyId))
        {
            throw new KeyNotFoundException("Nessuna strategia richiesta: l'identificativo è vuoto.");
        }

        var instance = StrategyFactory.CreateStrategy(strategyId.Trim(), string.Empty, 0)
            ?? throw new KeyNotFoundException(
                $"Strategia '{strategyId}' inesistente: {StrategyFactory.DescribeUnusableId(strategyId.Trim())}.");

        var type = instance.GetType();
        var warnings = new List<string>();

        var export = new StrategyExport
        {
            GeneratedAtUtc = DateTime.UtcNow,
            PiootooVersion = PiootooVersion.Current,
            Identity = BuildIdentity(instance, type),
            Instrument = BuildInstrument(instance.Symbol, warnings),
            Parameters = ReadParameters(instance, type, warnings),
            Warnings = warnings
        };

        var strategySource = ReadEmbeddedSource(type.Name, warnings, "la classe della strategia");
        if (strategySource != null)
        {
            export.Sources.Add(new StrategyExportDocument
            {
                Role = "strategy",
                Language = "csharp",
                Name = $"{type.Name}.cs",
                FromAssembly = true,
                Text = strategySource
            });
        }

        AppendConversion(export, type, strategySource, warnings);
        return export;
    }

    private static StrategyExportIdentity BuildIdentity(ITradingStrategy instance, Type type)
    {
        var holding = ReadHolding(instance);
        return new StrategyExportIdentity
        {
            Id = type.Name,
            ExecutionCode = instance.Name,
            Symbol = instance.Symbol,
            TimeframeMinutes = instance.TimeframeMinutes,
            BarType = DescribeBarType(instance.TimeframeMinutes),
            Description = instance.Description,
            ClassFullName = type.FullName ?? type.Name,
            RequiredCandles = instance.RequiredCandles,
            Overnight = holding.Overnight,
            Overweek = holding.Overweek,
            HoldingLabel = holding.Describe()
        };
    }

    /// <summary>
    /// La tenuta dichiarata dalla strategia. Il motore la espone come proprietà pubblica; le
    /// strategie che non derivano da un motore non ce l'hanno, e per quelle vale il default del
    /// catalogo (multiday), lo stesso che usa <see cref="StrategyDefinition"/>.
    /// </summary>
    private static StrategyHolding ReadHolding(ITradingStrategy instance)
    {
        var property = instance.GetType().GetProperty("Holding", BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(instance) as StrategyHolding is { } holding
            ? holding.Normalized()
            : StrategyHolding.Multiday;
    }

    private static string DescribeBarType(int timeframeMinutes) => timeframeMinutes switch
    {
        1 => "OneMinute",
        5 => "FiveMinute",
        15 => "FifteenMinute",
        30 => "ThirtyMinute",
        60 => "OneHour",
        240 => "FourHour",
        1440 => "Daily",
        10080 => "Weekly",
        _ => $"{timeframeMinutes}Minutes"
    };

    private static StrategyExportInstrument? BuildInstrument(string symbol, List<string> warnings)
    {
        if (!InstrumentRegistry.TryGet(symbol, out var spec))
        {
            warnings.Add(
                $"Il simbolo '{symbol}' non è in InstrumentRegistry: i parametri in denaro non sono " +
                "convertibili in punti senza il valore del contratto.");
            return null;
        }

        return new StrategyExportInstrument
        {
            Symbol = spec.Symbol,
            PointValue = spec.PointValue,
            Currency = spec.Currency,
            TickSize = spec.TickSize,
            SessionTimeZone = spec.SessionTimeZone,
            Description = spec.Description
        };
    }

    // ------------------------------------------------------------------ parametri

    /// <summary>
    /// Legge i parametri del motore dall'istanza, risalendo la catena di ereditarietà dalla classe
    /// della strategia fino alla base comune.
    ///
    /// <para><b>Perché per riflessione e non da una lista dichiarata.</b> I parametri sono campi
    /// <c>protected</c> impostati nel costruttore: una lista scritta a mano andrebbe tenuta
    /// allineata a quattordici motori e divergerebbe al primo parametro nuovo, esattamente nel caso
    /// in cui l'export serve. Qui un parametro nuovo compare da solo.</para>
    ///
    /// <para>Si tiene solo ciò che è visibile alle sottoclassi (<c>protected</c>, <c>internal</c>,
    /// <c>public</c>): i campi <c>private</c> di un motore sono stato di calcolo — cache di ADX,
    /// orologi memoizzati — e non taratura. Le proprietà si leggono solo se il tipo è
    /// rappresentabile in JSON come valore: così un <c>SessionClock</c> non viene invocato e non
    /// può far fallire l'export di una strategia su un simbolo senza specifica.</para>
    /// </summary>
    private static Dictionary<string, StrategyExportParameter> ReadParameters(
        ITradingStrategy instance,
        Type type,
        List<string> warnings)
    {
        var parameters = new Dictionary<string, StrategyExportParameter>(StringComparer.Ordinal);
        var skippedTypes = new SortedSet<string>(StringComparer.Ordinal);

        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            const BindingFlags declared =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (var field in current.GetFields(declared))
            {
                if (field.IsPrivate || field.IsSpecialName || field.Name.Contains('<'))
                {
                    continue; // stato interno del motore, o backing field di una proprietà già letta
                }

                TryAdd(field.Name, field.FieldType, () => field.GetValue(instance), current);
            }

            foreach (var property in current.GetProperties(declared))
            {
                var getter = property.GetMethod;
                if (getter is null || getter.IsPrivate || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                TryAdd(property.Name, property.PropertyType, () => property.GetValue(instance), current);
            }
        }

        if (skippedTypes.Count > 0)
        {
            warnings.Add(
                "Membri non inclusi fra i parametri perché di un tipo non rappresentabile come valore: " +
                string.Join(", ", skippedTypes) + ".");
        }

        return parameters
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        void TryAdd(string name, Type memberType, Func<object?> read, Type declaringType)
        {
            if (NonParameterMembers.Contains(name) || parameters.ContainsKey(name))
            {
                // Il primo che vince è il più derivato: se una classe ombreggia un membro del
                // motore, il valore che conta è il suo.
                return;
            }

            if (!IsExportableValue(memberType))
            {
                skippedTypes.Add($"{declaringType.Name}.{name} ({FriendlyTypeName(memberType)})");
                return;
            }

            object? value;
            try
            {
                value = read();
            }
            catch (Exception ex)
            {
                skippedTypes.Add($"{declaringType.Name}.{name} (lettura fallita: {ex.GetType().Name})");
                return;
            }

            parameters[name] = new StrategyExportParameter
            {
                Value = value,
                DeclaredIn = declaringType.Name,
                Type = FriendlyTypeName(memberType)
            };
        }
    }

    /// <summary>
    /// Tipi che finiscono in JSON come un valore leggibile. La lista è chiusa di proposito: un tipo
    /// nuovo viene segnalato in <c>warnings</c> invece di essere serializzato a caso — un oggetto
    /// con dentro un grafo di servizi renderebbe l'export illeggibile e potrebbe non serializzarsi
    /// affatto.
    /// </summary>
    private static bool IsExportableValue(Type type)
    {
        var effective = Nullable.GetUnderlyingType(type) ?? type;
        return effective.IsPrimitive
            || effective.IsEnum
            || effective == typeof(string)
            || effective == typeof(decimal)
            || effective == typeof(DateTime)
            || effective == typeof(TimeSpan)
            || effective == typeof(ZonedWindow)
            || effective == typeof(StrategyHolding);
    }

    private static string FriendlyTypeName(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        return underlying != null ? $"{underlying.Name}?" : type.Name;
    }

    // ------------------------------------------------------------------ provenienza e sorgenti

    /// <summary>
    /// Aggiunge motore, scheda di dossier e motore Python. Il motore si prende dalla catena di
    /// ereditarietà — la classe che la strategia estende <b>è</b> il motore — e l'S-ID dal
    /// paragrafo "Codice sorgente" del sorgente C#, che è l'unico posto in cui la classe dichiara
    /// da quale riga di ricerca viene.
    /// </summary>
    private void AppendConversion(
        StrategyExport export,
        Type type,
        string? strategySource,
        List<string> warnings)
    {
        var engineType = ResolveEngineType(type);
        if (engineType is null)
        {
            warnings.Add(
                $"'{type.Name}' non deriva da un motore noto: non c'è un motore Python di riferimento.");
            return;
        }

        export.Conversion.EngineClass = engineType.Name;

        var origin = EngineOrigins[engineType.Name];
        export.Conversion.EngineCode = origin.Code;

        var engineSource = ReadEmbeddedSource(engineType.Name, warnings, "il motore C#");
        if (engineSource != null)
        {
            export.Sources.Add(new StrategyExportDocument
            {
                Role = "engine",
                Language = "csharp",
                Name = $"{engineType.Name} (sorgente incorporato)",
                FromAssembly = true,
                Text = engineSource
            });
        }

        AppendPythonEngine(export, origin, warnings);
        AppendDossier(export, origin.Code, strategySource, warnings);
    }

    /// <summary>
    /// Il motore da cui la strategia deriva: il primo antenato presente in
    /// <see cref="EngineOrigins"/>. Non è sempre la base diretta — i due motori TF passano da
    /// <c>TfEngineBase</c> — quindi si risale invece di guardare solo <c>BaseType</c>.
    /// </summary>
    private static Type? ResolveEngineType(Type type)
    {
        for (var current = type.BaseType; current != null && current != typeof(object); current = current.BaseType)
        {
            if (EngineOrigins.ContainsKey(current.Name))
            {
                return current;
            }
        }

        return null;
    }

    private void AppendPythonEngine(StrategyExport export, EngineOrigin origin, List<string> warnings)
    {
        if (origin.PythonFile is null)
        {
            warnings.Add(
                $"Il motore '{export.Conversion.EngineClass}' non nasce da un motore di ricerca Python: " +
                "non c'è un file da allegare.");
            return;
        }

        var relative = Path.Combine(PythonEnginesRelativeDirectory, origin.PythonFile);
        export.Conversion.PythonEngineFile = relative;

        var full = ResolveRepositoryFile(relative);
        if (full is null || !File.Exists(full))
        {
            warnings.Add($"Motore Python non trovato: '{relative}' non esiste sotto il repository dati.");
            return;
        }

        export.Sources.Add(new StrategyExportDocument
        {
            Role = "engine-python",
            Language = "python",
            Name = relative,
            FromAssembly = false,
            Text = File.ReadAllText(full)
        });
    }

    /// <summary>
    /// Allega la scheda del dossier che corrisponde alla strategia.
    ///
    /// <para><b>L'aggancio passa dall'impronta numerica, non dall'S-ID.</b> Gli S-ID del dossier
    /// sono ordinati per atteso/trade e scorrono a ogni rigenerazione: quello scritto nel sorgente
    /// al momento della traduzione punta quasi sempre a un'altra riga nell'edizione corrente —
    /// <c>PTS_NQ_TFM_002_15</c> cita <c>S21</c>, che oggi e' un'altra strategia. Allegare la scheda
    /// sbagliata sarebbe peggio che non allegarne nessuna, quindi si usa la stessa chiave stabile di
    /// <c>tools/dossier-diff.py</c>: simbolo, timeframe, motore, stop e target in denaro.</para>
    ///
    /// <para>Quattro impronte del dossier corrente sono condivise da due schede — righe di ricerca
    /// diverse con la stessa taratura. Trailing e uscita a tempo ne separano una; per le altre
    /// l'export allega <b>tutte</b> le candidate e lo dichiara, invece di sceglierne una a caso.</para>
    /// </summary>
    private void AppendDossier(
        StrategyExport export,
        string? engineCode,
        string? strategySource,
        List<string> warnings)
    {
        if (strategySource is not null &&
            Regex.Match(strategySource, @"Codice sorgente:?\s*(S\d+)") is { Success: true } declared)
        {
            export.Conversion.DeclaredDossierId = declared.Groups[1].Value;
        }

        if (engineCode is null)
        {
            return; // motore senza controparte di ricerca: il warning c'e' gia'
        }

        export.Conversion.DossierFile = DossierRelativePath;

        var full = ResolveRepositoryFile(DossierRelativePath);
        if (full is null || !File.Exists(full))
        {
            warnings.Add($"Dossier del paniere non trovato: '{DossierRelativePath}' non esiste sotto il repository dati.");
            return;
        }

        var impronta = BuildFingerprint(export, engineCode);
        var candidate = MatchDossierCards(ReadDossierCards(full), impronta);

        if (candidate.Count == 0)
        {
            warnings.Add(
                $"Nessuna scheda del dossier ha l'impronta di questa strategia ({impronta}). Puo' venire " +
                "da un'edizione precedente del paniere, oppure la taratura e' stata cambiata dopo la " +
                "traduzione.");
            return;
        }

        if (candidate.Count > 1)
        {
            warnings.Add(
                $"L'impronta ({impronta}) corrisponde a {candidate.Count} schede del dossier " +
                $"({string.Join(", ", candidate.Select(card => card.Id))}): sono righe di ricerca diverse " +
                "con la stessa taratura, e i numeri non bastano a distinguerle. Allegate tutte.");
        }
        else
        {
            export.Conversion.DossierId = candidate[0].Id;
        }

        foreach (var card in candidate)
        {
            export.Sources.Add(new StrategyExportDocument
            {
                Role = "dossier",
                Language = "markdown",
                Name = $"{DossierRelativePath} · {card.Id}",
                FromAssembly = false,
                Text = card.Text
            });
        }

        if (export.Conversion.DeclaredDossierId is { } dichiarato &&
            export.Conversion.DossierId is { } risolto &&
            !string.Equals(dichiarato, risolto, StringComparison.Ordinal))
        {
            warnings.Add(
                $"Il sorgente dichiara '{dichiarato}' ma nel dossier allegato questa strategia e' " +
                $"'{risolto}'. È il caso normale: gli S-ID scorrono fra le edizioni, e vale quello " +
                "risolto per impronta.");
        }
    }

    /// <summary>
    /// L'impronta di un candidato di ricerca: gli stessi cinque numeri che usa
    /// <c>tools/dossier-diff.py</c>. Trailing e uscita a tempo restano fuori perche' capita che la
    /// taratura del port diverga da quella del report, e includerli farebbe sparire l'abbinamento
    /// invece di segnalare la differenza; entrano solo come spareggio fra schede identiche.
    /// </summary>
    private static DossierFingerprint BuildFingerprint(StrategyExport export, string engineCode) =>
        new(
            export.Identity.Symbol.TrimStart('@').ToUpperInvariant(),
            export.Identity.TimeframeMinutes,
            engineCode,
            ReadMoney(export, "StopMoney", "StopMoneyLong"),
            ReadMoney(export, "ProfitMoney", "ProfitMoneyLong"),
            ReadMoney(export, "TrailingStopMoney", "TrailingMoneyLong"),
            ReadMoney(export, "MaxBars", "MaxBarsInPosition"));

    /// <summary>
    /// Un parametro in denaro dell'export, con il nome alternativo dei motori che dichiarano stop e
    /// target per lato (BIASW). Il valore del lato long e' quello che il dossier riporta.
    /// </summary>
    private static int ReadMoney(StrategyExport export, string name, string alternative)
    {
        foreach (var candidate in new[] { name, alternative })
        {
            if (export.Parameters.TryGetValue(candidate, out var parameter) && parameter.Value is { } value)
            {
                var numero = Convert.ToDecimal(value);
                if (numero != 0m)
                {
                    return (int)numero;
                }
            }
        }

        return 0;
    }

    /// <summary>I numeri che identificano una riga di run fra un'edizione del dossier e l'altra.</summary>
    private sealed record DossierFingerprint(
        string Symbol,
        int TimeframeMinutes,
        string EngineCode,
        int StopMoney,
        int ProfitMoney,
        int TrailingMoney,
        int MaxBars)
    {
        /// <summary>La chiave di abbinamento: i cinque numeri, senza trailing ne' uscita a tempo.</summary>
        public bool SameRow(DossierFingerprint other) =>
            Symbol == other.Symbol
            && TimeframeMinutes == other.TimeframeMinutes
            && EngineCode == other.EngineCode
            && StopMoney == other.StopMoney
            && ProfitMoney == other.ProfitMoney;

        public override string ToString() =>
            $"{Symbol} {TimeframeMinutes}m {EngineCode} stop ${StopMoney} target ${ProfitMoney}";
    }

    /// <summary>Una scheda del dossier: la sua impronta e il testo integrale.</summary>
    private sealed record DossierCard(string Id, DossierFingerprint Fingerprint, string Text);

    /// <summary>
    /// Le schede del dossier che corrispondono all'impronta. Con piu' di una si tenta lo spareggio
    /// su trailing e uscita a tempo; se nemmeno quelli separano, si restituiscono tutte.
    /// </summary>
    private static IReadOnlyList<DossierCard> MatchDossierCards(
        IReadOnlyList<DossierCard> dossier,
        DossierFingerprint impronta)
    {
        var candidate = dossier
            .Where(card => card.Fingerprint.SameRow(impronta))
            .ToList();

        if (candidate.Count <= 1)
        {
            return candidate;
        }

        var esatte = candidate
            .Where(card => card.Fingerprint.TrailingMoney == impronta.TrailingMoney
                        && card.Fingerprint.MaxBars == impronta.MaxBars)
            .ToList();

        return esatte.Count == 1 ? esatte : candidate;
    }

    /// <summary>
    /// Le schede del dossier del paniere. Il formato e' quello che legge <c>tools/dossier-diff.py</c>:
    /// una sezione <c>### SNN · SIMBOLO tf · titolo</c> per scheda, con motore e uscite in tabella e
    /// in elenco. Le sezioni introduttive del dossier non hanno quell'intestazione e cadono da sole.
    /// </summary>
    /// <summary>
    /// Le schede del dossier, lette una volta sola per edizione del file.
    ///
    /// <para>Serve all'export della griglia intera: il dossier è 228 KB e senza cache verrebbe
    /// riletto e ripassato a regex una volta per strategia — 124 volte per un export completo, per
    /// ottenere ogni volta lo stesso risultato. La chiave include l'istante di ultima scrittura, così
    /// una nuova edizione del file entra senza riavviare il server.</para>
    /// </summary>
    private static IReadOnlyList<DossierCard> ReadDossierCards(string path)
    {
        var stamp = File.GetLastWriteTimeUtc(path);
        lock (DossierCacheGate)
        {
            if (_dossierCache is { } cached && cached.Path == path && cached.Stamp == stamp)
            {
                return cached.Cards;
            }

            var cards = ParseDossier(File.ReadAllText(path));
            _dossierCache = new DossierCacheEntry(path, stamp, cards);
            return cards;
        }
    }

    private sealed record DossierCacheEntry(string Path, DateTime Stamp, IReadOnlyList<DossierCard> Cards);

    private static readonly object DossierCacheGate = new();
    private static DossierCacheEntry? _dossierCache;

    private static IReadOnlyList<DossierCard> ParseDossier(string dossier)
    {
        var cards = new List<DossierCard>();
        foreach (Match block in Regex.Matches(
                     dossier, @"^###\s+.*?(?=^###\s|\z)", RegexOptions.Multiline | RegexOptions.Singleline))
        {
            var text = block.Value.TrimEnd();
            var head = Regex.Match(text, @"^###\s+(S\d+)\s+·\s+([A-Z0-9]+)\s+(\S+)\s+·");
            if (!head.Success || TimeframeFromDossier(head.Groups[3].Value) is not { } timeframe)
            {
                continue;
            }

            var engine = Regex.Match(text, @"\|\s*Motore\s*\|\s*(\S+)\s*\|");
            cards.Add(new DossierCard(
                head.Groups[1].Value,
                new DossierFingerprint(
                    head.Groups[2].Value,
                    timeframe,
                    NormalizeEngineCode(engine.Success ? engine.Groups[1].Value : string.Empty),
                    ReadDossierMoney(text, @"Stop loss:\s*\*\*\$([\d,]+)\*\*"),
                    ReadDossierMoney(text, @"Take profit:\s*\*\*\$([\d,]+)\*\*"),
                    ReadDossierMoney(text, @"Trailing stop:\s*\*\*\$([\d,]+)\*\*"),
                    ReadDossierMoney(text, @"Uscita a tempo dopo\s*\*\*(\d+) barre\*\*")),
                text));
        }

        return cards;
    }

    /// <summary>
    /// Il dossier scrive il timeframe come etichetta, non in minuti. <c>day</c> e' 1440: la giornata
    /// del calendario europeo su cui gira la ricerca, non la sessione del broker.
    /// </summary>
    private static int? TimeframeFromDossier(string label) => label switch
    {
        "15m" => 15,
        "30m" => 30,
        "1h" => 60,
        "4h" => 240,
        "day" => 1440,
        _ => null
    };

    /// <summary>
    /// La sigla del motore come la scrive il dossier. L'unica differenza nota e' il volatility
    /// breakout, <c>VB</c> nel dossier e <c>VBO</c> in catalogo: la stessa trappola che aveva
    /// sbagliato il primo abbinamento della mappa PTS.
    /// </summary>
    private static string NormalizeEngineCode(string code) =>
        code.Trim() == "VB" ? "VBO" : code.Trim();

    private static int ReadDossierMoney(string text, string pattern)
    {
        var match = Regex.Match(text, pattern);
        return match.Success && int.TryParse(match.Groups[1].Value.Replace(",", string.Empty), out var value)
            ? value
            : 0;
    }

    /// <summary>
    /// Risolve un percorso relativo alla radice del repository dati. <c>BasePath</c> è la cartella
    /// <c>piootoo-repository</c>: dossier e motori Python stanno accanto ai datafeed, non dentro.
    /// </summary>
    private string? ResolveRepositoryFile(string relativePath)
    {
        var basePath = _settings.BasePath;
        return string.IsNullOrWhiteSpace(basePath) ? null : Path.Combine(basePath, relativePath);
    }

    // ------------------------------------------------------------------ sorgenti incorporati

    private static readonly Assembly StrategiesAssembly = typeof(EasyLib).Assembly;
    private static readonly ConcurrentDictionary<string, string?> SourceByTypeName = new(StringComparer.Ordinal);

    /// <summary>
    /// Il sorgente C# in cui è dichiarato un tipo, letto dalle risorse incorporate nell'assembly
    /// delle strategie.
    ///
    /// <para>Il nome della risorsa segue il percorso del file, quindi per una classe che sta nel
    /// file omonimo basta il suffisso. Per le altre — i due motori TF stanno in <c>TfEngines.cs</c>,
    /// i due RBB in <c>ReversalBollingerBandEngines.cs</c> — si cerca la dichiarazione nel testo,
    /// invece di tenere una mappa classe→file che il primo accorpamento renderebbe falsa.</para>
    /// </summary>
    private static string? ReadEmbeddedSource(string typeName, List<string> warnings, string cosa)
    {
        var source = SourceByTypeName.GetOrAdd(typeName, FindEmbeddedSource);
        if (source is null)
        {
            warnings.Add(
                $"Sorgente non incorporato per {cosa} '{typeName}': l'export non porta i commenti di " +
                "conversione. Verifica gli EmbeddedResource di Piootoo.Strategies.csproj.");
        }

        return source;
    }

    private static string? FindEmbeddedSource(string typeName)
    {
        var names = StrategiesAssembly.GetManifestResourceNames();

        var exact = names.FirstOrDefault(name =>
            name.EndsWith($".{typeName}.cs", StringComparison.Ordinal));
        if (exact != null)
        {
            return ReadResource(exact);
        }

        var declaration = new Regex($@"\bclass\s+{Regex.Escape(typeName)}\b");
        foreach (var name in names.Where(name => name.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var text = ReadResource(name);
            if (text != null && declaration.IsMatch(text))
            {
                return text;
            }
        }

        return null;
    }

    private static string? ReadResource(string resourceName)
    {
        using var stream = StrategiesAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
