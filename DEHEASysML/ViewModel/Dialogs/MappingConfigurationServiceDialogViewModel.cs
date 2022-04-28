// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationServiceDialogViewModel.cs" company="RHEA System S.A.">
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
    using System.Linq;

    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.ViewModel.Dialogs.Interfaces;
    using DEHEASysML.Views.Dialogs;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// This view model lets the user setup the <see cref="MappingConfigurationService" /> configuration.
    /// This view model is mandatory because WinForm blocks the Keyboard event to WPF User Control
    /// </summary>
    public class MappingConfigurationServiceDialogViewModel : ReactiveObject, IMappingConfigurationServiceDialogViewModel, ICloseWindowViewModel
    {
        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IMappingConfigurationService" />
        /// </summary>
        private readonly IMappingConfigurationService mappingConfiguration;

        /// <summary>
        /// The <see cref="IHubController" />
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="INavigationService" />
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel" />
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// Backing field for <see cref="CurrentMappingConfigurationName" />
        /// </summary>
        private string currentMappingConfigurationName;

        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationServiceDialogViewModel" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="mappingConfigurationService">The <see cref="IMappingConfigurationService" /></param>
        /// <param name="navigationService">The <see cref="INavigationService" /></param>
        /// <param name="statusBar">The <see cref="statusBar" /></param>
        public MappingConfigurationServiceDialogViewModel(IHubController hubController, IDstController dstController,
            IMappingConfigurationService mappingConfigurationService, INavigationService navigationService, IStatusBarControlViewModel statusBar)
        {
            this.hubController = hubController;
            this.dstController = dstController;
            this.mappingConfiguration = mappingConfigurationService;
            this.navigationService = navigationService;
            this.statusBar = statusBar;

            this.PopulateAvailableMapping();
            this.InitializesCommands();
        }

        /// <summary>
        /// Gets or sets the name of the current <see cref="ExternalIdentifierMap" />
        /// </summary>
        public string CurrentMappingConfigurationName
        {
            get => this.currentMappingConfigurationName;
            set => this.RaiseAndSetIfChanged(ref this.currentMappingConfigurationName, value);
        }

        /// <summary>
        /// A collection of all available <see cref="ExternalIdentifierMap" /> names
        /// </summary>
        public ReactiveList<string> AvailableExternalIdentifierMap { get; private set; } = new() { ChangeTrackingEnabled = true };

        /// <summary>
        /// Gets the <see cref="ReactiveCommand" /> that will save or load the mapping configuration
        /// </summary>
        public ReactiveCommand<object> SaveOrLoadMappingConfiguration { get; private set; }

        /// <summary>
        /// Command to close the Window
        /// </summary>
        public ReactiveCommand<object> CloseCommand { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowViewModel" />
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        /// <summary>
        /// Populates the <see cref="AvailableExternalIdentifierMap" /> collection
        /// </summary>
        public void PopulateAvailableMapping()
        {
            this.AvailableExternalIdentifierMap.Clear();

            if (!this.hubController.IsSessionOpen || this.hubController.OpenIteration == null)
            {
                return;
            }

            this.CurrentMappingConfigurationName = this.mappingConfiguration.ExternalIdentifierMap.Name;

            this.AvailableExternalIdentifierMap.AddRange(this.hubController.AvailableExternalIdentifierMap(DstController.ThisToolName)
                .Select(x => x.Name)
                .ToList());

            this.AvailableExternalIdentifierMap.Sort();
        }

        /// <summary>
        /// Initiliaze all <see cref="ReactiveCommand{T}" /> of this viewmodel
        /// </summary>
        private void InitializesCommands()
        {
            this.SaveOrLoadMappingConfiguration = ReactiveCommand.Create(this.WhenAny(x => x.CurrentMappingConfigurationName,
                x => x.hubController.IsSessionOpen,
                (name, session)
                    => !string.IsNullOrWhiteSpace(name.Value) && session.Value));

            this.SaveOrLoadMappingConfiguration.Subscribe(_ => this.SaveOrLoadMappingConfigurationExecute());

            this.CloseCommand = ReactiveCommand.Create();
            this.CloseCommand.Subscribe(_ => this.CloseWindowBehavior?.Close());
        }

        /// <summary>
        /// Executes when the Save/Load configuration is pressed.
        /// Creates a new configuration or loads an existing one based on its name
        /// </summary>
        private void SaveOrLoadMappingConfigurationExecute()
        {
            var externalIdentifierMap = this.hubController.AvailableExternalIdentifierMap(DstController.ThisToolName)
                .FirstOrDefault(x => x.Name == this.CurrentMappingConfigurationName);

            externalIdentifierMap = externalIdentifierMap == null ? this.CreateNewMappingConfiguration() : externalIdentifierMap.Clone(true);

            this.mappingConfiguration.ExternalIdentifierMap = externalIdentifierMap;
            this.statusBar.Append($"Loading {this.CurrentMappingConfigurationName} mapping configuration ...");
            this.CloseCommand.Execute(null);
        }

        /// <summary>
        /// Creates a new <see cref="ExternalIdentifierMap" />
        /// </summary>
        /// <returns>The newly created <see cref="ExternalIdentifierMap" /></returns>
        private ExternalIdentifierMap CreateNewMappingConfiguration()
        {
            return this.mappingConfiguration.CreateExternalIdentifierMap(this.CurrentMappingConfigurationName,
                this.dstController.CurrentRepository.ProjectGUID, this.CheckForExistingTemporaryMapping());
        }

        /// <summary>
        /// Verifies that there is some existing mapping defined in the possibly temporary <see cref="ExternalIdentifierMap" />
        /// and asks the user if it wants to keep it
        /// </summary>
        /// <returns>The assert</returns>
        private bool CheckForExistingTemporaryMapping()
        {
            return this.mappingConfiguration.IsTheCurrentIdentifierMapTemporary
                   && this.mappingConfiguration.ExternalIdentifierMap.Correspondence.Any()
                   && this.navigationService.ShowDxDialog<SaveCurrentTemporaryIdentifierMap>() == true;
        }
    }
}
