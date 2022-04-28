// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementsBrowserViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.RequirementsBrowser
{
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    /// <summary>
    /// The <see cref="RequirementsBrowserViewModel" /> is responsible for managing the Requirements defined
    /// </summary>
    public class RequirementsBrowserViewModel : ObjectBrowserBaseViewModel, IRequirementsBrowserViewModel
    {
        /// <summary>
        /// The <see cref="IObjectBrowserTreeSelectorService" />
        /// </summary>
        private readonly IObjectBrowserTreeSelectorService objectBrowserTreeSelectorService;

        /// <summary>
        /// Initialize a new <see cref="RequirementsBrowserViewModel" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="objectBrowserTreeSelectorService">The <see cref="IObjectBrowserTreeSelectorService" /></param>
        public RequirementsBrowserViewModel(IHubController hubController, IObjectBrowserTreeSelectorService objectBrowserTreeSelectorService) :
            base(hubController, objectBrowserTreeSelectorService)
        {
            this.objectBrowserTreeSelectorService = objectBrowserTreeSelectorService;
            this.Caption = "Hub Requirements Browser";
        }

        /// <summary>
        /// Gets the Caption of the control
        /// </summary>
        public new string Caption { get; private set; }

        /// <summary>
        /// Adds to the <see cref="ObjectBrowserBaseViewModel.Things" /> collection the specified by
        /// <see cref="IObjectBrowserTreeSelectorService" /> trees
        /// </summary>
        /// <param name="iteration">An optional <see cref="Iteration" /> to use for generation of the trees</param>
        public override void BuildTrees(Iteration iteration = null)
        {
            foreach (var thingKind in this.objectBrowserTreeSelectorService.ThingKinds)
            {
                if (thingKind == typeof(RequirementsSpecification) &&
                    this.Things.OfType<IBrowserViewModelBase<Thing>>().All(x => x.Thing.Iid != this.HubController.OpenIteration.Iid))
                {
                    this.Things.Add(new IterationRequirementsViewModel(iteration ?? this.HubController.OpenIteration, this.HubController.Session));
                }
            }

            if (this.Things.FirstOrDefault() is { } firstNode)
            {
                firstNode.IsExpanded = true;
            }
        }
    }
}
