// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectConnectorToBinaryRelationshipMappingRuleTestFixture.cs" company="RHEA System S.A.">
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
    using DEHEASysML.Enumerators;
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
    public class EnterpriseArchitectConnectorToBinaryRelationshipMappingRuleTestFixture
    {
        private EnterpriseArchitectConnectorToBinaryRelationshipMappingRule rule;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Uri uri;
        private Assembler assembler;
        private DomainOfExpertise domain;
        private Mock<ISession> session;
        private Iteration iteration;
        private ModelReferenceDataLibrary referenceDataLibrary;

        [SetUp]
        public void Setup()
        {
            this.uri = new Uri("https://uri.test");
            this.assembler = new Assembler(this.uri);
            this.domain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri);
            this.session = new Mock<ISession>();
            this.session.Setup(x => x.Assembler).Returns(this.assembler);
            this.session.Setup(x => x.DataSourceUri).Returns(this.uri.AbsoluteUri);

            this.referenceDataLibrary = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri);

            var engineering = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                RequiredRdl = { this.referenceDataLibrary },
                Container = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri)
                {
                    Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, this.uri)
                }
            };

            this.iteration = new Iteration(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Container = engineering,
                IterationSetup = new IterationSetup()
                {
                    Container = engineering
                }
            };

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(this.domain);
            this.hubController.Setup(x => x.Session).Returns(this.session.Object);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.MappedConnectorsToBinaryRelationships).Returns(new List<BinaryRelationship>());

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.dstController.Object).As<IDstController>();
            AppContainer.Container = containerBuilder.Build();

            this.rule = new EnterpriseArchitectConnectorToBinaryRelationshipMappingRule();
        }

        [Test]
        public void VerifyTransform()
        {
            var tracableElements = new List<EnterpriseArchitectTracableMappedElement>();

            Assert.DoesNotThrow(() => this.rule.Transform(tracableElements));

            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            Assert.IsEmpty(this.rule.Transform(tracableElements));

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            var requirement1 = new Requirement()
            {
                Iid = Guid.NewGuid()
            };

            var requirement2 = new Requirement()
            {
                Iid = Guid.NewGuid()
            };

            var blockElement = new Mock<Element>();
            blockElement.Setup(x => x.ElementID).Returns(145);

            var requirementElement = new Mock<Element>();
            requirementElement.Setup(x => x.ElementID).Returns(154);

            var requirement2Element = new Mock<Element>();
            requirement2Element.Setup(x => x.ElementID).Returns(254);

            var traceConnector = new Mock<Connector>();
            traceConnector.Setup(x => x.Stereotype).Returns(StereotypeKind.Trace.ToString());
            traceConnector.Setup(x => x.SupplierID).Returns(requirement2Element.Object.ElementID);
            traceConnector.Setup(x => x.ClientID).Returns(requirementElement.Object.ElementID);

            var statisfyConnector = new Mock<Connector>();
            statisfyConnector.Setup(x => x.Stereotype).Returns(StereotypeKind.Satisfy.ToString());
            statisfyConnector.Setup(x => x.SupplierID).Returns(requirement2Element.Object.ElementID);
            statisfyConnector.Setup(x => x.ClientID).Returns(blockElement.Object.ElementID);

            blockElement.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection() { statisfyConnector.Object });
            requirementElement.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection() { traceConnector.Object });
            requirement2Element.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection() { traceConnector.Object });

            tracableElements.Add(new EnterpriseArchitectTracableMappedElement(new EnterpriseArchitectBlockElement(elementDefinition, blockElement.Object, MappingDirection.FromDstToHub)));
            tracableElements.Add(new EnterpriseArchitectTracableMappedElement(new EnterpriseArchitectRequirementElement(requirement1, requirementElement.Object, MappingDirection.FromDstToHub)));
            tracableElements.Add(new EnterpriseArchitectTracableMappedElement(new EnterpriseArchitectRequirementElement(requirement2, requirement2Element.Object, MappingDirection.FromDstToHub)));

            var traceCategory = new Category()
            {
                Name = "trace",
                ShortName = "trace",
                PermissibleClass = new List<ClassKind>()
                {
                    ClassKind.BinaryRelationship
                }
            };

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Category, bool>>(), ClassKind.Category, out traceCategory))
                .Returns(true);

            Assert.AreEqual(2,this.rule.Transform(tracableElements).Count);
        }
    }
}
