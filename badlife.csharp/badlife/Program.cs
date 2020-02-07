using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace badlife
{
    /// <summary>
    /// Implements Conway's Game Of Life badly
    /// https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life on a torus
    /// </summary>
    public class Program
    {
        private static bool IsToroidalBoard = true;

        private const char IsLiveCell = '*';
        private const char IsDeadCell = '_';

        //private static readonly string filePath = "sample_bad.txt";
        private static readonly string filePath = "sample_input.txt";
        //private static readonly string filePath = "sample_toad.txt";
        //private static readonly string filePath = "sample_blinker.txt";
        //private static readonly string filePath = "sample_glider.txt";

        private static IEnumerable<string> ReadGameData(string filePath)
        {
            var rows = new List<string>();

            try
            {
                using (var input = new StreamReader(filePath))
                {
                    string row;

                    do
                    {
                        row = input.ReadLine();

                        if (!string.IsNullOrWhiteSpace(row))
                        {
                            rows.Add(row);
                        }

                    } while (row != null);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error whilst reading file: {(string.IsNullOrWhiteSpace(filePath) ? "Not Specified" : filePath)}.", ex);
            }

            return rows;
        }

        private static void ValidateGameData(IEnumerable<string> data)
        {
            if (!data.Any(s => s.Contains(IsLiveCell.ToString()) || s.Contains(IsDeadCell.ToString())))
            {
                throw new ArgumentException("Game data is not in a recognized format.", nameof(data));
            }
        }

        private static bool[][] InitialiseBoard(string[] data)
        {
            var board = new bool[data.Length][];

            for (var row = 0; row < data.Length; row++)
            {
                board[row] = new bool[data[row].Length];

                for (var cell = 0; cell < data[row].Length; cell++)
                {
                    board[row][cell] = (data[row][cell] == IsLiveCell);
                }
            }

            return board;
        }

        private static int Main(string[] args)
        {
            try
            {
                Console.Clear();

                var data = ReadGameData(filePath).ToArray();

                ValidateGameData(data);

                var board = InitialiseBoard(data);

                Console.WriteLine("Press Escape key to finish or any other key to Evolve.");

                var generation = 0;
                do
                {
                    RenderBoard(generation, board);

                    generation += 1;

                    board = Evolve(board, IsToroidalBoard);

                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

                return 42; //Not sure about this return code...
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to continue.");
                Console.ReadLine();
                return -1;
            }
        }

        private static void RenderBoard(int generation, bool[][] boardData)
        {
            var columnCount =  boardData.Any()
                ? boardData[0]?.Length
                : 0;

            var grid = new StringBuilder();

            foreach (var row in boardData)
            {
                for (var cellIdx = 0; cellIdx < columnCount; cellIdx++)
                {
                    grid.Append(row[cellIdx] ? IsLiveCell : IsDeadCell);
                }

                grid.AppendLine();
            }

            Console.SetCursorPosition(0, 1); //Reset cursor position, better performance the Console.Clear().

            Console.WriteLine($"Generation: {generation}"); //Track Generation.

            Console.Write(grid.ToString());
        }

        private static bool[][] Evolve(bool[][] currentBoardState, bool isToroidal)
        {
            var rowCount = currentBoardState.Length;
            var newBoardState = new bool[rowCount][];

            if (newBoardState.Any())
            {
                var columnCount = currentBoardState[0].Length;

                for (var row = 0; row < rowCount; row++)
                {
                    newBoardState[row] = new bool[columnCount];

                    for (var cell = 0; cell < columnCount; cell++)
                    {
                        var liveNeighbors = GetLiveNeighborCount(row, cell, currentBoardState, isToroidal);

                        //Apply the Game of Life Rules
                        //Rules are:
                        //1. Any live cell with two or three neighbors survives.
                        //2. Any dead cell with three live neighbors becomes a live cell.
                        //3. All other live cells die in the next generation. Similarly, all other dead cells stay dead.

                        newBoardState[row][cell] = currentBoardState[row][cell]
                            ? !((liveNeighbors < 2) || (liveNeighbors > 3))
                            : liveNeighbors == 3;
                    }
                }
            }

            return newBoardState;
        }

        /// <summary>
        /// Gets the number of live neighbors in the 9 cell vicinity of a given cell.
        /// </summary>
        /// <param name="x">X-coordinate of the cell.</param>
        /// <param name="y">Y-coordinate of the cell.</param>
        /// <param name="board">The board.</param>
        /// <param name="isToroidalBoard">Indicates if the board is on a torus or not.</param>
        /// <returns>The number of live neighbors.</returns>
        private static int GetLiveNeighborCount(int x, int y, bool[][] board, bool isToroidalBoard)
        {
            var liveNeighbors = 0;

            var boardHeight = board.Length;

            var boardWidth = board[0].Length;

            for (var yOffset = -1; yOffset <= 1; yOffset++)
            {
                // If y + yOffset is off the board edge, and the board is not on a torus then continue.
                if ((((y + yOffset) < 0) || ((y + yOffset) >= boardHeight)) && !isToroidalBoard)
                {
                    continue;
                }

                // Loop around the edges if y + yOffset is off the board.
                var virtualY = (y + yOffset + boardHeight) % boardHeight;

                for (var xOffset = -1; xOffset <= 1; xOffset++)
                {
                    // If  x + xOffset is off the board edge, and the board is not on a torus then continue.
                    if (((x + xOffset < 0) || ((x + xOffset) >= boardWidth)) && !isToroidalBoard)
                    {
                        continue;
                    }

                    // Loop around the edges if x + xOffset is off the board.
                    var virtualX = (x + xOffset + boardWidth) % boardWidth;

                    // Count the neighbor cell at (h,k) if it is alive.
                    if (board[virtualX][virtualY])
                    {
                        liveNeighbors += 1;
                    }
                }
            }

            // Decrement by 1 if (x,y) is alive since we counted it as a neighbor.
            return liveNeighbors - (board[x][y] ? 1 : 0);
        }
    }
}