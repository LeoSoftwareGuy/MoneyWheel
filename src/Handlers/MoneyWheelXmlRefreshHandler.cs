using MGS.Casino.Veyron.Base.Plugin.Data;
using MGS.Casino.Veyron.Base.Plugin.Handlers;
using MGS.Casino.Veyron.Base.Plugin.Packets;
using MGS.Casino.Veyron.Base.Utils;
using MGS.Casino.Veyron.Base.Utils.Data;
using MoneyWheel.Helpers;
using System;
using System.Xml.Linq;

namespace MoneyWheel.Handlers
{
  /// <summary>
  /// MoneyWheelXmlRefreshHandler class.
  /// </summary>
  [Serializable]
  public class MoneyWheelXmlRefreshHandler : XmlGameEventHandler<RefreshEventData>
  {
    private const short Version = 6;

    /// <inheritdoc />
    public override bool IsReadOnly
    {
      get { return true; }
    }

    /// <summary>
    /// Variable to store the game play object this handler was created from, use this to access internal game specific data.
    /// </summary>
    protected MoneyWheelGamePlay Game { get; private set; }

    /// <summary>
    /// Constructor that takes the game play logic in case you need access to internal game information.
    /// </summary>
    /// <param name="game"></param>
    public MoneyWheelXmlRefreshHandler(MoneyWheelGamePlay game)
    {
      if (game == null)
      {
        throw new ArgumentNullException(MoneyWheelHelper.game);
      }

      Game = game;
    }

    /// <summary>
    /// Method to encode the XML response packet for your game.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public override XDocument EncodeXmlResponse(GameState state, GameSettings settings)
    {
      var xml = Game.Game.ToResponseXml(Game, true);
      return PacketUtil.CreateXmlResponsePacket(xml.ToString(), MoneyWheelHelper.Refresh, Version).ParseXml();
    }

    /// <summary>
    /// Method for decoding the refresh data. Refreshes usually have no data so you can leave it using the default classes.
    /// </summary>
    /// <param name="xml"></param>
    /// <returns></returns>
    public override RefreshEventData DecodeXmlRequest(XDocument xml)
    {
      // Since multiple XML packet requests can come through we need to check if this handler can decode the packet otherwise return null.
      // Doing a version and verb check is usually sufficient here.
      // Example:
      if (xml.GetVersion() == Version && xml.GetVerb().Equals(MoneyWheelHelper.Refresh, StringComparison.OrdinalIgnoreCase))
      {
        return new RefreshEventData();
      }

      // Cannot deal with this xml, pass on to another handler.
      return null;
    }
  }
}