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
    using DEHEASysML.Forms;
    using DEHEASysML.ViewModel;

    using DEHPCommon.HubController.Interfaces;
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
        /// The <see cref="IHubController" />
        /// </summary>
        private readonly IHubController hubController;

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
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel" /></param>
        public Dispatcher(IHubController hubController, IStatusBarControlViewModel statusBar)
        {
            this.hubController = hubController;
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

            this.hubController.Close();
        }
    }
}
