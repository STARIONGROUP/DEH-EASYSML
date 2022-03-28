﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubPanelViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel
{
    using System;

    using DEHEASysML.ViewModel.Interfaces;
    using DEHEASysML.Views;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.PublicationBrowser;
    using DEHPCommon.UserInterfaces.Views;

    using ReactiveUI;

    /// <summary>
    /// The view model for the <see cref="HubPanel" />
    /// </summary>
    public class HubPanelViewModel : ReactiveObject, IHubPanelViewModel
    {
        /// <summary>
        /// The connect text for the connect button
        /// </summary>
        private const string ConnectText = "Connect";

        /// <summary>
        /// The disconnect text for the connect button
        /// </summary>
        private const string DisconnectText = "Disconnect";

        /// <summary>
        /// The <see cref="IHubController" />
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// Gets the <see cref="INavigationService" />
        /// </summary>
        protected readonly INavigationService NavigationService;

        /// <summary>
        /// Determines if the <see cref="HubPanelViewModel" /> can appends message
        /// to the <see cref="IStatusBarControlViewModel" />
        /// </summary>
        private bool canLogToStatusBar;

        /// <summary>
        /// Backing field for <see cref="ConnectButtonText" />
        /// </summary>
        private string connectButtonText = ConnectText;

        /// <summary>
        /// Initializes a new <see cref="HubPanelViewModel" />
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService" /></param>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="sessionControl">The <see cref="IHubSessionControlViewModel" /></param>
        /// <param name="hubBrowserHeader">The <see cref="IHubBrowserHeaderViewModel" /></param>
        /// <param name="publicationBrowser">The <see cref="IPublicationBrowserViewModel" /></param>
        /// <param name="objectBrowser">The <see cref="IObjectBrowserViewModel" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel" /></param>
        public HubPanelViewModel(INavigationService navigationService, IHubController hubController, IHubSessionControlViewModel sessionControl,
            IHubBrowserHeaderViewModel hubBrowserHeader, IPublicationBrowserViewModel publicationBrowser,
            IObjectBrowserViewModel objectBrowser, IStatusBarControlViewModel statusBar)
        {
            this.NavigationService = navigationService;
            this.hubController = hubController;
            this.SessionControl = sessionControl;
            this.HubBrowserHeader = hubBrowserHeader;
            this.PublicationBrowser = publicationBrowser;
            this.ObjectBrowser = objectBrowser;
            this.StatusBar = statusBar;
            this.InitializeCommands();
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string ConnectButtonText
        {
            get => this.connectButtonText;
            set => this.RaiseAndSetIfChanged(ref this.connectButtonText, value);
        }

        /// <summary>
        /// <see cref="ReactiveCommand{T}" /> for connecting to a data source
        /// </summary>
        public ReactiveCommand<object> ConnectCommand { get; set; }

        /// <summary>
        /// The <see cref="IObjectBrowserViewModel" />
        /// </summary>
        public IObjectBrowserViewModel ObjectBrowser { get; set; }

        /// <summary>
        /// The <see cref="IPublicationBrowserViewModel" />
        /// </summary>
        public IPublicationBrowserViewModel PublicationBrowser { get; set; }

        /// <summary>
        /// The <see cref="IHubBrowserHeaderViewModel" />
        /// </summary>
        public IHubBrowserHeaderViewModel HubBrowserHeader { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IStatusBarControlViewModel" />
        /// </summary>
        public IStatusBarControlViewModel StatusBar { get; set; }

        /// <summary>
        /// Gets the <see cref="IHubSessionControlViewModel" />
        /// </summary>
        public IHubSessionControlViewModel SessionControl { get; set; }

        /// <summary>
        /// Append the connection status to the status bar
        /// </summary>
        /// <param name="isSessionOpen">Assert whether the status bar should update as connected or disconnected</param>
        public void UpdateStatusBar(bool isSessionOpen)
        {
            if (!this.canLogToStatusBar)
            {
                this.canLogToStatusBar = true;
                return;
            }

            var connectionStatus = isSessionOpen ? "Connection established to" : "Disconnected from";
            this.StatusBar.Append($"{connectionStatus} the hub");
        }

        /// <summary>
        /// Updates the <see cref="ConnectButtonText" />
        /// </summary>
        /// <param name="isSessionOpen">
        /// Assert whether the button text should be <see cref="ConnectText" /> or
        /// <see cref="DisconnectText" />
        /// </param>
        private void UpdateConnectButtonText(bool isSessionOpen)
        {
            this.ConnectButtonText = isSessionOpen ? DisconnectText : ConnectText;
        }

        /// <summary>
        /// Execute the <see cref="ConnectCommand" />
        /// </summary>
        private void ConnectCommandExecute()
        {
            if (this.hubController.IsSessionOpen)
            {
                this.ObjectBrowser.Things.Clear();
                this.hubController.Close();
            }
            else
            {
                this.NavigationService.ShowDialog<Login>();

                if (this.hubController.IsSessionOpen && this.hubController.OpenIteration == null)
                {
                    this.hubController.Close();
                }
            }
        }

        /// <summary>
        /// Initializes all <see cref="ReactiveCommand{T}" />
        /// </summary>
        private void InitializeCommands()
        {
            this.ConnectCommand = ReactiveCommand.Create(null, RxApp.MainThreadScheduler);
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());

            this.WhenAnyValue(x => x.hubController.IsSessionOpen)
                .Subscribe(this.UpdateStatusBar);

            this.WhenAny(x => x.hubController.OpenIteration,
                    x => x.hubController.IsSessionOpen,
                    (i, o) => i.Value != null && o.Value)
                .Subscribe(this.UpdateConnectButtonText);
        }
    }
}