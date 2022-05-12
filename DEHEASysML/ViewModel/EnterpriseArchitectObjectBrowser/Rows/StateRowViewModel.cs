// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StateRowViewModel.cs" company="RHEA System S.A.">
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
    using System.Linq;

    using DEHEASysML.Enumerators;

    using EA;

    /// <summary>
    /// The <see cref="StateRowViewModel" /> represents an <see cref="Element" /> of Type State
    /// </summary>
    public class StateRowViewModel : ElementRowViewModel
    {
        /// <summary>
        /// Initializes a new <see cref="StateRowViewModel" />
        /// </summary>
        /// <param name="parent">The parent row</param>
        /// <param name="eaObject">The object to represent</param>
        public StateRowViewModel(EnterpriseArchitectObjectBaseRowViewModel parent, Element eaObject) : base(parent, eaObject)
        {
            this.Initialize();
        }

        /// <summary>
        /// Compute the current row to initializes properties
        /// </summary>
        public override void ComputeRow()
        {
            this.RowType = StereotypeKind.State.ToString();

            foreach (var partition in this.RepresentedObject.Partitions.OfType<Partition>())
            {
                this.ContainedRows.Add(new PartitionRowViewModel(this, partition));
            }
        }

        /// <summary>
        /// Initializes this row properties
        /// </summary>
        private void Initialize()
        {
            this.UpdateProperties();
        }
    }
}
