// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Dispatcher.cs" company="RHEA System S.A.">
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
    using DEHEASysML.DstController;
    using DEHEASysML.Forms;
    using DEHEASysML.ViewModel;

    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    /// <summary>
    /// Handles the behavior for each EA Events
    /// </summary>
    public class Dispatcher : IDispatcher
    {
        /// <summary>
        /// The name of the <see cref="HubPanelControl" /> inside EA
        /// </summary>
        private const string HubPanelName = "Hub Panel";

        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="Repository" />
        /// </summary>
        private Repository currentRepository;

        /// <summary>
        /// Asserts if the <see cref="HubPanelControl" /> has been created
        /// </summary>
        private bool hubPanelControlCreated;

        /// <summary>
        /// Initializes a new <see cref="Dispatcher" />
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel" /></param>
        public Dispatcher(IDstController dstController, IStatusBarControlViewModel statusBar)
        {
            this.dstController = dstController;
            this.StatusBar = statusBar;
        }

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel" />
        /// </summary>
        public IStatusBarControlViewModel StatusBar { get; set; }

        /// <summary>
        /// Handle the connection to EA
        /// </summary>
        /// <param name="repository">The current <see cref="Repository" /></param>
        public void Connect(Repository repository)
        {
            this.currentRepository = repository;
            this.currentRepository.HideAddinWindow();
            this.currentRepository.RemoveWindow(HubPanelName);
            this.dstController.Connect(repository);
        }

        /// <summary>
        /// Show the Hub Panel to the user
        /// </summary>
        public void ShowHubPanel()
        {
            if (this.StatusBar is EnterpriseArchitectStatusBarControlViewModel enterpriseArchitectStatusBar)
            {
                enterpriseArchitectStatusBar.Initialize(this.currentRepository);
            }

            if (!this.hubPanelControlCreated)
            {
                this.currentRepository.AddWindow(HubPanelName, typeof(HubPanelControl).ToString());
                this.hubPanelControlCreated = true;
            }

            this.currentRepository.ShowAddinWindow(HubPanelName);
        }

        /// <summary>
        /// Handle the disconnection to EA
        /// </summary>
        public void Disconnect()
        {
            this.currentRepository.RemoveWindow(HubPanelName);
            this.currentRepository.HideAddinWindow();

            if (this.StatusBar is EnterpriseArchitectStatusBarControlViewModel enterpriseArchitectStatusBar)
            {
                enterpriseArchitectStatusBar.Clear();
            }

            this.dstController.Disconnect();
        }

        /// <summary>
        /// Handle the FileOpen event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnFileOpen(Repository repository)
        {
            this.dstController.OnFileOpen(repository);
        }

        /// <summary>
        /// Handle the FileClose event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnFileClose(Repository repository)
        {
            this.dstController.OnFileClose(repository);
        }

        /// <summary>
        /// Handle the FileNew event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnFileNew(Repository repository)
        {
            this.dstController.OnFileNew(repository);
        }

        /// <summary>
        /// Handle the OnNotifyContextItemModified event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="guid">The guid of the Item</param>
        /// <param name="objectType">The <see cref="ObjectType" /> of the item</param>
        public void OnNotifyContextItemModified(Repository repository, string guid, ObjectType objectType)
        {
            this.dstController.OnNotifyContextItemModified(repository, guid, objectType);
        }
    }
}
