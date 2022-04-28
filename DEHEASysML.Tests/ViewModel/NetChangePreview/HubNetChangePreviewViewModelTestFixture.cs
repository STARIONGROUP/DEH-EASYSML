// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubNetChangePreviewViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using CDP4Common.EngineeringModelData;

    using CDP4Dal;
    using CDP4Dal.Events;

    using DEHEASysML.DstController;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.NetChangePreview;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class HubNetChangePreviewViewModelTestFixture
    {
        private HubNetChangePreviewViewModel viewModel;
        private Mock<IHubObjectNetChangePreviewViewModel> objectNetChange;
        private Mock<IHubRequirementsNetChangePreviewViewModel> requirementsNetChange;
        private Mock<IDstController> dstController;
        private ReactiveList<IMappedElementRowViewModel> dstMapResult;
        private ReactiveList<IMappedElementRowViewModel> requirementsMappedElements;
        private ReactiveList<IMappedElementRowViewModel> objectMappedElements;

        [SetUp]
        public void Setup()
        {
            this.dstMapResult = new ReactiveList<IMappedElementRowViewModel>();
            this.requirementsMappedElements = new ReactiveList<IMappedElementRowViewModel>();
            this.objectMappedElements = new ReactiveList<IMappedElementRowViewModel>();

            this.objectNetChange = new Mock<IHubObjectNetChangePreviewViewModel>();
            this.objectNetChange.Setup(x => x.MappedElements).Returns(this.objectMappedElements);
            this.objectNetChange.Setup(x => x.ComputeValues());

            this.requirementsNetChange = new Mock<IHubRequirementsNetChangePreviewViewModel>();
            this.requirementsNetChange.Setup(x => x.MappedElements).Returns(this.requirementsMappedElements);
            this.requirementsNetChange.Setup(x => x.ComputeValues());

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.DstMapResult).Returns(this.dstMapResult);
            this.dstController.Setup(x => x.CanMap).Returns(false);

            this.viewModel = new HubNetChangePreviewViewModel(this.objectNetChange.Object, this.requirementsNetChange.Object,
                this.dstController.Object);
        }

        [TearDown]
        public void TearDown()
        {
            CDPMessageBus.Current.ClearSubscriptions();
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.viewModel.RequirementsNetChangePreview);
            Assert.IsNotNull(this.viewModel.ObjectNetChangePreview);
        }

        [Test]
        public void VerifyMapResultChangedObservable()
        {
            this.dstMapResult.Add(new EnterpriseArchitectBlockElement(null, null, MappingDirection.FromDstToHub));
            Assert.IsNotEmpty(this.objectMappedElements);
            Assert.IsEmpty(this.requirementsMappedElements);
            this.dstMapResult.Add(new EnterpriseArchitectRequirementElement(null, null, MappingDirection.FromDstToHub));
            Assert.IsNotEmpty(this.objectMappedElements);
            Assert.IsEmpty(this.requirementsMappedElements);

            this.dstMapResult.Clear();
            this.dstMapResult.Add(new EnterpriseArchitectRequirementElement(null, null, MappingDirection.FromDstToHub));
            Assert.IsEmpty(this.objectMappedElements);
            Assert.IsNotEmpty(this.requirementsMappedElements);
        }
    }
}
