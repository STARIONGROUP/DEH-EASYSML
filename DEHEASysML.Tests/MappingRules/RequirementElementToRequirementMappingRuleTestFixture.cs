// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementElementToRequirementMappingRuleTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.MappingRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.MappingRules;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.Utils.Stereotypes;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;

    using EA;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class RequirementElementToRequirementMappingRuleTestFixture
    {
        private RequirementElementToRequirementMappingRule rule;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Uri uri;
        private Assembler assembler;
        private DomainOfExpertise domain;
        private Mock<ISession> session;
        private Iteration iteration;
        private ModelReferenceDataLibrary referenceDataLibrary;
        private Mock<Repository> repository;
        private List<EnterpriseArchitectRequirementElement> elements;
        private Mock<IMappingConfigurationService> mappingConfiguration;

        [SetUp]
        public void Setup()
        {
            this.uri = new Uri("https://uri.test");
            this.assembler = new Assembler(this.uri);
            this.domain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri);
            this.session = new Mock<ISession>();
            this.session.Setup(x => x.Assembler).Returns(this.assembler);
            this.session.Setup(x => x.DataSourceUri).Returns(this.uri.AbsoluteUri);

            this.referenceDataLibrary = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri);

            var engineering = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                RequiredRdl = { this.referenceDataLibrary },
                Container = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri)
                {
                    Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, this.uri)
                }
            };

            this.iteration = new Iteration(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Container = engineering,
                IterationSetup = new IterationSetup()
                {
                    Container = engineering
                }
            };

            var category = new Category()
            {
                Iid = Guid.NewGuid(),
                Name = "Requirement",
                ShortName = "refineRelationship",
                PermissibleClass = new List<ClassKind>() { ClassKind.RequirementsGroup, ClassKind.RequirementsSpecification, ClassKind.Requirement }
            };

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(this.domain);
            this.hubController.Setup(x => x.Session).Returns(this.session.Object);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            this.hubController.Setup(x => x.GetSiteDirectory()).Returns(new SiteDirectory());
            this.hubController.Setup(x => x.GetDehpOrModelReferenceDataLibrary()).Returns(this.referenceDataLibrary);

            this.hubController.Setup(x => x.TryGetThingBy(It.IsAny<Func<Category, bool>>(), ClassKind.Category, out category))
                .Returns(true);

            this.repository = new Mock<Repository>();

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.CurrentRepository).Returns(this.repository.Object);

            this.SetupElements();

            this.mappingConfiguration = new Mock<IMappingConfigurationService>();

            this.mappingConfiguration.Setup(x =>
                x.AddToExternalIdentifierMap(It.IsAny<Guid>(), It.IsAny<string>(), MappingDirection.FromDstToHub));

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.dstController.Object).As<IDstController>();
            containerBuilder.RegisterInstance(this.mappingConfiguration.Object).As<IMappingConfigurationService>();
            AppContainer.Container = containerBuilder.Build();

            this.rule = new RequirementElementToRequirementMappingRule();
        }

        private void SetupElements()
        {
            this.elements = new List<EnterpriseArchitectRequirementElement>();
            var modelPackage = this.CreatePackage(1,0, "Model Package");
            var subModelPackage = this.CreatePackage(2, modelPackage.Object.PackageID, "SubModel Package");
            var subSubModelPackage = this.CreatePackage(3, subModelPackage.Object.PackageID, "SubSubModel Package");
            var subSubSubModelPackage = this.CreatePackage(4, subSubModelPackage.Object.PackageID, "SubSubSubModel Package");
            var subSubSubSubModelPackage = this.CreatePackage(5, subSubSubModelPackage.Object.PackageID, "SubSubSubSubModel Package");

            var firstRequirement = this.CreateRequirement(subModelPackage.Object.PackageID,"First requirement", "M001", "A simple text",6);
            firstRequirement.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());
            firstRequirement.Setup(x => x.GetStereotypeList()).Returns("requirement,aStereotype");

            var secondRequirement = this.CreateRequirement(subSubModelPackage.Object.PackageID, "Second requirement", "M002", "A simple text v2",7);
            secondRequirement.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());
            secondRequirement.Setup(x => x.GetStereotypeList()).Returns("requirement");

            var thirdRequirement = this.CreateRequirement(subSubSubModelPackage.Object.PackageID, "Third requirement", "M003", "A simple text v3",8);
            thirdRequirement.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());
            thirdRequirement.Setup(x => x.GetStereotypeList()).Returns("requirement");

            var forthRequirement = this.CreateRequirement(subSubSubSubModelPackage.Object.PackageID, "Forth requirement", "M004", "A simple text v4",9);
            var connector = new Mock<Connector>();
            connector.Setup(x => x.Stereotype).Returns(StereotypeKind.DeriveReqt.ToString());
            forthRequirement.Setup(x => x.GetStereotypeList()).Returns("requirement");

            forthRequirement.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection()
            {
                connector.Object
            });

            this.elements.Add(new EnterpriseArchitectRequirementElement(null, firstRequirement.Object, MappingDirection.FromDstToHub));
            this.elements.Add(new EnterpriseArchitectRequirementElement(null, secondRequirement.Object, MappingDirection.FromDstToHub));
            this.elements.Add(new EnterpriseArchitectRequirementElement(null, thirdRequirement.Object, MappingDirection.FromDstToHub));
            this.elements.Add(new EnterpriseArchitectRequirementElement(null, forthRequirement.Object, MappingDirection.FromDstToHub));

            this.dstController.Setup(x => x.ResolveConnector(It.IsAny<Connector>())).Returns((thirdRequirement.Object, forthRequirement.Object));
        }

        public Mock<Package> CreatePackage(int id, int parentId, string name)
        {
            var package = new Mock<Package>();
            package.Setup(x => x.PackageID).Returns(id);
            package.Setup(x => x.ParentID).Returns(parentId);
            package.Setup(x => x.Name).Returns(name);
            this.repository.Setup(x => x.GetPackageByID(package.Object.PackageID)).Returns(package.Object);
            return package;
        }

        public Mock<Element> CreateRequirement(int packageId, string name, string id, string description, int elementId)
        {
            var requirement = new Mock<Element>();
            requirement.Setup(x => x.PackageID).Returns(packageId);
            requirement.Setup(x => x.Name).Returns(name);
            requirement.Setup(x => x.ElementID).Returns(elementId);

            var idValue = new Mock<TaggedValue>();
            idValue.Setup(x => x.Name).Returns("Id");
            idValue.Setup(x => x.Value).Returns(id);

            var textValue = new Mock<TaggedValue>();
            textValue.Setup(x => x.Name).Returns("Text");
            textValue.Setup(x => x.Value).Returns(description);

            requirement.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection()
            {
                idValue.Object,
                textValue.Object
            });

            return requirement;
        }

        [Test]
        public void VerifyRowProperties()
        {
            Assert.IsFalse(this.elements.First().ShouldCreateNewTargetElement);
            Assert.AreEqual(MappingDirection.FromDstToHub,this.elements.First().MappingDirection);
            this.elements.First().ShouldCreateNewTargetElement = true;
            Assert.IsTrue(this.elements.First().ShouldCreateNewTargetElement);
        }

        [Test]
        public void VerifyMapping()
        {
            Assert.DoesNotThrow(() => this.rule.Transform((true,this.elements)));
            Assert.DoesNotThrow(() => this.rule.Transform((false,this.elements)));
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            Assert.DoesNotThrow(() => this.rule.Transform((true, this.elements)));
            Assert.DoesNotThrow(() => this.rule.Transform((false, this.elements)));
            var mappedElements = this.rule.Transform((true,this.elements));
           
            var specifications = mappedElements.Select(x => x.HubElement.Container)
                .OfType<RequirementsSpecification>().Distinct().ToList();

            Assert.AreEqual(1, specifications.Count);
            var requirements = specifications.SelectMany(x => x.Requirement).ToList();
            Assert.AreEqual(4,requirements.Count);
        }
    }
}
