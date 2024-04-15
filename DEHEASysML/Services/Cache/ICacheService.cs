// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICacheService.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Services.Cache
{
    using System;
    using System.Collections.Generic;

    using DEHEASysML.Enumerators;

    using EA;

    /// <summary>
    /// The <see cref="ICacheService"/> provides <see cref="Element" /> caching 
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Initializes the cache based on the current content of a <see cref="Repository"/>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/></param>
        void InitializeCache(Repository repository);

        /// <summary>
        /// Gets every <see cref="Element"/> that where any stereotype matches the provides <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>A collection of <see cref="Element"/></returns>
        IReadOnlyCollection<Element> GetElementsOfStereotype(StereotypeKind stereotype);

        /// <summary>
        /// Gets every <see cref="Element"/> that where Metatype matches the provides <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>A collection of <see cref="Element"/></returns>
        IReadOnlyCollection<Element> GetElementsOfMetaType(StereotypeKind stereotype);

        /// <summary>
        /// Gets all <see cref="Element"/> contains inside the project
        /// </summary>
        /// <returns>The collection of all cached <see cref="Element"/></returns>
        IReadOnlyCollection<Element> GetAllElements();

        /// <summary>
        /// Gets all id of all <see cref="Package"/> contained in the project
        /// </summary>
        IReadOnlyCollection<int> PackageIds { get; }

        /// <summary>
        /// Gets an <see cref="Element"/> based on its Id
        /// </summary>
        /// <param name="id">The <see cref="Element"/> Id</param>
        /// <returns>The <see cref="Element"/> if found, null otherwise</returns>
        Element GetElementById(int id);

        /// <summary>
        /// Gets all <see cref="Connector"/> associated to an <see cref="Element"/>
        /// </summary>
        /// <param name="elementId">The <see cref="Element"/> Id</param>
        /// <returns>A collection of <see cref="Connector"/></returns>
        IReadOnlyCollection<Connector> GetConnectorsOfElement(int elementId);
    }
}
