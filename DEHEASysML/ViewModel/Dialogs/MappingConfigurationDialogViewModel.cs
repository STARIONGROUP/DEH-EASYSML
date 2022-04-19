// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
// Copyright (c) 2020-2022 RHEA System S.A.
// 
// Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Antoine Théate.
// 
// This file is part of DEHEASysML
// 
// The DEHEASysML is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
// 
// The DEHEASysML is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this program; if not, write to the Free Software Foundation,
// Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHEASysML.ViewModel.Dialogs
{
    using System;

    using DEHEASysML.DstController;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using NLog;

    using ReactiveUI;

    /// <summary>
    /// Base mapping configuration dialog view model
    /// </summary>
    public abstract class MappingConfigurationDialogViewModel : ReactiveObject, ICloseWindowViewModel
    {
        /// <summary>
        /// The <see cref="NLog" /> logger
        /// </summary>
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IHubController" />
        /// </summary>
        protected readonly IHubController HubController;

        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        protected readonly IDstController DstController;

        /// <summary>
        /// Backing field for <see cref="IsBusy" />
        /// </summary>
        private bool isBusy;

        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationDialogViewModel" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        protected MappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController)
        {
            this.HubController = hubController;
            this.DstController = dstController;

            this.CancelCommand = ReactiveCommand.Create();
            this.CancelCommand.Subscribe(_ => this.CloseWindowBehavior?.Close());

            this.ResetCommand = ReactiveCommand.Create();
            this.ResetCommand.Subscribe(_ => this.PreMap());
        }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand" /> to Reset the premapping
        /// </summary>
        public ReactiveCommand<object> ResetCommand { get; set; }

        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        public bool IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowViewModel" />
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand" /> to continue
        /// </summary>
        public ReactiveCommand<object> ContinueCommand { get; set; }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand" /> for canceling the operation
        /// </summary>
        public ReactiveCommand<object> CancelCommand { get; set; }

        /// <summary>
        /// Executes the <see cref="ContinueCommand" />
        /// </summary>
        /// <param name="mapCommand">The actual map action to perform</param>
        protected virtual void ExecuteContinueCommand(Action mapCommand)
        {
            this.IsBusy = true;

            try
            {
                mapCommand?.Invoke();
                this.CloseWindowBehavior?.Close();
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Premaps the elements that has been selected for the mapping
        /// </summary>
        protected abstract void PreMap();
    }
}
