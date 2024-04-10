// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRowViewModel.cs" company="RHEA System S.A.">
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
    using System.Linq;

    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;

    using DEHPCommon.Extensions;

    using EA;

    /// <summary>
    /// The <see cref="PackageRowViewModel" /> represents a row view model for a <see cref="Package" />
    /// </summary>
    public class PackageRowViewModel : EnterpriseArchitectObjectRowViewModel<Package>
    {
        /// <summary>
        /// Initializes a new <see cref="PackageRowViewModel" />
        /// </summary>
        /// <param name="parent">The parent row</param>
        /// <param name="eaObject">The object to represent</param>
        public PackageRowViewModel(EnterpriseArchitectObjectBaseRowViewModel parent, Package eaObject)
            : base(parent, eaObject)
        {
            this.PackageId = eaObject.ParentID;
            this.Initialize();
        }

        /// <summary>
        /// Compute the current row to initializes properties
        /// </summary>
        public override void ComputeRow()
        {
        }

        /// <summary>
        /// Updates this view model properties;
        /// </summary>
        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            this.ComputeRow();
        }

        /// <summary>
        /// Initializes the properties of this row
        /// </summary>
        private void Initialize()
        {
            this.UpdateProperties();
        }

        public void SetCurrentPackageAsPackage(IEnumerable<EnterpriseArchitectObjectBaseRowViewModel> children)
        {
            this.ContainedRows.Clear();
            this.ContainedRows.AddRange(children);

            foreach (var enterpriseArchitectObjectBaseRowViewModel in this.ContainedRows)
            {
                enterpriseArchitectObjectBaseRowViewModel.Parent = this;
            }
        }

        /// <summary>
        /// Gets or create an <see cref="ElementRowViewModel" /> to represents the <see cref="Element" />
        /// </summary>
        /// <param name="element">The <see cref="Element" /></param>
        /// <param name="packagesId">The collection of package that contains the element</param>
        /// <returns>The <see cref="ElementRowViewModel"/></returns>
        public ElementRowViewModel GetOrCreateElementRowViewModel(Element element, List<int> packagesId)
        {
            foreach (var containedPackage in this.ContainedRows.OfType<PackageRowViewModel>())
            {
                if (packagesId.Contains(containedPackage.RepresentedObject.PackageID))
                {
                    return containedPackage.GetOrCreateElementRowViewModel(element, packagesId);
                }
            }

            foreach (var containedElement in this.ContainedRows.OfType<ElementRowViewModel>())
            {
                if (containedElement.RepresentedObject.ElementGUID == element.ElementGUID)
                {
                    containedElement.UpdateElement(element);
                    return containedElement;
                }
            }

            ElementRowViewModel row;

            if (element.HasStereotype(StereotypeKind.Requirement))
            {
                row = new ElementRequirementRowViewModel(this, element);
            }
            else if (element.Stereotype.AreEquals(StereotypeKind.State))
            {
                row = new StateRowViewModel(this, element);
            }
            else
            {
                row = new BlockRowViewModel(this, element, true);
            }

            this.ContainedRows.SortedInsert(row, ContainedRowsComparer);
            return row;
        }

        /// <summary>
        /// Gets or create an <see cref="PackageRowViewModel" /> to represents the <see cref="Package" />
        /// </summary>
        /// <param name="package">The <see cref="Package" /></param>
        /// <param name="packagesId">The collection of package that contains the element</param>
        /// <returns>The <see cref="PackageRowViewModel"/></returns>
        public PackageRowViewModel GetOrCreatePackageRowViewModel(Package package, List<int> packagesId)
        {
            if (this.RepresentedObject.PackageID == packagesId.Last())
            {
                var existingPackageRow = this.ContainedRows.OfType<PackageRowViewModel>()
                    .FirstOrDefault(x => x.RepresentedObject.PackageID == package.PackageID);

                if (existingPackageRow == null)
                {
                    existingPackageRow = new PackageRowViewModel(this, package);
                    this.ContainedRows.SortedInsert(existingPackageRow, ContainedRowsComparer);
                }

                return existingPackageRow;
            }

            foreach (var containedPackage in this.ContainedRows.OfType<PackageRowViewModel>().ToList())
            {
                if (packagesId.Contains(containedPackage.RepresentedObject.PackageID))
                {
                    return containedPackage.GetOrCreatePackageRowViewModel(package, packagesId);
                }
            }

            return null;
        }
    }
}
