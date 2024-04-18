// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubRequirementToDstRequirementMappingRuleTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.MappingRules
{
    using System;
    using System.Collections.Generic;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.MappingRules;
    using DEHEASysML.Services.Cache;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.Utils.Stereotypes;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;

    using EA;

    using Moq;

    using NUnit.Framework;

    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    [TestFixture]
    public class HubRequirementToDstRequirementMappingRuleTestFixture
    {
        private HubRequirementToDstRequirementMappingRule rule;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Mock<IMappingConfigurationService> mappingConfiguration;
        private Dictionary<string, (string, string)> requirementValues;
        private Dictionary<string, string> updatedStereotypes;
        private Mock<ICacheService> cacheService;

        [SetUp]
        public void Setup()
        {
            var defaultPackage = new Mock<Package>();
            defaultPackage.Setup(x => x.Elements);

            this.updatedStereotypes = new Dictionary<string, string>();
            this.requirementValues = new Dictionary<string, (string, string)>();
            this.hubController = new Mock<IHubController>();
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.UpdatedRequirementValues).Returns(this.requirementValues);
            this.dstController.Setup(x => x.UpdatedStereotypes).Returns(this.updatedStereotypes);
            this.dstController.Setup(x => x.GetDefaultPackage(It.IsAny<StereotypeKind>())).Returns(defaultPackage.Object);
            this.mappingConfiguration = new Mock<IMappingConfigurationService>();
            this.cacheService = new Mock<ICacheService>();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.dstController.Object).As<IDstController>();
            containerBuilder.RegisterInstance(this.mappingConfiguration.Object).As<IMappingConfigurationService>();
            containerBuilder.RegisterInstance(this.cacheService.Object).As<ICacheService>();
            AppContainer.Container = containerBuilder.Build();

            this.rule = new HubRequirementToDstRequirementMappingRule();
        }

        [Test]
        public void VerifyMapping()
        {
            var requirementsSpecification = new RequirementsSpecification()
            {
                Iid = Guid.NewGuid(),
                Name = "A RequirementSpecification"
            };

            var requirementsGroup = new RequirementsGroup()
            {
                Iid = Guid.NewGuid(),
                Name = "A RequirementGroup"
            };

            var requirement = new Requirement()
            {
                Name = "requirement",
                ShortName = "M05",
                Container = requirementsSpecification,
                Group = requirementsGroup,
                Definition = 
                { 
                    new Definition()
                    {
                        Content = "a definiton",
                        LanguageCode = "en"
                    }
                },
                Category =
                {
                    new Category()
                    {
                        Name = "requirement"
                    },
                    new Category()
                    {
                        Name ="customRequirement"
                    }
                }
            };

            var requirementElement = new Mock<Element>();
            requirementElement.Setup(x => x.Update());
            requirementElement.Setup(x => x.PackageID);
            requirementElement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());

            this.dstController.Setup(x => x.AddNewElement(It.IsAny<Collection>(),
                    requirement.Name, "requirement", StereotypeKind.Requirement))
                .Returns(requirementElement.Object);

            var requirementsSpecificationPackage = new Mock<Package>();
            requirementsSpecificationPackage.Setup(x => x.PackageID).Returns(1);
            requirementsSpecificationPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());

            this.dstController.Setup(x => x.AddNewPackage(It.IsAny<Package>(), requirementsSpecification.Name))
                .Returns(requirementsSpecificationPackage.Object);

            var requirementsGroupPackage = new Mock<Package>();
            requirementsGroupPackage.Setup(x => x.PackageID).Returns(2);

            this.dstController.Setup(x => x.AddNewPackage(It.IsAny<Package>(), requirementsGroup.Name))
                .Returns(requirementsGroupPackage.Object);

            var mappedElements = new List<RequirementMappedElement>()
            {
                new RequirementMappedElement(requirement, null, MappingDirection.FromHubToDst)
            };

            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            Assert.DoesNotThrow(() => this.rule.Transform((true, mappedElements)));

            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            this.dstController.Setup(x => x.IsFileOpen).Returns(false);
            Assert.DoesNotThrow(() => this.rule.Transform((true, mappedElements)));

            this.dstController.Setup(x => x.IsFileOpen).Returns(true);
            Assert.DoesNotThrow(() => this.rule.Transform((true, mappedElements)));

            Assert.IsNotEmpty(this.requirementValues);
        }
    }
}
