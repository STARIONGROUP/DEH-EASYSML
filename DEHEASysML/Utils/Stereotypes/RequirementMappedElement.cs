// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementMappedElement.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Utils.Stereotypes
{
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;

    using EA;

    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    /// <summary>
    /// The <see cref="RequirementMappedElement" /> represents an <see cref="Requirement"/> from COMET to EA.
    /// The purpose of this <see cref="RequirementMappedElement" /> is to type the Bloc from EA
    /// and distinguish it to <see cref="EnterpriseArchitectRequirementElement"/>.
    /// e.g. since the only obious differences between Blocks and Requirement for instance are from their names and stereotypes and
    /// there is no type difference in the EA API, they are both <see cref="Element" />
    /// </summary>
    public class RequirementMappedElement : MappedRequirementRowViewModel
    {
        /// <summary>
        /// Initializes a new <see cref="RequirementMappedElement" />
        /// </summary>
        /// <param name="thing">The <see cref="CDP4Common.EngineeringModelData.Requirement" /></param>
        /// <param name="dstElement">The <see cref="Element" /></param>
        /// <param name="mappingDirection">The <see cref="MappingDirection" /></param>
        public RequirementMappedElement(Requirement thing, Element dstElement, MappingDirection mappingDirection)
            : base(thing, dstElement, mappingDirection)
        {
        }
    }
}
