// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationServiceTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.Services.MappingConfiguration
{
    using System;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal.Operations;

    using DEHEASysML.Enumerators;
    using DEHEASysML.Services.MappingConfiguration;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using Moq;

    using Newtonsoft.Json;

    using NUnit.Framework;

    [TestFixture]
    public class MappingConfigurationServiceTestFixture
    {
        private MappingConfigurationService mappingConfiguration;
        private Mock<IHubController> hubController;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Mock<Repository> repository;

        [SetUp]
        public void Setup()
        {
            this.hubController = new Mock<IHubController>();
            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.statusBar.Setup(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Info));
            this.repository = new Mock<Repository>();

            this.mappingConfiguration = new MappingConfigurationService(this.hubController.Object, this.statusBar.Object);
        }

        private Mock<Element> CreateElement(StereotypeKind stereotype)
        {
            var element = new Mock<Element>();
            var guid = Guid.NewGuid();
            element.Setup(x => x.ElementGUID).Returns(guid.ToString());
            element.Setup(x => x.Stereotype).Returns(stereotype.ToString());
            this.repository.Setup(x => x.GetElementByGuid(element.Object.ElementGUID)).Returns(element.Object);
            return element;
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.mappingConfiguration.ExternalIdentifierMap);
            Assert.IsTrue(this.mappingConfiguration.IsTheCurrentIdentifierMapTemporary);
            this.mappingConfiguration.ExternalIdentifierMap.Iid = Guid.NewGuid();
            Assert.IsFalse(this.mappingConfiguration.IsTheCurrentIdentifierMapTemporary);
            this.mappingConfiguration.ExternalIdentifierMap.Name = "cfg";
            Assert.IsFalse(this.mappingConfiguration.IsTheCurrentIdentifierMapTemporary);
        }

        [Test]
        public void VerifyCreateExternalIdentifierMap()
        {
            var createdMap = this.mappingConfiguration.CreateExternalIdentifierMap("name", "", false);
            Assert.AreEqual("name", createdMap.Name);
            Assert.AreEqual("name", createdMap.ExternalModelName);
            Assert.IsEmpty(createdMap.Correspondence);
            createdMap = this.mappingConfiguration.CreateExternalIdentifierMap("name", "model", false);
            Assert.AreEqual("name", createdMap.Name);
            Assert.AreEqual("model", createdMap.ExternalModelName);
            Assert.IsEmpty(createdMap.Correspondence);
            this.mappingConfiguration.ExternalIdentifierMap.Correspondence.Add(new IdCorrespondence());
            createdMap = this.mappingConfiguration.CreateExternalIdentifierMap("name", "model", true);
            Assert.IsNotEmpty(createdMap.Correspondence);
        }

        [Test]
        public void VerifyPersistExternalIdentifierMap()
        {
            var transactionMock = new Mock<IThingTransaction>();
            var iterationClone = new Iteration();

            Assert.DoesNotThrow(() => this.mappingConfiguration.PersistExternalIdentifierMap(transactionMock.Object, iterationClone));
            this.mappingConfiguration.ExternalIdentifierMap.Name = "cfg";
            Assert.DoesNotThrow(() => this.mappingConfiguration.PersistExternalIdentifierMap(transactionMock.Object, iterationClone));

            this.mappingConfiguration.ExternalIdentifierMap.Iid = Guid.NewGuid();
            Assert.DoesNotThrow(() => this.mappingConfiguration.PersistExternalIdentifierMap(transactionMock.Object, iterationClone));

            this.mappingConfiguration.ExternalIdentifierMap.Correspondence.Add(new IdCorrespondence());

            this.mappingConfiguration.ExternalIdentifierMap.Correspondence.Add(new IdCorrespondence()
            {
                Iid = Guid.NewGuid()
            });

            Assert.DoesNotThrow(() => this.mappingConfiguration.PersistExternalIdentifierMap(transactionMock.Object, iterationClone));
            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Info), Times.Exactly(3));
        }

        [Test]
        public void VerifyRefresh()
        {
            Assert.DoesNotThrow(() => this.mappingConfiguration.RefreshExternalIdentifierMap());
            Assert.AreEqual(Guid.Empty, this.mappingConfiguration.ExternalIdentifierMap.Iid);

            var newMap = new ExternalIdentifierMap()
            {
                Iid = Guid.NewGuid(),
                Name = "cfg",
                Correspondence =
                {
                    new IdCorrespondence()
                    {
                        Iid = Guid.NewGuid(),
                        InternalThing = Guid.NewGuid(),
                        ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier())
                    },
                    new IdCorrespondence()
                    {
                        Iid = Guid.NewGuid(),
                        InternalThing = Guid.NewGuid(),
                        ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier() { Identifier = "anId" })
                    },
                }
            };

            this.mappingConfiguration.ExternalIdentifierMap.Iid = newMap.Iid;

            this.hubController.Setup(x => x.OpenIteration).Returns(new Iteration());
            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out newMap));
            Assert.DoesNotThrow(() => this.mappingConfiguration.RefreshExternalIdentifierMap());

            Assert.AreNotEqual(newMap, this.mappingConfiguration.ExternalIdentifierMap);
            Assert.IsNotNull(this.mappingConfiguration.ExternalIdentifierMap.Original);
        }

        [Test]
        public void VerifyAddToExternalMap()
        {
            var externalId = Guid.NewGuid().ToString();
            var internalId = Guid.NewGuid();

            var newMap = new ExternalIdentifierMap()
            {
                Correspondence = 
                {
                    new IdCorrespondence()
                    {
                        Iid = Guid.NewGuid(),
                        InternalThing = internalId, 
                        ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier() { Identifier = "anId" , MappingDirection = MappingDirection.FromHubToDst})
                    },
                    new IdCorrespondence()
                    {
                        Iid = Guid.NewGuid(),
                        InternalThing = internalId,
                        ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier() { Identifier = externalId , MappingDirection = MappingDirection.FromHubToDst})
                    },
                    new IdCorrespondence()
                    {
                        Iid = Guid.NewGuid(),
                        InternalThing = internalId,
                        ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier() { Identifier = externalId , MappingDirection = MappingDirection.FromDstToHub})
                    }
                }
            };

            this.mappingConfiguration.ExternalIdentifierMap = newMap;
            Assert.DoesNotThrow(() => this.mappingConfiguration.AddToExternalIdentifierMap(internalId, externalId, MappingDirection.FromDstToHub));
            Assert.DoesNotThrow(() => this.mappingConfiguration.AddToExternalIdentifierMap(internalId, externalId, MappingDirection.FromHubToDst));
            Assert.DoesNotThrow(() => this.mappingConfiguration.AddToExternalIdentifierMap(internalId, externalId, MappingDirection.FromDstToHub));
            Assert.DoesNotThrow(() => this.mappingConfiguration.AddToExternalIdentifierMap(Guid.NewGuid(), "externalId", MappingDirection.FromDstToHub));
            Assert.AreEqual(4, this.mappingConfiguration.ExternalIdentifierMap.Correspondence.Count);
        }

        [Test]
        public void VerifyLoadMappingFromDst()
        {
            var blockElement1 = this.CreateElement(StereotypeKind.Block);
            var blockElement2 = this.CreateElement(StereotypeKind.Block);
            var requirementElement = this.CreateElement(StereotypeKind.Requirement);
            var requirementElement2 = this.CreateElement(StereotypeKind.Requirement);

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            var elementDefinition2 = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            var requirement = new CDP4Common.EngineeringModelData.Requirement()
            {
                Iid = Guid.NewGuid()
            };

            var requirement2 = new CDP4Common.EngineeringModelData.Requirement()
            {
                Iid = Guid.NewGuid()
            };

            var iteration = new Iteration();
            this.hubController.Setup(x => x.OpenIteration).Returns(iteration);
            this.hubController.Setup(x => x.GetThingById(elementDefinition.Iid, iteration, out elementDefinition)).Returns(true);
            this.hubController.Setup(x => x.GetThingById(elementDefinition2.Iid, iteration, out elementDefinition2)).Returns(false);
            this.hubController.Setup(x => x.GetThingById(requirement.Iid, iteration, out requirement)).Returns(true);
            this.hubController.Setup(x => x.GetThingById(requirement2.Iid, iteration, out requirement2)).Returns(false);

            var map = new ExternalIdentifierMap();
        
            map.Correspondence.Add(new IdCorrespondence()
            {
                Iid = Guid.NewGuid(),
                InternalThing = elementDefinition.Iid, 
                ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier()
                {
                    Identifier = blockElement1.Object.ElementGUID
                })
            });

            map.Correspondence.Add(new IdCorrespondence()
            {
                Iid = Guid.NewGuid(),
                InternalThing = elementDefinition.Iid,
                ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier()
                {
                    Identifier = "blockElement1.Object.ElementGUID"
                })
            });

            map.Correspondence.Add(new IdCorrespondence()
            {
                Iid = Guid.NewGuid(),
                InternalThing = requirement.Iid,
                ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier()
                {
                    Identifier = requirementElement.Object.ElementGUID
                })
            });

            map.Correspondence.Add(new IdCorrespondence()
            {
                Iid = Guid.NewGuid(),
                InternalThing = elementDefinition2.Iid,
                ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier()
                {
                    Identifier = blockElement2.Object.ElementGUID
                })
            });

            map.Correspondence.Add(new IdCorrespondence()
            {
                Iid = Guid.NewGuid(),
                InternalThing = requirement2.Iid,
                ExternalId = JsonConvert.SerializeObject(new ExternalIdentifier()
                {
                    Identifier = requirementElement2.Object.ElementGUID
                })
            });

            this.mappingConfiguration.ExternalIdentifierMap = map;

            var loadedElement = this.mappingConfiguration.LoadMappingFromDstToHub(this.repository.Object);
            Assert.AreEqual(2, loadedElement.Count);
        }
    }
}
