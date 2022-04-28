// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementContainerChildRowComparer.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.Comparers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.Comparers;
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.ViewModel.RequirementsBrowser;

    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    /// <summary>
    /// The <see cref="IComparer{T}" /> used to sort the child rows of the <see cref="RequirementContainerRowViewModel{T}" />
    /// </summary>
    public class RequirementContainerChildRowComparer : IComparer<IRowViewModelBase<Thing>>
    {
        /// <summary>
        /// The <see cref="DefinedThingComparer" />
        /// </summary>
        private static readonly ShortNameThingComparer Comparer = new();

        /// <summary>
        /// The Permissible Kind of child <see cref="IRowViewModelBase{T}" />
        /// </summary>
        private static readonly List<Type> PermissibleRowTypes = new()
        {
            typeof(RequirementsGroupRowViewModel),
            typeof(RequirementRowViewModel)
        };

        /// <summary>
        /// Compares two <see cref="IRowViewModelBase{Thing}" />
        /// </summary>
        /// <param name="x">The first <see cref="IRowViewModelBase{Thing}" /> to compare</param>
        /// <param name="y">The second <see cref="IRowViewModelBase{Thing}" /> to compare</param>
        /// <returns>
        /// Less than zero : x is "lower" than y
        /// Zero: x "equals" y.
        /// Greater than zero: x is "greater" than y.
        /// </returns>
        public int Compare(IRowViewModelBase<Thing> x, IRowViewModelBase<Thing> y)
        {
            if (x == null || y == null)
            {
                throw new InvalidOperationException("One or both of the parameters is null");
            }

            var xType = x.GetType();
            var yType = y.GetType();

            if (!PermissibleRowTypes.Any(type => type.IsAssignableFrom(xType)) || !PermissibleRowTypes.Any(type => type.IsAssignableFrom(yType)))
            {
                throw new InvalidOperationException("The list contains other types of row than the specified ones.");
            }

            if (typeof(RequirementRowViewModel).IsAssignableFrom(xType) && typeof(RequirementsGroupRowViewModel).IsAssignableFrom(yType))
            {
                return -1;
            }

            if (typeof(RequirementsGroupRowViewModel).IsAssignableFrom(xType) && typeof(RequirementRowViewModel).IsAssignableFrom(yType))
            {
                return 1;
            }

            if (xType == typeof(RequirementRowViewModel))
            {
                return Comparer.Compare((Requirement) x.Thing, (Requirement) y.Thing);
            }

            return xType == typeof(RequirementsGroupRowViewModel) ? Comparer.Compare((RequirementsGroup) x.Thing, (RequirementsGroup) y.Thing) : 1;
        }
    }
}
