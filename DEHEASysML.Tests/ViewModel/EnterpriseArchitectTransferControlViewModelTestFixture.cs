// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectTransferControlViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using System;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class EnterpriseArchitectTransferControlViewModelTestFixture
    {
        private EnterpriseArchitectTransferControlViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Mock<IExchangeHistoryService> exchangeHistory;
        private ReactiveList<Thing> selectedDstMapResultForTransfer;
        private ReactiveList<Element> selectedHubMapResultForTransfer;

        [SetUp]
        public void Setup()
        {
            this.selectedHubMapResultForTransfer = new ReactiveList<Element>();
            this.selectedDstMapResultForTransfer = new ReactiveList<Thing>();
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.SelectedDstMapResultForTransfer).Returns(this.selectedDstMapResultForTransfer);
            this.dstController.Setup(x => x.SelectedHubMapResultForTransfer).Returns(this.selectedHubMapResultForTransfer);
            this.dstController.Setup(x => x.DstMapResult).Returns(new ReactiveList<IMappedElementRowViewModel>());
            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromDstToHub);

            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.statusBar.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.exchangeHistory = new Mock<IExchangeHistoryService>();
            this.exchangeHistory.Setup(x => x.ClearPending());

            this.viewModel = new EnterpriseArchitectTransferControlViewModel(this.dstController.Object, this.statusBar.Object,
                this.exchangeHistory.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsFalse(this.viewModel.CanTransfer);
            Assert.IsFalse(this.viewModel.TransferInProgress);
        }

        [Test]
        public void VerifyTransferToHub()
        {
            Assert.IsFalse(this.viewModel.TransferCommand.CanExecute(null));
            this.selectedDstMapResultForTransfer.Add(new ElementDefinition());
            Assert.IsTrue(this.viewModel.CanTransfer);
            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromHubToDst);
            this.viewModel.UpdateNumberOfThingsToTransfer();
            Assert.IsFalse(this.viewModel.CanTransfer);
            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromDstToHub);
            this.viewModel.UpdateNumberOfThingsToTransfer();
            Assert.IsTrue(this.viewModel.TransferCommand.CanExecute(null));
            Assert.DoesNotThrowAsync(() => this.viewModel.TransferCommand.ExecuteAsyncTask());
            this.dstController.Setup(x => x.TransferMappedThingsToHub()).Throws<InvalidOperationException>();
            Assert.ThrowsAsync<InvalidOperationException>(() => this.viewModel.TransferCommand.ExecuteAsyncTask());
        }

        [Test]
        public void VerifyCancelCommand()
        {
            Assert.IsFalse(this.viewModel.CancelCommand.CanExecute(null));
            this.viewModel.TransferInProgress = true;
            Assert.IsTrue(this.viewModel.CancelCommand.CanExecute(null));
            Assert.DoesNotThrowAsync(() => this.viewModel.CancelCommand.ExecuteAsyncTask());
            Assert.IsFalse(this.viewModel.CancelCommand.CanExecute(null));
        }
    }
}
