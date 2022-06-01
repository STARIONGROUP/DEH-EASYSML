// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingListPanelViewModel.cs" company="RHEA System S.A.">
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
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.ViewModel.Interfaces;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;

    using ReactiveUI;

    /// <summary>
    /// View Model for showing mapped things in a Panel
    /// </summary>
    public class MappingListPanelViewModel : ReactiveObject, IMappingListPanelViewModel
    {
        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Backing field for <see cref="IsBusy" />
        /// </summary>
        private bool? isBusy;

        /// <summary>
        /// Initializes a new <see cref="MappingListPanelViewModel" />
        /// </summary>
        public MappingListPanelViewModel(IDstController dstController)
        {
            this.dstController = dstController;

            this.InitializesObservables();
            this.PopulateMappedElements();
        }

        /// <summary>
        /// Gets the collection of <see cref="MappingRowViewModel" />
        /// </summary>
        public ReactiveList<MappingRowViewModel> MappingRows { get; } = new();

        /// <summary>
        /// Asserts if this view model is busy or not
        /// </summary>
        public bool? IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// Populates the <see cref="MappingRows" /> collection
        /// </summary>
        private void PopulateMappedElements()
        {
            foreach (var mappedElement in this.dstController.HubMapResult)
            {
                this.UpdateMappedThings(mappedElement);
            }

            foreach (var mappedElement in this.dstController.DstMapResult)
            {
                this.UpdateMappedThings(mappedElement);
            }
        }

        /// <summary>
        /// Initializes this viewmodel <see cref="Observable" />
        /// </summary>
        private void InitializesObservables()
        {
            this.dstController.DstMapResult.ItemsAdded.Subscribe(this.UpdateMappedThings);
            this.dstController.HubMapResult.ItemsAdded.Subscribe(this.UpdateMappedThings);

            this.dstController.DstMapResult.IsEmptyChanged.Where(x => x).Subscribe(_ =>
                this.MappingRows.RemoveAll(this.MappingRows
                    .Where(x => x.Direction == MappingDirection.FromDstToHub).ToList()));

            this.dstController.HubMapResult.IsEmptyChanged.Where(x => x).Subscribe(_ =>
                this.MappingRows.RemoveAll(this.MappingRows
                    .Where(x => x.Direction == MappingDirection.FromHubToDst).ToList()));
        }

        /// <summary>
        /// Updates the <see cref="MappingRows" />
        /// </summary>
        /// <param name="element">The <see cref="IMappedElementRowViewModel" /></param>
        private void UpdateMappedThings(IMappedElementRowViewModel element)
        {
            this.IsBusy = true;
            this.UpdateMappedThings(element, element.MappingDirection);
            this.IsBusy = false;
        }

        /// <summary>
        /// Updates the <see cref="MappingRows" />
        /// </summary>
        /// <param name="element">The <see cref="IMappedElementRowViewModel" /></param>
        /// <param name="mappingDirection">The <see cref="MappingDirection" /></param>
        private void UpdateMappedThings(IMappedElementRowViewModel element, MappingDirection mappingDirection)
        {
            if (mappingDirection == MappingDirection.FromDstToHub)
            {
                this.MappingRows.RemoveAll(this.MappingRows.Where(x => x.DstThing.First().Name == element.SourceElementName
                                                                       && x.Direction == MappingDirection.FromDstToHub).ToList());
            }
            else
            {
                this.MappingRows.RemoveAll(this.MappingRows.Where(x => x.HubThing.First().Name == element.SourceElementName
                                                                       && x.Direction == MappingDirection.FromHubToDst).ToList());
            }

            switch (element)
            {
                case MappedElementRowViewModel<ElementDefinition> mappedElementDefinition:
                    this.MappingRows.Add(new MappingRowViewModel(mappedElementDefinition, this.dstController));
                    break;
                case MappedElementRowViewModel<Requirement> mappedRequirement:
                    this.MappingRows.Add(new MappingRowViewModel(mappedRequirement, this.dstController));
                    break;
            }
        }
    }
}
