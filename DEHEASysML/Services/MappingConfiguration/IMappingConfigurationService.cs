// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMappingConfigurationService.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Services.MappingConfiguration
{
    using System;
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal.Operations;

    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;

    using EA;

    /// <summary>
    /// Interface definition for <see cref="MappingConfigurationService" />
    /// </summary>
    public interface IMappingConfigurationService
    {
        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap" />
        /// </summary>
        ExternalIdentifierMap ExternalIdentifierMap { get; set; }

        /// <summary>
        /// Get a value indicating wheter the current <see cref="ExternalIdentifierMap" /> is the default one
        /// </summary>
        bool IsTheCurrentIdentifierMapTemporary { get; }

        /// <summary>
        /// Creates the <see cref="MappingConfigurationService.ExternalIdentifierMap" />
        /// </summary>
        /// <param name="newName">
        /// The model name to use for creating the new
        /// <see cref="MappingConfigurationService.ExternalIdentifierMap" />
        /// </param>
        /// <param name="modelName">The name of the model</param>
        /// <param name="addTheTemporyMapping">a value indicating whether the current temporary should be transfered to new one</param>
        /// <returns>A newly created <see cref="MappingConfigurationService.ExternalIdentifierMap" /></returns>
        ExternalIdentifierMap CreateExternalIdentifierMap(string newName, string modelName, bool addTheTemporyMapping);

        /// <summary>
        /// Adds one correspondance to the <see cref="MappingConfigurationService.ExternalIdentifierMap" />
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId" /> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId" /> corresponds to</param>
        /// <param name="mappingDirection">The <see cref="MappingDirection" /> the mapping belongs</param>
        void AddToExternalIdentifierMap(Guid internalId, string externalId, MappingDirection mappingDirection);

        /// <summary>
        /// Updates the configured mapping, registering the <see cref="MappingConfigurationService.ExternalIdentifierMap" /> and
        /// its <see cref="IdCorrespondence" />
        /// to a <see name="IThingTransaction" />
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction" /></param>
        /// <param name="iterationClone">The <see cref="Iteration" /> clone</param>
        void PersistExternalIdentifierMap(IThingTransaction transaction, Iteration iterationClone);

        /// <summary>
        /// Refreshes the <see cref="MappingConfigurationService.ExternalIdentifierMap" /> usually done after a session write
        /// </summary>
        void RefreshExternalIdentifierMap();

        /// <summary>
        /// Loads the mapping configuration from dst to hub and generates the map result respectively
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of <see cref="IMappedElementRowViewModel" /></returns>
        List<IMappedElementRowViewModel> LoadMappingFromDstToHub(Repository repository);
    }
}
