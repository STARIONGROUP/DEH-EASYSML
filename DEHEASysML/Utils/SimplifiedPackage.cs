// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimplifiedPackage.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Utils
{
    using System.Collections.Generic;
    using System.Linq;

    using EA;

    /// <summary>
    /// Provide a simplified version of <see cref="Package" />
    /// </summary>
    public class SimplifiedPackage
    {
        /// <summary>
        /// Gets the id of the <see cref="Package" />
        /// </summary>
        public int PackageId { get; set; }

        /// <summary>
        /// Gets the id of the parent of the <see cref="Package" />
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Queries all <see cref="SimplifiedPackage" /> id that are contained by a <see cref="SimplifiedPackage" />
        /// </summary>
        /// <param name="simplifiedPackages">A collection of all existing <see cref="SimplifiedPackage" /></param>
        /// <param name="parentId">The id of the parent <see cref="SimplifiedPackage" /></param>
        /// <returns>A collection of all matching ids</returns>
        public static IReadOnlyCollection<int> QueryContainedPackagesId(IReadOnlyCollection<SimplifiedPackage> simplifiedPackages, int parentId)
        {
            var allDescendantsId = new List<int>();

            foreach (var childPackageId in simplifiedPackages.Where(x => x.ParentId == parentId).Select(x => x.PackageId))
            {
                allDescendantsId.Add(childPackageId);
                allDescendantsId.AddRange(QueryContainedPackagesId(simplifiedPackages, childPackageId));
            }

            return allDescendantsId;
        }
    }
}
