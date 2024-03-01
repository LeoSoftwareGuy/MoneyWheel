using MGS.Casino.Veyron.Base.Plugin;
using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.Helpers;
using MoneyWheel.Statistics;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MoneyWheel
{
  /// <summary>
  /// Represents the game and information provided byte the .veyrongame files. This class is loaded once and game play objects created off of it.
  /// </summary>
  public sealed partial class MoneyWheelVeyronGame : BaseVeyronGame<MoneyWheelGamePlay, MoneyWheelPlaycheckProvider>
  {
    private static readonly StatisticsCollection StatsCollection = new StatisticsCollection();

    /// <summary>
    /// Method to initialise your game based on configuration in the .veyrongame file. Remove this method if it does not need to be used.
    /// </summary>
    /// <param name="additionalPluginFiles"></param>
    /// <param name="additionalGameFiles"></param>
    /// <returns></returns>
    protected override bool Initialise(IEnumerable<FileInfo> additionalPluginFiles, IEnumerable<FileData> additionalGameFiles)
    {
      var configurationFile = additionalGameFiles.FirstOrDefault();
      if (configurationFile != null)
      {
        Configuration = new MoneyWheelConfiguration(configurationFile.Info.ToString());
        GameModuleId = int.Parse(Configuration.ModuleId);
      }

      return true;
    }

    /// <summary>
    /// Path to the configuration file.
    /// </summary>
    public MoneyWheelConfiguration Configuration;

    /// <summary>
    /// Method to create your game play object from the configuration available. Remove this if you game is simple enough not to require specific initialization.
    /// </summary>
    /// <returns></returns>
    protected override MoneyWheelGamePlay CreateGamePlay()
    {
      // Example:
      return new MoneyWheelGamePlay(Configuration, StatsCollection);
    }

    /// <summary>
    /// Method to create your Playcheck provider object from the configuration available. Remove this if you provider is simple enough not to require specific initialization.
    /// </summary>
    /// <returns></returns>
    protected override MoneyWheelPlaycheckProvider CreatePlaycheck()
    {
      // Example:
      return new MoneyWheelPlaycheckProvider();
    }
  }
}