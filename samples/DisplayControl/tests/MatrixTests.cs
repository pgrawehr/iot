using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisplayControl;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace DisplayController.Tests
{
    public class MatrixTests
    {
        [Fact]
        public void NoRotationIsIdentity()
        {
            var matrix = Matrix4D.ObjectRotationHeadingPitchRoll(0, 0, 0);
            var identity = Matrix<double>.Build.DiagonalIdentity(4);
            Assert.Equal(matrix, identity);
        }

        [Fact]
        public void RotatesAroundZ()
        {
            var matrix = Matrix4D.ObjectRotationHeadingPitchRoll(90, 0, 0);
            var expected = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { 0, 1, 0, 0 },
                { -1, 0, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            });

            Assert.True(matrix.AlmostEqual(expected, 1E-10));

            Vector<double> x = Vector<double>.Build.DenseOfArray(new double[] { 1, 0, 0, 1 });
            var rotation = matrix * x;
            Vector<double> resultVector = Vector<double>.Build.DenseOfArray(new double[] { 0, -1, 0, 1 });
            Assert.True(resultVector.AlmostEqual(rotation, 1E-10));
        }
    }
}
