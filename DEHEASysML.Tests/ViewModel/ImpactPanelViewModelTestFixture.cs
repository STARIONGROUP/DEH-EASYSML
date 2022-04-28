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
    using System.Reactive.Concurrency;

    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;
    using DEHEASysML.Views.Dialogs;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class ImpactPanelViewModelTestFixture
    {
        private ImpactPanelViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<IHubNetChangePreviewViewModel> hubNetChangePreview;
        private Mock<IDstNetChangePreviewViewModel> dstNetChangePreview;
        private Mock<ITransferControlViewModel> transferControl;
        private Mock<IHubController> hubController;
        private Mock<INavigationService> navigationService;
        private Mock<IMappingConfigurationService> mappingConfiguration;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromDstToHub);
            this.dstController.Setup(x => x.LoadMapping());
            this.dstNetChangePreview = new Mock<IDstNetChangePreviewViewModel>();
            this.hubNetChangePreview = new Mock<IHubNetChangePreviewViewModel>();
            this.transferControl = new Mock<ITransferControlViewModel>();
            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.OpenIteration).Returns((Iteration)null);
            this.navigationService = new Mock<INavigationService>();
            this.navigationService.Setup(x => x.ShowDialog<MappingConfigurationServiceDialog>());
            this.mappingConfiguration = new Mock<IMappingConfigurationService>();
            this.mappingConfiguration.Setup(x => x.ExternalIdentifierMap).Returns(new ExternalIdentifierMap());

            this.viewModel = new ImpactPanelViewModel(this.dstController.Object, this.hubNetChangePreview.Object, this.dstNetChangePreview.Object,
                this.transferControl.Object, this.hubController.Object, this.navigationService.Object, this.mappingConfiguration.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.viewModel.DstNetChangePreviewViewModel);
            Assert.IsNotNull(this.viewModel.HubNetChangePreviewViewModel);
            Assert.IsNotNull(this.viewModel.ChangeMappingDirection);
            Assert.AreEqual(0, this.viewModel.ArrowDirection);
            Assert.AreEqual(0, this.viewModel.CurrentMappingDirection);
            Assert.IsNotNull(this.viewModel.OpenMappingConfigurationDialog);
            Assert.IsNotNull(this.viewModel.TransferControlViewModel);
            Assert.IsFalse(this.viewModel.OpenMappingConfigurationDialog.CanExecute(null));
            Assert.IsEmpty(this.viewModel.CurrentMappingConfigurationName);
        }

        [Test]
        public void VerifyChangeDirectionCommandAndMappingName()
        {
            Assert.DoesNotThrow(() => this.viewModel.ChangeMappingDirection.Execute(null));
            Assert.AreEqual(0, this.viewModel.ArrowDirection);
            Assert.AreEqual(0, this.viewModel.CurrentMappingDirection);

            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromHubToDst);
            Assert.DoesNotThrow(() => this.viewModel.ChangeMappingDirection.Execute(null));
            Assert.AreEqual(180, this.viewModel.ArrowDirection);
            Assert.AreEqual(1, this.viewModel.CurrentMappingDirection);

            this.mappingConfiguration.Setup(x => x.ExternalIdentifierMap).Returns(new ExternalIdentifierMap() { Name = "cfg" });
            Assert.DoesNotThrow(() => this.viewModel.ChangeMappingDirection.Execute(null));
            Assert.IsNotEmpty(this.viewModel.CurrentMappingConfigurationName);
        }

        [Test]
        public void VerifyOpenMappingConfiguration()
        {
            Assert.DoesNotThrow(() => this.viewModel.OpenMappingConfigurationDialog.Execute(null));
            Assert.DoesNotThrow(() => this.viewModel.OpenMappingConfigurationDialog.Execute(null));
            this.dstController.Verify(x => x.LoadMapping(), Times.Exactly(2));
            this.navigationService.Verify(x => x.ShowDialog<MappingConfigurationServiceDialog>(), Times.Exactly(2));
        }
    }
}
