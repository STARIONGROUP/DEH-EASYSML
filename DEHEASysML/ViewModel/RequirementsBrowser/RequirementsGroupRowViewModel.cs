// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementsGroupRowViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Dal.Events;

    using DEHEASysML.ViewModel.Comparers;

    using DEHPCommon.Extensions;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    /// <summary>
    /// The row view model that represents a <see cref="RequirementsGroup" />
    /// </summary>
    public class RequirementsGroupRowViewModel : RequirementContainerRowViewModel<RequirementsGroup>
    {
        /// <summary>
        /// The <see cref="IComparer{T}" />
        /// </summary>
        protected static readonly IComparer<IRowViewModelBase<Thing>> ChildRowComparer = new RequirementContainerChildRowComparer();

        /// <summary>
        /// The collection of <see cref="Requirement" /> that possibly belong to the represented <see cref="RequirementsGroup" />
        /// </summary>
        private readonly List<Requirement> requirements;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsGroupRowViewModel" /> class
        /// </summary>
        /// <param name="requirementsContainer">The <see cref="RequirementsGroup" /></param>
        /// <param name="session">The <see cref="ISession" /></param>
        /// <param name="containerViewModel">The container <see cref="IViewModelBase{T}" /></param>
        /// <param name="requirements">
        /// A collection of <see cref="Requirement" /> that possibly belong to the represented
        /// <see cref="RequirementsGroup" />
        /// </param>
        public RequirementsGroupRowViewModel(RequirementsGroup requirementsContainer, ISession session, IViewModelBase<Thing> containerViewModel, List<Requirement> requirements)
            : base(requirementsContainer, session, containerViewModel)
        {
            this.requirements = requirements;
            this.Initialize();
        }

        /// <summary>
        /// Updates the contained rows
        /// </summary>
        public void UpdateChildren()
        {
            this.ComputeContainedRows();
        }

        /// <summary>
        /// Gets a collection of all <see cref="Requirement"/>s that are contained under this row view model
        /// </summary>
        /// <returns></returns>
        public List<Requirement> GetAllRequirementsChildren()
        {
            var requirementsChildren = new List<Requirement>();
            requirementsChildren.AddRange(this.ContainedRows.OfType<RequirementRowViewModel>().Select(x => x.Thing));

            foreach (var requirementsGroupRow in this.ContainedRows.OfType<RequirementsGroupRowViewModel>())
            {
                requirementsChildren.AddRange(requirementsGroupRow.GetAllRequirementsChildren());
            }

            return requirementsChildren;
        }

        /// <summary>
        /// The <see cref="ObjectChangedEvent" /> event-handler.
        /// </summary>
        /// <param name="objectChange">The <see cref="ObjectChangedEvent" /></param>
        protected override void ObjectChangeEventHandler(ObjectChangedEvent objectChange)
        {
            base.ObjectChangeEventHandler(objectChange);
            this.UpdateProperties();
        }

        /// <summary>
        /// Update the properties of this view model
        /// </summary>
        protected override void UpdateProperties()
        {
            base.UpdateProperties();

            foreach (var requirementInsideGroup in this.requirements.Where(x => x.Group.Iid == this.Thing.Iid && !x.IsDeprecated).ToList())
            {
                this.ContainedRows.SortedInsert(new RequirementRowViewModel(requirementInsideGroup, this.Session, this), ChildRowComparer);
            }

            foreach (var requirementGroup in this.Thing.Group)
            {
                this.ContainedRows.SortedInsert(new RequirementsGroupRowViewModel(requirementGroup, this.Session, this, this.requirements), ChildRowComparer);
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
