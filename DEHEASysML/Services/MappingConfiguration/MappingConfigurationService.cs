// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationService.cs" company="RHEA System S.A.">
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
    using System.Linq;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal.Operations;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using Newtonsoft.Json;

    using NLog;

    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    /// <summary>
    /// The <see cref="MappingConfigurationService" /> takes care of handling all operation
    /// related to saving and loading configured mapping.
    /// </summary>
    public class MappingConfigurationService : IMappingConfigurationService
    {
        /// <summary>
        /// The collection of id correspondence as tuple
        /// (<see cref="Guid" /> InternalId, <see cref="ExternalIdentifier" /> externalIdentifier, <see cref="Guid" /> Iid)
        /// including the deserialized external identifier
        /// </summary>
        private readonly List<(Guid InternalId, ExternalIdentifier ExternalIdentifier, Guid Iid)> correspondences = new();

        /// <summary>
        /// Gets the current class logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IHubController" />
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel" />
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// Backing field for <see cref="ExternalIdentifierMap" />
        /// </summary>
        private ExternalIdentifierMap externalIdentifierMap;

        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationService" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel" /></param>
        public MappingConfigurationService(IHubController hubController, IStatusBarControlViewModel statusBar)
        {
            this.hubController = hubController;
            this.statusBar = statusBar;

            this.ExternalIdentifierMap = new ExternalIdentifierMap();
        }

        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap" />
        /// </summary>
        public ExternalIdentifierMap ExternalIdentifierMap
        {
            get => this.externalIdentifierMap;
            set
            {
                this.externalIdentifierMap = value;
                this.ParseIdCorrespondence();
            }
        }

        /// <summary>
        /// Get a value indicating wheter the current <see cref="ExternalIdentifierMap" /> is the default one
        /// </summary>
        public bool IsTheCurrentIdentifierMapTemporary => this.ExternalIdentifierMap.Iid == Guid.Empty
                                                          && string.IsNullOrWhiteSpace(this.ExternalIdentifierMap.Name);

        /// <summary>
        /// Creates the <see cref="ExternalIdentifierMap" />
        /// </summary>
        /// <param name="newName">
        /// The model name to use for creating the new
        /// <see cref="ExternalIdentifierMap" />
        /// </param>
        /// <param name="modelName">The name of the model</param>
        /// <param name="addTheTemporyMapping">a value indicating whether the current temporary should be transfered to new one</param>
        /// <returns>A newly created <see cref="ExternalIdentifierMap" /></returns>
        public ExternalIdentifierMap CreateExternalIdentifierMap(string newName, string modelName, bool addTheTemporyMapping)
        {
            var newExternalIdentifierMap = new ExternalIdentifierMap
            {
                Name = newName,
                ExternalToolName = DstController.ThisToolName,
                ExternalModelName = string.IsNullOrWhiteSpace(modelName) ? newName : modelName,
                Owner = this.hubController.CurrentDomainOfExpertise
            };

            if (addTheTemporyMapping)
            {
                newExternalIdentifierMap.Correspondence.AddRange(this.ExternalIdentifierMap.Correspondence);
            }

            return newExternalIdentifierMap;
        }

        /// <summary>
        /// Updates the configured mapping, registering the <see cref="ExternalIdentifierMap" /> and
        /// its <see cref="IdCorrespondence" />
        /// to a <see name="IThingTransaction" />
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction" /></param>
        /// <param name="iterationClone">The <see cref="Iteration" /> clone</param>
        public void PersistExternalIdentifierMap(IThingTransaction transaction, Iteration iterationClone)
        {
            if (this.IsTheCurrentIdentifierMapTemporary)
            {
                this.logger.Error($"The current mapping with {this.ExternalIdentifierMap.Correspondence.Count} correspondences will not be saved as it is temporary");
                return;
            }

            if (this.ExternalIdentifierMap.Iid == Guid.Empty)
            {
                this.ExternalIdentifierMap = this.ExternalIdentifierMap.Clone(true);
                this.ExternalIdentifierMap.Iid = Guid.NewGuid();
                iterationClone.ExternalIdentifierMap.Add(this.ExternalIdentifierMap);
            }

            foreach (var correspondence in this.ExternalIdentifierMap.Correspondence)
            {
                if (correspondence.Iid == Guid.Empty)
                {
                    correspondence.Iid = Guid.NewGuid();
                    transaction.Create(correspondence);
                }
                else
                {
                    transaction.CreateOrUpdate(correspondence);
                }
            }

            transaction.CreateOrUpdate(this.ExternalIdentifierMap);

            this.statusBar.Append("Mapping configuration processed");
        }

        /// <summary>
        /// Refreshes the <see cref="ExternalIdentifierMap" /> usually done after a session write
        /// </summary>
        public void RefreshExternalIdentifierMap()
        {
            if (this.IsTheCurrentIdentifierMapTemporary)
            {
                return;
            }

            this.hubController.GetThingById(this.ExternalIdentifierMap.Iid, this.hubController.OpenIteration, out ExternalIdentifierMap map);
            this.ExternalIdentifierMap = map.Clone(true);
        }

        /// <summary>
        /// Loads the mapping configuration from dst to hub and generates the map result respectively
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of <see cref="IMappedElementRowViewModel" /></returns>
        public List<IMappedElementRowViewModel> LoadMappingFromDstToHub(Repository repository)
        {
            return this.LoadMapping(this.MapElementsFromTheExternalIdentifierMapToHub, repository);
        }

        /// <summary>
        /// Loads the mapping configuration from hub to dst and generates the map result respectively
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of <see cref="IMappedElementRowViewModel" /></returns>
        public List<IMappedElementRowViewModel> LoadMappingFromHubToDst(Repository repository)
        {
            return this.LoadMapping(this.MapElementsFromTheExternalIdentifierMapToDst, repository); 
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="ExternalIdentifierMap" />
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId" /> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId" /> corresponds to</param>
        /// <param name="mappingDirection">The <see cref="MappingDirection" /> the mapping belongs</param>
        public void AddToExternalIdentifierMap(Guid internalId, string externalId, MappingDirection mappingDirection)
        {
            this.AddToExternalIdentifierMap(internalId, new ExternalIdentifier
            {
                Identifier = externalId,
                MappingDirection = mappingDirection
            });
        }

        /// <summary>
        /// Adds one correspondence to the <see cref="ExternalIdentifierMap" />
        /// </summary>
        /// <param name="internalId">The thing that <paramref name="externalIdentifier" /> corresponds to</param>
        /// <param name="externalIdentifier">The external thing that <see cref="internalId" /> corresponds to</param>
        public void AddToExternalIdentifierMap(Guid internalId, ExternalIdentifier externalIdentifier)
        {
            var (_, _, correspondenceIid) = this.correspondences.FirstOrDefault(x =>
                x.InternalId == internalId
                && externalIdentifier.Identifier.Equals(x.ExternalIdentifier.Identifier)
                && externalIdentifier.MappingDirection == x.ExternalIdentifier.MappingDirection);

            if (correspondenceIid != Guid.Empty
                && this.ExternalIdentifierMap.Correspondence.FirstOrDefault(x => x.Iid == correspondenceIid)
                    is { } correspondence)
            {
                correspondence.InternalThing = internalId;
                correspondence.ExternalId = JsonConvert.SerializeObject(externalIdentifier);
                return;
            }

            this.ExternalIdentifierMap.Correspondence.Add(new IdCorrespondence
            {
                ExternalId = JsonConvert.SerializeObject(externalIdentifier),
                InternalThing = internalId
            });
        }

        /// <summary>
        /// Maps the <see cref="IMappedElementRowViewModel" />s defined in the <see cref="ExternalIdentifierMap" />
        /// for the <see cref="MappingDirection.FromDstToHub"/>
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of <see cref="IMappedElementRowViewModel" /></returns>
        private List<IMappedElementRowViewModel> MapElementsFromTheExternalIdentifierMapToHub(Repository repository)
        {
            var mappedElements = new List<IMappedElementRowViewModel>();

            foreach (var idCorrespondences in this.correspondences
                         .Where(x => x.ExternalIdentifier.MappingDirection == MappingDirection.FromDstToHub)
                         .GroupBy(x => x.ExternalIdentifier.Identifier))
            {
                var element = repository.GetElementByGuid(idCorrespondences.Key);

                if (element == null)
                {
                    continue;
                }

                if (element.Stereotype.AreEquals(StereotypeKind.Block))
                {
                    if (!this.hubController.GetThingById(idCorrespondences.First().InternalId, this.hubController.OpenIteration,
                            out ElementDefinition elementDefinition))
                    {
                        continue;
                    }

                    mappedElements.Add(new EnterpriseArchitectBlockElement(elementDefinition.Clone(true), element, MappingDirection.FromDstToHub));
                }
                else if (element.Stereotype.AreEquals(StereotypeKind.Requirement))
                {
                    if (!this.hubController.GetThingById(idCorrespondences.First().InternalId, this.hubController.OpenIteration,
                            out Requirement requirement))
                    {
                        continue;
                    }

                    mappedElements.Add(new EnterpriseArchitectRequirementElement(requirement.Clone(true), element, MappingDirection.FromDstToHub));
                }
            }

            return mappedElements;
        }

        /// <summary>
        /// Maps the <see cref="IMappedElementRowViewModel" />s defined in the <see cref="ExternalIdentifierMap" />
        /// for the <see cref="MappingDirection.FromHubToDst"/>
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of <see cref="IMappedElementRowViewModel" /></returns>
        private List<IMappedElementRowViewModel> MapElementsFromTheExternalIdentifierMapToDst(Repository repository)
        {
            var mappedElements = new List<IMappedElementRowViewModel>();

            foreach (var idCorrespondences in this.correspondences
                         .Where(x => x.ExternalIdentifier.MappingDirection == MappingDirection.FromHubToDst)
                         .GroupBy(x => x.ExternalIdentifier.Identifier))
            {
                var element = repository.GetElementByGuid(idCorrespondences.Key);

                if (element == null)
                {
                    continue;
                }

                if (element.Stereotype.AreEquals(StereotypeKind.Block))
                {
                    if (!this.hubController.GetThingById(idCorrespondences.First().InternalId, this.hubController.OpenIteration,
                            out ElementDefinition elementDefinition))
                    {
                        continue;
                    }

                    mappedElements.Add(new ElementDefinitionMappedElement(elementDefinition.Clone(true), element, MappingDirection.FromHubToDst));
                }
                else if (element.Stereotype.AreEquals(StereotypeKind.Requirement))
                {
                    if (!this.hubController.GetThingById(idCorrespondences.First().InternalId, this.hubController.OpenIteration,
                            out Requirement requirement))
                    {
                        continue;
                    }

                    mappedElements.Add(new RequirementMappedElement(requirement.Clone(true), element, MappingDirection.FromHubToDst));
                }
            }

            return mappedElements;
        }

        /// <summary>
        /// Calls the specify load mapping function
        /// <param name="loadMappingFunction"></param>
        /// </summary>
        /// <typeparam name="TViewModel">The type of row view model to return depending on the mapping direction</typeparam>
        /// <param name="loadMappingFunction">The specific load mapping <see cref="Func{TInput,TResult}" /></param>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of <typeparamref name="TViewModel" /></returns>
        private List<TViewModel> LoadMapping<TViewModel>(Func<Repository, List<TViewModel>> loadMappingFunction, Repository repository)
        {
            return this.ExternalIdentifierMap.Correspondence.Any() ? loadMappingFunction(repository) : default;
        }

        /// <summary>
        /// Parses the <see cref="ExternalIdentifierMap" /> correspondences and adds it to the <see cref="correspondences" />
        /// collection
        /// </summary>
        private void ParseIdCorrespondence()
        {
            this.correspondences.Clear();

            this.correspondences.AddRange(this.ExternalIdentifierMap.Correspondence.Select(x =>
            (
                x.InternalThing, JsonConvert.DeserializeObject<ExternalIdentifier>(x.ExternalId ?? string.Empty), x.Iid
            )));
        }
    }
}
