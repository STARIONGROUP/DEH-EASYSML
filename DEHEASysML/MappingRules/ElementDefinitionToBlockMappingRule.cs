// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ElementDefinitionToBlockMappingRule.cs" company="RHEA System S.A.">
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
    using System.Runtime.ExceptionServices;

    using Autofac;

    using DEHEASysML.DstController;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;

    /// <summary>
    /// The <see cref="ElementDefinitionToBlockMappingRule" /> is a <see cref="IMappingRule" /> for the
    /// <see cref="MappingEngine" />
    /// that takes a <see cref="List{T}" /> of <see cref="EnterpriseArchitectBlockElement" /> as input and
    /// outputs a E-TM-10-25 collection of <see cref="MappedElementDefinitionRowViewModel" />
    /// </summary>
    public class ElementDefinitionToBlockMappingRule : HubToDstBaseMappingRule<List<ElementDefinitionMappedElement>, List<MappedElementDefinitionRowViewModel>>
    {
        /// <summary>
        /// The collection of <see cref="EnterpriseArchitectBlockElement" />
        /// </summary>
        public List<ElementDefinitionMappedElement> Elements { get; private set; } = new();

        /// <summary>
        /// Transform a <see cref="List{T}" /> of <see cref="ElementDefinitionMappedElement" /> into a
        /// <see cref="List{T}" /> of
        /// <see cref="MappedRequirementRowViewModel" />
        /// </summary>
        /// <param name="input">
        /// A <see cref="List{T}" /> of <see cref="EnterpriseArchitectRequirementElement " />
        /// </param>
        /// <returns>A collection of <see cref="MappedRequirementRowViewModel" /></returns>
        public override List<MappedElementDefinitionRowViewModel> Transform(List<ElementDefinitionMappedElement> input)
        {
            try
            {
                if (!this.HubController.IsSessionOpen)
                {
                    return default;
                }

                this.MappingConfiguration = AppContainer.Container.Resolve<IMappingConfigurationService>();
                this.DstController = AppContainer.Container.Resolve<IDstController>();

                this.Elements = new List<ElementDefinitionMappedElement>(input);



                return new List<MappedElementDefinitionRowViewModel>(this.Elements);
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }
    }
}
