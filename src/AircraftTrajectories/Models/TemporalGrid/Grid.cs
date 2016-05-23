﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AircraftTrajectories.Models.TemporalGrid
{
    using Contours;

    /// <summary>
    /// Class that represents a two-dimensional dataset
    /// </summary>
    class Grid
    {
        public IEnumerable<Contour> Contours { get; protected set; }
        public double[][] Data { get; set; }

        /// <summary>
        /// Construct a Grid
        /// </summary>
        /// <param name="data"></param>
        public Grid(double[][] data)
        {
            Data = data;
            CalculateContours();
        }

        protected void CalculateContours()
        {
            IEnumerable<ContourPoint>[][] hgrid, vgrid;
            Contours = Contour.CreateContours(Data, out hgrid, out vgrid).ToArray();
        }
    }
}