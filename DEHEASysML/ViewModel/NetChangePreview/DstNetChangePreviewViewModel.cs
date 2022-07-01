// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstNetChangePreviewViewModel.cs" company="RHEA System S.A.">
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

    using CDP4Common;
    using CDP4Common.CommonData;

    using CDP4Dal;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Events;
    using DEHEASysML.Extensions;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser;
    using DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows;
    using DEHEASysML.ViewModel.NetChangePreview.Interfaces;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.ViewModels;

    using EA;

    using ReactiveUI;

    /// <summary>
    /// View model for this dst net change preview panel
    /// </summary>
    public class DstNetChangePreviewViewModel : EnterpriseArchitectObjectBrowserViewModel, IDstNetChangePreviewViewModel
    {
        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// A collection of <see cref="ElementRowViewModel" /> that are highlighted
        /// </summary>
        private readonly List<EnterpriseArchitectObjectBaseRowViewModel> highlightedRows = new();

        /// <summary>
        /// Initializes a new <see cref="DstNetChangePreviewViewModel" />
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        public DstNetChangePreviewViewModel(IDstController dstController)
        {
            this.dstController = dstController;

            this.InitializeCommandsAndObservables();
            this.ComputeValues(true);
        }

        /// <summary>
        /// The command for the context menu that allows to deselect all selectable <see cref="Element" /> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer" />
        /// </summary>
        public ReactiveCommand<object> DeselectAllCommand { get; set; }

        /// <summary>
        /// The command for the context menu that allows to select all selectable <see cref="Element" /> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer" />
        /// </summary>
        public ReactiveCommand<object> SelectAllCommand { get; set; }

        /// <summary>
        /// Populates the context menu
        /// </summary>
        public override void PopulateContextMenu()
        {
            this.ContextMenu.Clear();

            if (this.dstController != null && !this.dstController.HubMapResult.Any())
            {
                return;
            }

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Select all mapped elements for transfer", "", this.SelectAllCommand, MenuItemKind.Copy, ClassKind.NotThing));

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Deselect all mapped elements for transfer", "", this.DeselectAllCommand, MenuItemKind.Delete, ClassKind.NotThing));
        }

        /// <summary>
        /// Compute all rows
        /// </summary>
        /// <param name="shouldReset">A value indicating if the tree has to reset or not</param>
        public void ComputeValues(bool shouldReset)
        {
            if (!this.dstController.IsFileOpen)
            {
                this.Things.Clear();
                return;
            }

            this.IsBusy = true;

            if (shouldReset)
            {
                this.BuildNetChangeTree();
            }
            else
            {
                this.CleanTree();
            }

            this.SelectedThings.Clear();
            this.highlightedRows.Clear();

            this.ComputeRows();
            this.IsBusy = false;
        }

        /// <summary>
        /// Executes the <see cref="SelectAllCommand" /> and the <see cref="DeselectAllCommand" />
        /// </summary>
        /// <param name="areSelected">A value indicating whether the elements are to be selected</param>
        public void SelectDeselectAllForTransfer(bool areSelected = true)
        {
            foreach (var mappedElement in this.dstController.HubMapResult)
            {
                Element dstElement;

                switch (mappedElement)
                {
                    case ElementDefinitionMappedElement block:
                        dstElement = block.DstElement;
                        break;
                    case RequirementMappedElement requirement:
                        dstElement = requirement.DstElement;
                        break;
                    default:
                        return;
                }

                this.AddOrRemoveToSelectedThingsToTransfer(dstElement, areSelected);
                this.GetOrCreateRow(dstElement).IsSelectedForTransfer = areSelected;
            }
        }

        /// <summary>
        /// Occurs when the <see cref="EnterpriseArchitectObjectBrowserViewModel.SelectedThings" /> gets a new element added or
        /// removed
        /// </summary>
        /// <param name="row">The <see cref="object" /> row that was added or removed</param>
        public void WhenItemSelectedChanges(object row)
        {
            switch (row)
            {
                case BlockRowViewModel elementRow when this.IsThingTransferable(elementRow.RepresentedObject):
                    elementRow.IsSelectedForTransfer = !elementRow.IsSelectedForTransfer;
                    this.AddOrRemoveToSelectedThingsToTransfer(elementRow);
                    break;
                case ElementRequirementRowViewModel requirementRow when this.IsThingTransferable(requirementRow.RepresentedObject):
                    requirementRow.IsSelectedForTransfer = !requirementRow.IsSelectedForTransfer;
                    this.AddOrRemoveToSelectedThingsToTransfer(requirementRow);
                    break;
            }
        }

        /// <summary>
        /// Cleans the tree
        /// </summary>
        private void CleanTree()
        {
            foreach (var row in this.highlightedRows.OfType<ElementRowViewModel>())
            {
                row.IsHighlighted = false;
                row.IsSelectedForTransfer = false;

                var element = this.dstController.CurrentRepository.GetElementByGuid(row.RepresentedObject.ElementGUID);

                if (element != null)
                {
                    row.UpdateElement(element);
                }
                else
                {
                    row.Parent.ContainedRows.Remove(row);
                }
            }

            foreach (var row in this.highlightedRows.OfType<PackageRowViewModel>())
            {
                row.IsHighlighted = false;
                row.IsSelectedForTransfer = false;

                var package = this.dstController.CurrentRepository.GetPackageByGuid(row.RepresentedObject.PackageGUID);

                if (package == null)
                {
                    row.Parent.ContainedRows.Remove(row);
                }
            }
        }

        /// <summary>
        /// Compute the rows to display the current mapping
        /// </summary>
        private void ComputeRows()
        {
            foreach (var mappedElementRowViewModel in this.dstController.HubMapResult)
            {
                Element mappedElement;

                switch (mappedElementRowViewModel)
                {
                    case RequirementMappedElement requirementElement:
                        mappedElement = requirementElement.DstElement;
                        break;
                    case ElementDefinitionMappedElement blockElement:
                        mappedElement = blockElement.DstElement;
                        break;
                    default: return;
                }

                var row = this.GetOrCreateRow(mappedElement);

                if (this.dstController.UpdatedStereotypes.ContainsKey(mappedElement.ElementGUID))
                {
                    row.RowType = this.dstController.UpdatedStereotypes[mappedElement.ElementGUID];
                }

                this.HighlightRowAndParentRow(row);

                foreach (var valuePropertyRow in row.ContainedRows.OfType<ValuePropertyRowViewModel>())
                {
                    if (this.dstController.UpdatedValuePropretyValues.TryGetValue(valuePropertyRow.RepresentedObject.ElementGUID, out var newValue))
                    {
                        valuePropertyRow.OverrideValue(newValue);
                    }
                }

                if (row is ElementRequirementRowViewModel requirementRow
                    && this.dstController.UpdatedRequirementValues.TryGetValue(mappedElement.ElementGUID, out var newRequirementValue))
                {
                    requirementRow.OverrideValue(newRequirementValue.text);
                }
            }
        }

        /// <summary>
        /// Highlight a row and the parent of this row
        /// </summary>
        /// <param name="row">The row to highlight</param>
        private void HighlightRowAndParentRow(EnterpriseArchitectObjectBaseRowViewModel row)
        {
            row.IsHighlighted = true;
            this.highlightedRows.Add(row);

            if (row.Parent is { IsHighlighted: false })
            {
                this.HighlightRowAndParentRow(row.Parent);
            }
        }

        /// <summary>
        /// Gets or create the row corresponding to the <see cref="Element" />
        /// </summary>
        /// <param name="mappedElement">The <see cref="Element" /></param>
        /// <returns>A <see cref="ElementRowViewModel" /></returns>
        private ElementRowViewModel GetOrCreateRow(Element mappedElement)
        {
            var highlightedRow = this.highlightedRows
                .OfType<ElementRowViewModel>().FirstOrDefault(x => x.RepresentedObject.ElementGUID == mappedElement.ElementGUID);

            if (highlightedRow != null)
            {
                return highlightedRow;
            }

            var packagesId = this.dstController.RetrieveAllParentsIdPackage(new List<Element> { mappedElement }).ToList();

            foreach (var modelRow in this.Things.OfType<ModelRowViewModel>())
            {
                var row = modelRow.GetOrCreateElementRowViewModel(mappedElement, packagesId);

                if (row != null)
                {
                    return row;
                }
            }

            return null;
        }

        /// <summary>
        /// Build the tree for the complete model
        /// </summary>
        private void BuildNetChangeTree()
        {
            this.Things.Clear();

            var packages = new List<Package>();

            for (short modelCount = 0; modelCount < this.dstController.CurrentRepository.Models.Count; modelCount++)
            {
                packages.Add((Package)this.dstController.CurrentRepository.Models.GetAt(modelCount));
            }

            this.BuildTree(packages);
        }

        /// <summary>
        /// Initializes all <see cref="ReactiveCommand" /><see cref="Observable" /> of this view model
        /// </summary>
        private void InitializeCommandsAndObservables()
        {
            CDPMessageBus.Current.Listen<UpdateDstNetChangePreview>()
                .Subscribe(x => this.ComputeValues(x.Reset));

            CDPMessageBus.Current.Listen<EnterpriseArchitectPackageEvent>()
                .Subscribe(x => this.AddOrRemovePackageRow(x.Id, x.ChangeKind));

            CDPMessageBus.Current.Listen<EnterpriseArchitectElementEvent>()
                .Subscribe(x => this.AddOrRemoveElementRow(x.Id, x.ChangeKind));

            this.SelectedThings.BeforeItemsAdded.Subscribe(this.WhenItemSelectedChanges);
            this.SelectedThings.BeforeItemsRemoved.Subscribe(this.WhenItemSelectedChanges);

            this.SelectAllCommand = ReactiveCommand.Create();
            this.SelectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer());

            this.DeselectAllCommand = ReactiveCommand.Create();
            this.DeselectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer(false));

            this.Things.IsEmptyChanged.Where(x => !x).Subscribe(_ => this.PopulateContextMenu());
        }

        /// <summary>
        /// Adds or removes <see cref="ElementRowViewModel" />
        /// </summary>
        /// <param name="elementId">The id of the <see cref="Element" /></param>
        /// <param name="changeKind">The <see cref="ChangeKind" /></param>
        private void AddOrRemoveElementRow(int elementId, ChangeKind changeKind)
        {
            this.IsBusy = true;

            var element = this.dstController.CurrentRepository.GetElementByID(elementId);

            if (element.HasStereotype(StereotypeKind.Block) || element.HasStereotype(StereotypeKind.Requirement)
                                                            || element.Stereotype.AreEquals(StereotypeKind.State))
            {
                var row = this.GetOrCreateRow(element);

                if (changeKind == ChangeKind.Delete)
                {
                    row.Parent.ContainedRows.Remove(row);
                }
            }
            else if ((element.Stereotype.AreEquals(StereotypeKind.Port) || element.Stereotype.AreEquals(StereotypeKind.PartProperty)
                                                                       || element.Stereotype.AreEquals(StereotypeKind.ValueProperty))
                     && element.ParentID != 0)
            {
                var blockElement = this.dstController.CurrentRepository.GetElementByID(element.ParentID);

                var parentRow = this.GetOrCreateRow(blockElement);

                if (changeKind == ChangeKind.Delete)
                {
                    foreach (var containedRwows in parentRow.ContainedRows.OfType<ElementRowViewModel>()
                                 .Where(containedRwows => containedRwows.RepresentedObject.ElementID == elementId).ToList())
                    {
                        containedRwows.Parent.ContainedRows.Remove(containedRwows);
                    }
                }
            }

            this.IsBusy = false;
        }

        /// <summary>
        /// Adds or removes <see cref="PackageRowViewModel" />
        /// </summary>
        /// <param name="packageId">The id of the <see cref="Package" /></param>
        /// <param name="changeKind">The <see cref="ChangeKind" /></param>
        private void AddOrRemovePackageRow(int packageId, ChangeKind changeKind)
        {
            this.IsBusy = true;

            var package = this.dstController.CurrentRepository.GetPackageByID(packageId);

            var packageRow = this.GetOrCreatePackageRow(package);

            if (changeKind == ChangeKind.Delete)
            {
                packageRow.Parent.ContainedRows.Remove(packageRow);
            }

            this.IsBusy = false;
        }

        /// <summary>
        /// Gets or create the <see cref="PackageRowViewModel" /> representing the <see cref="Package" />
        /// </summary>
        /// <param name="package">The <see cref="Package" /></param>
        /// <returns>The <see cref="PackageRowViewModel" /></returns>
        private PackageRowViewModel GetOrCreatePackageRow(Package package)
        {
            var packagesId = new List<int>();
            this.dstController.GetPackageParentId(package.PackageID, ref packagesId);

            if (packagesId.Count > 1)
            {
                packagesId.RemoveAt(0);
            }

            packagesId.Reverse();

            foreach (var modelRow in this.Things.OfType<ModelRowViewModel>())
            {
                var row = modelRow.GetOrCreatePackageRowViewModel(package, packagesId);

                if (row != null)
                {
                    return row;
                }
            }

            return null;
        }

        /// <summary>
        /// The <see cref="Element" />
        /// </summary>
        /// <param name="elementRow">The <see cref="ElementRowViewModel" /></param>
        private void AddOrRemoveToSelectedThingsToTransfer(ElementRowViewModel elementRow)
        {
            this.AddOrRemoveToSelectedThingsToTransfer(elementRow.RepresentedObject, elementRow.IsSelectedForTransfer);
        }

        /// <summary>
        /// The <see cref="Element" />
        /// </summary>
        /// <param name="element">The <see cref="Element" /></param>
        /// <param name="isSelected">A value indicating wheter to select the element for transfer</param>
        private void AddOrRemoveToSelectedThingsToTransfer(Element element, bool isSelected)
        {
            this.dstController.SelectedHubMapResultForTransfer.RemoveAll(this.dstController.SelectedHubMapResultForTransfer
                .Where(x => x.ElementGUID == element.ElementGUID).ToList());

            if (isSelected)
            {
                this.dstController.SelectedHubMapResultForTransfer.Add(element);
            }
        }

        /// <summary>
        /// Verifies if an <see cref="Element" /> is transferable
        /// </summary>
        /// <param name="element">The <see cref="Element" /></param>
        /// <returns>Asserts if the <see cref="Element" /> is transferable</returns>
        private bool IsThingTransferable(Element element)
        {
            return this.dstController.HubMapResult.OfType<ElementDefinitionMappedElement>()
                       .Any(x => x.DstElement.ElementGUID == element.ElementGUID)
                   || this.dstController.HubMapResult.OfType<RequirementMappedElement>()
                       .Any(x => x.DstElement.ElementGUID == element.ElementGUID);
        }
    }
}
