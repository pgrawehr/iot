using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisplayControl;
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
	}
}
