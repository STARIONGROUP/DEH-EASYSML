// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryRelationshipToEnterpriseArchitectConnectorMappingRuleTestFixture.cs" company="RHEA System S.A.">
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

    using CDP4Dal;

    using DEHEASysML.DstController;
    using DEHEASysML.MappingRules;
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
    public class BinaryRelationshipToEnterpriseArchitectConnectorMappingRuleTestFixture
    {
        private BinaryRelationshipToEnterpriseArchitectConnectorMappingRule rule;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Iteration iteration;

        [SetUp]
        public void Setup()
        {
            this.dstController = new Mock<IDstController>();
            var uri = new Uri("https://uri.test");
            var assembler = new Assembler(uri);
            var session = new Mock<ISession>();
            session.Setup(x => x.Assembler).Returns(assembler);
            session.Setup(x => x.DataSourceUri).Returns(uri.AbsoluteUri);

            var  referenceDataLibrary = new ModelReferenceDataLibrary(Guid.NewGuid(), assembler.Cache, uri);

            var engineering = new EngineeringModelSetup(Guid.NewGuid(), assembler.Cache, uri)
            {
                RequiredRdl = { referenceDataLibrary },
                Container = new SiteReferenceDataLibrary(Guid.NewGuid(), assembler.Cache, uri)
                {
                    Container = new SiteDirectory(Guid.NewGuid(), assembler.Cache, uri)
                }
            };

            this.iteration = new Iteration(Guid.NewGuid(), assembler.Cache, uri)
            {
                Container = engineering,
                IterationSetup = new IterationSetup()
                {
                    Container = engineering
                }
            };

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.dstController.Object).As<IDstController>();
            AppContainer.Container = containerBuilder.Build();

            this.rule = new BinaryRelationshipToEnterpriseArchitectConnectorMappingRule();
        }

        [Test]
        public void VerifyTransform()
        {
            var relationships = new List<HubRelationshipMappedElement>();
            Assert.DoesNotThrow(() => this.rule.Transform(relationships));
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            Assert.DoesNotThrow(() => this.rule.Transform(relationships));

            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            this.dstController.Setup(x => x.IsFileOpen).Returns(false);
            Assert.DoesNotThrow(() => this.rule.Transform(relationships));

            this.dstController.Setup(x => x.IsFileOpen).Returns(true);
            Assert.DoesNotThrow(() => this.rule.Transform(relationships));

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid(),
            };

            var element = new Mock<Element>();
            element.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());

            var traceCategory = new Category()
            {
                Name = "trace",
                ShortName = "trace",
                PermissibleClass = new List<ClassKind>()
                {
                    ClassKind.BinaryRelationship
                }
            };

            var satisfyCategory = new Category()
            {
                Name = "satisfy",
                ShortName = "satisfy",
                PermissibleClass = new List<ClassKind>()
                {
                    ClassKind.BinaryRelationship
                }
            };

            var requirementspecification = new RequirementsSpecification()
            {
                Iid = Guid.NewGuid()
            };

            var requirement = new Requirement()
            {
                Iid = Guid.NewGuid() 
            };

            var requirementElement = new Mock<Element>();
            requirementElement.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());

            var requirement2 = new Requirement()
            {
                Iid = Guid.NewGuid()
            };

            requirementspecification.Requirement.Add(requirement);
            requirementspecification.Requirement.Add(requirement2);

            var requirementElement2 = new Mock<Element>();
            requirementElement2.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());

            var mappedElementDefinition = new ElementDefinitionMappedElement(elementDefinition, element.Object, MappingDirection.FromHubToDst); 
            relationships.Add(new HubRelationshipMappedElement(mappedElementDefinition));

            Assert.IsEmpty(this.rule.Transform(relationships));

            var mappedRequirement = new RequirementMappedElement(requirement, requirementElement.Object, MappingDirection.FromHubToDst);
            relationships.Add(new HubRelationshipMappedElement(mappedRequirement));

            var mappedRequirement2 = new RequirementMappedElement(requirement2, requirementElement2.Object, MappingDirection.FromHubToDst);
            relationships.Add(new HubRelationshipMappedElement(mappedRequirement2));

            Assert.IsEmpty(this.rule.Transform(relationships));

            var invalidRelationShip = new BinaryRelationship()
            {
                Iid = Guid.NewGuid(),
                Source = elementDefinition,
                Target = new ElementDefinition()
            };

            var validSatisfy = new BinaryRelationship()
            {
                Iid = Guid.NewGuid(),
                Source = elementDefinition,
                Target = requirement,
                Category = new List<Category>() { satisfyCategory }
            };

            var validTrace = new BinaryRelationship()
            {
                Iid = Guid.NewGuid(),
                Source = requirement2,
                Target = requirement,
                Category = new List<Category>() { traceCategory }
            };

            this.iteration.Relationship.Add(invalidRelationShip);
            this.iteration.Relationship.Add(validSatisfy);
            this.iteration.Relationship.Add(validTrace);

            Assert.AreEqual(2, this.rule.Transform(relationships).Count);
        }
    }
}
