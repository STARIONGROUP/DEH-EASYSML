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

    using DEHEASysML.DstController;
    using DEHEASysML.ViewModel.Interfaces;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;
    using DEHEASysML.Views;

    using DEHPCommon.Enumerators;

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
        /// Backing field for <see cref="ArrowDirection" />
        /// </summary>
        private int arrowDirection;

        /// <summary>
        /// Backing field for <see cref="CurrentMappingDirection" />
        /// </summary>
        private int currentMappingDirection;

        /// <summary>
        /// Initializes a new <see cref="ImpactPanelViewModel" />
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="hubNetChangePreviewViewModel">The <see cref="IHubNetChangePreviewViewModel" /></param>
        /// <param name="dstNetChangePreview">The <see cref="IDstNetChangePreviewViewModel" /></param>
        public ImpactPanelViewModel(IDstController dstController, IHubNetChangePreviewViewModel hubNetChangePreviewViewModel, IDstNetChangePreviewViewModel dstNetChangePreview)
        {
            this.dstController = dstController;
            this.HubNetChangePreviewViewModel = hubNetChangePreviewViewModel;
            this.DstNetChangePreviewViewModel = dstNetChangePreview;

            this.InitializesCommands();
            this.UpdateProperties();
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
        /// The <see cref="IHubNetChangePreviewViewModel" />
        /// </summary>
        public IHubNetChangePreviewViewModel HubNetChangePreviewViewModel { get; }

        /// <summary>
        /// The <see cref="IDstNetChangePreviewViewModel" />
        /// </summary>
        public IDstNetChangePreviewViewModel DstNetChangePreviewViewModel { get; }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand" /> that will change the mapping direction
        /// </summary>
        public ReactiveCommand<object> ChangeMappingDirection { get; private set; }

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
        /// Initiliaze all <see cref="ReactiveCommand{T}" /> of this viewmodel
        /// </summary>
        private void InitializesCommands()
        {
            this.ChangeMappingDirection = ReactiveCommand.Create();
            this.ChangeMappingDirection.Subscribe(_ => this.ChangeMappingDirectionExecute());
        }

        /// <summary>
        /// Updates this view-model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.CurrentMappingDirection = (int)this.dstController.MappingDirection;
            this.ArrowDirection = this.CurrentMappingDirection * 180;
        }
    }
}
