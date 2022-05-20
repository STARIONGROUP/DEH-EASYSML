// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectObjectRowComparer.cs" company="RHEA Sy
// stem S.A.">
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

    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows;

    using EA;

    /// <summary>
    /// The <see cref="IComparer{T}" /> used to sort <see cref="EnterpriseArchitectObjectBaseRowViewModel" />
    /// </summary>
    public class EnterpriseArchitectObjectRowComparer : IComparer<EnterpriseArchitectObjectBaseRowViewModel>
    {
        /// <summary>
        /// Compares two <see cref="EnterpriseArchitectObjectBaseRowViewModel" /> and returns a value indicating whether one is less than, equal
        /// to, or greater than the other.
        /// </summary>
        /// <param name="x">The first <see cref="EnterpriseArchitectObjectBaseRowViewModel" />.</param>
        /// <param name="y">The second o <see cref="EnterpriseArchitectObjectBaseRowViewModel" />.</param>
        /// <returns>
        /// Less than zero : x is "lower" than y
        /// Zero: x "equals" y.
        /// Greater than zero: x is "greater" than y.
        /// </returns>
        public int Compare(EnterpriseArchitectObjectBaseRowViewModel x, EnterpriseArchitectObjectBaseRowViewModel y)
        {
            if (x == null || y == null)
            {
                throw new InvalidOperationException("One or both of the parameters is null");
            }

            if (x is PackageRowViewModel && y is not PackageRowViewModel)
            {
                return -1;
            }

            if (x is not PackageRowViewModel && y is PackageRowViewModel)
            {
                return 1;
            }

            if (x is ValuePropertyRowViewModel && y is not ValuePropertyRowViewModel)
            {
                return -1;
            }

            if (x is not ValuePropertyRowViewModel && y is ValuePropertyRowViewModel)
            {
                return 1;
            }

            if (x is PartPropertyRowViewModel && y is not PartPropertyRowViewModel)
            {
                return -1;
            }

            if (x is not PartPropertyRowViewModel && y is PartPropertyRowViewModel)
            {
                return 1;
            }

            return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
