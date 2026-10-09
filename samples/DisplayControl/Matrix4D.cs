using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using static System.Math;

namespace DisplayControl
{
    public static class Matrix4D
    {
        public static double DegreesToRadians(double degrees)
        {
            return degrees / 180 * PI;
        }
        public static Matrix<double> ObjectRotationHeadingPitchRoll(
            double heading, double pitch, double roll)
        {
            // Original implementation, seemingly working
            Matrix<double> rotationMatrix = Matrix4D.RotationMatrixX(DegreesToRadians(roll)); // roll
            rotationMatrix *= Matrix4D.RotationMatrixY(DegreesToRadians(pitch)); // pitch
            rotationMatrix *= Matrix4D.RotationMatrixZ(DegreesToRadians(heading)); // heading

            return rotationMatrix;
        }

        public static Matrix<double> RotationMatrixZ(double angle)
        {
            var matrix = Matrix<double>.Build.DenseIdentity(4);
            matrix[0, 0] = Cos(angle);
            matrix[1, 1] = Cos(angle);
            matrix[0, 1] = Sin(angle);
            matrix[1, 0] = -Sin(angle);
            return matrix;
        }

        public static Matrix<double> RotationMatrixX(double angle)
        {
            var matrix = Matrix<double>.Build.DenseIdentity(4);
            matrix[1, 1] = Cos(angle);
            matrix[2, 2] = Cos(angle);
            matrix[1, 2] = Sin(angle);
            matrix[2, 1] = -Sin(angle);
            return matrix;
        }

        public static Matrix<double> RotationMatrixY(double angle)
        {
            var matrix = Matrix<double>.Build.DenseIdentity(4);
            matrix[0, 0] = Cos(angle);
            matrix[2, 2] = Cos(angle);
            matrix[2, 0] = Sin(angle);
            matrix[0, 2] = -Sin(angle);
            return matrix;
        }

        public static double TranslationMatrix(int x, int y, double z)
        {
            throw new NotImplementedException();
        }
    }
}
