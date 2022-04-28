// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectObjectBaseRowViewModel.cs" company="RHEA System S.A.">
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
    using ReactiveUI;

    /// <summary>
    /// The <see cref="EnterpriseArchitectObjectBaseRowViewModel" /> represents the base of all rows view model to represents an
    /// Enterprise Architect Object
    /// </summary>
    public abstract class EnterpriseArchitectObjectBaseRowViewModel : ReactiveObject 
    {
        /// <summary>
        /// Backing field for <see cref="Name" />
        /// </summary>
        private string name;

        /// <summary>
        /// Backing field for <see cref="Value" />
        /// </summary>
        private string value;

        /// <summary>
        /// Backing field for <see cref="RowType" />
        /// </summary>
        private string rowType;

        /// <summary>
        /// The name of the represented object
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// The value of the represented object, if applicable
        /// </summary>
        public string Value
        {
            get => this.value;
            set => this.RaiseAndSetIfChanged(ref this.value, value);
        }

        /// <summary>
        /// The type of this row
        /// </summary>
        public string RowType
        {
            get => this.rowType;
            set => this.RaiseAndSetIfChanged(ref this.rowType, value);
        }

        /// <summary>
        /// The collection of contained <see cref="ContainedRows" />
        /// </summary>
        public ReactiveList<EnterpriseArchitectObjectBaseRowViewModel> ContainedRows { get; } = new();

        /// <summary>
        /// Compute the current row to initializes properties
        /// </summary>
        public abstract void ComputeRow();
    }
}
