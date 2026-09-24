namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// La memoria di sessione di una strategia PT5DAV: OHLC della sessione in corso e delle cinque
/// precedenti, ATR50 di Wilder, indice della barra nella sessione.
///
/// <para><b>Perche' e' una struct.</b> Lo stato di una strategia viaggia fra una valutazione e
/// l'altra come <c>RuntimeState</c>, catturato e ripristinato per riflessione campo per campo
/// (<c>StatelessEasyStrategyBase</c>). Un tipo valore si copia intero ed e' indipendente fra le
/// valutazioni; un array o un oggetto verrebbe condiviso per riferimento fra l'istanza registrata e
/// i suoi cloni effimeri.</para>
///
/// <para><b>Perche' incrementale.</b> Una media di Wilder a 50 sessioni converge solo dopo centinaia
/// di sessioni: ricalcolarla dalla finestra a ogni barra costerebbe (barre × sessioni) sul punto piu'
/// caldo del sistema, e con la finestra che il backtest passa non converge mai. Tenuta come stato,
/// si aggiorna una volta per sessione e continua per tutto il run. Quando lo stato manca — primo
/// giro, riavvio del server, finestra che non si sovrappone — si ricostruisce dalla finestra ricevuta
/// (<see cref="Pt5DavEngineBase"/>).</para>
/// </summary>
public struct Pt5DavSessionState
{
    /// <summary>Apertura dell'ultima barra incorporata. <c>default</c> = stato vuoto.</summary>
    public DateTime LastBarUtc;

    /// <summary>Vero se l'ultima barra incorporata sta nella fascia di mercato della ricerca.</summary>
    public bool LastBarInMarket;

    /// <summary>Giorno della sessione in corso (giorno della ricerca, domenica accodata al lunedi').</summary>
    public DateTime CurrentDay;

    /// <summary>Vero se la sessione in corso e' iniziata prima della finestra ricevuta: i suoi OHLC sono parziali.</summary>
    public bool CurrentIsPartial;

    /// <summary>Indice della barra corrente nella sessione, da 0 (la ricerca conta cosi').</summary>
    public int CurrentBarIndex;

    /// <summary>OHLC della sessione in corso, barra corrente inclusa (d0).</summary>
    public decimal O0, H0, L0, C0;

    /// <summary>Massimo e minimo della sessione in corso <b>esclusa</b> la barra corrente.</summary>
    public decimal H0BeforeBar, L0BeforeBar;

    /// <summary>Vero se nella sessione in corso c'era almeno una barra prima della corrente.</summary>
    public bool HasBarBeforeInSession;

    /// <summary>Close della barra precedente alla corrente, anche se di un'altra sessione.</summary>
    public decimal PreviousBarClose;

    /// <summary>Vero se <see cref="PreviousBarClose"/> e' valorizzato.</summary>
    public bool HasPreviousBar;

    /// <summary>Sessioni chiuse d1..d5: OHLC.</summary>
    public decimal O1, H1, L1, C1;
    public decimal O2, H2, L2, C2;
    public decimal O3, H3, L3, C3;
    public decimal O4, H4, L4, C4;
    public decimal O5, H5, L5, C5;

    /// <summary>Quante sessioni chiuse e complete sono entrate nell'ATR.</summary>
    public int SessionsClosed;

    /// <summary>Quante sessioni chiuse ci sono in d1..d5 (anche parziali), fino a 5.</summary>
    public int SessionsInHistory;

    /// <summary>Somma dei range veri delle prime <see cref="Pt5DavEngineBase.AtrPeriod"/> sessioni: il seme di Wilder.</summary>
    public decimal TrueRangeSum;

    /// <summary>ATR50 di Wilder sulle sessioni chiuse. Vale solo con <see cref="SessionsClosed"/> ≥ 50.</summary>
    public decimal Atr;

    /// <summary>Close dell'ultima sessione chiusa, per il range vero della prossima.</summary>
    public decimal PreviousSessionClose;

    /// <summary>Vero se <see cref="PreviousSessionClose"/> e' valorizzato.</summary>
    public bool HasPreviousSessionClose;
}
