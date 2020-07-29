using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace DisplayControl
{
    public class EngineData
    {
        public EngineData(int engineNo, RotationalSpeed revolutions, Ratio pitch, TimeSpan operatingTime)
        {
            EngineNo = engineNo;
            Revolutions = revolutions;
            Pitch = pitch;
            OperatingTime = operatingTime;
        }

        public int EngineNo
        {
            get;
        }

        public RotationalSpeed Revolutions
        {
            get;
        }

        public Ratio Pitch
        {
            get;
        }

        public TimeSpan OperatingTime
        {
            get;
        }
    }
}
