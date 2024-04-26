// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlockRowViewModel.cs" company="RHEA System S.A.">
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
    using System.Linq;

    using Autofac;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;

    using DEHPCommon;

    using EA;

    /// <summary>
    /// The <see cref="BlockRowViewModel" /> represents an <see cref="Element" /> of Stereotype Block
    /// </summary>
    public class BlockRowViewModel : ElementRowViewModel
    {
        /// <summary>
        /// Gets the injected <see cref="IDstController" />
        /// </summary>
        private IDstController dstController;

        /// <summary>
        /// Initializes a new <see cref="BlockRowViewModel" />
        /// </summary>
        /// <param name="parent">The parent row</param>
        /// <param name="eaObject">The object to represent</param>
        /// <param name="shouldShowEverything">
        /// A value asserting if the row should display its contained <see cref="Element" />
        /// </param>
        public BlockRowViewModel(EnterpriseArchitectObjectBaseRowViewModel parent, Element eaObject, bool shouldShowEverything)
            : base(parent, eaObject)
        {
            this.ShouldShowEverything = shouldShowEverything;
            this.Initialize();
        }

        /// <summary>
        /// Compute the current row to initializes properties
        /// </summary>
        public override void ComputeRow()
        {
            if (!this.ShouldShowEverything)
            {
                return;
            }

            using (this.ContainedRows.SuppressChangeNotifications())
            {
                var containedElements = this.RepresentedObject.Elements.OfType<Element>().ToList();
                this.UpdateContainedRowsOfStereotype(StereotypeKind.ValueProperty, containedElements.Where(this.dstController.IsValueProperty).ToList());
                this.UpdateContainedRowsOfStereotype(StereotypeKind.PartProperty, containedElements.Where(this.dstController.IsPartProperty).ToList());
                this.UpdateContainedRowsOfStereotype(StereotypeKind.Port, containedElements.Where(x => x.MetaType.AreEquals(StereotypeKind.Port)).ToList());
                this.ContainedRows.Sort(ContainedRowsComparer);
            }
        }

        /// <summary>
        /// Initializes this row properties
        /// </summary>
        private void Initialize()
        {
            this.dstController = AppContainer.Container.Resolve<IDstController>();
            this.UpdateProperties();
        }
    }
}
