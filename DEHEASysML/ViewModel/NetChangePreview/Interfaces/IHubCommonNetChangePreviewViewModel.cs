// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHubCommonNetChangePreviewViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.NetChangePreview.Interfaces
{
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.ViewModel.Rows;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for the <see cref="HubObjectNetChangePreviewViewModel" /> and <see cref="HubRequirementsNetChangePreviewViewModel"/>
    /// </summary>
    public interface IHubCommonNetChangePreviewViewModel
    {
        /// <summary>
        /// The collection of <see cref="IMappedElementRowViewModel" />
        /// </summary>
        ReactiveList<IMappedElementRowViewModel> MappedElements { get; }

        /// <summary>
        /// The command for the context menu that allows to deselect all selectable <see cref="ElementBase" /> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer" />
        /// </summary>
        ReactiveCommand<object> DeselectAllCommand { get; set; }

        /// <summary>
        /// The command for the context menu that allows to select all selectable <see cref="ElementBase" /> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer" />
        /// </summary>
        ReactiveCommand<object> SelectAllCommand { get; set; }

        /// <summary>
        /// Computes the old values with the new <see cref="IMappedElementRowViewModel"/> collection
        /// </summary>
        void ComputeValues();
    }
}
