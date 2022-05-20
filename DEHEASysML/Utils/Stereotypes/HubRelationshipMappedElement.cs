// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubRelationshipMappedElement.cs" company="RHEA System S.A.">
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
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.MappingEngine;

    using EA;

    /// <summary>
    /// The <see cref="HubRelationshipMappedElement"/> is usable by the <see cref="MappingEngine"/> during a mapping in the
    /// <see cref="MappingDirection.FromHubToDst"/>
    ///
    /// Used to created <see cref="Connector"/> based on <see cref="BinaryRelationship"/>
    /// </summary>
    public class HubRelationshipMappedElement : MappedElementRowViewModel<Thing>
    {
        /// <summary>
        /// Initializes a new <see cref="HubRelationshipMappedElement"/>
        /// </summary>
        /// <param name="mappedElement">A <see cref="ElementDefinitionMappedElement"/></param>
        public HubRelationshipMappedElement(ElementDefinitionMappedElement mappedElement)
            : base(mappedElement.HubElement, mappedElement.DstElement, mappedElement.MappingDirection)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="HubRelationshipMappedElement"/>
        /// </summary>
        /// <param name="mappedElement">A <see cref="RequirementMappedElement"/></param>
        public HubRelationshipMappedElement(RequirementMappedElement mappedElement)
            : base(mappedElement.HubElement, mappedElement.DstElement, mappedElement.MappingDirection)
        {
        }
    }
}
