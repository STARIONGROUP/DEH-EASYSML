// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EAStatusBarControlViewModel.cs" company="RHEA System S.A.">
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

    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;

    using EA;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="EnterpriseArchitectStatusBarControlViewModel" /> is the main view  model of the status bar of this dst
    /// adapter
    /// </summary>
    public class EnterpriseArchitectStatusBarControlViewModel : StatusBarControlViewModel
    {
        /// <summary>
        /// The name of the Output Tab
        /// </summary>
        private const string TabName = "DEHP";

        /// <summary>
        /// The <see cref="Repository" />
        /// </summary>
        private Repository repository;

        /// <summary>
        /// Initiliaze a new <see cref="EnterpriseArchitectStatusBarControlViewModel" />
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService" /></param>
        public EnterpriseArchitectStatusBarControlViewModel(INavigationService navigationService) : base(navigationService)
        {
            this.WhenAnyValue(x => x.Message).Subscribe(this.Append);
        }

        /// <summary>
        /// Initialize the view model
        /// </summary>
        /// <param name="startRepository">The <see cref="Repository" /></param>
        public void Initialize(Repository startRepository)
        {
            this.repository = startRepository;
            this.repository.CreateOutputTab(TabName);
        }

        /// <summary>
        /// Appends a message into the Output Tab
        /// </summary>
        /// <param name="message">The message to display</param>
        public void Append(string message)
        {
            if (message == null || this.repository == null)
            {
                return;
            }

            this.repository.EnsureOutputVisible(TabName);
            this.repository.WriteOutput(TabName, message, 0);
        }

        /// <summary>
        /// Clear the output tab
        /// </summary>
        public void Clear()
        {
            this.repository.RemoveOutputTab(TabName);
        }

        /// <summary>
        /// Execute the <see cref="StatusBarControlViewModel.UserSettingCommand" />
        /// </summary>
        protected override void ExecuteUserSettingCommand()
        {
            this.Append("Execute User Setting Command");
        }
    }
}
