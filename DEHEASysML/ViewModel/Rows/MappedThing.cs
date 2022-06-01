// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappedThing.cs" company="RHEA System S.A.">
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
    using CDP4Common.CommonData;

    using EA;

    using ReactiveUI;

    /// <summary>
    /// Represents either a <see cref="Thing" /> or <see cref="Element" />
    /// </summary>
    public class MappedThing : ReactiveObject
    {
        /// <summary>
        /// Backing field for <see cref="Name" />
        /// </summary>
        private string name;

        /// <summary>
        /// Backing field for <see cref="Value" />
        /// </summary>
        private object value;

        /// <summary>
        /// Initializes a new <see cref="MappedThing"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public MappedThing(string name, object value)
        {
            this.name = name;
            this.value = value;
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public object Value
        {
            get => this.value;
            set => this.RaiseAndSetIfChanged(ref this.value, value);
        }

        /// <summary>
        /// A collection of <see cref="MappedThing"/>
        /// </summary>
        public ReactiveList<MappedThing> ContainedRows { get; } = new();
    }
}
