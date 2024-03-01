using System.Diagnostics.CodeAnalysis;

namespace MoneyWheel.Enums
{
  /// <summary>
  /// MoneyWheelPacketType class
  /// </summary>
  [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "Zero is reserved for Ping.")]
  public enum MoneyWheelPacketType : short
  {
    /// <summary>
    /// RefreshRequest.
    /// </summary>
    RefreshRequest = 2048,

    /// <summary>
    /// RefreshResponse.
    /// </summary>
    RefreshResponse = 2049,

    /// <summary>
    /// PlayRequest.
    /// </summary>
    PlayRequest = 2050,

    /// <summary>
    /// PlayResponse.
    /// </summary>
    PlayResponse = 2051,
  }
}