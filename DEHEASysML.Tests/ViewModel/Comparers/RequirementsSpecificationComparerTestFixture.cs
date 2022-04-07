// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementsSpecificationComparerTestFixture.cs" company="RHEA System S.A.">
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

    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHEASysML.ViewModel.Comparers;
    using DEHEASysML.ViewModel.RequirementsBrowser;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class RequirementsSpecificationComparerTestFixture
    {
        private RequirementsSpecificationComparer comparer;
        private Mock<ISession> session;
        private IterationRequirementsViewModel iterationRequirements;
        private Iteration iteration;

        [SetUp]
        public void Setup()
        {
            this.comparer = new RequirementsSpecificationComparer();
            this.session = new Mock<ISession>();
            this.iteration = new Iteration();
            this.iterationRequirements = new IterationRequirementsViewModel(this.iteration, this.session.Object);
        }

        [Test]
        public void VerifyComparer()
        {
            var requimentSpecification1 = new RequirementsSpecification()
            {
                ShortName = "a"
            };

            var requimentSpecification2 = new RequirementsSpecification()
            {
                ShortName = "B"
            };

            var requimentSpecification3 = new RequirementsSpecification()
            {
                ShortName = "b"
            };

            var requirementSpecificationRow1 = new RequirementsSpecificationRowViewModel(requimentSpecification1, this.session.Object, this.iterationRequirements);
            var requirementSpecificationRow2 = new RequirementsSpecificationRowViewModel(requimentSpecification2, this.session.Object, this.iterationRequirements);
            var requirementSpecificationRow3 = new RequirementsSpecificationRowViewModel(requimentSpecification3, this.session.Object, this.iterationRequirements);

            Assert.AreEqual(-1, this.comparer.Compare(requirementSpecificationRow1, requirementSpecificationRow2));
            Assert.AreEqual(0, this.comparer.Compare(requirementSpecificationRow2, requirementSpecificationRow3));
            Assert.AreEqual(-1, this.comparer.Compare(requirementSpecificationRow1, requirementSpecificationRow3));
            Assert.AreEqual(1, this.comparer.Compare(requirementSpecificationRow3, requirementSpecificationRow1));
            _ = Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(null, requirementSpecificationRow3));
            _ = Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(requirementSpecificationRow3, null));

            var requirement = new Requirement();
            var requirementRow = new RequirementRowViewModel(requirement, this.session.Object, requirementSpecificationRow3);

            _ = Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(requirementRow, requirementSpecificationRow3));
            _ = Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(requirementSpecificationRow3, requirementRow));
        }
    }
}
