// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingRowViewModel.cs" company="RHEA System S.A.">
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
    using System;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;

    using DEHPCommon.Enumerators;

    using EA;

    using ReactiveUI;

    using Requirement = CDP4Common.EngineeringModelData.Requirement;

    /// <summary>
    /// Represents a row of mapped <see cref="IMappedElementRowViewModel" />
    /// </summary>
    public class MappingRowViewModel : ReactiveObject
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Backing field for <see cref="ArrowDirection" />
        /// </summary>
        private double arrowDirection;

        /// <summary>
        /// Backing field for <see cref="direction" />
        /// </summary>
        private MappingDirection direction;

        /// <summary>
        /// Initializes a new <see cref="MappingRowViewModel" />
        /// </summary>
        /// <param name="mappedElementDefinition">A <see cref="MappedElementRowViewModel{ElementDefinition}" /></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public MappingRowViewModel(MappedElementRowViewModel<ElementDefinition> mappedElementDefinition, IDstController dstController)
        {
            var elementDefinition = mappedElementDefinition.MappingDirection == MappingDirection.FromHubToDst
                ? mappedElementDefinition.HubElement
                : mappedElementDefinition.HubElement.Original as ElementDefinition ?? new ElementDefinition
                {
                    Name = mappedElementDefinition.HubElement.Name
                };

            this.dstController = dstController;
            this.UpdateProperties(mappedElementDefinition, elementDefinition);
        }

        /// <summary>
        /// Initializes a new <see cref="MappingRowViewModel" />
        /// </summary>
        /// <param name="mappedRequirement">A <see cref="MappedElementRowViewModel{Requirement}" /></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public MappingRowViewModel(MappedElementRowViewModel<Requirement> mappedRequirement, IDstController dstController)
        {
            var requirement = mappedRequirement.MappingDirection == MappingDirection.FromHubToDst
                ? mappedRequirement.HubElement 
                : mappedRequirement.HubElement.Original as Requirement ?? new Requirement
            {
                Name = mappedRequirement.HubElement.Name
            };

            this.dstController = dstController;
            this.UpdateProperties(mappedRequirement, requirement);
        }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public MappingDirection Direction
        {
            get => this.direction;
            set => this.RaiseAndSetIfChanged(ref this.direction, value);
        }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public double ArrowDirection
        {
            get => this.arrowDirection;
            set => this.RaiseAndSetIfChanged(ref this.arrowDirection, value);
        }

        /// <summary>
        /// Gets or sets the hub Thing
        /// </summary>
        public ReactiveList<MappedThing> HubThing { get; } = new();

        /// <summary>
        /// Gets or sets the dst element
        /// </summary>
        public ReactiveList<MappedThing> DstThing { get; } = new();

        /// <summary>
        /// Gets or sets the tooltip
        /// </summary>
        public string ToolTip { get; set; }

        /// <summary>
        /// Updates this row properties
        /// </summary>
        /// <param name="dstElement">The <see cref="Element" /></param>
        /// <param name="dstElementName">The display name of the <paramref name="dstElement" /></param>
        /// <param name="hubElement">The <see cref="Thing" /></param>
        /// <param name="hubElementName">The display name of the <paramref name="hubElement" /></param>
        private void UpdateProperties(Element dstElement, string dstElementName, Thing hubElement, string hubElementName)
        {
            this.DstThing.Add(new MappedThing(dstElementName, null));
            this.HubThing.Add(new MappedThing(hubElementName, null));

            this.InitializeHubThing(hubElement);
            this.InitializeDstThing(dstElement);

            switch (this.Direction)
            {
                case MappingDirection.FromHubToDst:
                    this.ArrowDirection = 180;
                    this.ToolTip = $"Mapping representation from {hubElementName} to {dstElementName}";
                    break;
                case MappingDirection.FromDstToHub:
                    this.ArrowDirection = 0;
                    this.ToolTip = $"Mapping representation from {dstElementName} to {hubElementName}";
                    break;
                default:
                    throw new InvalidOperationException("Unsupported value for the Mapping Direction");
            }
        }

        /// <summary>
        /// Update this viewmodel properties
        /// </summary>
        /// <typeparam name="TThing">A <see cref="Thing" /></typeparam>
        /// <param name="mappedElement">The <see cref="MappedElementRowViewModel{TThing}" /></param>
        /// <param name="thing">The <see cref="Thing" /> represented</param>
        private void UpdateProperties<TThing>(MappedElementRowViewModel<TThing> mappedElement, TThing thing) where TThing : Thing
        {
            this.Direction = mappedElement.MappingDirection;
            var dstName = this.Direction == MappingDirection.FromHubToDst ? mappedElement.TargetElementName : mappedElement.SourceElementName;
            var hubName = this.Direction == MappingDirection.FromHubToDst ? mappedElement.SourceElementName : mappedElement.TargetElementName;
            this.UpdateProperties(mappedElement.DstElement, dstName, thing, hubName);
        }

        /// <summary>
        /// Initializes the <see cref="DstThing" />
        /// </summary>
        /// <param name="dstElement">The <see cref="Element" /> to represents</param>
        private void InitializeDstThing(Element dstElement)
        {
            if (dstElement.HasStereotype(StereotypeKind.Block))
            {
                foreach (var valueProperty in dstElement.Elements.OfType<Element>().Where(x => this.dstController.IsValueProperty(x))
                             .Where(x => this.dstController.CreatedElements.All(created => created.ElementGUID != x.ElementGUID)))
                {
                    var value = valueProperty.GetValueOfPropertyValue();
                    var scale = valueProperty.GetUnitOfValueProperty();

                    Element unit = null;

                    if (scale != null)
                    {
                        unit = this.dstController.CurrentRepository.GetElementByGuid(scale.Value);
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        value = "-";
                    }

                    if (unit != null && !string.IsNullOrEmpty(unit.Name))
                    {
                        value += $" [{unit.Name}]";
                    }

                    this.DstThing.First().ContainedRows.Add(new MappedThing(valueProperty.Name, value));
                }
            }
            else if (dstElement.HasStereotype(StereotypeKind.Requirement))
            {
                this.DstThing.First().ContainedRows.Add(new MappedThing("Id", dstElement.GetRequirementId()));
                this.DstThing.First().ContainedRows.Add(new MappedThing("Text", dstElement.GetRequirementText()));
            }
        }

        /// <summary>
        /// Initializes the <see cref="HubThing" />
        /// </summary>
        /// <param name="hubElement">The <see cref="Thing" /> to represents</param>
        private void InitializeHubThing(Thing hubElement)
        {
            if (hubElement is ElementDefinition elementDefinition)
            {
                foreach (var parameter in elementDefinition.Parameter)
                {
                    var parameterMappedThing = new MappedThing(parameter.ParameterType.Name, null);

                    var scale = parameter.Scale?.ShortName;

                    if (parameter.IsOptionDependent || parameter.StateDependence != null)
                    {
                        foreach (var parameterValueSet in parameter.ValueSet)
                        {
                            var parameterValueSetName = this.GetValueSetRepresentation(parameterValueSet);
                            var value = parameterValueSet.ActualValue.FirstOrDefault() ?? "-";
                            value += $" [{scale}]";
                            parameterMappedThing.ContainedRows.Add(new MappedThing(parameterValueSetName, value));
                        }
                    }
                    else
                    {
                        parameterMappedThing.Value = parameter.QueryParameterBaseValueSet(null, null).ActualValue.FirstOrDefault() ?? "-";
                        parameterMappedThing.Value += $" [{scale}]";
                    }

                    this.HubThing.First().ContainedRows.Add(parameterMappedThing);
                }
            }
            else if (hubElement is Requirement requirement)
            {
                var definition = requirement.Definition.FirstOrDefault(x => x.LanguageCode == "en")
                                 ?? requirement.Definition.FirstOrDefault();

                var definitionValue = definition?.Content ?? "-";

                if (!string.IsNullOrEmpty(requirement.ShortName))
                {
                    this.HubThing.First().ContainedRows.Add(new MappedThing("ShortName", requirement.ShortName));
                    this.HubThing.First().ContainedRows.Add(new MappedThing("Definition", definitionValue));
                }
            }
        }

        /// <summary>
        /// Gets a representation of a <see cref="ParameterValueSet" />
        /// </summary>
        /// <param name="parameterValueSet">The <see cref="ParameterValueSet" /></param>
        /// <returns>The representation</returns>
        private string GetValueSetRepresentation(ParameterValueSet parameterValueSet)
        {
            return $"{(parameterValueSet.ActualOption is null ? string.Empty : $" Option: {parameterValueSet.ActualOption.Name}")} " +
                   $"{(parameterValueSet.ActualState is null ? string.Empty : $" State: {parameterValueSet.ActualState.Name} ")}";
        }
    }
}
