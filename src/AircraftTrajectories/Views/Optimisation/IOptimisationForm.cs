﻿using System;

namespace AircraftTrajectories.Views.Optimisation
{
    public interface IOptimisationForm
    {
        event EventHandler RunOptimisation;
        event EventHandler CancelOptimisation;

        int PopulationSize { get; }
        int NumberOfGenerations { get; }
    }
}