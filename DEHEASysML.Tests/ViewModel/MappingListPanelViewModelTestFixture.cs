// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingListPanelViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;

    using EA;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;
    using Parameter = CDP4Common.EngineeringModelData.Parameter;
    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    [TestFixture]
    public class MappingListPanelViewModelTestFixture
    {
        private MappingListPanelViewModel viewModel;
        private Mock<IDstController> dstController;
        private ReactiveList<IMappedElementRowViewModel> dstMapResult;
        private ReactiveList<IMappedElementRowViewModel> hubMapResult;
        private List<Element> createdElements;
        private Mock<Repository> repository;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.dstMapResult = new ReactiveList<IMappedElementRowViewModel>();
            this.hubMapResult = new ReactiveList<IMappedElementRowViewModel>();
            this.createdElements = new List<Element>();
            this.repository = new Mock<Repository>();
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.CreatedElements).Returns(this.createdElements);
            this.dstController.Setup(x => x.HubMapResult).Returns(this.hubMapResult);
            this.dstController.Setup(x => x.DstMapResult).Returns(this.dstMapResult);
            this.dstController.Setup(x => x.CurrentRepository).Returns(this.repository.Object);

            this.viewModel = new MappingListPanelViewModel(this.dstController.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNull(this.viewModel.IsBusy);
            Assert.IsEmpty(this.viewModel.MappingRows);
        }

        [Test]
        public void VerifyElementDefinitionMapping()
        {
            var elementDefinition = new ElementDefinition()
            {
                Name = "ElementDefinition"
            };

            var parameter1 = new Parameter()
            {
                ParameterType = new TextParameterType()
                {
                    Name = "mass"
                },
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(),
                        ValueSwitch = ParameterSwitchKind.MANUAL,
                    }
                }
            };

            var possibleFiniteStateList = new PossibleFiniteStateList()
            {
                Name = "State"
            };

            possibleFiniteStateList.PossibleState.Add(new PossibleFiniteState()
            {
                Name = "SubState1"
            });

            possibleFiniteStateList.PossibleState.Add(new PossibleFiniteState()
            {
                Name = "SubState2"
            });

            var actualFiniteStateList = new ActualFiniteStateList();
            actualFiniteStateList.PossibleFiniteStateList.Add(possibleFiniteStateList);
            var actualFiniteState = new ActualFiniteState();
            actualFiniteState.PossibleState.Add(possibleFiniteStateList.PossibleState[0]);
            actualFiniteStateList.ActualState.Add(actualFiniteState);

            var parameter2 = new Parameter()
            {
                ParameterType = new SimpleQuantityKind()
                {
                    Name = "power",
                },
                Scale = new RatioScale()
                {
                    ShortName = "W",
                    Name = "watt",
                    Unit = new SimpleUnit()
                    {
                        ShortName = "W",
                        Name = "W"
                    }
                },
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(new []{"45"}),
                        ValueSwitch = ParameterSwitchKind.MANUAL,
                        ActualState = actualFiniteState
                    }
                },
                StateDependence = actualFiniteStateList
            };

            var option = new Option() { Name = "option1" };

            var parameter3 = new Parameter()
            {
                ParameterType = new SimpleQuantityKind()
                {
                    Name = "distance",
                },
                Scale = new RatioScale()
                {
                    ShortName = "m",
                    Name = "meter",
                    Unit = new SimpleUnit()
                    {
                        ShortName = "m",
                        Name = "meter"
                    }
                },
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(new []{"5"}),
                        ValueSwitch = ParameterSwitchKind.MANUAL,
                        ActualOption = option
                    }
                },
                IsOptionDependent = true
            };
            
            elementDefinition.Parameter.Add(parameter1);
            elementDefinition.Parameter.Add(parameter2);
            elementDefinition.Parameter.Add(parameter3);

            var element = new Mock<Element>();
            element.Setup(x => x.Name).Returns("Element");
            element.Setup(x => x.Stereotype).Returns(StereotypeKind.Block.ToString());
            var massProperty = new Mock<Element>();
            massProperty.Setup(x => x.Name).Returns("mass");
            massProperty.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            massProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            var massValue = new Mock<CustomProperty>();
            massValue.Setup(x => x.Name).Returns("default");
            massValue.Setup(x => x.Value).Returns("45");
            massProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection(){massValue.Object});
            var massUnitElement = new Mock<Element>();
            massUnitElement.Setup(x => x.Name).Returns("kg");
            massUnitElement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            var massUnit = new Mock<TaggedValue>();
            massUnit.Setup(x => x.Name).Returns("unit");
            massUnit.Setup(x => x.Value).Returns(massUnitElement.Object.ElementGUID);
            massProperty.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection() { massUnit.Object });
            this.repository.Setup(x => x.GetElementByGuid(massUnitElement.Object.ElementGUID)).Returns(massUnitElement.Object);

            var actionProperty = new Mock<Element>();
            actionProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            actionProperty.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            this.createdElements.Add(actionProperty.Object);

            var emptyProperty = new Mock<Element>();
            emptyProperty.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            emptyProperty.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            emptyProperty.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection());
            emptyProperty.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection());
            emptyProperty.Setup(x => x.Name).Returns("empty");

            element.Setup(x => x.EmbeddedElements).Returns(new EnterpriseArchitectCollection()
            {
                massProperty.Object,
                actionProperty.Object,
                emptyProperty.Object
            });

            this.dstMapResult.Add(new EnterpriseArchitectBlockElement(elementDefinition, element.Object, MappingDirection.FromDstToHub));
            this.hubMapResult.Add(new ElementDefinitionMappedElement(elementDefinition, element.Object, MappingDirection.FromHubToDst));

            Assert.AreEqual(0, this.viewModel.MappingRows.First().ArrowDirection);
            Assert.AreEqual(180, this.viewModel.MappingRows.Last().ArrowDirection);
            Assert.IsNotEmpty(this.viewModel.MappingRows.Last().ToolTip);
            Assert.IsNotEmpty(this.viewModel.MappingRows.Last().HubThing);
            Assert.IsNotEmpty(this.viewModel.MappingRows.Last().DstThing);
            Assert.AreEqual(2, this.viewModel.MappingRows.Count);

            this.dstMapResult.Add(new EnterpriseArchitectBlockElement(elementDefinition.Clone(true), element.Object, MappingDirection.FromDstToHub));
            this.hubMapResult.Add(new ElementDefinitionMappedElement(elementDefinition, element.Object, MappingDirection.FromHubToDst));

            Assert.AreEqual(2, this.viewModel.MappingRows.Count);
            this.dstMapResult.Clear();
            this.hubMapResult.Clear();
            Assert.IsEmpty(this.viewModel.MappingRows);
        }

        [Test]
        public void VerifyRequirementMapping()
        {
            var hubRequirement = new Requirement()
            {
                Name = "Requirement",
                ShortName = "MA-010",
                Definition =
                {
                    new Definition()
                    {
                        LanguageCode = "en",
                        Content = "A requirement definition"
                    }
                }
            };

            var dstRequirement = new Mock<Element>();
            dstRequirement.Setup(x => x.Stereotype).Returns(StereotypeKind.Requirement.ToString());
            dstRequirement.Setup(x => x.Name).Returns("dstRequirement");
            var requirementId = new Mock<TaggedValue>();
            requirementId.Setup(x => x.Name).Returns("id");
            requirementId.Setup(x => x.Value).Returns("MA-023");
            var requirementText = new Mock<TaggedValue>();
            requirementText.Setup(x => x.Name).Returns("text");
            requirementText.Setup(x => x.Notes).Returns("A requirement text");

            dstRequirement.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection()
            {
                requirementText.Object,
                requirementId.Object
            });

            this.dstMapResult.Add(new EnterpriseArchitectRequirementElement(hubRequirement, dstRequirement.Object, MappingDirection.FromDstToHub));
            this.hubMapResult.Add(new EnterpriseArchitectRequirementElement(hubRequirement, dstRequirement.Object, MappingDirection.FromHubToDst));
            Assert.AreEqual(2, this.viewModel.MappingRows.Count);

            this.dstMapResult.Add(new EnterpriseArchitectRequirementElement(hubRequirement.Clone(true), dstRequirement.Object, MappingDirection.FromDstToHub));
            this.hubMapResult.Add(new EnterpriseArchitectRequirementElement(hubRequirement, dstRequirement.Object, MappingDirection.FromHubToDst));
            Assert.AreEqual(2, this.viewModel.MappingRows.Count);

            Assert.DoesNotThrow(() => this.viewModel = new MappingListPanelViewModel(this.dstController.Object));
            Assert.AreEqual(2, this.viewModel.MappingRows.Count);
        }
    }
}
