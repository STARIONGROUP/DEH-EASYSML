// --------------------------------------------------------------------------------------------------------------------
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
    using System.Collections.Generic;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="MappedElementRowViewModel" /> is the base abstract row view model that represents a mapping done between
    /// a <see cref="Thing" /> and a DST Element
    /// </summary>
    public abstract class MappedElementRowViewModel<TThing, TDstElement> : ReactiveObject where TThing : Thing
    {
        /// <summary>
        /// Backing field for <see cref="DstElement" />
        /// </summary>
        private TDstElement dstElement;

        /// <summary>
        /// Backing field for <see cref="HubElement" />
        /// </summary>
        private TThing hubElement;

        /// <summary>
        /// Backing field for <see cref="MappingDirection" />
        /// </summary>
        private MappingDirection mappingDirection;

        /// <summary>
        /// Backing field for <see cref="ShouldCreateNewTargetElement"/>
        /// </summary>
        private bool shouldCreateNewTargetElement;

        /// <summary>
        /// Initializes a new <see cref="MappedElementRowViewModel{TThing,TDstElement}" />
        /// </summary>
        /// <param name="thing">The <see cref="TThing" /></param>
        /// <param name="dstElement">The <see cref="TDstElement" /></param>
        /// <param name="mappingDirection">The <see cref="MappingDirection" /></param>
        protected MappedElementRowViewModel(TThing thing, TDstElement dstElement, MappingDirection mappingDirection)
        {
            this.HubElement = thing;
            this.DstElement = dstElement;
            this.MappingDirection = mappingDirection;
            this.RelationShips = new List<BinaryRelationship>();
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
        /// The <see cref="TDstElement" />
        /// </summary>
        public TDstElement DstElement
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
        /// A collection of <see cref="BinaryRelationship"/>
        /// </summary>
        public readonly List<BinaryRelationship> RelationShips;
    }
}
