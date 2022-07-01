// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappedParameterRowViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;

    using EA;

    using ReactiveUI;

    using Parameter = CDP4Common.EngineeringModelData.Parameter;

    /// <summary>
    /// The <see cref="MappedParameterRowViewModel" /> row view model represents a <see cref="Parameter"/> and
    /// is used to select an <see cref="ActualFiniteState"/> when the <see cref="Parameter"/> is state depend
    /// </summary>
    public class MappedParameterRowViewModel : MappedElementRowViewModel<Parameter>
    {
        /// <summary>
        /// Backing field for <see cref="SelectedActualFiniteState"/>
        /// </summary>
        private ActualFiniteState selectedActualFiniteState;

        /// <summary>
        /// Initializes a new <see cref="MappedParameterRowViewModel" />
        /// </summary>
        /// <param name="thing">The <see cref="Parameter" /></param>
        /// <param name="dstElement">The <see cref="Element" /></param>
        /// <param name="mappingDirection">The <see cref="MappedElementRowViewModel{TThing}.MappingDirection" /></param>
        public MappedParameterRowViewModel(Parameter thing, Element dstElement, MappingDirection mappingDirection) : base(thing, dstElement, mappingDirection)
        {
            this.ShouldDisplayArrowAndIcons = false;
            this.SourceElementName = thing.ParameterType.Name;

            if (thing.StateDependence != null)
            {
                this.AvailableActualFiniteStates.AddRange(thing.StateDependence.ActualState);
                this.SelectedActualFiniteState = this.AvailableActualFiniteStates[0];
                this.ShoulDisplayComboBox = true;
            }
        }

        /// <summary>
        /// The current <see cref="ActualFiniteState"/>
        /// </summary>
        public ActualFiniteState SelectedActualFiniteState
        {
            get => this.selectedActualFiniteState;
            set
            {
                if (value != null)
                {
                    this.RaiseAndSetIfChanged(ref this.selectedActualFiniteState, value);
                }
            } 
        }

        /// <summary>
        /// A collection of all available <see cref="ActualFiniteState"/>
        /// </summary>
        public ReactiveList<ActualFiniteState> AvailableActualFiniteStates { get; } = new();
    }
}
