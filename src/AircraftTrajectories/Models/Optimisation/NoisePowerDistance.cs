﻿using AircraftTrajectories.Models.Space3D;
using AircraftTrajectories.Models.TemporalGrid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AircraftTrajectories.Models.Optimisation
{
    /// <summary>
    /// A singleton class that interpolates noise values based on the NPD database
    /// </summary>
    public class NoisePowerDistance
    {
        private static NoisePowerDistance instance;
        protected Dictionary<string, List<double[]>> _INMData;

        /// <summary>
        /// Create a NPD object
        /// </summary>
        private NoisePowerDistance()
        {
            readINMData();
        }
        
        /// <summary>
        /// Read and process the NPD database
        /// </summary>
        protected void readINMData()
        {
            string[][] rawData = ReadRawData();
            _INMData = new Dictionary<string, List<double[]>>();
            var currentOperatingMode = "";
            var currentKey = "";
            var engineData = new List<double[]>();
            foreach (string[] row in rawData)
            {
                // Check whether we switch from operating mode
                if (row[2] != currentOperatingMode)
                {
                    // Confirm that this is not the first iteration (i.e. engineData is filled)
                    if (currentOperatingMode != "")
                    {
                        _INMData.Add(currentKey, engineData);
                    }
                    // Reset engineData and currentOperatingMode for the next iterations
                    engineData = new List<double[]>();
                    currentOperatingMode = row[2];
                    currentKey = row[0] + row[1] + row[2];
                }

                // Construct a double array containing for a number of distances the uncorrected noise value
                double[] engineThrustNoiseData = new double[11];
                for (int c = 3; c < row.Length-1; c++)
                {
                    engineThrustNoiseData[c-3] = double.Parse(row[c]);
                }
                // Add noise noise values for the curent thrust setting to engineData
                engineData.Add(engineThrustNoiseData);
            }
        }

        /// <summary>
        /// Read the raw NPD database from disk
        /// </summary>
        /// <returns></returns>
        protected string[][] ReadRawData()
        {
            string rawData = File.ReadAllText(Globals.currentDirectory + "INM_data.dat");
            string[][] INMData = rawData
                .Split('\n')
                .Skip(2)
                .Select(q =>
                    q.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(Convert.ToString)
                     .ToArray()
                )
                .ToArray();
            return INMData;
        }

        /// <summary>
        /// Return the singleton instance
        /// </summary>
        public static NoisePowerDistance Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NoisePowerDistance();
                }
                return instance;
            }
        }

        public Grid NoiseMaxGrid;        
        public void CalculateNoise(Point3D aircraftPosition, double thrust)
        {
            int cellSize = 250;
            Console.WriteLine("ToDo: CellSize NPD currently hardcoded to 250");
            List<double[]> _engineData;
            _INMData.TryGetValue("GP7270"+'M'+'D', out _engineData);
            var distances = new List<double>() { 200, 400, 630, 1000, 2000, 4000, 6300, 10000, 16000, 25000 };
            var logDistances = new List<double>() { 2.301, 2.602, 2.799, 3.0, 3.301, 3.602, 3.799, 4.0, 4.204, 4.398 };

            double _x = aircraftPosition.X;
            double _y = aircraftPosition.Y;
            double _z = aircraftPosition.Z;
            int PIndex1, PIndex2, DIndex1, DIndex2;
            double D1, D2, dx, dy, D;
            double[][] data = new double[200][];
            for (int x = 0; x < 200 * cellSize; x = x + cellSize)
            {
                double[] col = new double[200];
                for (int y = 0; y < 200 * cellSize; y = y + cellSize)
                {
                    dx = (x - _x);
                    dy = (y - _y);
                    D = Math.Log10(Math.Sqrt(dx * dx + dy * dy + _z * _z));

                    // GET NOISE VALUE
                    PIndex1 = 0;
                    PIndex2 = 1;
                    for (int i = 1; i < _engineData.Count - 1; i++)
                    {
                        if (thrust >= _engineData[i][0])
                        {
                            PIndex1 = i;
                            PIndex2 = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    DIndex1 = 1;
                    DIndex2 = 2;
                    D1 = logDistances[0];
                    D2 = logDistances[1];
                    for (int i = 1; i < logDistances.Count - 1; i++)
                    {
                        if (D >= logDistances[i])
                        {
                            DIndex1 = i + 1;
                            DIndex2 = i + 2;
                            D1 = logDistances[i];
                            D2 = logDistances[i + 1];
                        }
                        else
                        {
                            break;
                        }
                    }

                    double P1 = _engineData[PIndex1][0];
                    double P2 = _engineData[PIndex2][0];
                    double LP1D1 = _engineData[PIndex1][DIndex1];
                    double LP1D2 = _engineData[PIndex1][DIndex2];
                    double LP2D1 = _engineData[PIndex2][DIndex1];
                    double LP2D2 = _engineData[PIndex2][DIndex2];

                    var LP1D = LP1D1 + (((LP1D2 - LP1D1) * (D - D1)) / (D2 - D1));
                    var LP2D = LP2D1 + (((LP2D2 - LP2D1) * (D - D1)) / (D2 - D1));
                    var noise = LP1D + (((LP2D - LP1D) * (thrust - P1)) / (P2 - P1));
                    //col[y / cellSize] = noise;
                    col[y / cellSize] = (NoiseMaxGrid == null || (x == 0 && y == 0)) ? noise : Math.Max(noise, NoiseMaxGrid.Data[x / cellSize][y / cellSize]);
                }
                data[x / cellSize] = col;
            }
            Grid grid = new Grid(data, new Point3D(0, 0, 0, CoordinateUnit.metric), cellSize, false);
            NoiseMaxGrid = grid;
        }
    }
}
