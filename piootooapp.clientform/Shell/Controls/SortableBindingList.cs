using System.ComponentModel;
using System.Reflection;

namespace piootooapp.clientform.Shell.Controls;

/// <summary>
/// <see cref="BindingList{T}"/> che sa ordinarsi.
///
/// Serve perché <see cref="BindingList{T}"/> dichiara <c>SupportsSortingCore == false</c>: una
/// <c>DataGridView</c> legata a quel tipo non ordina, e una colonna lasciata su
/// <c>SortMode.Automatic</c> solleva <see cref="InvalidOperationException"/> al primo click
/// sull'intestazione invece di non fare niente. Per questo tutte le liste della console erano
/// esplicitamente <c>NotSortable</c>: il problema non era l'interfaccia, era la collezione.
///
/// L'ordinamento è in memoria sulla lista già caricata: le liste della console tengono tutte le
/// righe del workspace, quindi non c'è niente da chiedere al server. Un ricaricamento ripristina
/// l'ordine della sorgente — è voluto, l'ordinamento è una lente sulla vista, non uno stato da
/// persistere.
/// </summary>
public sealed class SortableBindingList<T> : BindingList<T>
{
    private PropertyDescriptor? _sortProperty;
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;
    private bool _isSorted;

    public SortableBindingList()
    {
    }

    public SortableBindingList(IList<T> items) : base(items)
    {
    }

    protected override bool SupportsSortingCore => true;

    protected override bool IsSortedCore => _isSorted;

    protected override PropertyDescriptor? SortPropertyCore => _sortProperty;

    protected override ListSortDirection SortDirectionCore => _sortDirection;

    protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
    {
        _sortProperty = property;
        _sortDirection = direction;

        if (Items is not List<T> items)
        {
            // La base può essere costruita su una IList qualsiasi: senza List<T> sotto non c'è
            // un Sort in place da chiamare, e riordinare a mano non vale la complessità.
            _isSorted = false;
            return;
        }

        var comparer = new PropertyComparer(property, direction);
        items.Sort(comparer);
        _isSorted = true;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    /// <summary>
    /// Riapplica l'ordinamento corrente, se ce n'è uno.
    ///
    /// Le liste della console svuotano e riempiono la collezione a ogni cambio di filtro: senza
    /// questa chiamata la griglia continuerebbe a mostrare la freccetta sull'intestazione mentre
    /// le righe tornano nell'ordine della sorgente, che è il modo peggiore di sbagliare — sembra
    /// ordinata e non lo è.
    /// </summary>
    public void ReapplySort()
    {
        if (_isSorted && _sortProperty != null)
        {
            ApplySortCore(_sortProperty, _sortDirection);
        }
    }

    protected override void RemoveSortCore()
    {
        _sortProperty = null;
        _isSorted = false;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    /// <summary>
    /// Confronto sul valore della proprietà. I null vanno sempre in fondo indipendentemente dalla
    /// direzione: in queste liste il null è "dato assente" (piano mai assegnato, intervallo di un
    /// backtest senza risultati) e vederlo in cima invertendo l'ordine sarebbe solo rumore.
    /// </summary>
    private sealed class PropertyComparer : IComparer<T>
    {
        private readonly PropertyDescriptor _property;
        private readonly int _sign;

        public PropertyComparer(PropertyDescriptor property, ListSortDirection direction)
        {
            _property = property;
            _sign = direction == ListSortDirection.Descending ? -1 : 1;
        }

        public int Compare(T? x, T? y)
        {
            var left = x == null ? null : _property.GetValue(x);
            var right = y == null ? null : _property.GetValue(y);

            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            // Le stringhe sono nomi e codici: l'ordinamento culture-aware case-insensitive è
            // quello che l'utente si aspetta leggendo la colonna.
            if (left is string leftText && right is string rightText)
                return _sign * string.Compare(leftText, rightText, StringComparison.CurrentCultureIgnoreCase);

            if (left is IComparable comparable)
                return _sign * comparable.CompareTo(right);

            return _sign * string.Compare(
                left.ToString(), right.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }
    }
}

/// <summary>
/// Estensioni per portare una griglia già popolata dal designer al sorting automatico.
/// </summary>
public static class SortableGridExtensions
{
    /// <summary>
    /// Rende ordinabili tutte le colonne della griglia. Va chiamato dopo
    /// <c>InitializeComponent</c>, al posto del ciclo che le marcava <c>NotSortable</c>.
    /// Le colonne senza <c>DataPropertyName</c> restano escluse: non hanno un valore su cui
    /// ordinare e il click sull'intestazione fallirebbe.
    /// </summary>
    public static void EnableColumnSorting(this DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.SortMode = string.IsNullOrEmpty(column.DataPropertyName)
                ? DataGridViewColumnSortMode.NotSortable
                : DataGridViewColumnSortMode.Automatic;
        }
    }
}
