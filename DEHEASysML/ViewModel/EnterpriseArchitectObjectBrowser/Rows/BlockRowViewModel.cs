﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlockRowViewModel.cs" company="RHEA System S.A.">
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
    using DEHEASysML.Extensions;

    using EA;

    /// <summary>
    /// The <see cref="BlockRowViewModel" /> represents an <see cref="Element"/> of Stereotype Block
    /// </summary>
    public class BlockRowViewModel : ElementRowViewModel
    {
        /// <summary>
        /// Initializes a new <see cref="BlockRowViewModel" />
        /// </summary>
        /// <param name="parent">The parent row</param>
        /// <param name="eaObject">The object to represent</param>
        /// <param name="shouldShowEverything">A value asserting if the row should display its contained <see cref="Element"/></param>
        public BlockRowViewModel(EnterpriseArchitectObjectBaseRowViewModel parent, Element eaObject, bool shouldShowEverything) 
            : base(parent, eaObject)
        {
            this.ShouldShowEverything = shouldShowEverything;
            this.Initialize();
        }

        /// <summary>
        /// Initializes this row properties
        /// </summary>
        private void Initialize()
        {
            this.UpdateProperties();
        }

        /// <summary>
        /// Compute the current row to initializes properties
        /// </summary>
        public override void ComputeRow()
        {
            if (this.ShouldShowEverything)
            {
                foreach (var valueProperty in this.RepresentedObject.GetAllValuePropertiesOfElement())
                {
                    this.ContainedRows.Add(new ValuePropertyRowViewModel(this, valueProperty));
                }

                foreach (var partProperty in this.RepresentedObject.GetAllPartPropertiesOfElement())
                {
                    this.ContainedRows.Add(new PartPropertyRowViewModel(this, partProperty));
                }
            }
        }
    }
}
