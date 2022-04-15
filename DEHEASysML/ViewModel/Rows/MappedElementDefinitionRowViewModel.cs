// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappedElementDefinitionRowViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;

    using EA;

    /// <summary>
    /// The <see cref="MappedElementDefinitionRowViewModel" /> is the row view model that represents a mapping between
    /// an <see cref="ElementDefinition" /> and an <see cref="Element" />
    /// </summary>
    public class MappedElementDefinitionRowViewModel : MappedElementRowViewModel<ElementDefinition, Element>
    {
        /// <summary>
        /// Initializes a new <see cref="MappedElementDefinitionRowViewModel" />
        /// </summary>
        /// <param name="thing">The <see cref="ElementDefinition" /></param>
        /// <param name="dstElement">The <see cref="Element" /></param>
        /// <param name="mappingDirection">The <see cref="MappingDirection" /></param>
        public MappedElementDefinitionRowViewModel(ElementDefinition thing, Element dstElement, MappingDirection mappingDirection)
            : base(thing, dstElement, mappingDirection)
        {
        }
    }
}
