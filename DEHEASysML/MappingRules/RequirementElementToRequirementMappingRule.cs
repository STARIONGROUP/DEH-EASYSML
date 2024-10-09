// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementElementToRequirementMappingRule.cs" company="RHEA System S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.ExceptionServices;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;
    using DEHEASysML.Services.Cache;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;

    using EA;

    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    /// <summary>
    /// The <see cref="RequirementElementToRequirementMappingRule" /> is a <see cref="IMappingRule" /> for the
    /// <see cref="MappingEngine" />
    /// that takes a <see cref="List{T}" /> of <see cref="EnterpriseArchitectRequirementElement" /> as input and
    /// outputs a E-TM-10-25
    /// <see cref="CDP4Common.EngineeringModelData.Requirement" />
    /// </summary>
    public class RequirementElementToRequirementMappingRule :
        DstToHubBaseMappingRule<(bool completeMapping, List<EnterpriseArchitectRequirementElement> inputs), List<MappedRequirementRowViewModel>>
    {
        /// <summary>
        /// A collection of <see cref="RequirementsSpecification" />
        /// </summary>
        private readonly List<RequirementsSpecification> requirementsSpecifications = [];

        /// <summary>
        /// A collection of <see cref="RequirementsGroup" />
        /// </summary>
        private readonly List<RequirementsGroup> requirementsGroups = [];

        /// <summary>
        /// The category for BinaryRelation between requirements
        /// </summary>
        private readonly (string shortname, string name) requirementRelationshipCategoryNames = ("refineRelationship", "refine");

        /// <summary>
        /// Colletion of <see cref="EnterpriseArchitectRequirementElement" />
        /// </summary>
        private List<EnterpriseArchitectRequirementElement> mappedElements;

        /// <summary>
        /// Stores the mapping between the id of a <see cref="Package"/> to a <see cref="RequirementsContainer"/>
        /// </summary>
        private readonly Dictionary<int, RequirementsContainer> packageMapping = [];

        /// <summary>
        /// Transform a <see cref="List{T}" /> of <see cref="EnterpriseArchitectRequirementElement" /> into a
        /// <see cref="List{T}" /> of
        /// <see cref="MappedRequirementRowViewModel" />
        /// </summary>
        /// <param name="input">
        /// A Tuple of <see cref="bool" />, <see cref="List{T}" /> of <see cref="EnterpriseArchitectRequirementElement " />
        /// The <see cref="bool" /> handles the fact that it the mapping has to map everything
        /// </param>
        /// <returns>A collection of <see cref="MappedRequirementRowViewModel" /></returns>
        public override List<MappedRequirementRowViewModel> Transform((bool completeMapping, List<EnterpriseArchitectRequirementElement> inputs) input)
        {
            try
            {
                if (!this.HubController.IsSessionOpen)
                {
                    return default;
                }

                this.Owner = this.HubController.CurrentDomainOfExpertise;
                this.MappingConfiguration ??= AppContainer.Container.Resolve<IMappingConfigurationService>();
                this.CacheService ??= AppContainer.Container.Resolve<ICacheService>();
                this.DstController ??= AppContainer.Container.Resolve<IDstController>();
                var (completeMapping, elements) = input;

                this.requirementsSpecifications.Clear();
                this.requirementsGroups.Clear();

                foreach (var requirementsSpecification in this.HubController.OpenIteration
                             .RequirementsSpecification.Where(x => !x.IsDeprecated))
                {
                    var requirementsSpecificationsClone = requirementsSpecification.Clone(true);
                    this.requirementsSpecifications.Add(requirementsSpecificationsClone);
                    this.PopulateRequirementsGroupCollection(requirementsSpecificationsClone);
                }

                var mappingStopWatch = Stopwatch.StartNew();

                this.mappedElements = [..elements];
                this.packageMapping.Clear();

                if (!this.ComputeMappingToRequirementsContainer(this.mappedElements))
                {
                    return default;
                }

                foreach (var mappedElement in this.mappedElements)
                {
                    this.MapRequirement(mappedElement);
                }

                if (completeMapping)
                {
                    this.MapCategories();
                    this.CreateRelationShips();
                    this.SaveMappingConfiguration([..this.mappedElements]);
                }

                mappingStopWatch.Stop();
                this.Logger.Info("{0} for Requirements done in {1}[ms]", completeMapping? "Mapping" : "Premapping", mappingStopWatch.ElapsedMilliseconds);

                return [..this.mappedElements];
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Compute the complete mapping from <see cref="Package"/> to <see cref="RequirementsContainer"/>
        /// </summary>
        /// <param name="enterpriseArchitectRequirementElements">The collection of <see cref="EnterpriseArchitectRequirementElement"/></param>
        /// <returns>Asserts that the mapping has been successful</returns>
        private bool ComputeMappingToRequirementsContainer(IEnumerable<EnterpriseArchitectRequirementElement> enterpriseArchitectRequirementElements)
        {
            var usedPackages = enterpriseArchitectRequirementElements.Select(x => x.DstElement.PackageID).Distinct();

            foreach (var usedPackage in usedPackages)
            {
                if (this.packageMapping.ContainsKey(usedPackage))
                {
                    continue;
                }

                var currentPackage = this.DstController.CurrentRepository.GetPackageByID(usedPackage);
                var parentPackage = this.DstController.CurrentRepository.GetPackageByID(currentPackage.ParentID);

                if (parentPackage.ParentID == 0)
                {
                    if (!this.TryGetOrCreateRequirementsSpecification(currentPackage, out var requirementsSpecification))
                    {
                        this.Logger.Error($"Error during creation of the RequirementsSpecification for {currentPackage.Name} package");
                        return false;
                    }

                    this.packageMapping[usedPackage] = requirementsSpecification;
                }
                else
                {
                    RequirementsSpecification specification = null;
                    RequirementsGroup requirementsGroup = null;
                    var grandParentPackage = this.DstController.CurrentRepository.GetPackageByID(parentPackage.ParentID);

                    if (this.TryGetRequirementsSpecification(currentPackage.Name) is { } existingRequirementsSpecification)
                    {
                        this.packageMapping[currentPackage.PackageID] = existingRequirementsSpecification;
                        specification = existingRequirementsSpecification;
                    }
                    else
                    {
                        if (!this.TryGetOrCreateRequirementsGroup(currentPackage, out requirementsGroup))
                        {
                            this.Logger.Error($"Error during creation of the RequirementsGroup for {currentPackage.Name} package");
                            return false;
                        }

                        this.packageMapping[currentPackage.PackageID] = requirementsGroup;
                    }

                    if (grandParentPackage.ParentID != 0 && specification == null)
                    {
                        var greatGrandParentPackage = this.DstController.CurrentRepository.GetPackageByID(grandParentPackage.ParentID);

                        while (specification == null && greatGrandParentPackage.ParentID != 0 && !this.packageMapping.ContainsKey(parentPackage.PackageID))
                        {
                            currentPackage = parentPackage;
                            parentPackage = grandParentPackage;
                            grandParentPackage = greatGrandParentPackage;
                            greatGrandParentPackage = this.DstController.CurrentRepository.GetPackageByID(grandParentPackage.ParentID);

                            if (this.TryGetRequirementsSpecification(currentPackage.Name) is { } requirementsSpecification)
                            {
                                this.packageMapping[currentPackage.PackageID] = requirementsSpecification;
                                specification = requirementsSpecification;
                            }

                            else
                            {
                                if (!this.TryGetOrCreateRequirementsGroup(currentPackage, out var parentRequirementsGroup))
                                {
                                    this.Logger.Error($"Error during creation of the RequirementsGroup for {currentPackage.Name} package");
                                    return false;
                                }

                                this.packageMapping[currentPackage.PackageID] = parentRequirementsGroup;

                                parentRequirementsGroup.Group.RemoveAll(x => x.Iid == requirementsGroup.Iid);
                                parentRequirementsGroup.Group.Add(requirementsGroup);

                                requirementsGroup = parentRequirementsGroup;
                            }
                        }
                    }

                    if (!this.packageMapping.TryGetValue(parentPackage.PackageID, out var requirementsContainer))
                    {
                        if (!this.TryGetOrCreateRequirementsSpecification(parentPackage, out var requirementsSpecification))
                        {
                            this.Logger.Error($"Error during creation of the RequirementsSpecification for {parentPackage.Name} package");
                            return false;
                        }

                        requirementsContainer = requirementsSpecification;
                        this.packageMapping[parentPackage.PackageID] = requirementsSpecification;
                    }

                    if (requirementsGroup != null)
                    {
                        requirementsGroup.Group.RemoveAll(x => x.Iid == requirementsGroup.Iid);
                        requirementsContainer.Group.Add(requirementsGroup);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Maps all stereotypes to <see cref="Category"/> for all <see cref="Requirement" />
        /// </summary>
        private void MapCategories()
        {
            foreach (var mappedElement in this.mappedElements)
            {
                this.MapStereotypesToCategory(mappedElement.DstElement, mappedElement.HubElement);
            }
        }

        /// <summary>
        /// Populate the <see cref="requirementsGroups"/> collection
        /// </summary>
        /// <param name="container">The <see cref="RequirementsContainer"/></param>
        private void PopulateRequirementsGroupCollection(RequirementsContainer container)
        {
            this.requirementsGroups.AddRange(container.Group);

            foreach (var requirementsGroup in container.Group)
            {
                this.PopulateRequirementsGroupCollection(requirementsGroup);
            }
        }

        /// <summary>
        /// Creates all <see cref="BinaryRelationship" /> between the mapped
        /// <see cref="CDP4Common.EngineeringModelData.Requirement" />s
        /// </summary>
        private void CreateRelationShips()
        {
            foreach (var mappedElement in this.mappedElements)
            {
                this.CreateRelationShips(mappedElement);
            }
        }

        /// <summary>
        /// Create all <see cref="BinaryRelationship" /> for the given
        /// <see cref="EnterpriseArchitectRequirementElement" />
        /// </summary>
        /// <param name="mappedElement">The <see cref="EnterpriseArchitectRequirementElement" /></param>
        private void CreateRelationShips(EnterpriseArchitectRequirementElement mappedElement)
        {
            if (mappedElement.HubElement == null)
            {
                return;
            }

            var requirementsConnectors = this.CacheService.GetConnectorsOfElement(mappedElement.DstElement.ElementID)
                .Where(x => x.Stereotype.AreEquals(StereotypeKind.DeriveReqt));

            foreach (var connector in requirementsConnectors)
            {
                var (source, target) = this.DstController.ResolveConnector(connector);

                if (source == null || target == null)
                {
                    continue;
                }

                if (!(this.TryGetRequirement(source, out var sourceRequirement) && this.TryGetRequirement(target, out var targetRequirement)))
                {
                    continue;
                }

                var relationShipName = $"{sourceRequirement.ShortName} -> {targetRequirement.ShortName}";

                var relationShip = this.HubController.OpenIteration.Relationship.OfType<BinaryRelationship>()
                                       .FirstOrDefault(x => x.Name == relationShipName && x.Target.Iid
                                           == targetRequirement.Iid && x.Source.Iid == sourceRequirement.Iid)?
                                       .Clone(false)
                    ?? this.CreateBinaryRelationShip(sourceRequirement, targetRequirement,
                                       relationShipName, this.requirementRelationshipCategoryNames, false);

                mappedElement.RelationShips.Add(relationShip);
            }
        }

        /// <summary>
        /// Try to get an existing <see cref="Requirement" />
        /// </summary>
        /// <param name="requirementElement">The <see cref="Element" /></param>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <returns>Asserts if the <see cref="Requirement" /> has been found</returns>
        private bool TryGetRequirement(Element requirementElement, out Requirement requirement)
        {
            var requirementShortname = requirementElement.GetRequirementId();

            requirement = this.requirementsSpecifications.SelectMany(x => x.Requirement)
                .FirstOrDefault(x => !x.IsDeprecated && x.ShortName == requirementShortname);

            return requirement != null;
        }

        /// <summary>
        /// Maps the provided <see cref="EnterpriseArchitectRequirementElement" />
        /// </summary>
        /// <param name="mappedElement">The <see cref="EnterpriseArchitectRequirementElement" /> to map</param>
        private void MapRequirement(EnterpriseArchitectRequirementElement mappedElement)
        {
            if (!mappedElement.ShouldCreateNewTargetElement && mappedElement.HubElement != null)
            {
                UpdateRequirementProperties(mappedElement.DstElement, mappedElement.HubElement);
                var requirementSpecification = (mappedElement.HubElement.Container as RequirementsSpecification)!.Clone(true);
                requirementSpecification.Requirement.RemoveAll(x => x.Iid == mappedElement.HubElement.Iid);
                requirementSpecification.Requirement.Add(mappedElement.HubElement);
                return;
            }

            if (!this.TryGetOrCreateRequirement(mappedElement.DstElement, out var requirement))
            {
                this.Logger.Error($"Error during creation of the Requirement for {mappedElement.DstElement.Name} package");
                return;
            }

            var container = this.packageMapping[mappedElement.DstElement.PackageID];

            RequirementsSpecification requirementsSpecification;

            switch (container)
            {
                case RequirementsSpecification specification:
                    requirementsSpecification = specification;
                    break;
                case RequirementsGroup requirementsGroup:
                    requirement.Group = requirementsGroup;
                    requirementsSpecification = requirementsGroup.GetContainerOfType<RequirementsSpecification>();
                    break;
                default:
                    throw new InvalidOperationException("Container is neither a RequirementsSpecification or a RequirementsGroup");
            }

            requirementsSpecification.Requirement.RemoveAll(x => x.Iid == requirement.Iid);
            requirementsSpecification.Requirement.Add(requirement);

            mappedElement.HubElement = requirement;
            mappedElement.ShouldCreateNewTargetElement = mappedElement.HubElement.Original == null;
        }

        /// <summary>
        /// Tries to get a existing <see cref="RequirementsGroup" /> or created one based on the <see cref="Package" />
        /// </summary>
        /// <param name="package">The <see cref="Package" /></param>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /></param>
        /// <returns>A value indicating whether the <see cref="RequirementsSpecification" /> has been created or retrieved</returns>
        private bool TryGetOrCreateRequirementsSpecification(IDualPackage package, out RequirementsSpecification requirementsSpecification)
        {
            return this.TryGetOrCreateRequirementsSpecification(package.Name, out requirementsSpecification);
        }

        /// <summary>
        /// Tries to get a existing <see cref="RequirementsSpecification" /> or created one based on the name
        /// </summary>
        /// <param name="requirementsSpecificationName">The name</param>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /></param>
        /// <returns>A value indicating whether the <see cref="RequirementsSpecification" /> has been created or retrieved</returns>
        private bool TryGetOrCreateRequirementsSpecification(string requirementsSpecificationName, out RequirementsSpecification requirementsSpecification)
        {
            var alreadyCreated = this.TryGetRequirementsSpecification(requirementsSpecificationName) ?? new RequirementsSpecification
            {
                Iid = Guid.NewGuid(),
                Name = requirementsSpecificationName,
                ShortName = requirementsSpecificationName.GetShortName(),
                Owner = this.Owner
            };

            this.requirementsSpecifications.RemoveAll(x => x.Iid == alreadyCreated.Iid);
            this.requirementsSpecifications.Add(alreadyCreated);
            this.requirementsGroups.AddRange(alreadyCreated.GetAllContainedGroups());

            requirementsSpecification = alreadyCreated;

            return requirementsSpecification != null;
        }

        /// <summary>
        /// Tries to retrieve a <see cref="RequirementsSpecification" /> based on a name
        /// </summary>
        /// <param name="requirementsSpecificationName">The name of the <see cref="RequirementsSpecification"/></param>
        /// <returns>The <see cref="RequirementsSpecification"/> if found</returns>
        private RequirementsSpecification TryGetRequirementsSpecification(string requirementsSpecificationName)
        {
            var predicate = MatchingRequirementsSpecificationPredicate(requirementsSpecificationName);

            return this.requirementsSpecifications.Find(predicate) ?? this.HubController.OpenIteration.RequirementsSpecification.Find(predicate)?.Clone(true);
        }

        /// <summary>
        /// Tries to get a existing <see cref="RequirementsGroup" /> or created one based on the <see cref="Package" />
        /// </summary>
        /// <param name="package">The <see cref="Package" /></param>
        /// <param name="requirementsGroup">The <see cref="RequirementsGroup" /></param>
        /// <returns>A value indicating whether the <see cref="RequirementsGroup" /> has been created or retrieved</returns>
        private bool TryGetOrCreateRequirementsGroup(IDualPackage package, out RequirementsGroup requirementsGroup)
        {
            var createdRequirementsGroup = this.requirementsGroups.Find(x =>
                string.Equals(x.ShortName, package.Name.GetShortName(), StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(x.Name, package.Name, StringComparison.InvariantCultureIgnoreCase));

            if (createdRequirementsGroup != null)
            {
                requirementsGroup = createdRequirementsGroup;
            }
            else
            {
                requirementsGroup = new RequirementsGroup
                {
                    Name = package.Name,
                    ShortName = package.Name.GetShortName(),
                    Iid = Guid.NewGuid(),
                    Owner = this.Owner
                };

                this.requirementsGroups.Add(requirementsGroup);
            }

            return requirementsGroup != null;
        }

        /// <summary>
        /// Tries to get a existing <see cref="Requirement" /> or created one based on the <see cref="Element" />
        /// </summary>
        /// <param name="requirementElement">The <see cref="Element" /></param>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <returns>A value indicating whether the <see cref="Requirement" /> has been created or retrieved</returns>
        private bool TryGetOrCreateRequirement(Element requirementElement, out Requirement requirement)
        {
            var requirementShortname = requirementElement.GetRequirementId();

            if (this.TryGetRequirement(requirementElement, out var alreadyCreatedRequirement))
            {
                requirement = alreadyCreatedRequirement;

                var requirementsSpecification = this.requirementsSpecifications
                    .First(x => x.Requirement.Contains(alreadyCreatedRequirement));

                requirementsSpecification.Requirement.Remove(alreadyCreatedRequirement);
            }
            else
            {
                requirement = new Requirement
                {
                    Iid = Guid.NewGuid(),
                    Name = requirementElement.Name,
                    ShortName = requirementShortname,
                    Owner = this.Owner
                };
            }

            UpdateRequirementProperties(requirementElement, requirement);
            return requirement != null;
        }

        /// <summary>
        /// Update the properties of the <see cref="Requirement" />
        /// </summary>
        /// <param name="requirementElement">The <see cref="Element" /></param>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        private static void UpdateRequirementProperties(Element requirementElement, Requirement requirement)
        {
            UpdateOrCreateDefinition(requirementElement, requirement);
        }

        /// <summary>
        /// Updates or creates the defijntion according to the provided <see cref="Element" />
        /// </summary>
        /// <param name="requirementElement">The <see cref="Element" /></param>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        private static void UpdateOrCreateDefinition(Element requirementElement, Requirement requirement)
        {
            if (requirement == null)
            {
                return;
            }

            var definition = requirement.Definition.Find(x => string.Equals(x.LanguageCode, "en", StringComparison.InvariantCultureIgnoreCase))
                ?.Clone(true) ?? CreateDefinition();

            definition.Content = requirementElement.GetRequirementText();

            if (string.IsNullOrEmpty(definition.Content))
            {
                definition.Content = "-";
            }

            requirement.Definition.RemoveAll(x => x.Iid == definition.Iid);
            requirement.Definition.Add(definition);
        }

        /// <summary>
        /// Creates a new <see cref="Definition" />
        /// </summary>
        /// <returns>A <see cref="Definition" /></returns>
        private static Definition CreateDefinition()
        {
            return new Definition
            {
                Iid = Guid.NewGuid(),
                LanguageCode = "en"
            };
        }
        
        /// <summary>
        /// Predicate that is used to find a matching <see cref="RequirementsSpecification"/> based on its name
        /// </summary>
        /// <param name="requirementsSpecificationName">The name that should match</param>
        /// <returns>A <see cref="Predicate{T}"/></returns>
        private static Predicate<RequirementsSpecification> MatchingRequirementsSpecificationPredicate(string requirementsSpecificationName)
        {
            var shortName = requirementsSpecificationName.GetShortName();

            var predicate = new Predicate<RequirementsSpecification>(x => !x.IsDeprecated && (string.Equals(x.Name, requirementsSpecificationName, StringComparison.InvariantCultureIgnoreCase)
                                                                                              || string.Equals(x.ShortName, shortName, StringComparison.InvariantCultureIgnoreCase)));

            return predicate;
        }
    }
}
