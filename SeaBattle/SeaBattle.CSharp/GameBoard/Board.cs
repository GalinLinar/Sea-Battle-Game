﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SeatBattle.CSharp.GameBoard
{
    public class Board : Control
    {
        private const int BoardHeight = 10;
        private const int BoardWidth = 10;

        private readonly BoardCell[,] _cells;
        private readonly Label[] _rowHeaders;
        private readonly Label[] _columnHeaders;
        private readonly List<Ship> _ships;
        private DraggableShip _draggedShip;


        public Board()
        {
            _cells = new BoardCell[10, 10];
            _rowHeaders = new Label[10];
            _columnHeaders = new Label[10];
            _ships = new List<Ship>();

            CreateRowHeaders();
            CreateColumnHeaders();
            CreateBoard();

            base.MinimumSize = new Size(CellSize.Width * 11, CellSize.Height * 11);
            base.MaximumSize = new Size(CellSize.Width * 11, CellSize.Height * 11);
        }

        private void CreateColumnHeaders()
        {
            for (var i = 1; i < 11; i++)
            {
                var label = new Label
                                {
                                    AutoSize = false,
                                    BackColor = BackColor,
                                    TextAlign = ContentAlignment.MiddleCenter,
                                    Text = i.ToString(),
                                    Location = new Point(CellSize.Width * i, 0),
                                    Size = CellSize
                                };
                _columnHeaders[i - 1] = label;
                Controls.Add(label);

            }

        }

        private void CreateRowHeaders()
        {
            for (var i = 1; i < 11; i++)
            {
                var label = new Label
                                {
                                    AutoSize = false,
                                    BackColor = BackColor,
                                    TextAlign = ContentAlignment.MiddleCenter,
                                    Text = i.ToString(),
                                    Location = new Point(0, CellSize.Height * i),
                                    Size = CellSize
                                };
                _rowHeaders[i - 1] = label;
                Controls.Add(label);
            }

        }

        private void CreateBoard()
        {
            for (var x = 0; x < 10; x++)
            {
                for (var y = 0; y < 10; y++)
                {
                    var cell = new BoardCell(x, y)
                                   {
                                       Size = CellSize,
                                       Location = new Point(CellSize.Width * (x + 1), CellSize.Height * (y + 1)),
                                       State = BoardCellState.Normal,
                                       //IsValidForNewShip = true
                                   };
                    _cells[x, y] = cell;
                    cell.MouseDown += OnCellMouseDown;
                    cell.DragEnter += OnCellDragEnter;
                    cell.DragLeave += OnCellDragLeave;
                    cell.DragDrop += OnCellDragDrop;
                    Controls.Add(cell);
                }
            }
        }

        private Ship GetShipAt(int x, int y)
        {
            foreach (var ship in _ships)
            {
                if (ship.IsLocatedAt(x, y))
                    return ship;
            }
            return null;
        }

        private void OnCellMouseDown(object sender, MouseEventArgs e)
        {
            var cell = (BoardCell)sender;
            var ship = GetShipAt(cell.X, cell.Y);

            if (ship == null)
                return;

            _draggedShip = DraggableShip.From(ship);
            cell.DoDragDrop(ship, DragDropEffects.Copy | DragDropEffects.Move);
        }

        private void OnCellDragEnter(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(typeof(Ship)))
            {
                var cell = (BoardCell)sender;
                _draggedShip.MoveTo(cell.X, cell.Y);

                var canPlaceShip = CanPlaceShip(_draggedShip, cell.X, cell.Y);
                var state = canPlaceShip ? BoardCellState.ShipDrag : BoardCellState.ShipDragInvalid;

                DrawShip(_draggedShip, state);
                
                e.Effect = canPlaceShip ? DragDropEffects.Move : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnCellDragLeave(object sender, EventArgs e)
        {
            var cell = (BoardCell)sender;
            var x1 = cell.X;
            var y1 = cell.Y;
            var x2 = _draggedShip.Orientation == ShipOrientation.Horizontal ? x1 + _draggedShip.Length - 1 : x1;
            var y2 = _draggedShip.Orientation == ShipOrientation.Vertical ? y1 + _draggedShip.Length - 1 : y1;

            RedrawRegion(x1, y1, x2, y2);

        }

        private void OnCellDragDrop(object sender, DragEventArgs e)
        {
            var cell = (BoardCell)sender;
            if (e.Data.GetDataPresent(typeof(Ship)))
            {
                if (!CanPlaceShip(_draggedShip, cell.X, cell.Y))
                    return;

                MoveShip(_draggedShip.Source, cell.X, cell.Y);
                _draggedShip = null;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
            cell.Invalidate();
        }

        public Size CellSize { get { return new Size(25, 25); } }

        public void AddShip(Ship ship, int x, int y)
        {
            if (!CanPlaceShip(ship, x, y))
                throw new InvalidOperationException("Cannot place ship at a given location");
            
            ship.Location = new Point(x, y);
            _ships.Add(ship);
            DrawShip(ship);
        }

        private bool CanPlaceShip(Ship ship, int x, int y)
        {
            var wouldFit = true;

            if (ship.Orientation == ShipOrientation.Horizontal)
            {
                wouldFit = (x + ship.Length) <= BoardWidth;
            }
            else
            {
                wouldFit = (y + ship.Length) <= BoardHeight;
            }

            return wouldFit;
        }

        private void RedrawRegion(int x1, int y1, int x2, int y2)
        {
            SuspendLayout();
            for (var x = x1; x <= x2; x++)
            {
                for (var y = y1; y <= y2; y++)
                {
                    if (x >= BoardWidth || y >= BoardHeight)
                    {
                        continue;
                    }

                    var ship = GetShipAt(x, y);
                    _cells[x, y].State = ship == null ? BoardCellState.Normal : BoardCellState.Ship;
                    _cells[x, y].Invalidate();
                }
            }
            ResumeLayout();
        }


        private void DrawShip(Ship ship)
        {
            DrawShip(ship, BoardCellState.Ship);
        }


        private void DrawShip(Ship ship, BoardCellState state)
        {
            SuspendLayout();
            var rect = ship.GetShipRegion();

            for (var dx = rect.X; dx <= rect.Width; dx++)
            {
                for (var dy = rect.Y; dy <= rect.Height; dy++)
                {
                    if (dx < BoardWidth && dy < BoardHeight)
                    {
                        var cell = _cells[dx, dy];
                        cell.State = state;
                        cell.Invalidate();
                    }
                }
            }
            ResumeLayout();
        }

        private void MoveShip(Ship ship, int x, int y)
        {
            var rect = ship.GetShipRegion();
            _ships.Remove(ship);
            RedrawRegion(rect.X, rect.Y, rect.Right, rect.Bottom);


            //ship.Location = new Point(x, y);
            AddShip(ship, x, y);
        }
    }
}