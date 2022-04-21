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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;

    using DEHEASysML.DstController;
    using DEHEASysML.Forms;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.Dialogs.Interfaces;
    using DEHEASysML.Views.Dialogs;

    using DEHPCommon;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using ReactiveUI;

    /// <summary>
    /// Handles the behavior for each EA Events
    /// </summary>
    public class Dispatcher : ReactiveObject, IDispatcher
    {
        /// <summary>
        /// The name of the <see cref="HubPanelControl" /> inside EA
        /// </summary>
        private const string HubPanelName = "Hub Panel";

        /// <summary>
        /// The name of the <see cref="ImpactPanelControl" /> inside EA
        /// </summary>
        private const string ImpactPanelName = "Impact Panel";

        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="INavigationService" />
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// The <see cref="Repository" />
        /// </summary>
        private Repository currentRepository;

        /// <summary>
        /// Backing field for <see cref="CanMap" />
        /// </summary>
        private bool canMap;

        /// <summary>
        /// Initializes a new <see cref="Dispatcher" />
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel" /></param>
        /// <param name="navigationService">The <see cref="INavigationService" /></param>
        public Dispatcher(IDstController dstController, IStatusBarControlViewModel statusBar, INavigationService navigationService)
        {
            this.dstController = dstController;
            this.StatusBar = statusBar;
            this.navigationService = navigationService;

            this.dstController.WhenAnyValue(x => x.CanMap).Subscribe(this.UpdateCanMap);
        }

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel" />
        /// </summary>
        public IStatusBarControlViewModel StatusBar { get; set; }

        /// <summary>
        /// Asserts that the mapping is available
        /// </summary>
        public bool CanMap
        {
            get => this.canMap;
            set => this.RaiseAndSetIfChanged(ref this.canMap, value);
        }

        /// <summary>
        /// Handle the connection to EA
        /// </summary>
        /// <param name="repository">The current <see cref="Repository" /></param>
        public void Connect(Repository repository)
        {
            this.currentRepository = repository;
            this.dstController.Connect(repository);
        }

        /// <summary>
        /// Show the Hub Panel to the user
        /// </summary>
        public void ShowHubPanel()
        {
            this.HandleTabVisibility(HubPanelName, typeof(HubPanelControl).ToString());
        }

        /// <summary>
        /// Show the Impact Panel to the user
        /// </summary>
        public void ShowImpactPanel()
        {
            this.HandleTabVisibility(ImpactPanelName, typeof(ImpactPanelControl).ToString());
        }

        /// <summary>
        /// Handle the OnPostInitialized event from EA
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void OnPostInitiliazed(Repository repository)
        {
            this.currentRepository.HideAddinWindow();
        }

        /// <summary>
        /// Handle the disconnection to EA
        /// </summary>
        public void Disconnect()
        {
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

        /// <summary>
        /// Handle the execution of the map selected <see cref="Element" />s command
        /// </summary>
        /// <param name="repository">The working <see cref="Repository" /></param>
        public void MapSelectedElementsCommand(Repository repository)
        {
            var elements = this.dstController.GetAllSelectedElements(repository);
            this.OpenMappingDialog(elements);
        }

        /// <summary>
        /// Handle the execution of the map selected package command
        /// </summary>
        /// <param name="repository">The working <see cref="Repository" /></param>
        public void MapSelectedPackageCommand(Repository repository)
        {
            var elements = this.dstController.GetAllElementsInsidePackage(repository);
            this.OpenMappingDialog(elements);
        }

        /// <summary>
        /// Handle the visibility of a Tab inside EA
        /// </summary>
        /// <param name="tabName">The name of the tab</param>
        /// <param name="controlId">The id of the ActiveX Control</param>
        private void HandleTabVisibility(string tabName, string controlId)
        {
            this.InitializeStatusBar();

            switch (this.currentRepository.IsTabOpen(tabName))
            {
                case 0:
                    this.currentRepository.AddTab(tabName, controlId);
                    break;
                case 1:
                    this.currentRepository.ActivateTab(tabName);
                    break;
            }
        }

        /// <summary>
        /// Initializes the <see cref="IStatusBarControlViewModel" />
        /// </summary>
        private void InitializeStatusBar()
        {
            if (this.StatusBar is EnterpriseArchitectStatusBarControlViewModel enterpriseArchitectStatusBar)
            {
                enterpriseArchitectStatusBar.Initialize(this.currentRepository);
            }
        }

        /// <summary>
        /// Opens the <see cref="DstMappingConfigurationDialog" /> and initializes it
        /// </summary>
        /// <param name="elements">The <see cref="Element" /> to display</param>
        private void OpenMappingDialog(IEnumerable<Element> elements)
        {
            var elementsList = elements.ToList();
            var viewModel = AppContainer.Container.Resolve<IDstMappingConfigurationDialogViewModel>();
            viewModel.Initialize(elementsList, this.dstController.RetrieveAllParentsIdPackage(elementsList));
            this.navigationService.ShowDialog<DstMappingConfigurationDialog, IDstMappingConfigurationDialogViewModel>(viewModel);
        }

        /// <summary>
        /// Updates the value of <see cref="CanMap" />
        /// </summary>
        /// <param name="newCanMapValue">The new value</param>
        private void UpdateCanMap(bool newCanMapValue)
        {
            this.CanMap = newCanMapValue;
        }
    }
}
