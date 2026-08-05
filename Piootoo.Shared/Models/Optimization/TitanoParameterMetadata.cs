using System.ComponentModel;
using System.Globalization;

namespace Piootoo.Shared.Models.Optimization;

/// <summary>
/// Metadati di presentazione dei parametri Titano.
///
/// <para>Stanno in <c>Piootoo.Shared</c>, accanto al modello che descrivono, per la stessa ragione
/// per cui ci stanno gli attributi <c>Category</c> e <c>Description</c> di
/// <see cref="TitanoRotationSetup"/>: una classe adattatrice nel client sarebbe una seconda
/// dichiarazione del modello, e al primo parametro aggiunto resterebbe indietro in silenzio.
/// Non sono logica di dominio e non introducono dipendenze verso gli altri progetti.</para>
/// </summary>
public enum TitanoParameterLevel
{
    /// <summary>Parametri che si capiscono dal nome e che vale la pena toccare per primi.</summary>
    Base,

    /// <summary>
    /// Parametri di calibrazione fine. Cambiarli senza aver letto
    /// <c>docs/domini/titano-rotation.md</c> produce quasi sempre un manifest peggiore di quello di
    /// partenza — e, cosa peggiore, plausibile.
    /// </summary>
    Avanzato
}

/// <summary>
/// Livello di un parametro, usato dal <c>PropertyGrid</c> della console tramite
/// <c>BrowsableAttributes</c>: con il filtro su <see cref="TitanoParameterLevel.Base"/> il grid
/// mostra solo le proprietà che portano questo attributo con quel valore.
///
/// <para>Ogni proprietà visibile di <see cref="TitanoRotationSetup"/> deve averlo. Una proprietà
/// senza attributo sparisce dalla vista Base <em>e</em> da quella completa quando il filtro è
/// attivo: è voluto, così dimenticarlo si nota subito invece di produrre un campo fantasma.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TitanoLevelAttribute(TitanoParameterLevel level) : Attribute
{
    public TitanoParameterLevel Level { get; } = level;

    // BrowsableAttributes confronta gli attributi per valore: senza queste due l'uguaglianza sarebbe
    // per riferimento e il filtro non troverebbe mai una corrispondenza.
    public override bool Equals(object? obj) => obj is TitanoLevelAttribute other && other.Level == Level;

    public override int GetHashCode() => Level.GetHashCode();
}

/// <summary>
/// Mostra una frazione come percentuale e la riaccetta come tale: il modello continua a contenere
/// <c>0,15</c>, l'utente legge e digita <c>15 %</c>.
///
/// <para>È la correzione del singolo equivoco più costoso della schermata. I parametri di Titano
/// sono frazioni — drawdown, rendimenti minimi, moltiplicatori di allocazione — e un campo che
/// accetta sia <c>15</c> sia <c>0,15</c> senza dichiarare quale intende è un errore di fattore 100
/// che non produce eccezioni: produce un manifest con tutte le strategie accese, o tutte spente,
/// e nessun messaggio.</para>
///
/// <para>La conversione avviene solo verso e da <see cref="string"/>. La serializzazione JSON non
/// passa dai <c>TypeConverter</c>, quindi il contratto verso il server resta la frazione.</para>
/// </summary>
public sealed class PercentTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
        {
            return base.ConvertFrom(context, culture, value);
        }

        var cleaned = text.Replace("%", string.Empty).Trim();
        if (cleaned.Length == 0)
        {
            return 0m;
        }

        culture ??= CultureInfo.CurrentCulture;

        // Accetta sia la virgola sia il punto: chi digita in fretta usa quello che ha sul tastierino
        // numerico, e rifiutare la variante "sbagliata" per la cultura corrente è solo attrito.
        if (!decimal.TryParse(cleaned, NumberStyles.Float, culture, out var percent) &&
            !decimal.TryParse(cleaned.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out percent))
        {
            throw new FormatException($"'{text}' non è una percentuale valida. Esempio: 15 oppure 15,5.");
        }

        return percent / 100m;
    }

    public override object? ConvertTo(
        ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType != typeof(string) || value is not decimal fraction)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }

        // Due decimali sulla percentuale = quattro sulla frazione, che è la granularità che il resto
        // della console già usa per questi campi.
        return (fraction * 100m).ToString("0.##", culture ?? CultureInfo.CurrentCulture) + " %";
    }
}
