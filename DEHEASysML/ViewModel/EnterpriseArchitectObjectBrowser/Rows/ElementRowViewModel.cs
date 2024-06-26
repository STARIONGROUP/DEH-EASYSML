﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ElementRowViewModel.cs" company="RHEA System S.A.">
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
    using EA;

    /// <summary>
    /// The <see cref="ElementRowViewModel" /> represents a row for an <see cref="Element" />
    /// </summary>
    public abstract class ElementRowViewModel : EnterpriseArchitectObjectRowViewModel<Element>
    {
        /// <summary>
        /// Initializes a new <see cref="ElementRowViewModel" />
        /// </summary>
        /// <param name="parent">The parent row</param>
        /// <param name="eaObject">The object to represent</param>
        protected ElementRowViewModel(EnterpriseArchitectObjectBaseRowViewModel parent, Element eaObject)
            : base(parent, eaObject)
        {
            this.PackageId = eaObject.PackageID;
        }

        /// <summary>
        /// Update the current <see cref="Element" />
        /// </summary>
        /// <param name="element">The new <see cref="Element" /></param>
        public void UpdateElement(Element element)
        {
            this.ContainedRows.Clear();
            this.RepresentedObject = element;
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
    }
}
