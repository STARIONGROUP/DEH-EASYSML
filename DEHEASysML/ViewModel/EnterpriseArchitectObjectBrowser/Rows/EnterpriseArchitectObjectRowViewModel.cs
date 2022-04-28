// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectObjectRowViewModel.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.ViewModel.EnterpriseArchitectObjectBrowser.Rows
{
    using EA;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="EnterpriseArchitectObjectRowViewModel{TEAClass}"/> represents a <see cref="EnterpriseArchitectObjectBaseRowViewModel"/>
    /// with a dedicated object contained
    /// </summary>
    /// <typeparam name="TEaClass">Any class</typeparam>
    public abstract class EnterpriseArchitectObjectRowViewModel<TEaClass> : EnterpriseArchitectObjectBaseRowViewModel where TEaClass : class
    {
        /// <summary>
        /// Backing field for <see cref="RepresentedObject"/>
        /// </summary>
        private TEaClass representedObject;

        /// <summary>
        /// Backing field for <see cref="Parent" />
        /// </summary>
        private EnterpriseArchitectObjectBaseRowViewModel parent;

        /// <summary>
        /// Initializes a new <see cref="EnterpriseArchitectObjectBaseRowViewModel" />
        /// </summary>
        /// <param name="parent">The parent row</param>
        /// <param name="eaObject">The object to represent</param>
        protected EnterpriseArchitectObjectRowViewModel(EnterpriseArchitectObjectBaseRowViewModel parent, TEaClass eaObject)
        {
            this.Parent = parent;
            this.RepresentedObject = eaObject;
        }

        /// <summary>
        /// The object that the row represents
        /// </summary>
        public TEaClass RepresentedObject
        {
            get => this.representedObject;
            set => this.RaiseAndSetIfChanged(ref this.representedObject, value);
        }

        /// <summary>
        /// Gets or sets the parent of this row 
        /// </summary>
        public EnterpriseArchitectObjectBaseRowViewModel Parent
        {
            get => this.parent;
            set => this.RaiseAndSetIfChanged(ref this.parent, value);
        }

        /// <summary>
        /// Updates this view model properties;
        /// </summary>
        protected virtual void UpdateProperties()
        {
            switch (this.RepresentedObject)
            {
                case Package package:
                    this.Name = package.Name;
                    this.RowType = "Package";
                    break;
                case Element element:
                    this.Name = element.Name;
                    this.RowType = element.Stereotype;
                    break;
            }
        }
    }
}
