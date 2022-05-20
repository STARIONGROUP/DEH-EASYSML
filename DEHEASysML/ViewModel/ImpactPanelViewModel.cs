// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImpactPanelViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel
{
    using System;
    using System.Reactive.Linq;

    using DEHEASysML.DstController;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.ViewModel.Interfaces;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;
    using DEHEASysML.Views;
    using DEHEASysML.Views.Dialogs;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// The view model for the <see cref="ImpactPanel" />
    /// </summary>
    public class ImpactPanelViewModel : ReactiveObject, IImpactPanelViewModel
    {
        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IHubController" />
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="INavigationService" />
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// The <see cref="IMappingConfigurationService" />
        /// </summary>
        private readonly IMappingConfigurationService mappingConfiguration;

        /// <summary>
        /// Backing field for <see cref="ArrowDirection" />
        /// </summary>
        private int arrowDirection;

        /// <summary>
        /// Backing field for <see cref="CurrentMappingDirection" />
        /// </summary>
        private int currentMappingDirection;

        /// <summary>
        /// Backing field for <see cref="CurrentMappingConfigurationName" />
        /// </summary>
        private string currentMappingConfigurationName;

        /// <summary>
        /// Backing field for <see cref="IsBusy" />
        /// </summary>
        private bool? isBusy;

        /// <summary>
        /// Initializes a new <see cref="ImpactPanelViewModel" />
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="hubNetChangePreviewViewModel">The <see cref="IHubNetChangePreviewViewModel" /></param>
        /// <param name="dstNetChangePreview">The <see cref="IDstNetChangePreviewViewModel" /></param>
        /// <param name="transferControlViewModel">The <see cref="ITransferControlViewModel" /></param>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="navigationService">The <see cref="INavigationService" /></param>
        /// <param name="mappingConfiguration">The <see cref="IMappingConfigurationService" /></param>
        public ImpactPanelViewModel(IDstController dstController, IHubNetChangePreviewViewModel hubNetChangePreviewViewModel,
            IDstNetChangePreviewViewModel dstNetChangePreview, ITransferControlViewModel transferControlViewModel, IHubController hubController,
            INavigationService navigationService, IMappingConfigurationService mappingConfiguration)
        {
            this.dstController = dstController;
            this.HubNetChangePreviewViewModel = hubNetChangePreviewViewModel;
            this.DstNetChangePreviewViewModel = dstNetChangePreview;
            this.TransferControlViewModel = transferControlViewModel;
            this.hubController = hubController;
            this.navigationService = navigationService;
            this.mappingConfiguration = mappingConfiguration;

            this.InitializesCommandsAndObservables();
            this.UpdateProperties();
        }

        /// <summary>
        /// Asserts if this view model is busy or not
        /// </summary>
        public bool? IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// Gets or sets the direction of the arrow
        /// </summary>
        public int ArrowDirection
        {
            get => this.arrowDirection;
            set => this.RaiseAndSetIfChanged(ref this.arrowDirection, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="CurrentMappingDirection" />
        /// </summary>
        public int CurrentMappingDirection
        {
            get => this.currentMappingDirection;
            set => this.RaiseAndSetIfChanged(ref this.currentMappingDirection, value);
        }

        /// <summary>
        /// Gets or sets the name of the current <see cref="IMappingConfigurationService.ExternalIdentifierMap" />
        /// </summary>
        public string CurrentMappingConfigurationName
        {
            get => this.currentMappingConfigurationName;
            set => this.RaiseAndSetIfChanged(ref this.currentMappingConfigurationName, value);
        }

        /// <summary>
        /// The <see cref="IHubNetChangePreviewViewModel" />
        /// </summary>
        public IHubNetChangePreviewViewModel HubNetChangePreviewViewModel { get; }

        /// <summary>
        /// The <see cref="IDstNetChangePreviewViewModel" />
        /// </summary>
        public IDstNetChangePreviewViewModel DstNetChangePreviewViewModel { get; }

        /// <summary>
        /// The <see cref="ITransferControlViewModel" />
        /// </summary>
        public ITransferControlViewModel TransferControlViewModel { get; }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand" /> that will change the mapping direction
        /// </summary>
        public ReactiveCommand<object> ChangeMappingDirection { get; private set; }

        /// <summary>
        /// Opens a dialog to setup the mapping configuration
        /// </summary>
        public ReactiveCommand<object> OpenMappingConfigurationDialog { get; private set; }

        /// <summary>
        /// Executes the <see cref="ChangeMappingDirection" /> command
        /// </summary>
        private void ChangeMappingDirectionExecute()
        {
            this.dstController.MappingDirection = this.dstController.MappingDirection == MappingDirection.FromDstToHub
                ? MappingDirection.FromHubToDst
                : MappingDirection.FromDstToHub;

            this.UpdateProperties();
        }

        /// <summary>
        /// Initiliaze all <see cref="ReactiveCommand{T}" /> and <see cref="Observable" /> of this viewmodel
        /// </summary>
        private void InitializesCommandsAndObservables()
        {
            this.ChangeMappingDirection = ReactiveCommand.Create();
            this.ChangeMappingDirection.Subscribe(_ => this.ChangeMappingDirectionExecute());

            this.OpenMappingConfigurationDialog = ReactiveCommand.Create(this.WhenAny(x => x.hubController.OpenIteration,
                iteration => iteration.Value != null));

            this.OpenMappingConfigurationDialog.Subscribe(_ => this.OpenMappingConfigurationDialogExecute());

            this.WhenAnyValue(x => x.DstNetChangePreviewViewModel.IsBusy,
                x => x.HubNetChangePreviewViewModel.IsBusy,
                x => x.dstController.IsBusy).Subscribe(_ => this.UpdateIsBusy());

            this.WhenAnyValue(x => x.hubController.OpenIteration)
                .Where(x => x == null)
                .Subscribe(_ => this.UpdateProperties());
        }

        /// <summary>
        /// Execute the <see cref="OpenMappingConfigurationDialog" /> Command
        /// </summary>
        private void OpenMappingConfigurationDialogExecute()
        {
            this.navigationService.ShowDialog<MappingConfigurationServiceDialog>();
            this.dstController.LoadMapping();
            this.UpdateProperties();
        }

        /// <summary>
        /// Updates this view-model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.CurrentMappingDirection = (int)this.dstController.MappingDirection;
            this.ArrowDirection = this.CurrentMappingDirection * 180;

            this.CurrentMappingConfigurationName = string.IsNullOrWhiteSpace(this.mappingConfiguration.ExternalIdentifierMap.Name)
                ? ""
                : $"Current Mapping: {this.mappingConfiguration.ExternalIdentifierMap.Name}";
        }

        /// <summary>
        /// Update the <see cref="IsBusy" /> property
        /// </summary>
        private void UpdateIsBusy()
        {
            var dstNetChangeBusy = this.DstNetChangePreviewViewModel.IsBusy;
            var hubNetChangeBusy = this.HubNetChangePreviewViewModel.IsBusy;
            var dstControllerBusy = this.dstController.IsBusy;

            this.IsBusy = dstNetChangeBusy != null && hubNetChangeBusy != null && dstControllerBusy != null
                                                   && (dstNetChangeBusy.Value || hubNetChangeBusy.Value 
                                                       || dstControllerBusy.Value);
        }
    }
}
