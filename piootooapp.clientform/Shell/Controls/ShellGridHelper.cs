namespace piootooapp.clientform.Shell.Controls;

/// <summary>
/// Stili leggibili per le <see cref="DataGridView"/> della shell.
/// Il tema colora i contenitori con testo bianco; le griglie restano su sfondo
/// <see cref="SystemColors.Window"/> e devono forzare colori di cella nativi.
/// </summary>
internal static class ShellGridHelper
{
    public static void ConfigureReadableGrids(Control root)
    {
        if (root is DataGridView grid)
        {
            ConfigureReadableGrid(grid);
        }

        foreach (Control child in root.Controls)
        {
            ConfigureReadableGrids(child);
        }
    }

    public static void ConfigureReadableGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.BackColor = SystemColors.Window;
        grid.GridColor = SystemColors.ControlDark;
        grid.ForeColor = SystemColors.ControlText;

        ApplyReadableDataCellStyle(grid.DefaultCellStyle, SystemColors.Window);
        ApplyReadableDataCellStyle(grid.RowsDefaultCellStyle, SystemColors.Window);
        ApplyReadableDataCellStyle(grid.AlternatingRowsDefaultCellStyle, SystemColors.Control);
        ApplyReadableDataCellStyle(grid.RowTemplate.DefaultCellStyle, SystemColors.Window);

        ApplyReadableHeaderCellStyle(grid.ColumnHeadersDefaultCellStyle);
        ApplyReadableHeaderCellStyle(grid.RowHeadersDefaultCellStyle);

        foreach (DataGridViewColumn column in grid.Columns)
        {
            PreserveColumnFormatting(column);
        }

        foreach (DataGridViewRow row in grid.Rows)
        {
            ResetRowInheritedStyles(row);
        }
    }

    private static void ApplyReadableDataCellStyle(DataGridViewCellStyle style, Color backColor)
    {
        style.ForeColor = SystemColors.ControlText;
        style.BackColor = backColor;
        style.SelectionForeColor = SystemColors.HighlightText;
        style.SelectionBackColor = SystemColors.Highlight;
    }

    private static void ApplyReadableHeaderCellStyle(DataGridViewCellStyle style)
    {
        style.ForeColor = SystemColors.ControlText;
        style.BackColor = SystemColors.Control;
        style.SelectionForeColor = SystemColors.ControlText;
        style.SelectionBackColor = SystemColors.Control;
    }

    private static void PreserveColumnFormatting(DataGridViewColumn column)
    {
        var alignment = column.DefaultCellStyle.Alignment;
        var format = column.DefaultCellStyle.Format;
        var nullValue = column.DefaultCellStyle.NullValue;
        var wrapMode = column.DefaultCellStyle.WrapMode;

        ApplyReadableDataCellStyle(column.DefaultCellStyle, SystemColors.Window);

        column.DefaultCellStyle.Alignment = alignment;
        if (!string.IsNullOrEmpty(format))
        {
            column.DefaultCellStyle.Format = format;
        }

        if (nullValue is not null)
        {
            column.DefaultCellStyle.NullValue = nullValue;
        }

        column.DefaultCellStyle.WrapMode = wrapMode;
    }

    private static void ResetRowInheritedStyles(DataGridViewRow row)
    {
        row.DefaultCellStyle.BackColor = Color.Empty;
        row.DefaultCellStyle.ForeColor = Color.Empty;
        row.DefaultCellStyle.SelectionBackColor = Color.Empty;
        row.DefaultCellStyle.SelectionForeColor = Color.Empty;
    }
}
