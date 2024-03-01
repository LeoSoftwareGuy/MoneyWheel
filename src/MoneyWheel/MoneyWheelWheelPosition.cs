using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.Helpers;
using System.Xml;
using System.Xml.Linq;

namespace MoneyWheel.MoneyWheel
{
  /// <summary>
  /// MoneyWheelWheelPosition class.
  /// </summary>
  public class MoneyWheelWheelPosition
  {
    private MoneyWheelWheelPosition() { }

    /// <summary>
    /// Creates a WheelPosition basing on xml data from configuration file.
    /// </summary>
    /// <param name="positionXml">Xml formatted wheel position.</param>
    public MoneyWheelWheelPosition(XElement positionXml)
    {
      if (positionXml == null)
      {
        throw new VeyronException($"Couldn't create {this.GetType()} as provided position XElement is null.");
      }

      Index = int.Parse(positionXml.Attribute(MoneyWheelHelper.Index)?.Value
                        ?? throw new VeyronException($"Couldn't create {this.GetType()} as {MoneyWheelHelper.Index} is null in provided XElement."));
      Symbol = positionXml.Attribute(MoneyWheelHelper.Symbol)?.Value
               ?? throw new VeyronException($"Couldn't create {this.GetType()} as {MoneyWheelHelper.Symbol} is null in provided XElement.");
      Payout = int.Parse(positionXml.Attribute(MoneyWheelHelper.Payout)?.Value
                         ?? throw new VeyronException($"Couldn't create {this.GetType()} object as {MoneyWheelHelper.Payout} is null in provided XElement."));
      ResultingMultiplier = int.Parse(positionXml.Attribute(MoneyWheelHelper.ResultingMultiplier)?.Value
                                      ?? throw new VeyronException($"Couldn't create {this.GetType()} object as {MoneyWheelHelper.ResultingMultiplier} is null in provided XElement."));
      ToIgnore = false;
    }

    /// <summary>
    /// Index.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// Symbol.
    /// </summary>
    public string Symbol { get; private set; }

    /// <summary>
    /// Payout.
    /// </summary>
    public int Payout { get; private set; }

    /// <summary>
    /// ResultingMultiplier.
    /// </summary>
    public int ResultingMultiplier { get; private set; }

    /// <summary>
    /// Shows if this position was used and should be ignored on the current spin.
    /// </summary>
    public bool ToIgnore { get; set; }

    /// <summary>
    /// Writes data to game state xml.
    /// </summary>
    /// <param name="writer">XmlWriter used for writing.</param>
    public void ToGameStateXml(XmlWriter writer)
    {
      if (writer == null)
      {
        throw new VeyronException($"Cannot serialize {this.GetType()} as XmlWriter object is null.");
      }

      writer.WriteStartElement(MoneyWheelHelper.Position); // Start Position
      writer.WriteAttributeString(MoneyWheelHelper.Index, Index.ToString());
      writer.WriteAttributeString(MoneyWheelHelper.ToIgnore, ToIgnore.ToString());
      writer.WriteEndElement(); // End Position
    }

    /// <summary>
    /// Copies the wheel position.
    /// </summary>
    /// <returns>The copy of the current wheel position.</returns>
    public MoneyWheelWheelPosition Duplicate()
    {
      return new MoneyWheelWheelPosition
      {
        Index = Index,
        Symbol = Symbol,
        Payout = Payout,
        ResultingMultiplier = ResultingMultiplier,
        ToIgnore = ToIgnore
      };
    }
  }
}