// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IterationRequirementsViewModel.cs" company="RHEA System S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;
    using CDP4Dal.Events;

    using DEHEASysML.ViewModel.Comparers;

    using DEHPCommon.Extensions;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// Represent the view-model of the browser that displays all the <see cref="RequirementsSpecification" />s in one
    /// <see cref="Iteration" />
    /// </summary>
    public class IterationRequirementsViewModel : BrowserViewModel<Iteration>
    {
        /// <summary>
        /// The <see cref="IComparer{T}" />
        /// </summary>
        protected static readonly IComparer<IRowViewModelBase<Thing>> ChildRowComparer = new RequirementsSpecificationComparer();

        /// <summary>
        /// Initialize a new <see cref="RequirementsSpecification" />
        /// </summary>
        /// <param name="iteration">The <see cref="Iteration" /></param>
        /// <param name="session">The <see cref="ISession" /></param>
        public IterationRequirementsViewModel(Iteration iteration, ISession session) : base(iteration, session)
        {
            this.ShortName = "Requirements";
            this.Initialize();

            this.AddSubscriptions();
            this.UpdateProperties();
        }

        /// <summary>
        /// Gets or sets the name of this browsable thing
        /// </summary>
        public string ShortName { get; protected set; }

        /// <summary>
        /// Gets the view model current <see cref="EngineeringModelSetup"/>
        /// </summary>
        public EngineeringModelSetup CurrentEngineeringModelSetup => this.Thing.IterationSetup.GetContainerOfType<EngineeringModelSetup>();

        /// <summary>
        /// Update the properties of this view model
        /// </summary>
        public void UpdateProperties()
        {
            this.UpdateRequirementsSpecificationsRows();
        }

        /// <summary>
        /// The <see cref="ObjectChangedEvent"/> event-handler.
        /// </summary>
        /// <param name="objectChange">The <see cref="ObjectChangedEvent"/></param>
        protected override void ObjectChangeEventHandler(ObjectChangedEvent objectChange)
        {
            base.ObjectChangeEventHandler(objectChange);
            this.UpdateProperties();
        }

        /// <summary>
        /// Update the contained row of this view model
        /// </summary>
        private void UpdateRequirementsSpecificationsRows()
        {
            var currentRows = this.ContainedRows.Select(x => x.Thing).OfType<RequirementsSpecification>().ToList();

            var added = this.Thing.RequirementsSpecification.Except(currentRows).ToList();
           
            var removed = currentRows
                .Except(this.Thing.RequirementsSpecification.Where(x => !x.IsDeprecated)).ToList();

            foreach (var requirementsSpecification in added)
            {
                this.AddRequirementsSpecificationRow(requirementsSpecification);
            }

            foreach (var requirementsSpecification in removed)
            {
                this.RemoveRequirementsSpecificationRow(requirementsSpecification);
            }
        }

        /// <summary>
        /// Remove a row of the associated <see cref="RequirementsSpecification" />
        /// </summary>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /> to remove</param>
        private void RemoveRequirementsSpecificationRow(RequirementsSpecification requirementsSpecification)
        {
            var row = this.ContainedRows.SingleOrDefault(x => x.Thing == requirementsSpecification);

            if (row != null)
            {
                this.ContainedRows.RemoveAndDispose(row);
            }
        }

        /// <summary>
        /// Add a row of the associated <see cref="RequirementsSpecification" />
        /// </summary>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /> to add</param>
        private void AddRequirementsSpecificationRow(RequirementsSpecification requirementsSpecification)
        {
            if (requirementsSpecification.IsDeprecated)
            {
                return;
            }

            var row = new RequirementsSpecificationRowViewModel(requirementsSpecification, this.Session, this);
            this.ContainedRows.SortedInsert(row, ChildRowComparer);
        }

        /// <summary>
        /// Used to call virtual member when this gets initialized
        /// </summary>
        private void Initialize()
        {
            this.InitializeCommands();
        }

        /// <summary>
        /// Add the necessary subscriptions for this view model.
        /// </summary>
        private void AddSubscriptions()
        {
            var engineeringModelSetupSubscription = CDPMessageBus.Current.Listen<ObjectChangedEvent>(this.CurrentEngineeringModelSetup)
                .Where(objectChange => (objectChange.EventKind == EventKind.Updated)
                                       && (objectChange.ChangedThing.RevisionNumber > this.RevisionNumber))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            this.Disposables.Add(engineeringModelSetupSubscription);

            var domainOfExpertiseSubscription = CDPMessageBus.Current.Listen<ObjectChangedEvent>(typeof(DomainOfExpertise))
                .Where(objectChange => (objectChange.EventKind == EventKind.Updated) 
                                       && (objectChange.ChangedThing.RevisionNumber > this.RevisionNumber) 
                                       && (objectChange.ChangedThing.Cache == this.Session.Assembler.Cache))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            this.Disposables.Add(domainOfExpertiseSubscription);

            var iterationSetupSubscription = CDPMessageBus.Current.Listen<ObjectChangedEvent>(this.Thing.IterationSetup)
                .Where(objectChange => (objectChange.EventKind == EventKind.Updated)
                                       && (objectChange.ChangedThing.RevisionNumber > this.RevisionNumber))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            this.Disposables.Add(iterationSetupSubscription);
        }
    }
}
