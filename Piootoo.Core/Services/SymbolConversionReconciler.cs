using System.Globalization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Brokers;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>
/// Mette la tabella di conversione delle size accanto a cio' che il broker dichiara, e dice dove
/// non tornano.
///
/// <para><b>La regola.</b> <c>ContractMultiplier = valore punto del future / valore punto di un
/// lotto del broker</c>, entrambi nella valuta di quotazione. Per un CFD il valore di un punto per
/// un lotto e' la dimensione del lotto in unita' di sottostante (<c>Symbol.LotSize</c>): il
/// controvalore e' <c>unita' x prezzo</c>, e la sua derivata rispetto al prezzo e' il numero di
/// unita'. Su USTEC, lotto da 1 unita' contro le 20 $/punto di <c>@NQ</c>, fa 20.</para>
///
/// <para><b>Perche' il calcolo sta qui e non nel bot.</b> Il valore punto del <i>future</i> e' una
/// cosa che sa Piootoo, non il broker: sta in <see cref="InstrumentRegistry"/>. Un bot che lo
/// calcolasse dovrebbe portarsi dietro una copia del registro, e due copie di una tabella di
/// costanti divergono sempre — di solito il giorno in cui si aggiunge uno strumento.</para>
///
/// <para><b>Non corregge.</b> Vedi <see cref="SymbolConversionReconciliationDto"/>: quei numeri
/// decidono quanti contratti vanno a mercato.</para>
/// </summary>
public sealed class SymbolConversionReconciler
{
    /// <summary>
    /// Scarto relativo oltre il quale due moltiplicatori sono <b>diversi</b>. Non si confronta
    /// l'uguaglianza esatta: i valori misurati passano da <c>double</c> dell'API del broker e da
    /// una serializzazione a stringa, e una differenza nella dodicesima cifra non e' una notizia.
    /// Un millesimo invece lo e': su un moltiplicatore da 20 sono 0,02 contratti ogni mille.
    /// </summary>
    private const decimal Tolerance = 0.001m;

    private readonly SymbolInfoStore _symbolInfo;
    private readonly WorkspaceService _workspaces;

    public SymbolConversionReconciler(SymbolInfoStore symbolInfo, WorkspaceService workspaces)
    {
        _symbolInfo = symbolInfo;
        _workspaces = workspaces;
    }

    public SymbolConversionReconciliationDto Reconcile(string broker, string conversionCode) =>
        Reconcile(broker, _workspaces.GetSymbolConversion(conversionCode), _symbolInfo.GetArchives(broker));

    /// <summary>
    /// Il confronto vero, su dati gia' in mano. Separato dalla lettura perche' e' una funzione pura
    /// — due tabelle, nessun disco — e perche' e' la parte che vale la pena provare: la lettura la
    /// provano gia' i test del magazzino.
    /// </summary>
    public static SymbolConversionReconciliationDto Reconcile(
        string broker,
        SymbolConversion conversion,
        IReadOnlyList<SymbolInfoArchiveDto> archives)
    {
        var byBrokerSymbol = new Dictionary<string, SymbolInfoArchiveDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var archive in archives)
            byBrokerSymbol[archive.BrokerSymbol] = archive;

