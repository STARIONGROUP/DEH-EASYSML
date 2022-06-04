// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubRequirementsNetChangePreviewViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.NetChangePreview
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;
    using DEHEASysML.ViewModel.RequirementsBrowser;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// View model for this hub net change preview of the Hub Requirements
    /// </summary>
    public class HubRequirementsNetChangePreviewViewModel : RequirementsBrowserViewModel, IHubRequirementsNetChangePreviewViewModel
    {
        /// <summary>
        /// The collection of <see cref="EnterpriseArchitectRequirementElement" />
        /// </summary>
        private readonly List<EnterpriseArchitectRequirementElement> mappedRequirements = new();

        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Initialize a new <see cref="RequirementsBrowserViewModel" />
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="objectBrowserTreeSelectorService">The <see cref="IObjectBrowserTreeSelectorService" /></param>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        public HubRequirementsNetChangePreviewViewModel(IHubController hubController, IObjectBrowserTreeSelectorService objectBrowserTreeSelectorService,
            IDstController dstController)
            : base(hubController, objectBrowserTreeSelectorService)
        {
            this.dstController = dstController;
            this.Initialize();
        }

        /// <summary>
        /// The collection of <see cref="IMappedElementRowViewModel" />
        /// </summary>
        public ReactiveList<IMappedElementRowViewModel> MappedElements { get; } = new();

        /// <summary>
        /// The command for the context menu that allows to deselect all selectable <see cref="ElementBase" /> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer" />
        /// </summary>
        public ReactiveCommand<object> DeselectAllCommand { get; set; }

        /// <summary>
        /// The command for the context menu that allows to select all selectable <see cref="ElementBase" /> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer" />
        /// </summary>
        public ReactiveCommand<object> SelectAllCommand { get; set; }

        /// <summary>
        /// Computes the old values with the new <see cref="IMappedElementRowViewModel" /> collection
        /// </summary>
        public void ComputeValues()
        {
            this.ComputeValues(true);
        }

        /// <summary>
        /// Occurs when the <see cref="RequirementsBrowserViewModel.SelectedThings" /> gets a new element added or removed
        /// </summary>
        /// <param name="row">The <see cref="object" /> row that was added or removed</param>
        public void WhenItemSelectedChanges(object row)
        {
            switch (row)
            {
                case RequirementsSpecificationRowViewModel requirementsSpecificationRow when this.IsThingTransferable(requirementsSpecificationRow.Thing):
                    requirementsSpecificationRow.IsSelectedForTransfer = !requirementsSpecificationRow.IsSelectedForTransfer;
                    this.AddOrRemoveToSelectedThingsToTransfer(requirementsSpecificationRow, requirementsSpecificationRow.IsSelectedForTransfer);
                    break;
                case RequirementsGroupRowViewModel requirementsGroup when this.IsThingTransferable(requirementsGroup):
                    requirementsGroup.IsSelectedForTransfer = !requirementsGroup.IsSelectedForTransfer;
                    this.AddOrRemoveToSelectedThingsToTransfer(requirementsGroup, requirementsGroup.IsSelectedForTransfer);
                    this.UpdateContainerRowViewModelStatus(requirementsGroup);
                    break;
                case RequirementRowViewModel requirementRow when this.IsThingTransferable(requirementRow.Thing):
                    requirementRow.IsSelectedForTransfer = !requirementRow.IsSelectedForTransfer;
                    this.AddOrRemoveToSelectedThingsToTransfer(requirementRow.Thing, requirementRow.IsSelectedForTransfer);
                    this.UpdateContainerRowViewModelStatus(requirementRow);
                    break;
            }
        }

        /// <summary>
        /// Executes the <see cref="SelectAllCommand" /> and the <see cref="DeselectAllCommand" />
        /// </summary>
        /// <param name="areSelected">A value indicating whether the elements are to be selected</param>
        public void SelectDeselectAllForTransfer(bool areSelected = true)
        {
            foreach (var requirementsSpecification in
                     this.mappedRequirements.Select(x => x.HubElement.Container as RequirementsSpecification).Distinct())
            {
                this.GetRequirementSpecificationRowViewModel(requirementsSpecification, out var requirementsSpecificationRowViewModel);
                requirementsSpecificationRowViewModel.IsSelectedForTransfer = areSelected;
                this.AddOrRemoveToSelectedThingsToTransfer(requirementsSpecificationRowViewModel, areSelected);
            }
        }

        /// <summary>
        /// Populates the context menu
        /// </summary>
        public override void PopulateContextMenu()
        {
            this.ContextMenu.Clear();

            if (!this.MappedElements.Any())
            {
                return;
            }

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Select all mapped elements for transfer", "", this.SelectAllCommand, MenuItemKind.Copy, ClassKind.NotThing));

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Deselect all mapped elements for transfer", "", this.DeselectAllCommand, MenuItemKind.Delete, ClassKind.NotThing));
        }

        /// <summary>
        /// Verify that the given <see cref="RequirementsSpecification" /> already has a
        /// <see cref="RequirementsSpecificationRowViewModel" />
        /// If not, creates one
        /// </summary>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /></param>
        /// <param name="requirementsSpecificationRow">The <see cref="RequirementsSpecificationRowViewModel" /></param>
        /// <returns>Asserts if the tree already contained the <see cref="RequirementsSpecificationRowViewModel" /></returns>
        public bool GetRequirementSpecificationRowViewModel(RequirementsSpecification requirementsSpecification,
            out RequirementsSpecificationRowViewModel requirementsSpecificationRow)
        {
            var iterationRow = this.Things.OfType<IterationRequirementsViewModel>().FirstOrDefault();

            requirementsSpecificationRow = iterationRow.ContainedRows.OfType<RequirementsSpecificationRowViewModel>()
                .FirstOrDefault(x => x.Thing.Iid == requirementsSpecification.Iid
                                     && x.Thing.Name == requirementsSpecification.Name);

            if (requirementsSpecificationRow is null)
            {
                requirementsSpecificationRow = new RequirementsSpecificationRowViewModel(requirementsSpecification, this.HubController.Session, iterationRow);
                iterationRow.ContainedRows.Add(requirementsSpecificationRow);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialize this view model
        /// </summary>
        private void Initialize()
        {
            this.InitializeObservablesAndCommands();
            this.UpdateProperties();
            this.ComputeValues(false);
        }

        /// <summary>
        /// Initializes this view model <see cref="Observable" /> and <see cref="ReactiveCommand" />
        /// </summary>
        private void InitializeObservablesAndCommands()
        {
            this.SelectedThings.BeforeItemsAdded.Subscribe(this.WhenItemSelectedChanges);
            this.SelectedThings.BeforeItemsRemoved.Subscribe(this.WhenItemSelectedChanges);

            this.SelectAllCommand = ReactiveCommand.Create();
            this.SelectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer());

            this.DeselectAllCommand = ReactiveCommand.Create();
            this.DeselectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer(false));

            this.MappedElements.CountChanged.Subscribe(_ => this.PopulateContextMenu());
            this.MappedElements.IsEmptyChanged.Subscribe(_ => this.UpdateProperties());
            this.Things.IsEmptyChanged.Where(x => !x).Subscribe(_ => this.ComputeValues(false));
        }

        /// <summary>
        /// Update the IsSelectedForTransfer status of the containerRow of the <see cref="IHaveContainerViewModel" />
        /// </summary>
        /// <param name="row">The <see cref="IHaveContainerViewModel" /></param>
        private void UpdateContainerRowViewModelStatus(IHaveContainerViewModel row)
        {
            switch (row.ContainerViewModel)
            {
                case RequirementsGroupRowViewModel requirementsGroupRow:
                    this.SetIsSelectedForTransferDependingOnChildren(requirementsGroupRow);
                    this.UpdateContainerRowViewModelStatus(requirementsGroupRow);
                    break;
                case RequirementsSpecificationRowViewModel requirementsSpecificationRow:
                    this.SetIsSelectedForTransferDependingOnChildren(requirementsSpecificationRow);
                    break;
            }
        }

        /// <summary>
        /// Sets the IsSelectedForTransfer properties of a <see cref="RequirementContainerRowViewModel{T}" /> based on the status
        /// of its children
        /// </summary>
        /// <typeparam name="T">A <see cref="RequirementsContainer" /></typeparam>
        /// <param name="containerRow">The <see cref="RequirementContainerRowViewModel{T}" /></param>
        private void SetIsSelectedForTransferDependingOnChildren<T>(RequirementContainerRowViewModel<T> containerRow)
            where T : RequirementsContainer
        {
            containerRow.IsSelectedForTransfer = this.GetAllContainedRequirementRows(containerRow).Any(x => x.IsSelectedForTransfer);

            if (containerRow.Thing is RequirementsGroup requirementsGroup)
            {
                this.AddOrRemoveToSelectedThingsToTransfer(requirementsGroup, containerRow.IsSelectedForTransfer);
            }
        }

        /// <summary>
        /// Gets all contained row of type <see cref="RequirementRowViewModel" /> inside a
        /// <see cref="RequirementContainerRowViewModel{T}" />
        /// and also inside the <see cref="RequirementContainerRowViewModel{T}" />s contained
        /// </summary>
        /// <typeparam name="T">A <see cref="RequirementsContainer" /></typeparam>
        /// <param name="containerRowView">The <see cref="RequirementContainerRowViewModel{T}" /></param>
        /// <returns>A collection of <see cref="RequirementRowViewModel" /></returns>
        private List<RequirementRowViewModel> GetAllContainedRequirementRows<T>(RequirementContainerRowViewModel<T> containerRowView) where T
            : RequirementsContainer
        {
            var containedRequirements = containerRowView.ContainedRows.OfType<RequirementRowViewModel>().ToList();

            foreach (var requirementsGroupRowView in containerRowView.ContainedRows.OfType<RequirementContainerRowViewModel<RequirementsGroup>>())
            {
                containedRequirements.AddRange(this.GetAllContainedRequirementRows(requirementsGroupRowView));
            }

            return containedRequirements;
        }

        /// <summary>
        /// Adds or remove all <see cref="Requirement" /> contained inside a <see cref="RequirementContainerRowViewModel{T}" />
        /// </summary>
        /// <param name="containerRowView">The <see cref="RequirementContainerRowViewModel{T}" /></param>
        /// <param name="areSelected">A value indicating whether the elements are to be selected</param>
        private void AddOrRemoveToSelectedThingsToTransfer<T>(RequirementContainerRowViewModel<T> containerRowView, bool areSelected)
            where T : RequirementsContainer
        {
            if (containerRowView.Thing is RequirementsGroup requirementsGroup)
            {
                this.AddOrRemoveToSelectedThingsToTransfer(requirementsGroup, areSelected);
            }

            foreach (var containedRow in containerRowView.ContainedRows)
            {
                switch (containedRow)
                {
                    case RequirementRowViewModel requirementRow when this.IsThingTransferable(requirementRow.Thing):
                        requirementRow.IsSelectedForTransfer = areSelected;
                        this.AddOrRemoveToSelectedThingsToTransfer(requirementRow);
                        break;
                    case RequirementContainerRowViewModel<RequirementsGroup> requirementContainerRow:
                        requirementContainerRow.IsSelectedForTransfer = areSelected;
                        this.AddOrRemoveToSelectedThingsToTransfer(requirementContainerRow, areSelected);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds or remove a <see cref="RequirementsGroup"/> for the transfer
        /// </summary>
        /// <param name="group">The <see cref="RequirementsGroup"/></param>
        /// <param name="isSelected">A value indicating whether the elements are to be selected</param>
        private void AddOrRemoveToSelectedThingsToTransfer(RequirementsGroup group, bool isSelected)
        {
            this.dstController.SelectedGroupsForTransfer.RemoveAll(this.dstController.SelectedGroupsForTransfer
                .Where(x => x.Iid == group.Iid).ToList());

            if (isSelected)
            {
                this.dstController.SelectedGroupsForTransfer.Add(group);
            }
        }

        /// <summary>
        /// Adds or removes an <see cref="RequirementRowViewModel" /> to the selected thing to transfer
        /// </summary>
        /// <param name="requirementRow">The <see cref="RequirementRowViewModel" /></param>
        private void AddOrRemoveToSelectedThingsToTransfer(RequirementRowViewModel requirementRow)
        {
            this.AddOrRemoveToSelectedThingsToTransfer(requirementRow.Thing, requirementRow.IsSelectedForTransfer);
        }

        /// <summary>
        /// Adds or removes an <see cref="Requirement" /> to the selected thing to transfer
        /// </summary>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <param name="isSelected">A value indicating wheter to select the element for transfer</param>
        private void AddOrRemoveToSelectedThingsToTransfer(Requirement requirement, bool isSelected)
        {
            this.dstController.SelectedDstMapResultForTransfer.RemoveAll(this.dstController.SelectedDstMapResultForTransfer
                .Where(x => x.Iid == requirement.Iid).ToList());

            if (isSelected)
            {
                this.dstController.SelectedDstMapResultForTransfer.Add(requirement);
            }
        }

        /// <summary>
        /// Verifies if an <see cref="Requirement" /> can be transfered
        /// </summary>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <returns>Asserts that the <see cref="Requirement" /> can be transfered</returns>
        private bool IsThingTransferable(Requirement requirement)
        {
            return this.mappedRequirements.Any(x => x.HubElement.Iid == requirement.Iid);
        }

        /// <summary>
        /// Verifies if an <see cref="RequirementsGroupRowViewModel" /> can be transfered
        /// </summary>
        /// <param name="requirementGroupRowViewModel">The <see cref="RequirementsGroupRowViewModel" /></param>
        /// <returns>Asserts that the <see cref="RequirementsGroupRowViewModel" /> can be transfered</returns>
        private bool IsThingTransferable(RequirementsGroupRowViewModel requirementGroupRowViewModel)
        {
            var allContainedRequirements = this.GetAllContainedRequirementRows(requirementGroupRowViewModel);
            return allContainedRequirements.Any(x => this.IsThingTransferable(x.Thing));
        }

        /// <summary>
        /// Verifies if an <see cref="RequirementsSpecification" /> can be transfered
        /// </summary>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /></param>
        /// <returns>Asserts that the <see cref="RequirementsSpecification" /> can be transfered</returns>
        private bool IsThingTransferable(RequirementsSpecification requirementsSpecification)
        {
            return requirementsSpecification.Requirement.Any(this.IsThingTransferable);
        }

        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.mappedRequirements.Clear();
            this.mappedRequirements.AddRange(this.MappedElements.OfType<EnterpriseArchitectRequirementElement>());
        }

        /// <summary>
        /// Computes the old values with the new <see cref="IMappedElementRowViewModel" /> collection
        /// </summary>
        /// <param name="shouldReload">Asserts if the three has to reload itself</param>
        private void ComputeValues(bool shouldReload)
        {
            if (shouldReload)
            {
                this.Reload();
            }
            else
            {
                foreach (var mappedElementRowViewModel in
                         this.mappedRequirements.Select(x => x.HubElement.Container as RequirementsSpecification).Distinct())
                {
                    this.CreateOrUpdateRequirementsSpecificationRow(mappedElementRowViewModel);
                }

                foreach (var requirement in this.mappedRequirements.Select(x => x.HubElement))
                {
                    var requirementRow = this.GetRequirementRowViewModel(requirement);
                    requirementRow.IsHighlighted = true;
                    this.HighlightRequirementsGroupRow(requirementRow);
                }
            }
        }

        /// <summary>
        /// Set Highlight of all <see cref="RequirementsGroupRowViewModel" /> above a row
        /// </summary>
        /// <param name="row">A <see cref="IHaveContainerViewModel" /></param>
        private void HighlightRequirementsGroupRow(IHaveContainerViewModel row)
        {
            if (row.ContainerViewModel is RequirementsGroupRowViewModel { IsHighlighted: false } requirementsGroupRow)
            {
                requirementsGroupRow.IsHighlighted = true;
                this.HighlightRequirementsGroupRow(requirementsGroupRow);
            }
        }

        /// <summary>
        /// Creates or update the <see cref="RequirementsSpecificationRowViewModel" /> based on the given
        /// <see cref="RequirementsSpecification" />
        /// </summary>
        /// <param name="requirementsSpecification">The <see cref="RequirementsSpecification" /></param>
        private void CreateOrUpdateRequirementsSpecificationRow(RequirementsSpecification requirementsSpecification)
        {
            if (this.GetRequirementSpecificationRowViewModel(requirementsSpecification, out var requirementsSpecificationRowViewModel))
            {
                requirementsSpecificationRowViewModel.UpdateThing(requirementsSpecification);
                requirementsSpecificationRowViewModel.UpdateChildren();
            }

            requirementsSpecificationRowViewModel.IsHighlighted = true;
        }

        /// <summary>
        /// Retrieve the <see cref="RequirementRowViewModel" /> that contains the <see cref="Requirement" />
        /// </summary>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <returns>The <see cref="RequirementRowViewModel" /></returns>
        private RequirementRowViewModel GetRequirementRowViewModel(Requirement requirement)
        {
            this.GetRequirementSpecificationRowViewModel(requirement.Container as RequirementsSpecification, out var requirementsSpecificationRow);
            return this.GetRequirementRowViewModel(requirement, requirementsSpecificationRow);
        }

        /// <summary>
        /// Retrieve the <see cref="RequirementRowViewModel" /> that contains the <see cref="Requirement" /> inside a
        /// <see cref="RequirementContainerRowViewModel{T}" />
        /// </summary>
        /// <typeparam name="T">A <see cref="RequirementsContainer" /></typeparam>
        /// <param name="requirement">The <see cref="Requirement" /></param>
        /// <param name="containerRow">The <see cref="RequirementContainerRowViewModel{T}" /></param>
        /// <returns>The <see cref="RequirementRowViewModel" /></returns>
        private RequirementRowViewModel GetRequirementRowViewModel<T>(Requirement requirement, RequirementContainerRowViewModel<T> containerRow)
            where T : RequirementsContainer
        {
            foreach (var containerRowContainedRow in containerRow.ContainedRows)
            {
                switch (containerRowContainedRow)
                {
                    case RequirementRowViewModel requirementRow:
                        if (requirementRow.Thing.Iid == requirement.Iid)
                        {
                            return requirementRow;
                        }

                        break;
                    case RequirementContainerRowViewModel<RequirementsGroup> requirementContainerRow:
                        var foundRow = this.GetRequirementRowViewModel(requirement, requirementContainerRow);

                        if (foundRow != null)
                        {
                            return foundRow;
                        }

                        break;
                }
            }

            return null;
        }
    }
}
