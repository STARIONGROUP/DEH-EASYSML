// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubToDstBaseMappingRule.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.MappingRules
{
    using System.Linq;

    using CDP4Common.SiteDirectoryData;

    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;

    using EA;

    /// <summary>
    /// The <see cref="HubToDstBaseMappingRule{TInput,TOuput}" /> provides some methods common for all
    /// <see cref="MappingRules" /> from Hub to Dst
    /// </summary>
    public abstract class HubToDstBaseMappingRule<TInput, TOuput> : CommonBaseMappingRule<TInput, TOuput>
    {
        /// <summary>
        /// Maps all <see cref="Category"/> of the <see cref="ICategorizableThing"/> to stereotype for the <see cref="Element"/>
        /// </summary>
        /// <param name="element">The <see cref="Element"/></param>
        /// <param name="thing">The <see cref="ICategorizableThing"/></param>
        protected void MapCategoriesToStereotype(Element element, ICategorizableThing thing)
        {
            var categories = thing.Category.Where(x => !x.IsDeprecated).Select(x => x.Name).ToList();

            var hasDefaultStereotype = false;

            for (var categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
            {
                if (categories[categoryIndex].AreEquals(StereotypeKind.Block))
                {
                    categories[categoryIndex] = StereotypeKind.Block.GetFQStereotype();
                    hasDefaultStereotype = true;
                }

                if (categories[categoryIndex].AreEquals(StereotypeKind.Requirement))
                {
                    categories[categoryIndex] = StereotypeKind.Requirement.GetFQStereotype();
                    hasDefaultStereotype = true;
                }
            }

            if (categories.Count == 1 && hasDefaultStereotype)
            {
                return;
            }

            var stereotypesToApply = string.Join(",", categories);

            if (!string.IsNullOrEmpty(stereotypesToApply) && element.StereotypeEx != stereotypesToApply)
            {
                this.DstController.UpdatedStereotypes[element.ElementGUID] = stereotypesToApply;
            }
        }
    }
}
