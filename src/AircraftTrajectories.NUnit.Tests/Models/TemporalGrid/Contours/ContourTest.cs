﻿using NUnit.Framework;
using System.Collections.Generic;

namespace AircraftTrajectories.Models.Contours.Tests
{
    [TestFixture()]
    public class ContourTest
    {
        [Test()]
        public void Contour()
        {
            double[][] data = new double[3][];

            IEnumerable<ContourPoint>[][] hgrid = new IEnumerable<ContourPoint>[data.Length][];
            IEnumerable<ContourPoint>[][] vgrid = new IEnumerable<ContourPoint>[data.Length - 1][];

            Contour con = new Contours.Contour(new ContourPoint(), vgrid, hgrid, true);
            Assert.IsNotNull(con);
            Assert.AreEqual(true, con.IsClosed);
            Assert.AreEqual(0, con.Value);
            Assert.AreEqual(0, con.Points[0].Value);
        }
    }
}