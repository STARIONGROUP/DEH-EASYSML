// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectionService.cs" company="RHEA System S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using DEHEASysML.Utils;

    using EA;

    /// <summary>
    /// The <see cref="SelectionService" /> provides selection of any EA element based on filtering
    /// </summary>
    internal class SelectionService : ISelectionService
    {
        /// <summary>
        /// Gets all <see cref="Element" /> that have been selected or that is contained in selected <see cref="Package" />
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="isPackageSelection">Value asserting that we should select <see cref="Element"/> based on the package</param>
        /// <returns>A collection of retrieved <see cref="Element" /></returns>
        public IReadOnlyCollection<Element> GetSelectedElements(Repository repository, bool isPackageSelection)
        {
            var selectedPackagesId = QuerySelectedPackagesId(repository);
            var stereotypes = new []{ "block", "requirement"};

            var selectedElements = repository.CurrentSelection.ElementSet
                .OfType<Element>()
                .Where(x => Array.Exists(stereotypes, x.HasStereotype)).ToList();

            if (isPackageSelection && selectedElements.Any())
            {
                selectedPackagesId.Add(selectedElements[0].PackageID);
                selectedElements.Clear();
            }

            if (selectedPackagesId.Count == 0)
            {
                return selectedElements;
            }

            var allPackages = GetAllPackages(repository).ToList();

            var packageIdsToUse = new List<int>(selectedPackagesId);

            foreach (var selectedPackageId in selectedPackagesId)
            {
                packageIdsToUse.AddRange(SimplifiedPackage.QueryContainedPackagesId(allPackages, selectedPackageId));
            }

            var sqlResult = repository.SQLQuery($"SELECT Object_ID from t_object WHERE package_id IN ({string.Join(",", packageIdsToUse)})");
            var xmlElement = XElement.Parse(sqlResult);
            var rows = xmlElement.Descendants("Row");

            var elementId = rows.Select(row => int.Parse(row.Element("Object_ID")!.Value));

            selectedElements.AddRange(repository.GetElementSet(string.Join(",", elementId), 0).OfType<Element>()
                .Where(x => Array.Exists(stereotypes, x.HasStereotype)));

            return selectedElements;
        }

        /// <summary>
        /// Queries all id for all selected package in the current selection
        /// </summary>
        /// <param name="repository">The <see cref="IDualRepository" /></param>
        /// <returns>The <see cref="HashSet{T}" /> of package ID</returns>
        private static HashSet<int> QuerySelectedPackagesId(IDualRepository repository)
        {
            var selectedPackagesId = new HashSet<int>();

            for (short listIndex = 0; listIndex < repository.CurrentSelection.List.Count; listIndex++)
            {
                var item = (EAContext)repository.CurrentSelection.List.GetAt(listIndex);

                if (item.BaseType == nameof(Package))
                {
                    selectedPackagesId.Add(item.ElementID);
                }
            }

            return selectedPackagesId;
        }

        /// <summary>
        /// Retrieves all <see cref="SimplifiedPackage" /> that exists in the <see cref="IDualRepository" />
        /// </summary>
        /// <param name="repository">The <see cref="IDualRepository" /></param>
        /// <returns>The colleciton of <see cref="SimplifiedPackage" /></returns>
        private static IEnumerable<SimplifiedPackage> GetAllPackages(IDualRepository repository)
        {
            const string packageSqlQuery = "SELECT package.Package_Id AS Id, package.Parent_Id AS ParentId FROM t_package package order by 1";
            var sqlResult = repository.SQLQuery(packageSqlQuery);
            var xmlElement = XElement.Parse(sqlResult);
            var rows = xmlElement.Descendants("Row");

            return rows.Select(row => new SimplifiedPackage
            {
                PackageId = int.Parse(row.Element("Id")!.Value),
                ParentId = int.Parse(row.Element("ParentId")!.Value)
            }).ToList();
        }
    }
}
