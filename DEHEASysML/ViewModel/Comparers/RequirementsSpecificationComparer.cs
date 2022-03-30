// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementsSpecificationComparer.cs" company="RHEA System S.A.">
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

    using CDP4Common.CommonData;
    using CDP4Common.Comparers;
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    /// <summary>
    /// The <see cref="IComparer{T}" /> used to sort the child rows of the <see cref="RequirementsSpecification" />
    /// </summary>
    public class RequirementsSpecificationComparer : IComparer<IRowViewModelBase<Thing>>
    {
        /// <summary>
        /// The <see cref="DefinedThingComparer" />
        /// </summary>
        private static readonly ShortNameThingComparer Comparer = new();

        /// <summary>
        /// Compares two <see cref="RequirementsSpecification" />
        /// </summary>
        /// <param name="x">The first <see cref="RequirementsSpecification" /> to compare</param>
        /// <param name="y">The second <see cref="RequirementsSpecification" /> to compare</param>
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

            if (x.Thing is not RequirementsSpecification xSpec || y.Thing is not RequirementsSpecification ySpec)
            {
                throw new InvalidOperationException("One or both of the parameters is not a RequirementsSpecification row.");
            }

            return Comparer.Compare(xSpec, ySpec);
        }
    }
}
