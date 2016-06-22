﻿using AircraftTrajectories.Models.Space3D;
using MathNet.Numerics.Interpolation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AircraftTrajectories.Models.Trajectory
{
    class TrajectoryGenerator
    {
        protected Aircraft _aircraft;
        protected ReferencePoint _referencePoint;
        protected List<double> _xData;
        protected List<double> _yData;
        protected List<double> _zData;
        protected List<double> _tData;
        protected List<double> _latData;
        protected List<double> _longData;
        protected MetricToGeographic _converter;

        public TrajectoryGenerator(Aircraft aircraft, ReferencePoint referencePoint)
        {
            _aircraft = aircraft;
            _referencePoint = referencePoint;
            _converter = new MetricToGeographic(_referencePoint);

            _xData = new List<double>();
            _yData = new List<double>();
            _zData = new List<double>();
            _tData = new List<double>();
            _latData = new List<double>();
            _longData = new List<double>();
        }

        public void AddDatapoint(double x, double y, double z)
        {
            AddDatapoint(x, y, z, _tData.Count);
        }

        public void AddDatapoint(double x, double y, double z, double t)
        {
            _xData.Add(x);
            _yData.Add(y);
            _zData.Add(z);
            _tData.Add(t);
            var geoPoint = _converter.ConvertToLongLat(x, y);

            _longData.Add(geoPoint.Longitude);
            _latData.Add(geoPoint.Latitude);
        }

        public Trajectory GenerateTrajectory()
        {
            var xSpline = CubicSpline.InterpolateNatural(_tData, _xData);
            var ySpline = CubicSpline.InterpolateNatural(_tData, _yData);
            var zSpline = CubicSpline.InterpolateNatural(_tData, _zData);
            var latSpline = CubicSpline.InterpolateNatural(_tData, _latData);
            var longSpline = CubicSpline.InterpolateNatural(_tData, _longData);

            var trajectory = new Trajectory(xSpline, ySpline, zSpline, longSpline, latSpline, _aircraft);
            trajectory.LowerLeftPoint = new Point3D(_xData.Min(), _yData.Min(), 0, CoordinateUnit.metric);
            trajectory.UpperRightPoint = new Point3D(_xData.Max(), _yData.Max(), 0, CoordinateUnit.metric);
            trajectory.ReferencePoint = _referencePoint;
            trajectory.Duration = (int) _tData.Last();

            return trajectory;
        }
    }
}