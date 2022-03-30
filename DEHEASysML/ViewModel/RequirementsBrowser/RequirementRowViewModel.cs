// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementRowViewModel.cs" company="RHEA System S.A.">
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
    using System.Globalization;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;
    using DEHPCommon.Utilities;

    using ReactiveUI;

    /// <summary>
    /// The row view model that represents a <see cref="Requirement" />
    /// </summary>
    public class RequirementRowViewModel : DefinedThingRowViewModel<Requirement>
    {
        /// <summary>
        /// Backing field for <see cref="Definition" />
        /// </summary>
        private string definition;

        /// <summary>
        /// Initialize a new <see cref="RequirementRowViewModel" />
        /// </summary>
        /// <param name="requirement">The associated <see cref="Requirement " /></param>
        /// <param name="session">The <see cref="Session" /></param>
        /// <param name="containerViewModel">
        /// The <see cref="IViewModelBase{T}" /> that is the container of this
        /// <see cref="IRowViewModelBase{Thing}" />
        /// </param>
        public RequirementRowViewModel(Requirement requirement, ISession session, IViewModelBase<Thing> containerViewModel)
            : base(requirement, session, containerViewModel)
        {
            this.UpdateProperties();
        }

        /// <summary>
        /// Gets the definition of the current requirement
        /// </summary>
        public string Definition
        {
            get => this.definition;
            protected set => this.RaiseAndSetIfChanged(ref this.definition, value);
        }

        /// <summary>
        /// Update this view model properties
        /// </summary>
        public void UpdateProperties()
        {
            this.ThingStatus = new ThingStatus(this.Thing);

            var definitions = this.Thing.Definition;

            var definitionBasedOnCulture = definitions.FirstOrDefault(x =>
                string.Equals(x.LanguageCode, CultureInfo.CurrentCulture.Name, StringComparison.CurrentCultureIgnoreCase));

            this.Definition = definitionBasedOnCulture == null ? definitions.FirstOrDefault()?.Content : definitionBasedOnCulture.Content;
        }
    }
}
