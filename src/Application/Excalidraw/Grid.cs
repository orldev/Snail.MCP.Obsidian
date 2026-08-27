namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>
/// Arranges the stages of a scheme in a grid instead of one long row. A row is filled left to right, the next
/// one right to left (boustrophedon), so consecutive stages always end up side by side or one above the other,
/// and the end of the flow comes back near its start. The number of columns is the one that makes the whole
/// drawing closest to a comfortable landscape shape.
/// </summary>
internal sealed class Grid
{
    private const double TargetAspect = 1.5;
    private const double ColumnGap = 200;
    private const double RowGap = 150;

    private readonly double[] _columnWidth;
    private readonly double[] _rowHeight;
    private readonly double[] _columnX;
    private readonly double[] _rowY;

    private Grid(IReadOnlyList<(double Width, double Height)> stages, int columns)
    {
        Columns = columns;
        var rows = (stages.Count + columns - 1) / columns;

        _columnWidth = new double[columns];
        _rowHeight = new double[rows];
        for (var stage = 0; stage < stages.Count; stage++)
        {
            var (row, column) = Position(stage);
            _columnWidth[column] = Math.Max(_columnWidth[column], stages[stage].Width);
            _rowHeight[row] = Math.Max(_rowHeight[row], stages[stage].Height);
        }

        _columnX = Offsets(_columnWidth, ColumnGap);
        _rowY = Offsets(_rowHeight, RowGap);
    }

    public int Columns { get; }

    public static Grid Pack(IReadOnlyList<(double Width, double Height)> stages) =>
        Enumerable.Range(1, stages.Count)
            .Select(columns => new Grid(stages, columns))
            .MinBy(grid => Math.Abs(Math.Log(grid.Aspect / TargetAspect)))!;

    public (int Row, int Column) Position(int stage)
    {
        var row = stage / Columns;
        var step = stage % Columns;

        return (row, row % 2 == 0 ? step : Columns - 1 - step);
    }

    public (double X, double Y, double Width, double Height) Cell(int stage)
    {
        var (row, column) = Position(stage);

        return (_columnX[column], _rowY[row], _columnWidth[column], _rowHeight[row]);
    }

    private double Aspect => Total(_columnWidth, ColumnGap) / Total(_rowHeight, RowGap);

    private static double Total(double[] sizes, double gap) =>
        sizes.Sum() + gap * (sizes.Length - 1);

    private static double[] Offsets(double[] sizes, double gap)
    {
        var offsets = new double[sizes.Length];
        for (var i = 1; i < sizes.Length; i++)
            offsets[i] = offsets[i - 1] + sizes[i - 1] + gap;

        return offsets;
    }
}
