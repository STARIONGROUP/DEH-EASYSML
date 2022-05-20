// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlockDefinitionToElementDefinitionMappingRuleTestFixture.cs" company="RHEA System S.A.">
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
    using System.Linq;
    using System.Reactive.Concurrency;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.MappingRules;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.Utils.Stereotypes;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;

    using EA;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    using Parameter = CDP4Common.EngineeringModelData.Parameter;

    [TestFixture]
    public class BlockDefinitionToElementDefinitionMappingRuleTestFixture
    {
        private BlockDefinitionToElementDefinitionMappingRule rule;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Mock<IMappingConfigurationService> mappingConfiguration;
        private Uri uri;
        private Assembler assembler;
        private DomainOfExpertise domain;
        private Mock<ISession> session;
        private Iteration iteration;
        private ModelReferenceDataLibrary referenceDataLibrary;
        private Mock<Element> block;
        private Mock<Repository> repository;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

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
            this.hubController.Setup(x => x.GetSiteDirectory()).Returns(new SiteDirectory());
            this.hubController.Setup(x => x.GetDehpOrModelReferenceDataLibrary()).Returns(this.referenceDataLibrary);

            var massValueProperty = new Mock<Element>();
            massValueProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            massValueProperty.Setup(x => x.Name).Returns("mass");
            massValueProperty.Setup(x => x.ElementID).Returns(11245);

            var dependencyConnector = new Mock<Connector>();
            dependencyConnector.Setup(x => x.Type).Returns(StereotypeKind.Dependency.ToString());
            dependencyConnector.Setup(x => x.ClientID).Returns(massValueProperty.Object.ElementID);

            var state = new Mock<Element>();
            state.Setup(x => x.ElementID).Returns(125);
            state.Setup(x => x.Type).Returns(StereotypeKind.State.ToString());
            dependencyConnector.Setup(x => x.SupplierID).Returns(state.Object.ElementID);
            state.Setup(x => x.Name).Returns("State");
            state.Setup(x => x.Partitions).Returns(new EnterpriseArchitectCollection());

            massValueProperty.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection(){dependencyConnector.Object});

            var massCustomProperty = new Mock<CustomProperty>();
            massCustomProperty.Setup(x => x.Name).Returns("default");
            massCustomProperty.Setup(x => x.Value).Returns("45");
            massValueProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection { massCustomProperty.Object });
            var unitProperty = new Mock<TaggedValue>();
            unitProperty.Setup(x => x.Name).Returns("unit");
            unitProperty.Setup(x => x.Value).Returns("unitValue");
            massValueProperty.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection() { unitProperty.Object });

            var heightValueProperty = new Mock<Element>();
            heightValueProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            heightValueProperty.Setup(x => x.Name).Returns("height");
            var heightCustomProperty = new Mock<CustomProperty>();
            heightCustomProperty.Setup(x => x.Name).Returns("default");
            heightCustomProperty.Setup(x => x.Value).Returns((string)null);
            heightValueProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection { heightCustomProperty.Object });
            heightValueProperty.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection());
            heightValueProperty.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());

            var boolValueProperty = new Mock<Element>();
            boolValueProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            boolValueProperty.Setup(x => x.Name).Returns("aBoolean");
            var boolCustomProperty = new Mock<CustomProperty>();
            boolCustomProperty.Setup(x => x.Name).Returns("default");
            boolCustomProperty.Setup(x => x.Value).Returns("true");
            boolValueProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection { boolCustomProperty.Object });
            boolValueProperty.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection());
            boolValueProperty.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());

            var stringValueProperty = new Mock<Element>();
            stringValueProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            stringValueProperty.Setup(x => x.Name).Returns("aString");
            var stringCustomProperty = new Mock<CustomProperty>();
            stringCustomProperty.Setup(x => x.Name).Returns("default");
            stringCustomProperty.Setup(x => x.Value).Returns("aValue");
            stringValueProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection { stringCustomProperty.Object });
            stringValueProperty.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection());
            stringValueProperty.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());

            this.block = new Mock<Element>();
            this.block.Setup(x => x.Name).Returns("AName");

            var embeddedElement = new EnterpriseArchitectCollection();

            embeddedElement.AddRange(new List<object>()
            {
                massValueProperty.Object, heightValueProperty.Object,
                boolValueProperty.Object, stringValueProperty.Object
            });

            this.block.Setup(x => x.EmbeddedElements).Returns(embeddedElement);

            var unitElement = new Mock<Element>();
            unitElement.Setup(x => x.Name).Returns("kg");

            this.repository= new Mock<Repository>();
            this.repository.Setup(x => x.GetElementByGuid("unitValue")).Returns(unitElement.Object);
            this.repository.Setup(x => x.GetElementByID(state.Object.ElementID)).Returns(state.Object);

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.CurrentRepository).Returns(this.repository.Object);

            this.mappingConfiguration = new Mock<IMappingConfigurationService>();

            this.mappingConfiguration.Setup(x => 
                x.AddToExternalIdentifierMap(It.IsAny<Guid>(), It.IsAny<string>(), MappingDirection.FromDstToHub));

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.dstController.Object).As<IDstController>();
            containerBuilder.RegisterInstance(this.mappingConfiguration.Object).As<IMappingConfigurationService>();
            AppContainer.Container = containerBuilder.Build();

            this.rule = new BlockDefinitionToElementDefinitionMappingRule();
        }

        [Test]
        public void VerifyMap()
        {
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            Assert.DoesNotThrow(() => this.rule.Transform((true, null)));
            Assert.DoesNotThrow(() => this.rule.Transform((false, null)));

            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            Assert.DoesNotThrow(() => this.rule.Transform((true, null)));
            Assert.DoesNotThrow(() => this.rule.Transform((false, null)));

            Assert.DoesNotThrow(() => this.rule.Transform((true,new List<EnterpriseArchitectBlockElement>()
            {
                new EnterpriseArchitectBlockElement(null, this.block.Object, MappingDirection.FromDstToHub)
            })));

            Assert.DoesNotThrow(() => this.rule.Transform((false, new List<EnterpriseArchitectBlockElement>()
            {
                new EnterpriseArchitectBlockElement(null, this.block.Object, MappingDirection.FromDstToHub)
            })));
        }

        [Test]
        public void VerifyGetOrCreateElementDefinition()
        {
            Assert.IsNotNull(this.rule.GetOrCreateElementDefinition(this.block.Object));

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid(),
                ShortName = "aName"
            };

            this.rule.Elements.Add(new EnterpriseArchitectBlockElement(elementDefinition, null, MappingDirection.FromDstToHub));
            Assert.AreEqual(elementDefinition, this.rule.GetOrCreateElementDefinition(this.block.Object));

            this.rule.Elements.Clear();
            this.iteration.Element.Add(elementDefinition);
            Assert.AreNotEqual(elementDefinition, this.rule.GetOrCreateElementDefinition(this.block.Object));
            Assert.AreEqual(elementDefinition.Iid, this.rule.GetOrCreateElementDefinition(this.block.Object).Iid);
        }

        [Test]
        public void VerifyMapCategories()
        {
            var isEncapsulatedProperty = new Mock<Element>();
            isEncapsulatedProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            isEncapsulatedProperty.Setup(x => x.Name).Returns("isEncapsulated");
            var isEncapsulatedValue = new Mock<CustomProperty>();
            isEncapsulatedValue.Setup(x => x.Name).Returns("default");
            isEncapsulatedValue.Setup(x => x.Value).Returns((string)null);
            isEncapsulatedProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection { isEncapsulatedValue.Object });

            this.block.Setup(x => x.EmbeddedElements).Returns(new EnterpriseArchitectCollection() { isEncapsulatedProperty.Object });

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid(),
                ShortName = "aName"
            };

            Assert.DoesNotThrow(() => this.rule.MapCategories(null, null));
            Assert.DoesNotThrow(() => this.rule.MapCategories(null, this.block.Object));
            Assert.DoesNotThrow(() => this.rule.MapCategories(elementDefinition, this.block.Object));

            isEncapsulatedValue.Setup(x => x.Value).Returns("true");
            Assert.DoesNotThrow(() => this.rule.MapCategories(elementDefinition, this.block.Object));

            isEncapsulatedValue.Setup(x => x.Value).Returns("1");
            Assert.DoesNotThrow(() => this.rule.MapCategories(elementDefinition, this.block.Object));

            var isLeafCategory = new Category()
            {
                ShortName = "LEA",
                IsDeprecated = false
            };

            this.hubController.Setup(x =>
                x.TryGetThingBy(It.IsAny<Func<Thing, bool>>(), ClassKind.Category, out isLeafCategory)).Returns(true);

            Assert.DoesNotThrow(() => this.rule.MapCategories(elementDefinition, this.block.Object));

            this.hubController.Setup(x =>
                x.TryGetThingBy(It.IsAny<Func<Thing, bool>>(), ClassKind.Category, out isLeafCategory)).Returns(false);

            this.hubController.Setup(x => x.TryGetThingById(It.IsAny<Guid>(), ClassKind.Category, out isLeafCategory)).Returns(true);
            Assert.DoesNotThrow(() => this.rule.MapCategories(elementDefinition, this.block.Object));
        }

        [Test]
        public void VerifyMapProperties()
        {
            this.rule.DstController = this.dstController.Object;

            MeasurementUnit measurementUnit = new SimpleUnit();
            MeasurementScale measurementScale = new RatioScale();

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Thing, bool>>()
                , ClassKind.MeasurementUnit, out measurementUnit)).Returns(false);

            this.hubController.Setup(x => x.TryGetThingById(It.IsAny<Guid>()
                , ClassKind.MeasurementUnit, out measurementUnit)).Returns(false);

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Thing, bool>>()
                , ClassKind.MeasurementScale, out measurementScale)).Returns(false);

            this.hubController.Setup(x => x.TryGetThingById(It.IsAny<Guid>()
                , ClassKind.MeasurementScale, out measurementScale)).Returns(false);

            Assert.DoesNotThrow(() => this.rule.MapProperties(null, null));
            Assert.DoesNotThrow(() => this.rule.MapProperties(null, this.block.Object));

            var elementDefinition = new ElementDefinition();
            Assert.DoesNotThrow(() => this.rule.MapProperties(elementDefinition, this.block.Object));

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Thing, bool>>()
                , ClassKind.MeasurementUnit, out measurementUnit)).Returns(false);

            this.hubController.Setup(x => x.TryGetThingById(It.IsAny<Guid>()
                , ClassKind.MeasurementUnit, out measurementUnit)).Returns(true);

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Thing, bool>>()
                , ClassKind.MeasurementScale, out measurementScale)).Returns(false);

            this.hubController.Setup(x => x.TryGetThingById(It.IsAny<Guid>()
                , ClassKind.MeasurementScale, out measurementScale)).Returns(true);

            Assert.DoesNotThrow(() => this.rule.MapProperties(elementDefinition, this.block.Object));

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Thing, bool>>()
                , ClassKind.MeasurementUnit, out measurementUnit)).Returns(true);

            this.hubController.Setup(x => x.TryGetThingById(It.IsAny<Guid>()
                , ClassKind.MeasurementUnit, out measurementUnit)).Returns(true);

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Thing, bool>>()
                , ClassKind.MeasurementScale, out measurementScale)).Returns(true);

            this.hubController.Setup(x => x.TryGetThingById(It.IsAny<Guid>()
                , ClassKind.MeasurementScale, out measurementScale)).Returns(true);

            Assert.DoesNotThrow(() => this.rule.MapProperties(elementDefinition, this.block.Object));

            ParameterType parameterType = new SimpleQuantityKind()
            {
                Iid = Guid.NewGuid(),
                ShortName = "mass",
                Name = "mass",
            };

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Thing, bool>>()
                , ClassKind.ParameterType, out parameterType)).Returns(true);

            this.hubController.Setup(x => x.TryGetThingById(It.IsAny<Guid>()
                , ClassKind.ParameterType, out parameterType)).Returns(true);

            Assert.DoesNotThrow(() => this.rule.MapProperties(elementDefinition, this.block.Object));

            var parameter = new Parameter()
            {
                Iid = Guid.NewGuid(),
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(),
                        ValueSwitch = ParameterSwitchKind.MANUAL
                    },
                },
                ParameterType = new SimpleQuantityKind()
                {
                    Iid = Guid.NewGuid(),
                    ShortName = "mass"
                },
                Container = elementDefinition
            };

            elementDefinition.Parameter.Add(parameter);

            Assert.DoesNotThrow(() => this.rule.MapProperties(elementDefinition, this.block.Object));
            Assert.AreEqual("aValue", elementDefinition.Parameter.Last().ValueSet.First().ActualValue.First());
        }

        [Test]
        public void VerifyMapPartProperties()
        {
            this.rule.DstController = this.dstController.Object;

            var partProperty = new Mock<Element>();
            partProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.PartProperty.ToString());
            partProperty.Setup(x => x.PropertyType).Returns(42);

            var partBlock = new Mock<Element>();
            partBlock.Setup(x => x.Name).Returns("anOtherBlock");
            partBlock.Setup(x => x.EmbeddedElements).Returns(new EnterpriseArchitectCollection());

            this.repository.Setup(x => x.GetElementByID(42)).Returns(partBlock.Object);
            this.block.Setup(x => x.EmbeddedElements).Returns(new EnterpriseArchitectCollection() { partProperty.Object });
            Assert.DoesNotThrow(() => this.rule.MapPartProperties(null, this.block.Object));

            var elementDefinition = new ElementDefinition();
            Assert.DoesNotThrow(() => this.rule.MapPartProperties(elementDefinition, this.block.Object));
            Assert.AreEqual(1, elementDefinition.ContainedElement.Count);
        }

        [Test]
        public void VerifyMapPorts()
        {
            this.rule.DstController = this.dstController.Object;

            var port = new Mock<Element>();
            port.Setup(x => x.MetaType).Returns(StereotypeKind.Port.ToString());

            var portBlock = new Mock<Element>();
            portBlock.Setup(x => x.Name).Returns("port");
            var interfaceBlock = new Mock<Element>();
            interfaceBlock.Setup(x => x.Name).Returns("interface");

            this.block.Setup(x => x.EmbeddedElements).Returns(new EnterpriseArchitectCollection() { port.Object });
            this.dstController.Setup(x => x.ResolvePort(It.IsAny<Element>())).Returns((null, null));

            var elementDefinition = new ElementDefinition();
            this.rule.Elements.Add(new EnterpriseArchitectBlockElement(elementDefinition, this.block.Object, MappingDirection.FromDstToHub));
            Assert.DoesNotThrow(() => this.rule.MapPorts());
            
            this.dstController.Setup(x => x.ResolvePort(It.IsAny<Element>())).Returns((null, interfaceBlock.Object));
            Assert.DoesNotThrow(() => this.rule.MapPorts());
            Assert.DoesNotThrow(() => this.rule.ProcessInterfaces());

            this.dstController.Setup(x => x.ResolvePort(It.IsAny<Element>())).Returns((portBlock.Object, interfaceBlock.Object));
            Assert.DoesNotThrow(() => this.rule.MapPorts());
            Assert.DoesNotThrow(() => this.rule.ProcessInterfaces());

            var elementUsage = new ElementUsage()
            {
                Name = "interface_Impl",
                ElementDefinition = elementDefinition
            };

            this.iteration.Element.Add(new ElementDefinition(){ContainedElement = { elementUsage }});
            Assert.DoesNotThrow(() => this.rule.ProcessInterfaces());
        }
    }
}
