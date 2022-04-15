// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectStatusBarControlViewModelTestFixture.cs" company="RHEA System S.A.">
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

    using DEHEASysML.ViewModel;

    using DEHPCommon.Services.NavigationService;

    using EA;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class EnterpriseArchitectStatusBarControlViewModelTestFixture
    {
        private EnterpriseArchitectStatusBarControlViewModel viewModel;
        private Mock<Repository> repository;
        private Mock<INavigationService> navigationService;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.navigationService = new Mock<INavigationService>();

            this.repository = new Mock<Repository>();
            this.repository.Setup(x => x.CreateOutputTab(It.IsAny<string>()));
            this.repository.Setup(x => x.WriteOutput(It.IsAny<string>(),It.IsAny<string>(), 0)); 
            this.viewModel = new EnterpriseArchitectStatusBarControlViewModel(this.navigationService.Object);
        }

        [Test]
        public void VerifyAppend()
        {
            Assert.DoesNotThrow(() => this.viewModel.Append(null));
            Assert.DoesNotThrow(() => this.viewModel.Append("a message"));

            this.viewModel.Initialize(this.repository.Object);

            this.repository.Verify(x => x.CreateOutputTab(It.IsAny<string>()), Times.Once);
            Assert.DoesNotThrow(() => this.viewModel.Append(null));
            Assert.DoesNotThrow(() => this.viewModel.Append("a message"));

            this.repository.Verify(x => x.WriteOutput(It.IsAny<string>(), It.IsAny<string>(), 0), Times.Once);

            this.viewModel.UserSettingCommand.Execute(null);
            this.repository.Verify(x => x.WriteOutput(It.IsAny<string>(), It.IsAny<string>(), 0), Times.Exactly(2));
        }
    }
}
