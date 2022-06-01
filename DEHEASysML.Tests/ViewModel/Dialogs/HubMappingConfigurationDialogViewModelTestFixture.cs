// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubMappingConfigurationDialogViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Dialogs;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Interfaces;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows;
    using DEHEASysML.ViewModel.RequirementsBrowser;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using EA;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    [TestFixture]
    public class HubMappingConfigurationDialogViewModelTestFixture
    {
        private HubMappingConfigurationDialogViewModel viewModel;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Mock<IEnterpriseArchitectObjectBrowserViewModel> eaObjectBrowser;
        private Mock<IObjectBrowserViewModel> objectBrowser;
        private Mock<IRequirementsBrowserViewModel> requirementBrowser;
        private ReactiveList<object> eaObjectBrowserSelectedThings;
        private ReactiveList<object> objectBrowserSelectedThings;
        private ReactiveList<object> requirementBrowserSelectedThings;
        private ReactiveList<IMappedElementRowViewModel> hubMapResult;
        private List<IMappedElementRowViewModel> premapResult;
        private Mock<ICloseWindowBehavior> closeWindow;
        private Mock<IStatusBarControlViewModel> statusBar;

        [SetUp]
        public void Setup()
        {
            this.eaObjectBrowserSelectedThings = new ReactiveList<object>();
            this.objectBrowserSelectedThings = new ReactiveList<object>();
            this.requirementBrowserSelectedThings = new ReactiveList<object>();
            this.hubMapResult = new ReactiveList<IMappedElementRowViewModel>();
            this.premapResult = new List<IMappedElementRowViewModel>();

            this.hubController = new Mock<IHubController>();

            this.closeWindow = new Mock<ICloseWindowBehavior>();
            this.closeWindow.Setup(x => x.Close());

            this.statusBar = new Mock<IStatusBarControlViewModel>();

            var repository = new Mock<Repository>();
            repository.Setup(x => x.Models).Returns(new EnterpriseArchitectCollection());
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.CurrentRepository).Returns(repository.Object);
            this.dstController.Setup(x => x.HubMapResult).Returns(this.hubMapResult);
            this.dstController.Setup(x => x.PreMap(It.IsAny<List<IMappedElementRowViewModel>>(), MappingDirection.FromHubToDst)).Returns(this.premapResult);
            this.dstController.Setup(x => x.Map(It.IsAny<List<IMappedElementRowViewModel>>(), MappingDirection.FromHubToDst));
            this.dstController.Setup(x => x.GetAllBlocksAndRequirementsOfRepository()).Returns(new List<Element>());
            this.dstController.Setup(x => x.RetrieveAllParentsIdPackage(It.IsAny<List<Element>>()));

            this.eaObjectBrowser = new Mock<IEnterpriseArchitectObjectBrowserViewModel>();
            this.eaObjectBrowser.Setup(x => x.SelectedThings).Returns(this.eaObjectBrowserSelectedThings);

            this.eaObjectBrowser.Setup(x =>
                x.BuildTree(It.IsAny<IEnumerable<Package>>(), It.IsAny<IEnumerable<Element>>(), It.IsAny<IEnumerable<int>>()));

            this.objectBrowser = new Mock<IObjectBrowserViewModel>();
            this.objectBrowser.Setup(x => x.SelectedThings).Returns(this.objectBrowserSelectedThings);

            this.requirementBrowser = new Mock<IRequirementsBrowserViewModel>();
            this.requirementBrowser.Setup(x => x.SelectedThings).Returns(this.requirementBrowserSelectedThings);

            this.viewModel = new HubMappingConfigurationDialogViewModel(this.hubController.Object, this.dstController.Object,
                this.eaObjectBrowser.Object, this.objectBrowser.Object, this.requirementBrowser.Object, this.statusBar.Object)
            {
                CloseWindowBehavior = this.closeWindow.Object
            };
        }

        private Mock<Element> CreateElement(StereotypeKind stereotype)
        {
            var element = new Mock<Element>();
            var guid = Guid.NewGuid();
            element.Setup(x => x.ElementGUID).Returns(guid.ToString());
            element.Setup(x => x.Stereotype).Returns(stereotype.ToString());
            return element;
        }

        [Test]
        public void VerifyInitialize()
        {
            Assert.DoesNotThrow(() => this.viewModel.Initialize(new List<Thing>()));

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            var requirement = new Requirement()
            {
                Iid = Guid.NewGuid()
            };

            var things = new List<Thing>()
            {
                elementDefinition, requirement
            };

            Assert.DoesNotThrow(() => this.viewModel.Initialize(things));
            Assert.IsEmpty(this.viewModel.MappedElements);

            this.premapResult.Add(new ElementDefinitionMappedElement(elementDefinition, null, MappingDirection.FromHubToDst) { ShouldCreateNewTargetElement = false });
            this.premapResult.Add(new RequirementMappedElement(requirement,null, MappingDirection.FromHubToDst) { ShouldCreateNewTargetElement = true });
            Assert.DoesNotThrow(() => this.viewModel.Initialize(things));
            Assert.IsNotEmpty(this.viewModel.MappedElements);

            Assert.AreEqual(MappedRowStatus.ExistingElement, this.viewModel.MappedElements.First().MappedRowStatus);
            Assert.AreEqual(MappedRowStatus.NewElement, this.viewModel.MappedElements.Last().MappedRowStatus);

            this.hubMapResult.AddRange(this.premapResult);
            this.premapResult.Clear();

            Assert.DoesNotThrow(() => this.viewModel.Initialize(things));
            Assert.IsNotEmpty(this.viewModel.MappedElements);

            Assert.IsTrue(this.viewModel.MappedElements.All(x => x.MappedRowStatus == MappedRowStatus.ExistingMapping));
        }

        [Test]
        public void VerifyObservables()
        {
            var session = new Mock<ISession>();
            var domain = new DomainOfExpertise();

            var elementDefinition1 = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            var elementDefinition2 = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            this.objectBrowserSelectedThings.Add(new ElementDefinitionRowViewModel(elementDefinition1, domain, session.Object, null));
            this.objectBrowserSelectedThings.Add(new ElementDefinitionRowViewModel(elementDefinition2, domain, session.Object, null));
            Assert.AreEqual(1, this.objectBrowserSelectedThings.Count);
            Assert.IsNotNull(this.viewModel.SelectedObjectBrowserThing);

            var block = this.CreateElement(StereotypeKind.Block);
            var requirementElement = this.CreateElement(StereotypeKind.Requirement);
            requirementElement.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection());

            this.eaObjectBrowserSelectedThings.Add(new BlockRowViewModel(null, block.Object, false));
            this.eaObjectBrowserSelectedThings.Add(new ElementRequirementRowViewModel(null, requirementElement.Object));
            Assert.AreEqual(1, this.objectBrowserSelectedThings.Count);
            Assert.IsNull(this.viewModel.SelectedItem);

            this.viewModel.MappedElements.Add(new ElementDefinitionMappedElement(elementDefinition1, requirementElement.Object, MappingDirection.FromHubToDst));
            this.viewModel.MappedElements.Add(new ElementDefinitionMappedElement(elementDefinition2, block.Object, MappingDirection.FromHubToDst));

            this.eaObjectBrowserSelectedThings.Add(new BlockRowViewModel(null, block.Object, false));

            this.requirementBrowserSelectedThings.Add(new RequirementRowViewModel(new Requirement(), session.Object, null));
            this.requirementBrowserSelectedThings.Add(new RequirementRowViewModel(new Requirement(), session.Object, null));
            Assert.AreEqual(1, this.requirementBrowserSelectedThings.Count);
        }

        [Test]
        public void VerifyCommands()
        {
            Assert.DoesNotThrow(() => this.viewModel.ContinueCommand.Execute(null));
            Assert.IsFalse(this.viewModel.MapToNewElementCommand.CanExecute(null));

            var requirementElement = this.CreateElement(StereotypeKind.Requirement);
            var blockElement = this.CreateElement(StereotypeKind.Block);

            this.viewModel.SelectedElement = blockElement.Object;
            this.viewModel.SelectedItem = new ElementDefinitionMappedElement(new ElementDefinition(), null, MappingDirection.FromHubToDst);
            Assert.IsTrue(this.viewModel.MapToNewElementCommand.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.MapToNewElementCommand.Execute(null));
            Assert.AreEqual(MappedRowStatus.ExistingElement, this.viewModel.SelectedItem.MappedRowStatus);

            this.viewModel.SelectedElement = requirementElement.Object;
            this.viewModel.SelectedItem = new RequirementMappedElement(null, null, MappingDirection.FromHubToDst);
            Assert.IsTrue(this.viewModel.MapToNewElementCommand.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.MapToNewElementCommand.Execute(null));
            Assert.AreEqual(MappedRowStatus.ExistingElement, this.viewModel.SelectedItem.MappedRowStatus);
        }
    }
}
