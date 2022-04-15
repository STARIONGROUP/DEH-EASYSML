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

    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using NLog;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DstController" /> takes care of retrieving data from and to Enterprise Architext
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
        /// <summary>
        /// The <see cref="IHubController" />
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IMappingEngine" />
        /// </summary>
        private readonly IMappingEngine mappingEngine;

        /// <summary>
        /// Gets the current class logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel" />
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// Backing field for <see cref="CurrentRepository" />
        /// </summary>
        private Repository currentRepository;

        /// <summary>
        /// Backing field for <see cref="CanMap" />
        /// </summary>
        private bool canMap;

        /// <summary>
        /// Initializes a new <see cref="DstController" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="mappingEngine">The <see cref="IMappingEngine" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel" /></param>
        public DstController(IHubController hubController, IMappingEngine mappingEngine, IStatusBarControlViewModel statusBar)
        {
            this.hubController = hubController;
            this.mappingEngine = mappingEngine;
            this.statusBar = statusBar;

            this.hubController.WhenAnyValue(x => x.IsSessionOpen)
                .Subscribe(_ => this.UpdateProperties());
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
        /// A collection of <see cref="IMappedElementRowViewModel" />
        /// </summary>
        public ReactiveList<IMappedElementRowViewModel> DstMapResult { get; } = new();

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
            this.CurrentRepository = repository;
        }

        /// <summary>
        /// Handle the FileClose event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnFileClose(Repository repository)
        {
            this.CurrentRepository = repository;
        }

        /// <summary>
        /// Handle the FileNew event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnFileNew(Repository repository)
        {
            this.OnFileOpen(repository);
        }

        /// <summary>
        /// Handle the OnNotifyContextItemModified event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="guid">The guid of the Item</param>
        /// <param name="objectType">The <see cref="ObjectType" /> of the item</param>
        public void OnNotifyContextItemModified(Repository repository, string guid, ObjectType objectType)
        {
            this.OnFileOpen(repository);
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
        public void Map(List<IMappedElementRowViewModel> elements)
        {
            this.DstMapResult.Clear();
            this.Map(elements.OfType<EnterpriseArchitectBlockElement>().ToList(), true);
            this.Map(elements.OfType<EnterpriseArchitectRequirementElement>().ToList(), true);
        }

        /// <summary>
        /// Premaps all <see cref="IMappedElementRowViewModel" />
        /// </summary>
        /// <param name="elements">The collection of <see cref="IMappedElementRowViewModel"/> to premap</param>
        /// <returns>The collection of premapped <see cref="IMappedElementRowViewModel" /></returns>
        public List<IMappedElementRowViewModel> PreMap(List<IMappedElementRowViewModel> elements)
        {
            var premappedElements = new List<IMappedElementRowViewModel>();
            premappedElements.AddRange(this.Map(elements.OfType<EnterpriseArchitectBlockElement>().ToList(), false));
            premappedElements.AddRange(this.Map(elements.OfType<EnterpriseArchitectRequirementElement>().ToList(), false));
            return premappedElements;
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
                    this.statusBar.Append($"Mapping of {mappedElements.Count} blocks in progress...");
                    this.DstMapResult.AddRange(mappedElements);
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
                    this.DstMapResult.AddRange(mappedElements);
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
