// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectObjectRowComparerTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.ViewModel.Comparers
{
    using System;

    using Autofac;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Services.Cache;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Comparers;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows;

    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;

    using EA;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class EnterpriseArchitectObjectRowComparerTestFixture
    {
        private EnterpriseArchitectObjectRowComparer comparer;
        private PackageRowViewModel packageRow;
        private BlockRowViewModel blockRow;
        private ValuePropertyRowViewModel valuePropertyRow;
        private PartPropertyRowViewModel partPropertyRow;
        private PortRowViewModel portRow;

        [SetUp]
        public void Setup()
        {
            var dstController = new Mock<IDstController>();
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(dstController.Object).As<IDstController>();
            AppContainer.Container = containerBuilder.Build();
            this.comparer = new EnterpriseArchitectObjectRowComparer();
            
            var package = new Mock<Package>();
            package.Setup(x => x.Name).Returns("Package");
            package.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());
            package.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection());
            this.packageRow = new PackageRowViewModel(null,package.Object);

            var block = new Mock<Element>();
            block.Setup(x => x.Name).Returns("Block");
            block.Setup(x => x.Stereotype).Returns(StereotypeKind.Block.ToString());
            this.blockRow = new BlockRowViewModel(null, block.Object, false);

            var valueProperty = new Mock<Element>();
            valueProperty.Setup(x => x.Name).Returns("ValueProperty");
            valueProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection());
            valueProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            this.valuePropertyRow = new ValuePropertyRowViewModel(null, valueProperty.Object);

            var partProperty = new Mock<Element>();
            partProperty.Setup(x => x.Name).Returns("partProperty");
            partProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.PartProperty.ToString());
            this.partPropertyRow = new PartPropertyRowViewModel(null, partProperty.Object);

            var port = new Mock<Element>();
            port.Setup(x => x.Name).Returns("port");
            port.Setup(x => x.Type).Returns(StereotypeKind.Port.ToString());
            this.portRow = new PortRowViewModel(null, port.Object);
        }

        [Test]
        public void VerifyCompare()
        {
            Assert.Throws<InvalidOperationException>(()  => this.comparer.Compare(this.partPropertyRow, null));
            Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(null, this.partPropertyRow));
            Assert.AreEqual(-1, this.comparer.Compare(this.packageRow, this.portRow));
            Assert.AreEqual(-1, this.comparer.Compare(this.valuePropertyRow, this.portRow));
            Assert.AreEqual(-1, this.comparer.Compare(this.partPropertyRow, this.portRow));
            Assert.AreEqual(-1, this.comparer.Compare(this.portRow, this.blockRow));
            Assert.AreEqual(1, this.comparer.Compare(this.portRow, this.packageRow));
            Assert.AreEqual(1, this.comparer.Compare(this.portRow, this.valuePropertyRow));
            Assert.AreEqual(1, this.comparer.Compare(this.portRow, this.partPropertyRow));
            Assert.AreEqual(1, this.comparer.Compare(this.blockRow, this.portRow));
        }
    }
}
