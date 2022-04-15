// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstControllerTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.DstController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class DstControllerTestFixture
    {
        private DstController dstController;
        private Mock<IHubController> hubController;
        private Mock<Repository> repository;
        private Mock<Package> package;
        private Mock<IMappingEngine> mappingEngine;
        private Mock<IStatusBarControlViewModel> statusBarControlViewModel;

        [SetUp]
        public void Setup()
        {
            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.Close());

            this.repository = new Mock<Repository>();
            this.package = new Mock<Package>();
            this.package.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection());

            var requirementPackage = new Mock<Package>();
            var blocPackage = new Mock<Package>();
            var valueTypePackage = new Mock<Package>();
            blocPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection() { valueTypePackage.Object });
            requirementPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());

            var valueTypeElement = new Mock<Element>();
            valueTypeElement.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueType.ToString());
            valueTypePackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection() {valueTypeElement.Object });
            valueTypePackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());

            var blockElement = new Mock<Element>();
            blockElement.Setup(x => x.Stereotype).Returns(StereotypeKind.Block.ToString());
            blocPackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection() { blockElement.Object });

            var requirement = new Mock<Element>();
            requirement.Setup(x => x.Stereotype).Returns(StereotypeKind.Requirement.ToString());
            requirementPackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection() { requirement.Object });
            this.package.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection() { requirementPackage.Object, blocPackage.Object });

            this.mappingEngine = new Mock<IMappingEngine>();
            this.statusBarControlViewModel = new Mock<IStatusBarControlViewModel>();

            this.statusBarControlViewModel.Setup(x => 
                x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.dstController = new DstController(this.hubController.Object, this.mappingEngine.Object, this.statusBarControlViewModel.Object);
        }

        public Mock<Package> CreatePackage(int id, int parentId)
        {
            var createdPackage = new Mock<Package>();
            createdPackage.Setup(x => x.PackageID).Returns(id);
            createdPackage.Setup(x => x.ParentID).Returns(parentId);
            this.repository.Setup(x => x.GetPackageByID(createdPackage.Object.PackageID)).Returns(createdPackage.Object);
            return createdPackage;
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNull(this.dstController.CurrentRepository);
            Assert.IsFalse(this.dstController.CanMap);
        }

        [Test]
        public void VerifyConnectAndDisconnect()
        {
            this.dstController.Connect(this.repository.Object);
            Assert.IsNotNull(this.dstController.CurrentRepository);
            this.dstController.Disconnect();
            Assert.IsNull(this.dstController.CurrentRepository);
            this.hubController.Verify(x => x.Close(), Times.Once);
        }

        [Test]
        public void VerifyEventListener()
        {
            Assert.DoesNotThrow(() => this.dstController.OnFileNew(this.repository.Object));
            Assert.IsNotNull(this.dstController.CurrentRepository);
            
            Assert.DoesNotThrow(() => this.dstController.OnFileClose(this.repository.Object));
            Assert.IsNotNull(this.dstController.CurrentRepository);
            
            Assert.DoesNotThrow(() => this.dstController.OnFileOpen(this.repository.Object));
            Assert.IsNotNull(this.dstController.CurrentRepository);
            
            Assert.DoesNotThrow(() => this.dstController.OnFileClose(this.repository.Object));
            Assert.IsNotNull(this.dstController.CurrentRepository);

            Assert.DoesNotThrow(() => this.dstController.OnNotifyContextItemModified(this.repository.Object, Guid.NewGuid().ToString(), ObjectType.otDiagram));
            Assert.IsNotNull(this.dstController.CurrentRepository);
        }

        [Test]
        public void VerifyGetElementsFromModel()
        {
            var model = this.package.Object;
            var requirements = this.dstController.GetAllRequirements(model);
            var valueTypes = this.dstController.GetAllValueTypes(model);
            var blocks = this.dstController.GetAllBlocks(model);

            Assert.AreEqual(1, requirements.Count);
            Assert.AreEqual(1, valueTypes.Count);
            Assert.AreEqual(1, blocks.Count);
        }

        [Test]
        public void VerifyRetrievePort()
        {
            this.dstController.CurrentRepository = this.repository.Object;

            var port = new Mock<Element>();
            port.Setup(x => x.PropertyType).Returns(322);

            var propertyType = new Mock<Element>();
            propertyType.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());

            this.repository.Setup(x => x.GetElementByID(port.Object.PropertyType)).Returns(propertyType.Object);
            var (elementPort, interfacePort) = this.dstController.ResolvePort(port.Object);
            Assert.IsNull(elementPort);
            Assert.AreEqual(propertyType.Object, interfacePort);

            var connector = new Mock<Connector>();
            connector.Setup(x => x.ClientID).Returns(52);
            connector.Setup(x => x.SupplierID).Returns(152);

            var interfaceBlock = new Mock<Element>();
            var portblock = new Mock<Element>();

            this.repository.Setup(x => x.GetElementByID(connector.Object.ClientID)).Returns(portblock.Object);
            this.repository.Setup(x => x.GetElementByID(connector.Object.SupplierID)).Returns(interfaceBlock.Object);
            propertyType.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection(){connector.Object});

            (elementPort, interfacePort) = this.dstController.ResolvePort(port.Object);
            Assert.AreEqual(elementPort, portblock.Object);
            Assert.AreEqual(interfacePort, interfaceBlock.Object);
        }

        [Test]
        public void VerifyRetrieveAllParentsIdPackage()
        {
            this.dstController.CurrentRepository = this.repository.Object;

            var element = new Mock<Element>();
            element.Setup(x => x.PackageID).Returns(5);
            this.CreatePackage(6, 2);
            this.CreatePackage(5, 4);
            this.CreatePackage(4, 3);
            this.CreatePackage(3, 1);
            this.CreatePackage(1, 0);

            Assert.DoesNotThrow(() => this.dstController.RetrieveAllParentsIdPackage(new List<Element>()));

            Assert.DoesNotThrow(() => this.dstController.RetrieveAllParentsIdPackage(new List<Element>(){element.Object}));
            var packagesId = this.dstController.RetrieveAllParentsIdPackage(new List<Element>() { element.Object }).ToList();
            Assert.AreEqual(4, packagesId.Count);
            Assert.IsFalse(packagesId.Contains(0));
            Assert.IsFalse(packagesId.Contains(6));
        }

        [Test]
        public void VerifyGetAllSelectedElements()
        {
            var collection = new EnterpriseArchitectCollection();
            this.repository.Setup(x => x.GetTreeSelectedElements()).Returns(collection);
            this.dstController.CurrentRepository = this.repository.Object;

            Assert.IsEmpty(this.dstController.GetAllSelectedElements(this.repository.Object));
            var valueProperty = new Mock<Element>();
            valueProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            collection.Add(valueProperty.Object);
            Assert.IsEmpty(this.dstController.GetAllSelectedElements(this.repository.Object));

            var block = new Mock<Element>();
            block.Setup(x => x.Stereotype).Returns(StereotypeKind.Block.ToString());
            collection.Add(block.Object);
            Assert.AreEqual(1,this.dstController.GetAllSelectedElements(this.repository.Object).Count());

            var requirement = new Mock<Element>();
            requirement.Setup(x => x.Stereotype).Returns(StereotypeKind.Block.ToString());
            collection.Add(requirement.Object);
            Assert.AreEqual(2, this.dstController.GetAllSelectedElements(this.repository.Object).Count());

            collection.Add(block.Object);
            Assert.AreEqual(2, this.dstController.GetAllSelectedElements(this.repository.Object).Count());

            collection.Add(5);
            Assert.AreEqual(2, this.dstController.GetAllSelectedElements(this.repository.Object).Count());
        }

        [Test]
        public void VerifyGetAllElementsInsidePackage()
        {
            this.repository.Setup(x => x.GetTreeSelectedPackage()).Returns(this.package.Object);
            Assert.AreEqual(2, this.dstController.GetAllElementsInsidePackage(this.repository.Object).Count());
        }

        [Test]
        public void VerifyMapAndPremap()
        {
            Assert.IsEmpty(this.dstController.DstMapResult);

            var elementsToMap = new List<IMappedElementRowViewModel>
            {
                new EnterpriseArchitectBlockElement(null, null, MappingDirection.FromDstToHub),
                new EnterpriseArchitectBlockElement(null, null, MappingDirection.FromDstToHub)
            };

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectBlockElement>)>()))
                .Returns(null);

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectRequirementElement>)>()))
                .Returns(null);

            Assert.DoesNotThrow(() => this.dstController.PreMap(elementsToMap));
            Assert.IsEmpty(this.dstController.DstMapResult);
            Assert.DoesNotThrow(() => this.dstController.Map(elementsToMap));
            Assert.IsEmpty(this.dstController.DstMapResult);

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectBlockElement>)>()))
                .Returns(new List<MappedElementDefinitionRowViewModel>());

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectRequirementElement>)>()))
                .Returns(new List<MappedRequirementRowViewModel>());

            Assert.DoesNotThrow(() => this.dstController.PreMap(elementsToMap));
            Assert.IsEmpty(this.dstController.DstMapResult);
            Assert.DoesNotThrow(() => this.dstController.Map(elementsToMap));
            Assert.IsEmpty(this.dstController.DstMapResult);

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectBlockElement>)>()))
                .Returns(new List<MappedElementDefinitionRowViewModel>()
                {
                    new MappedElementDefinitionRowViewModel(null, null, MappingDirection.FromDstToHub)
                });

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectRequirementElement>)>()))
                .Returns(new List<MappedRequirementRowViewModel>()
                {
                    new MappedRequirementRowViewModel(null, null, MappingDirection.FromDstToHub)
                });

            Assert.DoesNotThrow(() => this.dstController.PreMap(elementsToMap));
            Assert.IsEmpty(this.dstController.DstMapResult);
            Assert.DoesNotThrow(() => this.dstController.Map(elementsToMap));
            Assert.AreEqual(2,this.dstController.DstMapResult.Count);

            this.statusBarControlViewModel.Verify(x => 
                    x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()), Times.Exactly(2));
        }
    }
}
