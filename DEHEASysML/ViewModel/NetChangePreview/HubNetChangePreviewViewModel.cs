// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubNetChangePreviewViewModelViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.NetChangePreview
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Dal;
    using CDP4Dal.Events;

    using DEHEASysML.DstController;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.HubController.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// View model for this hub net change preview panel for Hub Elements
    /// </summary>
    public class HubNetChangePreviewViewModel : IHubNetChangePreviewViewModel
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
        /// Initializes a new instance of the <see cref="HubNetChangePreviewViewModel" /> class.
        /// </summary>
        /// <param name="objectNetChangePreview">The <see cref="IHubObjectNetChangePreviewViewModel" /></param>
        /// <param name="requirementsNetChangePreview">The <see cref="IHubRequirementsNetChangePreviewViewModel" /></param>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        public HubNetChangePreviewViewModel(IHubObjectNetChangePreviewViewModel objectNetChangePreview,
            IHubRequirementsNetChangePreviewViewModel requirementsNetChangePreview, IDstController dstController, IHubController hubController)
        {
            this.ObjectNetChangePreview = objectNetChangePreview;
            this.RequirementsNetChangePreview = requirementsNetChangePreview;
            this.dstController = dstController;
            this.hubController = hubController;

            this.InitializeObservable();

            this.ComputeValues();
        }

        /// <summary>
        /// The <see cref="IHubObjectNetChangePreviewViewModel" />
        /// </summary>
        public IHubObjectNetChangePreviewViewModel ObjectNetChangePreview { get; private set; }

        /// <summary>
        /// The <see cref="IHubRequirementsNetChangePreviewViewModel" />
        /// </summary>
        public IHubRequirementsNetChangePreviewViewModel RequirementsNetChangePreview { get; private set; }

        /// <summary>
        /// Initializes this view-model observables
        /// </summary>
        private void InitializeObservable()
        {
            this.WhenAnyValue(x => x.dstController.CanMap)
                .Where(x => !x)
                .Subscribe(_ => this.ComputeValues());

            this.dstController.DstMapResult.IsEmptyChanged.Where(x => !x).Subscribe(_ => this.ComputeValues());
        }

        /// <summary>
        /// Update the <see cref="IHubObjectNetChangePreviewViewModel" /> and the
        /// <see cref="IHubRequirementsNetChangePreviewViewModel" />
        /// </summary>
        private void ComputeValues()
        {
            this.ComputeValues<EnterpriseArchitectBlockElement>(this.ObjectNetChangePreview);
            this.ComputeValues<EnterpriseArchitectRequirementElement>(this.RequirementsNetChangePreview);
        }

        /// <summary>
        /// Update the <see cref="IHubCommonNetChangePreviewViewModel" />
        /// </summary>
        /// <typeparam name="T">A <see cref="IMappedElementRowViewModel" /></typeparam>
        /// <param name="netChangePreviewViewModel">The <see cref="IHubCommonNetChangePreviewViewModel" /></param>
        private void ComputeValues<T>(IHubCommonNetChangePreviewViewModel netChangePreviewViewModel) where T : IMappedElementRowViewModel
        {
            netChangePreviewViewModel.MappedElements.Clear();
            netChangePreviewViewModel.MappedElements.AddRange(this.dstController.DstMapResult.OfType<T>().Cast<IMappedElementRowViewModel>());
            netChangePreviewViewModel.ComputeValues();
        }
    }
}
