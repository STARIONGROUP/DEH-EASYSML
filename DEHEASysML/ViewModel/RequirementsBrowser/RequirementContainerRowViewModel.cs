// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementContainerRowViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.RequirementsBrowser
{
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.Utilities;

    /// <summary>
    /// A Row view model that represents a <see cref="RequirementsContainer" />
    /// </summary>
    /// <typeparam name="T">
    /// A type of <see cref="RequirementsContainer" />
    /// </typeparam>
    public class RequirementContainerRowViewModel<T> : RequirementsContainerRowViewModel<T> where T : RequirementsContainer
    {
        /// <summary>
        /// Initialize a new <see cref="RequirementContainerRowViewModel{T}" />
        /// </summary>
        /// <param name="requirementsContainer">The <see cref="RequirementsContainer" /> associated with this row</param>
        /// <param name="session">The session</param>
        /// <param name="containerViewModel">
        /// The <see cref="IViewModelBase{T}" /> that is the container of this
        /// <see cref="IRowViewModelBase{Thing}" />
        /// </param>
        public RequirementContainerRowViewModel(T requirementsContainer, ISession session, IViewModelBase<Thing> containerViewModel)
            : base(requirementsContainer, session, containerViewModel)
        {
        }

        /// <summary>
        /// Updates this view model properties
        /// </summary>
        protected virtual void UpdateProperties()
        {
            this.ThingStatus = new ThingStatus(this.Thing);
            this.ComputeContainedRows();
        }

        /// <summary>
        /// Computes the contained rows
        /// </summary>
        protected virtual void ComputeContainedRows()
        {
            this.ContainedRows.ClearAndDispose();
        }
    }
}
