// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMappedElementRowViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.Rows
{
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;

    using DEHEASysML.Enumerators;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for <see cref="MappedElementRowViewModel{TThing}"/>
    /// </summary>
    public interface IMappedElementRowViewModel
    {
        /// <summary>
        /// Gets or sets the <see cref="MappedRowStatus"/>
        /// </summary>
        MappedRowStatus MappedRowStatus { get; set; }

        /// <summary>
        /// The name of the Target Element
        /// </summary>
        string TargetElementName { get; set; }

        /// <summary>
        /// The name of the Source Element
        /// </summary>
        string SourceElementName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating wheter this row represents a mapping done to a new element
        /// </summary>
        bool ShouldCreateNewTargetElement { get; set; }

        /// <summary>
        /// A collection of <see cref="BinaryRelationship" />
        /// </summary>
        List<BinaryRelationship> RelationShips { get; }

        /// <summary>
        /// A collection of <see cref="object"/> for the contained rows
        /// </summary>
        ReactiveList<object> ContainedRows { get; }
    }
}
