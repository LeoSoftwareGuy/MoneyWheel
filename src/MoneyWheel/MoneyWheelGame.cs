using MGS.Casino.Veyron.Base.Utils;
using MGS.Casino.Veyron.Interfaces;
using MGS.Random.Interfaces;
using MoneyWheel.Data;
using MoneyWheel.Helpers;
using MoneyWheel.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MoneyWheel.MoneyWheel
{
  /// <summary>
  /// Custom Game Play class
  /// </summary>
  public class MoneyWheelGame
  {
    private MoneyWheelConfiguration _configuration;
    private readonly StatisticsCollection _statsData;

    private List<MoneyWheelBet> _bets = new List<MoneyWheelBet>();

    /// <summary>
    /// Creates new instance with provided parameters.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <param name="statsData">Statistics data.</param>
    internal MoneyWheelGame(MoneyWheelConfiguration configuration, StatisticsCollection statsData)
    {
      _configuration = configuration;
      _statsData = statsData;

      TotalWin = 0;
      Name = _configuration.GameName;
      Wheel = new MoneyWheelWheel(_configuration);
      Spins = new List<MoneyWheelSpin>();
    }

    /// <summary>
    /// Name of the game.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Wheel used to play.
    /// </summary>
    public MoneyWheelWheel Wheel { get; set; }

    /// <summary>
    /// Spins made during the game.
    /// </summary>
    public List<MoneyWheelSpin> Spins { get; }

    /// <summary>
    /// Total win for the game.
    /// </summary>
    public long TotalWin { get; private set; }

    /// <summary>
    /// Creates XML formatted representation of the current Game instance.
    /// </summary>
    /// <returns>XML formatted Game instance.</returns>
    public XDocument ToResponseXml(MoneyWheelGamePlay gamePlay, bool includeSettings)
    {
      var output = new StringBuilder();
      using (var writer = XmlWriter.Create(output, XmlUtil.DefaultWriterSettings))
      {
        writer.WriteStartElement(MoneyWheelHelper.MoneyWheel); // Start MoneyWheel
        writer.WriteAttributeString(MoneyWheelHelper.name, Name);

        if (includeSettings)
        {
          writer.WriteStartElement(MoneyWheelHelper.GameSettings); // Start GameSettings
          writer.WriteAttributeString(MoneyWheelHelper.MinBet, gamePlay.Settings.Get<string>(MoneyWheelHelper.MinBet, true));
          writer.WriteAttributeString(MoneyWheelHelper.MaxBet, gamePlay.Settings.Get<string>(MoneyWheelHelper.MaxBet, true));
          writer.WriteAttributeString(MoneyWheelHelper.DefaultChipSize, gamePlay.Settings.Get<string>(MoneyWheelHelper.DefaultChipSize, true));
          writer.WriteAttributeString(MoneyWheelHelper.ValidChipSizes, gamePlay.Settings.Get<string>(MoneyWheelHelper.ValidChipSizes, true));
          writer.WriteAttributeString(MoneyWheelHelper.DefaultNumChips, gamePlay.Settings.Get<string>(MoneyWheelHelper.DefaultNumChips, true));
          writer.WriteAttributeString(MoneyWheelHelper.MinIncrement, gamePlay.Settings.Get<string>(MoneyWheelHelper.MinIncrement, true));
          writer.WriteAttributeString(MoneyWheelHelper.MinTableBet, gamePlay.Settings.Get<string>(MoneyWheelHelper.MinTableBet, true));
          writer.WriteAttributeString(MoneyWheelHelper.MaxTableBet, gamePlay.Settings.Get<string>(MoneyWheelHelper.MaxTableBet, true));

          // For first refresh only
          if (gamePlay.State.TransactionNumber < 0)
          {
            writer.WriteAttributeString(MoneyWheelHelper.defaultWheelIndex, Wheel.GetDefaultPositionIndex(_configuration).ToString());
          }

          writer.WriteEndElement(); // End GameSettings
        }

        writer.WriteStartElement(MoneyWheelHelper.GameState); // Start GameState
        writer.WriteAttributeString(MoneyWheelHelper.UserId, gamePlay.State.User.UserId.ToString());
        writer.WriteAttributeString(MoneyWheelHelper.UserType, Convert.ToByte(gamePlay.State.User.UserType).ToString());
        writer.WriteAttributeString(MoneyWheelHelper.Balance, gamePlay.State.User.BalanceInCents.ToString());
        writer.WriteAttributeString(MoneyWheelHelper.Loyalty, gamePlay.State.User.LoyaltyBalance.ToString());
        writer.WriteAttributeString(MoneyWheelHelper.Currency, gamePlay.State.User.Currency.CurrencyId.ToString());
        writer.WriteAttributeString(MoneyWheelHelper.TransNumber, gamePlay.State.TransactionNumber.ToString());
        writer.WriteEndElement(); // End GameState

        writer.WriteStartElement(MoneyWheelHelper.Financials); // Start Financials
        gamePlay.EventData?.RequestedBets.ToXml(writer);
        writer.WriteStartElement(MoneyWheelHelper.TotalWin); // Start TotalWin
        writer.WriteString(TotalWin.ToString());
        writer.WriteEndElement(); // End TotalWin
        writer.WriteEndElement(); // End Financials

        writer.WriteStartElement(MoneyWheelHelper.Spins); // Start Spins
        foreach (var spin in Spins)
        {
          spin.ToXml(writer);
        }
        writer.WriteEndElement(); // End Spins
        writer.WriteEndElement(); // End MoneyWheel
      }

      return output.ToString().ParseXml();
    }

    /// <summary>
    /// Creates XML formatted representation of the current Game instance.
    /// </summary>
    /// <returns>XML formatted Game instance</returns>
    public XDocument ToGameStateXml()
    {
      var output = new StringBuilder();
      using (var writer = XmlWriter.Create(output, XmlUtil.DefaultWriterSettings))
      {
        writer.WriteStartElement(MoneyWheelHelper.MoneyWheel); // Start MoneyWheelSDK

        writer.WriteStartElement(MoneyWheelHelper.TotalWin); // Start TotalWin
        writer.WriteString(TotalWin.ToString());
        writer.WriteEndElement(); // End TotalWin

        Wheel.ToXml(writer);

        writer.WriteStartElement(MoneyWheelHelper.Spins); // Start Spins
        foreach (var spin in Spins)
        {
          spin.ToXml(writer);
        }
        writer.WriteEndElement(); // End Spins
        writer.WriteEndElement(); // End MoneyWheelSDK
      }

      return output.ToString().ParseXml();
    }

    /// <summary>
    /// Updates the Game instance with data from XML.
    /// </summary>
    /// <param name="previousEvent">Previous event.</param>
    /// <param name="gameStateXml">XML to create the Game instance from.</param>
    public void UpdateFromGameStateXml(MoneyWheelPlayEventData previousEvent, XElement gameStateXml)
    {
      UpdateBets(previousEvent.RequestedBets.Bets);

      var totalWin = long.Parse(gameStateXml.Element(MoneyWheelHelper.TotalWin)?.Value
                                ?? throw new VeyronException($"Couldn't deserialize '{MoneyWheelHelper.TotalWin}' from the game state"));
      var wheelXml = gameStateXml.Element(MoneyWheelHelper.Wheel);
      var spinsXml = gameStateXml.Element(MoneyWheelHelper.Spins)
                     ?? throw new VeyronException($"Couldn't deserialize '{MoneyWheelHelper.Spins}' from the game state");

      TotalWin = totalWin;
      Wheel.UpdateFromGameStateXml(wheelXml);
      foreach (var spinXml in spinsXml.Elements(MoneyWheelHelper.Spin))
      {
        Spins.Add(MoneyWheelSpin.FromGameStateXml(spinXml));
      }
    }

    /// <summary>
    /// Cleans up the instance.
    /// </summary>
    /// <param name="resetWheel">Wheel reset flag.</param>
    public void ToInitialState(bool resetWheel)
    {
      TotalWin = 0;
      _bets.Clear();

      if (resetWheel)
      {
        Wheel.ToInitialState();
      }
      Spins.Clear();
    }

    /// <summary>
    /// Plays the game.
    /// </summary>
    public void Play(List<MoneyWheelBet> bets, IRandom rng)
    {
      UpdateBets(bets);

      var currentMultiplier = 1;
      int symbolMultiplier;
      int winIndex;
      string winSymbol;

      do
      {
        var winPosition = Wheel.GetRandomPositionAndStoreOnlyOneIgnore(rng);
        winIndex = winPosition.Index;
        winSymbol = winPosition.Symbol;
        symbolMultiplier = winPosition.ResultingMultiplier;
        if (symbolMultiplier > 1)
        {
          Spins.Add(new MoneyWheelSpin(winIndex, winSymbol, currentMultiplier, 0));
          currentMultiplier *= symbolMultiplier;
        }
        else
        {
          var win = CalculateSpinWin(winPosition, currentMultiplier, bets);
          var newSpin = new MoneyWheelSpin(winIndex, winSymbol, currentMultiplier, win);
          Spins.Add(newSpin);
          TotalWin += win;
        }
      }
      while (symbolMultiplier > 1);

#if DEBUG
      // Lock to avoid issues while multi threading counting
      lock (_test)
      {
        CalculateStatsMultiplier(currentMultiplier);
        CalculateStatsIndexesSymbols(winIndex, winSymbol);
      }
#endif
    }

    private static object _test = new object();

    private void CalculateStatsMultiplier(int key)
    {
      // Update if we already have an existing key
      if (_statsData.MultiplierStatsCollection.ContainsKey(key))
      {
        _statsData.MultiplierStatsCollection[key].TimesLanded++;
      }
      else
      {
        // Create a key-value if we still don't have a key
        _statsData.MultiplierStatsCollection.Add(key, new MultiplierNumbers
        {
          TimesLanded = 1
        });
      }
    }

    private void CalculateStatsIndexesSymbols(int winIndex, string winSymbol)
    {
      // Update if we already have an existing key
      if (_statsData.IndexesSymbolsStatsCollection.ContainsKey(winIndex))
      {
        _statsData.IndexesSymbolsStatsCollection[winIndex].IndexTimesLanded++;
      }
      else
      {
        // Create a key-value if we still don't have a key
        _statsData.IndexesSymbolsStatsCollection.Add(winIndex, new IndexesSymbols()
        {
          Index = winIndex,
          IndexTimesLanded = 1,
          Symbol = winSymbol
        });
      }
    }

    /// <summary>
    /// Calculates the total wager.
    /// </summary>
    /// <returns>Sum of wagers for all bets.</returns>
    public long GetTotalWager()
    {
      var totalWager = 0;
      foreach (var bet in _bets)
      {
        totalWager += (bet.ChipSize * bet.NumChips);
      }

      return totalWager;
    }

    /// <summary>
    /// Calculates the win for the spin.
    /// </summary>
    /// <param name="winPosition"></param>
    /// <param name="currentMultiplier"></param>
    /// <param name="bets"></param>
    /// <returns></returns>
    private long CalculateSpinWin(MoneyWheelWheelPosition winPosition, int currentMultiplier, List<MoneyWheelBet> bets)
    {
      if (winPosition == null)
      {
        throw new VeyronException("Provided win position is null.");
      }

      var bet = bets.FirstOrDefault(b => b.Position == winPosition.Symbol);

      if (bet == null)
      {
        return 0;
      }

      // Formula required: wager * multiplier(s) * (payout - 1) + wager
      var betWager = bet.NumChips * bet.ChipSize;
      var winFormula = betWager * currentMultiplier * (winPosition.Payout - 1) + betWager;

      return winFormula;
    }

    private void UpdateBets(List<MoneyWheelBet> bets)
    {
      foreach (var bet in bets)
      {
        _bets.Add(new MoneyWheelBet(bet.Position, bet.ChipSize, bet.NumChips));
      }
    }
  }
}