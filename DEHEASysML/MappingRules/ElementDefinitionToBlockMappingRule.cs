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
    using System.Linq;
    using System.Runtime.ExceptionServices;

    using Autofac;

    using CDP4Common;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;
    using DEHEASysML.Services.MappingConfiguration;
    using DEHEASysML.Utils.Stereotypes;
    using DEHEASysML.ViewModel.Rows;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;

    using EA;

    /// <summary>
    /// The <see cref="ElementDefinitionToBlockMappingRule" /> is a <see cref="IMappingRule" /> for the
    /// <see cref="MappingEngine" />
    /// that takes a <see cref="List{T}" /> of <see cref="ElementDefinitionMappedElement" /> as input and
    /// outputs a E-TM-10-25 collection of <see cref="MappedElementDefinitionRowViewModel" />
    /// </summary>
    public class ElementDefinitionToBlockMappingRule : HubToDstBaseMappingRule<(bool complete, List<ElementDefinitionMappedElement> elements), List<MappedElementDefinitionRowViewModel>>
    {
        /// <summary>
        /// A collection of <see cref="Element" /> representing ValueType that has been created during this mapping
        /// </summary>
        private readonly List<Element> temporaryValueTypes = new();

        /// <summary>
        /// A collection of <see cref="Element" /> representing Unit that has been created during this mapping
        /// </summary>
        private readonly List<Element> temporaryUnits = new();

        /// <summary>
        /// A collection of correspondances between an <see cref="ElementUsage" /> and a Port and its definition to create links
        /// </summary>
        private readonly List<(ElementUsage elementUsage, Element port, Element portDefinition)> portsToLink = new();

        /// <summary>
        /// The collection of <see cref="EnterpriseArchitectBlockElement" />
        /// </summary>
        public List<ElementDefinitionMappedElement> Elements { get; private set; } = new();

        /// <summary>
        /// Transform a <see cref="List{T}" /> of <see cref="ElementDefinitionMappedElement" /> into a
        /// <see cref="List{T}" /> of
        /// <see cref="MappedElementDefinitionRowViewModel" />
        /// </summary>
        /// <param name="input">
        /// Tuple of <see cref="bool" />, The <see cref="List{T}" /> of <see cref="ElementDefinitionMappedElement " />
        /// The <see cref="bool" /> handles the fact that it the mapping has to map everything
        /// </param>
        /// <returns>A collection of <see cref="MappedElementDefinitionRowViewModel" /></returns>
        public override List<MappedElementDefinitionRowViewModel> Transform((bool complete, List<ElementDefinitionMappedElement> elements) input)
        {
            try
            {
                this.MappingConfiguration = AppContainer.Container.Resolve<IMappingConfigurationService>();
                this.DstController = AppContainer.Container.Resolve<IDstController>();

                if (!this.HubController.IsSessionOpen || !this.DstController.IsFileOpen)
                {
                    return default;
                }

                var (complete, elements) = input;
                this.Elements = new List<ElementDefinitionMappedElement>(elements);
                this.portsToLink.Clear();

                foreach (var mappedElement in this.Elements.ToList())
                {
                    if (mappedElement.DstElement == null)
                    {
                        var alreadyExist = this.GetOrCreateElement(mappedElement.HubElement, out var element);
                        mappedElement.DstElement = element;
                        mappedElement.ShouldCreateNewTargetElement = !alreadyExist;
                    }

                    if (complete)
                    {
                        this.MapContainedElement(mappedElement);
                        this.MapProperties(mappedElement.HubElement, mappedElement.DstElement);
                    }
                }

                if (complete)
                {
                    foreach (var mappedElement in this.Elements.ToList())
                    {
                        this.MapPorts(mappedElement);
                    }

                    foreach (var portToLink in this.portsToLink)
                    {
                        this.LinkPort(portToLink);
                    }

                    this.SaveMappingConfiguration(new List<MappedElementRowViewModel<ElementDefinition>>(this.Elements));
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
        /// Create the relation between a port an his interface
        /// </summary>
        /// <param name="portToLink">The correspondence between <see cref="ElementUsage" /> and Port Definition block</param>
        private void LinkPort((ElementUsage elementUsage, Element port, Element portDefinition) portToLink)
        {
            var (elementUsage, port, portDefinition) = portToLink;

            if (elementUsage.QueryRelationships
                    .FirstOrDefault(x => x.Category.Any(cat => cat.Name == "interface")) is BinaryRelationship relationsShip)
            {
                var targetType = relationsShip.Source.Iid == elementUsage.Iid ? StereotypeKind.RequiredInterface : StereotypeKind.ProvidedInterface;

                var interfaceElement = this.GetOrCreateInterface(relationsShip.Name);

                if (targetType == StereotypeKind.RequiredInterface)
                {
                    this.CreateOrUpdateConnector(portDefinition, interfaceElement);
                }

                var embeddedInterface = port.EmbeddedElements.OfType<Element>().FirstOrDefault(x => x.MetaType.AreEquals(targetType))
                                        ?? this.DstController.AddNewElement(port.Elements, interfaceElement.Name, targetType.ToString(), targetType);

                embeddedInterface.Name = interfaceElement.Name;
                embeddedInterface.Update();
                port.Elements.Refresh();
            }
        }

        /// <summary>
        /// Create or update the connector representing the relation between port and interface
        /// </summary>
        /// <param name="portDefinition">The port <see cref="Element" /> definition</param>
        /// <param name="interfaceElement">The interface <see cref="Element" /></param>
        private void CreateOrUpdateConnector(Element portDefinition, Element interfaceElement)
        {
            var connector = portDefinition.GetAllConnectorsOfElement().FirstOrDefault();

            if (connector == null)
            {
                connector = portDefinition.Connectors.AddNew("", StereotypeKind.Usage.ToString()) as Connector;
                this.DstController.CreatedConnectors.Add(connector);
            }

            connector.ClientID = portDefinition.ElementID;
            connector.SupplierID = interfaceElement.ElementID;
            connector.Update();
            portDefinition.Connectors.Refresh();
        }

        /// <summary>
        /// Gets or creates an <see cref="Element" /> representing an Interface
        /// </summary>
        /// <param name="interfaceName">The name of the <see cref="Element" /></param>
        /// <returns>The <see cref="Element" /></returns>
        private Element GetOrCreateInterface(string interfaceName)
        {
            if (!this.DstController.TryGetElementByType(interfaceName, StereotypeKind.Interface, out var interfaceElement))
            {
                interfaceElement = this.DstController.AddNewElement(this.DstController.GetDefaultPackage(StereotypeKind.Block).Elements, interfaceName,
                    StereotypeKind.Interface.ToString(), StereotypeKind.Interface);

                interfaceElement.Update();
            }

            return interfaceElement;
        }

        /// <summary>
        /// Maps ports for the provided <see cref="ElementDefinitionMappedElement" />
        /// </summary>
        /// <param name="mappedElement">The <see cref="ElementDefinitionMappedElement" /></param>
        private void MapPorts(ElementDefinitionMappedElement mappedElement)
        {
            foreach (var elementUsage in mappedElement.HubElement.ContainedElement.Where(x => x.InterfaceEnd != InterfaceEndKind.NONE))
            {
                Element port = null;
                Element definition = null;

                if (!this.GetOrCreatePort(elementUsage, mappedElement.DstElement, ref port, ref definition))
                {
                    this.Logger.Error($"Error during the creation of the port {elementUsage.Name} inside {mappedElement.DstElement.Name}");
                    continue;
                }

                this.portsToLink.Add((elementUsage, port, definition));
            }
        }

        /// <summary>
        /// Gets or create the <see cref="Element" /> that represents a port based on the provided <see cref="ElementUsage" />
        /// </summary>
        /// <param name="elementUsage">The <see cref="ElementUsage" /></param>
        /// <param name="containerElement">The <see cref="Element" /> that will contains the Port</param>
        /// <param name="port">The <see cref="Element" /> for the Port</param>
        /// <param name="definition">The port definition</param>
        /// <returns>A value indicating if the port and definition has been retrieved or created correctly</returns>
        private bool GetOrCreatePort(ElementUsage elementUsage, Element containerElement, ref Element port, ref Element definition)
        {
            port = containerElement.GetAllPortsOfElement().FirstOrDefault(x => (string)x.PropertyTypeName == elementUsage.Name)
                   ?? this.DstController.AddNewElement(containerElement.Elements, "", "Port", StereotypeKind.Port);

            definition = containerElement.GetAllPortsDefinitionOfElement().FirstOrDefault(x => x.Name == elementUsage.Name)
                         ?? this.DstController.AddNewElement(containerElement.Elements, elementUsage.Name, "block", StereotypeKind.Block);

            if (port.PropertyType == 0)
            {
                port.PropertyType = definition.ElementID;
                port.Update();
            }

            containerElement.Elements.Refresh();

            return port != null && definition != null;
        }

        /// <summary>
        /// Maps the properties to the provided <see cref="ElementDefinition" />
        /// </summary>
        /// <param name="hubElement">The <see cref="ElementDefinition" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        private void MapProperties(ElementDefinition hubElement, Element element)
        {
            this.MapProperties(hubElement.Parameter, element);
        }

        /// <summary>
        /// Maps the properties of the provided <see cref="ElementUsage" />
        /// </summary>
        /// <param name="elementUsage">The <see cref="ElementUsage" /></param>
        /// <param name="dstElement">The <see cref="Element" /></param>
        private void MapProperties(ElementUsage elementUsage, Element dstElement)
        {
            var parametersAndOverrides = elementUsage.ElementDefinition.Parameter
                .Where(x => elementUsage.ParameterOverride.All(parameterOverride => x.Iid != parameterOverride.Iid)).Cast<ParameterOrOverrideBase>().ToList();

            parametersAndOverrides.AddRange(elementUsage.ParameterOverride);
            this.MapProperties(parametersAndOverrides, dstElement);
        }

        /// <summary>
        /// Maps the properties of the provided the collection of <see cref="ParameterOrOverrideBase" />
        /// </summary>
        /// <param name="parameters">The collection of <see cref="ParameterOrOverrideBase" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        private void MapProperties(IEnumerable<ParameterOrOverrideBase> parameters, Element element)
        {
            foreach (var parameter in parameters)
            {
                this.GetOrCreateValueType(parameter, out var valueType);

                if (!this.TryGetExistingProperty(element, parameter, out var property))
                {
                    this.CreateProperty(parameter, element, out property);
                }

                if (property.PropertyType != valueType.ElementID)
                {
                    this.DstController.UpdatePropertyTypes[property.ElementGUID] = valueType.ElementID;
                }

                this.VerifyStateDependency(parameter, property);
                this.UpdateValue(parameter, property);
            }
        }

        /// <summary>
        /// Verifies if the <see cref="ParameterOrOverrideBase" /> is State Dependent and update or create the related
        /// <see cref="Connector" />
        /// and State <see cref="Element" />
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase" /></param>
        /// <param name="property">The <see cref="Element" /> property</param>
        private void VerifyStateDependency(ParameterOrOverrideBase parameter, Element property)
        {
            if (parameter.StateDependence == null)
            {
                return;
            }

            var dependyConnectors = property.GetAllConnectorsOfElement().Where(x => x.Type.AreEquals(StereotypeKind.Dependency)
                                                                                    && x.ClientID == property.ElementID).ToList();

            var connectorTargets = dependyConnectors.Select(dependyConnector => this.DstController.CurrentRepository
                .GetElementByID(dependyConnector.SupplierID)).ToList();

            foreach (var possibleFiniteState in parameter.StateDependence.PossibleFiniteStateList.ToList())
            {
                var state = connectorTargets.FirstOrDefault(x => x.Name == possibleFiniteState.Name || x.Name == possibleFiniteState.ShortName);

                var connectorExisted = state != null;

                if (state == null && !this.DstController.TryGetElementByType(possibleFiniteState.Name, StereotypeKind.State, out state))
                {
                    var collection = this.DstController.GetDefaultPackage(StereotypeKind.State).Elements;

                    state = this.DstController.AddNewElement(collection, possibleFiniteState.Name,
                        StereotypeKind.State.ToString(), StereotypeKind.State);

                    state.Update();
                }

                this.UpdateState(state, possibleFiniteState);

                if (!connectorExisted)
                {
                    var connector = property.Connectors.AddNew("", StereotypeKind.Dependency.ToString()) as Connector;
                    connector.ClientID = property.ElementID;
                    connector.SupplierID = state.ElementID;
                    connector.Update();
                    this.DstController.CreatedConnectors.Add(connector);
                    property.Connectors.Refresh();
                }
            }
        }

        /// <summary>
        /// Update the State <see cref="Element" /> based on the <see cref="PossibleFiniteState" />
        /// </summary>
        /// <param name="state"></param>
        /// <param name="possibleFiniteState"></param>
        private void UpdateState(Element state, PossibleFiniteStateList possibleFiniteState)
        {
            var possibleStateNames = possibleFiniteState.PossibleState.Select(x => x.Name).ToList();
            var possibleStateShortNames = possibleFiniteState.PossibleState.Select(x => x.ShortName).ToList();

            this.DstController.ModifiedPartitions[state] = new List<(Partition, ChangeKind)>();

            if (possibleStateNames.Count == 1)
            {
                if (possibleStateNames[0] != state.Name)
                {
                    this.RemovesNonMatchingRegions(state, possibleStateNames, possibleStateShortNames);

                    if (state.Partitions.Count == 0)
                    {
                        var partition = state.Partitions.AddNew(possibleStateNames[0], StereotypeKind.Partition.ToString()) as Partition;
                        this.DstController.ModifiedPartitions[state].Add((partition, ChangeKind.Create));
                    }

                    state.Update();
                }
                else
                {
                    this.DeleteAllRegionsOfState(state);
                }
            }
            else
            {
                this.RemovesNonMatchingRegions(state, possibleStateNames, possibleStateShortNames);

                var presentNames = state.Partitions.OfType<Partition>().Select(x => x.Name);
                var missingNames = new List<string>();

                for (var nameIndex = 0; nameIndex < possibleStateNames.Count; nameIndex++)
                {
                    var name = possibleStateNames[nameIndex];
                    var shortName = possibleStateShortNames[nameIndex];

                    if (presentNames.All(x => !x.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                              && !x.Equals(shortName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        missingNames.Add(name);
                    }
                }

                foreach (var missingName in missingNames)
                {
                    var createdPartition = state.Partitions.AddNew(missingName, StereotypeKind.Partition.ToString()) as Partition;
                    this.DstController.ModifiedPartitions[state].Add((createdPartition, ChangeKind.Create));
                }

                state.Update();
            }
        }

        /// <summary>
        /// Removes all <see cref="Partition" /> of a State <see cref="Element" /> where the name does not to any
        /// <see cref="PossibleFiniteState" />
        /// </summary>
        /// <param name="state">The State <see cref="Element" /></param>
        /// <param name="possibleStateNames">A collection containing the name of all <see cref="PossibleFiniteState" /></param>
        /// <param name="possibleStateShortNames">
        /// A collection containing the shortname of all <see cref="PossibleFiniteState" />
        /// </param>
        private void RemovesNonMatchingRegions(Element state, List<string> possibleStateNames, List<string> possibleStateShortNames)
        {
            for (short partitionIndex = 0; partitionIndex < state.Partitions.Count; partitionIndex++)
            {
                var partition = (Partition)state.Partitions.GetAt(partitionIndex);

                if (possibleStateNames.All(x => x.Equals(partition.Name, StringComparison.InvariantCultureIgnoreCase))
                    && possibleStateShortNames.All(x => x.Equals(partition.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    this.DstController.ModifiedPartitions[state].Add((partition, ChangeKind.Delete));
                    state.Partitions.Delete(partitionIndex);
                }
            }

            state.Update();
        }

        /// <summary>
        /// Deletes all <see cref="Partition" /> representing Region of a State <see cref="Element" />
        /// </summary>
        /// <param name="state">The state <see cref="Element" /></param>
        private void DeleteAllRegionsOfState(Element state)
        {
            while (state.Partitions.Count > 0)
            {
                this.DstController.ModifiedPartitions[state].Add(((Partition)state.Partitions.GetAt(0), ChangeKind.Delete));
                state.Partitions.Delete(0);
                state.Update();
                state.Partitions.Refresh();
            }
        }

        /// <summary>
        /// Update the value of the ValueProperty
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase" /> that contins the value to transfer</param>
        /// <param name="property">The <see cref="Element" /> for the ValueProperty</param>
        private void UpdateValue(ParameterOrOverrideBase parameter, Element property)
        {
            var elementDefinition = parameter.Container as ElementDefinition;

            var mappedRow = this.Elements.First(x => x.HubElement.Iid == elementDefinition.Iid);

            var parameterRow = mappedRow.ContainedRows.OfType<MappedParameterRowViewModel>()
                .First(x => x.HubElement.Iid == parameter.Iid);

            if (parameterRow.SelectedActualFiniteState != null)
            {
                this.MappingConfiguration.AddToExternalIdentifierMap(parameter.Iid, parameterRow.SelectedActualFiniteState.Iid.ToString(), MappingDirection.FromHubToDst);
            }

            var valueToAssign = parameter.QueryParameterBaseValueSet(null, parameterRow.SelectedActualFiniteState).ActualValue[0];

            if (valueToAssign == "-")
            {
                valueToAssign = string.Empty;
            }

            this.DstController.UpdatedValuePropretyValues[property.ElementGUID] = valueToAssign;
        }

        /// <summary>
        /// Creates an ValueProperty inside the given <see cref="Element" /> based on the <see cref="ParameterOrOverrideBase" />
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase" /></param>
        /// <param name="container">The <see cref="Element" /> container</param>
        /// <param name="property">The <see cref="Element" /> for the ValueProperty</param>
        private void CreateProperty(ParameterOrOverrideBase parameter, Element container, out Element property)
        {
            property = this.DstController.AddNewElement(container.EmbeddedElements, parameter.ParameterType.Name, "Property", StereotypeKind.ValueProperty);
            property.Update();
        }

        /// <summary>
        /// Gets or create the <see cref="Element" /> representing a ValueType for the provided
        /// <see cref="ParameterOrOverrideBase" />
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase" /> </param>
        /// <param name="element">The <see cref="Element" /></param>
        private void GetOrCreateValueType(ParameterOrOverrideBase parameter, out Element element)
        {
            if (parameter.Scale != null && parameter.Scale.Unit.ShortName != "1")
            {
                this.GetOrCreateValueType(parameter.ParameterType, parameter.Scale, out element);
            }
            else
            {
                this.GetOrCreateValueType(parameter.ParameterType, out element);
            }
        }

        /// <summary>
        /// Gets or create the <see cref="Element" /> representing a ValueType for the provided <see cref="ParameterType" />
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType" /> </param>
        /// <param name="element">The <see cref="Element" /></param>
        private void GetOrCreateValueType(ParameterType parameterType, out Element element)
        {
            this.QueryCollectionByNameAndShortname(parameterType, this.temporaryValueTypes, out element);

            if (element == null && !this.DstController.TryGetValueType(parameterType, null, out element))
            {
                var collection = this.DstController.GetDefaultPackage(StereotypeKind.ValueType).Elements;
                var newValueType = this.DstController.AddNewElement(collection, parameterType.Name, "DataType", StereotypeKind.ValueType);

                this.temporaryValueTypes.Add(newValueType);
                newValueType.Update();
                element = newValueType;
                collection.Refresh();
            }
        }

        /// <summary>
        /// Gets or creates the <see cref="Element" /> that matches the propvided <see cref="MeasurementScale" />
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType" /> to map to</param>
        /// <param name="scale">The <see cref="MeasurementScale" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        private void GetOrCreateValueType(ParameterType parameterType, MeasurementScale scale, out Element element)
        {
            this.QueryCollectionByNameAndShortname(scale, this.temporaryValueTypes, out element);

            if (element == null && !this.DstController.TryGetValueType(parameterType, scale, out element))
            {
                var collection = this.DstController.GetDefaultPackage(StereotypeKind.ValueType).Elements;

                var newValueType = this.DstController.AddNewElement(collection, parameterType.Name,
                    "DataType", StereotypeKind.ValueType);

                if (scale.Unit != null)
                {
                    var newUnit = this.GetOrCreateUnit(scale.Unit);
                    var taggedValues = newValueType.TaggedValuesEx.AddNew("unit", StereotypeKind.TaggedValue.ToString()) as TaggedValue;
                    taggedValues.Value = newUnit.ElementGUID;
                    newValueType.Name = $"{newValueType.Name}[{scale.Unit.ShortName}]";
                    taggedValues.Update();
                    newUnit.Update();
                    newValueType.TaggedValuesEx.Refresh();
                }

                this.temporaryValueTypes.Add(newValueType);
                newValueType.Update();
                collection.Refresh();
                element = newValueType;
            }
        }

        /// <summary>
        /// Gets or creates the Unit that matches the provides <see cref="MeasurementUnit" />
        /// </summary>
        /// <param name="scaleUnit">The <see cref="MeasurementUnit" /></param>
        /// <returns>The <see cref="Element" /></returns>
        private Element GetOrCreateUnit(MeasurementUnit scaleUnit)
        {
            this.QueryCollectionByNameAndShortname(scaleUnit, this.temporaryUnits, out var unitElement);

            if (unitElement == null && !this.DstController.TryGetElement(scaleUnit.Name, StereotypeKind.Unit, out unitElement))
            {
                var collection = this.DstController.GetDefaultPackage(StereotypeKind.Unit).Elements;
                unitElement = this.DstController.AddNewElement(collection, scaleUnit.Name, "Unit", StereotypeKind.Unit);

                unitElement.Update();
                collection.Refresh();
                this.temporaryUnits.Add(unitElement);
            }

            return unitElement;
        }

        /// <summary>
        /// Tries to get an exisitng <see cref="Element" /> representing a ValueProperty contained into an <see cref="Element" />
        /// that matches with the provided <see cref="ParameterOrOverrideBase" />
        /// </summary>
        /// <param name="element">The <see cref="Element" /></param>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase" /></param>
        /// <param name="property">The <see cref="Element" /> representing the ValueProperty</param>
        /// <returns>A value indicating whether the ValueProperty could be found</returns>
        private bool TryGetExistingProperty(Element element, ParameterOrOverrideBase parameter, out Element property)
        {
            property = element.GetValuePropertyOfElement(parameter.ParameterType.Name) ?? element.GetValuePropertyOfElement(parameter.ParameterType.ShortName);

            return property != null;
        }

        /// <summary>
        /// Maps the contained element of the provided <see cref="ElementDefinitionMappedElement" />
        /// </summary>
        /// <param name="mappedElement">The <see cref="ElementDefinitionMappedElement" /></param>
        private void MapContainedElement(ElementDefinitionMappedElement mappedElement)
        {
            foreach (var elementUsage in mappedElement.HubElement.ContainedElement.Where(x => x.InterfaceEnd == InterfaceEndKind.NONE).ToList())
            {
                var usageDefinitionMappedElement = this.Elements.FirstOrDefault(x =>
                    string.Equals(x.DstElement.Name, elementUsage.Name, StringComparison.InvariantCultureIgnoreCase));

                if (usageDefinitionMappedElement == null)
                {
                    this.GetOrCreateElement(elementUsage, out var elementUsageElement);
                    usageDefinitionMappedElement = new ElementDefinitionMappedElement(elementUsage.ElementDefinition, elementUsageElement, MappingDirection.FromHubToDst);
                    this.Elements.Add(usageDefinitionMappedElement);
                    this.MapProperties(elementUsage, usageDefinitionMappedElement.DstElement);
                    this.UpdateContainement(mappedElement.DstElement, usageDefinitionMappedElement.DstElement);
                    this.MapContainedElement(usageDefinitionMappedElement);
                }
            }
        }

        /// <summary>
        /// Updates the containment information of the provided parent and element
        /// </summary>
        /// <param name="parent">The parent <see cref="Element" /></param>
        /// <param name="element">The child <see cref="Element" /></param>
        private void UpdateContainement(Element parent, Element element)
        {
            var partProperty = parent.GetAllPartPropertiesOfElement().FirstOrDefault(x => x.PropertyType == element.ElementID)
                               ?? this.DstController.AddNewElement(parent.EmbeddedElements, element.Name, "Property", StereotypeKind.PartProperty);

            partProperty.PropertyType = element.ElementID;
            partProperty.Update();
        }

        /// <summary>
        /// Gets or creates an <see cref="Element" /> based on a <see cref="ElementBase" />
        /// </summary>
        /// <param name="elementBase">The <see cref="ElementBase" /></param>
        /// <param name="element">The <see cref="Element" /></param>
        /// <returns>A value indicating if the <see cref="Element" /> already exists</returns>
        private bool GetOrCreateElement(ElementBase elementBase, out Element element)
        {
            return this.GetOrCreateElement(elementBase.Name, out element);
        }

        /// <summary>
        /// Gets or creates an <see cref="Element" /> based on a name
        /// </summary>
        /// <param name="elementBaseName">The name</param>
        /// <param name="element">The <see cref="Element" /></param>
        /// <returns>A value indicating if the <see cref="Element" /> already exists</returns>
        private bool GetOrCreateElement(string elementBaseName, out Element element)
        {
            if (!this.DstController.TryGetElement(elementBaseName, StereotypeKind.Block, out element))
            {
                var package = this.DstController.GetDefaultPackage(StereotypeKind.Block);
                element = this.DstController.AddNewElement(package.Elements, elementBaseName, StereotypeKind.Block.ToString().ToLower(), StereotypeKind.Block);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Searches an <see cref="Element" /> where the name matched the <see cref="DefinedThing.Name" /> or
        /// the <see cref="DefinedThing.ShortName" /> and where the given <see cref="StereotypeKind" /> is applied to
        /// </summary>
        /// <param name="definedThing">The <see cref="DefinedThing" /></param>
        /// <param name="collection">The collection to look into</param>
        /// <param name="element">The <see cref="Element" /></param>
        private void QueryCollectionByNameAndShortname(DefinedThing definedThing, IEnumerable<Element> collection, out Element element)
        {
            element = collection.FirstOrDefault(x => x.Name == definedThing.Name || x.Name == definedThing.ShortName);
        }
    }
}
