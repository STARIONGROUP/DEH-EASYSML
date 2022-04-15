// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappedRowStatus.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Enumerators
{
    using DEHEASysML.ViewModel.Rows;

    /// <summary>
    /// Represents the status of the <see cref="IMappedElementRowViewModel"/>
    /// </summary>
    public enum MappedRowStatus
    {
        /// <summary>
        /// Default Value
        /// </summary>
        None,

        /// <summary>
        /// Used when the <see cref="IMappedElementRowViewModel" /> was already mapped
        /// </summary>
        ExistingMapping,

        /// <summary>
        /// Used when the <see cref="IMappedElementRowViewModel" /> will create a new Element
        /// </summary>
        NewElement,

        /// <summary>
        /// Used when the <see cref="IMappedElementRowViewModel" /> will be mapped to an existing Element
        /// </summary>
        ExistingElement
    }
}
