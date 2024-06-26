﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectRequirementElement.cs" company="RHEA System S.A.">
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
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;

    using EA;

    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    /// <summary>
    /// The <see cref="EnterpriseArchitectRequirementElement" /> represents a Requirement from EA.
    /// The purpose of this <see cref="EnterpriseArchitectRequirementElement" /> is to type the Requirement from EA.
    /// e.g. since the only obious differences between Blocks and Requirement for instance are from their names and stereotypes and
    /// there is no type difference in the EA API, they are both <see cref="Element" />
    /// </summary>
    public class EnterpriseArchitectRequirementElement : MappedRequirementRowViewModel
    {
        /// <summary>
        /// Initializes a new <see cref="EnterpriseArchitectRequirementElement" />
        /// </summary>
        /// <param name="thing">The <see cref="RequirementsSpecification" /></param>
        /// <param name="dstElement">The <see cref="Element" /></param>
        /// <param name="mappingDirection">The <see cref="MappingDirection" /></param>
        public EnterpriseArchitectRequirementElement(Requirement thing, Element dstElement, MappingDirection mappingDirection) 
            : base(thing, dstElement, mappingDirection)
        {
        }
    }
}
