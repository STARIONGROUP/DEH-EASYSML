// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommonBaseMappingRule.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.MappingRules
{
    using System.Collections.Generic;

    using Autofac;

    using CDP4Common.CommonData;

    using DEHEASysML.DstController;
    using DEHEASysML.Services.Cache;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingRules.Core;

    using NLog;

    /// <summary>
    /// The <see cref="CommonBaseMappingRule{TInput,TOuput}" /> provides some methods common for all
    /// <see cref="MappingRules" /> 
    /// </summary>
    public abstract class CommonBaseMappingRule<TInput, TOuput> : MappingRule<TInput, TOuput>
    {
        /// <summary>
        /// The current class logger
        /// </summary>
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IHubController" />
        /// </summary>
        protected readonly IHubController HubController = AppContainer.Container.Resolve<IHubController>();

        /// <summary>
        /// Gets the <see cref="ICacheService"/>
        /// </summary>
        protected ICacheService CacheService;

        /// <summary>
        /// The <see cref="IMappingConfigurationService" />
        /// </summary>
        protected IMappingConfigurationService MappingConfiguration { get; set; }

        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        public IDstController DstController { get; set; }

        /// <summary>
        /// Saves the mapping configuration
        /// </summary>
        /// <typeparam name="TThing">A <see cref="Thing" /></typeparam>
        /// <param name="mappedElements">A collection of <see cref="mappedElements" /></param>
        protected void SaveMappingConfiguration<TThing>(List<MappedElementRowViewModel<TThing>> mappedElements) where TThing : Thing
        {
            foreach (var mappedElement in mappedElements)
            {
                this.MappingConfiguration.AddToExternalIdentifierMap(mappedElement.HubElement.Iid, mappedElement.DstElement.ElementGUID,
                   mappedElement.MappingDirection);
            }
        }
    }
}
