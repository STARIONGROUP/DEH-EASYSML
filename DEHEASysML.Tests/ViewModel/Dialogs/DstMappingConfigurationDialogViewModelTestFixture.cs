// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstMappingConfigurationDialogViewModelTestFixture.cs" company="RHEA System S.A.">
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

    [TestFixture]
    public class DstMappingConfigurationDialogViewModelTestFixture
    {
        private DstMappingConfigurationDialogViewModel viewModel;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Mock<IEnterpriseArchitectObjectBrowserViewModel> eaObjectBrowser;
        private Mock<IObjectBrowserViewModel> objectBrowser;
        private Mock<IRequirementsBrowserViewModel> requirementBrowser;
        private ReactiveList<object> eaObjectBrowserSelectedThings;
        private ReactiveList<object> objectBrowserSelectedThings;
        private ReactiveList<object> requirementBrowserSelectedThings;
        private ReactiveList<IMappedElementRowViewModel> dstMapResult;
        private List<IMappedElementRowViewModel> premapResult;
        private Mock<ICloseWindowBehavior> closeWindow;
        private Mock<IStatusBarControlViewModel> statusBar;

        [SetUp]
        public void Setup()
        {
            this.eaObjectBrowserSelectedThings= new ReactiveList<object>();
            this.objectBrowserSelectedThings= new ReactiveList<object>();
            this.requirementBrowserSelectedThings= new ReactiveList<object>();
            this.dstMapResult = new ReactiveList<IMappedElementRowViewModel>();
            this.premapResult = new List<IMappedElementRowViewModel>();

            this.hubController = new Mock<IHubController>();
            
            this.closeWindow = new Mock<ICloseWindowBehavior>();
            this.closeWindow.Setup(x => x.Close());

            this.statusBar = new Mock<IStatusBarControlViewModel>();

            var repository = new Mock<Repository>();
            repository.Setup(x => x.Models).Returns(new EnterpriseArchitectCollection());
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.CurrentRepository).Returns(repository.Object);
            this.dstController.Setup(x => x.DstMapResult).Returns(this.dstMapResult);
            this.dstController.Setup(x => x.PreMap(It.IsAny<List<IMappedElementRowViewModel>>(), MappingDirection.FromDstToHub)).Returns(this.premapResult);
            this.dstController.Setup(x => x.Map(It.IsAny<List<IMappedElementRowViewModel>>(), MappingDirection.FromDstToHub));

            this.eaObjectBrowser = new Mock<IEnterpriseArchitectObjectBrowserViewModel>();
            this.eaObjectBrowser.Setup(x => x.SelectedThings).Returns(this.eaObjectBrowserSelectedThings);
            
            this.eaObjectBrowser.Setup(x =>
                x.BuildTree(It.IsAny<IEnumerable<Package>>(), It.IsAny<IEnumerable<Element>>(), It.IsAny<IEnumerable<int>>()));

            this.objectBrowser = new Mock<IObjectBrowserViewModel>();
            this.objectBrowser.Setup(x => x.SelectedThings).Returns(this.objectBrowserSelectedThings);

            this.requirementBrowser = new Mock<IRequirementsBrowserViewModel>();
            this.requirementBrowser.Setup(x => x.SelectedThings).Returns(this.requirementBrowserSelectedThings);

            this.viewModel = new DstMappingConfigurationDialogViewModel(this.hubController.Object, this.dstController.Object,
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
        public void VerifyProperties()
        {
            Assert.IsFalse(this.viewModel.IsBusy);
            Assert.IsNull(this.viewModel.SelectedItem);
            Assert.IsNull(this.viewModel.SelectedObjectBrowserThing);
            Assert.IsEmpty(this.viewModel.MappedElements);
            Assert.IsNotNull(this.viewModel.EnterpriseArchitectObjectBrowser);
            Assert.IsNotNull(this.viewModel.ObjectBrowser);
            Assert.IsNotNull(this.viewModel.RequirementsBrowser);
            Assert.IsFalse(this.viewModel.CanExecuteMapToNewElement);
            Assert.IsEmpty(this.viewModel.ContextMenu);
            Assert.IsNotNull(this.viewModel.MapToNewElementCommand);
            Assert.IsNotNull(this.viewModel.CancelCommand);
            Assert.IsNotNull(this.viewModel.ResetCommand);
            Assert.IsNotNull(this.viewModel.ContinueCommand);
            Assert.IsNotNull(this.viewModel.CloseWindowBehavior);
        }

        [Test]
        public void VerifyInitialize()
        {
            var elements = new List<Element>();
            Assert.DoesNotThrow(() => this.viewModel.Initialize(elements, new List<int>()));

            var block = this.CreateElement(StereotypeKind.Block);
            var requirement = this.CreateElement(StereotypeKind.Requirement);
            var valueProperty = this.CreateElement(StereotypeKind.ValueProperty);
            elements.Add(block.Object);
            elements.Add(requirement.Object);
            elements.Add(valueProperty.Object);
            Assert.DoesNotThrow(() => this.viewModel.Initialize(elements, new List<int>()));
            Assert.IsEmpty(this.viewModel.MappedElements);

            this.premapResult.Add(new EnterpriseArchitectBlockElement(null, block.Object, MappingDirection.FromDstToHub){ShouldCreateNewTargetElement = false});
            this.premapResult.Add(new EnterpriseArchitectRequirementElement(null, requirement.Object, MappingDirection.FromDstToHub){ShouldCreateNewTargetElement = true});
            Assert.DoesNotThrow(() => this.viewModel.Initialize(elements, new List<int>()));
            Assert.IsNotEmpty(this.viewModel.MappedElements);

            Assert.AreEqual(MappedRowStatus.ExistingElement, this.viewModel.MappedElements.First().MappedRowStatus);
            Assert.AreEqual(MappedRowStatus.NewElement, this.viewModel.MappedElements.Last().MappedRowStatus);

            this.dstMapResult.AddRange(this.premapResult);
            this.premapResult.Clear();

            Assert.DoesNotThrow(() => this.viewModel.Initialize(elements, new List<int>()));
            Assert.IsNotEmpty(this.viewModel.MappedElements);

            Assert.IsTrue(this.viewModel.MappedElements.All(x => x.MappedRowStatus == MappedRowStatus.ExistingMapping));
        }

        [Test]
        public void VerifyCommands()
        {
            Assert.DoesNotThrow(() => this.viewModel.ContinueCommand.Execute(null));
            Assert.IsFalse(this.viewModel.MapToNewElementCommand.CanExecute(null));
            var block = this.CreateElement(StereotypeKind.Block);
            block.Setup(x => x.Name).Returns("aBlock");
            this.viewModel.SelectedItem = new EnterpriseArchitectBlockElement(null, block.Object, MappingDirection.FromDstToHub);
            this.viewModel.SelectedObjectBrowserThing = new CDP4Common.EngineeringModelData.Requirement();
            Assert.IsFalse(this.viewModel.MapToNewElementCommand.CanExecute(null));
            
            this.viewModel.SelectedObjectBrowserThing = new ElementDefinition()
            {
                Name = "Name"
            };

            Assert.IsTrue(this.viewModel.MapToNewElementCommand.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.MapToNewElementCommand.Execute(null));
            Assert.AreEqual(MappedRowStatus.ExistingElement, this.viewModel.SelectedItem.MappedRowStatus);
            Assert.IsNotEmpty(this.viewModel.SelectedItem.SourceElementName);
            Assert.IsNotEmpty(this.viewModel.SelectedItem.TargetElementName);

            var requirement = this.CreateElement(StereotypeKind.Requirement);
            requirement.Setup(x => x.Name).Returns("arequirement");
           
            var requirementSpecification1 = new RequirementsSpecification();

            var requirement1 = new CDP4Common.EngineeringModelData.Requirement()
            {
                Iid = Guid.NewGuid(),
                Container = requirementSpecification1
            };

            requirementSpecification1.Requirement.Add(requirement1);
            this.viewModel.SelectedItem = new EnterpriseArchitectRequirementElement(requirement1, requirement.Object, MappingDirection.FromDstToHub);

            Assert.IsFalse(this.viewModel.CanExecuteMapToNewElement);
            var requirementSpecification = new RequirementsSpecification();

            this.viewModel.SelectedObjectBrowserThing = new CDP4Common.EngineeringModelData.Requirement()
            {
                Container = requirementSpecification
            };

            requirementSpecification.Requirement.Add(this.viewModel.SelectedObjectBrowserThing as CDP4Common.EngineeringModelData.Requirement);

            Assert.IsTrue(this.viewModel.MapToNewElementCommand.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.MapToNewElementCommand.Execute(null));
            Assert.AreEqual(MappedRowStatus.ExistingElement, this.viewModel.SelectedItem.MappedRowStatus);

            this.viewModel.SelectedItem = new EnterpriseArchitectRequirementElement(requirement1.Clone(false), requirement.Object, MappingDirection.FromDstToHub);
            requirementSpecification1.Requirement.Add(requirement1);

            Assert.DoesNotThrow(() => this.viewModel.MapToNewElementCommand.Execute(null));

            Assert.IsNotEmpty(this.viewModel.SelectedItem.SourceElementName);
            Assert.IsNotEmpty(this.viewModel.SelectedItem.TargetElementName);

            Assert.DoesNotThrow(() => this.viewModel.ResetCommand.Execute(null));
            Assert.DoesNotThrow(() => this.viewModel.CancelCommand.Execute(null));

            this.closeWindow.Verify(x => x.Close(), Times.Exactly(2));
        }

        [Test]
        public void VerifyObservables()
        {
            var session = new Mock<ISession>();
            var domain = new DomainOfExpertise();

            this.objectBrowserSelectedThings.Add(new ElementDefinitionRowViewModel(new ElementDefinition(), domain, session.Object, null));
            this.objectBrowserSelectedThings.Add(new ElementDefinitionRowViewModel(new ElementDefinition(), domain, session.Object, null));
            Assert.AreEqual(1, this.objectBrowserSelectedThings.Count);
            Assert.IsNotNull(this.viewModel.SelectedObjectBrowserThing);

            var block = this.CreateElement(StereotypeKind.Block);
            var requirementElement = this.CreateElement(StereotypeKind.Requirement);
            requirementElement.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection());

            this.eaObjectBrowserSelectedThings.Add(new BlockRowViewModel(null, block.Object, false));
            this.eaObjectBrowserSelectedThings.Add(new ElementRequirementRowViewModel(null, requirementElement.Object));
            Assert.AreEqual(1, this.objectBrowserSelectedThings.Count);
            Assert.IsNull(this.viewModel.SelectedItem);

            this.viewModel.MappedElements.Add(new EnterpriseArchitectRequirementElement(null, requirementElement.Object, MappingDirection.FromDstToHub));
            this.viewModel.MappedElements.Add(new EnterpriseArchitectBlockElement(null, block.Object, MappingDirection.FromDstToHub));

            this.eaObjectBrowserSelectedThings.Add(new BlockRowViewModel(null, block.Object, false));
            Assert.AreEqual(this.viewModel.MappedElements[1], this.viewModel.SelectedItem);
            this.eaObjectBrowserSelectedThings.Add(new ElementRequirementRowViewModel(null, requirementElement.Object));
            Assert.AreEqual(this.viewModel.MappedElements[0], this.viewModel.SelectedItem);
            var valueProperty = this.CreateElement(StereotypeKind.ValueProperty);
            valueProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection());
            this.eaObjectBrowserSelectedThings.Add(new ValuePropertyRowViewModel(null, valueProperty.Object));
            Assert.IsNull(this.viewModel.SelectedItem);

            this.requirementBrowserSelectedThings.Add(new RequirementRowViewModel(new CDP4Common.EngineeringModelData.Requirement(), session.Object, null));
            this.requirementBrowserSelectedThings.Add(new RequirementRowViewModel(new CDP4Common.EngineeringModelData.Requirement(), session.Object, null));
            Assert.AreEqual(1, this.requirementBrowserSelectedThings.Count);
            Assert.IsNotNull(this.viewModel.SelectedObjectBrowserThing);
        }
    }
}
