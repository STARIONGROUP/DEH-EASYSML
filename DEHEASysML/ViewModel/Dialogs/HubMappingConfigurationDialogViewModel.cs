// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Dialogs.Interfaces;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Interfaces;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows;
    using DEHEASysML.ViewModel.RequirementsBrowser;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using EA;

    using ReactiveUI;

    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    /// <summary>
    /// This view model let the user configure the mapping from the hub to the dst source
    /// </summary>
    public class HubMappingConfigurationDialogViewModel : MappingConfigurationDialogViewModel, IHubMappingConfigurationDialogViewModel, IHaveContextMenuViewModel
    {
        /// <summary>
        /// A collection of <see cref="Thing" />
        /// </summary>
        private List<Thing> things;

        /// <summary>
        /// Backing field for <see cref="SelectedElement" />
        /// </summary>
        private Element selectedElement;

        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationDialogViewModel" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="enterpriseArchitectObject">The <see cref="IEnterpriseArchitectObjectBrowserViewModel" /></param>
        /// <param name="objectBrowser">The <see cref="IObjectBrowserViewModel" /></param>
        /// <param name="requirementsBrowser">The <see cref="IObjectBrowserViewModel" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public HubMappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController,
            IEnterpriseArchitectObjectBrowserViewModel enterpriseArchitectObject, IObjectBrowserViewModel objectBrowser,
            IRequirementsBrowserViewModel requirementsBrowser, IStatusBarControlViewModel statusBar) 
            : base(hubController, dstController, enterpriseArchitectObject, objectBrowser, requirementsBrowser, statusBar)
        {
            this.InitializeObservablesAndCommands();
        }

        /// <summary>
        /// Gets or sets the selected <see cref="Element" />
        /// </summary>
        public Element SelectedElement
        {
            get => this.selectedElement;
            set => this.RaiseAndSetIfChanged(ref this.selectedElement, value);
        }

        /// <summary>
        /// Gets the Context Menu for the implementing view model
        /// </summary>
        public ReactiveList<ContextMenuItemViewModel> ContextMenu { get; } = new();

        /// <summary>
        /// Populate the context menu for the implementing view model
        /// </summary>
        public void PopulateContextMenu()
        {
            this.ContextMenu.Clear();

            if (this.SelectedItem is null)
            {
                return;
            }

            this.ContextMenu.Add(new ContextMenuItemViewModel("Map select row to a new Dst Element", "", this.MapToNewElementCommand,
                MenuItemKind.Edit, ClassKind.NotThing));
        }

        /// <summary>
        /// Initializes this view model properties
        /// </summary>
        /// <param name="selectedThings">A collection of <see cref="Thing" /> that has been selected for mapping</param>
        public void Initialize(List<Thing> selectedThings)
        {
            this.things = selectedThings;

            var allElements = this.DstController.GetAllBlocksAndRequirementsOfRepository();
            var packageIds = this.DstController.RetrieveAllParentsIdPackage(allElements);

            this.EnterpriseArchitectObjectBrowser.BuildTree(this.DstController.CurrentRepository.Models.OfType<Package>(), allElements, packageIds);

            this.PreMap();
        }

        /// <summary>
        /// Premaps the elements that has been selected for the mapping
        /// </summary>
        protected override void PreMap()
        {
            this.IsBusy = true;
            this.MappedElements.Clear();
            this.MappedElements.AddRange(this.DstController.HubMapResult);

            foreach (var alreadyMapped in this.MappedElements)
            {
                alreadyMapped.MappedRowStatus = MappedRowStatus.ExistingMapping;
            }

            var newElementsToMap = this.things.Where(x => this.MappedElements
                    .OfType<ElementDefinitionMappedElement>()
                    .All(mapped => mapped.HubElement.Iid != x.Iid) && x is ElementDefinition)
                .Select(elementDefinition => new ElementDefinitionMappedElement((ElementDefinition)elementDefinition, null, MappingDirection.FromHubToDst))
                .Cast<IMappedElementRowViewModel>().ToList();

            newElementsToMap.AddRange(this.things.Where(x => this.MappedElements
                    .OfType<RequirementMappedElement>()
                    .All(mapped => mapped.HubElement.Iid != x.Iid) && x is Requirement)
                .Select(requirement => new RequirementMappedElement((Requirement)requirement, null, MappingDirection.FromHubToDst))
                .Cast<IMappedElementRowViewModel>().ToList());

            var newMappingCollection = this.DstController.PreMap(newElementsToMap, MappingDirection.FromHubToDst);

            foreach (var mappedElement in newMappingCollection)
            {
                mappedElement.MappedRowStatus = mappedElement.ShouldCreateNewTargetElement ? MappedRowStatus.NewElement : MappedRowStatus.ExistingElement;
            }

            this.MappedElements.AddRange(newMappingCollection);
            this.IsBusy = false;
        }

        /// <summary>
        /// Initializes all <see cref="Observable" /> and <see cref="ReactiveCommand{T}" /> of this view model
        /// </summary>
        private void InitializeObservablesAndCommands()
        {
            this.ContinueCommand = ReactiveCommand.Create();

            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand(() =>
            {
                try
                {
                    this.DstController.Map(this.MappedElements.ToList(), MappingDirection.FromHubToDst);
                }
                catch (Exception ex)
                {
                    this.StatusBar.Append($"An error occured during the mapping : {ex.Message}");
                    this.CloseWindowBehavior?.Close();
                }
            }));

            this.MapToNewElementCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanExecuteMapToNewElement));
            this.MapToNewElementCommand.Subscribe(_ => this.ExecuteMapToNewElementCommand());

            this.WhenAnyValue(x => x.SelectedItem,
                x => x.SelectedElement).Subscribe(_ => this.UpdateCanExecute());

            this.WhenAnyValue(x => x.SelectedItem)
                .Subscribe(_ => this.PopulateContextMenu());

            this.ObjectBrowser.SelectedThings.CountChanged.Where(x => x >= 1)
                .Subscribe(_ => this.ReduiceCollectionAndUpdateSelectedThing(this.ObjectBrowser.SelectedThings));

            this.RequirementsBrowser.SelectedThings.CountChanged.Where(x => x >= 1)
                .Subscribe(_ => this.ReduiceCollectionAndUpdateSelectedThing(this.RequirementsBrowser.SelectedThings));

            this.EnterpriseArchitectObjectBrowser.SelectedThings.CountChanged.Where(x => x >= 1)
                .Subscribe(_ => this.ReduiceCollectionAndUpdateSelectedThing(this.EnterpriseArchitectObjectBrowser.SelectedThings));
        }

        /// <summary>
        /// Update the DstElement that the <see cref="SelectedElement" /> will be mapped to
        /// </summary>
        private void ExecuteMapToNewElementCommand()
        {
            switch (this.SelectedItem)
            {
                case ElementDefinitionMappedElement elementDefinitionMappedElement:
                    elementDefinitionMappedElement.DstElement = this.SelectedElement;
                    elementDefinitionMappedElement.ShouldCreateNewTargetElement = false;
                    elementDefinitionMappedElement.MappedRowStatus = MappedRowStatus.ExistingElement;
                    break;
                case RequirementMappedElement requirementMappedElement:
                    requirementMappedElement.DstElement = this.SelectedElement;
                    requirementMappedElement.ShouldCreateNewTargetElement = false;
                    requirementMappedElement.MappedRowStatus = MappedRowStatus.ExistingElement;
                    break;
            }
        }

        /// <summary>
        /// Verifies the compatibility of Type of <see cref="MappingConfigurationDialogViewModel.SelectedItem" /> and
        /// <see cref="SelectedElement" />
        /// </summary>
        private void UpdateCanExecute()
        {
            if (this.SelectedItem == null || this.SelectedElement == null)
            {
                this.CanExecuteMapToNewElement = false;
                return;
            }

            this.CanExecuteMapToNewElement = this.SelectedElement.Stereotype.AreEquals(StereotypeKind.Requirement) && this.SelectedItem is RequirementMappedElement ||
                                             this.SelectedElement.Stereotype.AreEquals(StereotypeKind.Block) && this.SelectedItem is ElementDefinitionMappedElement;
        }

        /// <summary>
        /// Reduice the collection to have always only one element inside it
        /// </summary>
        /// <param name="collection">The collection</param>
        private void ReduiceCollectionAndUpdateSelectedThing(IReactiveList<object> collection)
        {
            switch (collection.Count)
            {
                case 1:
                    this.UpdateDstSelectedThing(collection[0]);
                    this.UpdateHubSelectedThing(collection[0]);
                    break;
                case > 1:
                    collection.RemoveRange(0, collection.Count - 1);
                    break;
            }
        }

        /// <summary>
        /// Update the <see cref="MappingConfigurationDialogViewModel.SelectedObjectBrowserThing" />
        /// </summary>
        /// <param name="newSelectedRow">The new selected </param>
        private void UpdateHubSelectedThing(object newSelectedRow)
        {
            this.SelectedObjectBrowserThing = newSelectedRow switch
            {
                IRowViewModelBase<Requirement> requirementRow => requirementRow.Thing,
                IRowViewModelBase<ElementDefinition> elementDefinitionRow => elementDefinitionRow.Thing,
                _ => null
            };

            this.SetHubSelectedThing(this.SelectedObjectBrowserThing);
        }

        /// <summary>
        /// Update the <see cref="MappingConfigurationDialogViewModel.SelectedItem" /> depending on the selected row inside the
        /// <see cref="EnterpriseArchitectObjectBrowser" />
        /// </summary>
        /// <param name="newSelectedRow">The selected row inside the <see cref="EnterpriseArchitectObjectBrowser" /></param>
        private void UpdateDstSelectedThing(object newSelectedRow)
        {
            switch (newSelectedRow)
            {
                case ElementRowViewModel elementRow:
                    this.SelectedElement = elementRow.RepresentedObject;
                    break;
                default:
                    this.SelectedItem = null;
                    return;
            }
        }

        /// <summary>
        /// Find the correct <see cref="IMappedElementRowViewModel" /> corresponding to the given <see cref="Thing" />
        /// </summary>
        /// <param name="thing">The <see cref="Thing" /></param>
        private void SetHubSelectedThing(Thing thing)
        {
            this.SelectedItem = thing switch
            {
                ElementDefinition elementDefinition => this.MappedElements.OfType<ElementDefinitionMappedElement>()
                    .FirstOrDefault(x => x.HubElement.Iid == elementDefinition.Iid),
                Requirement requirement => this.MappedElements.OfType<RequirementMappedElement>()
                    .FirstOrDefault(x => x.HubElement.Iid == requirement.Iid),
                _ => null
            };
        }
    }
}
