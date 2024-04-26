// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ElementDefinitionToBlockMappingRuleTestFixture.cs" company="RHEA System S.A.">
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

    using CDP4Common;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

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

    using Parameter = CDP4Common.EngineeringModelData.Parameter;

    [TestFixture]
    public class ElementDefinitionToBlockMappingRuleTestFixture
    {
        private ElementDefinitionToBlockMappingRule rule;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Mock<IMappingConfigurationService> mappingConfiguration;
        private Dictionary<string, string> updatedValuePropretyValues;
        private Dictionary<Element, List<(Partition, ChangeKind)>> modifiedPartitions;
        private Dictionary<string, int> updatedPropertyTypes;
        private List<Connector> createrConnectors;
        private Mock<ICacheService> cacheService;

        [SetUp]
        public void Setup()
        {
            var defaultPackage = new Mock<Package>();
            defaultPackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection());

            this.updatedPropertyTypes = new Dictionary<string, int>();
            this.createrConnectors = new List<Connector>();
            this.modifiedPartitions = new Dictionary<Element, List<(Partition, ChangeKind)>>();
            this.updatedValuePropretyValues = new Dictionary<string, string>();
            this.hubController = new Mock<IHubController>();
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.UpdatedValuePropretyValues).Returns(this.updatedValuePropretyValues);
            this.dstController.Setup(x => x.GetDefaultPackage(It.IsAny<StereotypeKind>())).Returns(defaultPackage.Object);
            this.dstController.Setup(x => x.ModifiedPartitions).Returns(this.modifiedPartitions);
            this.dstController.Setup(x => x.CreatedConnectors).Returns(this.createrConnectors);
            this.dstController.Setup(x => x.UpdatePropertyTypes).Returns(this.updatedPropertyTypes);
            this.dstController.Setup(x => x.CreatedElements).Returns([]);
            this.mappingConfiguration = new Mock<IMappingConfigurationService>();
            this.cacheService = new Mock<ICacheService>();
            this.cacheService.Setup(x => x.GetElementsOfStereotype(It.IsAny<StereotypeKind>())).Returns([]);
            this.cacheService.Setup(x => x.GetElementsOfMetaType(It.IsAny<StereotypeKind>())).Returns([]);
            this.cacheService.Setup(x => x.GetConnectorsOfElement(It.IsAny<int>())).Returns([]);
            this.cacheService.Setup(x => x.GetAllElements()).Returns([]);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.dstController.Object).As<IDstController>();
            containerBuilder.RegisterInstance(this.mappingConfiguration.Object).As<IMappingConfigurationService>();
            containerBuilder.RegisterInstance(this.cacheService.Object).As<ICacheService>();
            AppContainer.Container = containerBuilder.Build();

            this.rule = new ElementDefinitionToBlockMappingRule();
        }

        [Test]
        public void VerifyMapping()
        {
            var possibleFiniteStateList = new PossibleFiniteStateList()
            {
                Name = "State"
            };

            possibleFiniteStateList.PossibleState.Add(new PossibleFiniteState()
            {
                Name = "SubState1"
            });

            possibleFiniteStateList.PossibleState.Add(new PossibleFiniteState()
            {
                Name = "SubState2"
            });

            var actualFiniteStateList = new ActualFiniteStateList();
            actualFiniteStateList.PossibleFiniteStateList.Add(possibleFiniteStateList);
            var actualFiniteState = new ActualFiniteState();
            actualFiniteState.PossibleState.Add(possibleFiniteStateList.PossibleState[0]);
            actualFiniteStateList.ActualState.Add(actualFiniteState);

            var parameter = new Parameter()
            {
                Iid = Guid.NewGuid(),
                ParameterType = new SimpleQuantityKind()
                {
                    Name = "mass",
                },
                Scale = new RatioScale()
                {
                    ShortName = "m",
                    Name = "mass",
                    Unit = new SimpleUnit()
                    {
                        ShortName = "kg",
                        Name = "kilogram"
                    }
                },
                ValueSet = 
                { 
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(new []{"45"}),
                        ValueSwitch = ParameterSwitchKind.MANUAL,
                        ActualState = actualFiniteState
                    }
                }, 
                StateDependence = actualFiniteStateList
            };

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid(),
                Name = "ElementDefinition",
                Parameter = { parameter },
                Category =
                {
                    new Category()
                    {
                        Name = "block"
                    }
                }
            };

            var mappedElements = new List<ElementDefinitionMappedElement>()
            {
                new ElementDefinitionMappedElement(elementDefinition, null, MappingDirection.FromHubToDst)
            };

            var embeddedElements = new EnterpriseArchitectCollection();
            var blockElement = new Mock<Element>();
            blockElement.Setup(x => x.Elements).Returns(embeddedElements);
            blockElement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());

            var taggedValue = new Mock<TaggedValue>();
            taggedValue.Setup(x => x.Value);
            taggedValue.Setup(x => x.Update());
            var taggedValuesEx = new Mock<Collection>();
            taggedValuesEx.Setup(x => x.AddNew("unit", StereotypeKind.TaggedValue.ToString())).Returns(taggedValue.Object);
            taggedValuesEx.Setup(x => x.Refresh());

            var valueType = new Mock<Element>();
            valueType.Setup(x => x.TaggedValuesEx).Returns(taggedValuesEx.Object);
            valueType.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            valueType.Setup(x => x.ElementID).Returns(45);

            var unit = new Mock<Element>();
            unit.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());

            var property = new Mock<Element>();
            property.Setup(x => x.PropertyType);
            property.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            property.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());
            property.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection());

            var state = new Mock<Element>();
            state.Setup(x => x.Partitions).Returns(new EnterpriseArchitectCollection());

            this.dstController.Setup(x => x.AddNewElement(It.IsAny<Collection>(), elementDefinition.Name, "block", StereotypeKind.Block))
                .Returns(blockElement.Object);

            this.dstController.Setup(x => x.AddNewElement(It.IsAny<Collection>(), "kilogram", "Unit", StereotypeKind.Unit))
                .Returns(unit.Object);

            this.dstController.Setup(x => x.AddNewElement(It.IsAny<Collection>(), "mass", "DataType", StereotypeKind.ValueType))
                .Returns(valueType.Object);

            this.dstController.Setup(x => x.AddNewElement(It.IsAny<Collection>(), "mass", "Property"))
                .Returns(property.Object);

            this.dstController.Setup(x => x.AddNewElement(It.IsAny<Collection>(), It.IsAny<string>(), StereotypeKind.State.ToString(),
                StereotypeKind.State)).Returns(state.Object);

            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            Assert.DoesNotThrow(() => this.rule.Transform((true, mappedElements)));

            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            this.dstController.Setup(x => x.IsFileOpen).Returns(false);
            Assert.DoesNotThrow(() => this.rule.Transform((true, mappedElements)));

            this.dstController.Setup(x => x.IsFileOpen).Returns(true);
            Assert.DoesNotThrow(() => this.rule.Transform((true, mappedElements)));
        }
    }
}
