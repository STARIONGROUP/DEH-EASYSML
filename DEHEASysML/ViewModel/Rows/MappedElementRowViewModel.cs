﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappedElementRowViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.Rows
{
    using System;
    using System.Collections.Generic;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.Enumerators;

    using DEHPCommon.Enumerators;

    using EA;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="MappedElementRowViewModel{TThing}" /> is the base abstract row view model that represents a mapping done
    /// between
    /// a <see cref="Thing" /> and a DST Element
    /// </summary>
    public abstract class MappedElementRowViewModel<TThing> : ReactiveObject, IMappedElementRowViewModel where TThing : Thing
    {
        /// <summary>
        /// Backing field for <see cref="DstElement" />
        /// </summary>
        private Element dstElement;

        /// <summary>
        /// Backing field for <see cref="HubElement" />
        /// </summary>
        private TThing hubElement;

        /// <summary>
        /// Backing field for <see cref="MappingDirection" />
        /// </summary>
        private MappingDirection mappingDirection;

        /// <summary>
        /// Backing field for <see cref="ShouldCreateNewTargetElement" />
        /// </summary>
        private bool shouldCreateNewTargetElement;

        /// <summary>
        /// Backing field for <see cref="TargetElementName" />
        /// </summary>
        private string targetElementName;

        /// <summary>
        /// Backing field for <see cref="SourceElementName" />
        /// </summary>
        private string sourceElementName;

        /// <summary>
        /// Backing field for <see cref="MappedRowStatus" />
        /// </summary>
        private MappedRowStatus mappedRowStatus;

        /// <summary>
        /// Backing field for <see cref="ShoulDisplayComboBox"/>
        /// </summary>
        private bool shouldDisplayComboBox;

        /// <summary>
        /// Backing field for <see cref="ShouldDisplayArrowAndIcons"/>
        /// </summary>
        private bool shouldDisplayArrowAndIcons;

        /// <summary>
        /// A collection of <see cref="IDisposable"/>
        /// </summary>
        protected List<IDisposable> Disposables = [];

        /// <summary>
        /// Initializes a new <see cref="MappedElementRowViewModel{TThing}" />
        /// </summary>
        /// <param name="thing">The <see cref="TThing" /></param>
        /// <param name="dstElement">The <see cref="Element" /></param>
        /// <param name="mappingDirection">The <see cref="MappingDirection" /></param>
        protected MappedElementRowViewModel(TThing thing, Element dstElement, MappingDirection mappingDirection)
        {
            this.HubElement = thing;
            this.DstElement = dstElement;
            this.MappingDirection = mappingDirection;
            this.ShouldDisplayArrowAndIcons = true;

            this.Disposables.Add(this.WhenAnyValue(x => x.DstElement, x => x.HubElement)
                .Subscribe(_ => this.UpdateProperties()));
        }

        /// <summary>
        /// Gets or sets the value indicating if the combobox should be visible
        /// </summary>
        public bool ShoulDisplayComboBox
        {
            get => this.shouldDisplayComboBox;
            set => this.RaiseAndSetIfChanged(ref this.shouldDisplayComboBox, value);
        }

        /// <summary>
        /// Gets or sets the value indicating if the Arrow and icons should be display
        /// </summary>
        public bool ShouldDisplayArrowAndIcons
        {
            get => this.shouldDisplayArrowAndIcons;
            set => this.RaiseAndSetIfChanged(ref this.shouldDisplayArrowAndIcons, value);
        }

        /// <summary>
        /// A collection of <see cref="MappedParameterRowViewModel"/>
        /// </summary>
        public ReactiveList<object> ContainedRows { get; } = new();

        /// <summary>
        /// A collection of <see cref="BinaryRelationship" />
        /// </summary>
        public List<BinaryRelationship> RelationShips { get; } = new();

        /// <summary>
        /// Gets or sets the <see cref="IMappedElementRowViewModel.MappedRowStatus" />
        /// </summary>
        public MappedRowStatus MappedRowStatus
        {
            get => this.mappedRowStatus;
            set => this.RaiseAndSetIfChanged(ref this.mappedRowStatus, value);
        }

        /// <summary>
        /// The <see cref="MappingDirection " />
        /// </summary>
        public MappingDirection MappingDirection
        {
            get => this.mappingDirection;
            set => this.RaiseAndSetIfChanged(ref this.mappingDirection, value);
        }

        /// <summary>
        /// The <see cref="TThing" />
        /// </summary>
        public TThing HubElement
        {
            get => this.hubElement;
            set => this.RaiseAndSetIfChanged(ref this.hubElement, value);
        }

        /// <summary>
        /// The <see cref="Element" />
        /// </summary>
        public Element DstElement
        {
            get => this.dstElement;
            set => this.RaiseAndSetIfChanged(ref this.dstElement, value);
        }

        /// <summary>
        /// Gets or sets a value indicating wheter this row represents a mapping done to a new element
        /// </summary>
        public bool ShouldCreateNewTargetElement
        {
            get => this.shouldCreateNewTargetElement;
            set => this.RaiseAndSetIfChanged(ref this.shouldCreateNewTargetElement, value);
        }

        /// <summary>
        /// The name of the Target Element
        /// </summary>
        public string TargetElementName
        {
            get => this.targetElementName;
            set => this.RaiseAndSetIfChanged(ref this.targetElementName, value);
        }

        /// <summary>
        /// The name of the Source Element
        /// </summary>
        public string SourceElementName
        {
            get => this.sourceElementName;
            set => this.RaiseAndSetIfChanged(ref this.sourceElementName, value);
        }

        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void UpdateProperties()
        {
            var dstElementDisplay = $"{this.DstElement?.Name} [{this.DstElement?.Stereotype}]";

            if (this.DstElement == null)
            {
                dstElementDisplay = string.Empty;
            }

            string hubElementDisplay;

            if (this.HubElement is INamedThing namedThing)
            {
                hubElementDisplay = $"{namedThing.Name} [{namedThing.GetType().Name}]";
            }
            else
            {
                hubElementDisplay = $"{this.HubElement?.UserFriendlyName}";
            }

            this.SourceElementName = this.MappingDirection == MappingDirection.FromDstToHub ? dstElementDisplay : hubElementDisplay;
            this.TargetElementName = this.MappingDirection == MappingDirection.FromDstToHub ? hubElementDisplay : dstElementDisplay;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">If the object have to dispose or not</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            this.Disposables.ForEach(x => x.Dispose());
            this.Disposables.Clear();
        }
    }
}
