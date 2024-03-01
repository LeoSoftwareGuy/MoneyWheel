using MGS.Casino.Veyron.Base.Plugin;
using MGS.Casino.Veyron.Base.Utils;
using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.Helpers;
using MoneyWheel.MoneyWheel;
using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MoneyWheel.Data
{
  /// <summary>
  /// MoneyWheelPlayEventData class.
  /// </summary>
  [Serializable]
  public class MoneyWheelPlayEventData : BaseGameEventData
  {
    /// <summary>
    /// Default constructor.
    /// </summary>
    public MoneyWheelPlayEventData() : base() { }

    /// <summary>
    /// Requested bets.
    /// </summary>
    public MoneyWheelRequestedBets RequestedBets { get; private set; } = new MoneyWheelRequestedBets();

    /// <inheritdoc />
    public override XDocument ToXml()
    {
      var xml = new StringBuilder();
      using (var writer = XmlWriter.Create(xml, XmlUtil.DefaultWriterSettings))
      {
        writer.WriteStartElement(MoneyWheelHelper.Request); // Start Request
        RequestedBets.ToXml(writer);
        writer.WriteEndElement(); // End Request
      }

      return XmlUtil.ParseXml(xml.ToString());
    }

    /// <inheritdoc />
    public override void FromXml(XDocument eventXml)
    {
      var betsXml = eventXml.Descendants(MoneyWheelHelper.Request).First().Element(MoneyWheelHelper.Bets)
                    ?? throw new VeyronException($"Could not deserialize {this.GetType()}.");

      RequestedBets = new MoneyWheelRequestedBets(betsXml);
    }
  }
}