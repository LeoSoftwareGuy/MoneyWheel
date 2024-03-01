using MGS.Casino.Veyron.Base.Utils.Data;
using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.MoneyWheel;
using System.Collections.Generic;
using System.Linq;

namespace MoneyWheel.Helpers
{
  /// <summary>
  /// Helper class to perform different of validations for the game.
  /// </summary>
  public static class Validator
  {
    /// <summary>
    /// Method for bet validation.
    /// </summary>
    /// <param name="settings">Game settings.</param>
    /// /// <param name="game">Game.</param>
    /// <param name="bets">Event data bets to be validated.</param>
    public static void ValidateBets(GameSettings settings, MoneyWheelGame game, List<MoneyWheelBet> bets)
    {
      if (bets.Count == 0)
      {
        throw new VeyronException("No bets made.");
      }

      // Checks if no more than 1 bet made per position
      var uniqueBetsCount = bets.GroupBy(b => b.Position).ToList().Count;
      if (uniqueBetsCount != bets.Count)
      {
        throw new VeyronException($"Incorrect bets made.");
      }

      var correctBetPositions = game.Wheel.Positions.Where(p => !(p.Symbol.StartsWith("x"))).ToList();
      var validChipSizes = settings.Get<string>(MoneyWheelHelper.ValidChipSizes)
         .Split(',')
         .Select(int.Parse)
         .ToList();
      var totalBet = 0;

      // Loops all bets
      foreach (var betPosition in bets)
      {
        // Checks if the bet made on correct value
        if (!correctBetPositions.Any(p => p.Symbol == betPosition.Position))
        {
          throw new VeyronException($"The bet on position {betPosition.Position} is invalid. There is no such valid bet position on the wheel.");
        }

        // Checks num chips
        if(betPosition.NumChips <= 0)
        {
          throw new VeyronException($"The bet on position {betPosition.Position} is invalid. Value {betPosition.NumChips} is not a valid chip amount.");
        }

        // Checks the chip size
        if (!validChipSizes.Contains(betPosition.ChipSize))
        {
          throw new VeyronException(512, $"The bet on position {betPosition.Position} is invalid. Value {betPosition.ChipSize} is not a valid chip size.");
        }

        // Checks MinBet and MaxBet values.
        var bet = betPosition.ChipSize * betPosition.NumChips;
        totalBet += bet;

        // MinBet
        if (bet < settings.Get<int>(MoneyWheelHelper.minBet))
        {
          throw new VeyronException(510, $"The bet on position {betPosition.Position} is invalid. Value {bet} is smaller than {MoneyWheelHelper.minBet} value.");
        }
        // MaxBet
        if (bet > settings.Get<int>(MoneyWheelHelper.maxBet))
        {
          throw new VeyronException(502, $"The bet on position {betPosition.Position} is invalid. Value {bet} is larger than {MoneyWheelHelper.maxBet} value.");
        }

        // Checks the MinIncrement value.
        if (bet % settings.Get<int>(MoneyWheelHelper.MinIncrement) != 0)
        {
          throw new VeyronException($"The bet on position {betPosition.Position} is invalid. {MoneyWheelHelper.MinIncrement} is not respected.");
        }
      }

      // Checks MinTableBet and MaxTableBet values
      var minTableBet = settings.Get<int>(MoneyWheelHelper.MinTableBet, true);
      var maxTableBet = settings.Get<int>(MoneyWheelHelper.MaxTableBet, true);

      if (totalBet < minTableBet || totalBet > maxTableBet)
      {
        throw new VeyronException($"The total wager is invalid. Value {totalBet} is smaller than {MoneyWheelHelper.MinTableBet} value or larger than {MoneyWheelHelper.MaxTableBet} value.");
      }
    }
  }
}