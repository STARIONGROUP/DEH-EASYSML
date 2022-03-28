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
    using DEHEASysML.Services.Dispatcher;
    using DEHEASysML.ViewModel;

    using DEHPCommon.HubController.Interfaces;
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
        private Mock<IHubController> hubController;
        private Mock<IStatusBarControlViewModel> statusBar;

        [SetUp]
        public void Setup()
        {
            this.repository = new Mock<Repository>();
            this.repository.Setup(x => x.AddWindow(It.IsAny<string>(), It.IsAny<string>()));
            this.repository.Setup(x => x.ShowAddinWindow(It.IsAny<string>()));
            this.repository.Setup(x => x.RemoveWindow(It.IsAny<string>()));

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.Close());

            this.statusBar = new Mock<IStatusBarControlViewModel>();

            this.dispatcher = new Dispatcher(this.hubController.Object, this.statusBar.Object);
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
            this.repository.Verify(x => x.ShowAddinWindow(It.IsAny<string>()), Times.Exactly(3));
        }

        [Test]
        public void VerifyConnectAndDisconnect()
        {
            Assert.DoesNotThrow(() => this.dispatcher.Connect(this.repository.Object));
            Assert.DoesNotThrow(() => this.dispatcher.Disconnect());

            var enterpriseArchitectStatusBarControlViewModel = new EnterpriseArchitectStatusBarControlViewModel(new Mock<INavigationService>().Object);
            enterpriseArchitectStatusBarControlViewModel.Initialize(this.repository.Object);
            this.dispatcher.StatusBar = enterpriseArchitectStatusBarControlViewModel; 
            Assert.DoesNotThrow(() => this.dispatcher.Disconnect());

            this.repository.Verify(x => x.RemoveWindow(It.IsAny<string>()), Times.Exactly(3));
            this.hubController.Verify(x => x.Close(), Times.Exactly(2));
        }
    }
}
