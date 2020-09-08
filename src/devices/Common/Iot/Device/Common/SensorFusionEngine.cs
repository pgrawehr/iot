using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public sealed class SensorFusionEngine : IDisposable
    {
        private readonly MeasurementManager _manager;
        private readonly List<FusionOperation> _fusionOperations;
        private readonly object _lock;

        public SensorFusionEngine(MeasurementManager manager)
        {
            _manager = manager;
            _fusionOperations = new List<FusionOperation>();
            _lock = new object();
            _manager.AnyMeasurementChanged += ManagerOnAnyMeasurementChanged;
        }

        /// <summary>
        /// Registers a fusion operation. The <paramref name="operation"/> will be called whenever at least one of the inputs changed.
        /// </summary>
        /// <param name="arguments">List of input measurements this operation operates on</param>
        /// <param name="operation">The operation to perform. This gets the measurements in the same order as defined above.
        /// Should return a tuple containing the new value to assign to the result and a bool that can be set to false to skip assignment.
        /// This is different from returning null as the result, which may overwrite an existing value with void. Therefore the bool
        /// is only set to true if another operation/sensor might have set a valid value and no update is necessary</param>
        /// <param name="result">The measurement that is updated</param>
        public void RegisterFusionOperation(List<SensorMeasurement> arguments,
            Func<List<SensorMeasurement>, (IQuantity Value, bool useValue)> operation, SensorMeasurement result)
        {
            _manager.TryAddMeasurement(result); // Must be there, otherwise the result will be sent to the void usually
            foreach (var a in arguments)
            {
                _manager.TryAddMeasurement(a);
            }

            lock (_lock)
            {
                _fusionOperations.Add(new FusionOperation(arguments, operation, result));
            }
        }

        private void ManagerOnAnyMeasurementChanged(IList<SensorMeasurement> changedMeasurements)
        {
            lock (_lock)
            {
                foreach (var op in _fusionOperations)
                {
                    foreach (var measurement in changedMeasurements)
                    {
                        if (op.OnMeasurementChanges.Contains(measurement))
                        {
                            // Execute if anything matches, but only once (all values will hopefully be updated already)
                            ExecuteFusionOperation(op);
                            return;
                        }
                    }
                }
            }
        }

        private void ExecuteFusionOperation(FusionOperation op)
        {
            // We do not need to query the manager, since the SensorMeasurement instances within the operation already
            // contain the proper handle to the values we need
            var result = op.OperationToPerform(op.OnMeasurementChanges);
            if (result.Item2)
            {
                op.Result.UpdateValue(result.Item1);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _manager.AnyMeasurementChanged -= ManagerOnAnyMeasurementChanged;
            }
        }

        private sealed class FusionOperation
        {
            public FusionOperation(List<SensorMeasurement> onMeasurementChanges, Func<List<SensorMeasurement>, (IQuantity, bool)> operationToPerform, SensorMeasurement result)
            {
                OnMeasurementChanges = onMeasurementChanges;
                OperationToPerform = operationToPerform;
                Result = result;
            }

            public List<SensorMeasurement> OnMeasurementChanges { get; }
            public Func<List<SensorMeasurement>, (IQuantity, bool)> OperationToPerform { get; }
            public SensorMeasurement Result { get; }
        }
    }
}
