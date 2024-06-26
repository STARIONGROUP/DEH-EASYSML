﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubObjectNetChangePreviewViewModel.cs" company="RHEA System S.A.">
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
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.NetChangePreview;
    using DEHPCommon.UserInterfaces.ViewModels.Rows;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using ReactiveUI;

    /// <summary>
    /// View model for this hub net change preview of the Hub Object
    /// </summary>
    public class HubObjectNetChangePreviewViewModel : NetChangePreviewViewModel, IHubObjectNetChangePreviewViewModel
    {
        /// <summary>
        /// The collection of <see cref="EnterpriseArchitectBlockElement" />
        /// </summary>
        private readonly List<EnterpriseArchitectBlockElement> mappedBlocks = new();

        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetChangePreviewViewModel" /> class.
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController" /></param>
        /// <param name="objectBrowserTreeSelectorService">The <see cref="IObjectBrowserTreeSelectorService" /></param>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        public HubObjectNetChangePreviewViewModel(IHubController hubController, IObjectBrowserTreeSelectorService objectBrowserTreeSelectorService,
            IDstController dstController)
            : base(hubController, objectBrowserTreeSelectorService)
        {
            this.dstController = dstController;

            this.Initialize();
        }

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
        /// The collection of <see cref="IMappedElementRowViewModel" />
        /// </summary>
        public ReactiveList<IMappedElementRowViewModel> MappedElements { get; } = new();

        /// <summary>
        /// Computes the old values with the new <see cref="IMappedElementRowViewModel" /> collection
        /// </summary>
        public override void ComputeValues()
        {
            this.ComputeValues(true);
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
                new ContextMenuItemViewModel("Deselect all for mapped elements transfer", "", this.DeselectAllCommand, MenuItemKind.Delete, ClassKind.NotThing));
        }

        /// <summary>
        /// Occurs when the <see cref="NetChangePreviewViewModel.SelectedThings" /> gets a new element added or removed
        /// </summary>
        /// <param name="row">The <see cref="object" /> row that was added or removed</param>
        public void WhenItemSelectedChanges(object row)
        {
            switch (row)
            {
                case ParameterRowViewModel parameterRowViewModel when this.IsThingTransferable(parameterRowViewModel.Thing) && !this.IsContainedInElementUsage(parameterRowViewModel):
                    this.WhenItemSelectedChanges(parameterRowViewModel);
                    break;

                case ElementUsageRowViewModel elementUsageRowViewModel when this.IsThingTransferable(elementUsageRowViewModel.Thing):
                    this.WhenItemSelectedChanges(elementUsageRowViewModel);
                    break;

                case ElementDefinitionRowViewModel elementDefinitionRow when this.IsThingTransferable(elementDefinitionRow.Thing):
                    this.SelectDeselectElementDefinitionRowToTransfer(elementDefinitionRow);
                    break;
            }
        }

        /// <summary>
        /// Executes the <see cref="SelectAllCommand" /> and the <see cref="DeselectAllCommand" />
        /// </summary>
        /// <param name="areSelected">A value indicating whether the elements are to be selected</param>
        public void SelectDeselectAllForTransfer(bool areSelected = true)
        {
            foreach (var hubElement in this.mappedBlocks.Select(x => x.HubElement))
            {
                this.GetElementDefinitionRowViewModel(hubElement, out var elementDefinitionRowViewModel);
                this.SelectDeselectElementDefinitionRowToTransfer(elementDefinitionRowViewModel, areSelected);
            }
        }

        /// <summary>
        /// Verify that the given <see cref="ElementDefinition" /> already has a <see cref="ElementDefinitionRowViewModel" />
        /// If not, creates one
        /// </summary>
        /// <param name="hubElement">The <see cref="ElementDefinition" /></param>
        /// <param name="elementDefinitionRow">The <see cref="ElementDefinitionRowViewModel" /></param>
        /// <returns>Asserts if the tree already contained the <see cref="ElementDefinitionRowViewModel" /></returns>
        public bool GetElementDefinitionRowViewModel(ElementDefinition hubElement, out ElementDefinitionRowViewModel elementDefinitionRow)
        {
            var iterationRow =
                this.Things.OfType<ElementDefinitionsBrowserViewModel>().FirstOrDefault();

            elementDefinitionRow = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                .FirstOrDefault(e => e.Thing.Iid == hubElement.Iid
                                     && e.Thing.Name == hubElement.Name);

            if (elementDefinitionRow is null)
            {
                elementDefinitionRow = new ElementDefinitionRowViewModel(hubElement,
                    this.HubController.CurrentDomainOfExpertise, this.HubController.Session, iterationRow);

                iterationRow.ContainedRows.Add(elementDefinitionRow);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies that a <see cref="IHaveContainerViewModel" /> is not part of an <see cref="ElementUsageRowViewModel"/>
        /// </summary>
        /// <param name="containedRow">A <see cref="IHaveContainerViewModel"/></param>
        /// <returns>A value indicating if the <see cref="IHaveContainerViewModel" /> if part of an <see cref="ElementUsageRowViewModel"/></returns>
        private bool IsContainedInElementUsage(IHaveContainerViewModel containedRow)
        {
            return containedRow.ContainerViewModel switch
            {
                ElementUsageRowViewModel => true,
                ParameterGroupRowViewModel parameterGroup => this.IsContainedInElementUsage(parameterGroup),
                _ => false
            };
        }

        /// <summary>
        /// Select or deselect an <see cref="ElementDefinitionRowViewModel" /> to transfer
        /// </summary>
        /// <param name="elementDefinitionRow">The <see cref="ElementDefinitionRowViewModel" /></param>
        private void SelectDeselectElementDefinitionRowToTransfer(ElementDefinitionRowViewModel elementDefinitionRow)
        {
            this.SelectDeselectElementDefinitionRowToTransfer(elementDefinitionRow, !elementDefinitionRow.IsSelectedForTransfer);
        }

        /// <summary>
        /// Select or deselect an <see cref="ElementDefinitionRowViewModel" /> to transfer
        /// </summary>
        /// <param name="elementDefinitionRow">the <see cref="ElementDefinitionRowViewModel" /></param>
        /// <param name="isSelected">A value indicating if the <see cref="ElementDefinitionRowViewModel" /> is selected or not</param>
        private void SelectDeselectElementDefinitionRowToTransfer(ElementDefinitionRowViewModel elementDefinitionRow, bool isSelected)
        {
            elementDefinitionRow.IsSelectedForTransfer = isSelected;

            foreach (var transferableRow in this.GetAllTransferableRows(elementDefinitionRow))
            {
                switch (transferableRow)
                {
                    case ParameterRowViewModel parameterRow:
                        parameterRow.IsSelectedForTransfer = elementDefinitionRow.IsSelectedForTransfer;
                        break;
                    case ElementUsageRowViewModel elementUsageRow:
                        elementUsageRow.IsSelectedForTransfer = elementDefinitionRow.IsSelectedForTransfer;
                        break;
                }
            }

            this.AddOrRemoveToSelectedThingsToTransfer(elementDefinitionRow);
        }

        /// <summary>
        /// Retrieves all transferable rows contained inside a <see cref="IHaveContainedRows" />
        /// </summary>
        /// <param name="row">The <see cref="IHaveContainedRows" /></param>
        /// <returns>A collection of transferable rows</returns>
        private IEnumerable<IHaveContainerViewModel> GetAllTransferableRows(IHaveContainedRows row)
        {
            var transferableRows = new List<IHaveContainerViewModel>();

            foreach (var containedRow in row.ContainedRows)
            {
                switch (containedRow)
                {
                    case ElementUsageRowViewModel elementUsageRow:
                        transferableRows.Add(elementUsageRow);
                        break;

                    case ParameterRowViewModel parameterRow:
                        transferableRows.Add(parameterRow);
                        break;
                    case ParameterGroupRowViewModel parameterGroupRow:
                        transferableRows.AddRange(this.GetAllTransferableRows(parameterGroupRow));
                        break;
                }
            }

            return transferableRows;
        }

        /// <summary>
        /// Occurs when a <see cref="RowViewModelBase{TThing}" /> has been selected or deselected
        /// </summary>
        /// <typeparam name="TThing">A <see cref="Thing" /></typeparam>
        /// <param name="row">The <see cref="RowViewModelBase{TThing}" /></param>
        private void WhenItemSelectedChanges<TThing>(RowViewModelBase<TThing> row) where TThing : Thing
        {
            row.IsSelectedForTransfer = !row.IsSelectedForTransfer;

            if (row.ContainerViewModel is ElementDefinitionRowViewModel elementDefinitionRow)
            {
                elementDefinitionRow.IsSelectedForTransfer = this.IsAnyChildrenSelectedForTransfer(elementDefinitionRow);
            }

            this.AddOrRemoveToSelectedThingsToTransfer(row.Thing, row.IsSelectedForTransfer);
        }

        /// <summary>
        /// Verifies in all children of this row if there is any row selected for transfer
        /// </summary>
        /// <param name="row">The row to check the children</param>
        /// <returns>True if any of the children is selected</returns>
        private bool IsAnyChildrenSelectedForTransfer(IHaveContainedRows row)
        {
            foreach (var children in row.ContainedRows)
            {
                switch (children)
                {
                    case ParameterGroupRowViewModel parameterGroupRowViewModel when this.IsAnyChildrenSelectedForTransfer(parameterGroupRowViewModel):
                    case ParameterOrOverrideBaseRowViewModel { IsSelectedForTransfer: true }:
                    case ElementUsageRowViewModel { IsSelectedForTransfer: true }:
                        return true;
                }
            }

            return false;
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
        /// Verifies if an <see cref="ElementDefinition" /> can be transfered
        /// </summary>
        /// <param name="elementDefinition">The <see cref="ElementDefinition" /></param>
        /// <returns>Asserts that the <see cref="ElementDefinition" /> can be transfered</returns>
        private bool IsThingTransferable(ElementDefinition elementDefinition)
        {
            return this.mappedBlocks.Any(x => x.HubElement.Iid == elementDefinition.Iid);
        }

        /// <summary>
        /// Verifies that the <see cref="Thing" /> is transferable
        /// </summary>
        /// <param name="thing">The <see cref="Thing" /></param>
        /// <returns>An assert</returns>
        private bool IsThingTransferable(Thing thing)
        {
            return thing.Container is ElementDefinition elementDefinition && this.IsThingTransferable(elementDefinition);
        }

        /// <summary>
        /// Adds or removes an <see cref="ElementDefinitionRowViewModel" /> to the selected thing to transfer
        /// </summary>
        /// <param name="elementDefinitionRow">The <see cref="ElementDefinitionRowViewModel" /></param>
        private void AddOrRemoveToSelectedThingsToTransfer(ElementDefinitionRowViewModel elementDefinitionRow)
        {
            this.AddOrRemoveToSelectedThingsToTransfer(elementDefinitionRow.Thing, elementDefinitionRow.IsSelectedForTransfer);
        }

        /// <summary>
        /// Adds or removes an <see cref="ElementDefinition" /> to the selected thing to transfer
        /// </summary>
        /// <param name="elementDefinition">The <see cref="ElementDefinition" /></param>
        /// <param name="isSelected">A value indicating wheter to select the element for transfer</param>
        private void AddOrRemoveToSelectedThingsToTransfer(ElementDefinition elementDefinition, bool isSelected)
        {
            this.AddOrRemoveToSelectedThingsToTransfer((Thing)elementDefinition, isSelected);

            foreach (var parameter in elementDefinition.Parameter)
            {
                this.AddOrRemoveToSelectedThingsToTransfer(parameter, isSelected);
            }

            foreach (var elementUsage in elementDefinition.ContainedElement)
            {
                this.AddOrRemoveToSelectedThingsToTransfer(elementUsage, isSelected);
            }
        }

        /// <summary>
        /// Adds or removes a <see cref="Thing" /> to the selected thing to transfer
        /// </summary>
        /// <param name="thing">The <see cref="Thing" /></param>
        /// <param name="isSelected">A value indicating wheter to select the element for transfer</param>
        private void AddOrRemoveToSelectedThingsToTransfer(Thing thing, bool isSelected)
        {
            this.dstController.SelectedDstMapResultForTransfer.RemoveAll(this.dstController.SelectedDstMapResultForTransfer
                .Where(x => x.Iid == thing.Iid).ToList());

            if (isSelected)
            {
                this.dstController.SelectedDstMapResultForTransfer.Add(thing);
            }
        }

        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.mappedBlocks.Clear();
            this.mappedBlocks.AddRange(this.MappedElements.OfType<EnterpriseArchitectBlockElement>());
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
                foreach (var mappedElementRowViewModel in this.mappedBlocks)
                {
                    this.CreateOrUpdateElementDefinitionRow(mappedElementRowViewModel);
                }
            }
        }

        /// <summary>
        /// Creates or update the <see cref="ElementDefinitionRowViewModel" /> based on the given
        /// <see cref="EnterpriseArchitectBlockElement" />
        /// </summary>
        /// <param name="mappedElementRowViewModel">The <see cref="EnterpriseArchitectBlockElement" /></param>
        private void CreateOrUpdateElementDefinitionRow(EnterpriseArchitectBlockElement mappedElementRowViewModel)
        {
            if (this.GetElementDefinitionRowViewModel(mappedElementRowViewModel.HubElement, out var elementDefinitionRowViewModel))
            {
                elementDefinitionRowViewModel.UpdateThing(mappedElementRowViewModel.HubElement);
                elementDefinitionRowViewModel.UpdateChildren();
            }

            elementDefinitionRowViewModel.IsHighlighted = true;
            this.HighlightContainedRows(elementDefinitionRowViewModel);
        }

        /// <summary>
        /// Highlighs all rows contained inside the <see cref="IHaveContainedRows" />
        /// </summary>
        /// <param name="container">The <see cref="IHaveContainedRows" /></param>
        private void HighlightContainedRows(IHaveContainedRows container)
        {
            foreach (var containedRow in container.ContainedRows)
            {
                switch (containedRow)
                {
                    case ParameterOrOverrideBaseRowViewModel parameterOrOverrideBaseRowViewModel:
                        parameterOrOverrideBaseRowViewModel.IsHighlighted = true;
                        break;
                    case ElementUsageRowViewModel elementUsageRowViewModel:
                        elementUsageRowViewModel.IsHighlighted = true;
                        break;
                    case ParameterGroupRowViewModel parameterGroupRowView:
                        this.HighlightContainedRows(parameterGroupRowView);
                        break;
                }
            }
        }
    }
}
