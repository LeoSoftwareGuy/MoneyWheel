using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.Helpers;
using System.Xml;
using System.Xml.Linq;

namespace MoneyWheel.MoneyWheel
{
  /// <summary>
  /// MoneyWheelBet class.
  /// </summary>
  public class MoneyWheelBet
  {
    /// <summary>
    /// Default constructor.
    /// </summary>
    public MoneyWheelBet() { }

    /// <summary>
    /// Creates new instance with provided parameters.
    /// </summary>
    /// <param name="position">Represents the position on the wheel.</param>
    /// <param name="chipSize">Represents the size of the chips.</param>
    /// <param name="numChips">Represents the number of chips.</param>
    public MoneyWheelBet(string position, int chipSize, int numChips)
    {
      Position = position;
      ChipSize = chipSize;
      NumChips = numChips;
    }

    /// <summary>
    /// Creates new instance with provided parameters.
    /// </summary>
    /// <param name="betXml">XElement to create an instance.</param>
    public MoneyWheelBet(XElement betXml)
    {
      if (betXml == null)
      {
        throw new VeyronException("Couldn't create Bet object as provided bet XElement is null.");
      }

      Position = betXml.Attribute(MoneyWheelHelper.position)?.Value
                 ?? throw new VeyronException("Couldn't create Bet object as provided bet position in config file is null.");
      ChipSize = int.Parse(betXml.Attribute(MoneyWheelHelper.ChipSize)?.Value
                 ?? throw new VeyronException("Couldn't create Bet object as provided bet chipSize in config file is null."));
      NumChips = int.Parse(betXml.Attribute(MoneyWheelHelper.NumChips)?.Value
                 ?? throw new VeyronException("Couldn't create Bet object as provided bet numChips in config file is null."));
    }

    /// <summary>
    /// Writes data to xml.
    /// </summary>
    /// <param name="writer">XmlWriter used for writing.</param>
    public void ToXml(XmlWriter writer)
    {
      if (writer == null)
      {
        throw new VeyronException("Cannot serialize bet as XmlWriter object is null.");
      }

      writer.WriteStartElement(MoneyWheelHelper.Bet); // Start Bet
      writer.WriteAttributeString(MoneyWheelHelper.position, Position);
      writer.WriteAttributeString(MoneyWheelHelper.ChipSize, ChipSize.ToString());
      writer.WriteAttributeString(MoneyWheelHelper.NumChips, NumChips.ToString());
      writer.WriteEndElement(); // End Bet
    }

    /// <summary>
    /// Position the bet is made on.
    /// </summary>
    public string Position { get; } = string.Empty;

    /// <summary>
    /// The size of the chips.
    /// </summary>
    public int ChipSize { get; set; }

    /// <summary>
    /// The number of chips.
    /// </summary>
    public int NumChips { get; set; }
  }
}