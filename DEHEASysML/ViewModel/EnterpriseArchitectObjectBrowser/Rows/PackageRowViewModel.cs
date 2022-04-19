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

    using EA;

    /// <summary>
    /// The <see cref="PackageRowViewModel" /> represents a row view model for a <see cref="Package" />
    /// </summary>
    public class PackageRowViewModel: EnterpriseArchitectObjectRowViewModel<Package>
    {
        /// <summary>
        /// The collection of <see cref="Element"/> that as to be visible
        /// </summary>
        protected IEnumerable<Element> VisibleElements;

        /// <summary>
        /// The collection of id of <see cref="Package"/> that will be displayed
        /// </summary>
        protected IEnumerable<int> PackagesId;

        /// <summary>
        /// Initializes a new <see cref="PackageRowViewModel" />
        /// </summary>
        /// <param name="parent">The parent row</param>
        /// <param name="eaObject">The object to represent</param>
        /// <param name="visibleElements">Collection of <see cref="Element"/> that as to be visible</param>
        /// <param name="packagesId">The Id of <see cref="Package" /> to display</param>
        public PackageRowViewModel(EnterpriseArchitectObjectBaseRowViewModel parent, Package eaObject, IEnumerable<Element> visibleElements,
            IEnumerable<int> packagesId) 
            : base(parent, eaObject)
        {
            this.VisibleElements = visibleElements;
            this.PackagesId = packagesId;
            this.Initialize();
        }

        /// <summary>
        /// Initializes the properties of this row 
        /// </summary>
        private void Initialize()
        {
            this.UpdateProperties();
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
        /// Compute the current row to initializes properties
        /// </summary>
        public override void ComputeRow()
        {
            var requirements = this.RepresentedObject.GetElementsOfStereotypeInPackage(StereotypeKind.Requirement);
            var blocks = this.RepresentedObject.GetElementsOfStereotypeInPackage(StereotypeKind.Block);
            var packages = this.RepresentedObject.Packages.OfType<Package>();

            foreach (var package in packages.Where(x => this.PackagesId.Contains(x.PackageID)))
            {
                this.ContainedRows.Add(new PackageRowViewModel(this, package, this.VisibleElements, this.PackagesId));
            }

            foreach (var requirement in requirements.Where(x => this.VisibleElements.Any(vx => x.ElementGUID == vx.ElementGUID)))
            {
                this.ContainedRows.Add(new ElementRequirementRowViewModel(this, requirement));
            }

            foreach (var block in blocks.Where(x => this.VisibleElements.Any(vx => x.ElementGUID == vx.ElementGUID)))
            { 
                this.ContainedRows.Add(new BlockRowViewModel(this, block));
            }
        }
    }
}
