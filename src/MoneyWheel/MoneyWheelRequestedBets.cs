using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.Helpers;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace MoneyWheel.MoneyWheel
{
  /// <summary>
  /// MoneyWheelRequestedBets class.
  /// </summary>
  public class MoneyWheelRequestedBets
  {
    /// <summary>
    /// Default constructor.
    /// </summary>
    public MoneyWheelRequestedBets() { }

    /// <summary>
    /// Creates new instance with provided parameters.
    /// </summary>
    /// <param name="requestedBetsXml">XElement to create an instance.</param>
    public MoneyWheelRequestedBets(XElement requestedBetsXml)
    {
      if (requestedBetsXml == null)
      {
        throw new VeyronException($"Couldn't create {typeof(MoneyWheelRequestedBets)} object as provided bets XElement is null.");
      }

      foreach (var betXml in requestedBetsXml.Elements(MoneyWheelHelper.Bet))
      {
        Bets.Add(new MoneyWheelBet(betXml));
      }
    }

    /// <summary>
    /// Writes data to xml.
    /// </summary>
    /// <param name="writer">XmlWriter used for writing.</param>
    public void ToXml(XmlWriter writer)
    {
      if (writer == null)
      {
        throw new VeyronException("Cannot serialize PlayRequest as XmlWriter object is null.");
      }

      writer.WriteStartElement(MoneyWheelHelper.Bets); // Start Bets
      foreach (var bet in Bets)
      {
        bet.ToXml(writer);
      }
      writer.WriteEndElement(); // End Bets
    }

    /// <summary>
    /// List of bets made.
    /// </summary>
    public List<MoneyWheelBet> Bets { get; } = new List<MoneyWheelBet>();
  }
}