// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ISelectionService.cs" company="RHEA System S.A.">
// Copyright (c) 2020-2024 RHEA System S.A.
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

namespace DEHEASysML.Services.Selection
{
    using System.Collections.Generic;

    using EA;

    /// <summary>
    /// The <see cref="ISelectionService" /> provides selection of any EA element based on filtering
    /// </summary>
    public interface ISelectionService
    {
        /// <summary>
        /// Gets all <see cref="Element" /> that have been selected or that is contained in selected <see cref="Package" />
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of retrieved <see cref="Element" /> with the hierarchy of package id</returns>
        IReadOnlyCollection<Element> GetSelectedElements(Repository repository);
    }
}
