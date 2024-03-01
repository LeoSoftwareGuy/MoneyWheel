using MGS.Casino.Veyron.Base.Plugin;
using MGS.Casino.Veyron.Interfaces;
using MGS.Random.Interfaces;
using MoneyWheel.Handlers;
using MoneyWheel.Helpers;
using MoneyWheel.MoneyWheel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MoneyWheel.Data
{
  // Converts strategy XML data into a simple class that will return event data based on the strategy.
  internal class MoneyWheelStrategyData
  {
    private readonly MoneyWheelGamePlay _game;
    private readonly List<BaseGameEventData> _strategyEvents = new List<BaseGameEventData>();
    private int _currentStrategy = 0;
    private bool IsRandom;

    // Constructor that will be called to initialise strategy data.
    public MoneyWheelStrategyData(MoneyWheelGamePlay game, XDocument strategyDataXml)
    {
      _game = game ?? throw new ArgumentNullException(nameof(_game), "Valid Game object must be provided.");

      if (strategyDataXml != null)
      {
        var strategyXml = strategyDataXml.Element(MoneyWheelHelper.Strategy);
        switch (strategyXml.Attribute(MoneyWheelHelper.type).Value)
        {
          case MoneyWheelHelper.Simple:
            var allActions = strategyDataXml.Descendants(MoneyWheelHelper.Action);
            foreach (var action in allActions)
            {
              var playEvent = new MoneyWheelPlayEventData();

              foreach (var bet in action.Descendants(MoneyWheelHelper.Bet))
              {
                if (bet.Attribute(MoneyWheelHelper.position) == null)
                {
                  throw new ArgumentOutOfRangeException($"Attribute '{MoneyWheelHelper.position}' not found in strategy 'Action/Bet' node.");
                }
                var position = bet.Attribute(MoneyWheelHelper.position)?.Value;

                if (bet.Attribute(MoneyWheelHelper.ChipSize) == null)
                {
                  throw new ArgumentOutOfRangeException($"Attribute '{MoneyWheelHelper.ChipSize}' not found in strategy 'Action/Bet' node.");
                }
                var chipSize = Convert.ToInt32(bet.Attribute(MoneyWheelHelper.ChipSize)?.Value);

                if (bet.Attribute(MoneyWheelHelper.NumChips) == null)
                {
                  throw new ArgumentOutOfRangeException($"Attribute '{MoneyWheelHelper.NumChips}' not found in strategy 'Action/Bet' node.");
                }
                var numChips = Convert.ToInt32(bet.Attribute(MoneyWheelHelper.NumChips)?.Value);

                playEvent.RequestedBets.Bets.Add(new MoneyWheelBet(position, chipSize, numChips));
              }

              playEvent.EventHandler = new MoneyWheelXmlPlayHandler(game);
              _strategyEvents.Add(playEvent);
            }

            break;

          case MoneyWheelHelper.Random:
            IsRandom = true;

            break;

          default:
            throw new VeyronException($"Unsupported strategy action type: {strategyXml.Attribute(MoneyWheelHelper.type).Value}");
        }
      }
      else
      {
        throw new VeyronException("Invalid strategy data.");
      }
    }

    private BaseGameEventData CreateRandomPlayEvent(IRandom defaultRng)
    {
      var playEvent = new MoneyWheelPlayEventData();

      // StartsWith("x") is for multipliers. There should be no bets made on these.
      var availableBetPositions = _game.Game.Wheel.Positions.Where(p => !p.Symbol.StartsWith("x")).Select(p => p.Symbol).Distinct().ToList();
      var randomBetPositionIndex = (int)defaultRng.GetValue(0, Convert.ToUInt64(availableBetPositions.Count - 1));

      var validChipSizes = _game.Settings.Get<string>(MoneyWheelHelper.ValidChipSizes).Split(',');
      var randomChipSize = int.Parse(validChipSizes[Convert.ToInt32(defaultRng.GetValue(0, Convert.ToUInt64(validChipSizes.Length - 1)))]);
      
      var numChips = 1;

      playEvent.RequestedBets.Bets.Add(new MoneyWheelBet(availableBetPositions[randomBetPositionIndex], randomChipSize, numChips));

      playEvent.EventHandler = new MoneyWheelXmlPlayHandler(_game);

      return playEvent;
    }
    // Cycles the list of strategies returning event data.
    public BaseGameEventData GetNextStrategy(IRandom defaultRng)
    {
      if (IsRandom)
      {
        return CreateRandomPlayEvent(defaultRng);
      }

      if (_strategyEvents.Count == 0)
      {
        return null;
      }

      if (_currentStrategy == _strategyEvents.Count)
      {
        _currentStrategy = 0;
      }

      return _strategyEvents[_currentStrategy++];
    }
  }
}