        var report = new SymbolConversionReconciliationDto
        {
            Broker = broker,
            ConversionCode = conversion.Code,
            ConversionName = conversion.Name
        };

        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in conversion.Mappings)
        {
            mapped.Add(mapping.AccountSymbol);
            var row = Compare(mapping, byBrokerSymbol.GetValueOrDefault(mapping.AccountSymbol));
            report.Rows.Add(row);

            if (row.Unverifiable)
                report.Unverifiable++;
            else if (row.Findings.Count > 0)
                report.Divergent++;
            else
                report.Confirmed++;
        }

        foreach (var archive in archives)
        {
            if (!mapped.Contains(archive.BrokerSymbol))
                report.ArchivedButNotMapped.Add(archive.BrokerSymbol);
        }

        return report;
    }

    private static SymbolConversionReconciliationRowDto Compare(
        AccountSymbolMapping mapping,
        SymbolInfoArchiveDto? archive)
    {
        var row = new SymbolConversionReconciliationRowDto
        {
            Symbol = mapping.Symbol,
            AccountSymbol = mapping.AccountSymbol,
            TableMultiplier = mapping.ContractMultiplier,
            TableMinimumQuantity = mapping.MinimumQuantity,
            TableQuantityStep = mapping.QuantityStep
        };

        // L'ultimo scatto e non uno a scelta: la tabella descrive come si opera ADESSO, quindi si
        // confronta con le specifiche di adesso. Gli scatti vecchi servono ai backtest, non qui.
        var snapshot = archive?.Snapshots.Count > 0 ? archive.Snapshots[^1] : null;
        if (snapshot is null)
        {
            row.Unverifiable = true;
            row.Findings.Add(
                $"nessuna rilevazione per '{mapping.AccountSymbol}' su questo broker: " +
                "lancia il bot con Lavori = SoloSpecifiche.");
            return row;
        }

        row.MeasuredAtUtc = snapshot.TakenUtc;

        var lotSize = Number(snapshot, "LotSize");
        row.BrokerLotSize = lotSize;

        if (!InstrumentRegistry.TryGet(mapping.Symbol, out var spec))
        {
            row.Unverifiable = true;
            row.Findings.Add(
                $"il registro strumenti non conosce '{mapping.Symbol}': senza il valore punto del " +
                "contratto di riferimento il moltiplicatore non e' calcolabile.");
            return row;
        }

        row.FuturePointValue = spec.PointValue;

        // PriceScale diverso da 1 significa che il broker quota il sottostante in un'ALTRA unita'
        // di prezzo (un indice in centesimi contro i punti interi del future). La regola sopra
        // presuppone la stessa unita', quindi qui non si pronuncia invece di dare un numero che
        // sembra una verifica.
        if (mapping.PriceScale != 1m && mapping.PriceScale > 0m)
        {
            row.Unverifiable = true;
            row.Findings.Add(
                $"la riga dichiara PriceScale {mapping.PriceScale}: il broker quota in un'altra " +
                "unita' di prezzo e il moltiplicatore non si verifica da solo.");
            return row;
        }

        if (lotSize is null or <= 0m)
        {
            row.Unverifiable = true;
            row.Findings.Add(
                "la rilevazione non porta un LotSize utilizzabile: senza, il valore di un punto per " +
                "un lotto e' ignoto.");
            return row;
        }

        var measured = spec.PointValue / lotSize.Value;
        row.MeasuredMultiplier = measured;

        if (Diverges(mapping.ContractMultiplier, measured))
        {
            row.Findings.Add(
                $"moltiplicatore: la tabella usa {Format(mapping.ContractMultiplier)}, la rilevazione " +
                $"dice {Format(measured)} ({Format(spec.PointValue)} {spec.Currency} al punto / lotto " +
                $"da {Format(lotSize.Value)}).");
        }

        // Volumi: l'API li da' in UNITA' di sottostante, la tabella in LOTTI. Confrontarli senza
        // dividere per il lotto darebbe divergenze inventate su ogni riga.
        var minimumUnits = Number(snapshot, "VolumeInUnitsMin");
        if (minimumUnits is > 0m)
        {
            var minimum = minimumUnits.Value / lotSize.Value;
            row.MeasuredMinimumQuantity = minimum;

            if (Diverges(mapping.MinimumQuantity, minimum))
            {
                row.Findings.Add(
                    $"quantita' minima: la tabella usa {Format(mapping.MinimumQuantity)}, il broker " +
                    $"dichiara {Format(minimum)} lotti.");
            }
        }

        var stepUnits = Number(snapshot, "VolumeInUnitsStep");
        if (stepUnits is > 0m)
        {
            var step = stepUnits.Value / lotSize.Value;
            row.MeasuredQuantityStep = step;

            if (Diverges(mapping.QuantityStep, step))
            {
                row.Findings.Add(
                    $"passo di quantita': la tabella usa {Format(mapping.QuantityStep)}, il broker " +
                    $"dichiara {Format(step)} lotti.");
            }
        }

        return row;
    }

    /// <summary>
    /// Scarto <b>relativo</b> e non assoluto: la stessa differenza di 0,01 e' rumore su un
    /// moltiplicatore da 500 ed e' un errore del 100% su uno da 0,01, e i due casi convivono nella
    /// stessa tabella (<c>@CT</c> vale 500, il passo di un CFD vale 0,01).
    /// </summary>
    private static bool Diverges(decimal table, decimal measured)
    {
        if (table == measured)
            return false;

        var reference = Math.Max(Math.Abs(table), Math.Abs(measured));
        if (reference == 0m)
            return false;

        return Math.Abs(table - measured) / reference > Tolerance;
    }

    /// <summary>
    /// Una proprieta' numerica della rilevazione. I valori arrivano come stringhe scritte da un bot
    /// in cultura invariante: si legge cosi' e non con la cultura corrente, altrimenti su una
    /// macchina italiana "0.1" diventerebbe 1.
    /// </summary>
    private static decimal? Number(SymbolInfoSnapshotDto snapshot, string property)
    {
        if (!snapshot.Properties.TryGetValue(property, out var text) || string.IsNullOrWhiteSpace(text))
            return null;

        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string Format(decimal value) =>
        value.ToString("0.##########", CultureInfo.InvariantCulture);
}
