// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationServiceDialogViewModelTestFixture.cs" company="RHEA System S.A.">
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

    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.ViewModel.Dialogs;
    using DEHEASysML.Views.Dialogs;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class MappingConfigurationServiceDialogViewModelTestFixture
    {
        private MappingConfigurationServiceDialogViewModel viewModel;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Mock<IMappingConfigurationService> mappingConfiguration;
        private Mock<INavigationService> navigationService;
        private Mock<IStatusBarControlViewModel> statusBar;
        private List<ExternalIdentifierMap> availableExternalIdentifierMap;
        private Mock<Repository> repository;

        [SetUp]
        public void Setup()
        {
            this.availableExternalIdentifierMap = new List<ExternalIdentifierMap>();

            this.hubController = new Mock<IHubController>();

            this.hubController.Setup(x => x.AvailableExternalIdentifierMap(It.IsAny<string>()))
                .Returns(this.availableExternalIdentifierMap);

            this.repository = new Mock<Repository>();
            this.repository.Setup(x => x.ProjectGUID).Returns(Guid.NewGuid().ToString());

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.CurrentRepository).Returns(this.repository.Object);

            this.mappingConfiguration = new Mock<IMappingConfigurationService>();
            this.navigationService = new Mock<INavigationService>();
            this.statusBar = new Mock<IStatusBarControlViewModel>();

            this.viewModel = new MappingConfigurationServiceDialogViewModel(this.hubController.Object, this.dstController.Object, this.mappingConfiguration.Object,
                this.navigationService.Object, this.statusBar.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsEmpty(this.viewModel.AvailableExternalIdentifierMap);
            Assert.IsTrue(this.viewModel.AvailableExternalIdentifierMap.ChangeTrackingEnabled);
            Assert.IsNull(this.viewModel.CurrentMappingConfigurationName);
            Assert.IsNotNull(this.viewModel.CloseCommand);
            Assert.IsNotNull(this.viewModel.SaveOrLoadMappingConfiguration);
            Assert.IsNull(this.viewModel.CloseWindowBehavior);
        }

        [Test]
        public void VerifyPopulateAvailableMapping()
        {
            Assert.DoesNotThrow(() => this.viewModel.PopulateAvailableMapping());
            Assert.IsEmpty(this.viewModel.AvailableExternalIdentifierMap);
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);

            Assert.DoesNotThrow(() => this.viewModel.PopulateAvailableMapping());
            Assert.IsEmpty(this.viewModel.AvailableExternalIdentifierMap);

            this.mappingConfiguration.Setup(x => x.ExternalIdentifierMap).Returns(new ExternalIdentifierMap());

            this.hubController.Setup(x => x.OpenIteration).Returns(new Iteration());
            Assert.DoesNotThrow(() => this.viewModel.PopulateAvailableMapping());
            Assert.IsEmpty(this.viewModel.AvailableExternalIdentifierMap);

            this.availableExternalIdentifierMap.Add(new ExternalIdentifierMap()
            {
                Name = "cfg"
            });

            Assert.DoesNotThrow(() => this.viewModel.PopulateAvailableMapping());
            Assert.IsNotEmpty(this.viewModel.AvailableExternalIdentifierMap);
        }

        [Test]
        public void VerifyCloseCommand()
        {
            Assert.DoesNotThrow(() => this.viewModel.CloseCommand.Execute(null));
            var closeWindow= new Mock<ICloseWindowBehavior>();
            closeWindow.Setup(x => x.Close());
            this.viewModel.CloseWindowBehavior = closeWindow.Object;

            Assert.DoesNotThrow(() => this.viewModel.CloseCommand.Execute(null));
        }

        [Test]
        public void VerifySaveOrLoadMappingConfiguration()
        {
            this.viewModel.CurrentMappingConfigurationName = "cfg";

            var externalMap = new ExternalIdentifierMap()
            {
                Correspondence = { new IdCorrespondence() }
            };

            this.mappingConfiguration.Setup(x => x.CreateExternalIdentifierMap(It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<bool>())).Returns(new ExternalIdentifierMap());

            this.mappingConfiguration.Setup(x => x.IsTheCurrentIdentifierMapTemporary).Returns(true);
            this.mappingConfiguration.Setup(x => x.ExternalIdentifierMap).Returns(externalMap);
            this.navigationService.Setup(x => x.ShowDxDialog<SaveCurrentTemporaryIdentifierMap>()).Returns(true);

            Assert.DoesNotThrow(() => this.viewModel.SaveOrLoadMappingConfiguration.Execute(null));
            this.navigationService.Setup(x => x.ShowDxDialog<SaveCurrentTemporaryIdentifierMap>()).Returns(false);
            Assert.DoesNotThrow(() => this.viewModel.SaveOrLoadMappingConfiguration.Execute(null));

            this.availableExternalIdentifierMap.Add(new ExternalIdentifierMap()
            {
                Name = "cfg"
            });

            this.availableExternalIdentifierMap.Insert(0, new ExternalIdentifierMap());
            Assert.DoesNotThrow(() => this.viewModel.SaveOrLoadMappingConfiguration.Execute(null));
        }
    }
}
