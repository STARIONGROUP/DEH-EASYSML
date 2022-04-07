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

    using DEHEASysML.DstController;
    using DEHEASysML.Services.Dispatcher;
    using DEHEASysML.ViewModel;

    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

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

        [SetUp]
        public void Setup()
        {
            this.repository = new Mock<Repository>();
            this.repository.Setup(x => x.AddWindow(It.IsAny<string>(), It.IsAny<string>()));
            this.repository.Setup(x => x.ShowAddinWindow(It.IsAny<string>()));
            this.repository.Setup(x => x.RemoveWindow(It.IsAny<string>()));
            this.repository.Setup(x => x.HideAddinWindow());

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.OnFileClose(this.repository.Object));
            this.dstController.Setup(x => x.OnFileNew(this.repository.Object));
            this.dstController.Setup(x => x.OnFileOpen(this.repository.Object));
            this.dstController.Setup(x => x.OnNotifyContextItemModified(this.repository.Object, It.IsAny<string>(),It.IsAny<ObjectType>()));

            this.statusBar = new Mock<IStatusBarControlViewModel>();

            this.dispatcher = new Dispatcher(this.dstController.Object, this.statusBar.Object);
        }

        [Test]
        public void VerifyShowHubPanel()
        {
            this.dispatcher.Connect(this.repository.Object);
            Assert.DoesNotThrow(() => this.dispatcher.ShowHubPanel());
            Assert.DoesNotThrow(() => this.dispatcher.ShowHubPanel());
            this.repository.Verify(x => x.AddWindow(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.repository.Verify(x => x.ShowAddinWindow(It.IsAny<string>()), Times.Exactly(2));

            this.dispatcher.StatusBar = new EnterpriseArchitectStatusBarControlViewModel(new Mock<INavigationService>().Object);
            Assert.DoesNotThrow(() => this.dispatcher.ShowHubPanel());
            Assert.DoesNotThrow(() => this.dispatcher.OnPostInitiliazed(this.repository.Object));
            this.repository.Verify(x => x.ShowAddinWindow(It.IsAny<string>()), Times.Exactly(3));
            this.repository.Verify(x => x.HideAddinWindow(), Times.Exactly(2));
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

            this.repository.Verify(x => x.RemoveWindow(It.IsAny<string>()), Times.Exactly(6));
        }

        [Test]
        public void VerifyFileEvents()
        {
            Assert.DoesNotThrow(() => this.dispatcher.OnFileNew(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.OnFileClose(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.OnFileOpen(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.OnNotifyContextItemModified(this.repository.Object, Guid.NewGuid().ToString(), ObjectType.otDiagram));

            this.dstController.Verify(x => x.OnFileNew(this.repository.Object), Times.Once);
            this.dstController.Verify(x => x.OnFileClose(this.repository.Object), Times.Once);
            this.dstController.Verify(x => x.OnFileOpen(this.repository.Object), Times.Once);
            this.dstController.Verify(x => x.OnNotifyContextItemModified(this.repository.Object, It.IsAny<string>(), It.IsAny<ObjectType>()), Times.Once);
        }
    }
}
