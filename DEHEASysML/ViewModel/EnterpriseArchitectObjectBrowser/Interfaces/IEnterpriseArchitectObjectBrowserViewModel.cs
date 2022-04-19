// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEnterpriseArchitectObjectBrowserViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Interfaces
{
    using System;
    using System.Collections.Generic;

    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows;

    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for <see cref="EnterpriseArchitectObjectBrowserViewModel" />
    /// </summary>
    public interface IEnterpriseArchitectObjectBrowserViewModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether the browser is busy
        /// </summary>
        bool? IsBusy { get; set; }

        /// <summary>
        /// Gets or sets the selected thing
        /// </summary>
        public object SelectedThing { get; set; }

        /// <summary>
        /// Gets or sets the selected things collection
        /// </summary>
        ReactiveList<object> SelectedThings { get; set; }

        /// <summary>
        /// Gets the command that allows to map the selected things
        /// </summary>
        ReactiveCommand<object> MapCommand { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IObservable{T}" /> of <see cref="bool" /> that is bound to the
        /// <see cref="IObjectBrowserViewModel.MapCommand" /> <see cref="ReactiveCommand{T}.CanExecute" /> property
        /// </summary>
        /// <remarks>This observable is intended to be Merged with another observable</remarks>
        IObservable<bool> CanMap { get; set; }

        /// <summary>
        /// Gets the Caption of the control
        /// </summary>
        string Caption { get; }

        /// <summary>
        /// Gets the tooltip of the control
        /// </summary>
        string ToolTip { get; }

        /// <summary>
        /// Gets the collection of <see cref="EnterpriseArchitectObjectBaseRowViewModel" /> to be displayed in the tree
        /// </summary>
        ReactiveList<EnterpriseArchitectObjectBaseRowViewModel> Things { get; }

        /// <summary>
        /// Build the tree to display the given <see cref="Element" />
        /// </summary>
        /// <param name="models">The collection of <see cref="Package" /> that represents Model</param>
        /// <param name="elements">The collection of <see cref="Element" /> to display</param>
        /// <param name="packagesId">The Id of <see cref="Package" /> to display</param>
        void BuildTree(IEnumerable<Package> models, IEnumerable<Element> elements, IEnumerable<int> packagesId);
    }
}
