// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlockDefinitionToElementDefinitionMappingRule.cs" company="RHEA System S.A.">
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
    using CDP4Common.Types;

    using DEHEASysML.DstController;
    using DEHEASysML.Extensions;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;

    using EA;

    using Parameter = CDP4Common.EngineeringModelData.Parameter;

    /// <summary>
    /// The <see cref="BlockDefinitionToElementDefinitionMappingRule" /> is a <see cref="IMappingRule" /> for the
    /// <see cref="MappingEngine" />
    /// that takes a <see cref="List{T}" /> of <see cref="EnterpriseArchitectBlockElement" /> as input and outputs a E-TM-10-25
    /// <see cref="ElementDefinition" />
    /// </summary>
    public class BlockDefinitionToElementDefinitionMappingRule :
        DstToHubBaseMappingRule<(bool completeMapping, List<EnterpriseArchitectBlockElement> elements), List<MappedElementDefinitionRowViewModel>>

    {
        /// <summary>
        /// The string that specifies the <see cref="ElementDefinition" /> representing ports
        /// </summary>
        private const string PortElementDefinitionName = "Port";

        /// <summary>
        /// The category for interface/BinaryRelationship names where Item1 is the short name
        /// </summary>
        private readonly (string shortname, string name) interfaceCategoryNames = ("interface", "interface");

        /// <summary>
        /// The isAbstract category names where Item1 is the short name
        /// </summary>
        private readonly (string shortname, string name) isAbstractCategoryNames = ("ABS", "isAbstract");

        /// <summary>
        /// The isActive category names where Item1 is the short name
        /// </summary>
        private readonly (string shortname, string name) isActiveCategoryNames = ("ACT", "isActive");

        /// <summary>
        /// The isEncapsulated category names where Item1 is the short name
        /// </summary>
        private readonly (string shortname, string name) isEncapsulatedCategoryNames = ("ENC", "isEncapsulated");

        /// <summary>
        /// The isLeaf category names where Item1 is the short name
        /// </summary>
        private readonly (string shortname, string name) isLeafCategoryNames = ("LEA", "isLeaf");

        /// <summary>
        /// Collection containing relation for a Port, used for the creation of future <see cref="BinaryRelationship" />
        /// </summary>
        private readonly List<(Element, EnterpriseArchitectBlockElement, ElementUsage)> portsToConnect = new();

        /// <summary>
        /// The <see cref="ElementDefinition" /> the represents the ports
        /// </summary>
        private ElementDefinition portElementDefinition;

        /// <summary>
        /// The collection of <see cref="EnterpriseArchitectBlockElement" />
        /// </summary>
        public List<EnterpriseArchitectBlockElement> Elements { get; private set; } = new();

        /// <summary>
        /// Transform a <see cref="List{T}" /> of <see cref="EnterpriseArchitectBlockElement" /> into a <see cref="List{T}" /> of
        /// <see cref="MappedElementDefinitionRowViewModel" />
        /// </summary>
        /// <param name="input">Tuple of <see cref="bool"/>, The <see cref="List{T}" /> of <see cref="EnterpriseArchitectBlockElement " />
        /// The <see cref="bool"/> handles the fact that it the mapping has to map everything</param>
        /// <returns>A collection of <see cref="MappedElementDefinitionRowViewModel" /></returns>
        public override List<MappedElementDefinitionRowViewModel> Transform((bool completeMapping, List<EnterpriseArchitectBlockElement> elements) input)
        {
            try
            {
                var (completeMapping, elements) = input;

                if (!this.HubController.IsSessionOpen || elements == null)
                {
                    return default;
                }

                this.DstController = AppContainer.Container.Resolve<IDstController>();

                this.Elements = new List<EnterpriseArchitectBlockElement>(elements);

                foreach (var mappedElement in this.Elements.ToList())
                {
                    mappedElement.HubElement ??= this.GetOrCreateElementDefinition(mappedElement.DstElement);
                    mappedElement.ShouldCreateNewTargetElement = mappedElement.HubElement.Original == null;

                    if (completeMapping)
                    {
                        this.MapElement(mappedElement);
                    }
                }

                if (completeMapping)
                {
                    this.MapPorts();
                    this.ProcessInterfaces();
                }

                return new List<MappedElementDefinitionRowViewModel>(this.Elements);
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Gets an existing or creates an <see cref="ElementDefinition" /> that will be mapped to the <see cref="Element" />
        /// </summary>
        /// <param name="blockElement">The Bloc <see cref="Element" /></param>
        /// <returns>An <see cref="ElementDefinition" /></returns>
        public ElementDefinition GetOrCreateElementDefinition(Element blockElement)
        {
            return this.GetOrCreateElementDefinition(blockElement.Name);
        }

        /// <summary>
        /// Creates the <see cref="BinaryRelationship" /> thats connects ports between each others
        /// </summary>
        public void ProcessInterfaces()
        {
            foreach (var (port, element, elementUsage) in this.portsToConnect)
            {
                var (portBlock, interfaceBlock) = this.DstController.ResolvePort(port);

                if (interfaceBlock == null || portBlock == null)
                {
                    continue;
                }

                var elementUsageName = $"{interfaceBlock.Name}_Impl";

                var interfaceElementUsage = this.portsToConnect
                    .FirstOrDefault(x => x.Item2.DstElement.Name == elementUsageName).Item3;

                if (interfaceElementUsage == null)
                {
                    interfaceElementUsage = this.HubController.OpenIteration.Element.SelectMany(x => x.ContainedElement)
                        .FirstOrDefault(x => x.Name == elementUsageName);

                    if (interfaceElementUsage == null)
                    {
                        continue;
                    }
                }

                var relationShip = this.HubController.OpenIteration.Relationship.OfType<BinaryRelationship>()
                    .FirstOrDefault(x => x.Name == interfaceBlock.Name && x.Target.Iid
                        == interfaceElementUsage.Iid && x.Source.Iid == elementUsage.Iid)?
                    .Clone(false) ?? this.CreateBinaryRelationShip(elementUsage, interfaceElementUsage,
                    interfaceBlock.Name, this.interfaceCategoryNames);

                element.RelationShips.Add(relationShip);
            }
        }

        /// <summary>
        /// Maps the attached ports of all mapped <see cref="Element" />
        /// </summary>
        public void MapPorts()
        {
            foreach (var element in this.Elements)
            {
                this.MapPorts(element);
            }
        }

        /// <summary>
        /// Maps all PartProperties of a Bloc
        /// </summary>
        /// <param name="elementDefinition">The <see cref="ElementDefinition" /></param>
        /// <param name="element">The block element</param>
        public void MapPartProperties(ElementDefinition elementDefinition, Element element)
        {
            foreach (var partProperty in element.GetAllPartPropertiesOfElement())
            {
                this.MapPartProperty(elementDefinition, partProperty);
            }
        }

        /// <summary>
        /// Maps the properties of the specified block
        /// </summary>
        /// <param name="elementDefinition">The <see cref="ElementDefinition" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        public void MapProperties(ElementDefinition elementDefinition, Element element)
        {
            if (elementDefinition == null)
            {
                return;
            }

            foreach (var property in element.GetAllValuePropertiesOfElement())
            {
                var existingParameter = elementDefinition.Parameter.FirstOrDefault(x =>
                    string.Equals(x.ParameterType.ShortName, property.GetShortName(), StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(x.ParameterType.Name, property.Name, StringComparison.InvariantCultureIgnoreCase));

                var valueOfProperty = property.GetValueOfPropertyValue();

                Parameter parameter;

                if (existingParameter == null)
                {
                    if (this.TryCreateParameterType(property, valueOfProperty, out var parameterType))
                    {
                        parameter = new Parameter
                        {
                            Iid = Guid.NewGuid(),
                            Owner = this.Owner,
                            ParameterType = parameterType
                        };

                        elementDefinition.Parameter.Add(parameter);

                        if (parameterType is QuantityKind quantityKind)
                        {
                            parameter.Scale = quantityKind.DefaultScale;
                        }
                    }
                    else
                    {
                        this.Logger.Error($"Could not create the ParameterType {property.Name}");
                        continue;
                    }
                }
                else
                {
                    parameter = existingParameter.Clone(true);
                }

                this.UpdateValueSet(parameter, valueOfProperty);
            }
        }

        /// <summary>
        /// Map the proper <see cref="Category" /> to the associated Hub Element of the provided
        /// <see cref="EnterpriseArchitectBlockElement" />
        /// </summary>
        /// <param name="elementDefinition">The <see cref="ElementDefinition" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        public void MapCategories(ElementDefinition elementDefinition, Element element)
        {
            if (elementDefinition == null)
            {
                return;
            }

            var isEncapsulated = false;

            var isEncapsulatedValue = element.GetValueOfPropertyValueOfElement(this.isEncapsulatedCategoryNames.Item2);

            if (!string.IsNullOrEmpty(isEncapsulatedValue))
            {
                isEncapsulated = isEncapsulatedValue == "1" || string.Equals(bool.TrueString, isEncapsulatedValue, StringComparison.InvariantCultureIgnoreCase);
            }

            this.MapCategory(elementDefinition, this.isLeafCategoryNames, element.IsLeaf, false);
            this.MapCategory(elementDefinition, this.isAbstractCategoryNames, element.Abstract == "1", true);
            this.MapCategory(elementDefinition, this.isActiveCategoryNames, element.IsActive, false);
            this.MapCategory(elementDefinition, this.isEncapsulatedCategoryNames, isEncapsulated, true);
        }

        /// <summary>
        /// Maps all ports of an <see cref="Element" />
        /// </summary>
        /// <param name="element">The <see cref="EnterpriseArchitectBlockElement" /></param>
        private void MapPorts(EnterpriseArchitectBlockElement element)
        {
            foreach (var port in element.DstElement.GetAllPortsOfElement())
            {
                var (portBlock, interfaceElement) = this.DstController.ResolvePort(port);

                var interfaceEndKind = InterfaceEndKind.OUTPUT;

                if (portBlock == null && interfaceElement != null)
                {
                    portBlock = interfaceElement;
                    interfaceEndKind = InterfaceEndKind.INPUT;
                }

                if (portBlock == null || element.HubElement.ContainedElement.Any(x => x.Name == portBlock.Name))
                {
                    continue;
                }

                var elementUsage = new ElementUsage
                {
                    Name = portBlock.Name,
                    ShortName = portBlock.GetShortName(),
                    Iid = Guid.NewGuid(),
                    Owner = this.Owner,
                    ElementDefinition = this.GetPortElementDefinition(),
                    InterfaceEnd = interfaceEndKind
                };

                element.HubElement.ContainedElement.Add(elementUsage);
                this.portsToConnect.Add((port, element, elementUsage));
            }
        }

        /// <summary>
        /// Gets the <see cref="ElementDefinition" /> that represents all ports
        /// </summary>
        /// <returns>The <see cref="ElementDefinition" /></returns>
        private ElementDefinition GetPortElementDefinition()
        {
            if (this.portElementDefinition != null)
            {
                return this.portElementDefinition;
            }

            this.portElementDefinition = this.GetOrCreateElementDefinition(PortElementDefinitionName);
            return this.portElementDefinition;
        }

        /// <summary>
        /// Maps an Element with all Properties and Categories
        /// </summary>
        /// <param name="mappedElement">The <see cref="EnterpriseArchitectBlockElement" /></param>
        private void MapElement(EnterpriseArchitectBlockElement mappedElement)
        {
            this.MapCategories(mappedElement.HubElement, mappedElement.DstElement);
            this.MapProperties(mappedElement.HubElement, mappedElement.DstElement);
            this.MapPartProperties(mappedElement.HubElement, mappedElement.DstElement);
        }

        /// <summary>
        /// Maps a PartProperty
        /// </summary>
        /// <param name="container">The <see cref="ElementDefinition" /> container </param>
        /// <param name="partProperty">The PartProperty</param>
        private void MapPartProperty(ElementDefinition container, Element partProperty)
        {
            if (container == null)
            {
                return;
            }

            var partPropertyBlock = this.DstController.CurrentRepository.GetElementByID(partProperty.PropertyType);
            var mappedElement = this.Elements.FirstOrDefault(x => x.DstElement.ElementGUID == partProperty.ElementGUID);

            if (mappedElement == null)
            {
                mappedElement = new EnterpriseArchitectBlockElement(this.GetOrCreateElementDefinition(partPropertyBlock), partPropertyBlock, MappingDirection.FromDstToHub);
                this.Elements.Add(mappedElement);
            }

            this.MapElement(mappedElement);

            if (container.ContainedElement.Any(x => x.ElementDefinition.Iid == mappedElement.HubElement.Iid))
            {
                return;
            }

            var elementUsage = new ElementUsage
            {
                Name = mappedElement.HubElement.Name,
                ShortName = mappedElement.HubElement.ShortName,
                Iid = Guid.NewGuid(),
                Owner = this.Owner,
                ElementDefinition = mappedElement.HubElement
            };

            container.ContainedElement.Add(elementUsage);
        }

        /// <summary>
        /// Update the correct value set depened of the selected <paramref name="valueOfProperty" />
        /// </summary>
        /// <param name="parameter">The <see cref="Parameter" /> to update the value set</param>
        /// <param name="valueOfProperty">The new value</param>
        private void UpdateValueSet(Parameter parameter, string valueOfProperty)
        {
            ParameterValueSet valueSet;

            if (parameter.Original != null || parameter.ValueSet.Any())
            {
                valueSet = (ParameterValueSet)parameter.QueryParameterBaseValueSet(null, null);
            }
            else
            {
                valueSet = new ParameterValueSet
                {
                    Iid = Guid.NewGuid(),
                    Reference = new ValueArray<string>(),
                    Formula = new ValueArray<string>(),
                    Published = new ValueArray<string>(),
                    Computed = new ValueArray<string>()
                };

                parameter.ValueSet.Add(valueSet);
            }

            valueSet.ValueSwitch = ParameterSwitchKind.MANUAL;
            var valueToSet = string.IsNullOrEmpty(valueOfProperty) ? "-" : valueOfProperty;
            valueSet.Manual = new ValueArray<string>(new[] { FormattableString.Invariant($"{valueToSet}") });
        }

        /// <summary>
        /// Tries to create a <see cref="ParameterType" /> if it does not exist yet in the chain of rdls
        /// </summary>
        /// <param name="property">The ValueProperty</param>
        /// <param name="valueOfProperty">The value of the ValueProperty</param>
        /// <param name="parameterType">The created <see cref="ParameterType" /></param>
        /// <returns>Asserts if the <see cref="ParameterType" /> has been successfully created</returns>
        private bool TryCreateParameterType(Element property, string valueOfProperty, out ParameterType parameterType)
        {
            try
            {
                var shortName = property.GetShortName();

                if (!this.HubController.TryGetThingBy(x => 
                            !x.IsDeprecated && 
                            (string.Equals(x.ShortName, shortName, StringComparison.InvariantCultureIgnoreCase)
                             || string.Equals(x.Name, property.Name, StringComparison.InvariantCultureIgnoreCase))
                             , ClassKind.ParameterType, out parameterType))
                {
                    ParameterType newParameterType;

                    if (decimal.TryParse(valueOfProperty, out _) ||
                        property.GetUnitOfValueProperty()?.Value != null &&
                        this.DstController.CurrentRepository.GetElementByGuid(property.GetUnitOfValueProperty().Value) != null)
                    {
                        newParameterType = new SimpleQuantityKind();

                        var quantityKind = (SimpleQuantityKind)newParameterType;

                        if (this.TryCreateOrGetMeasurementScale(property, out var scale))
                        {
                            quantityKind.AllPossibleScale.Add(scale);
                            quantityKind.DefaultScale = scale;
                            quantityKind.PossibleScale.Add(scale);
                        }
                    }
                    else if (bool.TryParse(valueOfProperty, out _))
                    {
                        newParameterType = new BooleanParameterType();
                    }
                    else
                    {
                        newParameterType = new TextParameterType();
                    }

                    newParameterType.Iid = Guid.NewGuid();
                    newParameterType.Name = property.Name;
                    newParameterType.ShortName = shortName;
                    newParameterType.Symbol = shortName.Substring(0, 1);

                    var rdl = this.HubController.GetDehpOrModelReferenceDataLibrary().Clone(false);
                    rdl.ParameterType.Add(newParameterType);
                    return this.TryCreateReferenceDataLibraryThing(newParameterType, rdl, ClassKind.ParameterType, out parameterType);
                }

                return true;
            }
            catch (Exception exception)
            {
                this.Logger.Error($"Could not create the parameter type of the property {property.Name} : {exception}");
                parameterType = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to create a new <see cref="MeasurementScale" /> based on the provided ValueProperty
        /// </summary>
        /// <param name="property">The ValueProperty</param>
        /// <param name="scale">The <see cref="MeasurementScale" /></param>
        /// <returns>Asserts if the <see cref="MeasurementScale" /> has been successfully created</returns>
        private bool TryCreateOrGetMeasurementScale(Element property, out MeasurementScale scale)
        {
            var taggedValue = property.GetUnitOfValueProperty();

            string unitName;
            string scaleShortName;

            ElementClass unit = null;

            if (taggedValue != null)
            {
                unit = this.DstController.CurrentRepository.GetElementByGuid(taggedValue.Value) as ElementClass;
            }

            if (unit == null || string.IsNullOrEmpty(unit.Name))
            {
                unitName = "1";
                scaleShortName = "-";
            }
            else
            {
                unitName = unit.Name;
                scaleShortName = unit.Name.Replace(" ", string.Empty);
            }

            if (!this.HubController.TryGetThingBy(x => !x.IsDeprecated
                                                       && string.Equals(x.ShortName, scaleShortName), ClassKind.MeasurementScale, out scale))
            {
                var newScale = new RatioScale
                {
                    Name = unitName,
                    NumberSet = NumberSetKind.REAL_NUMBER_SET
                };

                if (!this.TryCreateOrGetMeasurementUnit(unitName, scaleShortName, out var measurementUnit))
                {
                    return false;
                }

                newScale.Unit = measurementUnit;
                newScale.ShortName = measurementUnit.ShortName;

                var rdl = this.HubController.GetDehpOrModelReferenceDataLibrary().Clone(false);
                rdl.Scale.Add(newScale);
                this.TryCreateReferenceDataLibraryThing(newScale, rdl, ClassKind.MeasurementScale, out scale);
            }

            return true;
        }

        /// <summary>
        /// Tries to create a new <see cref="MeasurementUnit" /> based on the provided <paramref name="unitName" />
        /// </summary>
        /// <param name="unitName">The unit name</param>
        /// <param name="unitShortName">The shortname of the unit</param>
        /// <param name="measurementUnit">The <see cref="MeasurementUnit" /></param>
        /// <returns>Asserts if the <see cref="MeasurementUnit" /> has been successfully created</returns>
        private bool TryCreateOrGetMeasurementUnit(string unitName, string unitShortName, out MeasurementUnit measurementUnit)
        {
            if (!this.HubController.TryGetThingBy(x => !x.IsDeprecated &&
                                                       (x.ShortName == unitShortName
                                                        || x.Name == unitName
                                                        || x.ShortName == "-"), ClassKind.MeasurementUnit, out measurementUnit))
            {
                var newMeasurementUnit = new SimpleUnit
                {
                    Name = unitName,
                    ShortName = unitShortName
                };

                var rdl = this.HubController.GetDehpOrModelReferenceDataLibrary().Clone(false);
                rdl.Unit.Add(newMeasurementUnit);
                return this.TryCreateReferenceDataLibraryThing(newMeasurementUnit, rdl, ClassKind.MeasurementUnit, out measurementUnit);
            }

            return true;
        }

        /// <summary>
        /// Maps the specified <see cref="Category" /> to the provided <see cref="ElementDefinition" />
        /// </summary>
        /// <param name="elementDefinition">The <see cref="ElementDefinition" /></param>
        /// <param name="categoryNames">The shortname and the name of the <see cref="Category" /></param>
        /// <param name="value">The <see cref="bool" /> value from the SysML Bloc</param>
        /// <param name="shouldCreateCategory">Asserts if the <see cref="Category" /> should be created if it doesn't exist yet</param>
        private void MapCategory(ElementDefinition elementDefinition, (string shortname, string name) categoryNames, bool value, bool shouldCreateCategory)
        {
            try
            {
                if (!this.HubController.TryGetThingBy(x => !x.IsDeprecated
                                                           && x.ShortName == categoryNames.shortname, ClassKind.Category, out Category category)
                    && shouldCreateCategory && !this.TryCreateCategory(categoryNames, out category, ClassKind.ElementDefinition, ClassKind.ElementUsage))
                {
                    return;
                }

                if (category != null)
                {
                    if (value)
                    {
                        elementDefinition.Category.Add(category);
                    }
                    else
                    {
                        elementDefinition.Category.Remove(category);
                    }
                }
                else
                {
                    this.Logger.Error($"The Category {categoryNames.Item1} could not be found or created for the element {elementDefinition.Name}");
                }
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
            }
        }

        /// <summary>
        /// Gets an existing or creates an <see cref="ElementDefinition" /> that will be mapped to the <see cref="Element" />
        /// </summary>
        /// <param name="dstElementName">The name of the <see cref="Element" /></param>
        /// <returns>The <see cref="ElementDefinition" /></returns>
        private ElementDefinition GetOrCreateElementDefinition(string dstElementName)
        {
            var shortName = dstElementName.GetShortName();

            var elementDefinition = this.Elements.FirstOrDefault(x =>
                                        x.HubElement != null && string.Equals(x.HubElement.ShortName, shortName, StringComparison.CurrentCultureIgnoreCase))?.HubElement
                                    ?? this.HubController.OpenIteration.Element
                                        .FirstOrDefault(x => string.Equals(x.ShortName, shortName, StringComparison.CurrentCultureIgnoreCase))
                                        ?.Clone(true);

            if (elementDefinition is null)
            {
                elementDefinition = new ElementDefinition
                {
                    Iid = Guid.NewGuid(),
                    Owner = this.Owner,
                    Name = dstElementName,
                    ShortName = shortName,
                    Container = this.HubController.OpenIteration
                };
            }

            return elementDefinition;
        }
    }
}
