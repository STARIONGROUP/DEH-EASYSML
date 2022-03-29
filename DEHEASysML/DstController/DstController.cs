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
    using System.Collections.Generic;
    using System.Linq;

    using DEHPCommon.HubController.Interfaces;

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
        /// Gets the current class logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Backing field for <see cref="CurrentRepository" />
        /// </summary>
        private Repository currentRepository;

        /// <summary>
        /// Initializes a new <see cref="DstController" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        public DstController(IHubController hubController)
        {
            this.hubController = hubController;
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
            return this.GetElementsFromPackage(model, "Requirement");
        }

        /// <summary>
        /// Gets all blocks present inside a model
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>A collection of <see cref="Element" /> representing block</returns>
        public List<Element> GetAllBlocks(IDualPackage model)
        {
            return this.GetElementsFromPackage(model, "block");
        }

        /// <summary>
        /// Gets all ValueTypes present inside a model
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>A collection of <see cref="Element" /> representing ValueType</returns>
        public List<Element> GetAllValueTypes(IDualPackage model)
        {
            return this.GetElementsFromPackage(model, "ValueType");
        }

        /// <summary>
        /// Gets all element of a given stereo presents inside a package, including sub packages
        /// </summary>
        /// <param name="package">The <see cref="IDualPackage" /></param>
        /// <param name="stereotype">The stereotype of the <see cref="Element" /></param>
        /// <returns>A collection of <see cref="Element" /></returns>
        private List<Element> GetElementsFromPackage(IDualPackage package, string stereotype)
        {
            var requirements = new List<Element>();
            requirements.AddRange(package.Elements.OfType<Element>().Where(x => x.Stereotype == stereotype));

            foreach (var subPackage in package.Packages.OfType<Package>())
            {
                requirements.AddRange(this.GetElementsFromPackage(subPackage, stereotype));
            }

            return requirements;
        }
    }
}
