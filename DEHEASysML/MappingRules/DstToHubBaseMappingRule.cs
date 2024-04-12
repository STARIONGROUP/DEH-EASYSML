// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstToHubBaseMappingRule.cs" company="RHEA System S.A.">
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
    using System;
    using System.Collections.Generic;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal.Operations;

    using DEHEASysML.Extensions;
    using DEHEASysML.Services.Cache;

    using EA;

    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// The <see cref="DstToHubBaseMappingRule{TInput,TOuput}" /> provides some methods common for all
    /// <see cref="MappingRules" /> from DST to Hub
    /// </summary>
    public abstract class DstToHubBaseMappingRule<TInput, TOuput> : CommonBaseMappingRule<TInput, TOuput>
    {
        /// <summary>
        /// The current <see cref="DomainOfExpertise" />
        /// </summary>
        protected DomainOfExpertise Owner;

        /// <summary>
        /// Gets the <see cref="ICacheService"/>
        /// </summary>
        protected ICacheService CacheService;

        /// <summary>
        /// Tries to create the category with the specified <paramref name="categoryNames" />
        /// </summary>
        /// <param name="categoryNames">The shortname and the name of the <see cref="Category" /></param>
        /// <param name="category">The category</param>
        /// <param name="permissibleClass">params of permissive class</param>
        /// <returns></returns>
        protected bool TryCreateCategory((string shortname, string name) categoryNames, out Category category, params ClassKind[] permissibleClass)
        {
            var (shortname, name) = categoryNames;

            var newCategory = new Category
            {
                Name = name,
                ShortName = shortname,
                Iid = Guid.NewGuid(),
                PermissibleClass = new List<ClassKind>(permissibleClass)
            };

            var rdl = this.HubController.GetDehpOrModelReferenceDataLibrary().Clone(false);
            rdl.DefinedCategory.Add(newCategory);
            return this.TryCreateReferenceDataLibraryThing(newCategory, rdl, ClassKind.Category, out category);
        }

        /// <summary>
        /// Tries to add a specified <see cref="Thing" /> to the provided <see cref="ReferenceDataLibrary" /> and retrieve the new
        /// reference from the cache after save
        /// </summary>
        /// <typeparam name="TThing">The Type of <see cref="Thing" /> to get</typeparam>
        /// <param name="newThing">The <see cref="Thing" /></param>
        /// <param name="clonedRdl">The cloned <see cref="ReferenceDataLibrary" /></param>
        /// <param name="classKind">The <see cref="ClassKind" /></param>
        /// <param name="thing">The out parameter</param>
        /// <returns>Asserts if the <paramref name="newThing" /> has been successfully created</returns>
        protected bool TryCreateReferenceDataLibraryThing<TThing>(TThing newThing, ReferenceDataLibrary clonedRdl, ClassKind classKind, out TThing thing) where TThing : Thing
        {
            try
            {
                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(clonedRdl), clonedRdl);
                transaction.CreateOrUpdate(clonedRdl);
                transaction.CreateOrUpdate(newThing);

                Task.Run(async () =>
                {
                    await this.HubController.Write(transaction);
                    await this.HubController.RefreshReferenceDataLibrary(clonedRdl);
                }).ContinueWith(task =>
                {
                    if (!task.IsCompleted)
                    {
                        this.Logger.Error($"Error during the task of the creation of {newThing.UserFriendlyName} because {task.Exception}");
                    }
                    else
                    {
                        this.Logger.Info($"Thing {newThing.UserFriendlyName} has been succesfully created");
                    }
                }).Wait();

                return this.HubController.TryGetThingById(newThing.Iid, classKind, out thing);
            }
            catch (Exception exception)
            {
                this.Logger.Error($"Could not create {newThing} because {exception}");
                thing = default;
                return false;
            }
        }

        /// <summary>
        /// Create a <see cref="BinaryRelationship" /> based
        /// </summary>
        /// <param name="source">The source of the <see cref="BinaryRelationship" /></param>
        /// <param name="target">The target of the <see cref="BinaryRelationship" /></param>
        /// <param name="relationShipName">The name of the <see cref="BinaryRelationship" /></param>
        /// <param name="categoryNames">The category name and shortname for the <see cref="BinaryRelationship" /></param>
        /// <param name="shouldAddCategory">Asserts if the <see cref="BinaryRelationship" /> will be categorize or not</param>
        /// <returns>The created <see cref="BinaryRelationship" /></returns>
        protected BinaryRelationship CreateBinaryRelationShip(Thing source, Thing target, string relationShipName,
            (string shortname, string name) categoryNames, bool shouldAddCategory = true)
        {
            var relationship = new BinaryRelationship
            {
                Iid = Guid.NewGuid(),
                Owner = this.Owner,
                Name = relationShipName,
                Source = source,
                Target = target
            };

            if (!shouldAddCategory)
            {
                return relationship;
            }

            if (this.HubController.TryGetThingBy(x => string.Equals(x.Name, categoryNames.name, StringComparison.InvariantCultureIgnoreCase)
                                                      && !x.IsDeprecated, ClassKind.Category, out Category category)
                || this.TryCreateCategory(categoryNames, out category, ClassKind.BinaryRelationship))
            {
                relationship.Category.Add(category);
            }

            return relationship;
        }

        /// <summary>
        /// Maps all Stereotypes applied to the <see cref="Element"/> to <see cref="Category"/> and adds all <see cref="Category"/>
        /// to the <see cref="ICategorizableThing"/>
        /// </summary>
        /// <param name="element">The <see cref="Element"/></param>
        /// <param name="thing">The <see cref="ICategorizableThing"/></param>
        protected void MapStereotypesToCategory(Element element, ICategorizableThing thing)
        {
            var stereotypes = element.GetStereotypeList().Split(',');

            foreach (var stereotype in stereotypes)
            {
                if (this.HubController.TryGetThingBy(x => string.Equals(x.Name, stereotype, StringComparison.InvariantCultureIgnoreCase)
                                                          && !x.IsDeprecated, ClassKind.Category, out Category category)
                    || this.TryCreateCategory((stereotype.GetShortName(), stereotype), out category, ((Thing)thing).ClassKind))
                {
                    if (!thing.Category.Exists(x => x.Iid == category.Iid))
                    {
                        thing.Category.Add(category);
                    }
                }
            }
        }
    }
}
