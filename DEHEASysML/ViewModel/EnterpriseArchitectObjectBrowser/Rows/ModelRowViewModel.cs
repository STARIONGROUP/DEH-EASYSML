// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModelRowViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows
{
    using System.Collections.Generic;

    using EA;

    /// <summary>
    /// The <see cref="ModelRowViewModel" /> represents the root element of a Model
    /// </summary>
    public class ModelRowViewModel : PackageRowViewModel
    {
        /// <summary>
        /// Initializes a new <see cref="ModelRowViewModel" />
        /// </summary>
        /// <param name="eaObject">The object to represent</param>
        /// <param name="visibleElements">Collection of <see cref="Element"/> that as to be visible</param>
        /// <param name="packagesId">The Id of <see cref="Package" /> to display</param>
        public ModelRowViewModel(Package eaObject, IEnumerable<Element> visibleElements, IEnumerable<int> packagesId) 
            : base(null, eaObject, visibleElements, packagesId)
        {
        }
    }
}
