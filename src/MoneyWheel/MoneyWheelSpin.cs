using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.Helpers;
using System.Xml;
using System.Xml.Linq;

namespace MoneyWheel.MoneyWheel
{
  /// <summary>
  /// Represents the spin
  /// </summary>
  public class MoneyWheelSpin
  {
    /// <summary>
    /// Default constructor.
    /// </summary>
    public MoneyWheelSpin() { }

    /// <summary>
    /// Creates new instance with provided parameters.
    /// </summary>
    /// <param name="winIndex">Winning index on the wheel.</param>
    /// <param name="winSymbol">Winning symbol on the wheel.</param>
    /// <param name="multiplier">Multiplier for the current spin.</param>
    /// <param name="win">Win for the current spin.</param>
    public MoneyWheelSpin(int winIndex, string winSymbol, int multiplier, long win)
    {
      WinIndex = winIndex;
      WinSymbol = winSymbol;
      Multiplier = multiplier;
      Win = win;
    }

    /// <summary>
    /// Creates new instance with provided parameters.
    /// </summary>
    /// <param name="spinXml">XElement to create an instance.</param>
    public static MoneyWheelSpin FromGameStateXml(XElement spinXml)
    {
      if (spinXml == null)
      {
        throw new VeyronException($"Couldn't create {typeof(MoneyWheelSpin)} as provided XElement is null.");
      }

      var spinToReturn = new MoneyWheelSpin
      {
        WinIndex = int.Parse(spinXml.Attribute(MoneyWheelHelper.WinIndex)?.Value
                             ?? throw new VeyronException($"Couldn't create {typeof(MoneyWheelSpin)} as provided winIndex is null.")),
        WinSymbol = spinXml.Attribute(MoneyWheelHelper.WinSymbol)?.Value
                    ?? throw new VeyronException($"Couldn't create {typeof(MoneyWheelSpin)} as provided winSymbol is null."),
        Multiplier = int.Parse(spinXml.Attribute(MoneyWheelHelper.Multiplier)?.Value
                               ?? throw new VeyronException($"Couldn't create {typeof(MoneyWheelSpin)} as provided multiplier is null.")),
        Win = long.Parse(spinXml.Attribute(MoneyWheelHelper.Win)?.Value
                         ?? throw new VeyronException($"Couldn't create {typeof(MoneyWheelSpin)} as provided win is null."))
      };

      return spinToReturn;
    }

    /// <summary>
    /// Winning index on the wheel.
    /// </summary>
    public int WinIndex { get; set; }

    /// <summary>
    /// Winning symbol on the wheel.
    /// </summary>
    public string WinSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Multiplier for the current spin.
    /// </summary>
    public int Multiplier { get; set; } = 1;

    /// <summary>
    /// Win for the current spin.
    /// </summary>
    public long Win { get; set; }

    /// <summary>
    /// Writes bet data to xml.
    /// </summary>
    /// <param name="writer">XmlWriter used for writing.</param>
    public void ToXml(XmlWriter writer)
    {
      if (writer == null)
      {
        throw new VeyronException($"Cannot serialize {this.GetType()} as XmlWriter object is null.");
      }

      writer.WriteStartElement(MoneyWheelHelper.Spin); // Start Spin
      writer.WriteAttributeString(MoneyWheelHelper.WinIndex, WinIndex.ToString());
      writer.WriteAttributeString(MoneyWheelHelper.WinSymbol, WinSymbol);
      writer.WriteAttributeString(MoneyWheelHelper.Multiplier, Multiplier.ToString());
      writer.WriteAttributeString(MoneyWheelHelper.Win, Win.ToString());
      writer.WriteEndElement(); // End Spin
    }
  }
}