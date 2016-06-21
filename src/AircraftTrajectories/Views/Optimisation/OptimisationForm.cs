﻿using AircraftTrajectories.Presenters;
using System;
using System.Windows.Forms;

namespace AircraftTrajectories.Views.Optimisation
{
    public partial class OptimisationForm : MDIContainerForm, IOptimisationForm
    {
        public event EventHandler RunOptimisation;
        public event EventHandler CancelOptimisation;

        public OptimisationPresenter Presenter { get; protected set; }
        public OptimisationSettingsForm SettingsForm { get; protected set; }
        public OptimisationRunForm RunForm { get; protected set; }
        public OptimisationCompletedForm CompletedForm { get; protected set; }


        /// <summary>
        /// Construct the OptimisationForm
        /// </summary>
        public OptimisationForm()
        {
            InitializeComponent();
            Presenter = new OptimisationPresenter(this);
            SettingsForm = new OptimisationSettingsForm();
            RunForm = new OptimisationRunForm();
            CompletedForm = new OptimisationCompletedForm();
        }

        /// <summary>
        /// Run the optimisation
        /// this method switches to the OptimisationRunForm
        /// </summary>
        public void RunOptimisationClick()
        {
            RunForm.lblPercentage.Text = "Optimising trajectory";
            RunForm.lblTimeLeft.Text = "estimating time left";
            RunForm.BringToFront();
            RunOptimisation(this, EventArgs.Empty);
        }

        /// <summary>
        /// Cancel the optimisation
        /// this method switches back to the OptimisationSettingsForm
        /// </summary>
        public void CancelOptimisationClick()
        {
            SettingsForm.BringToFront();
            CancelOptimisation(this, EventArgs.Empty);
        }
        

        #region "EVENTS"

        protected void OptimisationForm_Load(object sender, EventArgs e)
        {
            base.MDIContainerForm_Load();

            SettingsForm.MdiParent = this;
            SettingsForm.Show();
            RunForm.MdiParent = this;
            RunForm.Show();
            CompletedForm.MdiParent = this;
            CompletedForm.Show();

            SettingsForm.BringToFront();
        }

        protected void OptimisationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CancelOptimisation(this, EventArgs.Empty);
            Application.Exit();
        }

        #endregion

        #region "ACCESS CONTROL ELEMENTS"

        public int PopulationSize
        {
            get
            {
                return int.Parse(SettingsForm.txtPopulationSize.Text);
            }
        }
        public int NumberOfGenerations
        {
            get
            {
                return int.Parse(SettingsForm.txtNumberOfGenerations.Text);
            }
        }

        public int Percentage
        {
            set
            {
                RunForm.lblPercentage.Text = "Optimising trajectory " + value + "%";
            }
        }

        public int TimeLeft
        {
            set
            {
                string message = (value < 1) ? "completed in 1min" : value + "min remaining";
                RunForm.lblTimeLeft.Text = message;
            }
        }

        public int NumberOfSegments
        {
            get
            {
                return int.Parse(SettingsForm.txtNumberOfSegments.Text);
            }
        }

        public double EndLatitude
        {
            get
            {
                return double.Parse(SettingsForm.txtEndLatitude.Text);
            }
        }

        public double EndLongitude
        {
            get
            {
                return double.Parse(SettingsForm.txtEndLongitude.Text);
            }
        }

        /// <summary>
        /// Allow changing control elements from inside threads
        /// </summary>
        /// <param name="methodInvoker"></param>
        public void Invoke(MethodInvoker methodInvoker)
        {
            base.Invoke(methodInvoker);
        }

        #endregion
    }
}