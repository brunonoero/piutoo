namespace Piootoo.Shared.Models.Brokers;

/// <summary>
/// Il confronto fra quello che il broker <b>dichiara</b> (l'archivio <c>symbol-info</c>) e quello
/// che la tabella di conversione <b>usa</b> per calcolare le size.
///
/// <para><b>Confronta e basta: non riscrive niente.</b> Quei numeri decidono quanti contratti va a
/// mercato un segnale, e una riga cambiata da sola — magari perche' il broker ha ritoccato il lotto
/// minimo — sposterebbe il rischio di ogni strategia senza che nessuno abbia deciso nulla. Il
/// rapporto dice cosa non torna; correggere resta una scelta, presa guardando.</para>
/// </summary>
public sealed class SymbolConversionReconciliationDto
{
    public string Broker { get; set; } = string.Empty;

    /// <summary>Codice della tabella di conversione confrontata.</summary>
    public string ConversionCode { get; set; } = string.Empty;

    public string ConversionName { get; set; } = string.Empty;

    /// <summary>Righe che non tornano: e' il motivo per cui si guarda questo rapporto.</summary>
    public int Divergent { get; set; }

    /// <summary>Righe verificate e coerenti.</summary>
    public int Confirmed { get; set; }

    /// <summary>Righe su cui non si puo' dire niente: manca la rilevazione, o manca la spec del future.</summary>
    public int Unverifiable { get; set; }

    public List<SymbolConversionReconciliationRowDto> Rows { get; set; } = [];

    /// <summary>
    /// Strumenti che il broker dichiara e che la tabella non mappa. Non e' un errore — un conto
    /// offre centinaia di strumenti e il piano ne usa venti — ma su un broker <b>nuovo</b> e'
    /// l'elenco da cui si costruisce la tabella.
    /// </summary>
    public List<string> ArchivedButNotMapped { get; set; } = [];
}

/// <summary>Una riga della tabella di conversione, messa accanto alla rilevazione del broker.</summary>
public sealed class SymbolConversionReconciliationRowDto
{
    /// <summary>Simbolo Piootoo (<c>@NQ</c>).</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Simbolo sul conto, come lo dichiara la tabella (<c>USTEC</c>).</summary>
    public string AccountSymbol { get; set; } = string.Empty;

    /// <summary>Il moltiplicatore che la tabella usa oggi.</summary>
    public decimal TableMultiplier { get; set; }

    /// <summary>
    /// Il moltiplicatore che discende dalla rilevazione:
    /// <c>valore punto del future / dimensione del lotto del broker</c>. Null quando non e'
    /// calcolabile.
    /// </summary>
    public decimal? MeasuredMultiplier { get; set; }

    /// <summary>Valore di un punto del contratto di riferimento, dal registro strumenti.</summary>
    public decimal? FuturePointValue { get; set; }

    /// <summary>Unita' di sottostante in un lotto del broker (<c>Symbol.LotSize</c>).</summary>
    public decimal? BrokerLotSize { get; set; }

    /// <summary>Quantita' minima in lotti che discende dalla rilevazione, contro quella in tabella.</summary>
    public decimal? MeasuredMinimumQuantity { get; set; }

    public decimal TableMinimumQuantity { get; set; }

    public decimal? MeasuredQuantityStep { get; set; }

    public decimal TableQuantityStep { get; set; }

    /// <summary>Data della rilevazione da cui escono i valori misurati.</summary>
    public DateTime? MeasuredAtUtc { get; set; }

    /// <summary>
    /// Cosa non torna, a parole, riga per riga. Vuoto = la riga e' confermata. Il rapporto elenca
    /// <b>tutte</b> le righe e non solo quelle divergenti: una riga assente si leggerebbe come "non
    /// c'era niente da dire", che e' il contrario di "non c'era nessuna rilevazione".
    /// </summary>
    public List<string> Findings { get; set; } = [];

    /// <summary>Vero quando non c'e' abbastanza per dire alcunche'.</summary>
    public bool Unverifiable { get; set; }
}
