using MGS.Casino.Veyron.Base.Plugin.Data;
using MGS.Casino.Veyron.Base.Plugin.Handlers;
using MGS.Casino.Veyron.Base.Utils;
using MGS.Casino.Veyron.Base.Utils.Data;
using MoneyWheel.Data;
using MoneyWheel.Helpers;
using System;
using System.Xml.Linq;

namespace MoneyWheel.Handlers
{
  /// <summary>
  /// MoneyWheelXmlPlayHandler class.
  /// </summary>
  [Serializable]
  public class MoneyWheelXmlPlayHandler : XmlGameEventHandler<MoneyWheelPlayEventData>
  {
    private const short Version = 6;

    /// <summary>
    /// Game play instance.
    /// </summary>
    protected MoneyWheelGamePlay Game { get; private set; }

    /// <summary>
    /// Constructor that takes the game play logic in case you need access to internal game information.
    /// </summary>
    /// <param name="game"></param>
    public MoneyWheelXmlPlayHandler(MoneyWheelGamePlay game)
    {
      if (game == null)
      {
        throw new ArgumentNullException(MoneyWheelHelper.game);
      }

      Game = game;
    }

    ///<inheritdoc />
    public override XDocument EncodeXmlResponse(GameState state, GameSettings settings)
    {
      var xml = Game.Game.ToResponseXml(Game, false);
      return PacketUtil.CreateXmlResponsePacket(xml.ToString(), MoneyWheelHelper.Play, Version).ParseXml();
    }

    ///<inheritdoc />
    public override MoneyWheelPlayEventData DecodeXmlRequest(XDocument xml)
    {
      if (xml.GetVersion() == Version && xml.GetVerb().Equals(MoneyWheelHelper.Play, StringComparison.OrdinalIgnoreCase))
      {
        var playData = new MoneyWheelPlayEventData();
        playData.FromXml(xml);

        return playData;
      }

      // Cannot deal with this xml, pass on to another handler.
      return null;
    }
  }
}