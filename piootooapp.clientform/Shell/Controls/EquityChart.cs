using System.Drawing.Drawing2D;
using System.Globalization;
using Piootoo.Shared.Models.BestPlans;

namespace piootooapp.clientform.Shell.Controls;

/// <summary>
/// Curva di equity di un best plan, con il drawdown dal picco sotto. Disegnata a mano: la console
/// non ha una libreria di grafici, e per una linea e un'area non ne vale la pena.
/// </summary>
/// <remarks>
/// L'asse del tempo e' proporzionale al tempo, non all'indice del punto: una curva realizzata ha un
/// punto per trade, e disegnarla a indice schiaccerebbe i mesi fermi e allargherebbe quelli fitti.
/// </remarks>
public sealed class EquityChart : Control
{
    private static readonly Color EquityColor = Color.FromArgb(31, 119, 180);
    private static readonly Color DrawdownColor = Color.FromArgb(214, 39, 40);
    private static readonly Color GridColor = Color.FromArgb(225, 225, 225);

    private IReadOnlyList<BestPlanEquityPoint> _points = Array.Empty<BestPlanEquityPoint>();
    private decimal _initialCapital;

    public EquityChart()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint,
            true);
        BackColor = SystemColors.Window;
        ForeColor = SystemColors.ControlText;
    }

    public void SetData(IReadOnlyList<BestPlanEquityPoint> points, decimal initialCapital)
    {
        _points = points;
        _initialCapital = initialCapital;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_points.Count < 2)
        {
            TextRenderer.DrawText(g, "Nessuna curva di equity.", Font, ClientRectangle, SystemColors.GrayText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        const int left = 90, right = 16, top = 12, bottom = 26, gap = 18;
        var width = ClientSize.Width - left - right;
        var height = ClientSize.Height - top - bottom - gap;
        if (width < 40 || height < 60)
            return;

        var equityArea = new Rectangle(left, top, width, (int)(height * 0.72));
        var drawdownArea = new Rectangle(left, equityArea.Bottom + gap, width, height - equityArea.Height);

        var start = _points[0].TimeUtc;
        var span = Math.Max(1d, (_points[^1].TimeUtc - start).TotalSeconds);
        float X(DateTime time) => left + (float)((time - start).TotalSeconds / span * width);

        var min = Math.Min(_points.Min(point => point.Equity), _initialCapital);
        var max = Math.Max(_points.Max(point => point.Equity), _initialCapital);
        if (max == min)
            max = min + 1;
        float Y(decimal equity) => equityArea.Bottom - (float)((equity - min) / (max - min)) * equityArea.Height;

        var maxDrawdown = Math.Max(0.01m, _points.Max(point => point.DrawdownPercent));
        float Yd(decimal drawdown) => drawdownArea.Top + (float)(drawdown / maxDrawdown) * drawdownArea.Height;

        using var gridPen = new Pen(GridColor);
        using var textBrush = new SolidBrush(ForeColor);

        // Griglia orizzontale dell'equity: quattro livelli con l'etichetta a sinistra.
        for (var step = 0; step <= 4; step++)
        {
            var value = min + (max - min) * step / 4;
            var y = Y(value);
            g.DrawLine(gridPen, left, y, left + width, y);
            DrawLabelLeft(g, value.ToString("N0", CultureInfo.CurrentCulture), y);
        }

        // Un separatore per anno: e' l'unita' in cui la lista legge le cifre.
        for (var year = start.Year + 1; year <= _points[^1].TimeUtc.Year; year++)
        {
            var x = X(new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            g.DrawLine(gridPen, x, top, x, drawdownArea.Bottom);
            TextRenderer.DrawText(g, year.ToString(CultureInfo.InvariantCulture), Font,
                new Point((int)x + 3, drawdownArea.Bottom + 4), SystemColors.GrayText);
        }

        TextRenderer.DrawText(g, start.ToString("yyyy-MM-dd"), Font,
            new Point(left, drawdownArea.Bottom + 4), SystemColors.GrayText);
        var endText = _points[^1].TimeUtc.ToString("yyyy-MM-dd");
        var endSize = TextRenderer.MeasureText(endText, Font);
        TextRenderer.DrawText(g, endText, Font,
            new Point(left + width - endSize.Width, drawdownArea.Bottom + 4), SystemColors.GrayText);

        using (var capitalPen = new Pen(Color.Gray) { DashStyle = DashStyle.Dash })
            g.DrawLine(capitalPen, left, Y(_initialCapital), left + width, Y(_initialCapital));

        var equityLine = _points.Select(point => new PointF(X(point.TimeUtc), Y(point.Equity))).ToArray();
        using (var equityPen = new Pen(EquityColor, 1.6f))
            g.DrawLines(equityPen, equityLine);

        var drawdownShape = new List<PointF>(_points.Count + 2) { new(X(_points[0].TimeUtc), drawdownArea.Top) };
        drawdownShape.AddRange(_points.Select(point => new PointF(X(point.TimeUtc), Yd(point.DrawdownPercent))));
        drawdownShape.Add(new PointF(X(_points[^1].TimeUtc), drawdownArea.Top));
        using (var drawdownBrush = new SolidBrush(Color.FromArgb(110, DrawdownColor)))
            g.FillPolygon(drawdownBrush, drawdownShape.ToArray());

        g.DrawLine(gridPen, left, drawdownArea.Top, left + width, drawdownArea.Top);
        DrawLabelLeft(g, "DD 0%", drawdownArea.Top);
        DrawLabelLeft(g, $"-{maxDrawdown:N1}%", drawdownArea.Bottom);
    }

    private void DrawLabelLeft(Graphics g, string text, float y)
    {
        var size = TextRenderer.MeasureText(text, Font);
        TextRenderer.DrawText(g, text, Font, new Point(84 - size.Width, (int)(y - size.Height / 2f)), SystemColors.GrayText);
    }

    /// <summary>
    /// La curva in miniatura per una cella di griglia: solo la linea, verde se chiude sopra il
    /// capitale iniziale, rossa se sotto.
    /// </summary>
    public static void DrawSparkline(Graphics g, Rectangle bounds, IReadOnlyList<BestPlanEquityPoint> points, decimal initialCapital)
    {
        if (points.Count < 2 || bounds.Width < 10 || bounds.Height < 6)
            return;

        var min = Math.Min(points.Min(point => point.Equity), initialCapital);
        var max = Math.Max(points.Max(point => point.Equity), initialCapital);
        if (max == min)
            max = min + 1;

        var start = points[0].TimeUtc;
        var span = Math.Max(1d, (points[^1].TimeUtc - start).TotalSeconds);
        var line = points
            .Select(point => new PointF(
                bounds.Left + (float)((point.TimeUtc - start).TotalSeconds / span * bounds.Width),
                bounds.Bottom - (float)((point.Equity - min) / (max - min)) * bounds.Height))
            .ToArray();

        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var baseline = bounds.Bottom - (float)((initialCapital - min) / (max - min)) * bounds.Height;
        using (var capitalPen = new Pen(Color.LightGray) { DashStyle = DashStyle.Dot })
            g.DrawLine(capitalPen, bounds.Left, baseline, bounds.Right, baseline);
        var color = points[^1].Equity >= initialCapital ? Color.FromArgb(44, 160, 44) : DrawdownColor;
        using (var pen = new Pen(color, 1.4f))
            g.DrawLines(pen, line);
        g.SmoothingMode = previous;
    }
}
