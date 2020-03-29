using System;
using System.Collections.Generic;
using System.Text;

namespace Units
{
    /// <summary>
    /// The struct containing information about a single geodesic.  This must be
    /// initialized by geod_lineinit(), geod_directline(), geod_gendirectline(),
    /// or geod_inverseline() before use.
    /// </summary>
    public class GeodesicLine
    {
        public double lat1;

        /**< the starting latitude */
        public double lon1;

        /**< the starting longitude */
        public double azi1;

        /**< the starting azimuth */
        public double a;

        /**< the equatorial radius */
        public double f;

        /**< the flattening */
        public double salp1;

        /**< sine of \e azi1 */
        public double calp1;

        /**< cosine of \e azi1 */
        public double a13;

        /**< arc length to reference point */
        public double s13;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double b;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double c2;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double f1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double salp0;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double calp0;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double k2;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double ssig1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double csig1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double dn1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double stau1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double ctau1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double somg1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double comg1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double A1m1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double A2m1;

        /**< distance to reference point */
        /**< @cond SKIP */
        public double A3c;

        /**< distance to reference point */
        public double B11;

        /**< distance to reference point */
        public double B21;

        /**< distance to reference point */
        public double B31;

        /**< distance to reference point */
        public double A4;

        /**< distance to reference point */
        public double B41;

        public double[] C1a = new double[6 + 1];
        public double[] C1pa = new double[6 + 1];
        public double[] C2a = new double[6 + 1];
        public double[] C3a = new double[6];
        public double[] C4a = new double[6];

        public GeodeticMask caps;

    }
}
