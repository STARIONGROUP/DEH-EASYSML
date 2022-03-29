// --------------------------------------------------------------------------------------------------------------------
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

    using DEHEASysML.DstController;
    using DEHEASysML.Services.Dispatcher;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.Interfaces;

    using DEHPCommon;
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
        private const string MenuHeader = "-&DEHP";

        /// <summary>
        /// The name of the Hub Panel Menu
        /// </summary>
        private const string HubPanelMenu = "&Open/Close Hub Panel";

        /// <summary>
        /// The name of the Impact Panel Menu
        /// </summary>
        private const string ImpactPanelMenu = "&Impact Panel";

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
            if (location != "MainMenu")
            {
                return null;
            }

            switch (menuName)
            {
                case "":
                    return MenuHeader;
                case MenuHeader:
                    string[] subMenuItems = { HubPanelMenu, ImpactPanelMenu };
                    return subMenuItems;
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
            if (itemName == HubPanelMenu)
            {
                this.dispatcher.ShowHubPanel();
            }
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
        }

        /// <summary>
        /// Registers the view models types that can be resolved by the <see cref="IContainer" />
        /// </summary>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder" /></param>
        private void RegisterViewModels(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<HubPanelViewModel>().As<IHubPanelViewModel>().SingleInstance();
            containerBuilder.RegisterType<EnterpriseArchitectStatusBarControlViewModel>().As<IStatusBarControlViewModel>().SingleInstance();
        }
    }
}
