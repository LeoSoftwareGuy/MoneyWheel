using MGS.Casino.Veyron.Interfaces;
using MoneyWheel.MoneyWheel;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MoneyWheel.Helpers
{
  /// <summary>
  /// Configuration class
  /// </summary>
  public class MoneyWheelConfiguration
  {
    /// <summary>
    /// Creates new instance with provided parameters.
    /// </summary>
    /// <param name="configurationFile">Configuration file path.</param>
    public MoneyWheelConfiguration(string configurationFile)
    {
      // Loads the config file.
      var config = XDocument.Load(configurationFile);

      // Initializes local variables.
      var versionInfoXml = config.Root?.Element(MoneyWheelHelper.VersionInfo);
      var serviceInfoXml = config.Root?.Element(MoneyWheelHelper.ServiceInfo);
      var wheelXml = config.Root?.Element(MoneyWheelHelper.Wheel);

      // Checks local variables.
      if (versionInfoXml == null) { throw new VeyronException($"Couldn't read node 'VersionInfo' from the configuration file: {configurationFile}"); }
      if (serviceInfoXml == null) { throw new VeyronException($"Couldn't read node 'ServiceInfo' from the configuration file: {configurationFile}"); }
      if (wheelXml == null) { throw new VeyronException($"Couldn't read node 'Wheel' from the configuration file: {configurationFile}"); }

      // Initializes global variables.
      Version = versionInfoXml.Attribute(MoneyWheelHelper.version)?.Value
                ?? throw new VeyronException($"Couldn't read attribute 'version' from the configuration file: {configurationFile}");
      ModuleId = serviceInfoXml.Attribute(MoneyWheelHelper.moduleId)?.Value
                 ?? throw new VeyronException($"Couldn't read attribute 'moduleId' from the configuration file: {configurationFile}");
      GameName = serviceInfoXml.Attribute(MoneyWheelHelper.gameName)?.Value
                 ?? throw new VeyronException($"Couldn't read attribute 'gameName' from the configuration file: {configurationFile}");
      DefaultWheelIndex = wheelXml.Attribute(MoneyWheelHelper.defaultWheelIndex)?.Value
                                    ?? throw new VeyronException($"Couldn't read attribute 'defaultWheelIndex' from the configuration file: {configurationFile}");
      WheelPositions = new List<MoneyWheelWheelPosition>();
      foreach (var positionXml in wheelXml.Elements(MoneyWheelHelper.Position))
      {
        WheelPositions.Add(new MoneyWheelWheelPosition(positionXml));
      }
    }

    /// <summary>
    /// Version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// ModuleId.
    /// </summary>
    public string ModuleId { get; }

    /// <summary>
    /// GameName.
    /// </summary>
    public string GameName { get; }

    /// <summary>
    /// List of wheel positions.
    /// </summary>
    public List<MoneyWheelWheelPosition> WheelPositions { get; }

    /// <summary>
    /// DefaultWheelIndex
    /// </summary>
    public string DefaultWheelIndex { get; set; }
  }
}
