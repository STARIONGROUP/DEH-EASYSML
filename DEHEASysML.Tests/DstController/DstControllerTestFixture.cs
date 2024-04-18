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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Services.Cache;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Tests.Utils.Stereotypes;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;

    using EA;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    using Parameter = CDP4Common.EngineeringModelData.Parameter;
    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    [TestFixture]
    public class DstControllerTestFixture
    {
        private DstController dstController;
        private Mock<IHubController> hubController;
        private Mock<Repository> repository;
        private Mock<Package> package;
        private Mock<IMappingEngine> mappingEngine;
        private Mock<IStatusBarControlViewModel> statusBarControlViewModel;
        private Mock<IExchangeHistoryService> exchangeService;
        private Mock<INavigationService> navigationService;
        private Mock<IMappingConfigurationService> mappingConfiguration;
        private Iteration iteration;
        private Mock<Element> blockElement;
        private Mock<ICacheService> cacheService;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            var uri = new Uri("http://t.e");
            var assembler = new Assembler(uri);

            this.iteration =
                new Iteration(Guid.NewGuid(), assembler.Cache, uri)
                {
                    Container = new EngineeringModel(Guid.NewGuid(), assembler.Cache, uri)
                    {
                        EngineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), assembler.Cache, uri)
                        {
                            RequiredRdl = { new ModelReferenceDataLibrary(Guid.NewGuid(), assembler.Cache, uri) },
                            Container = new SiteReferenceDataLibrary(Guid.NewGuid(), assembler.Cache, uri)
                            {
                                Container = new SiteDirectory(Guid.NewGuid(), assembler.Cache, uri)
                            }
                        }
                    }
                };

            assembler.Cache.TryAdd(new CacheKey(this.iteration.Iid, null), new Lazy<Thing>(() => this.iteration));

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.Close());
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(new DomainOfExpertise());
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            this.hubController.Setup(x => x.Write(It.IsAny<ThingTransaction>())).Returns(System.Threading.Tasks.Task.CompletedTask);
            this.hubController.Setup(x => x.RegisterNewLogEntryToTransaction(It.IsAny<string>(), It.IsAny<ThingTransaction>()));

            this.repository = new Mock<Repository>();
            this.repository.Setup(x => x.EnableUIUpdates);
            this.package = new Mock<Package>();
            this.package.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection());
            this.package.Setup(x => x.PackageID).Returns(1);
            this.package.Setup(x => x.ParentID).Returns(0);

            var requirementPackage = new Mock<Package>();
            var blocPackage = new Mock<Package>();
            var valueTypePackage = new Mock<Package>();
            blocPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection() { valueTypePackage.Object });
            requirementPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());

            var valueTypeElement = new Mock<Element>();
            valueTypeElement.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueType.ToString());
            valueTypeElement.Setup(x => x.HasStereotype(StereotypeKind.ValueType.ToString())).Returns(true);
            valueTypePackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection() {valueTypeElement.Object });
            valueTypePackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());

            this.blockElement = new Mock<Element>();
            this.blockElement.Setup(x => x.HasStereotype(StereotypeKind.Block.ToString().ToLower())).Returns(true);
            this.blockElement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString);
            blocPackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection() { this.blockElement.Object });

            var requirement = new Mock<Element>();
            requirement.Setup(x => x.HasStereotype(StereotypeKind.Requirement.ToString().ToLower())).Returns(true);
            requirementPackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection() { requirement.Object });
            requirement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString);
            this.package.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection() { requirementPackage.Object, blocPackage.Object });

            this.mappingEngine = new Mock<IMappingEngine>();
            this.statusBarControlViewModel = new Mock<IStatusBarControlViewModel>();

            this.exchangeService = new Mock<IExchangeHistoryService>();
            this.navigationService = new Mock<INavigationService>();

            this.statusBarControlViewModel.Setup(x => 
                x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.mappingConfiguration = new Mock<IMappingConfigurationService>();
            this.cacheService = new Mock<ICacheService>();

            this.dstController = new DstController(this.hubController.Object, this.mappingEngine.Object, this.statusBarControlViewModel.Object,
                this.exchangeService.Object, this.navigationService.Object, this.mappingConfiguration.Object, this.cacheService.Object);
        }

        public Mock<Package> CreatePackage(int id, int parentId)
        {
            var createdPackage = new Mock<Package>();
            createdPackage.Setup(x => x.PackageID).Returns(id);
            createdPackage.Setup(x => x.ParentID).Returns(parentId);
            this.repository.Setup(x => x.GetPackageByID(createdPackage.Object.PackageID)).Returns(createdPackage.Object);
            return createdPackage;
        }

        [TearDown]
        public void TearDown()
        {
            CDPMessageBus.Current.ClearSubscriptions();
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNull(this.dstController.CurrentRepository);
            Assert.IsFalse(this.dstController.CanMap);
            Assert.AreEqual(MappingDirection.FromDstToHub, this.dstController.MappingDirection);
            this.dstController.MappingDirection = MappingDirection.FromHubToDst;
            Assert.AreEqual(MappingDirection.FromHubToDst, this.dstController.MappingDirection);
            Assert.IsEmpty(this.dstController.SelectedGroupsForTransfer);
            Assert.IsEmpty(this.dstController.UpdatedCollections);
            Assert.IsEmpty(this.dstController.UpdatedValuePropretyValues);
            Assert.IsEmpty(this.dstController.UpdatedRequirementValues);
            Assert.IsNull(this.dstController.IsBusy);
            Assert.IsFalse(this.dstController.IsFileOpen);
            Assert.NotNull(this.dstController.UpdatePropertyTypes);
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
        public void VerifyRetrievePort()
        {
            this.dstController.CurrentRepository = this.repository.Object;

            var port = new Mock<Element>();
            port.Setup(x => x.PropertyType).Returns(322);

            var propertyType = new Mock<Element>();
            propertyType.Setup(x => x.ElementID).Returns(322);

            this.cacheService.Setup(x => x.GetConnectorsOfElement(propertyType.Object.ElementID)).Returns([]);
            this.cacheService.Setup(x => x.GetElementById(port.Object.PropertyType)).Returns(propertyType.Object);
            var (elementPort, interfacePort) = this.dstController.ResolvePort(port.Object);
            Assert.IsNull(elementPort);
            Assert.AreEqual(propertyType.Object, interfacePort);

            var connector = new Mock<Connector>();
            connector.Setup(x => x.ClientID).Returns(52);
            connector.Setup(x => x.SupplierID).Returns(152);
            connector.Setup(x => x.Type).Returns(StereotypeKind.Usage.ToString());

            var interfaceBlock = new Mock<Element>();
            var portblock = new Mock<Element>();

            this.cacheService.Setup(x => x.GetElementById(connector.Object.ClientID)).Returns(portblock.Object);
            this.cacheService.Setup(x => x.GetElementById(connector.Object.SupplierID)).Returns(interfaceBlock.Object);
            this.cacheService.Setup(x => x.GetConnectorsOfElement(propertyType.Object.ElementID)).Returns([connector.Object]);

            (elementPort, interfacePort) = this.dstController.ResolvePort(port.Object);
            Assert.AreEqual(elementPort, portblock.Object);
            Assert.AreEqual(interfacePort, interfaceBlock.Object);
        }

        [Test]
        public void VerifyRetrieveAllParentsIdPackage()
        {
            this.dstController.CurrentRepository = this.repository.Object;

            var element = new Mock<Element>();
            element.Setup(x => x.PackageID).Returns(5);
            this.CreatePackage(6, 2);
            this.CreatePackage(5, 4);
            this.CreatePackage(4, 3);
            this.CreatePackage(3, 1);
            this.CreatePackage(1, 0);

            Assert.DoesNotThrow(() => this.dstController.RetrieveAllParentsIdPackage(new List<Element>()));

            Assert.DoesNotThrow(() => this.dstController.RetrieveAllParentsIdPackage(new List<Element>(){element.Object}));
            var packagesId = this.dstController.RetrieveAllParentsIdPackage(new List<Element>() { element.Object }).ToList();
            Assert.AreEqual(4, packagesId.Count);
            Assert.IsFalse(packagesId.Contains(0));
            Assert.IsFalse(packagesId.Contains(6));
        }

        [Test]
        public void VerifyMapAndPremap()
        {
            this.dstController.CurrentRepository = this.repository.Object;
            Assert.IsEmpty(this.dstController.DstMapResult);

            var elementsToMap = new List<IMappedElementRowViewModel>
            {
                new EnterpriseArchitectBlockElement(null, null, MappingDirection.FromDstToHub),
                new EnterpriseArchitectBlockElement(null, null, MappingDirection.FromDstToHub),
                new ElementDefinitionMappedElement(null, null, MappingDirection.FromHubToDst),
                new RequirementMappedElement(null, null, MappingDirection.FromHubToDst)
            };

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectBlockElement>)>()))
                .Returns(null);

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectRequirementElement>)>()))
                .Returns(null);

            Assert.DoesNotThrow(() => this.dstController.PreMap(elementsToMap, MappingDirection.FromDstToHub));
            Assert.IsEmpty(this.dstController.DstMapResult);
            Assert.DoesNotThrow(() => this.dstController.Map(elementsToMap, MappingDirection.FromDstToHub));
            Assert.IsEmpty(this.dstController.DstMapResult);

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectBlockElement>)>()))
                .Returns(new List<MappedElementDefinitionRowViewModel>());

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectRequirementElement>)>()))
                .Returns(new List<MappedRequirementRowViewModel>());

            Assert.DoesNotThrow(() => this.dstController.PreMap(elementsToMap, MappingDirection.FromDstToHub));
            Assert.IsEmpty(this.dstController.DstMapResult);
            Assert.DoesNotThrow(() => this.dstController.Map(elementsToMap, MappingDirection.FromDstToHub));
            Assert.IsEmpty(this.dstController.DstMapResult);

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectBlockElement>)>()))
                .Returns(new List<MappedElementDefinitionRowViewModel>()
                {
                    new MappedElementDefinitionRowViewModel(null, null, MappingDirection.FromDstToHub)
                });

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<EnterpriseArchitectRequirementElement>)>()))
                .Returns(new List<MappedRequirementRowViewModel>()
                {
                    new MappedRequirementRowViewModel(null, null, MappingDirection.FromDstToHub)
                });

            Assert.DoesNotThrow(() => this.dstController.PreMap(elementsToMap, MappingDirection.FromDstToHub));
            Assert.IsEmpty(this.dstController.DstMapResult);
            Assert.DoesNotThrow(() => this.dstController.Map(elementsToMap, MappingDirection.FromDstToHub));
            Assert.AreEqual(2,this.dstController.DstMapResult.Count);

            this.statusBarControlViewModel.Verify(x => 
                    x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()), Times.Exactly(8));

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<ElementDefinitionMappedElement>)>()))
                .Returns(new List<MappedElementDefinitionRowViewModel>()
                {
                    new MappedElementDefinitionRowViewModel(null, null, MappingDirection.FromHubToDst)
                });

            this.mappingEngine.Setup(x => x.Map(It.IsAny<(bool, List<RequirementMappedElement>)>()))
                .Returns(new List<MappedRequirementRowViewModel>()
                {
                    new MappedRequirementRowViewModel(null, null, MappingDirection.FromHubToDst)
                });

            Assert.DoesNotThrow(() => this.dstController.Map(elementsToMap, MappingDirection.FromHubToDst));
            Assert.AreEqual(2, this.dstController.HubMapResult.Count);
        }

        [Test]
        public void VerifyTransferToHub()
        {
            this.dstController.CurrentRepository = this.repository.Object;

            Assert.DoesNotThrowAsync(async () => await this.dstController.TransferMappedThingsToHub());

            this.navigationService.Setup(x =>
                x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(It.IsAny<CreateLogEntryDialogViewModel>())).Returns(false);

            this.dstController.SelectedDstMapResultForTransfer.Add(new ElementDefinition());
            Assert.DoesNotThrowAsync(async () => await this.dstController.TransferMappedThingsToHub());
            this.dstController.SelectedDstMapResultForTransfer.Clear();

            this.navigationService.Setup(x =>
                x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(It.IsAny<CreateLogEntryDialogViewModel>())).Returns(true);

            Assert.DoesNotThrowAsync(async () => await this.dstController.TransferMappedThingsToHub());
           
            this.statusBarControlViewModel.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Warning),
                Times.Exactly(3));

            var parameter = new Parameter()
            {
                Iid = Guid.NewGuid(),
                ParameterType = new SimpleQuantityKind(),
                ValueSet =
                {
                    new ParameterValueSet
                    {
                        Computed = new ValueArray<string>(new[] { "654321" }),
                        ValueSwitch = ParameterSwitchKind.MANUAL
                    }
                }
            };

            var elementDefinition1 = new ElementDefinition()
            {
                Iid = Guid.NewGuid(),
                Parameter =
                {
                    parameter
                },
                ContainedElement =
                {
                    new ElementUsage()
                    {
                        Iid = Guid.NewGuid(),
                        ElementDefinition = new ElementDefinition()
                        {
                            Iid = Guid.NewGuid(),
                            Container = this.iteration
                        }
                    }
                },
                Container = this.iteration
            };

            var existingElementDefinition = new ElementDefinition()
            {
                Iid = Guid.NewGuid(),
                Container = this.iteration
            };

            this.iteration.Element.Add(existingElementDefinition);
            existingElementDefinition = existingElementDefinition.Clone(false);

            var elementDefinition2 = new ElementDefinition()
            {
                Iid = Guid.NewGuid(),
                Container = this.iteration
            };

            var elementDefinition3 = new ElementDefinition()
            {
                Iid = Guid.NewGuid(),
                Container = this.iteration
            };

            this.dstController.DstMapResult.Add(new EnterpriseArchitectBlockElement(elementDefinition1, null, MappingDirection.FromDstToHub)
            {
                RelationShips = 
                {
                    new BinaryRelationship()
                    {
                        Source = elementDefinition1,
                        Target = elementDefinition2,
                        Iid = Guid.NewGuid()
                    },
                    new BinaryRelationship()
                    {
                        Source = elementDefinition1,
                        Target = elementDefinition3,
                        Iid = Guid.NewGuid()
                    },
                    new BinaryRelationship()
                    {
                        Source = elementDefinition2,
                        Target = elementDefinition1,
                        Iid = Guid.NewGuid()
                    },
                    new BinaryRelationship()
                    {
                        Source = elementDefinition1,
                        Target = existingElementDefinition,
                        Iid = Guid.NewGuid()
                    },
                    new BinaryRelationship()
                    {
                        Source = existingElementDefinition,
                        Target = elementDefinition1,
                        Iid = Guid.NewGuid()
                    }
                }
            });

            this.dstController.DstMapResult.Add(new EnterpriseArchitectBlockElement(elementDefinition2, null, MappingDirection.FromDstToHub));
            this.dstController.DstMapResult.Add(new EnterpriseArchitectBlockElement(elementDefinition3, null, MappingDirection.FromDstToHub));

            var requirementsGroup1 = new RequirementsGroup()
            {
                Iid = Guid.NewGuid()
            };

            var requirementsGroup2 = new RequirementsGroup()
            {
                Iid = Guid.NewGuid()
            };

            var requirementsSpecification1 = new RequirementsSpecification()
            {
                Iid = Guid.NewGuid(),
                Group = { requirementsGroup1, requirementsGroup2 }
            };
            
            var requirementsSpecification2 = new RequirementsSpecification()
            {
                Iid = Guid.NewGuid()
            };

            this.dstController.SelectedGroupsForTransfer.Add(requirementsGroup1);

            var requirement1 = new Requirement()
            {
                Iid = Guid.NewGuid(),
                Group = requirementsGroup1,
                Definition = 
                { 
                    new Definition()
                    {
                        Content = "A definition"
                    }
                }
            };

            var requirement2 = new Requirement()
            {
                Iid = Guid.NewGuid(),
                Group = requirementsGroup2
            };

            var requirement3 = new Requirement()
            {
                Iid = Guid.NewGuid(),
            };

            requirementsSpecification1.Requirement.Add(requirement1);
            requirementsSpecification1.Requirement.Add(requirement2);
            requirementsSpecification2.Requirement.Add(requirement3);

            this.dstController.DstMapResult.Add(new EnterpriseArchitectRequirementElement(requirement1, null, MappingDirection.FromDstToHub)
            {
                RelationShips =
                {
                    new BinaryRelationship()
                    {
                        Iid = Guid.NewGuid(),
                        Source = requirement1,
                        Target = requirement2
                    },
                    new BinaryRelationship()
                    {
                        Iid = Guid.NewGuid(),
                        Source = requirement2,
                        Target = requirement1
                    },
                    new BinaryRelationship()
                    {
                        Iid = Guid.NewGuid(),
                        Source = requirement1,
                        Target = requirement3
                    },
                    new BinaryRelationship()
                    {
                        Iid = Guid.NewGuid(),
                        Source = requirement3,
                        Target = requirement1
                    }
                }
            });

            this.dstController.DstMapResult.Add(new EnterpriseArchitectRequirementElement(requirement2, null, MappingDirection.FromDstToHub));
            this.dstController.DstMapResult.Add(new EnterpriseArchitectRequirementElement(requirement3, null, MappingDirection.FromDstToHub));
            this.dstController.SelectedDstMapResultForTransfer.Add(requirement1);
            this.dstController.SelectedDstMapResultForTransfer.Add(requirement3);
            this.dstController.SelectedDstMapResultForTransfer.AddRange(elementDefinition1.Parameter);
            this.dstController.SelectedDstMapResultForTransfer.AddRange(elementDefinition1.ContainedElement);
            this.dstController.SelectedDstMapResultForTransfer.AddRange(elementDefinition3.Parameter);
            this.dstController.SelectedDstMapResultForTransfer.AddRange(elementDefinition3.ContainedElement);

            this.hubController.Setup(x => x.GetThingById(parameter.Iid, It.IsAny<Iteration>(), out parameter)).Returns(true);

            Assert.DoesNotThrowAsync(async () => await this.dstController.TransferMappedThingsToHub());
        }

        [Test]
        public void VerifyLoadMapping()
        {
            this.dstController.CurrentRepository = this.repository.Object;
            Assert.DoesNotThrow(() => this.dstController.LoadMapping());
            this.hubController.Setup(x => x.OpenIteration).Returns((Iteration)null);
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            Assert.DoesNotThrow(() => this.dstController.LoadMapping());
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            Assert.DoesNotThrow(() => this.dstController.LoadMapping());
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            Assert.DoesNotThrow(() => this.dstController.LoadMapping());
            Assert.IsEmpty(this.dstController.DstMapResult);

            var mappedElements = new List<IMappedElementRowViewModel>();

            this.mappingConfiguration.Setup(x => x.LoadMappingFromDstToHub(It.IsAny<Repository>()))
                .Returns(mappedElements);

            Assert.DoesNotThrow(() => this.dstController.LoadMapping());
            Assert.IsEmpty(this.dstController.DstMapResult);

            mappedElements.Add(new EnterpriseArchitectBlockElement(null, null, MappingDirection.FromDstToHub));

            Assert.AreEqual(1, this.dstController.LoadMapping());

            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new HubSessionControlEvent()));

            Assert.DoesNotThrow(() => this.dstController.ResetConfigurationMapping());
        }

        [Test]
        public void VerifyTryGetAndAddFromRepository()
        {
            this.dstController.CurrentRepository = this.repository.Object;
            var element = new Mock<Element>();
            element.Setup(x => x.ElementID).Returns(42);
            element.Setup(x => x.Name).Returns("element");
            element.Setup(x => x.Stereotype).Returns("block");
            element.Setup(x => x.HasStereotype(StereotypeKind.Block.ToString().ToLower())).Returns(true);

            this.repository.Setup(x => x.GetElementSet("42", 0)).Returns(new EnterpriseArchitectCollection() { element.Object });

            this.repository.Setup(x => x.SQLQuery(It.Is<string>(s => s.Contains(element.Object.Name))))
                .Returns("<Data><Row><Object_ID>42</Object_ID></Row></Data>");

            this.repository.Setup(x => x.SQLQuery(It.Is<string>(s => !s.Contains(element.Object.Name))))
                .Returns("<Data></Data>");

            Assert.IsFalse(this.dstController.TryGetElement("element", StereotypeKind.Requirement, out var retrievedElement));
            Assert.IsFalse(this.dstController.TryGetElement("req", StereotypeKind.Requirement, out  retrievedElement));
            Assert.IsTrue(this.dstController.TryGetElement("element", StereotypeKind.Block, out  retrievedElement));

            var packageElement = new Mock<Element>();
            packageElement.Setup(x => x.Name).Returns("pack");
            packageElement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            packageElement.Setup(x => x.Type).Returns("Package");

            this.repository.Setup(x => x.GetPackageByGuid(packageElement.Object.ElementGUID)).Returns(this.package.Object);
            
            this.repository.Setup(x => x.SQLQuery(It.Is<string>(s => s.EndsWith($"\"{packageElement.Object.Name}\""))))
                .Returns($"<Data><Row><ea_guid>{packageElement.Object.ElementGUID}</ea_guid></Row></Data>");

            this.repository.Setup(x => x.SQLQuery(It.Is<string>(s => !s.EndsWith($"\"{packageElement.Object.Name}\""))))
                .Returns("<Data></Data>");

            Assert.IsFalse(this.dstController.TryGetPackage("req", out var retrievedPackage));
            Assert.IsTrue(this.dstController.TryGetPackage("pack", out  retrievedPackage));
        }

        [Test]
        public void VerifyTransferToDst()
        {
            this.dstController.CurrentRepository = this.repository.Object;
            this.repository.Setup(x => x.RefreshModelView(0));

            var requirement = new Mock<Element>();
            requirement.Setup(x => x.HasStereotype(StereotypeKind.Requirement.ToString().ToLower())).Returns(true);
            requirement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            requirement.Setup(x => x.PackageID).Returns(1);

            var taggedValueId = new Mock<TaggedValue>();
            taggedValueId.Setup(x => x.Name).Returns("id");
            taggedValueId.Setup(x => x.Value);
            taggedValueId.Setup(x => x.Update());
            var taggedValueText = new Mock<TaggedValue>();
            taggedValueText.Setup(x => x.Name).Returns("text");
            taggedValueText.Setup(x => x.Notes);
            taggedValueText.Setup(x => x.Update());

            requirement.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection()
            {
                taggedValueText, taggedValueId
            });

            this.dstController.UpdatedRequirementValues[requirement.Object.ElementGUID] = ("M05", "aText");
            this.dstController.CreatedElements.Add(requirement.Object);

            var block = new Mock<Element>();
            block.Setup(x => x.HasStereotype(StereotypeKind.Block.ToString().ToLower())).Returns(true);
            block.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());

            this.dstController.UpdatedStereotypes[block.Object.ElementGUID] = "customBlock";

            var customProperty = new Mock<CustomProperty>();
            customProperty.Setup(x => x.Name).Returns("default");
            customProperty.Setup(x => x.Value);

            var property = new Mock<Element>();
            property.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueProperty.ToString());
            property.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            property.Setup(x => x.CustomProperties).Returns(new EnterpriseArchitectCollection() { customProperty.Object });
            property.Setup(x => x.Connectors).Returns(new EnterpriseArchitectCollection());
            block.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection() { property.Object });
            block.Setup(x => x.EmbeddedElements).Returns(new EnterpriseArchitectCollection());
            this.dstController.UpdatedValuePropretyValues[property.Object.ElementGUID] = "423";

            var createdPackage = new Mock<Package>();
            createdPackage.Setup(x => x.ParentID).Returns(1);
            createdPackage.Setup(x => x.PackageID).Returns(4);
            createdPackage.Setup(x => x.PackageGUID).Returns(Guid.NewGuid().ToString());
            this.repository.Setup(x => x.GetPackageByID(4)).Returns(createdPackage.Object);
            this.repository.Setup(x => x.GetPackageByID(1)).Returns(this.package.Object);
            this.package.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection() { createdPackage.Object });
            this.dstController.CreatedPackages.Add(createdPackage.Object);

            this.dstController.SelectedHubMapResultForTransfer.Add(block.Object);
            this.dstController.SelectedHubMapResultForTransfer.Add(requirement.Object);
            Assert.DoesNotThrowAsync( async () => await this.dstController.TransferMappedThingsToDst());
        }

        [Test]
        public void VerifyTryGetValueType()
        {
            this.dstController.CurrentRepository = this.repository.Object;

            this.cacheService.Setup(x => x.GetElementsOfStereotype(StereotypeKind.ValueType)).Returns([]);

            var parameterType = new SimpleQuantityKind()
            {
                Name = "mass",
                ShortName = "mass"
            };

            var scale = new RatioScale()
            {
                Name = "kilogram",
                ShortName = "kg"
            };

            Assert.IsFalse(this.dstController.TryGetValueType(parameterType, scale, out var valueType));

            var existingValueType = new Mock<Element>();
            existingValueType.Setup(x => x.Stereotype).Returns(StereotypeKind.ValueType.ToString());
            existingValueType.Setup(x => x.Name).Returns("mass");
            existingValueType.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection());

            this.cacheService.Setup(x => x.GetElementsOfStereotype(StereotypeKind.ValueType)).Returns([existingValueType.Object]);

            Assert.IsFalse(this.dstController.TryGetValueType(parameterType, scale, out valueType));
            existingValueType.Setup(x => x.Name).Returns("mass[kg]");
            Assert.IsFalse(this.dstController.TryGetValueType(parameterType, scale, out valueType));

            var unitElement = new Mock<Element>();
            unitElement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            unitElement.Setup(x => x.Name).Returns("kg");

            var taggedValue = new Mock<TaggedValue>();
            taggedValue.Setup(x => x.Name).Returns("unit");
            taggedValue.Setup(x => x.Value).Returns(unitElement.Object.ElementGUID);
            existingValueType.Setup(x => x.TaggedValuesEx).Returns(new EnterpriseArchitectCollection(){taggedValue.Object});

            this.repository.Setup(x => x.GetElementByGuid(unitElement.Object.ElementGUID)).Returns(unitElement.Object);
            Assert.IsTrue(this.dstController.TryGetValueType(parameterType, scale, out valueType));
        }

        [Test]
        public void VerifyGetDefaultPackage()
        {
            this.dstController.CurrentRepository = this.repository.Object;
            var modelPackage = new Mock<Package>();
            modelPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection());
            this.repository.Setup(x => x.Models).Returns(new EnterpriseArchitectCollection() { modelPackage.Object });
            var existingPackageElement = new Mock<Element>();
            existingPackageElement.Setup(x => x.Type).Returns(StereotypeKind.Package.ToString());
            existingPackageElement.Setup(x => x.ElementGUID).Returns(Guid.NewGuid().ToString());
            existingPackageElement.Setup(x => x.Name).Returns($"COMET_{StereotypeKind.ValueType}s");
            var existingPackage = new Mock<Package>();

            this.cacheService.Setup(x => x.GetElementsOfMetaType(StereotypeKind.Package)).Returns([existingPackageElement.Object]);

            this.repository.Setup(x => x.SQLQuery(It.Is<string>(s => s.Contains(existingPackageElement.Object.Name))))
                .Returns($"<Data><Row><ea_guid>{existingPackageElement.Object.ElementGUID}</ea_guid></Row></Data>");

            this.repository.Setup(x => x.SQLQuery(It.Is<string>(s => !s.Contains(existingPackageElement.Object.Name))))
                .Returns($"<Data></Data>");

            this.repository.Setup(x => x.GetPackageByGuid(existingPackageElement.Object.ElementGUID)).Returns(existingPackage.Object);
            var defaultBlockPackage = this.dstController.GetDefaultPackage(StereotypeKind.Block);

            Assert.IsNotNull(defaultBlockPackage);
            Assert.AreNotEqual(existingPackage.Object, defaultBlockPackage);

            var defaultValueTypePackage = this.dstController.GetDefaultPackage(StereotypeKind.ValueType);

            Assert.IsNotNull(defaultValueTypePackage);
            Assert.AreEqual(existingPackage.Object, defaultValueTypePackage);

            var stateElement = new Mock<Element>();
            stateElement.Setup(x => x.MetaType).Returns(StereotypeKind.State.ToString());
            var statePackage = new Mock<Package>();
            statePackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection() { stateElement.Object });
            var subModelPackage = new Mock<Package>();
            subModelPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection() { statePackage.Object });
            subModelPackage.Setup(x => x.Elements).Returns(new EnterpriseArchitectCollection());
            modelPackage.Setup(x => x.Packages).Returns(new EnterpriseArchitectCollection() { subModelPackage.Object });

            var defaultStatePackage = this.dstController.GetDefaultPackage(StereotypeKind.State);
            Assert.IsNotNull(defaultStatePackage);
            Assert.AreEqual(statePackage.Object, defaultStatePackage);
        }
    }
}
