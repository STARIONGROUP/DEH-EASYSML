// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubObjectNetChangePreviewViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using System.Collections.ObjectModel;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;
    using CDP4Dal.Permission;

    using DEHEASysML.DstController;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.NetChangePreview;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class HubObjectNetChangePreviewViewModelTestFixture
    {
        private HubObjectNetChangePreviewViewModel viewModel;
        private Mock<IHubController> hubController;
        private Mock<IObjectBrowserTreeSelectorService> objectBrowserTreeSelectorService;
        private Mock<IPermissionService> permissionService;
        private Mock<IDstController> dstController;
        private Mock<ISession> session;
        private DomainOfExpertise domain;
        private Iteration iteration;
        private Person person;
        private Participant participant;
        private ReactiveList<Thing> selectedDstMapResultForTransfer;

        [SetUp]
        public void Setup()
        {
            this.domain = new DomainOfExpertise(Guid.NewGuid(), null, null)
            {
                Name = "t",
                ShortName = "e"
            };

            this.person = new Person(Guid.NewGuid(), null, null) { GivenName = "test", DefaultDomain = this.domain };

            this.participant = new Participant(Guid.NewGuid(), null, null)
            {
                Person = this.person
            };

            var engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), null, null)
            {
                Participant = { this.participant },
                Name = "est"
            };

            this.iteration = new Iteration(Guid.NewGuid(), null, null)
            {
                IterationSetup = new IterationSetup(Guid.NewGuid(), null, null)
                {
                    IterationNumber = 23,
                    Container = engineeringModelSetup
                },
                Container = new EngineeringModel(Guid.NewGuid(), null, null)
                {
                    EngineeringModelSetup = engineeringModelSetup
                }
            };

            this.permissionService = new Mock<IPermissionService>();
            this.permissionService.Setup(x => x.CanRead(It.IsAny<Thing>())).Returns(true);
            this.permissionService.Setup(x => x.CanWrite(It.IsAny<Thing>())).Returns(true);
            this.permissionService.Setup(x => x.CanWrite(It.IsAny<ClassKind>(), It.IsAny<Thing>())).Returns(true);

            this.session = new Mock<ISession>();
            this.session.Setup(x => x.DataSourceUri).Returns("aDatasourceUri");
            this.session.Setup(x => x.ActivePerson).Returns(this.person);
            this.session.Setup(x => x.PermissionService).Returns(this.permissionService.Object);
            
            this.session.Setup(x => x.OpenIterations).Returns(
                new ReadOnlyDictionary<Iteration, Tuple<DomainOfExpertise, Participant>>(
                    new Dictionary<Iteration, Tuple<DomainOfExpertise, Participant>>()
                    {
                        {
                            this.iteration,
                            new Tuple<DomainOfExpertise, Participant>(this.domain, this.participant)
                        }
                    }));

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.Session).Returns(this.session.Object);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            this.objectBrowserTreeSelectorService = new Mock<IObjectBrowserTreeSelectorService>();
            this.objectBrowserTreeSelectorService.Setup(x => x.ThingKinds).Returns(new List<Type>() { typeof(ElementDefinition) });

            this.selectedDstMapResultForTransfer = new ReactiveList<Thing>();
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.SelectedDstMapResultForTransfer).Returns(this.selectedDstMapResultForTransfer);

            this.viewModel = new HubObjectNetChangePreviewViewModel(this.hubController.Object, this.objectBrowserTreeSelectorService.Object, 
                this.dstController.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.viewModel.SelectAllCommand);
            Assert.IsNotNull(this.viewModel.DeselectAllCommand);
            Assert.IsEmpty(this.viewModel.ContextMenu);
            Assert.IsEmpty(this.viewModel.MappedElements);
        }

        [Test]
        public void VerifyMappedElementsCollectionObservables()
        {
            Assert.DoesNotThrow(() => this.viewModel.PopulateContextMenu());
            Assert.DoesNotThrow(() => this.viewModel.MappedElements.Add(new EnterpriseArchitectRequirementElement(null, null, MappingDirection.FromDstToHub)));
            this.viewModel.MappedElements.Clear();
            Assert.DoesNotThrow(() => this.viewModel.MappedElements.Add(new EnterpriseArchitectBlockElement(null, null, MappingDirection.FromDstToHub)));
            Assert.AreEqual(2, this.viewModel.ContextMenu.Count);
        }

        [Test]
        public void VerifyComputeValues()
        {
            Assert.DoesNotThrow(() => this.viewModel.ComputeValues());

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            this.iteration.Element.Add(elementDefinition);
            elementDefinition.Container = this.iteration;
            Assert.DoesNotThrow(() => this.viewModel.ComputeValues());
            Assert.IsEmpty(this.viewModel.Things);

            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            Assert.DoesNotThrow(() => this.viewModel.ComputeValues());
            Assert.IsNotEmpty(this.viewModel.Things);
            this.viewModel.MappedElements.Add(new EnterpriseArchitectBlockElement(elementDefinition.Clone(true), null, MappingDirection.FromDstToHub));
            Assert.DoesNotThrow(() => this.viewModel.ComputeValues());

            this.viewModel.MappedElements.Clear();
            this.viewModel.MappedElements.Add(new EnterpriseArchitectBlockElement(new ElementDefinition(), null, MappingDirection.FromDstToHub));
            Assert.DoesNotThrow(() => this.viewModel.ComputeValues());
        }

        [Test]
        public void VerifySelectedThingsAndCommands()
        {
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);

            var elementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            var parameterGroup = new ParameterGroup()
            {
                Iid = Guid.NewGuid(),
                Container = elementDefinition
            };

            elementDefinition.ParameterGroup.Add(parameterGroup);

            var parameter = new Parameter()
            {
                Iid = Guid.NewGuid(),
                Container = elementDefinition,
                ParameterType = new TextParameterType(),
                Group = parameterGroup
            };

            var elementUsage = new ElementUsage()
            {
                Iid = Guid.NewGuid(),
                Container = elementDefinition,
                ElementDefinition = new ElementDefinition()
            };

            elementDefinition.Parameter.Add(parameter);
            elementDefinition.ContainedElement.Add(elementUsage);

            var elementDefinition2 = new ElementDefinition()
            {
                Iid = Guid.NewGuid()
            };

            this.iteration.Element.Add(elementDefinition);
            this.iteration.Element.Add(elementDefinition2);

            this.viewModel.MappedElements.Add(new EnterpriseArchitectBlockElement(elementDefinition.Clone(true), null, MappingDirection.FromDstToHub));
            this.viewModel.ComputeValues();

            Assert.DoesNotThrow(() => this.viewModel.SelectAllCommand.Execute(null));
            Assert.AreEqual(2, this.selectedDstMapResultForTransfer.Count);

            Assert.DoesNotThrow(() => this.viewModel.DeselectAllCommand.Execute(null));
            Assert.AreEqual(0, this.selectedDstMapResultForTransfer.Count);

            Assert.DoesNotThrow(() => this.viewModel.SelectedThings.Add(45));
            Assert.DoesNotThrow(() => this.viewModel.SelectedThings.Remove(45));

            this.viewModel.GetElementDefinitionRowViewModel(elementDefinition, out var elementDefinitionRow);
            this.viewModel.GetElementDefinitionRowViewModel(elementDefinition2, out var elementDefinitionRow2);
            Assert.DoesNotThrow(() => this.viewModel.SelectedThings.Add(elementDefinitionRow));
            Assert.DoesNotThrow(() => this.viewModel.SelectedThings.Add(elementDefinitionRow2));
            Assert.AreEqual(2, this.selectedDstMapResultForTransfer.Count);

            var parameterGroupRow = elementDefinitionRow.ContainedRows.OfType<ParameterGroupRowViewModel>().First();
            Assert.DoesNotThrow(() => this.viewModel.SelectedThings.Add(parameterGroupRow));
            Assert.AreEqual(2, this.selectedDstMapResultForTransfer.Count);

            var parameterRow = parameterGroupRow.ContainedRows.OfType<ParameterRowViewModel>().First();
            Assert.DoesNotThrow(() => this.viewModel.SelectedThings.Add(parameterRow));
            Assert.AreEqual(1, this.selectedDstMapResultForTransfer.Count);

            var elementUsageRow = elementDefinitionRow.ContainedRows.OfType<ElementUsageRowViewModel>().First();
            Assert.DoesNotThrow(() => this.viewModel.SelectedThings.Add(elementUsageRow));
            Assert.AreEqual(0, this.selectedDstMapResultForTransfer.Count);
        }
    }
}
