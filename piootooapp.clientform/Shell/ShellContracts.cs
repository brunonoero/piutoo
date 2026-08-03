namespace piootooapp.clientform.Shell;

/// <summary>Quello che una schermata riceve dallo shell: servizi e navigazione.</summary>
public sealed class ShellContext
{
    public ShellContext(AppServices services, INavigationHost navigation)
    {
        Services = services;
        Navigation = navigation;
    }

    public AppServices Services { get; }

    public INavigationHost Navigation { get; }
}

/// <summary>Area contenuti dello shell, vista da una schermata.</summary>
public interface INavigationHost
{
    /// <summary>Apre una schermata figlia impilandola su quella corrente (lista → dettaglio).</summary>
    void Push(Control screen);

    /// <summary>Torna alla schermata precedente e la ricarica.</summary>
    void GoBack();

    void SetStatus(string message);

    void SetError(string message);
}

/// <summary>
/// Contratto delle schermate. Non è una classe base astratta di proposito: il designer
/// WinForms rifiuta di renderizzare un controllo la cui base sia astratta o generica.
/// </summary>
public interface IShellScreen
{
    /// <summary>Titolo mostrato nella breadcrumb.</summary>
    string ScreenTitle { get; }

    void Initialize(ShellContext context);

    Task LoadAsync(CancellationToken cancellationToken);
}

/// <summary>Implementato dalle schermate di dettaglio per bloccare l'uscita con modifiche pendenti.</summary>
public interface IDirtyAware
{
    bool HasUnsavedChanges { get; }
}
