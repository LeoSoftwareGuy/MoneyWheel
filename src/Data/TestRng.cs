using MGS.Random.Interfaces;
using System.Collections.Generic;

namespace MoneyWheel.Data
{
  /// <summary>
  /// RNG for testdata.
  /// </summary>
  public class TestRng : IRandom
  {
    private readonly List<ulong> _resultValues = new List<ulong>();
    private int _currentValueIndex = 0;

    #region IRandom

    /// <summary>
    /// Return Rng buffer in bytes.
    /// </summary>
    /// <param name="bufferLength">Length of the buffer.</param>
    public byte[] GetRngBytes(uint bufferLength)
    {
      return null;
    }

    /// <summary>
    /// Retrieve random value within provided range.
    /// </summary>
    /// <param name="minValue">Minimum value.</param>
    /// <param name="maxValue">Maximum value.</param>
    public ulong GetValue(ulong minValue, ulong maxValue)
    {
      if (_currentValueIndex == _resultValues.Count)
      {
        _currentValueIndex = 0;
      }

      return _resultValues[_currentValueIndex++];
    }

    #endregion

    /// <summary>
    /// Add result value to the list of results.
    /// </summary>
    public void AddResultValue(ulong resultValue)
    {
      _resultValues.Add(resultValue);
    }
  }
}
