// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectObjectBrowserViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Interfaces;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows;

    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="EnterpriseArchitectObjectBrowserViewModel" /> is the view model for the Enterprise Architect object
    /// browser
    /// </summary>
    public class EnterpriseArchitectObjectBrowserViewModel : ReactiveObject, IEnterpriseArchitectObjectBrowserViewModel, IHaveContextMenuViewModel
    {
        /// <summary>
        /// Backing field for <see cref="IsBusy" />
        /// </summary>
        private bool? isBusy;

        /// <summary>
        /// Backing field for the <see cref="SelectedThing" />
        /// </summary>
        private object selectedThing;

        /// <summary>
        /// Initializes a new <see cref="EnterpriseArchitectObjectBrowserViewModel" />
        /// </summary>
        public EnterpriseArchitectObjectBrowserViewModel()
        {
            this.Caption = "Enterprise Architect Object Browser";
            this.ToolTip = "This Object Browser displays the Enterprise Architect objects";
            this.PopulateContextMenu();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the browser is busy
        /// </summary>
        public bool? IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// Gets or sets the selected thing
        /// </summary>
        public object SelectedThing
        {
            get => this.selectedThing;
            set => this.RaiseAndSetIfChanged(ref this.selectedThing, value);
        }

        /// <summary>
        /// Gets or sets the selected things collection
        /// </summary>
        public ReactiveList<object> SelectedThings { get; set; } = new();

        /// <summary>
        /// Gets the collection of <see cref="EnterpriseArchitectObjectBaseRowViewModel" /> to be displayed in the tree
        /// </summary>
        public ReactiveList<EnterpriseArchitectObjectBaseRowViewModel> Things { get; } = new();

        /// <summary>
        /// Gets the Context Menu for the implementing view model
        /// </summary>
        public ReactiveList<ContextMenuItemViewModel> ContextMenu { get; } = new();

        /// <summary>
        /// Gets the command that allows to map the selected things
        /// </summary>
        public ReactiveCommand<object> MapCommand { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IObservable{T}" /> of <see cref="bool" /> that is bound to the
        /// <see cref="IObjectBrowserViewModel.MapCommand" /> <see cref="ReactiveCommand{T}.CanExecute" /> property
        /// </summary>
        /// <remarks>This observable is intended to be Merged with another observable</remarks>
        public IObservable<bool> CanMap { get; set; }

        /// <summary>
        /// Gets the Caption of the control
        /// </summary>
        public string Caption { get; private set; }

        /// <summary>
        /// Gets the tooltip of the control
        /// </summary>
        public string ToolTip { get; private set; }

        /// <summary>
        /// Build the tree to display the given <see cref="Element"/>
        /// </summary>
        /// <param name="models">The collection of <see cref="Package" /> that represents Model</param>
        /// <param name="elements">The collection of <see cref="Element"/> to display</param>
        /// <param name="packagesId">The Id of <see cref="Package" /> to display</param>
        public void BuildTree(IEnumerable<Package> models, IEnumerable<Element> elements, IEnumerable<int> packagesId)
        {
            var visibleElements = elements.ToList();
            var packagesIdList = packagesId.ToList();

            foreach (var repositoryModel in models.Where(x => packagesIdList.Contains(x.PackageID)))
            {
                this.Things.Add(new ModelRowViewModel(repositoryModel, visibleElements, packagesIdList));
            }
        }

        /// <summary>
        /// Populate the context menu for the implementing view model
        /// </summary>
        public void PopulateContextMenu()
        {
            this.ContextMenu.Clear();
        }
    }
}
