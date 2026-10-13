using System;
using System.Collections.Generic;

namespace GreedyBestFirstSearchGUI
{
    public enum CellType
    {
        Empty,
        Obstacle,
        Start,
        Goal,
        Frontier,
        Visited,
        Path
    }

    public class GridCell
    {
        public int Row { get; }
        public int Col { get; }
        public CellType Type { get; set; } = CellType.Empty;

        public GridCell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public double EuclideanDistanceTo(GridCell target)
        {
            double dr = Row - target.Row;
            double dc = Col - target.Col;
            return Math.Sqrt(dr * dr + dc * dc);
        }

        public double ManhattanDistanceTo(GridCell target)
        {
            return Math.Abs(Row - target.Row) + Math.Abs(Col - target.Col);
        }
    }

    public class CellSearchNode
    {
        public GridCell Cell { get; }
        public CellSearchNode? Parent { get; }
        public double H { get; }

        public CellSearchNode(GridCell cell, CellSearchNode? parent, double h)
        {
            Cell = cell;
            Parent = parent;
            H = h;
        }
    }
}
