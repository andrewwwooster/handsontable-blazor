namespace HandsontableBlazor;

public class CellRange {
    public CellCoords From { get; set; }
    public CellCoords To { get; set; }

    public int X1 => From.Row;
    public int Y1 => From.Col;
    public int X2 => To.Row;
    public int Y2 => To.Col;

    public CellRange(int x1, int y1, int x2, int y2) {
        From = new CellCoords{Row = x1, Col = y1};
        To = new CellCoords{Row = x2, Col = y2};
    }
}