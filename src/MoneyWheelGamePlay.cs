using MGS.Casino.Veyron.Base.Plugin;
using MGS.Casino.Veyron.Base.Plugin.Data;
using MGS.Casino.Veyron.Base.Utils.Enums;
using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.Data;
using MoneyWheel.Handlers;
using MoneyWheel.Helpers;
using MoneyWheel.MoneyWheel;
using MoneyWheel.Statistics;
using System.Xml.Linq;

namespace MoneyWheel
{
  /// <summary>
  /// GamePlay class
  /// </summary>
  public sealed class MoneyWheelGamePlay : BaseVeyronGamePlay
  {
    private MoneyWheelTestData _testData;
    private MoneyWheelStrategyData _strategyData;
    private readonly StatisticsCollection _statsData;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public MoneyWheelGamePlay()
    {
      throw new VeyronException($"Default constructor of {typeof(MoneyWheelGamePlay)} can not be directly called. How could this have happened?!");
    }

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    /// <param name="configuration">Path to the configuration file.</param>
    /// <param name="statsData">Statistics data.</param>
    internal MoneyWheelGamePlay(MoneyWheelConfiguration configuration, StatisticsCollection statsData)
    {
      _statsData = statsData;
      Game = new MoneyWheelGame(configuration, _statsData);
    }

    /// <summary>
    /// Game play.
    /// </summary>
    public MoneyWheelGame Game { get; private set; }

    /// <summary>
    /// Play event data.
    /// </summary>
    public MoneyWheelPlayEventData EventData { get; private set; }

    /// <inheritdoc />
    public override string ToString()
    {
      return MoneyWheelHelper.MoneyWheel;
    }

    /// <inheritdoc />
    protected override void RegisterClientEventHandlers()
    {
      RegisterClientEventHandler(new MoneyWheelXmlPlayHandler(this));
      RegisterClientEventHandler(new MoneyWheelXmlRefreshHandler(this));
    }

    /// <inheritdoc />
    protected override XDocument GenerateGameStateXml()
    {
      return Game.ToGameStateXml();
    }

    /// <inheritdoc />
    protected override GameResult RestoreGameStateFromXml(BaseGameEventData previousGameEventData, XDocument previousGameStateXml)
    {
      Game.ToInitialState(true);

      var data = (MoneyWheelPlayEventData)previousGameEventData;

      if (data != null && previousGameStateXml != null)
      {
        var moneyWheelSdkXml = previousGameStateXml.Element(MoneyWheelHelper.X).Element(MoneyWheelHelper.MoneyWheel);

        Game.UpdateFromGameStateXml(data, moneyWheelSdkXml);
      }

      return new GameResult(Game.GetTotalWager(), Game.TotalWin);
    }

    ///<inheritdoc />
    protected override GameResult PlayGame(BaseGameEventData gameEventData)
    {
      Game.ToInitialState(false);
      EventData = null;

      if (gameEventData is MoneyWheelPlayEventData data)
      {
        EventData = data;
        Validator.ValidateBets(Settings, Game, EventData.RequestedBets.Bets);

        var rng = DefaultRandomNumberGenerator;

        if (_testData != null)
        {

          rng = _testData.GetNextTestRng();
        }

        Game.Play(EventData.RequestedBets.Bets, rng);

        return new GameResult(Game.GetTotalWager(), Game.TotalWin)
        {
          IsFirstEventInGame = true,
          CompletionStatus = GameCompletionStatus.Complete
        };
      }

      // Theoretically we should never get here, but who knows.
      return new GameResult(0L, 0L)
      {
        IsFirstEventInGame = true,
        CompletionStatus = GameCompletionStatus.Complete
      };
    }

    /// <inheritdoc />
    protected override BaseGameEventData GetEventDataFromStrategy()
    {
      if (_strategyData != null)
      {
        return _strategyData.GetNextStrategy(DefaultRandomNumberGenerator);
      }

      return new MoneyWheelPlayEventData() { EventHandler = new MoneyWheelXmlPlayHandler(this) };
    }

    /// <inheritdoc />
    protected override void ConfigureTestData(XDocument testDataXml)
    {
      _testData = new MoneyWheelTestData(this, testDataXml, DefaultRandomNumberGenerator, Game.Wheel.Positions.Count);
    }

    /// <inheritdoc />
    protected override void ConfigureStrategyData(XDocument strategyDataXml)
    {
      _strategyData = new MoneyWheelStrategyData(this, strategyDataXml);
    }
  }
}