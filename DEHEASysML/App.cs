﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.cs" company="RHEA System S.A.">
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

namespace DEHEASysML
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    using Autofac;

    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.Services.Cache;
    using DEHEASysML.Services.Dispatcher;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Services.Selection;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.Dialogs;
    using DEHEASysML.ViewModel.Dialogs.Interfaces;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Interfaces;
    using DEHEASysML.ViewModel.Interfaces;
    using DEHEASysML.ViewModel.NetChangePreview;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;
    using DEHEASysML.ViewModel.RequirementsBrowser;

    using DEHPCommon;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.AdapterVersionService;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using NLog;

    using File = System.IO.File;

    /// <summary>
    /// The entry point of the EA AddIn
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class App
    {
        /// <summary>
        /// The name of the Menu Header
        /// </summary>
        private const string MenuHeader = "-&COMET";

        /// <summary>
        /// The name of the Hub Panel Menu
        /// </summary>
        private const string HubPanelMenu = "&Open Hub Panel";

        /// <summary>
        /// The name of the Impact Panel Menu
        /// </summary>
        private const string ImpactPanelMenu = "&Open Impact Panel";

        /// <summary>
        /// The name of the Mapping List Panel Menu
        /// </summary>
        private const string MappingListPanelMenu = "&Open Mapping List Panel";

        /// <summary>
        /// The name of the Map Selected Elements command
        /// </summary>
        private const string MapSelectedElementsMenu = "&Map selected element(s)";

        /// <summary>
        /// The name of the Transfer History command
        /// </summary>
        private const string TransferHistoryMenu = "&Open Transfer History";

        /// <summary>
        /// The name of the Map Selected Package command
        /// </summary>
        private const string MapSelectedPackage = "&Map all objects contained in the package";

        /// <summary>
        /// The name of the Ribbon Category
        /// </summary>
        private const string RibbonCategoryName = "Publish";

        /// <summary>
        /// The <see cref="NLog" /> logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IDispatcher" />
        /// </summary>
        private IDispatcher dispatcher;

        /// <summary>
        /// Initializes a new <see cref="App" />
        /// </summary>
        public App()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
            AppDomain.CurrentDomain.AssemblyResolve += this.CurrentDomainOnAssemblyResolve;
            LogManager.LoadConfiguration(Path.Combine(Directory.GetCurrentDirectory(), "NLog.config"));
            LogManager.Configuration.Variables["basedir"] = Directory.GetCurrentDirectory();
            this.LogAppStart();
            var containerBuilder = new ContainerBuilder();
            this.RegisterTypes(containerBuilder);
            this.RegisterViewModels(containerBuilder);
            AppContainer.BuildContainer(containerBuilder);
        }

        /// <summary>
        /// Called before EA starts to check Add-In Exists, necessary for the Add-In to work
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void EA_Connect(Repository repository)
        {
            using var scope = AppContainer.Container.BeginLifetimeScope();
            this.dispatcher = scope.Resolve<IDispatcher>();
            this.dispatcher.Connect(repository);
            scope.Resolve<IAdapterVersionService>().CurrentAdapterVersion = Assembly.GetExecutingAssembly().GetName().Version;
            scope.Resolve<IObjectBrowserTreeSelectorService>().Add<RequirementsSpecification>();
        }

        /// <summary>
        /// Used by EA to identify the the Ribbon in which the Add-In should place its menu icon
        /// </summary>
        /// <param name="repository"></param>
        /// <returns>The Category where the AddIn is placed</returns>
        public string EA_GetRibbonCategory(Repository repository)
        {
            return RibbonCategoryName;
        }

        /// <summary>
        /// Called when user Clicks Add-Ins Menu item from within EA.
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="location">
        /// A string representing the part of the user interface that brought up the menu. This can be
        /// TreeView, MainMenu or Diagram.
        /// </param>
        /// <param name="menuName">
        /// The name of the parent menu for which sub-items are to be defined. In the case of the top-level
        /// menu this is an empty string.
        /// </param>
        /// <returns>The definition of the menu option</returns>
        public object EA_GetMenuItems(Repository repository, string location, string menuName)
        {
            switch (location)
            {
                case "MainMenu":
                    switch (menuName)
                    {
                        case "":
                            return MenuHeader;
                        case MenuHeader:
                            string[] subMenuItems = { HubPanelMenu, ImpactPanelMenu, MappingListPanelMenu, MapSelectedElementsMenu,"-", TransferHistoryMenu };
                            return subMenuItems;
                    }

                    break;
                case "TreeView":
                    switch (menuName)
                    {
                        case "":
                            return MenuHeader;
                        case MenuHeader:
                            string[] subMenuItems = { MapSelectedPackage };
                            return subMenuItems;
                    }

                    break;
            }

            return null;
        }

        /// <summary>
        /// EA_MenuClick events are received by an Add-In in response to user selection of a menu option.
        /// The event is raised when the user clicks on a particular menu option. When a user clicks on one of your non-parent menu
        /// options, your Add-In receives a <c>MenuClick</c> event.
        /// Notice that your code can directly access Enterprise Architect data and UI elements using CurrentRepository methods.
        /// </summary>
        /// <param name="repository">
        /// An EA.CurrentRepository object representing the currently open Enterprise Architect model. Poll its
        /// members to retrieve model data and user interface status information.
        /// </param>
        /// <param name="location">Not used</param>
        /// <param name="menuName">
        /// The name of the parent menu for which sub-items are to be defined.
        /// In the case of the top-level menu this is an empty string.
        /// </param>
        /// <param name="itemName">The name of the option actually clicked.</param>
        public void EA_MenuClick(Repository repository, string location, string menuName, string itemName)
        {
            switch (itemName)
            {
                case HubPanelMenu:
                    this.dispatcher.ShowHubPanel();
                    break;
                case ImpactPanelMenu:
                    this.dispatcher.ShowImpactPanel();
                    break;
                case MapSelectedElementsMenu:
                    this.dispatcher.MapSelectedElementsCommand(repository);
                    break;
                case MapSelectedPackage:
                    this.dispatcher.MapSelectedPackageCommand(repository);
                    break;
                case TransferHistoryMenu:
                    this.dispatcher.OpenTransferHistory();
                    break;
                case MappingListPanelMenu:
                    this.dispatcher.ShowMappingListPanel();
                    break;
            }
        }

        /// <summary>
        /// Called once Menu has been opened to see what menu items should active.
        /// </summary>
        /// <param name="repository">the repository</param>
        /// <param name="location">the location of the menu</param>
        /// <param name="menuName">the name of the menu</param>
        /// <param name="itemName">the name of the menu item</param>
        /// <param name="isEnabled">boolean indicating whethe the menu item is enabled</param>
        /// <param name="isChecked">boolean indicating whether the menu is checked</param>
        public void EA_GetMenuState(Repository repository, string location, string menuName, string itemName, ref bool isEnabled, ref bool isChecked)
        {
            if (this.IsProjectOpen(repository))
            {
                isEnabled = itemName switch
                {
                    MapSelectedElementsMenu => this.dispatcher.CanMap && repository.GetTreeSelectedElements().Count > 0,
                    MapSelectedPackage => this.dispatcher.CanMap,
                    _ => true
                };
            }
            else
            {
                isEnabled = false;
            }
        }

        /// <summary>
        /// EA_OnPostInitialized notifies Add-Ins that the repository object has finished loading and any necessary initialization
        /// steps can now be performed on the object.
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void EA_OnPostInitialized(Repository repository)
        {
            this.dispatcher.OnPostInitiliazed(repository);
        }

        /// <summary>
        /// EA calls this operation on Exit
        /// </summary>
        public void EA_Disconnect()
        {
            this.dispatcher.Disconnect();
            AppDomain.CurrentDomain.AssemblyResolve -= this.CurrentDomainOnAssemblyResolve;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// The event occurs when the model being viewed by the Enterprise Architect user changes, for whatever reason (through
        /// user interaction or Add-In activity).
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void EA_FileOpen(Repository repository)
        {
            this.dispatcher.OnFileOpen(repository);
        }

        /// <summary>
        /// This event occurs when the model currently opened within Enterprise Architect is about to be closed (when another model
        /// is about to be opened
        /// or when Enterprise Architect is about to shutdown).
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void EA_FileClose(Repository repository)
        {
            this.dispatcher.OnFileClose(repository);
        }

        /// <summary>
        /// The event occurs when the model being viewed by the Enterprise Architect user changes, for whatever reason (through
        /// user interaction or Add-In activity).
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        public void EA_FileNew(Repository repository)
        {
            this.dispatcher.OnFileNew(repository);
        }

        /// <summary>
        /// This event occurs when a user drags a new Package from the Toolbox or Resources window onto a diagram,
        /// or by selecting the New Package icon from the Project Browser.
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="info">The <see cref="EventProperties"/></param>
        public bool EA_OnPostNewPackage(Repository repository, EventProperties info)
        {
            for(short propertiesIndex = 0; propertiesIndex < info.Count; propertiesIndex++)
            {
                this.dispatcher.OnNewPackage(repository, int.Parse((string)info.Get(propertiesIndex).Value));
            }

            return false;
        }

        /// <summary>
        /// This event occurs after a user has dragged a new element from the Toolbox or Resources window onto a diagram.
        /// The notification is provided immediately after the element is added to the model. 
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="info">The <see cref="EventProperties"/></param>
        public bool EA_OnPostNewElement(Repository repository, EventProperties info)
        {
            for (short propertiesIndex = 0; propertiesIndex < info.Count; propertiesIndex++)
            {
                this.dispatcher.OnNewElement(repository, int.Parse((string)info.Get(propertiesIndex).Value));
            }

            return false;
        }

        /// <summary>
        /// This event occurs when a user deletes an element from the Project Browser or on a diagram.
        /// The notification is provided immediately before the element is deleted, so that the Add-In can disable deletion of the element.
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="info">The <see cref="EventProperties"/></param>
        public bool EA_OnPreDeleteElement(Repository repository, EventProperties info)
        {
            for (short propertiesIndex = 0; propertiesIndex < info.Count; propertiesIndex++)
            {
                this.dispatcher.OnDeleteElement(repository, int.Parse((string)info.Get(propertiesIndex).Value));
            }

            return true;
        }

        /// <summary>
        /// This event occurs when a user attempts to permanently delete a Package from the Project Browser.
        /// The notification is provided immediately before the Package is deleted, so that the Add-In can disable deletion of the Package.
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="info">The <see cref="EventProperties"/></param>
        public bool EA_OnPreDeletePackage(Repository repository, EventProperties info)
        {
            for (short propertiesIndex = 0; propertiesIndex < info.Count; propertiesIndex++)
            {
                this.dispatcher.OnDeletePackage(repository, int.Parse((string)info.Get(propertiesIndex).Value));
            }

            return true;
        }

        /// <summary>
        /// This event occurs when a user has modified the context item. Add-Ins that require knowledge of when an item has been
        /// modified can subscribe to this broadcast function.
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <param name="guid">The guid of the Item</param>
        /// <param name="objectType">The <see cref="ObjectType" /> of the item</param>
        public void EA_OnNotifyContextItemModified(Repository repository, string guid, ObjectType objectType)
        {
            this.dispatcher.OnNotifyContextItemModified(repository, guid, objectType);
        }

        /// <summary>
        /// This event occurs after a user has selected an item anywhere in the Enterprise Architect GUI.
        /// Add-Ins that require knowledge of the current item in context can subscribe to this broadcast function.
        /// If ot = otRepository, then this function behaves in the same way as EA_FileOpen.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/></param>
        /// <param name="guid">Contains the GUID of the new context item</param>
        /// <param name="objectType">The <see cref="ObjectType"/></param>
        public void EA_OnContextItemChanged(Repository repository, string guid, ObjectType objectType)
        {
            this.dispatcher.OnContextItemChanged(repository, guid, objectType);
        }

        /// <summary>
        /// Asserts that a project is opened
        /// </summary>
        /// <param name="repository">The <see cref="Repository" /></param>
        /// <returns>True if a project is opened</returns>
        private bool IsProjectOpen(Repository repository)
        {
            try
            {
                var _ = repository.Models;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Occures when <see cref="AppDomain.AssemblyResolve" /> event is called
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="args">The event args</param>
        /// <returns>The assembly</returns>
        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrEmpty(folderPath))
            {
                return null;
            }

            var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");

            return !File.Exists(assemblyPath) ? null : Assembly.LoadFrom(assemblyPath);
        }

        /// <summary>
        /// Add a header to the log file
        /// </summary>
        private void LogAppStart()
        {
            this.logger.Info("-----------------------------------------------------------------------------------------");
            this.logger.Info($"Starting Enterprise Architect Plugin {Assembly.GetExecutingAssembly().GetName().Version}");
            this.logger.Info("-----------------------------------------------------------------------------------------");
        }

        /// <summary>
        /// Registers the types that can be resolved by the <see cref="IContainer" />
        /// </summary>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder" /></param>
        private void RegisterTypes(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<Dispatcher>().As<IDispatcher>().SingleInstance();
            containerBuilder.RegisterType<DstController.DstController>().As<IDstController>().SingleInstance();
            containerBuilder.RegisterType<MappingEngine>().As<IMappingEngine>().WithParameter(MappingEngine.ParameterName, Assembly.GetExecutingAssembly());
            containerBuilder.RegisterType<MappingConfigurationService>().As<IMappingConfigurationService>().SingleInstance();
            containerBuilder.RegisterType<SelectionService>().As<ISelectionService>().SingleInstance();
            containerBuilder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
        }

        /// <summary>
        /// Registers the view models types that can be resolved by the <see cref="IContainer" />
        /// </summary>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder" /></param>
        private void RegisterViewModels(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<HubPanelViewModel>().As<IHubPanelViewModel>().SingleInstance();
            containerBuilder.RegisterType<EnterpriseArchitectStatusBarControlViewModel>().As<IStatusBarControlViewModel>().SingleInstance();
            containerBuilder.RegisterType<RequirementsBrowserViewModel>().As<IRequirementsBrowserViewModel>();
            containerBuilder.RegisterType<DstMappingConfigurationDialogViewModel>().As<IDstMappingConfigurationDialogViewModel>().SingleInstance();
            containerBuilder.RegisterType<EnterpriseArchitectObjectBrowserViewModel>().As<IEnterpriseArchitectObjectBrowserViewModel>();
            containerBuilder.RegisterType<ImpactPanelViewModel>().As<IImpactPanelViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstNetChangePreviewViewModel>().As<IDstNetChangePreviewViewModel>().SingleInstance();
            containerBuilder.RegisterType<HubNetChangePreviewViewModel>().As<IHubNetChangePreviewViewModel>().SingleInstance();
            containerBuilder.RegisterType<HubObjectNetChangePreviewViewModel>().As<IHubObjectNetChangePreviewViewModel>().SingleInstance();
            containerBuilder.RegisterType<HubRequirementsNetChangePreviewViewModel>().As<IHubRequirementsNetChangePreviewViewModel>().SingleInstance();
            containerBuilder.RegisterType<EnterpriseArchitectTransferControlViewModel>().As<ITransferControlViewModel>().SingleInstance();
            containerBuilder.RegisterType<MappingConfigurationServiceDialogViewModel>().As<IMappingConfigurationServiceDialogViewModel>();
            containerBuilder.RegisterType<HubMappingConfigurationDialogViewModel>().As<IHubMappingConfigurationDialogViewModel>();
            containerBuilder.RegisterType<MappingListPanelViewModel>().As<IMappingListPanelViewModel>().SingleInstance();
        }
    }
}
