// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        /// <summary>
        /// Removes any entries older than the given age. Assumes the list is sorted from old to new
        /// </summary>
        /// <param name="dataSet">The dataset to modify</param>
        /// <param name="age">The age of the oldest elements to keep</param>
        /// <param name="minElementsRemaining">Minimum elements to keep (even if they are older than <paramref name="age"/>)</param>
        public static void RemoveOlderThan(this List<HistoricValue> dataSet, TimeSpan age, int minElementsRemaining = 0)
        {
            var now = DateTimeOffset.UtcNow;
            for (var index = 0; index < dataSet.Count - minElementsRemaining; index++)
            {
                var x = dataSet[index];
                if (now - x.MeasurementTime > age)
                {
                    dataSet.RemoveAt(index);
                    index--;
                }
            }
        }
    }
}
