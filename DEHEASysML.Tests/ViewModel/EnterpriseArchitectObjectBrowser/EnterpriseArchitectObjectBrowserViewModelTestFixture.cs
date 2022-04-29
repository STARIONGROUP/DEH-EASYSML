// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectObjectBrowserViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.ViewModel.EnterpriseArchitectObjectBrowser
{
    using System.Collections.Generic;
    using System.Linq;

    using DEHEASysML.Enumerators;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser;

    using EA;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class EnterpriseArchitectObjectBrowserViewModelTestFixture
    {
        private EnterpriseArchitectObjectBrowserViewModel viewModel;
        private List<Package> models;
        private Mock<Element> requirement;
        private Mock<Element> block;
        private Mock<Element> valueType;

        [SetUp]
        public void Setup()
        {
            this.viewModel = new EnterpriseArchitectObjectBrowserViewModel();
            this.models = new List<Package>();

            this.requirement = new Mock<Element>();
            this.requirement.Setup(x => x.Name).Returns("A requirement");
            this.requirement.Setup(x => x.ElementGUID).Returns("0001");
            this.requirement.Setup(x => x.Stereotype).Returns(StereotypeKind.Requirement.ToString());
            var taggedValue = new Mock<TaggedValue>();
            taggedValue.Setup(x => x.Name).Returns("Text");
            taggedValue.Setup(x => x.Value).Returns("Requirement Text");
            this.requirement.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection() { taggedValue.Object });
            this.block = new Mock<Element>();
            this.block.Setup(x => x.Name).Returns("a Block");
            this.block.Setup(x => x.ElementGUID).Returns("0002");
            this.block.Setup(x => x.Stereotype).Returns(StereotypeKind.Block.ToString());

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
            firstModel.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection(){subPackage.Object});
            firstModel.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection());

            var secondModel = new Mock<Package>();
            secondModel.Setup(x => x.PackageID).Returns(2);

            this.models.Add(firstModel.Object);
            this.models.Add(secondModel.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNull(this.viewModel.IsBusy);
            Assert.IsNull(this.viewModel.SelectedThing);
            Assert.IsEmpty(this.viewModel.SelectedThings);
            Assert.IsEmpty(this.viewModel.ContextMenu);
            Assert.IsEmpty(this.viewModel.Things);
            Assert.IsNotEmpty(this.viewModel.Caption);
            Assert.IsNotEmpty(this.viewModel.ToolTip);
            Assert.IsNull(this.viewModel.MapCommand);
            Assert.IsNull(this.viewModel.CanMap);
            Assert.DoesNotThrow(() => this.viewModel.MapCommand = ReactiveCommand.Create());
            Assert.DoesNotThrow(() => this.viewModel.SelectedThings = new ReactiveList<object>());
            Assert.DoesNotThrow(() => this.viewModel.SelectedThing = null);
            Assert.DoesNotThrow(() => this.viewModel.IsBusy = null);
            Assert.DoesNotThrow(() => this.viewModel.CanMap = this.viewModel.WhenAnyValue(x => x.IsBusy.Value));
        }

        [Test]
        public void VerifyBuildTree()
        {
            Assert.DoesNotThrow(() => this.viewModel.BuildTree(this.models, new List<Element>(), new List<int>()));
            Assert.IsEmpty(this.viewModel.Things);
            Assert.DoesNotThrow(() => this.viewModel.BuildTree(this.models, new List<Element>(), new List<int>(){1}));
            Assert.AreEqual(1, this.viewModel.Things.Count);
            var modelRow = this.viewModel.Things.First();
            Assert.IsEmpty(modelRow.ContainedRows);
            Assert.AreEqual(this.models.First().Name, modelRow.Name);
            Assert.IsNull(modelRow.Value);
            Assert.AreEqual("Package", modelRow.RowType);
            Assert.DoesNotThrow(() => this.viewModel.BuildTree(this.models, new List<Element>(), new List<int>() { 1, 3 }));
            Assert.DoesNotThrow(() => this.viewModel.BuildTree(this.models, new List<Element>(){this.requirement.Object}, new List<int>() { 1, 3 }));
            Assert.DoesNotThrow(() => this.viewModel.BuildTree(this.models, new List<Element>(){this.requirement.Object, this.block.Object}, new List<int>() { 1, 3 }));
            Assert.DoesNotThrow(() => this.viewModel.BuildTree(this.models, new List<Element>(){this.requirement.Object, this.block.Object, this.valueType.Object}, new List<int>() { 1, 3 }));
            Assert.DoesNotThrow(() => this.viewModel.BuildTree(this.models, new List<Element>(){this.requirement.Object, this.block.Object, this.valueType.Object}, new List<int>() { 1}));
        }
    }
}
