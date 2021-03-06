﻿using System.Collections.Generic;
using System.Linq;

namespace AircraftTrajectories.Models.TemporalGrid
{
    using Contours;
    using Space3D;
    using System;
    using System.Drawing;

    /// <summary>
    /// Class that represents a two-dimensional dataset
    /// </summary>
    public class Grid
    {
        public IEnumerable<Contour> Contours { get; protected set; }
        public double[][] Data { get; set; }
        public Point3D LowerLeftCorner { get; set; }
        protected ReferencePoint _referencePoint;
        public ReferencePoint ReferencePoint
        {
            get
            {
                return _referencePoint;
            }
            set
            {
                _referencePoint = value;
                Converter = new MetricToGeographic(_referencePoint);
            }
        }
        public MetricToGeographic Converter { get; set; }
        public int CellSize { get; set; }

        /// <summary>
        /// Construct a Grid
        /// </summary>
        /// <param name="data"></param>
        public Grid(double[][] data, Point3D lowerLeftCorner, int cellSize, bool calculateContours = true)
        {
            LowerLeftCorner = lowerLeftCorner;
            CellSize = cellSize;
            Data = data;
            Converter = new MetricToGeographic(ReferencePoint);
            ReferencePoint = new ReferencePointRD();

            if (calculateContours)
            {
                CalculateContours();
            }
        }

        /// <summary>
        /// Calculates the contours within a vertical and horizontal grid
        /// </summary>
        protected void CalculateContours()
        {
            IEnumerable<ContourPoint>[][] hgrid, vgrid;
            Contours = Contour.CreateContours(Data, out hgrid, out vgrid).ToArray();
        }

        /// <summary>
        /// Create a grid of the specified size filled with zeros
        /// </summary>
        /// <param name="height">the number of vertical cells</param>
        /// <param name="width">the number of horizontal cells</param>
        /// <returns></returns>
        public static Grid CreateEmptyGrid(int height, int width, Point3D lowerLeftCorner, ReferencePoint referencePoint, int cellSize)
        {
            double[][] data = new double[width][];
            for (int x=0; x<width; x++)
            {
                double[] column = new double[height];
                for (int y=0; y<height; y++)
                {
                    column[y] = 0;
                }
                data[x] = column;
            }

            Grid newGrid = new Grid(data, lowerLeftCorner, cellSize, false);
            newGrid.ReferencePoint = referencePoint;
            return newGrid;
        }

        /// <summary>
        /// Retuns GeoPoint based on the entered x y index of the grid
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public GeoPoint3D GridGeoCoordinate(double gridX, double gridY)
        {
            return Converter.ConvertToLongLat(LowerLeftCorner.X + (gridX * CellSize), LowerLeftCorner.Y + (gridY * CellSize));
        }

        /// <summary>
        /// Retuns a Point3D based on the entered x y index of the grid
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public Point3D GridCoordinate(double gridX, double gridY)
        {
            return new Point3D(LowerLeftCorner.X + (gridX * CellSize), LowerLeftCorner.Y + (gridY * CellSize));
        }

        /// <summary>
        /// Returns a x y index of the grid based on the entered corner coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Point CoordinateToGridIndex(double x, double y)
        {
            int gridX = (int)Math.Floor((decimal)(x - LowerLeftCorner.X) / CellSize);
            int gridY = (int)Math.Floor((decimal)(y - LowerLeftCorner.Y) / CellSize);
            return new Point(gridX, gridY);
        }

        public static Grid MapOnLargerGrid(Grid input, Point3D lowerLeftPoint, Point3D upperRightPoint)
        {
            int width = (int) Math.Ceiling((upperRightPoint.X - lowerLeftPoint.X) / input.CellSize);
            int height = (int) Math.Ceiling((upperRightPoint.Y - lowerLeftPoint.Y) / input.CellSize);

            Grid upscaled = CreateEmptyGrid(height, width, lowerLeftPoint, input.ReferencePoint, input.CellSize);

            for (int x = 0; x < input.Data.Length; x++)
            {
                for (int y = 0; y < input.Data[0].Length; y++)
                {
                    double value = input.Data[x][y];
                    Point3D gridPoint = input.GridCoordinate(x, y);
                    Point upscaledIndexes = upscaled.CoordinateToGridIndex(gridPoint.X, gridPoint.Y);
                    upscaled.Data[upscaledIndexes.X][upscaledIndexes.Y] = value;
                }
            }

            upscaled.CalculateContours();
            return upscaled;
        }
    }
}