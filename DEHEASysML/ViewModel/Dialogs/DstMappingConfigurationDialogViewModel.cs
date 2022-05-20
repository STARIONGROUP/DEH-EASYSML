// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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
    /// This view model let the user configure the mapping from the dst to the hub source
    /// </summary>
    public class DstMappingConfigurationDialogViewModel : MappingConfigurationDialogViewModel, IDstMappingConfigurationDialogViewModel, IHaveContextMenuViewModel
    {
        /// <summary>
        /// The collection of <see cref="Element" /> to map
        /// </summary>
        private List<Element> elements;

        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationDialogViewModel" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="enterpriseArchitectObject">The <see cref="IEnterpriseArchitectObjectBrowserViewModel" /></param>
        /// <param name="objectBrowser">The <see cref="IObjectBrowserViewModel" /></param>
        /// <param name="requirementsBrowser">The <see cref="IObjectBrowserViewModel" /></param>
        public DstMappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController,
            IEnterpriseArchitectObjectBrowserViewModel enterpriseArchitectObject, IObjectBrowserViewModel objectBrowser,
            IRequirementsBrowserViewModel requirementsBrowser) : base(hubController, dstController, enterpriseArchitectObject, objectBrowser, requirementsBrowser)
        {
            this.InitializeObservablesAndCommands();
        }

        /// <summary>
        /// Gets the Context Menu for the implementing view model
        /// </summary>
        public ReactiveList<ContextMenuItemViewModel> ContextMenu { get; } = new();

        /// <summary>
        /// Initializes this view model properties
        /// </summary>
        /// <param name="selectedElements">The collection of <see cref="Element" /> to display</param>
        /// <param name="packageIds">The collection of <see cref="Package" /> to display</param>
        public void Initialize(IEnumerable<Element> selectedElements, IEnumerable<int> packageIds)
        {
            this.elements = selectedElements.ToList();
            this.EnterpriseArchitectObjectBrowser.BuildTree(this.DstController.CurrentRepository.Models.OfType<Package>(), this.elements, packageIds);

            this.PreMap();
        }

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

            this.ContextMenu.Add(new ContextMenuItemViewModel("Map select row to a new Hub Element", "", this.MapToNewElementCommand,
                MenuItemKind.Edit, ClassKind.NotThing));
        }

        /// <summary>
        /// Premap all <see cref="Element" /> to map
        /// </summary>
        protected override void PreMap()
        {
            this.IsBusy = true;
            this.MappedElements.Clear();
            this.MappedElements.AddRange(this.DstController.DstMapResult);

            foreach (var alreadyMapped in this.MappedElements)
            {
                alreadyMapped.MappedRowStatus = MappedRowStatus.ExistingMapping;
            }

            var newElementsToMap = this.elements.Where(x => this.MappedElements
                                                                .OfType<EnterpriseArchitectRequirementElement>()
                                                                .All(mapped => mapped.DstElement.ElementGUID != x.ElementGUID) &&
                                                            x.Stereotype.AreEquals(StereotypeKind.Requirement))
                .Select(requirement => new EnterpriseArchitectRequirementElement(null, requirement, MappingDirection.FromDstToHub))
                .Cast<IMappedElementRowViewModel>().ToList();

            newElementsToMap.AddRange(this.elements.Where(x => this.MappedElements
                    .OfType<EnterpriseArchitectBlockElement>()
                    .All(mapped => mapped.DstElement.ElementGUID != x.ElementGUID) && x.Stereotype.AreEquals(StereotypeKind.Block))
                .Select(block => new EnterpriseArchitectBlockElement(null, block, MappingDirection.FromDstToHub))
                .Cast<IMappedElementRowViewModel>().ToList());

            var newMappingCollection = this.DstController.PreMap(newElementsToMap, MappingDirection.FromDstToHub);

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

            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand(() => { this.DstController.Map(this.MappedElements.ToList(), MappingDirection.FromDstToHub); }));

            this.MapToNewElementCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanExecuteMapToNewElement));
            this.MapToNewElementCommand.Subscribe(_ => this.ExecuteMapToNewElementCommand());

            this.WhenAnyValue(x => x.SelectedItem,
                x => x.SelectedObjectBrowserThing).Subscribe(_ => this.UpdateCanExecute());

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
        /// Verifies the compatibility of Type of <see cref="SelectedItem" /> and <see cref="SelectedObjectBrowserThing" />
        /// </summary>
        private void UpdateCanExecute()
        {
            if (this.SelectedItem == null || this.SelectedObjectBrowserThing == null)
            {
                this.CanExecuteMapToNewElement = false;
                return;
            }

            this.CanExecuteMapToNewElement = this.SelectedObjectBrowserThing is Requirement && this.SelectedItem is EnterpriseArchitectRequirementElement ||
                                             this.SelectedObjectBrowserThing is ElementDefinition && this.SelectedItem is EnterpriseArchitectBlockElement;
        }

        /// <summary>
        /// Update the HubElement that the <see cref="SelectedItem" /> will be mapped to
        /// </summary>
        private void ExecuteMapToNewElementCommand()
        {
            switch (this.SelectedItem)
            {
                case EnterpriseArchitectRequirementElement requirement:
                    var requirementsSpecification = requirement.HubElement.Container as RequirementsSpecification;

                    if (requirement.HubElement.Original == null)
                    {
                        requirementsSpecification.Requirement.Remove(requirement.HubElement);
                    }
                    else
                    {
                        var originalRequirement = requirement.HubElement.Original as Requirement;
                        requirementsSpecification.Requirement.RemoveAll(x => x.Iid == requirement.HubElement.Iid);
                        requirementsSpecification.Requirement.Add(originalRequirement);
                    }

                    requirement.HubElement = (Requirement)this.SelectedObjectBrowserThing.Clone(true);
                    requirement.ShouldCreateNewTargetElement = false;
                    requirement.MappedRowStatus = MappedRowStatus.ExistingElement;
                    break;
                case EnterpriseArchitectBlockElement block:
                    block.HubElement = (ElementDefinition)this.SelectedObjectBrowserThing.Clone(true);
                    block.ShouldCreateNewTargetElement = false;
                    block.MappedRowStatus = MappedRowStatus.ExistingElement;
                    break;
            }
        }

        /// <summary>
        /// Update the <see cref="SelectedObjectBrowserThing" />
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
        }

        /// <summary>
        /// Update the <see cref="SelectedItem" /> depending on the selected row inside the
        /// <see cref="EnterpriseArchitectObjectBrowser" />
        /// </summary>
        /// <param name="newSelectedRow">The selected row inside the <see cref="EnterpriseArchitectObjectBrowser" /></param>
        private void UpdateDstSelectedThing(object newSelectedRow)
        {
            switch (newSelectedRow)
            {
                case ElementRowViewModel elementRow:
                    this.SetDstSelectedThing(elementRow.RepresentedObject);
                    break;
                default:
                    this.SelectedItem = null;
                    return;
            }
        }

        /// <summary>
        /// Find the correct <see cref="IMappedElementRowViewModel" /> corresponding to the given <see cref="Element" />
        /// </summary>
        /// <param name="element">The <see cref="Element" /></param>
        private void SetDstSelectedThing(Element element)
        {
            if (element.Stereotype.AreEquals(StereotypeKind.Requirement))
            {
                this.SelectedItem = this.MappedElements.OfType<EnterpriseArchitectRequirementElement>()
                    .FirstOrDefault(x => x.DstElement.ElementGUID == element.ElementGUID);
            }
            else if (element.Stereotype.AreEquals(StereotypeKind.Block))
            {
                this.SelectedItem = this.MappedElements.OfType<EnterpriseArchitectBlockElement>()
                    .FirstOrDefault(x => x.DstElement.ElementGUID == element.ElementGUID);
            }
            else
            {
                this.SelectedItem = null;
            }
        }
    }
}
