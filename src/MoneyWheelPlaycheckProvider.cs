using MGS.Casino.Veyron.Base.Plugin;
using MGS.Casino.Veyron.Base.Utils;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MoneyWheel
{
  /// <summary>
  /// Contains the logic to create Playcheck XML data from events and corresponding game state XML.
  /// </summary>
  public sealed class MoneyWheelPlaycheckProvider : BaseVeyronPlaycheck<MoneyWheelGamePlay>
  {
    /// <summary>
    /// Required default constructor.
    /// </summary>
    public MoneyWheelPlaycheckProvider() : base()
    {
    }

    /// <inheritdoc />
    public override void Initialise()
    {
      // TODO: Optional initialise code here to clear any game state before generating Playcheck information.
    }

    /// <inheritdoc />
    public override XDocument GenerateInXmlFromEvent(BaseGameEventData eventData)
    {
      // Example:
      var xml = new StringBuilder();
      using (XmlWriter writer = XmlWriter.Create(xml, XmlUtil.DefaultWriterSettings))
      {
        // TODO: Optional writing of Playcheck specific XML for events here. Remove this whole method if not required or call the base class.
        var baseImplementationXml = base.GenerateInXmlFromEvent(eventData);
        if (baseImplementationXml != null)
        {
          xml.Append(baseImplementationXml.ToString());
        }
      }

      return xml.ToString().ParseXml();
    }

    /// <inheritdoc />
    public override XDocument GenerateOutXmlFromState()
    {
      // Example:
      var xml = new StringBuilder();
      using (XmlWriter writer = XmlWriter.Create(xml, XmlUtil.DefaultWriterSettings))
      {
        // TODO: Optional writing of Playcheck specific XML for game state here. Remove this whole method if not required or call the base class
        var baseImplementationXml = base.GenerateOutXmlFromState();
        if (baseImplementationXml != null)
        {
          xml.Append(baseImplementationXml.ToString());
        }
      }

      return xml.ToString().ParseXml();
    }
  }
}