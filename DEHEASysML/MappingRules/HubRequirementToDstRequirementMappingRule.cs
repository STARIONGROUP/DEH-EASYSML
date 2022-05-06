// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubRequirementToDstRequirementMappingRule.cs" company="RHEA System S.A.">
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

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
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
    /// The <see cref="HubRequirementToDstRequirementMappingRule" /> is a <see cref="IMappingRule" /> for the
    /// <see cref="MappingEngine" />
    /// that takes a <see cref="List{T}" /> of <see cref="RequirementMappedElement" /> as input and
    /// outputs a E-TM-10-25 collection of <see cref="MappedRequirementRowViewModel" />
    /// </summary>
    public class HubRequirementToDstRequirementMappingRule : HubToDstBaseMappingRule<(bool completeMapping, List<RequirementMappedElement> elements), List<MappedRequirementRowViewModel>>
    {
        /// <summary>
        /// The collection of <see cref="RequirementMappedElement" />
        /// </summary>
        public List<RequirementMappedElement> Elements { get; private set; } = new();

        /// <summary>
        /// Transform a <see cref="List{T}" /> of <see cref="RequirementMappedElement" /> into a
        /// <see cref="List{T}" /> of
        /// <see cref="MappedRequirementRowViewModel" />
        /// </summary>
        /// <param name="input">
        /// Tuple of <see cref="bool" />, The <see cref="List{T}" /> of <see cref="RequirementMappedElement " />
        /// The <see cref="bool" /> handles the fact that it the mapping has to map everything
        /// </param>
        /// <returns>A collection of <see cref="MappedRequirementRowViewModel" /></returns>
        public override List<MappedRequirementRowViewModel> Transform((bool completeMapping, List<RequirementMappedElement> elements) input)
        {
            try
            {
                this.MappingConfiguration = AppContainer.Container.Resolve<IMappingConfigurationService>();
                this.DstController = AppContainer.Container.Resolve<IDstController>();

                if (!this.HubController.IsSessionOpen || !this.DstController.IsFileOpen)
                {
                    return default;
                }

                var (complete, elements) = input;
                this.Elements = new List<RequirementMappedElement>(elements);

                foreach (var mappedElement in this.Elements.ToList())
                {
                    if (mappedElement.DstElement == null)
                    {
                        var alreadyExist = this.GetOrCreateRequirement(mappedElement.HubElement, out var requirementElement);
                        mappedElement.DstElement = requirementElement;
                        mappedElement.ShouldCreateNewTargetElement = !alreadyExist;
                    }

                    if (complete)
                    {
                        this.UpdateProperties(mappedElement.HubElement, mappedElement.DstElement);
                        this.UpdateOrCreateRequirementPackages(mappedElement.HubElement, mappedElement.DstElement);
                    }
                }

                if (complete)
                {
                    foreach (var requirementMappedElement in this.Elements)
                    {
                        this.LinkRequirements(requirementMappedElement);
                    }

                    this.SaveMappingConfiguration(new List<MappedElementRowViewModel<Requirement>>(this.Elements));
                }

                return new List<MappedRequirementRowViewModel>(this.Elements);
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Links <see cref="Element"/> requirements based on their relationship
        /// </summary>
        /// <param name="requirementMappedElement">The <see cref="RequirementMappedElement"/></param>
        private void LinkRequirements(RequirementMappedElement requirementMappedElement)
        {
            var relationShips = requirementMappedElement.HubElement.QueryRelationships
                .OfType<BinaryRelationship>().ToList();

            foreach (var relationship in relationShips)
            {
                if (relationship.Source is Requirement source && relationship.Target is Requirement target)
                {
                    if (this.DstController.TryGetRequirement(source.Name, source.ShortName, out var sourceElement)
                        && this.DstController.TryGetRequirement(target.Name, target.ShortName, out var targetElement))
                    {
                        this.CreateOrUpdateConnector(requirementMappedElement.DstElement, sourceElement, targetElement);
                    }
                }
            }
        }

        /// <summary>
        /// Creates or Updates the connector for the requirement
        /// </summary>
        /// <param name="dstElement">The current <see cref="Element"/></param>
        /// <param name="sourceElement">The source <see cref="Element"/></param>
        /// <param name="targetElement">The target <see cref="Element"/></param>
        private void CreateOrUpdateConnector(Element dstElement, Element sourceElement, Element targetElement)
        {
            var connector = dstElement.Connectors.OfType<Connector>()
                .FirstOrDefault(x => x.Stereotype.AreEquals(StereotypeKind.DeriveReqt) && x.ClientID == sourceElement.ElementID
                                                                                       && x.SupplierID == targetElement.ElementID);

            if (connector == null)
            {
                connector = dstElement.Connectors.AddNew("", "Abstraction") as Connector;
                connector.StereotypeEx = StereotypeKind.DeriveReqt.GetFQStereotype();
                connector.ClientID = sourceElement.ElementID;
                connector.SupplierID = targetElement.ElementID;
                connector.Update();
                dstElement.Connectors.Refresh();
            }
        }

        /// <summary>
        /// Update or create the <see cref="Package" /> tree that will contains the <see cref="Element" />
        /// </summary>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        private void UpdateOrCreateRequirementPackages(Requirement requirement, Element element)
        {
            var treeNodesNames = new List<string>();

            if (requirement.Container is not RequirementsSpecification requirementsSpecification)
            {
                return;
            }

            var container = requirement.Group as RequirementsContainer;

            while (container != null && container.Iid != requirementsSpecification.Iid)
            {
                treeNodesNames.Add(container.Name);
                container = container.Container as RequirementsContainer;
            }

            treeNodesNames.Reverse();

            var package = this.GetOrCreateRequirementsContainerPackage(requirementsSpecification);

            foreach (var packageNames in treeNodesNames)
            {
                package = this.GetOrCreatePackage(package, packageNames);
            }

            element.PackageID = package.PackageID;
            element.Update();
        }

        /// <summary>
        /// Gets or create <see cref="Package" /> that can represent the <see cref="RequirementsSpecification" />
        /// </summary>
        /// <param name="container">The <see cref="RequirementsSpecification" /></param>
        /// <returns>The package</returns>
        private Package GetOrCreateRequirementsContainerPackage(RequirementsSpecification container)
        {
            if (!this.DstController.TryGetPackage(container.Name, out var package))
            {
                package = this.DstController.AddNewPackage(this.DstController.GetDefaultBlocksPackage(), container.Name);
            }

            return package;
        }

        /// <summary>
        /// Gets or create a <see cref="Package" /> as child of the provided <see cref="Package" />
        /// </summary>
        /// <param name="parentPackage">The parent <see cref="Package" /></param>
        /// <param name="subPackageName">The name of the <see cref="Package" /> to get or creates</param>
        /// <returns>The child <see cref="Package" /></returns>
        private Package GetOrCreatePackage(Package parentPackage, string subPackageName)
        {
            var package = parentPackage.Packages.OfType<Package>().FirstOrDefault(x => x.Name == subPackageName);

            if (package == null)
            {
                package = this.DstController.AddNewPackage(parentPackage, subPackageName);
                package.Update();
                parentPackage.Update();
                parentPackage.Packages.Refresh();
            }

            return package;
        }

        /// <summary>
        /// Update the <see cref="Element" /> properties
        /// </summary>
        /// <param name="hubElement">The <see cref="Requirement" /></param>
        /// <param name="dstElement">The <see cref="Element" /> to update</param>
        private void UpdateProperties(Requirement hubElement, Element dstElement)
        {
            (string id, string text) taggedValue = new()
            {
                id = hubElement.ShortName,
                text = this.GetDefinition(hubElement)
            };

            this.DstController.UpdatedRequirementValues[dstElement.ElementGUID] = taggedValue;
        }

        /// <summary>
        /// Gets the <see cref="Definition" /> text of the <see cref="Requirement" />
        /// </summary>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <returns>The text of the <see cref="Definition" /></returns>
        private string GetDefinition(Requirement requirement)
        {
            if (requirement.Definition.Any())
            {
                var definition = requirement.Definition.FirstOrDefault(x =>
                    string.Equals(x.LanguageCode, "en", StringComparison.InvariantCultureIgnoreCase)) ?? requirement.Definition[0];

                return definition.Content;
            }

            return "";
        }

        /// <summary>
        /// Gets or creates the <see cref="Element" /> representing a Requirement based on the <see cref="Requirement" />
        /// </summary>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <param name="requirementElement">The <see cref="Element" /></param>
        /// <returns>A value indicating if the <see cref="Element" /> already exists</returns>
        private bool GetOrCreateRequirement(Requirement requirement, out Element requirementElement)
        {
            if (!this.DstController.TryGetElement(requirement.Name, StereotypeKind.Requirement, out requirementElement)
                || requirement.ShortName != requirementElement.GetRequirementId())
            {
                requirementElement = this.DstController.AddNewElement(this.DstController.GetDefaultBlocksPackage().Elements,
                    requirement.Name, "requirement", StereotypeKind.Requirement);

                requirementElement.Update();
                return false;
            }

            return true;
        }
    }
}
