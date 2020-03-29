using System;
using System.Collections.Generic;
using System.Text;

namespace Units
{
    public class GeodesicEllipsoidParams
    {
        /// <summary>
        /// Equatioral radios
        /// </summary>
        internal double a;

        /// <summary>
        /// flattening
        /// </summary>
        internal double f;

        /// <summary>
        /// Pre-Computed helper variables
        /// </summary>
        internal double f1;

        /// <summary>
        /// Pre-Computed helper variables
        /// </summary>
        internal double e2;

        /// <summary>
        /// Pre-Computed helper variables
        /// </summary>
        internal double ep2;

        /// <summary>
        /// Pre-Computed helper variables
        /// </summary>
        internal double n;

        /// <summary>
        /// Pre-Computed helper variables
        /// </summary>
        internal double b;

        /// <summary>
        /// Pre-Computed helper variables
        /// </summary>
        internal double c2;

        /// <summary>
        /// Pre-Computed helper variables
        /// </summary>
        internal double etol2;

        internal double[] A3x = new double[6];
        internal double[] C3x = new double[15];
        internal double[] C4x = new double[21];
    }
}
