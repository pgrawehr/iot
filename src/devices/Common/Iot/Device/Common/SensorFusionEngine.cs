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
        private readonly List<HistoryOperation> _historyOperations;
        private readonly object _lock;
        private bool _operationsInProgress;

        public SensorFusionEngine(MeasurementManager manager)
        {
            _manager = manager;
            _fusionOperations = new List<FusionOperation>();
            _historyOperations = new List<HistoryOperation>();
            _lock = new object();
            _operationsInProgress = false;
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
        /// <param name="minWaitBetweenUpdates">Waits at least this amount between calls to the fusion operation.
        /// Used to throttle updates</param>
        public void RegisterFusionOperation(IList<SensorMeasurement> arguments,
            Func<IList<SensorMeasurement>, (IQuantity? Value, bool UseValue)> operation, SensorMeasurement result,
            TimeSpan minWaitBetweenUpdates)
        {
            _manager.TryAddMeasurement(result); // Must be there, otherwise the result will be sent to the void usually
            foreach (var a in arguments)
            {
                _manager.TryAddMeasurement(a);
            }

            lock (_lock)
            {
                _fusionOperations.Add(new FusionOperation(arguments, operation, result, minWaitBetweenUpdates));
            }
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
        public void RegisterFusionOperation(IList<SensorMeasurement> arguments,
            Func<IList<SensorMeasurement>, (IQuantity? Value, bool UseValue)> operation, SensorMeasurement result)
        {
            RegisterFusionOperation(arguments, operation, result, TimeSpan.Zero);
        }

        /// <summary>
        /// Perform an operation on a measurement history (i.e. calculate a smoothed measurement).
        /// History settings for the given input measurement need to be configured already.
        /// </summary>
        public void RegisterHistoryOperation(SensorMeasurement measurement,
            Func<SensorMeasurement, MeasurementManager, IQuantity> operation, SensorMeasurement result)
        {
            _manager.TryAddMeasurement(result);
            _manager.TryAddMeasurement(measurement);
            lock (_lock)
            {
                _historyOperations.Add(new HistoryOperation(measurement, operation, result));
            }
        }

        private void ManagerOnAnyMeasurementChanged(IList<SensorMeasurement> changedMeasurements)
        {
            lock (_lock)
            {
                // Break stack overflow error that happens if two registered operations interfere with each other
                // TODO: Improve
                if (_operationsInProgress)
                {
                    return;
                }

                _operationsInProgress = true;
                foreach (var op in _fusionOperations)
                {
                    foreach (var measurement in changedMeasurements)
                    {
                        if (op.OnMeasurementChanges.Contains(measurement))
                        {
                            // Execute if anything matches, but only once (all values will hopefully be updated already)
                            ExecuteFusionOperation(op);
                            break;
                        }
                    }
                }

                foreach (var op in _historyOperations)
                {
                    if (changedMeasurements.Contains(op.Input))
                    {
                        IQuantity result = op.Operation(op.Input, _manager);
                        op.Output.UpdateValue(result);
                    }
                }

                _operationsInProgress = false;
            }
        }

        private void ExecuteFusionOperation(FusionOperation op)
        {
            var now = DateTime.Now;
            if (now - op.LastUpdate < op.MinWaitBetweenUpdates)
            {
                return;
            }

            op.LastUpdate = now;
            // We do not need to query the manager, since the SensorMeasurement instances within the operation already
            // contain the proper handle to the values we need
            var result = op.OperationToPerform(op.OnMeasurementChanges);
            if (!result.UseValue)
            {
                // Mark all results of the fusion engine as indirect
                op.Result.UpdateValue(result.Value, SensorMeasurementStatus.IndirectResult);
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
            public FusionOperation(IList<SensorMeasurement> onMeasurementChanges,
                Func<IList<SensorMeasurement>, (IQuantity? Value, bool UseValue)> operationToPerform, SensorMeasurement result,
                TimeSpan minWaitBetweenUpdates)
            {
                OnMeasurementChanges = onMeasurementChanges;
                OperationToPerform = operationToPerform;
                Result = result;
                MinWaitBetweenUpdates = minWaitBetweenUpdates;
                LastUpdate = DateTime.MinValue;
            }

            public IList<SensorMeasurement> OnMeasurementChanges { get; }
            public Func<IList<SensorMeasurement>, (IQuantity? Value, bool UseValue)> OperationToPerform { get; }
            public SensorMeasurement Result { get; }
            public TimeSpan MinWaitBetweenUpdates { get; }

            public DateTime LastUpdate
            {
                get;
                set;
            }
        }

        private sealed class HistoryOperation
        {
            public HistoryOperation(SensorMeasurement input,
                Func<SensorMeasurement, MeasurementManager, IQuantity> operation, SensorMeasurement output)
            {
                Input = input;
                Operation = operation;
                Output = output;
            }

            public SensorMeasurement Input { get; }
            public Func<SensorMeasurement, MeasurementManager, IQuantity> Operation { get; }
            public SensorMeasurement Output { get; }

        }
    }
}
