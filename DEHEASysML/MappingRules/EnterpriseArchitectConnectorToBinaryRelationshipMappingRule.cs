// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectConnectorToBinaryRelationshipMappingRule.cs" company="RHEA System S.A.">
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
    using System.Linq;
    using System.Runtime.ExceptionServices;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;
    using DEHEASysML.Utils.Stereotypes;

    using DEHPCommon;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;

    /// <summary>
    /// The <see cref="EnterpriseArchitectConnectorToBinaryRelationshipMappingRule" /> is a <see cref="IMappingRule" /> for the
    /// <see cref="MappingEngine" />
    /// that takes a <see cref="List{T}" /> of <see cref="EnterpriseArchitectTracableMappedElement" /> as input and
    /// outputs a E-TM-10-25 collection of <see cref="BinaryRelationship" />
    /// </summary>
    public class EnterpriseArchitectConnectorToBinaryRelationshipMappingRule : DstToHubBaseMappingRule<List<EnterpriseArchitectTracableMappedElement>, List<BinaryRelationship>>
    {
        /// <summary>
        /// The category for trace/BinaryRelationship names where Item1 is the short name
        /// </summary>
        private readonly (string shortname, string name) traceCategoryNames = ("trace", "trace");

        /// <summary>
        /// The category for satisfy/BinaryRelationship names where Item1 is the short name
        /// </summary>
        private readonly (string shortname, string name) satisfyCategoryNames = ("satisfy", "satisfy");

        /// <summary>
        /// A collection of <see cref="BinaryRelationship" /> created during the mapping
        /// </summary>
        private readonly List<BinaryRelationship> result = new();

        /// <summary>
        /// Transforms a collection of <see cref="EnterpriseArchitectTracableMappedElement" /> to a
        /// collection of <see cref="BinaryRelationship" />
        /// </summary>
        public override List<BinaryRelationship> Transform(List<EnterpriseArchitectTracableMappedElement> input)
        {
            try
            {
                if (!this.HubController.IsSessionOpen)
                {
                    return default;
                }

                this.result.Clear();

                this.Owner = this.HubController.CurrentDomainOfExpertise;

                this.DstController = AppContainer.Container.Resolve<IDstController>();

                this.Map(input);

                return new List<BinaryRelationship>(this.result);
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Map the provided collection of <see cref="EnterpriseArchitectTracableMappedElement" /> to creates
        /// <see cref="BinaryRelationship" />s
        /// </summary>
        /// <param name="elements">A collection of <see cref="EnterpriseArchitectTracableMappedElement" /></param>
        private void Map(List<EnterpriseArchitectTracableMappedElement> elements)
        {
            foreach (var element in elements)
            {
                foreach (var connector in element.DstElement.GetAllConnectorsOfElement()
                             .Where(x => x.ClientID == element.DstElement.ElementID &&
                                         (x.Stereotype.AreEquals(StereotypeKind.Trace) || x.Stereotype.AreEquals(StereotypeKind.Satisfy))))
                {
                    var existingSupplierMappedElement = elements.FirstOrDefault(x =>
                        x.DstElement.ElementID == connector.SupplierID);

                    if (existingSupplierMappedElement == null)
                    {
                        continue;
                    }

                    var matchingCategory = connector.Stereotype.AreEquals(StereotypeKind.Trace)
                        ? this.traceCategoryNames
                        : this.satisfyCategoryNames;

                    if (this.DoesRelationshipAlreadyExists(matchingCategory, element.HubElement, existingSupplierMappedElement.HubElement))
                    {
                        continue;
                    }

                    var relationShipName = $"{element.DstElement.Name} → {existingSupplierMappedElement.DstElement.Name}";

                    this.result.Add(this.CreateBinaryRelationShip(element.HubElement, existingSupplierMappedElement.HubElement,
                        relationShipName, matchingCategory));
                }
            }
        }

        /// <summary>
        /// Verifies that no relationship already exists bewteen the provided the
        /// <param name="source"></param>
        /// and
        /// <param name="target"></param>
        /// </summary>
        /// <param name="categoryNames">The shortname and the name of the <see cref="Category" /></param>
        /// <param name="source">The source <see cref="Thing" /></param>
        /// <param name="target">The target <see cref="Thing" /></param>
        /// <returns>A value indicating if a relationship already exists bewteen the
        /// <param name="source"></param>
        /// and
        /// <param name="target"></param>
        /// </returns>
        private bool DoesRelationshipAlreadyExists((string shortname, string name) categoryNames, Thing source, Thing target)
        {
            bool Predicate(BinaryRelationship relationship)
            {
                return relationship.Source.Iid == source.Iid
                       && relationship.Target.Iid == target.Iid
                       && relationship.Category
                           .Any(x => string.Equals(x.Name, categoryNames.name, StringComparison.InvariantCultureIgnoreCase));
            }

            return this.result.Any(Predicate)
                   || this.HubController.OpenIteration.Relationship.OfType<BinaryRelationship>().Any(Predicate)
                   || this.DstController.MappedConnectorsToBinaryRelationships.Any(Predicate);
        }
    }
}
