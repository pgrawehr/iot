using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnitsNet;

namespace Iot.Device.Common
{
    /// <summary>
    /// Contains operations on history data sets
    /// </summary>
    public static class HistoryOperations
    {
        /// <summary>
        /// Converts absolute timestamps in the history data set to ages (time ago)
        /// </summary>
        /// <param name="dataSet">The input data set</param>
        /// <param name="now">The current time (or the last time of the data set)</param>
        /// <returns></returns>
        public static List<(TimeSpan Age, IQuantity Value)> ConvertToAges(this List<HistoricValue> dataSet, DateTime now)
        {
            var ret = new List<(TimeSpan, IQuantity)>();
            foreach (var e in dataSet)
            {
                ret.Add((now - e.MeasurementTime, e.Value));
            }

            return ret;
        }

        /// <summary>
        /// Returns the maximum value within the sequence.
        /// </summary>
        /// <param name="dataSet">The data set to use</param>
        /// <returns></returns>
        public static IQuantity MaxValue(this List<HistoricValue> dataSet)
        {
            var referenceType = dataSet.First().Value;
            return dataSet.Max(x => x.Value, referenceType.Unit);
        }

        /// <summary>
        /// Returns the minimum value within the sequence.
        /// </summary>
        /// <param name="dataSet">The data set to use</param>
        /// <returns></returns>
        public static IQuantity MinValue(this List<HistoricValue> dataSet)
        {
            var referenceType = dataSet.First().Value;
            return dataSet.Min(x => x.Value, referenceType.Unit);
        }

        /// <summary>
        /// Returns the average value within the sequence.
        /// </summary>
        /// <param name="dataSet">The data set to use</param>
        /// <returns></returns>
        public static IQuantity AverageValue(this List<HistoricValue> dataSet)
        {
            var referenceType = dataSet.First().Value;
            return dataSet.Average(x => x.Value, referenceType.Unit);
        }
    }
}
