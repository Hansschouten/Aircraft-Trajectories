﻿using AircraftTrajectories.Models.IntegratedNoiseModel;
using AircraftTrajectories.Models.Optimisation;
using AircraftTrajectories.Models.Population;
using AircraftTrajectories.Models.Space3D;
using AircraftTrajectories.Models.TemporalGrid;
using AircraftTrajectories.Models.Trajectory;
using AircraftTrajectories.Models.Visualisation;
using AircraftTrajectories.Models.Visualisation.KML;
using AircraftTrajectories.Models.Visualisation.KML.AnimationSections;
using AircraftTrajectories.Models.Visualisation.KML.AnimationSections.Cameras;
using AircraftTrajectories.Views.Visualisation;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AircraftTrajectories.Presenters
{
    public class VisualisationPresenter
    {
        protected IVisualisationForm _view;
        
        Thread thread;
        DateTime startTime;

        public VisualisationPresenter(IVisualisationForm view)
        {
            _view = view;
            _view.CalculateNoise += delegate (object sender, EventArgs e) { CalculateNoise(); };
            _view.CancelNoiseCalculation += delegate (object sender, EventArgs e) { Cancel(); };
            _view.PrepareVisualisation += delegate (object sender, EventArgs e) { PrepareVisualisation(); };
            _view.CancelVisualisationPreparation += delegate (object sender, EventArgs e) { Cancel(); };
            _view.VisualiseTrajectoryEvent += delegate (object sender, EventArgs e) { VisualiseTrajectory(); };
        }

        protected void Cancel()
        {
            if (thread != null)
            {
                thread.Abort();
            }
        }

        Trajectory trajectory;
        List<Trajectory> trajectories;
        IntegratedNoiseModel noiseModel;
        TemporalGrid temporalGrid;
        ReferencePoint referencePoint;

        #region "Calculate Noise"

        public void CalculateNoise()
        {
            startTime = DateTime.Now;
            referencePoint = new ReferencePointRD();
            if (_view.CustomReference)
            {
                referencePoint = new ReferencePoint(_view.GeoReference, _view.MetricReference);
            }

            if (!_view.ExternalNoise)
            {
                if (_view.OneTrajectory)
                {
                    thread = new Thread(OneTrajectoryINM);
                } else
                {
                    thread = new Thread(MultipleTrajectoriesINM);
                }
                thread.Start();
            }
        }
        
        protected void OneTrajectoryINM()
        {
            var aircraft = new Aircraft("GP7270", "wing");
            var trajectoryGenerator = new TrajectoryGenerator(aircraft, referencePoint);

            if (_view.TrajectoryFile.Contains("."))
            {
                var reader = new TrajectoryFileReader(CoordinateUnit.metric, trajectoryGenerator);
                trajectory = reader.CreateTrajectoryFromFile(_view.TrajectoryFile);
            }

            trajectory.Aircraft = aircraft;
            noiseModel = new IntegratedNoiseModel(trajectory, aircraft);
            bool integrateToCurrentPosition = true;
            int metric = 0;
            switch (_view.NoiseMetric)
            {
                case 0:
                    metric = 1;
                    integrateToCurrentPosition = false;
                    break;
                case 1:
                    metric = 1;
                    break;
                case 2:
                    metric = 0;
                    break;
                case 3:
                    metric = 2;
                    break;
                case 4:
                    metric = 3;
                    break;
            }
            noiseModel.IntegrateToCurrentPosition = integrateToCurrentPosition;
            noiseModel.NoiseMetric = metric;

            noiseModel.StartCalculation(ProgressChanged);
            temporalGrid = noiseModel.TemporalGrid;
            _view.Invoke(delegate { _view.NoiseCalculationCompleted(); });
        }

        bool VisualiseOptimisation = false;
        protected void MultipleTrajectoriesINM()
        {
            var reader = new TrajectoriesFileReader();
            /*
            if (TrajectoryFitness.trajectories != null)
            {
                trajectories = TrajectoryFitness.trajectories;
                VisualiseOptimisation = true;
                return;
            } else
            {
                */
                trajectories = reader.CreateFromFile(_view.TrajectoryFile, referencePoint);
            //}
            
            /*
            _view.Invoke(delegate { _view.NoiseCalculationCompleted(); });
            return;
            */

            // Calculate the noise for each trajectory
            temporalGrid = new TemporalGrid();
            int counter = 0;
            foreach (Trajectory trajectory in trajectories)
            {
                double percentage = (double) counter / trajectories.Count * 100.0;
                ProgressChanged(percentage);
                Console.WriteLine("INM "+counter+" started");
                //if (counter > 3) { break; }

                var INM = new IntegratedNoiseModel(trajectory, trajectory.Aircraft);
                INM.CellSize = 125;
                INM.MapToLargerGrid(reader.LowerLeftPoint, reader.UpperRightPoint);
                INM.MaxDistanceFromAirport(referencePoint.Point, 100000);
                INM.IntegrateToCurrentPosition = true;
                INM.NoiseMetric = 0;
                if (_view.NoiseMetric == 1)
                {
                    INM.NoiseMetric = 1;
                }
                INM.RunINMFullTrajectory();

                Grid grid = INM.TemporalGrid.GetGrid(0);
                Console.WriteLine(grid.Data.Length + "x"+ grid.Data[0].Length);
                grid.ReferencePoint = referencePoint;
                temporalGrid.AddGrid(grid);
                Console.WriteLine("INM " + counter + " completed");
                counter++;
            }

            GridConverter converter;
            switch (_view.NoiseMetric)
            {
                case 0:
                    converter = new GridConverter(temporalGrid, GridTransformation.LDEN);
                    temporalGrid = converter.Transform();
                    break;
                case 1:
                    converter = new GridConverter(temporalGrid, GridTransformation.SEL);
                    temporalGrid = converter.Transform();
                    break;
            }
            _view.Invoke(delegate { _view.NoiseCalculationCompleted(); });
        }

        public void VisualiseTrajectory()
        {
            referencePoint = new ReferencePointRD();
            if (_view.CustomReference)
            {
                referencePoint = new ReferencePoint(_view.GeoReference, _view.MetricReference);
            }

            VisualiseOptimisation = true;
            trajectory = _view.Trajectory;
        }

        #endregion




        #region "Prepare Visualisation"

        public void PrepareVisualisation()
        {
            startTime = DateTime.Now;

            if (_view.OneTrajectory)
            {
                thread = new Thread(OneTrajectoryVisualisation);
            }
            else
            {
                thread = new Thread(MultipleTrajectoryVisualisation);
            }
            thread.Start();
        }


        protected void OneTrajectoryVisualisation()
        {
            // Create legend
            var legend = new LegendCreator();
            legend.OutputLegendImage();
            legend.OutputLegendTitle();
            
            var sections = new List<KMLAnimatorSectionInterface>();
            
            // Contour animator
            List<int> contoursOfInterest = (_view.VisualiseContoursOfInterest) ? _view.ContoursOfInterest : null;
            var contourAnimator = new ContourKMLAnimator(temporalGrid, trajectory, contoursOfInterest);
            if (_view.VisualiseGradient)
            {
                contourAnimator.SetGradientSettings((int)_view.LowestContourValue, (int)_view.HighestContourValue, (int)_view.ContourValueStep);
            }

            // Create sections
            sections.Add(new LegendKMLAnimator());
            sections.Add(new AircraftKMLAnimator(trajectory.Aircraft, trajectory));
            sections.Add(new AirplotKMLAnimator(trajectory));
            sections.Add(new GroundplotKMLAnimator(trajectory));
            sections.Add(contourAnimator);
            //new AnnoyanceKMLAnimator(temporalGrid, population.getPopulationData())
            if (_view.Heatmap)
            {
                var population = new PopulationData(Globals.currentDirectory + "population.dat");
                population.Chance = _view.PopulationFactor;
                var section = new HeatmapKMLAnimator(population);
                section.DotSize = _view.PopulationDotSize;
                section.DotFile = _view.PopulationDotFile;
                sections.Add(section);
            }

            // Create animator
            var camera = new FollowKMLAnimatorCamera(trajectory.Aircraft, trajectory);
            var animator = new KMLAnimator(sections, camera);
            animator.AnimationToFile(trajectory.Duration, Globals.webrootDirectory + "visualisation.kml");

            _view.Invoke(delegate { _view.PreparationCalculationCompleted(); });
        }


        protected void MultipleTrajectoryVisualisation()
        {
            // Legend
            var legend = new LegendCreator();

            // plot legend
            legend.OutputLegendImage();
            legend.OutputLegendTitle();

            // Create sections
            var sections = new List<KMLAnimatorSectionInterface>();
            //sections.Add(new MaintainMultipleGroundPlotKMLAnimator(trajectories));
            if (!VisualiseOptimisation) {
                // Contour animator
                List<int> contoursOfInterest = (_view.VisualiseContoursOfInterest) ? _view.ContoursOfInterest : null;
                var contourAnimator = new ContourKMLAnimator(temporalGrid, null, contoursOfInterest);
                if (_view.VisualiseGradient)
                {
                    legend.SetSettings(_view.LowestContourValue, _view.HighestContourValue);
                    contourAnimator.SetGradientSettings((int)_view.LowestContourValue, (int)_view.HighestContourValue, (int)_view.ContourValueStep);
                }
                contourAnimator.AltitudeOffset = (_view.MapFile != "");
                sections.Add(new MultipleGroundplotKMLAnimator(trajectories));
                sections.Add(new LegendKMLAnimator());
                sections.Add(contourAnimator);
            } else
            {
                trajectories = TrajectoryFitness.trajectories;
                sections.Add(new FitnessGroundPlotKMLAnimator(trajectories));
            }
            if (_view.MapFile != "")
            {
                sections.Add(new CustomMapKMLAnimator(_view.MapFile, _view.MapBottomLeft, _view.MapUpperRight));
            }
            if (_view.Heatmap)
            {
                var population = new PopulationData(Globals.currentDirectory + "population.dat");
                population.Chance = _view.PopulationFactor;
                var section = new HeatmapKMLAnimator(population);
                section.DotSize = _view.PopulationDotSize;
                section.DotFile = _view.PopulationDotFile;
                sections.Add(section);
            }
            
            var camera = new TopViewKMLAnimatorCamera(
                new GeoPoint3D(4.9773743, 52.2384423,
                _view.CameraAltitude)
            );
            // Create animator
            /*
            var camera = new TopViewKMLAnimatorCamera(
                new GeoPoint3D(referencePoint.GeoPoint.Longitude, referencePoint.GeoPoint.Latitude, 
                _view.CameraAltitude)
            );
            */
            var animator = new KMLAnimator(sections, camera);
            animator.Duration = 0;
            animator.AnimationToFile(trajectories.Count, Globals.webrootDirectory + "visualisation.kml");

            _view.Invoke(delegate { _view.PreparationCalculationCompleted(); });
        }

        #endregion





        protected void ProgressChanged(double progress)
        {
            _view.Invoke(delegate
            {
                if (progress == 0) { return; }

                double factor = progress / 100;
                double secElapsed = DateTime.Now.Subtract(startTime).TotalSeconds;

                _view.Percentage = (int)(factor * 100);
                _view.TimeLeft = (int)Math.Ceiling(((secElapsed / factor) - secElapsed) / 60.0);
            });
        }

    }
}
