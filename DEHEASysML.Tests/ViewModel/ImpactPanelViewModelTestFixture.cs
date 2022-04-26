// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImpactPanelViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.ViewModel
{
    using DEHEASysML.DstController;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ImpactPanelViewModelTestFixture
    {
        private ImpactPanelViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<IHubNetChangePreviewViewModel> hubNetChangePreview;
        private Mock<IDstNetChangePreviewViewModel> dstNetChangePreview;
        private Mock<ITransferControlViewModel> transferControl;

        [SetUp]
        public void Setup()
        {
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromDstToHub);
            this.dstNetChangePreview = new Mock<IDstNetChangePreviewViewModel>();
            this.hubNetChangePreview = new Mock<IHubNetChangePreviewViewModel>();
            this.transferControl = new Mock<ITransferControlViewModel>();

            this.viewModel = new ImpactPanelViewModel(this.dstController.Object, this.hubNetChangePreview.Object, this.dstNetChangePreview.Object,
                this.transferControl.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.viewModel.DstNetChangePreviewViewModel);
            Assert.IsNotNull(this.viewModel.HubNetChangePreviewViewModel);
            Assert.IsNotNull(this.viewModel.ChangeMappingDirection);
            Assert.AreEqual(0, this.viewModel.ArrowDirection);
            Assert.AreEqual(0, this.viewModel.CurrentMappingDirection);
        }

        [Test]
        public void VerifyChangeDirectionCommand()
        {
            Assert.DoesNotThrow(() => this.viewModel.ChangeMappingDirection.Execute(null));
            Assert.AreEqual(0, this.viewModel.ArrowDirection);
            Assert.AreEqual(0, this.viewModel.CurrentMappingDirection);

            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromHubToDst);
            Assert.DoesNotThrow(() => this.viewModel.ChangeMappingDirection.Execute(null));
            Assert.AreEqual(180, this.viewModel.ArrowDirection);
            Assert.AreEqual(1, this.viewModel.CurrentMappingDirection);
        }
    }
}
