// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheService.cs" company="RHEA System S.A.">
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
    using System.Linq;
    using System.Xml.Linq;

    using DEHEASysML.Extensions;
    using DEHEASysML.Enumerators;

    using EA;

    /// <summary>
    /// The <see cref="CacheService"/> provides <see cref="Element" /> caching 
    /// </summary>
    public class CacheService: ICacheService
    {
        /// <summary>
        /// Gets the <see cref="Dictionary{TKey,TValue}"/> that contains cached <see cref="Element"/>
        /// </summary>
        private Dictionary<int, Element> cache;

        /// <summary>
        /// Gets all id of all <see cref="Package"/> contained in the project
        /// </summary>
        public IReadOnlyCollection<int> PackageIds { get; private set; } = [];

        /// <summary>
        /// Initializes the cache based on the current content of a <see cref="Repository"/>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/></param>
        public void InitializeCache(Repository repository)
        {
            this.QueryAllElements(repository);
            this.QueryAllPackagesId(repository);
        }

        /// <summary>
        /// Query all <see cref="Package"/> id
        /// </summary>
        /// <param name="repository"></param>
        private void QueryAllPackagesId(IDualRepository repository)
        {
            const string sqlQuery = "SELECT Package_ID FROM t_package";
            var sqlResult = repository.SQLQuery(sqlQuery);

            var xmlElement = XElement.Parse(sqlResult);
            var rows = xmlElement.Descendants("Row");

            this.PackageIds = rows.Select(row => int.Parse(row.Element("Package_ID")!.Value)).ToList();
        }

        /// <summary>
        /// Retrieves all <see cref="Element"/> contained in the <paramref name="repository"/>
        /// </summary>
        /// <param name="repository">The <see cref="IDualRepository"/></param>
        private void QueryAllElements(IDualRepository repository)
        {
            const string sqlQuery = "SELECT Object_ID FROM t_object WHERE NOT Object_Type = \"Package\"";
            var sqlResult = repository.SQLQuery(sqlQuery);

            var xmlElement = XElement.Parse(sqlResult);
            var rows = xmlElement.Descendants("Row");

            var elementIds = rows.Select(row => int.Parse(row.Element("Object_ID")!.Value));
            this.cache = repository.GetElementSet(string.Join(",", elementIds), 0).OfType<Element>().ToDictionary(x => x.ElementID, x => x);
        }

        /// <summary>
        /// Gets every <see cref="Element"/> that where any stereotype matches the provides <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>A collection of <see cref="Element"/></returns>
        public IReadOnlyCollection<Element> GetElementsOfStereotype(StereotypeKind stereotype)
        {
            return this.cache == null ? Array.Empty<Element>() :  this.GetAllElements().Where(x => x.HasStereotype(stereotype)).ToList();
        }

        /// <summary>
        /// Gets every <see cref="Element"/> that where any stereotype matches the provides <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>A collection of <see cref="Element"/></returns>
        public IReadOnlyCollection<Element> GetElementsOfMetaType(StereotypeKind stereotype)
        {
            return this.cache == null ? Array.Empty<Element>() : this.GetAllElements().Where(x =>x.MetaType.AreEquals(stereotype)).ToList();
        }

        /// <summary>
        /// Gets an <see cref="Element"/> based on its Id
        /// </summary>
        /// <param name="id">The <see cref="Element"/> Id</param>
        /// <returns>The <see cref="Element"/> if found, null otherwise</returns>
        public Element GetElementById(int id)
        {
            return this.cache.TryGetValue(id, out var element) ? element : null;
        }

        /// <summary>
        /// Gets all <see cref="Element"/> contains inside the project
        /// </summary>
        /// <returns>The collection of all cached <see cref="Element"/></returns>
        public IReadOnlyCollection<Element> GetAllElements()
        {
            return this.cache.Values;
        }
    }
}
