// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.DstController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using DEHEASysML.Enumerators;
    using DEHEASysML.Events;
    using DEHEASysML.Extensions;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;

    using EA;

    using NLog;

    using ReactiveUI;

    using Parameter = CDP4Common.EngineeringModelData.Parameter;
    using Requirement = CDP4Common.EngineeringModelData.Requirement;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// The <see cref="DstController" /> takes care of retrieving data from and to Enterprise Architext
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
        /// <summary>
        /// Gets this running tool name
        /// </summary>
        public static readonly string ThisToolName = typeof(DstController).Assembly.GetName().Name;

        /// <summary>
        /// The <see cref="IHubController" />
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IMappingEngine" />
        /// </summary>
        private readonly IMappingEngine mappingEngine;

        /// <summary>
        /// The <see cref="IExchangeHistoryService" />
        /// </summary>
        private readonly IExchangeHistoryService exchangeHistory;

        /// <summary>
        /// Gets the current class logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel" />
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// The <see cref="INavigationService" />
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// The <see cref="IMappingConfigurationService" />
        /// </summary>
        private readonly IMappingConfigurationService mappingConfigurationService;

        /// <summary>
        /// Backing field for <see cref="CurrentRepository" />
        /// </summary>
        private Repository currentRepository;

        /// <summary>
        /// Backing field for <see cref="CanMap" />
        /// </summary>
        private bool canMap;

        /// <summary>
        /// Backing field for <see cref="MappingDirection" />
        /// </summary>
        private MappingDirection mappingDirection;

        /// <summary>
        /// Backing field for <see cref="IsFileOpen" />
        /// </summary>
        private bool isFileOpen;

        /// <summary>
        /// Reference to the <see cref="Package" /> where Blocks are stores, to not recalculate it each time
        /// </summary>
        private Package defaultBlocksPackage;

        /// <summary>
        /// A value indicating if the next notifyContext event should be ignore
        /// </summary>
        private bool shouldIgnoreEvents;

        /// <summary>
        /// Backing field for <see cref="IsBusy" />
        /// </summary>
        private bool? isBusy;

        /// <summary>
        /// Initializes a new <see cref="DstController" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="mappingEngine">The <see cref="IMappingEngine" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel" /></param>
        /// <param name="exchangeHistory">The <see cref="IExchangeHistoryService" /></param>
        /// <param name="navigationService">The <see cref="INavigationService" /></param>
        /// <param name="mappingConfigurationService">The <see cref="IMappingConfigurationService" /></param>
        public DstController(IHubController hubController, IMappingEngine mappingEngine, IStatusBarControlViewModel statusBar,
            IExchangeHistoryService exchangeHistory, INavigationService navigationService, IMappingConfigurationService mappingConfigurationService)
        {
            this.hubController = hubController;
            this.mappingEngine = mappingEngine;
            this.statusBar = statusBar;
            this.exchangeHistory = exchangeHistory;
            this.navigationService = navigationService;
            this.mappingConfigurationService = mappingConfigurationService;

            this.InitializesObservables();
        }

        /// <summary>
        /// Gets or sets the <see cref="MappingDirection" />
        /// </summary>
        public MappingDirection MappingDirection
        {
            get => this.mappingDirection;
            set => this.RaiseAndSetIfChanged(ref this.mappingDirection, value);
        }

        /// <summary>
        /// The <see cref="CurrentRepository" />
        /// </summary>
        public Repository CurrentRepository
        {
            get => this.currentRepository;
            set => this.RaiseAndSetIfChanged(ref this.currentRepository, value);
        }

        /// <summary>
        /// Asserts that the mapping is available
        /// </summary>
        public bool CanMap
        {
            get => this.canMap;
            set => this.RaiseAndSetIfChanged(ref this.canMap, value);
        }

        /// <summary>
        /// Value asserting if the <see cref="DstController" /> is busy
        /// </summary>
        public bool? IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// A collection of <see cref="IMappedElementRowViewModel" /> resulting of the mapping from dst to hub
        /// </summary>
        public ReactiveList<IMappedElementRowViewModel> DstMapResult { get; } = new();

        /// <summary>
        /// A collection of <see cref="IMappedElementRowViewModel" /> resulting of the mapping from hub to dst
        /// </summary>
        public ReactiveList<IMappedElementRowViewModel> HubMapResult { get; } = new();

        /// <summary>
        /// A collection of <see cref="CDP4Common.CommonData.Thing" /> selected for the transfer
        /// </summary>
        public ReactiveList<Thing> SelectedDstMapResultForTransfer { get; } = new();

        /// <summary>
        /// A collection of <see cref="Thing" /> selected for the transfer
        /// </summary>
        public ReactiveList<Element> SelectedHubMapResultForTransfer { get; } = new();

        /// <summary>
        /// A collection of all <see cref="CDP4Common.EngineeringModelData.RequirementsGroup" /> that should be transfered
        /// </summary>
        public ReactiveList<RequirementsGroup> SelectedGroupsForTransfer { get; } = new();

        /// <summary>
        /// A collection of <see cref="Element" /> that has been created
        /// </summary>
        public List<Element> CreatedElements { get; } = new();

        /// <summary>
        /// A collection of <see cref="Element" /> that has been created
        /// </summary>
        public List<Package> CreatedPackages { get; } = new();

        /// <summary>
        /// A collection of <see cref="Collection" /> that has beeen updated
        /// </summary>
        public HashSet<Collection> UpdatedCollections { get; } = new();

        /// <summary>
        /// Gets the correspondance to the new value of a ValueProperty
        /// </summary>
        public Dictionary<string, string> UpdatedValuePropretyValues { get; } = new();

        /// <summary>
        /// Gets the correspondance to the new value of a ValueProperty
        /// </summary>
        public Dictionary<string, (string id, string text)> UpdatedRequirementValues { get; } = new();

        /// <summary>
        /// Gets or set the value if a file is open or not
        /// </summary>
        public bool IsFileOpen
        {
            get => this.isFileOpen;
            set => this.RaiseAndSetIfChanged(ref this.isFileOpen, value);
        }

        /// <summary>
        /// Handle to clear everything when Enterprise Architect close
        /// </summary>
        public void Disconnect()
        {
            this.hubController.Close();
            this.CurrentRepository = null;
        }

        /// <summary>
        /// Handle the initialization when Enterprise Architect connects the AddIn
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void Connect(Repository repository)
        {
            this.CurrentRepository = repository;
            this.logger.Info("DST Controller initialized");
        }

        /// <summary>
        /// Handle the FileOpen event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnFileOpen(Repository repository)
        {
            this.IsFileOpen = true;
            this.OnAnyEvent(repository);
        }

        /// <summary>
        /// Handle the FileClose event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnFileClose(Repository repository)
        {
            this.CleanProject();
            this.IsFileOpen = false;
            this.OnAnyEvent(repository);
        }

        /// <summary>
        /// Handle the FileNew event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnFileNew(Repository repository)
        {
            this.IsFileOpen = true;
            this.OnAnyEvent(repository);
        }

        /// <summary>
        /// Handle the OnNotifyContextItemModified event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="guid">The guid of the Item</param>
        /// <param name="objectType">The <see cref="ObjectType" /> of the item</param>
        public void OnNotifyContextItemModified(Repository repository, string guid, ObjectType objectType)
        {
            if (!this.shouldIgnoreEvents)
            {
                this.OnAnyEvent(repository);
            }
        }

        /// <summary>
        /// Tries to get an <see cref="Element" />
        /// </summary>
        /// <param name="name">The name of the <see cref="Element" /></param>
        /// <param name="stereotype">The stereotype applied to the <see cref="Element" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        /// <returns>A value asserting if the Element has been found</returns>
        public bool TryGetElement(string name, StereotypeKind stereotype, out Element element)
        {
            var queryResult = this.CurrentRepository.GetElementsByQuery("Simple", name);
            element = queryResult.OfType<Element>().FirstOrDefault(x => x.Name == name && x.Stereotype.AreEquals(stereotype));
            return element != null;
        }

        /// <summary>
        /// Tries to get an <see cref="Element" /> that represents an Interface
        /// </summary>
        /// <param name="name">The name of the interface</param>
        /// <param name="element">The <see cref="Element" /></param>
        /// <returns>A value asserting if the Element has been found</returns>
        public bool TryGetInterface(string name, out Element element)
        {
            var queryResult = this.CurrentRepository.GetElementsByQuery("Simple", name);
            element = queryResult.OfType<Element>().FirstOrDefault(x => x.Name == name && x.MetaType.AreEquals(StereotypeKind.Interface));
            return element != null;
        }

        /// <summary>
        /// Tries to get a <see cref="Package" />
        /// </summary>
        /// <param name="name">The name of the <see cref="Package" /></param>
        /// <param name="package">The <see cref="Package" /></param>
        /// <returns>A value indicating if the <see cref="Package" /> has been found</returns>
        public bool TryGetPackage(string name, out Package package)
        {
            package = null;

            var elementPackage = this.CurrentRepository.GetElementsByQuery("Simple", name)
                .OfType<Element>().FirstOrDefault(x => x.Type == "Package" && x.Name == name);

            if (elementPackage != null)
            {
                package = this.CurrentRepository.GetPackageByGuid(elementPackage.ElementGUID);
            }

            return package != null;
        }

        /// <summary>
        /// Tries to get a ValueType
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType" /></param>
        /// <param name="scale">The <see cref="MeasurementScale" /></param>
        /// <param name="valueType">The <see cref="Element" /> representing the ValueType</param>
        /// <returns>A value indicating if the <see cref="Element" /> has been found</returns>
        public bool TryGetValueType(ParameterType parameterType, MeasurementScale scale, out Element valueType)
        {
            QuantityKind generalQuantityKind = null;

            if (parameterType is SpecializedQuantityKind specialized)
            {
                generalQuantityKind = specialized.General;
            }

            var valueTypes = this.CurrentRepository.GetElementsByQuery("Extended", StereotypeKind.ValueType.ToString())
                .OfType<Element>().Where(x => x.Stereotype.AreEquals(StereotypeKind.ValueType)).ToList();

            valueType = valueTypes.FirstOrDefault(x => this.VerifyNames(parameterType, scale, x) || this.VerifyNames(generalQuantityKind, scale, x));
            return valueType != null;
        }

        /// <summary>
        /// Adds a new <see cref="Element" /> to the given <see cref="Collection" />
        /// </summary>
        /// <param name="collection">The collection where to add the element</param>
        /// <param name="name">The <see cref="name" /> of the <see cref="Element" /></param>
        /// <param name="type">The type of the <see cref="Element" /></param>
        /// <param name="stereotypeKind">The <see cref="Stereotype" /> to apply</param>
        /// <returns>The added <see cref="Element" /></returns>
        public Element AddNewElement(Collection collection, string name, string type, StereotypeKind stereotypeKind)
        {
            var element = collection.AddNew(name, type) as Element;
            element.StereotypeEx = stereotypeKind.GetFQStereotype();

            this.UpdatedCollections.Add(collection);
            this.CreatedElements.Add(element);

            return element;
        }

        /// <summary>
        /// Tries to get an <see cref="Element" /> representing a Requirement based on is Id and on his name
        /// </summary>
        /// <param name="name">The name of the <see cref="Element" /></param>
        /// <param name="id">The Id of the requirement</param>
        /// <param name="elementRequirement">The retrieved <see cref="Element" /></param>
        /// <returns>A value indicating if the <see cref="Element" /> has been found</returns>
        public bool TryGetRequirement(string name, string id, out Element elementRequirement)
        {
            elementRequirement = null;

            var elements = this.CurrentRepository.GetElementsByQuery("Simple", name).OfType<Element>()
                .Where(x => x.Stereotype.AreEquals(StereotypeKind.Requirement));

            foreach (var element in elements)
            {
                var retrieveId = element.GetRequirementId();

                if (this.UpdatedRequirementValues.TryGetValue(element.ElementGUID, out var updatedValue))
                {
                    retrieveId = updatedValue.id;
                }

                if (retrieveId == id)
                {
                    elementRequirement = element;
                    break;
                }
            }

            return elementRequirement != null;
        }

        /// <summary>
        /// Adds a new <see cref="Package" /> under the given <see cref="Package" />
        /// </summary>
        /// <param name="parentPackage">The parent <see cref="Package" /></param>
        /// <param name="name">The name of the new package</param>
        /// <returns></returns>
        public Package AddNewPackage(Package parentPackage, string name)
        {
            this.shouldIgnoreEvents = true;
            var newPackage = parentPackage.Packages.AddNew(name, "Package") as Package;
            this.CreatedPackages.Add(newPackage);
            newPackage.Update();
            return newPackage;
        }

        /// <summary>
        /// Gets the default <see cref="Package" /> where Blocks are stored
        /// </summary>
        /// <returns>The default package</returns>
        public Package GetDefaultBlocksPackage()
        {
            if (this.defaultBlocksPackage == null)
            {
                foreach (var package in this.CurrentRepository.Models.OfType<Package>())
                {
                    var defaultPackage = this.GetDefaultBlocksPackage(package);

                    if (defaultPackage != null)
                    {
                        this.defaultBlocksPackage = defaultPackage;
                        break;
                    }
                }

                if (this.defaultBlocksPackage == null)
                {
                    var collection = this.CurrentRepository.Models.OfType<Package>().First().Packages;
                    this.defaultBlocksPackage = collection.AddNew("COMET", "Package") as Package;
                    this.defaultBlocksPackage.Update();
                    collection.Refresh();
                }
            }

            return this.defaultBlocksPackage;
        }

        /// <summary>
        /// Retrieve all <see cref="Element" /> of stereotype block or requirement contained in the project
        /// </summary>
        /// <returns>A collection of <see cref="Element" /></returns>
        public List<Element> GetAllBlocksAndRequirementsOfRepository()
        {
            var elements = this.CurrentRepository.GetElementsByQuery("Extended", "block").OfType<Element>()
                .Where(x => x.StereotypeEx.AreEquals(StereotypeKind.Block) && x.ParentID == 0).ToList();

            elements.AddRange(this.CurrentRepository.GetElementsByQuery("Extended", "requirement").OfType<Element>()
                .Where(x => x.StereotypeEx.AreEquals(StereotypeKind.Requirement)));

            foreach (var element in elements.ToList().Where(element => this.CreatedElements.Any(x => x.ElementGUID == element.ElementGUID)))
            {
                elements.Remove(element);
            }

            return elements;
        }

        /// <summary>
        /// Gets all requirements present inside a model
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>A collection of <see cref="Element" /> representing requirement</returns>
        public List<Element> GetAllRequirements(IDualPackage model)
        {
            return this.GetElementsFromPackage(model, StereotypeKind.Requirement);
        }

        /// <summary>
        /// Gets all blocks present inside a model
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>A collection of <see cref="Element" /> representing block</returns>
        public List<Element> GetAllBlocks(IDualPackage model)
        {
            return this.GetElementsFromPackage(model, StereotypeKind.Block);
        }

        /// <summary>
        /// Gets all ValueTypes present inside a model
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>A collection of <see cref="Element" /> representing ValueType</returns>
        public List<Element> GetAllValueTypes(IDualPackage model)
        {
            return this.GetElementsFromPackage(model, StereotypeKind.ValueType);
        }

        /// <summary>
        /// Gets the port <see cref="Element" /> and the interface <see cref="Element" /> of a port
        /// </summary>
        /// <param name="port">The port</param>
        /// <returns>A <see cref="Tuple{T1}" /> to represents the connection of the port</returns>
        public (Element port, Element interfaceElement) ResolvePort(Element port)
        {
            var propertyTypeElement = this.CurrentRepository.GetElementByID(port.PropertyType);
            var connector = propertyTypeElement.Connectors.OfType<Connector>().FirstOrDefault();

            return connector == null ? (null, propertyTypeElement) : this.ResolveConnector(connector);
        }

        /// <summary>
        /// Gets the source and the target <see cref="Element" />s of a <see cref="Connector" />
        /// </summary>
        /// <param name="connector">The <see cref="Connector" /></param>
        /// <returns>a <see cref="Tuple{T}" /> containing source and target</returns>
        public (Element source, Element target) ResolveConnector(Connector connector)
        {
            var source = this.CurrentRepository.GetElementByID(connector.ClientID);
            var target = this.CurrentRepository.GetElementByID(connector.SupplierID);

            return (source, target);
        }

        /// <summary>
        /// Retrieves all selected <see cref="Element" />
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of selected Element</returns>
        public IEnumerable<Element> GetAllSelectedElements(Repository repository)
        {
            this.CurrentRepository = repository;
            var mappableElement = new List<Element>();
            var collection = repository.GetTreeSelectedElements();

            for (short elementIndex = 0; elementIndex < collection.Count; elementIndex++)
            {
                if (collection.GetAt(elementIndex) is Element element &&
                    (element.Stereotype.AreEquals(StereotypeKind.Requirement) || element.Stereotype.AreEquals(StereotypeKind.Block))
                    && !mappableElement.Contains(element))
                {
                    mappableElement.Add(element);
                }
            }

            return mappableElement;
        }

        /// <summary>
        /// Retrieves all <see cref="Element" /> from the selected <see cref="Package" />
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of <see cref="Element" /></returns>
        public IEnumerable<Element> GetAllElementsInsidePackage(Repository repository)
        {
            this.CurrentRepository = repository;
            var mappableElement = new List<Element>();
            var selectedPackage = repository.GetTreeSelectedPackage();
            mappableElement.AddRange(this.GetAllBlocks(selectedPackage));
            mappableElement.AddRange(this.GetAllRequirements(selectedPackage));

            foreach (var createdElement in mappableElement.Where(x => this.CreatedElements.Any(created => created.ElementGUID == x.ElementGUID)).ToList())
            {
                mappableElement.Remove(createdElement);
            }

            return mappableElement;
        }

        /// <summary>
        /// Retrieve all Id of <see cref="Package" /> and its parent hierarchy that contains  <see cref="Element" /> inside the
        /// given collection
        /// </summary>
        /// <param name="elements">The collection of <see cref="Element" /></param>
        /// <returns>A collection of Id</returns>
        public IEnumerable<int> RetrieveAllParentsIdPackage(IEnumerable<Element> elements)
        {
            var packagesId = new List<int>();

            foreach (var element in elements.Where(element => !packagesId.Contains(element.PackageID)))
            {
                this.GetPackageParentId(element.PackageID, ref packagesId);
            }

            return packagesId;
        }

        /// <summary>
        /// Map all <see cref="IMappedElementRowViewModel" />
        /// </summary>
        /// <param name="elements">The collection of <see cref="IMappedElementRowViewModel" /></param>
        /// <param name="mappingDirectionToMap">The <see cref="MappingDirection" /></param>
        public void Map(List<IMappedElementRowViewModel> elements, MappingDirection mappingDirectionToMap)
        {
            if (mappingDirectionToMap == MappingDirection.FromDstToHub)
            {
                this.DstMapResult.Clear();
                var mappedElement = this.Map(elements.OfType<EnterpriseArchitectBlockElement>().ToList(), true);
                mappedElement.AddRange(this.Map(elements.OfType<EnterpriseArchitectRequirementElement>().ToList(), true));
                this.DstMapResult.AddRange(mappedElement);
            }
            else
            {
                this.CurrentRepository.EnableUIUpdates = false;
                this.HubMapResult.Clear();
                var hubMappedElement = this.Map(elements.OfType<ElementDefinitionMappedElement>().ToList(), true);
                hubMappedElement.AddRange(this.Map(elements.OfType<RequirementMappedElement>().ToList(), true));
                this.HubMapResult.AddRange(hubMappedElement);
                this.shouldIgnoreEvents = false;
            }
        }

        /// <summary>
        /// Premaps all <see cref="IMappedElementRowViewModel" />
        /// </summary>
        /// <param name="elements">The collection of <see cref="IMappedElementRowViewModel" /> to premap</param>
        /// <param name="mappingDirectionToMap">The <see cref="MappingDirection" /></param>
        /// <returns>The collection of premapped <see cref="IMappedElementRowViewModel" /></returns>
        public List<IMappedElementRowViewModel> PreMap(List<IMappedElementRowViewModel> elements, MappingDirection mappingDirectionToMap)
        {
            var premappedElements = new List<IMappedElementRowViewModel>();

            if (mappingDirectionToMap == MappingDirection.FromDstToHub)
            {
                premappedElements.AddRange(this.Map(elements.OfType<EnterpriseArchitectBlockElement>().ToList(), false));
                premappedElements.AddRange(this.Map(elements.OfType<EnterpriseArchitectRequirementElement>().ToList(), false));
            }
            else
            {
                premappedElements.AddRange(this.Map(elements.OfType<ElementDefinitionMappedElement>().ToList(), false));
                premappedElements.AddRange(this.Map(elements.OfType<RequirementMappedElement>().ToList(), false));
            }

            return premappedElements;
        }

        /// <summary>
        /// Transfers the mapped variables to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        public async Task TransferMappedThingsToHub()
        {
            this.IsBusy = true;

            try
            {
                var (iterationClone, transaction) = this.GetIterationTransaction();

                if (!(this.SelectedDstMapResultForTransfer.Any() && this.TrySupplyingAndCreatingLogEntry(transaction)))
                {
                    this.statusBar.Append("Transfer to the Hub aborted !", StatusBarMessageSeverity.Warning);
                    return;
                }

                this.PrepareThingsForTransfer(iterationClone, transaction);
                this.mappingConfigurationService.PersistExternalIdentifierMap(transaction, iterationClone);
                transaction.CreateOrUpdate(iterationClone);

                await this.hubController.Write(transaction);

                this.mappingConfigurationService.RefreshExternalIdentifierMap();
                await this.hubController.Refresh();
                await this.UpdateParametersValueSets();
                await this.hubController.Refresh();
            }
            catch (Exception e)
            {
                this.logger.Error(e);
                throw;
            }
            finally
            {
                this.SelectedGroupsForTransfer.Clear();
                this.SelectedDstMapResultForTransfer.Clear();
                this.LoadMapping();
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Transfers the mapped variables to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        public async Task TransferMappedThingsToDst()
        {
            this.IsBusy = true;

            foreach (var element in this.SelectedHubMapResultForTransfer)
            {
                if (element.Stereotype.AreEquals(StereotypeKind.Requirement))
                {
                    this.ProcessTransferOfRequirement(element);
                }
                else if (element.Stereotype.AreEquals(StereotypeKind.Block))
                {
                    this.ProcessTransferOfBlock(element);
                }
            }

            this.CleanProject();
            this.CurrentRepository.RefreshModelView(0);
            var (iteration, transaction) = this.GetIterationTransaction();
            this.mappingConfigurationService.PersistExternalIdentifierMap(transaction, iteration);
            transaction.CreateOrUpdate(iteration);

            await this.hubController.Write(transaction);
            await this.hubController.Refresh();

            this.mappingConfigurationService.RefreshExternalIdentifierMap();

            this.LoadMapping();
            this.IsBusy = false;
        }

        /// <summary>
        /// Loads the saved mapping and applies the mapping rule
        /// </summary>
        /// <returns>The number of mapped things loaded</returns>
        public int LoadMapping()
        {
            var hadHubMapping = this.HubMapResult.Any();
            this.IsBusy = true;
            var elementsLoaded = 0;

            if (this.hubController.IsSessionOpen && this.hubController.OpenIteration != null)
            {
                elementsLoaded += this.LoadMappingFromDstToHub() + this.LoadMappingFromHubToDst();
            }

            if (elementsLoaded == 0)
            {
                this.SelectedDstMapResultForTransfer.Clear();
                this.SelectedGroupsForTransfer.Clear();
                this.DstMapResult.Clear();
                this.HubMapResult.Clear();
            }

            this.CurrentRepository.EnableUIUpdates = !this.HubMapResult.IsEmpty;

            if (this.hubController.IsSessionOpen && this.hubController.OpenIteration != null && hadHubMapping)
            {
                CDPMessageBus.Current.SendMessage(new UpdateDstNetChangePreview());
            }

            this.IsBusy = false;
            this.statusBar.Append($"{elementsLoaded} Element(s) has been loaded");
            return elementsLoaded;
        }

        /// <summary>
        /// Resets the current <see cref="IMappingConfigurationService.ExternalIdentifierMap" />
        /// </summary>
        public void ResetConfigurationMapping()
        {
            this.mappingConfigurationService.ExternalIdentifierMap = new ExternalIdentifierMap();
        }

        /// <summary>
        /// Process the transfer of an mapped Element of stereotype block
        /// </summary>
        /// <param name="element">The <see cref="Element" /> to transfer</param>
        private void ProcessTransferOfBlock(Element element)
        {
            foreach (var port in element.Elements.GetAllPortsOfElement().ToList())
            {
                this.CreatedElements.RemoveAll(x => x.ElementGUID == port.ElementGUID);
            }

            foreach (var blockDefinition in element.GetAllPortsDefinitionOfElement().ToList())
            {
                this.CreatedElements.RemoveAll(x => x.ElementGUID == blockDefinition.ElementGUID);
            }

            foreach (var partProperty in element.Elements.GetAllPartPropertiesOfElement().ToList())
            {
                this.CreatedElements.RemoveAll(x => x.ElementGUID == partProperty.ElementGUID);
            }

            foreach (var property in element.Elements.GetAllValuePropertiesOfElement().ToList())
            {
                if (this.UpdatedValuePropretyValues.TryGetValue(property.ElementGUID, out var newValue))
                {
                    property.SetValueOfPropertyValue(newValue);
                    property.Update();
                    property.CustomProperties.Refresh();
                    this.UpdatedValuePropretyValues.Remove(property.ElementGUID);
                    this.CreatedElements.RemoveAll(x => x.ElementGUID == property.ElementGUID);
                }

                element.Update();
                element.Elements.Refresh();
                element.EmbeddedElements.Refresh();
            }
        }

        /// <summary>
        /// Process the transfer of an mapped Element of stereotype Requirement
        /// </summary>
        /// <param name="element">The <see cref="Element" /> to transfer</param>
        private void ProcessTransferOfRequirement(Element element)
        {
            if (this.UpdatedRequirementValues.TryGetValue(element.ElementGUID, out var newValues))
            {
                element.SetRequirementId(newValues.id);
                element.TaggedValuesEx.Refresh();
                element.SetRequirementText(newValues.text);
                element.TaggedValuesEx.Refresh();
                element.Update();
                this.CreatedElements.RemoveAll(x => x.ElementGUID == element.ElementGUID);
                this.ClearPackages(element);
            }
        }

        /// <summary>
        /// Removes <see cref="Package" /> from the <see cref="CreatedPackages" />
        /// </summary>
        /// <param name="element">The <see cref="Element" /></param>
        private void ClearPackages(Element element)
        {
            if (this.CreatedPackages.Any())
            {
                var packagesId = this.RetrieveAllParentsIdPackage(new List<Element> { element });

                foreach (var packageId in packagesId)
                {
                    this.CreatedPackages.RemoveAll(x => x.PackageID == packageId);
                }
            }
        }

        /// <summary>
        /// Loads the saved mapping to the dst and applies the mapping rule
        /// </summary>
        /// <returns>The number of mapped things loaded</returns>
        private int LoadMappingFromHubToDst()
        {
            if (this.mappingConfigurationService.LoadMappingFromHubToDst(this.CurrentRepository)
                    is not { } mappedElements || !mappedElements.Any())
            {
                this.SelectedHubMapResultForTransfer.Clear();
                this.HubMapResult.Clear();
                return 0;
            }

            this.Map(mappedElements, MappingDirection.FromHubToDst);

            return mappedElements.Count;
        }

        /// <summary>
        /// Delete all created <see cref="Element" /> that has not been transfered
        /// </summary>
        private void CleanProject()
        {
            foreach (var createdElement in this.CreatedElements)
            {
                try
                {
                    Collection collection = null;

                    if (createdElement.Stereotype.AreEquals(StereotypeKind.Block) && createdElement.ParentID == 0
                        || createdElement.Stereotype.AreEquals(StereotypeKind.Requirement) || createdElement.MetaType.AreEquals(StereotypeKind.Interface))
                    {
                        collection = this.CurrentRepository.GetPackageByID(createdElement.PackageID).Elements;
                    }
                    else if (createdElement.Stereotype.AreEquals(StereotypeKind.ValueProperty)
                             || createdElement.Stereotype.AreEquals(StereotypeKind.PartProperty) || createdElement.MetaType.AreEquals(StereotypeKind.Port)
                             || createdElement.Stereotype.AreEquals(StereotypeKind.Block) && createdElement.ParentID != 0)
                    {
                        if (this.CreatedElements.Any(x => x.ElementID == createdElement.ParentID))
                        {
                            continue;
                        }

                        collection = this.CurrentRepository.GetElementByID(createdElement.ParentID).Elements;
                    }

                    if (collection == null)
                    {
                        continue;
                    }

                    for (short collectionIndex = 0; collectionIndex < collection.Count; collectionIndex++)
                    {
                        if (collection.GetAt(collectionIndex) is Element element && element.ElementGUID == createdElement.ElementGUID)
                        {
                            collection.DeleteAt(collectionIndex, false);
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    this.logger.Warn($"Tries to delete an Element that does not exist anymore : {createdElement.Name}");
                }
            }

            foreach (var createdPackage in this.CreatedPackages)
            {
                try
                {
                    if (this.CreatedPackages.Any(x => x.PackageID == createdPackage.ParentID))
                    {
                        continue;
                    }

                    var collection = this.CurrentRepository.GetPackageByID(createdPackage.ParentID).Packages;

                    if (collection == null)
                    {
                        continue;
                    }

                    for (short collectionIndex = 0; collectionIndex < collection.Count; collectionIndex++)
                    {
                        if (collection.GetAt(collectionIndex) is not Package package || package.PackageGUID != createdPackage.PackageGUID)
                        {
                            continue;
                        }

                        collection.DeleteAt(collectionIndex, false);
                        break;
                    }
                }
                catch (Exception)
                {
                    this.logger.Warn($"Tries to delete an Package that does not exist anymore : {createdPackage.Name}");
                }
            }

            foreach (var updatedCollection in this.UpdatedCollections)
            {
                updatedCollection.Refresh();
            }

            this.CreatedPackages.Clear();
            this.UpdatedCollections.Clear();
            this.CreatedElements.Clear();
        }

        /// <summary>
        /// Verifies that the provided <see cref="ParameterType" /> and <see cref="Element" /> have matching names
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType" /></param>
        /// <param name="scale">The <see cref="MeasurementScale" /> of reference</param>
        /// <param name="valueType">The <see cref="Element" /></param>
        /// <returns>The result of the match</returns>
        private bool VerifyNames(ParameterType parameterType, MeasurementScale scale, Element valueType)
        {
            var unit = valueType.GetUnitOfValueProperty();

            if (parameterType is QuantityKind quantityKind && scale != null && !string.IsNullOrEmpty(scale.Name) && unit != null)
            {
                return this.VerifyQuantityKindNameAndUnit(quantityKind, scale, valueType, this.CurrentRepository.GetElementByGuid(unit.Value));
            }

            return scale == null && this.VerifyNames(parameterType, valueType.Name);
        }

        /// <summary>
        /// Verifies that the provided <see cref="DefinedThing" /> matches with the names
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType" /></param>
        /// <param name="valueTypeName">The name to check</param>
        /// <returns>The result of the match</returns>
        private bool VerifyNames(DefinedThing parameterType, string valueTypeName)
        {
            return parameterType.Name == valueTypeName || parameterType.ShortName == valueTypeName;
        }

        /// <summary>
        /// Verifies that the provided <see cref="ParameterType" /> and ValueType have matching names
        /// and alos that the provided <see cref="MeasurementScale" /> matches the Unit
        /// </summary>
        /// <param name="quantityKind">The <see cref="QuantityKind" /></param>
        /// <param name="scale">The <see cref="MeasurementScale" /></param>
        /// <param name="valueType">The <see cref="Element" /> for the ValueType</param>
        /// <param name="unit">The <see cref="Element" /> for the unit</param>
        /// <returns>The result of the match</returns>
        private bool VerifyQuantityKindNameAndUnit(QuantityKind quantityKind, MeasurementScale scale, Element valueType, Element unit)
        {
            return this.VerifyNames(quantityKind, valueType.Name.Split('[')[0]) && this.VerifyNames(scale, unit.Name);
        }

        /// <summary>
        /// Gets the default <see cref="Package" /> where Blocks are stored
        /// </summary>
        /// <param name="containerPackage">The <see cref="Package" /> to look into</param>
        /// <returns>The default package</returns>
        private Package GetDefaultBlocksPackage(Package containerPackage)
        {
            foreach (var package in containerPackage.Packages.OfType<Package>())
            {
                if (package.Elements.OfType<Element>().Any(x => x.Stereotype.AreEquals(StereotypeKind.Block)))
                {
                    return package;
                }

                var subPackage = this.GetDefaultBlocksPackage(package);

                if (subPackage != null)
                {
                    return subPackage;
                }
            }

            return null;
        }

        /// <summary>
        /// Initializes all <see cref="Observable" />
        /// </summary>
        private void InitializesObservables()
        {
            this.hubController.WhenAnyValue(x => x.IsSessionOpen)
                .Subscribe(_ => this.UpdateProperties());

            this.hubController.WhenAnyValue(x => x.OpenIteration)
                .Where(x => x == null).Subscribe(_ => this.ResetConfigurationMapping());
        }

        /// <summary>
        /// Handle the common behavior to all EA Events
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        private void OnAnyEvent(Repository repository)
        {
            this.CurrentRepository = repository;
            this.defaultBlocksPackage = null;
            this.LoadMapping();
        }

        /// <summary>
        /// Loads the saved mapping to the hub and applies the mapping rule
        /// </summary>
        /// <returns>The number of mapped things loaded</returns>
        private int LoadMappingFromDstToHub()
        {
            if (this.mappingConfigurationService.LoadMappingFromDstToHub(this.CurrentRepository)
                    is not { } mappedElements || !mappedElements.Any())
            {
                this.SelectedDstMapResultForTransfer.Clear();
                this.SelectedGroupsForTransfer.Clear();
                this.DstMapResult.Clear();
                return 0;
            }

            this.Map(mappedElements, MappingDirection.FromDstToHub);

            return mappedElements.Count;
        }

        /// <summary>
        /// Updates the <see cref="IValueSet" /> with the new values
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        private async Task UpdateParametersValueSets()
        {
            var (iterationClone, transaction) = this.GetIterationTransaction();

            var parameters = this.SelectedDstMapResultForTransfer
                .OfType<ElementDefinition>()
                .SelectMany(x => x.Parameter)
                .ToList();

            foreach (var parameter in parameters)
            {
                if (this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out Parameter newParameter))
                {
                    var clonedParameter = newParameter.Clone(false);

                    for (var valueSetIndex = 0; valueSetIndex < clonedParameter.ValueSet.Count; valueSetIndex++)
                    {
                        var valueSet = clonedParameter.ValueSet[valueSetIndex].Clone(false);
                        this.UpdateValueSet(valueSet, parameter.ValueSet[valueSetIndex]);
                        transaction.CreateOrUpdate(valueSet);
                    }

                    transaction.CreateOrUpdate(clonedParameter);
                }
            }

            transaction.CreateOrUpdate(iterationClone);
            await this.hubController.Write(transaction);
        }

        /// <summary>
        /// Update the <see cref="CDP4Common.EngineeringModelData.ParameterValueSet" />
        /// </summary>
        /// <param name="toUpdate">The <see cref="CDP4Common.EngineeringModelData.ParameterValueSet" /> to update</param>
        /// <param name="valueSet">The <see cref="CDP4Common.EngineeringModelData.ParameterValueSet" /> containing new values</param>
        private void UpdateValueSet(ParameterValueSet toUpdate, IValueSet valueSet)
        {
            this.exchangeHistory.Append(toUpdate, valueSet);

            toUpdate.Manual = valueSet.Manual;
            toUpdate.ValueSwitch = ParameterSwitchKind.MANUAL;
        }

        /// <summary>
        /// Prepares all <see cref="Thing" /> that are selected for transfer
        /// </summary>
        /// <param name="iterationClone">The <see cref="CDP4Common.EngineeringModelData.Iteration" /> clone</param>
        /// <param name="transaction">The <see cref="IThingTransaction" /></param>
        private void PrepareThingsForTransfer(Iteration iterationClone, IThingTransaction transaction)
        {
            var thingsToTransfer = new List<Thing>(this.SelectedDstMapResultForTransfer.OfType<ElementDefinition>());

            thingsToTransfer.AddRange(this.SelectedDstMapResultForTransfer.OfType<Requirement>()
                .Select(x => x.Container as RequirementsSpecification).Distinct());

            var mappedElements = this.SelectedDstMapResultForTransfer.Select(x => x.Iid)
                .Select(thingId => this.DstMapResult.OfType<EnterpriseArchitectBlockElement>()
                                       .FirstOrDefault(x => x.HubElement.Iid == thingId) ??
                                   (IMappedElementRowViewModel)this.DstMapResult.OfType<EnterpriseArchitectRequirementElement>()
                                       .FirstOrDefault(x => x.HubElement.Iid == thingId))
                .ToList();

            var relationships = mappedElements.SelectMany(x => x.RelationShips).GroupBy(x => x.Name)
                .Select(g => g.First()).ToList();

            foreach (var relationship in relationships.ToList())
            {
                if (this.SelectedDstMapResultForTransfer.All(x => x.Iid != relationship.Source.Container.Iid && x.Iid != relationship.Source.Iid)
                    && relationship.Target.Original == null ||
                    this.SelectedDstMapResultForTransfer.All(x => x.Iid != relationship.Target.Container.Iid
                                                                  && x.Iid != relationship.Target.Iid) && relationship.Target.Original == null)
                {
                    relationships.RemoveAll(x => x.Iid == relationship.Iid);
                }
            }

            this.statusBar.Append($"Processing {relationships.Count} relationship(s)");
            thingsToTransfer.AddRange(relationships);

            foreach (var thing in thingsToTransfer)
            {
                switch (thing)
                {
                    case ElementDefinition elementDefinition:
                        this.PrepareElementDefinitionForTransfer(iterationClone, transaction, elementDefinition);
                        break;
                    case RequirementsSpecification requirementsSpecification:
                        this.PrepareRequirementsSpecificationForTransfer(iterationClone, transaction, requirementsSpecification);
                        break;
                    case BinaryRelationship relationship:
                        this.AddOrUpdateIterationAndTransaction(relationship, iterationClone.Relationship, transaction);
                        break;
                }
            }
        }

        /// <summary>
        /// Updates the <see cref="IThingTransaction" /> and the <see cref="ContainerList{T}" /> with the provided
        /// <see cref="Thing" />
        /// </summary>
        /// <typeparam name="TThing">A <see cref="Thing" /></typeparam>
        /// <param name="thing">The <see cref="Thing" /></param>
        /// <param name="containerList">The <see cref="ContainerList{T}" /></param>
        /// <param name="transaction">The <see cref="IThingTransaction" /></param>
        private void AddOrUpdateIterationAndTransaction<TThing>(TThing thing, ContainerList<TThing> containerList, IThingTransaction transaction)
            where TThing : Thing
        {
            try
            {
                if (thing.Container == null || containerList.All(x => x.Iid != thing.Iid))
                {
                    containerList.Add(thing);
                    this.exchangeHistory.Append(thing, ChangeKind.Create);
                }
                else
                {
                    this.exchangeHistory.Append(thing, ChangeKind.Update);
                }

                transaction.CreateOrUpdate(thing);
            }
            catch (Exception exception)
            {
                this.logger.Error(exception);
            }
        }

        /// <summary>
        /// Prepares the provided <see cref="RequirementsSpecification" /> for transfer
        /// </summary>
        /// <param name="iterationClone">The <see cref="Iteration" /> clone</param>
        /// <param name="transaction">The <see cref="IThingTransaction" /></param>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /> to transfer</param>
        private void PrepareRequirementsSpecificationForTransfer(Iteration iterationClone, IThingTransaction transaction, RequirementsSpecification requirementsSpecification)
        {
            this.AddOrUpdateIterationAndTransaction(requirementsSpecification, iterationClone.RequirementsSpecification, transaction);

            var groups = requirementsSpecification.Group;

            this.RegisterRequirementsGroups(transaction, groups);

            foreach (var requirement in this.SelectedDstMapResultForTransfer.OfType<Requirement>()
                         .Where(x => x.Container.Iid == requirementsSpecification.Iid))
            {
                transaction.CreateOrUpdate(requirement);

                foreach (var definition in requirement.Definition)
                {
                    transaction.CreateOrUpdate(definition);
                }
            }
        }

        /// <summary>
        /// Registers the <see cref="RequirementsGroup" /> to be created or updated
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction" /></param>
        /// <param name="groups">The <see cref="ContainerList{T}" /> of <see cref="RequirementsGroup" /></param>
        private void RegisterRequirementsGroups(IThingTransaction transaction, ContainerList<RequirementsGroup> groups)
        {
            foreach (var requirementsGroup in groups)
            {
                if (this.SelectedGroupsForTransfer.Any(x => x.Iid == requirementsGroup.Iid))
                {
                    transaction.CreateOrUpdate(requirementsGroup);
                }

                if (requirementsGroup.Group.Any())
                {
                    this.RegisterRequirementsGroups(transaction, requirementsGroup.Group);
                }
            }
        }

        /// <summary>
        /// Prepares the provided <see cref="ElementDefinition" /> for transfer
        /// </summary>
        /// <param name="iterationClone">The <see cref="Iteration" /> clone</param>
        /// <param name="transaction">The <see cref="IThingTransaction" /></param>
        /// <param name="elementDefinition">The <see cref="ElementDefinition" /> to prepare</param>
        private void PrepareElementDefinitionForTransfer(Iteration iterationClone, IThingTransaction transaction, ElementDefinition elementDefinition)
        {
            this.AddOrUpdateIterationAndTransaction(elementDefinition, iterationClone.Element, transaction);

            foreach (var elementUsage in elementDefinition.ContainedElement)
            {
                this.AddOrUpdateIterationAndTransaction(elementUsage.ElementDefinition.Clone(false), iterationClone.Element, transaction);
                this.AddOrUpdateIterationAndTransaction(elementUsage, elementDefinition.ContainedElement, transaction);
            }

            foreach (var parameter in elementDefinition.Parameter)
            {
                transaction.CreateOrUpdate(parameter);
            }
        }

        /// <summary>
        /// Pops the <see cref="CreateLogEntryDialog" /> and based on its result, either registers a new ModelLogEntry to the
        /// <see cref="transaction" /> or not
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction" /> that will get the changes registered to</param>
        /// <returns>A boolean result, true if the user pressed OK, otherwise false</returns>
        private bool TrySupplyingAndCreatingLogEntry(ThingTransaction transaction)
        {
            var vm = new CreateLogEntryDialogViewModel();

            var dialogResult = this.navigationService
                .ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(vm);

            if (dialogResult != true)
            {
                return false;
            }

            this.hubController.RegisterNewLogEntryToTransaction(vm.LogEntryContent, transaction);
            return true;
        }

        /// <summary>
        /// Initializes a new <see cref="IThingTransaction" /> based on the current open <see cref="Iteration" />
        /// </summary>
        /// <returns>
        /// A <see cref="ValueTuple" /> Containing the <see cref="Iteration" /> clone and the
        /// <see cref="IThingTransaction" />
        /// </returns>
        private (Iteration clone, ThingTransaction transaction) GetIterationTransaction()
        {
            var iterationClone = this.hubController.OpenIteration.Clone(false);
            return (iterationClone, new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone));
        }

        /// <summary>
        /// Map all <see cref="EnterpriseArchitectBlockElement" />
        /// </summary>
        /// <param name="blockElements">The collection of <see cref="EnterpriseArchitectBlockElement" /></param>
        /// <param name="isCompleteMapping">Asserts if the mapping has to been complete or not</param>
        /// <returns>A collection of <see cref="IMappedElementRowViewModel" /></returns>
        private List<IMappedElementRowViewModel> Map(List<EnterpriseArchitectBlockElement> blockElements, bool isCompleteMapping)
        {
            if (this.mappingEngine.Map((isCompleteMapping, blockElements)) is List<MappedElementDefinitionRowViewModel> mappedElements && mappedElements.Any())
            {
                if (isCompleteMapping)
                {
                    this.statusBar.Append($"Mapping of {mappedElements.Count} blocks proceed...");
                }

                return new List<IMappedElementRowViewModel>(mappedElements);
            }

            return new List<IMappedElementRowViewModel>();
        }

        /// <summary>
        /// Map all <see cref="EnterpriseArchitectRequirementElement" />
        /// </summary>
        /// <param name="requirementElements">
        /// The collection of <see cref="EnterpriseArchitectRequirementElement" />
        /// </param>
        /// <param name="isCompleteMapping">Asserts if the mapping has to been complete or not</param>
        /// <returns>A collection of <see cref="IMappedElementRowViewModel" /></returns>
        private List<IMappedElementRowViewModel> Map(List<EnterpriseArchitectRequirementElement> requirementElements, bool isCompleteMapping)
        {
            if (this.mappingEngine.Map((isCompleteMapping, requirementElements)) is List<MappedRequirementRowViewModel> mappedElements
                && mappedElements.Any())
            {
                if (isCompleteMapping)
                {
                    this.statusBar.Append($"Mapping of {mappedElements.Count} requirements in progress...");
                }

                return new List<IMappedElementRowViewModel>(mappedElements);
            }

            return new List<IMappedElementRowViewModel>();
        }

        /// <summary>
        /// Maps a collection of <see cref="ElementDefinitionMappedElement" />
        /// </summary>
        /// <param name="mappedElement">The collection of <see cref="ElementDefinitionMappedElement" /></param>
        /// <param name="isComplete">A value indicating if the mapping has to be complete</param>
        /// <returns>The result of the mapping</returns>
        private List<IMappedElementRowViewModel> Map(List<ElementDefinitionMappedElement> mappedElement, bool isComplete)
        {
            if (this.mappingEngine.Map((isComplete, mappedElement)) is List<MappedElementDefinitionRowViewModel> mappedElements && mappedElements.Any())
            {
                if (isComplete)
                {
                    this.statusBar.Append($"Mapping of {mappedElements.Count} Element(s) Definition in progress...");
                }

                return new List<IMappedElementRowViewModel>(mappedElements);
            }

            return new List<IMappedElementRowViewModel>();
        }

        /// <summary>
        /// Maps a collection of <see cref="RequirementMappedElement" />
        /// </summary>
        /// <param name="requirementElements">The collection of <see cref="RequirementMappedElement" /></param>
        /// <param name="isComplete">A value indicating if the mapping has to be complete</param>
        /// <returns>The result of the mapping</returns>
        private List<IMappedElementRowViewModel> Map(List<RequirementMappedElement> requirementElements, bool isComplete)
        {
            if (this.mappingEngine.Map((isComplete, requirementElements)) is List<MappedRequirementRowViewModel> mappedElements && mappedElements.Any())
            {
                if (isComplete)
                {
                    this.statusBar.Append($"Mapping of {mappedElements.Count} Requirement(s) in progress...");
                }

                return new List<IMappedElementRowViewModel>(mappedElements);
            }

            return new List<IMappedElementRowViewModel>();
        }

        /// <summary>
        /// Gets the Id of each parent of the given <see cref="Package" /> Id
        /// </summary>
        /// <param name="packageId">The <see cref="Package" /> id</param>
        /// <param name="packagesId">A collection of all <see cref="Package" /> already found</param>
        private void GetPackageParentId(int packageId, ref List<int> packagesId)
        {
            while (true)
            {
                var package = this.CurrentRepository.GetPackageByID(packageId);

                if (!packagesId.Contains(package.PackageID))
                {
                    packagesId.Add(package.PackageID);

                    if (package.ParentID != 0)
                    {
                        packageId = package.ParentID;
                        continue;
                    }
                }

                break;
            }
        }

        /// <summary>
        /// Update the properties
        /// </summary>
        private void UpdateProperties()
        {
            this.HubMapResult.Clear();
            this.SelectedHubMapResultForTransfer.Clear();
            this.DstMapResult.Clear();
            this.SelectedDstMapResultForTransfer.Clear();
            this.CleanProject();
            this.CanMap = this.hubController.IsSessionOpen;
        }

        /// <summary>
        /// Gets all element of a given stereo presents inside a package, including sub packages
        /// </summary>
        /// <param name="package">The <see cref="IDualPackage" /></param>
        /// <param name="stereotype">The stereotype of the <see cref="Element" /></param>
        /// <returns>A collection of <see cref="Element" /></returns>
        private List<Element> GetElementsFromPackage(IDualPackage package, StereotypeKind stereotype)
        {
            var elements = new List<Element>();
            elements.AddRange(package.GetElementsOfStereotypeInPackage(stereotype));

            foreach (var subPackage in package.Packages.OfType<Package>())
            {
                elements.AddRange(this.GetElementsFromPackage(subPackage, stereotype));
            }

            return elements;
        }
    }
}
