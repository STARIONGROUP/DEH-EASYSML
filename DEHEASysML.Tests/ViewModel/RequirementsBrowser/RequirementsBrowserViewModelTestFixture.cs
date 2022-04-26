// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementsBrowserViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.ViewModel.RequirementsBrowser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;

    using DEHEASysML.ViewModel.RequirementsBrowser;

    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class RequirementsBrowserViewModelTestFixture
    {
        private RequirementsBrowserViewModel viewModel;
        private Mock<IHubController> hubController;
        private Mock<IObjectBrowserTreeSelectorService> objectBrowserTreeSelectorService;
        private Mock<ISession> session;
        private Iteration iteration;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            var person = new Person();

            this.session = new Mock<ISession>();
            this.session.Setup(x => x.DataSourceUri).Returns("AnUri");
            this.session.Setup(x => x.ActivePerson).Returns(person);

            this.iteration = new Iteration();

            this.iteration.IterationSetup = new IterationSetup()
            {
                Container = new EngineeringModelSetup()
            };

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.Session).Returns(this.session.Object);
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);

            this.objectBrowserTreeSelectorService = new Mock<IObjectBrowserTreeSelectorService>();
            var thingKinds = new List<Type>() { typeof(ElementDefinition), typeof(RequirementsSpecification) };
            this.objectBrowserTreeSelectorService.Setup(x => x.ThingKinds).Returns(thingKinds.AsReadOnly());

            this.viewModel = new RequirementsBrowserViewModel(this.hubController.Object, this.objectBrowserTreeSelectorService.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNull(this.viewModel.SelectedThing);
            Assert.IsEmpty(this.viewModel.SelectedThings);
            this.viewModel.SelectedThings = new ReactiveList<object>();
            Assert.IsFalse(string.IsNullOrEmpty(this.viewModel.Caption));
            Assert.IsTrue(string.IsNullOrEmpty(this.viewModel.ToolTip));
            Assert.IsFalse(this.viewModel.IsBusy);
            Assert.IsEmpty(this.viewModel.Things);
            Assert.AreEqual(0, this.viewModel.ContextMenu.Count);
            Assert.IsNotNull(this.viewModel.CanMap);
            Assert.IsNull(this.viewModel.MapCommand);
            this.viewModel.MapCommand = ReactiveCommand.Create();
            Assert.IsNotNull(this.viewModel.MapCommand);
        }

        [Test]
        public void VerifyPopulateContextMenu()
        {
            Assert.DoesNotThrow(() => this.viewModel.PopulateContextMenu());
            this.viewModel.SelectedThing = new IterationRequirementsViewModel(this.iteration, this.session.Object);
            Assert.IsNotNull(this.viewModel.SelectedThing);
            Assert.DoesNotThrow(() => this.viewModel.PopulateContextMenu());
            Assert.AreEqual(1,this.viewModel.ContextMenu.Count);
            this.viewModel.SelectedThing = null;
            Assert.DoesNotThrow(() => this.viewModel.PopulateContextMenu());
            Assert.AreEqual(0, this.viewModel.ContextMenu.Count);
        }

        [Test]
        public void VerifyObservables()
        {
            Assert.IsEmpty(this.viewModel.Things);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            Assert.DoesNotThrow(() => this.viewModel.Reload());
            Assert.IsEmpty(this.viewModel.Things);
            this.hubController.Setup(x => x.OpenIteration).Returns((Iteration)null);
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            Assert.DoesNotThrow(() => this.viewModel.Reload());
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            Assert.DoesNotThrow(() => this.viewModel.Reload());
            Assert.AreEqual(1,this.viewModel.Things.Count);
            Assert.IsEmpty(this.viewModel.Things.First().ContainedRows);

            var requirementsDeprecated = new RequirementsSpecification()
            {
                IsDeprecated = true
            };

            var requirementsSpecifiation = new RequirementsSpecification()
            {
                Iid = Guid.NewGuid(),
                ShortName = "ARequirement",
                Owner = new DomainOfExpertise()
                {
                    Name = "AName",
                    ShortName = "aShortName"
                }
            };

            var requirementsSpecifiation2 = new RequirementsSpecification()
            {
                Iid = Guid.NewGuid(),
                ShortName = "ARequirement2",
                Owner = new DomainOfExpertise()
                {
                    Name = "AName",
                    ShortName = "aShortName"
                }
            };

            var requirementsGroup1 = new RequirementsGroup()
            {
                Iid = Guid.NewGuid(),
                ShortName = "RequiremntGroup"
            };

            var requirementsGroup2 = new RequirementsGroup()
            {
                Iid = Guid.NewGuid(),
                ShortName = "Requiremnt2Group"
            };

            var requirementsGroup3 = new RequirementsGroup()
            {
                Iid = Guid.NewGuid(),
                ShortName = "Requiremnt3Group"
            };

            requirementsGroup2.Group.Add(requirementsGroup3);

            var requirement1 = new Requirement()
            {
                ShortName = "Requirement1",
                Group = requirementsGroup1,
                Definition =
                {
                    new Definition()
                    {
                        Content = "Requirement definition in english",
                        LanguageCode = "en-EN"
                    },
                }
            };

            var requirement2 = new Requirement()
            {
                ShortName = "Requirement2",
                Group = requirementsGroup2,
                Definition =
                {
                    new Definition()
                    {
                        Content = "Requirement definition en francais",
                        LanguageCode = "fr-FR"
                    },
                }
            };

            var requirement3 = new Requirement()
            {
                ShortName = "Requirement3",
                Definition =
                {
                    new Definition()
                    {
                        Content = "Requirement definition in english",
                        LanguageCode = "en-EN"
                    },
                    new Definition()
                    {
                        Content = "Requirement definition en francais",
                        LanguageCode = "fr-FR"
                    },
                }
            };

            var requirement4 = new Requirement()
            {
                IsDeprecated = true,
                ShortName = "Requirement4",
                Definition =
                {
                    new Definition()
                    {
                        Content = "Requirement definition in english",
                        LanguageCode = "en-EN"
                    },
                    new Definition()
                    {
                        Content = "Requirement definition en francais",
                        LanguageCode = "fr-FR"
                    },
                }
            };

            var requirement5 = new Requirement()
            {
                IsDeprecated = true,
                ShortName = "Requirement5",
                Group = requirementsGroup3,
                Definition =
                {
                    new Definition()
                    {
                        Content = "Requirement definition in english",
                        LanguageCode = "en-EN"
                    },
                    new Definition()
                    {
                        Content = "Requirement definition en francais",
                        LanguageCode = "fr-FR"
                    },
                }
            };

            requirementsSpecifiation.Requirement.Add(requirement1);
            requirementsSpecifiation.Requirement.Add(requirement2);
            requirementsSpecifiation.Requirement.Add(requirement3);
            requirementsSpecifiation.Requirement.Add(requirement4);
            requirementsSpecifiation.Requirement.Add(requirement5);
            requirementsSpecifiation.Group.Add(requirementsGroup1);
            requirementsSpecifiation.Group.Add(requirementsGroup2);
            this.iteration.RequirementsSpecification.Add(requirementsSpecifiation);
            this.iteration.RequirementsSpecification.Add(requirementsDeprecated);

            Assert.DoesNotThrow(() => this.viewModel.Reload());
            Assert.AreEqual(1, this.viewModel.Things.Count);
            Assert.IsNotEmpty(this.viewModel.Things.First().ContainedRows);
            Assert.IsFalse(string.IsNullOrEmpty(this.viewModel.ToolTip));

            var iterationRequirement = this.viewModel.Things.First() as IterationRequirementsViewModel;
            var requirementSpeficationRow = iterationRequirement?.ContainedRows.First() as RequirementsSpecificationRowViewModel;
            var requirementRow = requirementSpeficationRow?.ContainedRows.OfType<RequirementRowViewModel>().First();

            Assert.IsFalse(string.IsNullOrEmpty(iterationRequirement.ShortName));
            Assert.IsFalse(string.IsNullOrEmpty(requirementSpeficationRow.ShortName));
            Assert.IsFalse(string.IsNullOrEmpty(requirementSpeficationRow.OwnerName));
            Assert.IsFalse(string.IsNullOrEmpty(requirementSpeficationRow.OwnerShortName));
            Assert.IsNotNull(requirementSpeficationRow.Owner);
            Assert.AreEqual("Requirement definition in english", requirementRow.Definition);

            iterationRequirement.Thing.RequirementsSpecification.Add(requirementsSpecifiation2);
            Assert.DoesNotThrow(() => iterationRequirement.UpdateProperties());

            iterationRequirement.Thing.RequirementsSpecification.Remove(requirementsSpecifiation2);
            Assert.DoesNotThrow(() => iterationRequirement.UpdateProperties());

            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent()));
            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true)));
            Assert.AreEqual(1, this.viewModel.Things.Count);

            this.iteration.RequirementsSpecification.Clear();
            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent()));
            Assert.AreEqual(1, this.viewModel.Things.Count);
            Assert.IsNotEmpty(this.viewModel.Things.First().ContainedRows);

            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true)));
            Assert.AreEqual(1, this.viewModel.Things.Count);
            Assert.IsEmpty(this.viewModel.Things.First().ContainedRows);
        }

        [Test]
        public void VerifyDispose()
        {
            Assert.DoesNotThrow(() => this.viewModel.Dispose());
        }
    }
}
