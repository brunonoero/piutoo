namespace Piootoo.Shared.Interfaces;

/// <summary>
/// Esclude una strategia dal catalogo eseguibile lasciandone il sorgente nel repository.
///
/// <para><b>Perché esiste.</b> Prima di questo attributo l'unico modo per togliere una strategia
/// dal catalogo era <c>IsPositionCloseDependent</c>, che però significa un'altra cosa — "l'uscita
/// è decisa a runtime e non è esprimibile nel segnale di ingresso". Usarlo per nascondere una
/// strategia perfettamente eseguibile avrebbe reso quel flag inaffidabile come diagnosi. Una
/// strategia disabilitata non è rotta: è corretta e si sceglie di non eseguirla.</para>
///
/// <para><b>Effetto.</b> <c>StrategyFactory.GetRegisteredStrategies</c> la salta, quindi non è
/// selezionabile nel masterfilter, non entra nei backtest né nelle sessioni. Resta però
/// istanziabile per nome con <c>CreateStrategy</c>: i test di parità e i confronti storici devono
/// poterla ancora costruire.</para>
///
/// <para><b>Il motivo è obbligatorio</b> e viene mostrato a chi legge il catalogo: una strategia
/// disabilitata senza una ragione scritta viene riattivata da qualcuno, mesi dopo, che non sa
/// perché era spenta.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StrategiaDisabilitataAttribute : Attribute
{
    public StrategiaDisabilitataAttribute(string motivo) => Motivo = motivo;

    /// <summary>Perché la strategia non deve essere eseguita.</summary>
    public string Motivo { get; }
}
