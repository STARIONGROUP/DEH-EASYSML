// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementsSpecificationRowViewModel.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHEASysML.ViewModel.Comparers;

    using DEHPCommon.Extensions;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows;

    /// <summary>
    /// The row view model that represents a <see cref="RequirementsSpecification" />
    /// </summary>
    public class RequirementsSpecificationRowViewModel : RequirementContainerRowViewModel<RequirementsSpecification>
    {
        /// <summary>
        /// The <see cref="IComparer{T}" />
        /// </summary>
        protected static readonly IComparer<IRowViewModelBase<Thing>> ChildRowComparer = new RequirementContainerChildRowComparer();

        /// <summary>
        /// Initializes a new <see cref="RequirementsSpecificationRowViewModel" />
        /// </summary>
        /// <param name="requirementsContainer">The <see cref="RequirementsSpecification" /></param>
        /// <param name="session">Tje <see cref="Session" /></param>
        /// <param name="containerViewModel">The container <see cref="IViewModelBase{T}" /></param>
        public RequirementsSpecificationRowViewModel(RequirementsSpecification requirementsContainer, ISession session, IViewModelBase<Thing> containerViewModel)
            : base(requirementsContainer, session, containerViewModel)
        {
            this.Initialize();
        }

        /// <summary>
        /// Updates this view model properties
        /// </summary>
        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            this.ComputeContainedRows();
        }

        /// <summary>
        /// Computes this view model <see cref="RowViewModelBase{T}.ContainedRows" />
        /// </summary>
        protected override void ComputeContainedRows()
        {
            base.ComputeContainedRows();

            foreach (var requirement in this.Thing.Requirement.Where(x => x.Group == null && !x.IsDeprecated).ToList())
            {
                this.ContainedRows.SortedInsert(new RequirementRowViewModel(requirement, this.Session, this), ChildRowComparer);
            }

            foreach (var requirementGroup in this.Thing.Group.Where(x => x.Container.Iid == this.Thing.Iid))
            {
                this.ContainedRows.SortedInsert(new RequirementsGroupRowViewModel(requirementGroup, this.Session, this,
                    this.Thing.Requirement.Where(x => x.Group != null).ToList()), ChildRowComparer);
            }
        }

        /// <summary>
        /// Used to call virtual member when this gets initialized
        /// </summary>
        private void Initialize()
        {
            this.UpdateProperties();
        }
    }
}
