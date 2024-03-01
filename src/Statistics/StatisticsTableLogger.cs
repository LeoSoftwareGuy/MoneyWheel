using MGS.Casino.Interfaces.Logging;
using System.Collections.Generic;
using System.Text;

namespace MoneyWheel.Statistics
{
  /// <summary>
  /// Draws visual table inside the Veyron Plugin Tester.
  /// </summary>
  public class StatisticsTableLogger
  {
    private readonly string _tableName;
    private readonly bool _hasMainTable;
    private readonly bool _hasEndTable;
    private readonly int _fixedSize;
    private readonly IList<int> _columnSizes;
    private readonly IList<string> _rows;
    private readonly IList<string> _endRows;
    private string _tableHeader;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="tableName">Name.</param>
    /// <param name="hasMainTable">Is a main table.</param>
    /// <param name="hasEndTable">Is an end table.</param>
    /// <param name="fixedSize">Size.</param>
    public StatisticsTableLogger(string tableName, bool hasMainTable = true, bool hasEndTable = true, int fixedSize = 0)
    {
      _tableName = tableName;
      _hasMainTable = hasMainTable;
      _hasEndTable = hasEndTable;
      _hasMainTable = hasMainTable;
      _fixedSize = fixedSize;

      _tableHeader = "| ";
      _columnSizes = new List<int>();
      _rows = new List<string>();
      _endRows = new List<string>();
    }

    /// <summary>
    /// Adds a column to the table.
    /// </summary>
    public void AddColumn(string columnName)
    {
      _tableHeader += columnName + " | ";
      _columnSizes.Add(columnName.Length + 1);
    }

    /// <summary>
    /// Adds a row to the table.
    /// </summary>
    /// <param name="rowCells"></param>
    public void AddRow(IList<string> rowCells)
    {
      var builder = new StringBuilder();
      builder.Append("| ");
      for (int i = 0; i < rowCells.Count; i++)
      {
        builder.Append(rowCells[i].PadRight(_columnSizes[i]));
        builder.Append("| ");
      }
      _rows.Add(builder.ToString().Trim());
    }

    /// <summary>
    /// Adds an end row to the table.
    /// </summary>
    /// <param name="row">Insert a row.</param>
    /// <param name="toBeginning">Insert a row at the beginning.</param>
    public void AddEndRow(string row, bool toBeginning = false)
    {
      row = _hasMainTable ? $"| {row.PadRight(_tableHeader.Trim().Length - 4)} |" : $"| {row.PadRight(_fixedSize - 4)} |";

      if (toBeginning)
      {
        _endRows.Insert(0, row);
      }
      else
      {
        _endRows.Add(row);
      }
    }

    /// <summary>
    /// Draws table at Veyron Plugin Tester.
    /// </summary>
    public void LogTable()
    {
      string splitter;
      if (_hasMainTable)
      {
        _tableHeader = _tableHeader.Trim();
        splitter = "-" + "".PadRight(_tableHeader.Length - 2, '-') + "-";
      }
      else
      {
        splitter = "-" + "".PadRight(_fixedSize - 2, '-') + "-";
      }
      var innerSplitter = "|" + splitter.Substring(2) + "|";

      Logger.LogInformationalEvent("");
      Logger.LogInformationalEvent(_tableName + " :");
      Logger.LogInformationalEvent(splitter);

      if (_hasMainTable)
      {
        Logger.LogInformationalEvent(_tableHeader);
        Logger.LogInformationalEvent(innerSplitter);
        foreach (var row in _rows)
        {
          Logger.LogInformationalEvent(row);
        }

        Logger.LogInformationalEvent(_hasEndTable ? innerSplitter : splitter);
      }

      if (_hasEndTable)
      {
        AddEndRow(" ", true);

        foreach (var row in _endRows)
        {
          Logger.LogInformationalEvent(row);
        }
        Logger.LogInformationalEvent(splitter);
      }
    }
  }
}