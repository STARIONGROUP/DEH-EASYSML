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

    using CDP4Common.CommonData;

    using CDP4Dal;

    using DEHEASysML.DstController;
    using DEHEASysML.Events;
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
        /// Initializes a new <see cref="DstNetChangePreviewViewModel" />
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        public DstNetChangePreviewViewModel(IDstController dstController)
        {
            this.dstController = dstController;

            this.InitializeCommandsAndObservables();
            this.ComputeValues();
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
                new ContextMenuItemViewModel("Select all for transfer", "", this.SelectAllCommand, MenuItemKind.Copy, ClassKind.NotThing));

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Deselect all for transfer", "", this.DeselectAllCommand, MenuItemKind.Delete, ClassKind.NotThing));
        }

        /// <summary>
        /// Compute all rows
        /// </summary>
        public void ComputeValues()
        {
            if (!this.dstController.IsFileOpen)
            {
                return;
            }

            this.IsBusy = true;
            this.BuildNetChangeTree();

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
                    case EnterpriseArchitectRequirementElement requirement:
                        dstElement = requirement.DstElement;
                        break;
                    case EnterpriseArchitectBlockElement block:
                        dstElement = block.DstElement;
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
                case ElementRowViewModel elementRow when this.IsThingTransferable(elementRow.RepresentedObject):
                    elementRow.IsSelectedForTransfer = !elementRow.IsSelectedForTransfer;
                    this.AddOrRemoveToSelectedThingsToTransfer(elementRow);
                    break;
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
                    case EnterpriseArchitectRequirementElement requirementElement:
                        mappedElement = requirementElement.DstElement;
                        break;
                    case EnterpriseArchitectBlockElement blockElement:
                        mappedElement = blockElement.DstElement;
                        break;
                    default: return;
                }

                var row = this.GetOrCreateRow(mappedElement);
                row.IsHighlighted = true;
            }
        }

        /// <summary>
        /// Gets or create the row corresponding to the <see cref="Element" />
        /// </summary>
        /// <param name="mappedElement">The <see cref="Element" /></param>
        /// <returns>A <see cref="ElementRowViewModel" /></returns>
        private ElementRowViewModel GetOrCreateRow(Element mappedElement)
        {
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
            CDPMessageBus.Current.Listen<UpdateDstNetChangePreview>().Subscribe(_ => this.ComputeValues());

            this.SelectedThings.BeforeItemsAdded.Subscribe(this.WhenItemSelectedChanges);
            this.SelectedThings.BeforeItemsRemoved.Subscribe(this.WhenItemSelectedChanges);

            this.SelectAllCommand = ReactiveCommand.Create();
            this.SelectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer());

            this.DeselectAllCommand = ReactiveCommand.Create();
            this.DeselectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer(false));

            this.Things.IsEmptyChanged.Where(x => !x).Subscribe(_ => this.PopulateContextMenu());
        }

        /// <summary>
        /// The <see cref="Element" />
        /// </summary>
        /// <param name="elementRow">The <see cref="ElementRowViewModel"/></param>
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
            return this.dstController.HubMapResult.OfType<EnterpriseArchitectBlockElement>()
                       .Any(x => x.DstElement.ElementGUID == element.ElementGUID)
                   || this.dstController.HubMapResult.OfType<EnterpriseArchitectRequirementElement>()
                       .Any(x => x.DstElement.ElementGUID == element.ElementGUID);
        }
    }
}
