// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryRelationshipToEnterpriseArchitectConnectorMappingRule.cs" company="RHEA System S.A.">
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

    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;
    using DEHEASysML.Utils.Stereotypes;

    using DEHPCommon;
    using DEHPCommon.MappingRules.Core;

    using EA;

    /// <summary>
    /// The <see cref="BinaryRelationshipToEnterpriseArchitectConnectorMappingRule" /> is a
    /// <see cref="MappingRule{TInput,TOutput}" />
    /// that maps <see cref="BinaryRelationship" /> to <see cref="Connector" />
    /// </summary>
    public class BinaryRelationshipToEnterpriseArchitectConnectorMappingRule : HubToDstBaseMappingRule<List<HubRelationshipMappedElement>, List<Connector>>
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
        /// A collection of created <see cref="Connector" />
        /// </summary>
        private readonly List<Connector> result = new();

        /// <summary>
        /// Transforms collection of <see cref="HubRelationshipMappedElement" />
        /// to a collection of <see cref="Connector" />
        /// </summary>
        public override List<Connector> Transform(List<HubRelationshipMappedElement> input)
        {
            try
            {
                this.DstController = AppContainer.Container.Resolve<IDstController>();

                if (!this.HubController.IsSessionOpen || !this.DstController.IsFileOpen)
                {
                    return default;
                }

                this.result.Clear();

                this.Map(input);
                return new List<Connector>(this.result);
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Maps the provided collection of <see cref="HubRelationshipMappedElement" />
        /// </summary>
        /// <param name="elements">The collection of <see cref="HubRelationshipMappedElement" /></param>
        private void Map(List<HubRelationshipMappedElement> elements)
        {
            foreach (var element in elements)
            {
                var relationships = this.HubController.OpenIteration.Relationship
                    .OfType<BinaryRelationship>()
                    .Where(x => x.Source.Iid == element.HubElement.Iid && x.Category.Any(category =>
                        string.Equals(category.Name, this.satisfyCategoryNames.name, StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals(category.Name, this.traceCategoryNames.name, StringComparison.InvariantCultureIgnoreCase)));

                foreach (var relationship in relationships)
                {
                    var targetElement = elements.FirstOrDefault(x => x.HubElement.Iid == relationship.Target.Iid);

                    var connectorStereotype = relationship.Category.Any(x => string.Equals(x.Name, this.satisfyCategoryNames.name, StringComparison.InvariantCultureIgnoreCase))
                        ? StereotypeKind.Satisfy
                        : StereotypeKind.Trace;

                    if (targetElement == null || this.DoesConnectorAlreadyExist(connectorStereotype, element.DstElement, targetElement.DstElement))
                    {
                        continue;
                    }

                    var connector = element.DstElement.Connectors.AddNew("", StereotypeKind.Abstraction.ToString()) as Connector;
                    connector.StereotypeEx = connectorStereotype.ToString();
                    connector.ClientID = element.DstElement.ElementID;
                    connector.SupplierID = targetElement.DstElement.ElementID;
                    connector.Update();
                    this.result.Add(connector);
                }
            }
        }

        /// <summary>
        /// Verifies if any <see cref="Connector" /> already correspond to the <see cref="BinaryRelationship" />
        /// </summary>
        /// <param name="connectorStereotype">The <see cref="StereotypeKind" /> of the <see cref="Connector" /></param>
        /// <param name="source">The source <see cref="Element" /></param>
        /// <param name="target">The target <see cref="Element" /></param>
        /// <returns>A value indicating if the <see cref="Connector" /> already exists</returns>
        private bool DoesConnectorAlreadyExist(StereotypeKind connectorStereotype, Element source, Element target)
        {
            bool Predicate(Connector connector)
            {
                return connector.Stereotype.AreEquals(connectorStereotype)
                       && connector.ClientID == source.ElementID && connector.SupplierID == target.ElementID;
            }

            return this.result.Any(Predicate) || source.GetAllConnectorsOfElement().Any(Predicate);
        }
    }
}
