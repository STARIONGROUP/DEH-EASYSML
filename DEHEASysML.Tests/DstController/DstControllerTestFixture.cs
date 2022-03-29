// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstControllerTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.DstController
{
    using System;
    using System.IO;

    using DEHEASysML.DstController;

    using DEHPCommon.HubController.Interfaces;

    using EA;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class DstControllerTestFixture
    {
        private DstController dstController;
        private Mock<IHubController> hubController;
        private Mock<Repository> repository;

        [SetUp]
        public void Setup()
        {
            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.Close());

            this.repository = new Mock<Repository>();

            this.dstController = new DstController(this.hubController.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNull(this.dstController.CurrentRepository);
        }

        [Test]
        public void VerifyConnectAndDisconnect()
        {
            this.dstController.Connect(this.repository.Object);
            Assert.IsNotNull(this.dstController.CurrentRepository);
            this.dstController.Disconnect();
            Assert.IsNull(this.dstController.CurrentRepository);
            this.hubController.Verify(x => x.Close(), Times.Once);
        }

        [Test]
        public void VerifyEventListener()
        {
            Assert.DoesNotThrow(() => this.dstController.OnFileNew(this.repository.Object));
            Assert.IsNotNull(this.dstController.CurrentRepository);
            
            Assert.DoesNotThrow(() => this.dstController.OnFileClose(this.repository.Object));
            Assert.IsNotNull(this.dstController.CurrentRepository);
            
            Assert.DoesNotThrow(() => this.dstController.OnFileOpen(this.repository.Object));
            Assert.IsNotNull(this.dstController.CurrentRepository);
            
            Assert.DoesNotThrow(() => this.dstController.OnFileClose(this.repository.Object));
            Assert.IsNotNull(this.dstController.CurrentRepository);

            Assert.DoesNotThrow(() => this.dstController.OnNotifyContextItemModified(this.repository.Object, Guid.NewGuid().ToString(), ObjectType.otDiagram));
            Assert.IsNotNull(this.dstController.CurrentRepository);
        }

        [Test]
        public void VerifyGetElementsFromModel()
        {
            var realRepository = new RepositoryClass();
            realRepository.OpenFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "TestProject.eapx"));

            var model = realRepository.Models.GetAt(0) as Package;
            var requirements = this.dstController.GetAllRequirements(model);
            var valueTypes = this.dstController.GetAllValueTypes(model);
            var blocks = this.dstController.GetAllBlocks(model);

            Assert.AreEqual(79, requirements.Count);
            Assert.AreEqual(8, valueTypes.Count);
            Assert.AreEqual(93, blocks.Count);
        }
    }
}
