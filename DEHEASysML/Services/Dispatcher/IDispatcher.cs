// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDispatcher.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Services.Dispatcher
{
    using EA;

    /// <summary>
    /// Interface definition for <see cref="Dispatcher" />
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Asserts that the mapping is available
        /// </summary>
        bool CanMap { get; set; }

        /// <summary>
        /// Handle the connection to EA
        /// </summary>
        /// <param name="repository">The current <see cref="Repository" /></param>
        void Connect(Repository repository);

        /// <summary>
        /// Show the Hub Panel to the user
        /// </summary>
        void ShowHubPanel();

        /// <summary>
        /// Handle the disconnetion to EA
        /// </summary>
        void Disconnect();

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
        /// Handle the OnPostInitialized event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        void OnPostInitiliazed(Repository repository);

        /// <summary>
        /// Handle the execution of the map selected elements command
        /// </summary>
        /// <param name="repository">The working <see cref="Repository"/></param>
        void MapSelectedElementsCommand(Repository repository);

        /// <summary>
        /// Handle the execution of the map selected package command
        /// </summary>
        /// <param name="repository">The working <see cref="Repository"/></param>
        void MapSelectedPackageCommand(Repository repository);

        /// <summary>
        /// Show the Impact Panel to the user
        /// </summary>
        void ShowImpactPanel();

        /// <summary>
        /// Open the Transfer History dialog
        /// </summary>
        void OpenTransferHistory();
    }
}
