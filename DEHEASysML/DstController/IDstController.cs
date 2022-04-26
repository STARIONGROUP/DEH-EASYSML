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

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

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
        /// A collection of <see cref="Thing" /> selected for the transfer
        /// </summary>
        ReactiveList<Thing> SelectedDstMapResultForTransfer { get; }

        /// <summary>
        /// A collection of all <see cref="RequirementsGroup" /> that should be transfered
        /// </summary>
        ReactiveList<RequirementsGroup> SelectedGroupsForTransfer { get; }

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
        void Map(List<IMappedElementRowViewModel> elements);

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
        /// <returns>The collection of premapped <see cref="IMappedElementRowViewModel" /></returns>
        List<IMappedElementRowViewModel> PreMap(List<IMappedElementRowViewModel> elements);

        /// <summary>
        /// Transfers the mapped variables to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        Task TransferMappedThingsToHub();
    }
}
