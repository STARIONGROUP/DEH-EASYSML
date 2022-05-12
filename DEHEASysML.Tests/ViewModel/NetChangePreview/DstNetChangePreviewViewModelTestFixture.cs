// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstNetChangePreviewViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.ViewModel.NetChangePreview
{
    using System;
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Events;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.NetChangePreview;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;

    using EA;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class DstNetChangePreviewViewModelTestFixture
    {
        private DstNetChangePreviewViewModel viewModel;
        private Mock<IDstController> dstController;
        private ReactiveList<IMappedElementRowViewModel> hubMapResult;
        private ReactiveList<Element> selectedMapElements;
        private List<Package> models;
        private Mock<Element> requirement;
        private Mock<Element> block;
        private Mock<Element> valueType;
        private Mock<Repository> repository;
        private Dictionary<string, string> updatedValues;
        private Dictionary<string, (string,string)> updatedRequirementValues;

        [SetUp]
        public void Setup()
        {
            this.updatedValues = new Dictionary<string, string>();
            this.updatedRequirementValues = new Dictionary<string, (string, string)>();
            this.hubMapResult = new ReactiveList<IMappedElementRowViewModel>();
            this.selectedMapElements = new ReactiveList<Element>();
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.HubMapResult).Returns(this.hubMapResult);
            this.dstController.Setup(x => x.SelectedHubMapResultForTransfer).Returns(this.selectedMapElements);
            this.dstController.Setup(x => x.IsFileOpen).Returns(false);
            this.dstController.Setup(x => x.UpdatedValuePropretyValues).Returns(this.updatedValues);
            this.dstController.Setup(x => x.UpdatedRequirementValues).Returns(this.updatedRequirementValues);

            this.viewModel = new DstNetChangePreviewViewModel(this.dstController.Object);
            this.models = new List<Package>();
            var taggedValue = new Mock<TaggedValue>();
            taggedValue.Setup(x => x.Name).Returns("Text");
            taggedValue.Setup(x => x.Value).Returns("Requirement Text");
            this.requirement = new Mock<Element>();
            this.requirement.Setup(x => x.Name).Returns("A requirement");
            this.requirement.Setup(x => x.ElementGUID).Returns("0001");
            this.requirement.Setup(x => x.Stereotype).Returns(StereotypeKind.Requirement.ToString());
            this.requirement.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection() { taggedValue.Object });
            this.requirement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());

            var valueProperty = new Mock<Element>();
            valueProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            valueProperty.Setup(x => x.Name).Returns("mass");
            valueProperty.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            var partProperty = new Mock<Element>();
            partProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.PartProperty.ToString());
            partProperty.Setup(x => x.Name).Returns("a contained element");
            var taggedValueProperty = new Mock<TaggedValue>();
            taggedValueProperty.Setup(x => x.Name).Returns("default");
            taggedValueProperty.Setup(x => x.Value).Returns("42");
            valueProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection() { taggedValueProperty.Object });
            this.block = new Mock<Element>();
            this.block.Setup(x => x.Name).Returns("a Block");
            this.block.Setup(x => x.ElementGUID).Returns("0002");
            this.block.Setup(x => x.Stereotype).Returns(StereotypeKind.Block.ToString());
            
            this.block.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection()
            {
                valueProperty.Object, partProperty.Object
            });

            this.valueType = new Mock<Element>();
            this.valueType.Setup(x => x.Name).Returns("a ValueType");
            this.valueType.Setup(x => x.ElementGUID).Returns("0003");
            this.valueType.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueType.ToString());

            var subPackage = new Mock<Package>();
            subPackage.Setup(x => x.PackageID).Returns(3);
            subPackage.Setup(x => x.Name).Returns("subPackage");
            subPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());

            subPackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection()
            {
                this.requirement.Object, this.block.Object, this.valueType.Object
            });

            var firstModel = new Mock<Package>();
            firstModel.Setup(x => x.PackageID).Returns(1);
            firstModel.Setup(x => x.Name).Returns("Model");
            firstModel.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection() { subPackage.Object });
            firstModel.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection());

            var secondModel = new Mock<Package>();
            secondModel.Setup(x => x.PackageID).Returns(2);
            secondModel.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());
            secondModel.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection());

            this.models.Add(firstModel.Object);
            this.models.Add(secondModel.Object);

            var collection = new EnterpriseArchitectCollection();
            collection.AddRange(this.models);

            this.repository = new Mock<Repository>();
            this.repository.Setup(x => x.Models).Returns(collection);

            this.dstController.Setup(x => x.CurrentRepository).Returns(this.repository.Object);
            this.dstController.Setup(x => x.RetrieveAllParentsIdPackage(It.IsAny<IEnumerable<Element>>())).Returns(new List<int>() { 1, 3 });
        }

        [TearDown]
        public void Teardown()
        {
            CDPMessageBus.Current.ClearSubscriptions();
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.viewModel.SelectAllCommand);
            Assert.IsNotNull(this.viewModel.DeselectAllCommand);
            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateDstNetChangePreview()));
            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateDstNetChangePreview(true)));
        }

        [Test]
        public void VerifyPopulateContextMenu()
        {
            Assert.DoesNotThrow(() => this.viewModel.PopulateContextMenu());
            this.hubMapResult.Add(new ElementDefinitionMappedElement(new ElementDefinition(),null, MappingDirection.FromHubToDst));
            Assert.DoesNotThrow(() => this.viewModel.PopulateContextMenu());
            Assert.AreEqual(2, this.viewModel.ContextMenu.Count);
        }

        [Test]
        public void VerifyComputesValues()
        {
            Assert.DoesNotThrow(() => this.viewModel.ComputeValues());
            this.dstController.Setup(x => x.IsFileOpen).Returns(true);
            Assert.DoesNotThrow(() => this.viewModel.ComputeValues());
        }

        [Test]
        public void VerifySelectDeselectCommands()
        {
            Assert.DoesNotThrow(() => this.viewModel.SelectAllCommand.Execute(null));
            Assert.IsEmpty(this.selectedMapElements);
            Assert.DoesNotThrow(() => this.viewModel.DeselectAllCommand.Execute(null));
            this.hubMapResult.Add(new ElementDefinitionMappedElement(null, this.block.Object, MappingDirection.FromHubToDst));
            this.hubMapResult.Add(new RequirementMappedElement(null, this.requirement.Object, MappingDirection.FromHubToDst));
            this.dstController.Setup(x => x.IsFileOpen).Returns(true);
            this.viewModel.ComputeValues();
            Assert.DoesNotThrow(() => this.viewModel.SelectAllCommand.Execute(null));
            Assert.IsNotEmpty(this.selectedMapElements);
            Assert.DoesNotThrow(() => this.viewModel.DeselectAllCommand.Execute(null));
            Assert.IsEmpty(this.selectedMapElements);
        }
    }
}
