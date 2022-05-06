// --------------------------------------------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHEASysML.DstController;
    using DEHEASysML.Events;
    using DEHEASysML.ViewModel.Dialogs.Interfaces;
    using DEHEASysML.ViewModel.Interfaces;
    using DEHEASysML.ViewModel.RequirementsBrowser;
    using DEHEASysML.Views;
    using DEHEASysML.Views.Dialogs;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.PublicationBrowser;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;
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
        /// Backing field for <see cref="IsBusy" />
        /// </summary>
        private bool? isBusy;

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
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

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
        /// <param name="requirementsBrowser">The <see cref="IRequirementsBrowserViewModel" /></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public HubPanelViewModel(INavigationService navigationService, IHubController hubController, IHubSessionControlViewModel sessionControl,
            IHubBrowserHeaderViewModel hubBrowserHeader, IPublicationBrowserViewModel publicationBrowser,
            IObjectBrowserViewModel objectBrowser, IStatusBarControlViewModel statusBar, IRequirementsBrowserViewModel requirementsBrowser, IDstController dstController)
        {
            this.NavigationService = navigationService;
            this.hubController = hubController;
            this.SessionControl = sessionControl;
            this.HubBrowserHeader = hubBrowserHeader;
            this.PublicationBrowser = publicationBrowser;
            this.ObjectBrowser = objectBrowser;
            this.StatusBar = statusBar;
            this.RequirementsBrowser = requirementsBrowser;
            this.dstController = dstController;
            this.InitializeCommandsAndObservables();
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
        /// Asserts if this view model is busy or not
        /// </summary>
        public bool? IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// <see cref="ReactiveCommand{T}" /> for connecting to a data source
        /// </summary>
        public ReactiveCommand<object> ConnectCommand { get; set; }

        /// <summary>
        /// <see cref="ReactiveCommand"/> for mapping the topElement of the model
        /// </summary>
        public ReactiveCommand<object> MapTopElementCommand { get; set; }

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
        /// Gets or set the <see cref="RequirementsBrowser" />
        /// </summary>
        public IRequirementsBrowserViewModel RequirementsBrowser { get; set; }

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
        /// Initializes all <see cref="ReactiveCommand{T}" /> and <see cref="Observable"/>
        /// </summary>
        private void InitializeCommandsAndObservables()
        {
            this.ConnectCommand = ReactiveCommand.Create(null, RxApp.MainThreadScheduler);
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());

            this.WhenAnyValue(x => x.hubController.IsSessionOpen)
                .Subscribe(this.UpdateStatusBar);

            this.WhenAny(x => x.hubController.OpenIteration,
                    x => x.hubController.IsSessionOpen,
                    (i, o) => i.Value != null && o.Value)
                .Subscribe(this.UpdateConnectButtonText);

            this.WhenAnyValue(x => x.ObjectBrowser.IsBusy, 
                x => x.RequirementsBrowser.IsBusy).Subscribe(_ => this.UpdateIsBusy());

            var canMap = this.ObjectBrowser.CanMap.Merge(this.WhenAny(
                x => x.dstController.IsFileOpen,
                (s) => s.Value));

            this.ObjectBrowser.MapCommand = ReactiveCommand.Create(canMap);
            this.ObjectBrowser.MapCommand.Subscribe(_ => this.MapCommandObjectExecute());

            this.RequirementsBrowser.MapCommand = ReactiveCommand.Create(canMap);
            this.RequirementsBrowser.MapCommand.Subscribe(_ => this.MapCommandRequirementsExecute());

            this.ObjectBrowser.ContextMenu.IsEmptyChanged.Where(x => !x).Subscribe(_ => this.AddMapTopElementCommand());

            this.MapTopElementCommand = ReactiveCommand.Create(canMap);
            this.MapTopElementCommand.Subscribe(_ => this.MapTopElementCommandExecute());
        }

        /// <summary>
        /// Adds the <see cref="MapTopElementCommand"/> to the <see cref="IObjectBrowserViewModel.ContextMenu"/>
        /// </summary>
        private void AddMapTopElementCommand()
        {
            this.ObjectBrowser.ContextMenu.Add(new ContextMenuItemViewModel("Map Top Element", "", this.MapTopElementCommand
                , MenuItemKind.Edit, ClassKind.NotThing));
        }

        /// <summary>
        /// Maps the Top Element of the model
        /// </summary>
        private void MapTopElementCommandExecute()
        {
            this.IsBusy = true;

            if (this.hubController.OpenIteration.TopElement != null)
            {
                this.OpenHubDialog(new List<Thing>(){this.hubController.OpenIteration.TopElement});
            }
        }

        /// <summary>
        /// Executes the <see cref="IRequirementsBrowserViewModel.MapCommand" />
        /// </summary>
        private void MapCommandRequirementsExecute()
        {
            this.IsBusy = true;
            var requirements = this.RetrieveAllSelectedRequirements();
            this.OpenHubDialog(new List<Thing>(requirements));
        }

        /// <summary>
        /// Gets the collection of all selected <see cref="Requirement"/>
        /// This collection include requirement included inside selected <see cref="RequirementsContainer"/>
        /// </summary>
        /// <returns>A collection of <see cref="Requirement"/></returns>
        private IEnumerable<Requirement> RetrieveAllSelectedRequirements()
        {
            var requirements = new List<Requirement>();

            foreach (var selectedThing in this.RequirementsBrowser.SelectedThings)
            {
                switch (selectedThing)
                {
                    case RequirementsSpecificationRowViewModel requirementsSpecificationRow:
                        requirements.AddRange(requirementsSpecificationRow.Thing.Requirement.Where(x => !x.IsDeprecated));
                        break;
                    case RequirementRowViewModel requirementRow when !requirementRow.Thing.IsDeprecated:
                        requirements.Add(requirementRow.Thing);
                        break;
                    case RequirementsGroupRowViewModel requirementsGroupRow:
                        requirements.AddRange(requirementsGroupRow.GetAllRequirementsChildren());
                        break;
                }
            }

            return requirements.Distinct();
        }

        /// <summary>
        /// Executes the <see cref="IObjectBrowserViewModel.MapCommand" />
        /// </summary>
        private void MapCommandObjectExecute()
        {
            this.IsBusy = true;

            var elementDefinitions = this.ObjectBrowser.SelectedThings
                .OfType<ElementDefinitionRowViewModel>().Select(x => x.Thing);

            this.OpenHubDialog(new List<Thing>(elementDefinitions));
        }

        /// <summary>
        /// Opens the <see cref="HubMappingConfigurationDialog"/>
        /// </summary>
        /// <param name="things">A collection of selected <see cref="Thing"/></param>
        private void OpenHubDialog(List<Thing> things)
        {
            if (things.Any())
            {
                var viewModel = AppContainer.Container.Resolve<IHubMappingConfigurationDialogViewModel>();
                viewModel.Initialize(things);
                this.IsBusy = false;
                this.NavigationService.ShowDialog<HubMappingConfigurationDialog, IHubMappingConfigurationDialogViewModel>(viewModel);

                if (this.dstController.HubMapResult.Any())
                {
                    CDPMessageBus.Current.SendMessage(new UpdateDstNetChangePreview());
                }
            }

            this.IsBusy = false;
        }

        /// <summary>
        /// Update the <see cref="IsBusy"/> property
        /// </summary>
        private void UpdateIsBusy()
        {
            var requirementsBrowserIsBusy = this.RequirementsBrowser.IsBusy;
            var objectBrowserIsBusy = this.ObjectBrowser.IsBusy;

            this.IsBusy = objectBrowserIsBusy != null && requirementsBrowserIsBusy != null
                                                      && (objectBrowserIsBusy.Value || requirementsBrowserIsBusy.Value);
        }
    }
}
