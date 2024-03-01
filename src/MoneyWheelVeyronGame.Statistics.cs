using MoneyWheel.Statistics;
using System.Collections.Generic;
using System.Linq;

namespace MoneyWheel
{
  public partial class MoneyWheelVeyronGame
  {
    /// <summary>
    /// Destructor to load the statistics.
    /// </summary>
    ~MoneyWheelVeyronGame()
    {
#if DEBUG
      LogMultiplierStatsCount();
      LogIndexesSymbolsStatsCount();
#endif
    }

    private void LogMultiplierStatsCount()
    {
      var tableLogger = new StatisticsTableLogger(" Multiplier Statistics", true, false);
      tableLogger.AddColumn("Multiplier X: ");
      tableLogger.AddColumn("Landed: ");

      var sortedCollection = StatsCollection.MultiplierStatsCollection.OrderBy(x => x.Key);

      foreach (var x in sortedCollection)
      {
        tableLogger.AddRow(
          new List<string>()
          {
            x.Key.ToString(),
            x.Value.TimesLanded.ToString()
          });
      }

      tableLogger.AddRow(
        new List<string>()
        {
          "sum ",
          StatsCollection.MultiplierStatsCollection.Sum(s => s.Value.TimesLanded).ToString()
        });

      tableLogger.LogTable();
    }


    private void LogIndexesSymbolsStatsCount()
    {
      var tableLogger = new StatisticsTableLogger(" Indexes & Symbols Statistics", true, false);

      tableLogger.AddColumn("Index Received: ");
      tableLogger.AddColumn("Symbol Received: ");
      tableLogger.AddColumn("Index Landed: ");

      var sortedCollection = StatsCollection.IndexesSymbolsStatsCollection.OrderBy(x => x.Key);

      foreach (var x in sortedCollection)
      {
        {
          tableLogger.AddRow(
            new List<string>()
            {
              x.Key.ToString(),
              x.Value.Symbol,
              x.Value.IndexTimesLanded.ToString()
            });
        }
      }

      tableLogger.AddRow(
        new List<string>()
        {
          "sum ",
          StatsCollection.IndexesSymbolsStatsCollection.Sum(s => s.Value.IndexTimesLanded).ToString()
        });

      tableLogger.LogTable();
    }
  }
}