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
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.ExceptionServices;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using DEHEASysML.DstController;
    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;
    using DEHEASysML.Services.Cache;
    using DEHEASysML.Services.MappingConfiguration;
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
        /// A collection of <see cref="PossibleFiniteStateList" /> created during the mapping
        /// </summary>
        private readonly List<PossibleFiniteStateList> createdPossibleFiniteStateLists = new();

        /// <summary>
        /// A collection of <see cref="ActualFiniteStateList" /> created during the mapping
        /// </summary>
        private readonly List<ActualFiniteStateList> createdActualFiniteStateLists = new();

        /// <summary>
        /// Collection containing relation for a Port, used for the creation of future <see cref="BinaryRelationship" />
        /// </summary>
        private readonly List<(Element, EnterpriseArchitectBlockElement, ElementUsage)> portsToConnect = new();

        /// <summary>
        /// Gets the default <see cref="ValueArray{T}"/>
        /// </summary>
        private readonly ValueArray<string> defaultValueArray = new ([ "-" ]);

        /// <summary>
        /// The <see cref="ElementDefinition" /> the represents the ports
        /// </summary>
        private ElementDefinition portElementDefinition;

        /// <summary>
        /// The <see cref="Dictionary{TKey,TValue}"/> that stores all <see cref="EnterpriseArchitectBlockElement"/> by ElementID
        /// </summary>
        private Dictionary<int, EnterpriseArchitectBlockElement> elementsById;

        /// <summary>
        /// Gets the collection of part properties
        /// </summary>
        private Dictionary<int, List<Element>> allPartPropertiesPerElement;

        /// <summary>
        /// Gets the collection of value properties
        /// </summary>
        private Dictionary<int, List<Element>> allValuePropertiesPerElement;

        /// <summary>
        /// The collection of <see cref="EnterpriseArchitectBlockElement" />
        /// </summary>
        public List<EnterpriseArchitectBlockElement> Elements { get; private set; } = new();

        /// <summary>
        /// Transform a <see cref="List{T}" /> of <see cref="EnterpriseArchitectBlockElement" /> into a <see cref="List{T}" /> of
        /// <see cref="MappedElementDefinitionRowViewModel" />
        /// </summary>
        /// <param name="input">
        /// Tuple of <see cref="bool" />, The <see cref="List{T}" /> of <see cref="EnterpriseArchitectBlockElement " />
        /// The <see cref="bool" /> handles the fact that it the mapping has to map everything
        /// </param>
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

                this.Owner = this.HubController.CurrentDomainOfExpertise;

                this.DstController ??= AppContainer.Container.Resolve<IDstController>();
                this.CacheService ??= AppContainer.Container.Resolve<ICacheService>();
                this.MappingConfiguration ??= AppContainer.Container.Resolve<IMappingConfigurationService>();
                this.Elements = [..elements];

                if (completeMapping)
                {
                    this.InitializeCachingProperties();
                }

                this.portsToConnect.Clear();

                this.createdPossibleFiniteStateLists.RemoveAll(x => this.HubController.OpenIteration
                    .PossibleFiniteStateList.Exists(possibleFiniteState => x.Iid == possibleFiniteState.Iid) || x.Container?.Iid != this.HubController.OpenIteration.Iid);

                this.createdActualFiniteStateLists.RemoveAll(x => this.HubController.OpenIteration
                    .ActualFiniteStateList.Exists(actualFiniteStateList => x.Iid == actualFiniteStateList.Iid || x.Container?.Iid != this.HubController.OpenIteration.Iid));

                var mappingStopWatch = Stopwatch.StartNew();

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
                    this.SaveMappingConfiguration([..this.Elements]);
                }

                mappingStopWatch.Stop();
                this.Logger.Info("{0} for blocks done in {1}[ms]", completeMapping? "Mapping" : "Premapping", mappingStopWatch.ElapsedMilliseconds);

                return [..this.Elements];
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Initialize all caching dictionaries based on the content of the <see cref="ICacheService"/>
        /// </summary>
        public void InitializeCachingProperties()
        {
            this.elementsById = this.Elements.ToDictionary(x => x.DstElement.ElementID, x => x);

            this.CacheService ??= AppContainer.Container.Resolve<ICacheService>();
            this.DstController ??= AppContainer.Container.Resolve<IDstController>();
            this.allPartPropertiesPerElement = this.CacheService.GetAllElements().Where(this.DstController.IsPartProperty).GroupBy(x => x.ParentID).ToDictionary(x => x.Key, x => x.ToList());
            this.allValuePropertiesPerElement = this.CacheService.GetAllElements().Where(this.DstController.IsValueProperty).GroupBy(x => x.ParentID).ToDictionary(x => x.Key, x => x.ToList());
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

                if (this.DstController.TryGetInterfaceImplementation(interfaceBlock, out var blockDefinition))
                {
                    elementUsageName = blockDefinition.Name;
                }

                var interfaceElementUsage = this.portsToConnect
                    .Find(x => x.Item1.PropertyTypeName as string == elementUsageName).Item3;

                if (interfaceElementUsage == null)
                {
                    interfaceElementUsage = this.HubController.OpenIteration.Element.SelectMany(x => x.ContainedElement)
                        .FirstOrDefault(x => x.Name == elementUsageName)?.Clone(false);

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

                this.elementsById[element.DstElement.ElementID].RelationShips.Add(relationShip);
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
            if(this.allPartPropertiesPerElement.TryGetValue(element.ElementID, out var partProperties))
            {
                foreach (var partProperty in partProperties)
                {
                    this.MapPartProperty(elementDefinition, partProperty);
                }
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

            if (this.allValuePropertiesPerElement.TryGetValue(element.ElementID, out var valueProperties))
            {
                foreach (var property in valueProperties)
                {
                    var propertyType = this.DstController.CurrentRepository.GetElementByID(property.PropertyType);

                    var existingParameter = elementDefinition.Parameter.Find(x =>
                        string.Equals(x.ParameterType.ShortName, propertyType.GetShortName(), StringComparison.InvariantCultureIgnoreCase) ||
                        string.Equals(x.ParameterType.Name, propertyType.Name, StringComparison.InvariantCultureIgnoreCase));

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

                    this.VerifyStateDependency(parameter, property);
                    this.UpdateValueSet(parameter, valueOfProperty);

                    elementDefinition.Parameter.RemoveAll(x => x.Iid == parameter.Iid);
                    elementDefinition.Parameter.Add(parameter);
                }
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

            if (this.allValuePropertiesPerElement.TryGetValue(element.ElementID, out var valueProperties) && valueProperties.Find(x => x.Name == this.isEncapsulatedCategoryNames.Item2) is { } isEncapsulatedValueProperty)
            {
                var isEncapsulatedValue = isEncapsulatedValueProperty.GetValueOfPropertyValue();
                isEncapsulated = isEncapsulatedValue == "1" || string.Equals(bool.TrueString, isEncapsulatedValue, StringComparison.InvariantCultureIgnoreCase);
            }

            this.MapCategory(elementDefinition, this.isLeafCategoryNames, element.IsLeaf, false);
            this.MapCategory(elementDefinition, this.isAbstractCategoryNames, element.Abstract == "1", true);
            this.MapCategory(elementDefinition, this.isActiveCategoryNames, element.IsActive, false);
            this.MapCategory(elementDefinition, this.isEncapsulatedCategoryNames, isEncapsulated, true);
            this.MapStereotypesToCategory(element, elementDefinition);
        }

        /// <summary>
        /// Verifies that the <see cref="Parameter" /> has to be State Dependent or not
        /// </summary>
        /// <param name="parameter">The <see cref="Parameter" /></param>
        /// <param name="property">The <see cref="Element" /> property</param>
        private void VerifyStateDependency(Parameter parameter, Element property)
        {
            var dependencies = property.GetAllConnectorsOfElement().Where(x => x.Type.AreEquals(StereotypeKind.Dependency)
                                                                               && x.ClientID == property.ElementID).ToList();

            if (!dependencies.Any())
            {
                parameter.StateDependence = null;
                return;
            }

            var possibleFiniteStateList = new List<PossibleFiniteStateList>();

            foreach (var dependency in dependencies)
            {
                var state = this.CacheService.GetElementById(dependency.SupplierID);

                if (state.Type.AreEquals(StereotypeKind.State))
                {
                    possibleFiniteStateList.Add(this.GetOrCreatePossibleFiniteState(state));
                }
            }

            if (!possibleFiniteStateList.Any())
            {
                parameter.StateDependence = null;
                return;
            }

            parameter.StateDependence = this.GetOrCreateActualFiniteStateList(possibleFiniteStateList);
        }

        /// <summary>
        /// Gets or create the <see cref="ActualFiniteStateList" />
        /// </summary>
        /// <param name="possibleFiniteStateListCollection">
        /// A collection of <see cref="PossibleFiniteStateList" /> that the
        /// <see cref="ActualFiniteStateList" /> should contains
        /// </param>
        /// <returns>The created or retrieved <see cref="ActualFiniteStateList" /></returns>
        private ActualFiniteStateList GetOrCreateActualFiniteStateList(List<PossibleFiniteStateList> possibleFiniteStateListCollection)
        {
            var actualFiniteStateList = this.GetActualFiniteStateListFromCollection(this.HubController.OpenIteration.ActualFiniteStateList.ToList(),
                                            possibleFiniteStateListCollection)?.Clone(true)
                                        ?? this.GetActualFiniteStateListFromCollection(this.createdActualFiniteStateLists.ToList(),
                                            possibleFiniteStateListCollection)?.Clone(true)
                                        ?? this.CreateActualFiniteStateList(possibleFiniteStateListCollection);

            actualFiniteStateList.PossibleFiniteStateList.Clear();

            foreach (var possibleFiniteStateList in possibleFiniteStateListCollection)
            {
                actualFiniteStateList.PossibleFiniteStateList.Add(possibleFiniteStateList);
            }

            this.UpdateActualFiniteStateList(actualFiniteStateList);
            return actualFiniteStateList;
        }

        /// <summary>
        /// Update the <see cref="ActualFiniteStateList" /> to apply change on the NetChangePreview.
        /// </summary>
        /// <param name="actualFiniteStateList">The <see cref="ActualFiniteStateList" /> to update</param>
        private void UpdateActualFiniteStateList(ActualFiniteStateList actualFiniteStateList)
        {
            var combinations = this.GetAllPossibleCombination(actualFiniteStateList.PossibleFiniteStateList.ToList());
            actualFiniteStateList.ActualState.Clear();

            foreach (var combination in combinations)
            {
                actualFiniteStateList.ActualState.Add(new ActualFiniteState
                {
                    PossibleState = combination,
                    Iid = Guid.NewGuid()
                });
            }
        }

        /// <summary>
        /// Generates all possible combination crossing all <see cref="PossibleFiniteState" /> from all
        /// <see cref="PossibleFiniteStateList" />
        /// </summary>
        /// <param name="possibleFiniteStateLists">A collection of <see cref="PossibleFiniteStateList" /></param>
        /// <returns>A collection of <see cref="PossibleFiniteState" /></returns>
        private List<List<PossibleFiniteState>> GetAllPossibleCombination(List<PossibleFiniteStateList> possibleFiniteStateLists)
        {
            var allPossibleFiniteState = new List<List<PossibleFiniteState>>();

            foreach (var possibleFiniteStateList in possibleFiniteStateLists)
            {
                allPossibleFiniteState.Add(possibleFiniteStateList.PossibleState.ToList());
            }

            IEnumerable<List<PossibleFiniteState>> combinations = new List<List<PossibleFiniteState>> { new() };

            foreach (var possibleFinites in allPossibleFiniteState)
            {
                combinations = combinations.SelectMany(combi => possibleFinites.Select(x =>
                {
                    var newList = combi.ToList();
                    newList.Add(x);
                    return newList;
                }).ToList());
            }

            return combinations.ToList();
        }

        /// <summary>
        /// Creates a <see cref="ActualFiniteStateList" /> based on a collection of <see cref="PossibleFiniteStateList" />
        /// </summary>
        /// <param name="possibleFiniteStateListCollection">
        /// The collection of <see cref="possibleFiniteStateListCollection" />
        /// </param>
        /// <returns></returns>
        private ActualFiniteStateList CreateActualFiniteStateList(List<PossibleFiniteStateList> possibleFiniteStateListCollection)
        {
            var actualFiniteStateList = new ActualFiniteStateList
            {
                Iid = Guid.NewGuid(),
                Owner = this.Owner,
                Container = this.HubController.OpenIteration
            };

            foreach (var possibleFiniteStateList in possibleFiniteStateListCollection)
            {
                actualFiniteStateList.PossibleFiniteStateList.Add(possibleFiniteStateList);
            }

            this.createdActualFiniteStateLists.Add(actualFiniteStateList);

            return actualFiniteStateList;
        }

        /// <summary>
        /// Gets an <see cref="ActualFiniteStateList" /> contained inside a collection
        /// </summary>
        /// <param name="actualFiniteStateLists">A collection to look into</param>
        /// <param name="possibleFiniteStateListCollection">
        /// The <see cref="PossibleFiniteStateList" /> that compose the
        /// <see cref="ActualFiniteStateList" />
        /// </param>
        /// <returns>A <see cref="ActualFiniteStateList" /> if found, null if not present inside the collection</returns>
        private ActualFiniteStateList GetActualFiniteStateListFromCollection(IEnumerable<ActualFiniteStateList> actualFiniteStateLists,
            List<PossibleFiniteStateList> possibleFiniteStateListCollection)
        {
            var matchOnNumberOfPossibleFinitieState = actualFiniteStateLists
                .Where(x => x.PossibleFiniteStateList.Count == possibleFiniteStateListCollection.Count)
                .ToList();

            return matchOnNumberOfPossibleFinitieState.Find(finiteStateList =>
                finiteStateList.PossibleFiniteStateList.All(possibleState =>
                    possibleFiniteStateListCollection.Exists(x => x.Iid == possibleState.Iid)));
        }

        /// <summary>
        /// Gets or creates a <see cref="PossibleFiniteStateList" /> based on a State <see cref="Element" />
        /// </summary>
        /// <param name="state">The state <see cref="Element" /></param>
        /// <returns>The retrieved or created <see cref="PossibleFiniteStateList" />></returns>
        private PossibleFiniteStateList GetOrCreatePossibleFiniteState(IDualElement state)
        {
            var partitions = state.Partitions.OfType<Partition>().Select(x => x.Name).ToList();

            if (!partitions.Any())
            {
                partitions.Add(state.Name);
            }

            var possibleFiniteStateList = this.HubController.OpenIteration.PossibleFiniteStateList
                                              .Find(x => x.Name == state.Name || x.ShortName == state.Name.GetShortName())?.Clone(true)
                                          ?? this.createdPossibleFiniteStateLists
                                              .Find(x => x.Name == state.Name || x.ShortName == state.Name.GetShortName())
                                          ?? this.CreatePossibleFiniteStateList(state.Name);

            this.UpdatePossibleFiniteStateList(possibleFiniteStateList, partitions);

            return possibleFiniteStateList;
        }

        /// <summary>
        /// Update the current <see cref="PossibleFiniteStateList" /> based on the name of partitions
        /// </summary>
        /// <param name="possibleFiniteStateList">The <see cref="PossibleFiniteStateList" /></param>
        /// <param name="statesName">
        /// A collection of name to populate the <see cref="PossibleFiniteStateList.PossibleState" />
        /// </param>
        private void UpdatePossibleFiniteStateList(PossibleFiniteStateList possibleFiniteStateList, List<string> statesName)
        {
            var finiteStates = possibleFiniteStateList.PossibleState;

            for (var finiteStateIndex = statesName.Count; finiteStateIndex < finiteStates.Count; finiteStateIndex++)
            {
                finiteStates.Remove(finiteStates[finiteStateIndex]);
            }

            for (var namesIndex = 0; namesIndex < statesName.Count; namesIndex++)
            {
                if (finiteStates.Count > namesIndex)
                {
                    this.UpdatePossibleFiniteState(finiteStates[namesIndex], statesName[namesIndex]);
                }
                else
                {
                    possibleFiniteStateList.PossibleState.Add(this.CreatePossibleFiniteState(statesName[namesIndex]));
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="PossibleFiniteState" /> based on a named
        /// </summary>
        /// <param name="name">The name of the <see cref="PossibleFiniteState" /></param>
        /// <returns>The created <see cref="PossibleFiniteState" /></returns>
        private PossibleFiniteState CreatePossibleFiniteState(string name)
        {
            return new PossibleFiniteState
            {
                Iid = Guid.NewGuid(),
                Name = name,
                ShortName = name.GetShortName(),
                Container = this.HubController.OpenIteration
            };
        }

        /// <summary>
        /// Updates a <see cref="PossibleFiniteState" /> based on a name
        /// </summary>
        /// <param name="finiteState">The <see cref="PossibleFiniteState" /></param>
        /// <param name="name">The name of the state</param>
        private void UpdatePossibleFiniteState(PossibleFiniteState finiteState, string name)
        {
            if (finiteState.Name != name || finiteState.ShortName != name.GetShortName())
            {
                finiteState.Name = name;
                finiteState.ShortName = name.GetShortName();
            }
        }

        /// <summary>
        /// Creates a new <see cref="PossibleFiniteStateList" />
        /// </summary>
        /// <param name="name">The name of the <see cref="PossibleFiniteStateList" /></param>
        /// <returns>A new <see cref="PossibleFiniteStateList" /></returns>
        private PossibleFiniteStateList CreatePossibleFiniteStateList(string name)
        {
            var possibleFiniteStateList = new PossibleFiniteStateList
            {
                Iid = Guid.NewGuid(),
                Name = name,
                ShortName = name.GetShortName(),
                Owner = this.Owner
            };

            this.createdPossibleFiniteStateLists.Add(possibleFiniteStateList);
            return possibleFiniteStateList;
        }

        /// <summary>
        /// Maps all ports of an <see cref="Element" />
        /// </summary>
        /// <param name="element">The <see cref="EnterpriseArchitectBlockElement" /></param>
        private void MapPorts(EnterpriseArchitectBlockElement element)
        {
            var ports = this.CacheService.GetElementsOfMetaType(StereotypeKind.Port).Where(x => x.ParentID == element.DstElement.ElementID).ToList();
                
            foreach (var port in ports)
            {
                var (portBlock, interfaceElement) = this.DstController.ResolvePort(port);

                var interfaceEndKind = InterfaceEndKind.OUTPUT;

                if (portBlock == null && interfaceElement != null)
                {
                    portBlock = interfaceElement;
                    interfaceEndKind = InterfaceEndKind.INPUT;
                }

                if (portBlock == null)
                {
                    continue;
                }

                var elementUsage = element.HubElement.ContainedElement.Find(x => x.Name == portBlock.Name);

                if (elementUsage == null)
                {
                    elementUsage = new ElementUsage
                    {
                        Name = portBlock.Name,
                        ShortName = portBlock.GetShortName(),
                        Iid = Guid.NewGuid(),
                        Owner = this.Owner,
                        ElementDefinition = this.GetPortElementDefinition(),
                        InterfaceEnd = interfaceEndKind
                    };

                    element.HubElement.ContainedElement.Add(elementUsage);
                }

                this.portsToConnect.Add((port, element, elementUsage));
            }
        }

        /// <summary>
        /// Gets the <see cref="ElementDefinition" /> that represents all ports
        /// </summary>
        /// <returns>The <see cref="ElementDefinition" /></returns>
        private ElementDefinition GetPortElementDefinition()
        {
            if (this.portElementDefinition != null && this.portElementDefinition.Container.Iid == this.HubController.OpenIteration.Iid) 
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
            if (container == null || partProperty.PropertyType == 0)
            {
                return;
            }

            var partPropertyBlock = this.CacheService.GetElementById(partProperty.PropertyType);

            if (!this.elementsById.TryGetValue(partPropertyBlock.ElementID, out var mappedElement))
            {
                mappedElement = new EnterpriseArchitectBlockElement(this.GetOrCreateElementDefinition(partPropertyBlock), partPropertyBlock, MappingDirection.FromDstToHub);
                this.elementsById[partPropertyBlock.ElementID] = mappedElement;
                this.Elements.Add(mappedElement);
                this.MapElement(mappedElement);
            }

            if (container.ContainedElement.Exists(x => x.ElementDefinition.Iid == mappedElement.HubElement.Iid))
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
        /// Update the correct value set depend of the selected <paramref name="parameter" />
        /// </summary>
        /// <param name="parameter">The <see cref="Parameter" /> to update the value set</param>
        /// <param name="valueOfProperty">The new value</param>
        private void UpdateValueSet(Parameter parameter, string valueOfProperty)
        {
            if (parameter.StateDependence == null)
            {
                this.CreateOrUpdateParameterValueSet(parameter, null);
            }
            else
            {
                foreach (var actualFiniteState in parameter.StateDependence.ActualState)
                {
                    this.CreateOrUpdateParameterValueSet(parameter, actualFiniteState);
                }

                if (parameter.ValueSet.Find(x => x.ActualState == null) == null)
                {
                    parameter.ValueSet.Add(new ParameterValueSet
                    {
                        Iid = Guid.NewGuid(),
                        Reference = this.defaultValueArray,
                        Formula = this.defaultValueArray,
                        Published = this.defaultValueArray,
                        Computed = this.defaultValueArray,
                        ActualState = null
                    });
                }
            }

            foreach (var valueSet in parameter.ValueSet)
            {
                valueSet.ValueSwitch = ParameterSwitchKind.MANUAL;
                var valueToSet = string.IsNullOrEmpty(valueOfProperty) ? "-" : valueOfProperty;
                valueSet.Manual = new ValueArray<string>([FormattableString.Invariant($"{valueToSet}")]);
            }
        }

        /// <summary>
        /// Creates or update a <see cref="ParameterValueSetBase" /> for the given <see cref="Parameter" /> and
        /// <see cref="ActualFiniteState" />
        /// </summary>
        /// <param name="parameter">The <see cref="Parameter" /></param>
        /// <param name="actualFiniteState">The <see cref="ActualFiniteState" /></param>
        /// <returns>The <see cref="ParameterValueSetBase" /></returns>
        private void CreateOrUpdateParameterValueSet(Parameter parameter, ActualFiniteState actualFiniteState)
        {
            if (!((parameter.Original != null || parameter.ValueSet.Any()) && actualFiniteState == null))
            {
                var valueSet = new ParameterValueSet
                {
                    Iid = Guid.NewGuid(),
                    Reference =this.defaultValueArray,
                    Formula = this.defaultValueArray,
                    Published = this.defaultValueArray,
                    Computed =this.defaultValueArray,
                    ActualState = actualFiniteState
                };

                parameter.ValueSet.Add(valueSet);
            }
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
                        (property.GetUnitOfValueProperty()?.Value != null &&
                         this.DstController.CurrentRepository.GetElementByGuid(property.GetUnitOfValueProperty().Value) != null))
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
                this.Logger.Error(exception, "Coult not create the ParameterType of the Property {0}", property.Name);
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

            Element unit = null;

            if (taggedValue != null)
            {
                unit = this.DstController.CurrentRepository.GetElementByGuid(taggedValue.Value);
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
                    if (shouldCreateCategory)
                    {
                        this.Logger.Error($"The Category {categoryNames.Item1} could not be found or created for the element {elementDefinition.Name}");
                    }
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

            var elementDefinition = (this.Elements.Find(x =>
                                         x.HubElement != null && string.Equals(x.HubElement.ShortName, shortName, StringComparison.CurrentCultureIgnoreCase))?.HubElement
                                     ?? this.HubController.OpenIteration.Element
                                         .Find(x => string.Equals(x.ShortName, shortName, StringComparison.CurrentCultureIgnoreCase))
                                         ?.Clone(true)) ?? new ElementDefinition
            {
                Iid = Guid.NewGuid(),
                Owner = this.Owner,
                Name = dstElementName,
                ShortName = shortName,
                Container = this.HubController.OpenIteration
            };

            return elementDefinition;
        }
    }
}
