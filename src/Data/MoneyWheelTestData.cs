using MGS.Casino.Veyron.Interfaces;
using MGS.Random.Interfaces;
using MoneyWheel.Helpers;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MoneyWheel.Data
{
  // Converts test XML data into a simple class that will provide a configured RNG based on the test data.
  internal class MoneyWheelTestData
  {
    private readonly MoneyWheelGamePlay _game;
    private readonly List<IRandom> _testRngs = new List<IRandom>();
    private int _currentRng;

    // Constructor that will be called to initialise test data.
    public MoneyWheelTestData(MoneyWheelGamePlay game, XDocument testDataXml, IRandom defaultRng, int wheelPositionsCount)
    {
      _game = game ?? throw new ArgumentNullException(nameof(_game), "Valid Game object must be provided.");

      if (testDataXml != null)
      {
        var resultSets = testDataXml.Descendants().Elements(MoneyWheelHelper.Results);

        foreach (var set in resultSets)
        {
          var testRng = new TestRng();

          var results = set.Elements(MoneyWheelHelper.Result);

          // Parse the test data to get the results.
          foreach (var result in results)
          {
            if (result.Attribute(MoneyWheelHelper.ResultIndex) == null)
            {
              throw new VeyronException($"Attribute '{MoneyWheelHelper.ResultIndex}' not found in testdata '{MoneyWheelHelper.Result}' node.");
            }

            var resultIndex = Convert.ToInt32(result.Attribute(MoneyWheelHelper.ResultIndex)?.Value
                                              ?? throw new VeyronException(527, "Invalid test setup."));
            if (resultIndex < 0 || resultIndex > wheelPositionsCount - 1)
            {
              throw new VeyronException(527, "Invalid test setup.");
            }

            testRng.AddResultValue((ulong)resultIndex);
          }

          _testRngs.Add(testRng);
        }
      }
      else
      {
        throw new VeyronException(527, "Invalid test setup.");
      }

      // Fall back on the default (real) RNG.
      if (_testRngs.Count == 0)
      {
        _testRngs.Add(defaultRng);
      }
    }

    // Cycles the list of test random number generators.
    public IRandom GetNextTestRng()
    {
      if (_testRngs.Count == 0)
      {
        return null;
      }

      if (_currentRng == _testRngs.Count)
      {
        _currentRng = 0;
      }

      return _testRngs[_currentRng++];
    }
  }
}