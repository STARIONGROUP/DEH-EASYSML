// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementContainerChildRowComparerTestFixture.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;

    using DEHEASysML.ViewModel.Comparers;
    using DEHEASysML.ViewModel.RequirementsBrowser;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class RequirementContainerChildRowComparerTestFixture
    {
        private RequirementContainerChildRowComparer comparer;
        private Mock<ISession> session;
        private IterationRequirementsViewModel iterationRequirements;
        private RequirementsSpecification requirementsSpecification;
        private RequirementsSpecificationRowViewModel requirementsSpecificationRow;
        private Iteration iteration;

        [SetUp]
        public void Setup()
        {
            this.comparer = new RequirementContainerChildRowComparer();
            this.session = new Mock<ISession>();
            this.iteration = new Iteration();

            this.iteration.IterationSetup = new IterationSetup()
            {
                Container = new EngineeringModelSetup()
            };

            this.requirementsSpecification = new RequirementsSpecification();
            this.iterationRequirements = new IterationRequirementsViewModel(this.iteration, this.session.Object);
            this.requirementsSpecificationRow = new RequirementsSpecificationRowViewModel(this.requirementsSpecification,this.session.Object, this.iterationRequirements);
            this.iteration.RequirementsSpecification.Add(this.requirementsSpecification);
        }

        [Test]
        public void VerifyComparer()
        {
            var requirement1 = new Requirement()
            {
                ShortName = "a"
            };

            var requirement2 = new Requirement()
            {
                ShortName = "b"
            };

            var requirementsGroup1 = new RequirementsGroup()
            {
                Iid = Guid.NewGuid(),
                ShortName = "a"
            };

            var requirementsGroup2 = new RequirementsGroup()
            {
                Iid = Guid.NewGuid(),
                ShortName = "b"
            };

            this.requirementsSpecification.Group.Add(requirementsGroup2);
            this.requirementsSpecification.Group.Add(requirementsGroup1);
            requirement1.Group = requirementsGroup1;
            requirement2.Group = requirementsGroup2;

            var requirements = new List<Requirement>() { requirement1, requirement2 };

            var requirementRow1 = new RequirementRowViewModel(requirement1, this.session.Object, this.requirementsSpecificationRow);
            var requirementRow2 = new RequirementRowViewModel(requirement2, this.session.Object, this.requirementsSpecificationRow);
            var requirementsGroupRow1 = new RequirementsGroupRowViewModel(requirementsGroup1, this.session.Object, this.requirementsSpecificationRow, requirements);
            var requirementsGroupRow2 = new RequirementsGroupRowViewModel(requirementsGroup2, this.session.Object, this.requirementsSpecificationRow, requirements);

            Assert.AreEqual(0,this.comparer.Compare( requirementRow1,requirementRow1));
            Assert.AreEqual(-1,this.comparer.Compare(requirementRow1, requirementRow2));
            Assert.AreEqual(1,this.comparer.Compare(requirementRow2, requirementRow1));
            Assert.AreEqual(0, this.comparer.Compare(requirementsGroupRow1, requirementsGroupRow1));
            Assert.AreEqual(-1, this.comparer.Compare(requirementsGroupRow1, requirementsGroupRow2));
            Assert.AreEqual(1, this.comparer.Compare(requirementsGroupRow2, requirementsGroupRow1));
            Assert.AreEqual(1, this.comparer.Compare(requirementsGroupRow2, requirementRow2));
            Assert.AreEqual(-1, this.comparer.Compare(requirementRow2, requirementsGroupRow2));
            _ = Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(null, requirementRow1));
            _ = Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(requirementRow1, null));
            _ = Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(requirementRow1, this.requirementsSpecificationRow));
            _ = Assert.Throws<InvalidOperationException>(() => this.comparer.Compare(this.requirementsSpecificationRow,requirementRow1));
        }
    }
}
