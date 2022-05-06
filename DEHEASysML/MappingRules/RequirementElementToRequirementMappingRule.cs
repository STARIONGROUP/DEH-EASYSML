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
    using System.Linq;
    using System.Runtime.ExceptionServices;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHEASysML.DstController;
    using DEHEASysML.Extensions;
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
        private readonly List<RequirementsSpecification> requirementsSpecifications = new();

        /// <summary>
        /// A collection of <see cref="RequirementsGroup" />
        /// </summary>
        private readonly List<RequirementsGroup> requirementsGroups = new();

        /// <summary>
        /// The category for BinaryRelation between requirements
        /// </summary>
        private readonly (string shortname, string name) requirementCategoryNames = ("refineRelationship", "Requirement");

        /// <summary>
        /// Colletion of <see cref="EnterpriseArchitectRequirementElement" />
        /// </summary>
        private List<EnterpriseArchitectRequirementElement> mappedElements;

        /// <summary>
        /// The <see cref="Category" /> to apply to each <see cref="CDP4Common.EngineeringModelData.Requirement" />,
        /// <see cref="RequirementsGroup" /> and
        /// <see cref="RequirementsSpecification" />
        /// </summary>
        private Category requirementCategory;

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
                this.MappingConfiguration = AppContainer.Container.Resolve<IMappingConfigurationService>();
                this.DstController = AppContainer.Container.Resolve<IDstController>();
                var (completeMapping, elements) = input;

                this.HubController.TryGetThingBy(x => x.ShortName == this.requirementCategoryNames.shortname
                                                      && !x.IsDeprecated, ClassKind.Category, out this.requirementCategory);

                if (this.requirementCategory == null)
                {
                    this.TryCreateCategory(this.requirementCategoryNames, out this.requirementCategory, ClassKind.Requirement,
                        ClassKind.RequirementsGroup, ClassKind.RequirementsSpecification);
                }

                this.requirementsSpecifications.Clear();
                this.requirementsGroups.Clear();

                foreach (var requirementsSpecification in this.HubController.OpenIteration
                             .RequirementsSpecification.Where(x => !x.IsDeprecated))
                {
                    var requirementsSpecificationsClone = requirementsSpecification.Clone(true);
                    this.requirementsSpecifications.Add(requirementsSpecificationsClone);
                    this.PopulateRequirementsGroupCollection(requirementsSpecificationsClone);
                }

                this.mappedElements = new List<EnterpriseArchitectRequirementElement>(elements);

                foreach (var mappedElement in this.mappedElements)
                {
                    this.MapRequirement(mappedElement);
                }

                if (completeMapping)
                {
                    this.CreateRelationShips();
                    this.SaveMappingConfiguration(new List<MappedElementRowViewModel<Requirement>>(this.mappedElements));
                }

                return new List<MappedRequirementRowViewModel>(this.mappedElements);
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
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

            foreach (var connector in mappedElement.DstElement.GetRequirementsRelationShipConnectors())
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
                                       relationShipName, this.requirementCategoryNames, false);

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
                this.UpdateRequirementProperties(mappedElement.DstElement, mappedElement.HubElement);
                var requirementSpecification = mappedElement.HubElement.Container as RequirementsSpecification;
                requirementSpecification.Requirement.RemoveAll(x => x.Iid == mappedElement.HubElement.Iid);
                requirementSpecification.Requirement.Add(mappedElement.HubElement);
                return;
            }

            Requirement requirement;
            var packageParent = this.DstController.CurrentRepository.GetPackageByID(mappedElement.DstElement.PackageID);
            var packageGrandParent = this.DstController.CurrentRepository.GetPackageByID(packageParent.ParentID);

            if (packageGrandParent.ParentID == 0)
            {
                if (!this.TryGetOrCreateRequirementsSpecificationAndRequirement(mappedElement.DstElement, out requirement))
                {
                    this.Logger.Error($"Error during creation of the RequirementsSpecification for {mappedElement.DstElement.Name} requirement");
                }
            }
            else
            {
                if (!this.TryGetOrCreateRequirement(mappedElement.DstElement, out requirement))
                {
                    this.Logger.Error($"Error during creation of the Requirement for {mappedElement.DstElement.Name} package");
                    return;
                }

                var packageGreatGrandParent = this.DstController.CurrentRepository.GetPackageByID(packageGrandParent.ParentID);

                RequirementsSpecification requirementsSpecification;

                if (packageGreatGrandParent.ParentID == 0)
                {
                    if (!this.TryGetOrCreateRequirementsSpecification(packageParent, out requirementsSpecification))
                    {
                        this.Logger.Error($"Error during creation of the RequirementsSpecification for {packageParent.Name} package");
                        return;
                    }
                }
                else
                {
                    if (!this.ProcessPackageHierarchy(packageParent, requirement, out requirementsSpecification))
                    {
                        this.Logger.Error($"Error during creation of the RequirementsSpecification for {requirement.Name} requirement during the Process hierarchy");
                        return;
                    }
                }

                requirementsSpecification.Requirement.Add(requirement);
            }

            mappedElement.HubElement = requirement;
            mappedElement.ShouldCreateNewTargetElement = mappedElement.HubElement.Original == null;
        }

        /// <summary>
        /// Process the whole hierachy from the <see cref="Package" /> containing the Requirement to the root of the project to
        /// create
        /// <see cref="RequirementsGroup" /> and <see cref="RequirementsSpecification" />
        /// </summary>
        /// <param name="packageParent">The <see cref="Package" /></param>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /></param>
        /// <returns>Value representing if the whole hierarchy has been processed</returns>
        private bool ProcessPackageHierarchy(Package packageParent, Requirement requirement, out RequirementsSpecification requirementsSpecification)
        {
            requirementsSpecification = null;
            var packageGrandParent = this.DstController.CurrentRepository.GetPackageByID(packageParent.ParentID);
            var packageGreatGrandParent = this.DstController.CurrentRepository.GetPackageByID(packageGrandParent.ParentID);

            if (!this.TryGetOrCreateRequirementsGroup(packageParent, out var parentRequirementsGroup))
            {
                this.Logger.Error($"Error during creation of the RequirementsGroup for {packageParent.Name} package");
                return false;
            }

            requirement.Group = parentRequirementsGroup;

            while (packageGreatGrandParent.ParentID != 0)
            {
                packageParent = packageGrandParent;
                packageGrandParent = this.DstController.CurrentRepository.GetPackageByID(packageParent.ParentID);
                packageGreatGrandParent = this.DstController.CurrentRepository.GetPackageByID(packageGrandParent.ParentID);

                if (packageGreatGrandParent.ParentID == 0)
                {
                    continue;
                }

                if (!this.TryGetOrCreateRequirementsGroup(packageParent, out var newestRequirementsGroup))
                {
                    this.Logger.Error($"Error during creation of the RequirementsGroup for {packageParent.Name} package");
                    return false;
                }

                newestRequirementsGroup.Group.RemoveAll(x => x.Iid == parentRequirementsGroup.Iid);
                newestRequirementsGroup.Group.Add(parentRequirementsGroup);
                parentRequirementsGroup = newestRequirementsGroup;
            }

            if (!this.TryGetOrCreateRequirementsSpecification(packageParent, out requirementsSpecification))
            {
                this.Logger.Error($"Error during creation of the RequirementsSpecification for {packageGrandParent.Name} package");
                return false;
            }

            requirementsSpecification.Group.RemoveAll(x => x.Iid == parentRequirementsGroup.Iid);
            requirementsSpecification.Group.Add(parentRequirementsGroup);
            return true;
        }

        /// <summary>
        /// Tries to get a existing <see cref="RequirementsGroup" /> or created one based on the <see cref="Package" />
        /// </summary>
        /// <param name="package">The <see cref="Package" /></param>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /></param>
        /// <returns>A value indicating whether the <see cref="RequirementsSpecification" /> has been created or retrieved</returns>
        private bool TryGetOrCreateRequirementsSpecification(Package package, out RequirementsSpecification requirementsSpecification)
        {
            return this.TryGetOrCreateRequirementsSpecification(package.Name, out requirementsSpecification);
        }

        /// <summary>
        /// Tries to get a existing <see cref="Requirement" /> or created one based on the <see cref="Element" />
        /// It also creates the <see cref="Requirement" /> corresponding to the <see cref="Element" />
        /// </summary>
        /// <param name="requirementElement">The <see cref="Element" /></param>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <returns>A value indicating whether the <see cref="RequirementsSpecification" /> has been created or retrieved</returns>
        private bool TryGetOrCreateRequirementsSpecificationAndRequirement(Element requirementElement, out Requirement requirement)
        {
            if (!this.TryGetOrCreateRequirement(requirementElement, out requirement) ||
                !this.TryGetOrCreateRequirementsSpecification(requirementElement.Name, out var requirementsSpecification))
            {
                return false;
            }

            requirementsSpecification.Requirement.Add(requirement);

            return true;
        }

        /// <summary>
        /// Tries to get a existing <see cref="RequirementsSpecification" /> or created one based on the name
        /// </summary>
        /// <param name="requirementsSpecificationName">The name</param>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /></param>
        /// <returns>A value indicating whether the <see cref="RequirementsSpecification" /> has been created or retrieved</returns>
        private bool TryGetOrCreateRequirementsSpecification(string requirementsSpecificationName, out RequirementsSpecification requirementsSpecification)
        {
            var shortName = requirementsSpecificationName.GetShortName();

            var alreadyCreated = this.requirementsSpecifications
                                     .FirstOrDefault(x => string.Equals(x.ShortName, shortName, StringComparison.InvariantCultureIgnoreCase))
                                 ?? this.HubController.OpenIteration.RequirementsSpecification
                                     .FirstOrDefault(x => !x.IsDeprecated
                                                          && string.Equals(x.ShortName, shortName, StringComparison.InvariantCultureIgnoreCase))
                                     ?.Clone(true);

            if (alreadyCreated == null)
            {
                alreadyCreated = new RequirementsSpecification
                {
                    Iid = Guid.NewGuid(),
                    Name = requirementsSpecificationName,
                    ShortName = shortName,
                    Owner = this.Owner
                };
            }

            this.requirementsSpecifications.RemoveAll(x => string.Equals(x.ShortName, shortName, StringComparison.InvariantCultureIgnoreCase));
            this.requirementsSpecifications.Add(alreadyCreated);
            this.requirementsGroups.AddRange(alreadyCreated.GetAllContainedGroups());
            alreadyCreated.Category.RemoveAll(x => x.Iid == this.requirementCategory.Iid);
            alreadyCreated.Category.Add(this.requirementCategory);

            requirementsSpecification = alreadyCreated;

            return requirementsSpecification != null;
        }

        /// <summary>
        /// Tries to get a existing <see cref="RequirementsGroup" /> or created one based on the <see cref="Package" />
        /// </summary>
        /// <param name="package">The <see cref="Package" /></param>
        /// <param name="requirementsGroup">The <see cref="RequirementsGroup" /></param>
        /// <returns>A value indicating whether the <see cref="RequirementsGroup" /> has been created or retrieved</returns>
        private bool TryGetOrCreateRequirementsGroup(Package package, out RequirementsGroup requirementsGroup)
        {
            var createdRequirementsGroup = this.requirementsGroups.FirstOrDefault(x =>
                string.Equals(x.ShortName, package.Name.GetShortName(), StringComparison.InvariantCultureIgnoreCase));

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

            requirementsGroup.Category.RemoveAll(x => x.Iid == this.requirementCategory.Iid);
            requirementsGroup.Category.Add(this.requirementCategory);

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

            this.UpdateRequirementProperties(requirementElement, requirement);
            return requirement != null;
        }

        /// <summary>
        /// Update the properties of the <see cref="Requirement" />
        /// </summary>
        /// <param name="requirementElement">The <see cref="Element" /></param>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        private void UpdateRequirementProperties(Element requirementElement, Requirement requirement)
        {
            requirement.Category.RemoveAll(x => x.Iid == this.requirementCategory.Iid);
            requirement.Category.Add(this.requirementCategory);

            this.UpdateOrCreateDefinition(requirementElement, requirement);
        }

        /// <summary>
        /// Updates or creates the defijntion according to the provided <see cref="Element" />
        /// </summary>
        /// <param name="requirementElement">The <see cref="Element" /></param>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        private void UpdateOrCreateDefinition(Element requirementElement, Requirement requirement)
        {
            if (requirement == null)
            {
                return;
            }

            var definition = requirement.Definition.FirstOrDefault(x => string.Equals(x.LanguageCode, "en", StringComparison.InvariantCultureIgnoreCase))
                ?.Clone(true) ?? this.CreateDefinition();

            definition.Content = requirementElement.GetRequirementText();
            requirement.Definition.RemoveAll(x => x.Iid == definition.Iid);
            requirement.Definition.Add(definition);
        }

        /// <summary>
        /// Creates a new <see cref="Definition" />
        /// </summary>
        /// <returns>A <see cref="Definition" /></returns>
        private Definition CreateDefinition()
        {
            return new Definition
            {
                Iid = Guid.NewGuid(),
                LanguageCode = "en"
            };
        }
    }
}
