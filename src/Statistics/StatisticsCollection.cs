using System.Collections.Generic;

namespace MoneyWheel.Statistics
{
  internal class StatisticsCollection
  {
    public StatisticsCollection()
    {
      MultiplierStatsCollection = new Dictionary<int, MultiplierNumbers>();
      IndexesSymbolsStatsCollection = new Dictionary<int, IndexesSymbols>();
    }

    public Dictionary<int, MultiplierNumbers> MultiplierStatsCollection { get; }
    public Dictionary<int, IndexesSymbols> IndexesSymbolsStatsCollection { get; }
  }

  /// <summary>
  /// Contains all properties required for Dictionary.
  /// </summary>
  public class MultiplierNumbers
  {
    /// <summary>
    /// The number of hits.
    /// </summary>
    public long TimesLanded { get; set; }
  }

  /// <summary>
  /// Contains all properties required for Dictionary.
  /// </summary>
  public class IndexesSymbols
  {
    /// <summary>
    /// Index at the wheel.
    /// </summary>
    public long Index { get; set; }

    /// <summary>
    /// The number of hits.
    /// </summary>
    public long IndexTimesLanded { get; set; }

    /// <summary>
    /// Symbol at the wheel.
    /// </summary>
    public string Symbol { get; set; }
  }
}