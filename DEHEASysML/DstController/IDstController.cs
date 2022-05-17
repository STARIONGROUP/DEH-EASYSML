// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDstController.cs" company="RHEA System S.A.">
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

    using CDP4Common;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHEASysML.Enumerators;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;

    using EA;

    using ReactiveUI;

    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Interface definition for <see cref="DstController" />
    /// </summary>
    public interface IDstController
    {
        /// <summary>
        /// The <see cref="Repository" />
        /// </summary>
        Repository CurrentRepository { get; }

        /// <summary>
        /// Asserts if the mapping is available
        /// </summary>
        bool CanMap { get; set; }

        /// <summary>
        /// A collection of <see cref="IMappedElementRowViewModel" />
        /// </summary>
        ReactiveList<IMappedElementRowViewModel> DstMapResult { get; }

        /// <summary>
        /// The <see cref="MappingDirection" />
        /// </summary>
        MappingDirection MappingDirection { get; set; }

        /// <summary>
        /// A collection of <see cref="CDP4Common.CommonData.Thing" /> selected for the transfer
        /// </summary>
        ReactiveList<Thing> SelectedDstMapResultForTransfer { get; }

        /// <summary>
        /// A collection of all <see cref="CDP4Common.EngineeringModelData.RequirementsGroup" /> that should be transfered
        /// </summary>
        ReactiveList<RequirementsGroup> SelectedGroupsForTransfer { get; }

        /// <summary>
        /// A collection of <see cref="IMappedElementRowViewModel" /> resulting of the mapping from hub to dst
        /// </summary>
        ReactiveList<IMappedElementRowViewModel> HubMapResult { get; }

        /// <summary>
        /// Gets or set the value if a file is open or not
        /// </summary>
        bool IsFileOpen { get; set; }

        /// <summary>
        /// A collection of <see cref="Thing" /> selected for the transfer
        /// </summary>
        ReactiveList<Element> SelectedHubMapResultForTransfer { get; }

        /// <summary>
        /// Gets the correspondance to the new value of a ValueProperty
        /// </summary>
        Dictionary<string, string> UpdatedValuePropretyValues { get; }

        /// <summary>
        /// A collection of <see cref="Element" /> that has been created
        /// </summary>
        List<Element> CreatedElements { get; }

        /// <summary>
        /// Gets the correspondance to the new value of a ValueProperty
        /// </summary>
        Dictionary<string, (string id, string text)> UpdatedRequirementValues { get; }

        /// <summary>
        /// Value asserting if the <see cref="DstController" /> is busy
        /// </summary>
        bool? IsBusy { get; set; }

        /// <summary>
        /// Correspondance between a state <see cref="Element" /> and a collection of the <see cref="Partition" /> where it as been
        /// modified and the <see cref="ChangeKind" /> applied
        /// to the partitions
        /// </summary>
        Dictionary<Element, List<(Partition, ChangeKind)>> ModifiedPartitions { get; }

        /// <summary>
        /// A collectior of <see cref="Connector" /> that has been created during the mapping from hub to dst
        /// </summary>
        List<Connector> CreatedConnectors { get; }

        /// <summary>
        /// Corrspondance between a <see cref="Element"/> Guid of Stereotype ValueProperty and new PropertyType Value
        /// </summary>
        Dictionary<string, int> UpdatePropertyTypes { get; }

        /// <summary>
        /// Handle to clear everything when Enterprise Architect close
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Handle the initialization when Enterprise Architect connects the AddIn
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        void Connect(Repository repository);

        /// <summary>
        /// Handle the FileOpen event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        void OnFileOpen(Repository repository);

        /// <summary>
        /// Handle the FileClose event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        void OnFileClose(Repository repository);

        /// <summary>
        /// Handle the FileNew event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        void OnFileNew(Repository repository);

        /// <summary>
        /// Handle the OnNotifyContextItemModified event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="guid">The guid of the Item</param>
        /// <param name="objectType">The <see cref="ObjectType" /> of the item</param>
        void OnNotifyContextItemModified(Repository repository, string guid, ObjectType objectType);

        /// <summary>
        /// Map all <see cref="IMappedElementRowViewModel" />
        /// </summary>
        /// <param name="elements">The collection of <see cref="IMappedElementRowViewModel" /></param>
        /// <param name="mappingDirectionToMap">The <see cref="MappingDirection" /></param>
        void Map(List<IMappedElementRowViewModel> elements, MappingDirection mappingDirectionToMap);

        /// <summary>
        /// Gets all requirements present inside a model
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>A collection of <see cref="Element" /> representing requirement</returns>
        List<Element> GetAllRequirements(IDualPackage model);

        /// <summary>
        /// Gets all blocks present inside a model
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>A collection of <see cref="Element" /> representing block</returns>
        List<Element> GetAllBlocks(IDualPackage model);

        /// <summary>
        /// Gets all ValueTypes present inside a model
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>A collection of <see cref="Element" /> representing ValueType</returns>
        List<Element> GetAllValueTypes(IDualPackage model);

        /// <summary>
        /// Gets the port <see cref="Element" /> and the interface <see cref="Element" /> of a port
        /// </summary>
        /// <param name="port">The port</param>
        /// <returns>A <see cref="Tuple{T1}" /> to represents the connection of the port</returns>
        (Element port, Element interfaceElement) ResolvePort(Element port);

        /// <summary>
        /// Gets the source and the target <see cref="Element" />s of a <see cref="Connector" />
        /// </summary>
        /// <param name="connector">The <see cref="Connector" /></param>
        /// <returns>a <see cref="Tuple{T}" /> containing source and target</returns>
        (Element source, Element target) ResolveConnector(Connector connector);

        /// <summary>
        /// Retrieves all selected <see cref="Element" />
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of selected  <see cref="Element" /></returns>
        IEnumerable<Element> GetAllSelectedElements(Repository repository);

        /// <summary>
        /// Retrieves all <see cref="Element" /> from the selected <see cref="Package" />
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>A collection of <see cref="Element" /></returns>
        IEnumerable<Element> GetAllElementsInsidePackage(Repository repository);

        /// <summary>
        /// Retrieve all Id of <see cref="Package" /> and its parent hierarchy that contains  <see cref="Element" /> inside the
        /// given collection
        /// </summary>
        /// <param name="elements">The collection of <see cref="Element" /></param>
        /// <returns>A collection of Id</returns>
        IEnumerable<int> RetrieveAllParentsIdPackage(IEnumerable<Element> elements);

        /// <summary>
        /// Premaps all <see cref="IMappedElementRowViewModel" />
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="mappingDirectionToMap">The <see cref="MappingDirection" /></param>
        /// <returns>The collection of premapped <see cref="IMappedElementRowViewModel" /></returns>
        List<IMappedElementRowViewModel> PreMap(List<IMappedElementRowViewModel> elements, MappingDirection mappingDirectionToMap);

        /// <summary>
        /// Transfers the mapped variables to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        Task TransferMappedThingsToHub();

        /// <summary>
        /// Transfers the mapped variables to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        Task TransferMappedThingsToDst();

        /// <summary>
        /// Loads the saved mapping and applies the mapping rule
        /// </summary>
        /// <returns>The number of mapped things loaded</returns>
        int LoadMapping();

        /// <summary>
        /// Tries to get an <see cref="Element" />
        /// </summary>
        /// <param name="name">The name of the <see cref="Element" /></param>
        /// <param name="stereotype">The stereotype applied to the <see cref="Element" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        /// <returns>A value asserting if the <see cref="Element" /> has been found</returns>
        bool TryGetElement(string name, StereotypeKind stereotype, out Element element);

        /// <summary>
        /// Tries to get a ValueType
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType" /></param>
        /// <param name="scale">The <see cref="MeasurementScale" /></param>
        /// <param name="valueType">The <see cref="Element" /> representing the ValueType</param>
        /// <returns>A value indicating if the <see cref="Element" /> has been found</returns>
        bool TryGetValueType(ParameterType parameterType, MeasurementScale scale, out Element valueType);

        /// <summary>
        /// Gets the default <see cref="Package" /> where Element of the given StereoType are stored
        /// </summary>
        /// <param name="stereotypeKind">The <see cref="StereotypeKind" /></param>
        /// <returns>The default package</returns>
        Package GetDefaultPackage(StereotypeKind stereotypeKind);

        /// <summary>
        /// Adds a new <see cref="Element" /> to the given <see cref="Collection" />
        /// </summary>
        /// <param name="collection">The collection where to add the element</param>
        /// <param name="name">The <see cref="name" /> of the <see cref="Element" /></param>
        /// <param name="type">The type of the <see cref="Element" /></param>
        /// <param name="stereotypeKind">The <see cref="Stereotype" /> to apply</param>
        /// <returns>The added <see cref="Element" /></returns>
        Element AddNewElement(Collection collection, string name, string type, StereotypeKind stereotypeKind);

        /// <summary>
        /// Tries to get a <see cref="Package" />
        /// </summary>
        /// <param name="name">The name of the <see cref="Package" /></param>
        /// <param name="package">The <see cref="Package" /></param>
        /// <returns>A value indicating if the <see cref="Package" /> has been found</returns>
        bool TryGetPackage(string name, out Package package);

        /// <summary>
        /// Adds a new <see cref="Package" /> under the given <see cref="Package" />
        /// </summary>
        /// <param name="parentPackage">The parent <see cref="Package" /></param>
        /// <param name="name">The name of the new package</param>
        /// <returns></returns>
        Package AddNewPackage(Package parentPackage, string name);

        /// <summary>
        /// Retrieve all <see cref="Element" /> of stereotype block or requirement contained in the project
        /// </summary>
        /// <returns>A collection of <see cref="Element" /></returns>
        List<Element> GetAllBlocksAndRequirementsOfRepository();

        /// <summary>
        /// Tries to get an <see cref="Element" /> that represents by his type
        /// </summary>
        /// <param name="name">The name of the <see cref="Element" /></param>
        /// <param name="type">The type of the Element</param>
        /// <param name="element">The <see cref="Element" /></param>
        /// <returns>A value asserting if the Element has been found</returns>
        bool TryGetElementByType(string name, StereotypeKind type, out Element element);

        /// <summary>
        /// Tries to get an <see cref="Element" /> representing a Requirement based on is Id and on his name
        /// </summary>
        /// <param name="name">The name of the <see cref="Element" /></param>
        /// <param name="id">The Id of the requirement</param>
        /// <param name="elementRequirement">The retrieved <see cref="Element" /></param>
        /// <returns>A value indicating if the <see cref="Element" /> has been found</returns>
        bool TryGetRequirement(string name, string id, out Element elementRequirement);

        /// <summary>
        /// Tries to get the block that define the correct given Interface
        /// </summary>
        /// <param name="interfaceElement">The <see cref="Element"/></param>
        /// <param name="blockDefinition">The retrieve block <see cref="Element"/></param>
        /// <returns>a value indicating if the <see cref="Element"/> has been found</returns>
        bool TryGetInterfaceImplementation(Element interfaceElement, out Element blockDefinition);
    }
}
