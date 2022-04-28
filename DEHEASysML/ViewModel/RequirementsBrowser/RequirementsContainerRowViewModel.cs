// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementsContainerRowViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;
    using CDP4Dal.Events;

    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using ReactiveUI;

    /// <summary>
    /// A Row view model that represents a <see cref="RequirementsContainer" />
    /// </summary>
    /// <typeparam name="T">
    /// A type of <see cref="RequirementsContainer" />
    /// </typeparam>
    public abstract class RequirementsContainerRowViewModel<T> : DefinedThingRowViewModel<T> where T : RequirementsContainer
    {
        /// <summary>
        /// Backing field for <see cref="Owner" />
        /// </summary>
        private DomainOfExpertise owner;

        /// <summary>
        /// Backing field for <see cref="OwnerName" />
        /// </summary>
        private string ownerName;

        /// <summary>
        /// Backing field for <see cref="OwnerShortName" />
        /// </summary>
        private string ownerShortName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsContainerRowViewModel{T}" />
        /// </summary>
        /// <param name="requirementsContainer">The <see cref="RequirementsContainer" /> associated with this row</param>
        /// <param name="session">The session</param>
        /// <param name="containerViewModel">
        /// The <see cref="IViewModelBase{T}" /> that is the container of this
        /// <see cref="IRowViewModelBase{Thing}" />
        /// </param>
        protected RequirementsContainerRowViewModel(T requirementsContainer, ISession session, IViewModelBase<Thing> containerViewModel) : base(requirementsContainer, session, containerViewModel)
        {
            this.UpdateProperties();
        }

        /// <summary>
        /// Gets or sets the Owner
        /// </summary>
        public DomainOfExpertise Owner
        {
            get => this.owner;
            set => this.RaiseAndSetIfChanged(ref this.owner, value);
        }

        /// <summary>
        /// Gets or set the ShortName of <see cref="Owner" />
        /// </summary>
        public string OwnerShortName
        {
            get => this.ownerShortName;
            set => this.RaiseAndSetIfChanged(ref this.ownerShortName, value);
        }

        /// <summary>
        /// Gets or set the Name of <see cref="Owner" />
        /// </summary>
        public string OwnerName
        {
            get => this.ownerName;
            set => this.RaiseAndSetIfChanged(ref this.ownerName, value);
        }

        /// <summary>
        /// The event-handler that is invoked by the subscription that listens for updates
        /// on the <see cref="Thing" /> that is being represented by the view-model
        /// </summary>
        /// <param name="objectChange">
        /// The payload of the event that is being handled
        /// </param>
        protected override void ObjectChangeEventHandler(ObjectChangedEvent objectChange)
        {
            base.ObjectChangeEventHandler(objectChange);
            this.UpdateProperties();
        }

        /// <summary>
        /// Updates the properties of this row
        /// </summary>
        private void UpdateProperties()
        {
            this.ModifiedOn = this.Thing.ModifiedOn;

            if (this.Thing.Owner != null)
            {
                this.OwnerShortName = this.Thing.Owner.ShortName;
                this.OwnerName = this.Thing.Owner.Name;
            }

            this.Owner = this.Thing.Owner;
        }
    }
}
