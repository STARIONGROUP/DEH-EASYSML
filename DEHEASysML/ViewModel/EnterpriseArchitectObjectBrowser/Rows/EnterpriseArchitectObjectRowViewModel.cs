// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectObjectRowViewModel.cs" company="RHEA System S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DEHEASysML.Enumerators;

    using DEHPCommon.Extensions;

    using EA;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="EnterpriseArchitectObjectRowViewModel{TEAClass}" /> represents a
    /// <see cref="EnterpriseArchitectObjectBaseRowViewModel" />
    /// with a dedicated object contained
    /// </summary>
    /// <typeparam name="TEaClass">Any class</typeparam>
    public abstract class EnterpriseArchitectObjectRowViewModel<TEaClass> : EnterpriseArchitectObjectBaseRowViewModel where TEaClass : class
    {
        /// <summary>
        /// Backing field for <see cref="RepresentedObject" />
        /// </summary>
        private TEaClass representedObject;

        /// <summary>
        /// Backing field for <see cref="ToolTip" />
        /// </summary>
        private string toolTip;

        /// <summary>
        /// Initializes a new <see cref="EnterpriseArchitectObjectBaseRowViewModel" />
        /// </summary>
        /// <param name="parent">The parent row</param>
        /// <param name="eaObject">The object to represent</param>
        protected EnterpriseArchitectObjectRowViewModel(EnterpriseArchitectObjectBaseRowViewModel parent, TEaClass eaObject)
        {
            this.Parent = parent;
            this.RepresentedObject = eaObject;
        }

        /// <summary>
        /// Asserts that the tree has to show every element
        /// </summary>
        protected bool ShouldShowEverything { get; set; }

        /// <summary>
        /// The object that the row represents
        /// </summary>
        public TEaClass RepresentedObject
        {
            get => this.representedObject;
            set => this.RaiseAndSetIfChanged(ref this.representedObject, value);
        }

        /// <summary>
        /// Gets or sets the Tooltip of the row
        /// </summary>
        public string ToolTip
        {
            get => this.toolTip;
            set => this.RaiseAndSetIfChanged(ref this.toolTip, value);
        }

        /// <summary>
        /// Update the <see cref="RepresentedObject" />
        /// </summary>
        /// <param name="newObject">The new <see cref="TEaClass" /> object</param>
        public void UpdateRepresentedObject(TEaClass newObject)
        {
            this.RepresentedObject = newObject;
            this.UpdateProperties();
        }

        /// <summary>
        /// Updates this view model properties;
        /// </summary>
        protected virtual void UpdateProperties()
        {
            switch (this.RepresentedObject)
            {
                case Package package:
                    this.Name = package.Name;
                    this.RowType = "Package";
                    break;
                case Element element:
                    this.Name = element.Name;
                    this.RowType = element.StereotypeEx;
                    break;
                case Partition partition:
                    this.Name = partition.Name;
                    this.RowType = "Partition";
                    break;
            }

            this.ToolTip = $"Row respresenting : {this.Name} of type {this.RowType}";
        }

        /// <summary>
        /// Update the <see cref="EnterpriseArchitectObjectBaseRowViewModel.ContainedRows"/> that correspond to a certain <see cref="StereotypeKind"/>
        /// to apply the latest changes
        /// </summary>
        /// <param name="stereotypeKind">The <see cref="StereotypeKind"/></param>
        /// <param name="elements">The contained <see cref="Element"/></param>
        protected void UpdateContainedRowsOfStereotype(StereotypeKind stereotypeKind, List<Element> elements)
        {
            var rows = this.GetContainedRowsOfStereotype(stereotypeKind);

            var rowsToUpdate = rows.Where(x =>
                elements.Any(element => x.RepresentedObject.ElementGUID == element.ElementGUID));

            var rowsToRemoves = rows.Where(x =>
                elements.All(element => x.RepresentedObject.ElementGUID != element.ElementGUID));

            var elementsToAdd = elements.Where(x =>
                rows.All(row => row.RepresentedObject.ElementGUID != x.ElementGUID));

            foreach (var row in rowsToUpdate)
            {
                row.UpdateElement(elements.FirstOrDefault(x => x.ElementGUID == row.RepresentedObject.ElementGUID));
            }

            foreach (var elementRowViewModel in rowsToRemoves)
            {
                this.ContainedRows.Remove(elementRowViewModel);
            }

            foreach (var elementToAdd in elementsToAdd)
            {
                this.ContainedRows.SortedInsert(this.CreateRow(elementToAdd, stereotypeKind), ContainedRowsComparer);
            }
        }

        /// <summary>
        /// Creates a new <see cref="EnterpriseArchitectObjectBaseRowViewModel"/> based on an <see cref="Element"/>
        /// and the <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="element">The <see cref="Element"/></param>
        /// <param name="stereotypeKind">The <see cref="StereotypeKind"/></param>
        /// <returns>A newly created <see cref="EnterpriseArchitectObjectBaseRowViewModel"/></returns>
        protected EnterpriseArchitectObjectBaseRowViewModel CreateRow(Element element, StereotypeKind stereotypeKind)
        {
            return stereotypeKind switch
            {
                StereotypeKind.State => new StateRowViewModel(this, element),
                StereotypeKind.Requirement => new ElementRequirementRowViewModel(this, element),
                StereotypeKind.Block => new BlockRowViewModel(this, element, true),
                StereotypeKind.PartProperty => new PartPropertyRowViewModel(this, element),
                StereotypeKind.ValueProperty => new ValuePropertyRowViewModel(this, element),
                StereotypeKind.Port => new PortRowViewModel(this, element),
                _ => throw new ArgumentOutOfRangeException(nameof(stereotypeKind), "Stereotype not supported")
            };
        }

        /// <summary>
        /// Gets all <see cref="ElementRowViewModel"/> contained that correspond to a certain <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="stereotypeKind">The <see cref="StereotypeKind"/></param>
        /// <returns>A collection of <see cref="ElementRowViewModel"/></returns>
        protected List<ElementRowViewModel> GetContainedRowsOfStereotype(StereotypeKind stereotypeKind)
        {
            var rows = new List<ElementRowViewModel>();

            switch (stereotypeKind)
            {
                case StereotypeKind.State:
                    rows.AddRange(this.ContainedRows.OfType<StateRowViewModel>());
                    break;
                case StereotypeKind.Requirement:
                    rows.AddRange(this.ContainedRows.OfType<ElementRequirementRowViewModel>());
                    break;
                case StereotypeKind.Block:
                    rows.AddRange(this.ContainedRows.OfType<BlockRowViewModel>());
                    break;
                case StereotypeKind.PartProperty:
                    rows.AddRange(this.ContainedRows.OfType<PartPropertyRowViewModel>());
                    break;
                case StereotypeKind.ValueProperty:
                    rows.AddRange(this.ContainedRows.OfType<ValuePropertyRowViewModel>());
                    break;
                case StereotypeKind.Port:
                    rows.AddRange(this.ContainedRows.OfType<PortRowViewModel>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stereotypeKind), "Stereotype not supported");
            }

            return rows;
        }
    }
}
