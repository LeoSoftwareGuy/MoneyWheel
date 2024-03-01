using MGS.Casino.Veyron.Interfaces;
using MGS.Random.Interfaces;
using MGS.RNG;
using MoneyWheel.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace MoneyWheel.MoneyWheel
{
  /// <summary>
  /// The wheel.
  /// </summary>
  public class MoneyWheelWheel
  {
    /// <summary>
    /// Creates new instance with provided parameters.
    /// </summary>
    /// <param name="configuration">Game configuration.</param>
    public MoneyWheelWheel(MoneyWheelConfiguration configuration)
    {
      Positions = DuplicateWheelPositions(configuration.WheelPositions);
      SortPositions();
    }

    /// <summary>
    /// Generates defaultWheelIndex used for first refresh only.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="VeyronException"></exception>
    public int GetDefaultPositionIndex(MoneyWheelConfiguration configuration)
    {
      int defaultIndex = 0;
      Random rnd = new Random();

      var noMultiplierIndexes = configuration.WheelPositions.Where(p => p.ResultingMultiplier == 1).Select(p => p.Index).ToList();

      if (configuration.DefaultWheelIndex == "random")
      {
        // All available indexes except ones with multipliers
        defaultIndex = rnd.Next(0, noMultiplierIndexes.Count - 1);
      }
      else
      {
        // Sanity check
        if (int.TryParse(configuration.DefaultWheelIndex, out defaultIndex) == false)
        {
          throw new VeyronException("DefaultWheelIndex value is not set properly in XML.");
        }
      }
      return defaultIndex;
    }

    /// <summary>
    /// All positions on the wheel.
    /// </summary>
    public List<MoneyWheelWheelPosition> Positions { get; private set; } = new List<MoneyWheelWheelPosition>();

    /// <summary>
    /// Copies the wheel positions list.
    /// </summary>
    /// <param name="positionsToDuplicate">List of wheel positions to be duplicated.</param>
    /// <returns>The copy of the provided list of wheel positions.</returns>
    private static List<MoneyWheelWheelPosition> DuplicateWheelPositions(List<MoneyWheelWheelPosition> positionsToDuplicate)
    {
      var duplicatedPositions = new List<MoneyWheelWheelPosition>();
      foreach (var wheelPosition in positionsToDuplicate)
      {
        duplicatedPositions.Add(wheelPosition.Duplicate());
      }

      return duplicatedPositions;
    }

    /// <summary>
    /// Resets the wheel object to its initial state.
    /// </summary>
    public void ToInitialState()
    {
      foreach (var position in Positions)
      {
        position.ToIgnore = false;
      }
    }

    /// <summary>
    /// Restores data from hte game state xml.
    /// </summary>
    /// <param name="wheelXml"></param>
    /// <returns></returns>
    public void UpdateFromGameStateXml(XElement wheelXml)
    {
      if (wheelXml == null)
      {
        throw new VeyronException($"Couldn't update {this.GetType()} as provided XElement is null.");
      }

      foreach (var positionXml in wheelXml.Elements(MoneyWheelHelper.Position))
      {
        var positionToUpdate = Positions.FirstOrDefault(i => i.Index == int.Parse(positionXml.Attribute(MoneyWheelHelper.Index).Value));

        if (positionToUpdate != null)
        {
          bool toIgnore = bool.Parse(positionXml.Attribute(MoneyWheelHelper.ToIgnore).Value);
          if (positionToUpdate.ToIgnore != toIgnore)
          {
            positionToUpdate.ToIgnore = toIgnore;
          }
        }
        else
        {
          throw new VeyronException($"Couldn't update {this.GetType()} from the GameState.");
        }
      }
    }

    /// <summary>
    /// Writes data to xml.
    /// Used to serialize the game state.
    /// </summary>
    /// <param name="writer">XmlWriter used for writing.</param>
    public void ToXml(XmlWriter writer)
    {
      if (writer == null)
      {
        throw new VeyronException($"Cannot serialize {this.GetType()} as XmlWriter object is null.");
      }

      writer.WriteStartElement(MoneyWheelHelper.Wheel); // Start Wheel

      foreach (var position in Positions)
      {
        position.ToGameStateXml(writer);
      }

      writer.WriteEndElement(); // End Wheel
    }

    /// <summary>
    /// Gets random position on the wheel.
    /// </summary>
    /// <returns>Random position.</returns>
    public MoneyWheelWheelPosition GetRandomPosition(IRandom rng)
    {
      // Gets random index on the wheel
      var randomPosition1 = Positions[(int)rng.GetValue(0, (ulong)Positions.Count - 1)];

      // If the position is used
      if (randomPosition1.ToIgnore)
      {
        // Gets the list of unused positions with the same symbol.
        var unusedPositions = Positions.Where(p => !p.ToIgnore && p.Symbol == randomPosition1.Symbol).ToList();

        //if there are no more unused positions with the same symbol.
        if (!unusedPositions.Any())
        {
          // Marks them all as unused.
          MarkPositionsAsNotIgnored(randomPosition1.Symbol);

          // Marks randomPosition1 as used.
          randomPosition1.ToIgnore = true;
          // returns it.
          return randomPosition1;
        }
        // If there are some, then
        // Gets the random one from them
        var rng2 = new MgsRng();
        rng2.Initialise();
        var randomPosition2 = unusedPositions[(int)rng2.GetValue(0, (ulong)unusedPositions.Count - 1)];
        // Marks it as used
        randomPosition2.ToIgnore = true;
        // And returns it
        return randomPosition2;
      }

      // If the position is unused and there are multiple such symbols on the wheel
      // (all except 40, x2 and x7, as there is only 1 instance of each on the wheel).
      if (Positions.Count(p => p.Symbol == randomPosition1.Symbol) > 1)
      {
        // Marks it as used
        randomPosition1.ToIgnore = true;
      }

      // returns it
      return randomPosition1;
    }

    /// <summary>
    /// Gets random position on the wheel and ignores only the last landed position for each symbol.
    /// </summary>
    /// <returns>Random position.</returns>
    public MoneyWheelWheelPosition GetRandomPositionAndStoreOnlyOneIgnore(IRandom rng)
    {
      MoneyWheelWheelPosition positionToReturn = null;

      // Gets random index on the wheel
      var randomPosition1 = Positions[(int)rng.GetValue(0, (ulong)Positions.Count - 1)];
      var sameSymbolPositions = Positions.Where(p => p.Symbol == randomPosition1.Symbol).ToList();

      // If the position is unused
      if (!randomPosition1.ToIgnore)
      {
        // If there are multiple such symbols on the wheel (all except 40, x2 and x7, as there is only 1 instance of each on the wheel).
        if (sameSymbolPositions.Count > 1)
        {
          // Cancels ignorance for all positions with the same symbol. Ideally should be only one, but just in case gets the list.
          sameSymbolPositions.Where(p => p.ToIgnore)
            .ToList()
            .ForEach(
              p =>
              {
                p.ToIgnore = false;
              });
          // Marks the position we randomly choose as used
          randomPosition1.ToIgnore = true;
        }

        positionToReturn = randomPosition1;
      }
      else // If the position is used
      {
        // Gets the list of unused positions with the same symbol.
        var unusedPositions = sameSymbolPositions.Where(p => !p.ToIgnore).ToList();
        // if there are no unused positions with the same symbol, throw an exception.
        if (!unusedPositions.Any())
        {
          throw new VeyronException("Problem determining unused positions.");
        }

        // Gets random position from them.
        var rng2 = new MgsRng();
        rng2.Initialise();
        var randomNumber = (int)rng2.GetValue(0, (ulong)unusedPositions.Count - 1);
        var randomPosition2 = unusedPositions[randomNumber];
        //var randomPosition2 = unusedPositions[(int)rng.GetValue(0, (ulong)unusedPositions.Count - 1)];

        // Marks randomPosition1 as unused and randomPosition2 as used.
        randomPosition1.ToIgnore = false;
        randomPosition2.ToIgnore = true;
        positionToReturn = randomPosition2;
      }

      return positionToReturn;
    }

    /// <summary>
    /// Marks positions with provided symbol as unused.
    /// </summary>
    /// <param name="symbol">Symbol to find positions by.</param>
    private void MarkPositionsAsNotIgnored(string symbol)
    {
      foreach (var position in Positions.Where(p => p.Symbol == symbol))
      {
        position.ToIgnore = false;
      }
    }

    /// <summary>
    /// Sorts wheel positions ascending by 'index'
    /// </summary>
    private void SortPositions()
    {
      Positions = Positions.OrderBy(p => p.Index).ToList();
    }
  }
}