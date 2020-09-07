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

        public void RegisterFusionOperation(List<SensorMeasurement> arguments,
            Func<List<SensorMeasurement>, IQuantity> operation, SensorMeasurement result)
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

        private void ManagerOnAnyMeasurementChanged(SensorMeasurement changedMeasurement)
        {
            lock (_lock)
            {
                foreach (var op in _fusionOperations)
                {
                    if (op.OnMeasurementChanges.Contains(changedMeasurement))
                    {
                        ExecuteFusionOperation(op);
                    }
                }
            }
        }

        private void ExecuteFusionOperation(FusionOperation op)
        {
            // We do not need to query the manager, since the SensorMeasurement instances within the operation already
            // contain the proper handle to the values we need
            var result = op.OperationToPerform(op.OnMeasurementChanges);
            op.Result.UpdateValue(result);
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
            public FusionOperation(List<SensorMeasurement> onMeasurementChanges, Func<List<SensorMeasurement>, IQuantity> operationToPerform, SensorMeasurement result)
            {
                OnMeasurementChanges = onMeasurementChanges;
                OperationToPerform = operationToPerform;
                Result = result;
            }

            public List<SensorMeasurement> OnMeasurementChanges { get; }
            public Func<List<SensorMeasurement>, IQuantity> OperationToPerform { get; }
            public SensorMeasurement Result { get; }
        }
    }
}
