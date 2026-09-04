using System;
using System.Collections.Generic;

namespace TerminalConsole
{
    partial class Program
    {
        // A minimal column writer, in place of System.CommandLine.Rendering's
        // TableView. That package never shipped past 0.4.0-alpha, and its
        // renderer needed Console.WindowWidth, which throws the moment output
        // is redirected.
        private static void WriteTable(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
        {
            int columns = headers.Count;
            int[] widths = new int[columns];

            for (int column = 0; column < columns; column++)
            {
                widths[column] = headers[column].Length;

                foreach (string[] row in rows)
                    widths[column] = Math.Max(widths[column], (row[column] ?? string.Empty).Length);
            }

            // one lock for the whole table, so device output cannot land
            // between two rows and shred the columns
            SayBlock(() =>
            {
                SayLine(Row(headers, widths));

                foreach (string[] row in rows)
                    SayLine(Row(row, widths));
            });
        }

        private static string Row(IReadOnlyList<string> cells, int[] widths)
        {
            var line = new System.Text.StringBuilder();

            for (int column = 0; column < cells.Count; column++)
            {
                string cell = cells[column] ?? string.Empty;

                // the last column is never padded, so nothing trails the line
                line.Append(column == cells.Count - 1 ? cell : cell.PadRight(widths[column] + 2));
            }

            return line.ToString();
        }
    }
}
