// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DispatcherTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.Services.Dispatcher
{
    using System;
    using System.Collections.Generic;

    using Autofac;

    using CDP4Common;

    using DEHEASysML.DstController;
    using DEHEASysML.Services.Dispatcher;
    using DEHEASysML.Services.Selection;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.Dialogs.Interfaces;
    using DEHEASysML.Views.Dialogs;

    using DEHPCommon;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views.ExchangeHistory;

    using EA;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class DispatcherTestFixture
    {
        private Dispatcher dispatcher;
        private Mock<Repository> repository;
        private Mock<IDstController> dstController;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Mock<INavigationService> navigationService;
        private Mock<IDstMappingConfigurationDialogViewModel> dstMappingDialog;
        private Mock<ISelectionService> selectionService;

        [SetUp]
        public void Setup()
        {
            this.repository = new Mock<Repository>();
            this.repository.Setup(x => x.AddTab(It.IsAny<string>(), It.IsAny<string>()));
            this.repository.Setup(x => x.ActivateTab(It.IsAny<string>()));

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.OnFileClose(this.repository.Object));
            this.dstController.Setup(x => x.OnFileNew(this.repository.Object));
            this.dstController.Setup(x => x.OnFileOpen(this.repository.Object));
            this.dstController.Setup(x => x.OnNotifyContextItemModified(this.repository.Object, It.IsAny<string>(),It.IsAny<ObjectType>()));
            this.dstController.Setup(x => x.OnContextItemChanged(this.repository.Object, It.IsAny<string>(),It.IsAny<ObjectType>()));
            this.dstController.Setup(x => x.OnPackageEvent(It.IsAny<Repository>(), It.IsAny<ChangeKind>(), It.IsAny<int>()));
            this.dstController.Setup(x => x.OnElementEvent(It.IsAny<Repository>(), It.IsAny<ChangeKind>(), It.IsAny<int>()));

            this.dstController.Setup(x => x.RetrieveAllParentsIdPackage(It.IsAny<IEnumerable<Element>>()))
                .Returns(new List<int>());

            this.statusBar = new Mock<IStatusBarControlViewModel>();

            this.dstMappingDialog = new Mock<IDstMappingConfigurationDialogViewModel>();

            this.dstMappingDialog.Setup(x =>
                x.Initialize(It.IsAny<IEnumerable<Element>>(), It.IsAny<IEnumerable<int>>()));

            this.navigationService = new Mock<INavigationService>();

            this.navigationService.Setup(x =>
                x.ShowDialog<DstMappingConfigurationDialog, IDstMappingConfigurationDialogViewModel>(this.dstMappingDialog.Object));

            this.navigationService.Setup(x => x.ShowDialog<ExchangeHistory>());

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.dstMappingDialog.Object).As<IDstMappingConfigurationDialogViewModel>();
            AppContainer.Container = containerBuilder.Build();

            this.selectionService = new Mock<ISelectionService>();

            this.dispatcher = new Dispatcher(this.dstController.Object, this.statusBar.Object, this.navigationService.Object, this.selectionService.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsFalse(this.dispatcher.CanMap);
        }

        [Test]
        public void VerifyShowPanels()
        {
            this.dispatcher.Connect(this.repository.Object);
            this.repository.Setup(x => x.IsTabOpen(Dispatcher.HubPanelName)).Returns(0);
            this.repository.Setup(x => x.IsTabOpen(Dispatcher.ImpactPanelName)).Returns(0);
            this.repository.Setup(x => x.IsTabOpen(Dispatcher.MappingListPanelName)).Returns(0);
            Assert.DoesNotThrow(() => this.dispatcher.ShowHubPanel());
            Assert.DoesNotThrow(() => this.dispatcher.ShowImpactPanel());
            Assert.DoesNotThrow(() => this.dispatcher.ShowMappingListPanel());

            this.repository.Setup(x => x.IsTabOpen(Dispatcher.HubPanelName)).Returns(1);
            this.repository.Setup(x => x.IsTabOpen(Dispatcher.ImpactPanelName)).Returns(1);
            this.repository.Setup(x => x.IsTabOpen(Dispatcher.MappingListPanelName)).Returns(1);
            Assert.DoesNotThrow(() => this.dispatcher.ShowHubPanel());
            Assert.DoesNotThrow(() => this.dispatcher.ShowImpactPanel());
            Assert.DoesNotThrow(() => this.dispatcher.ShowMappingListPanel());

            this.dispatcher.StatusBar = new EnterpriseArchitectStatusBarControlViewModel(new Mock<INavigationService>().Object);
            this.repository.Setup(x => x.IsTabOpen(Dispatcher.HubPanelName)).Returns(2);
            this.repository.Setup(x => x.IsTabOpen(Dispatcher.ImpactPanelName)).Returns(2);
            this.repository.Setup(x => x.IsTabOpen(Dispatcher.MappingListPanelName)).Returns(2);
            Assert.DoesNotThrow(() => this.dispatcher.ShowHubPanel());
            Assert.DoesNotThrow(() => this.dispatcher.ShowImpactPanel());
            Assert.DoesNotThrow(() => this.dispatcher.ShowMappingListPanel());

            Assert.DoesNotThrow(() => this.dispatcher.OnPostInitiliazed(this.repository.Object));
            this.repository.Verify(x => x.AddTab(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
            this.repository.Verify(x => x.ActivateTab(It.IsAny<string>()), Times.Exactly(3));
        }

        [Test]
        public void VerifyConnectAndDisconnect()
        {
            Assert.DoesNotThrow(() => this.dispatcher.Connect(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.Disconnect());

            var enterpriseArchitectStatusBarControlViewModel = new EnterpriseArchitectStatusBarControlViewModel(new Mock<INavigationService>().Object);
            Assert.DoesNotThrow(() => this.dispatcher.Disconnect());

            enterpriseArchitectStatusBarControlViewModel.Initialize(this.repository.Object);
            this.dispatcher.StatusBar = enterpriseArchitectStatusBarControlViewModel;
            Assert.DoesNotThrow(() => this.dispatcher.Disconnect());

            this.repository.Setup(x => x.IsTabOpen("DEHP")).Returns(0);
            Assert.DoesNotThrow(() => this.dispatcher.Disconnect());

            this.repository.Setup(x => x.IsTabOpen("DEHP")).Returns(2);
            Assert.DoesNotThrow(() => this.dispatcher.Disconnect());
        }

        [Test]
        public void VerifyFileEvents()
        {
            Assert.DoesNotThrow(() => this.dispatcher.OnFileNew(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.OnFileClose(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.OnFileOpen(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.OnNotifyContextItemModified(this.repository.Object, Guid.NewGuid().ToString(), ObjectType.otDiagram));
            Assert.DoesNotThrow(() => this.dispatcher.OnContextItemChanged(this.repository.Object, Guid.NewGuid().ToString(), ObjectType.otDiagram));
            Assert.DoesNotThrow(() => this.dispatcher.OnNewElement(this.repository.Object, 45));
            Assert.DoesNotThrow(() => this.dispatcher.OnDeleteElement(this.repository.Object, 45));
            Assert.DoesNotThrow(() => this.dispatcher.OnNewPackage(this.repository.Object, 45));
            Assert.DoesNotThrow(() => this.dispatcher.OnDeletePackage(this.repository.Object, 45));

            this.dstController.Verify(x => x.OnFileNew(this.repository.Object), Times.Once);
            this.dstController.Verify(x => x.OnFileClose(this.repository.Object), Times.Once);
            this.dstController.Verify(x => x.OnFileOpen(this.repository.Object), Times.Once);
            this.dstController.Verify(x => x.OnNotifyContextItemModified(this.repository.Object, It.IsAny<string>(), It.IsAny<ObjectType>()), Times.Once);
        }

        [Test]
        public void VerifyMapCommands()
        {
            this.selectionService.Setup(x => x.GetSelectedElements(this.repository.Object, It.IsAny<bool>())).Returns(new List<Element>());
            Assert.DoesNotThrow(() => this.dispatcher.MapSelectedElementsCommand(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.MapSelectedPackageCommand(this.repository.Object));

            var element = new Mock<Element>();
            this.selectionService.Setup(x => x.GetSelectedElements(this.repository.Object,It.IsAny<bool>())).Returns(new List<Element>(){element.Object});

            Assert.DoesNotThrow(() => this.dispatcher.MapSelectedElementsCommand(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.MapSelectedPackageCommand(this.repository.Object));
        }

        [Test]
        public void VerifyOpenTransferHistory()
        {
            this.dispatcher.OpenTransferHistory();
            this.navigationService.Verify(x => x.ShowDialog<ExchangeHistory>(), Times.Once);
        }
    }
}
