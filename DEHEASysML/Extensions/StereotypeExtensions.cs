// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StereotypeExtensions.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using DEHEASysML.Enumerators;

    using EA;

    using CustomProperty = EA.CustomProperty;

    /// <summary>
    /// The <see cref="StereotypeExtensions" /> provides useful methods to verify stereotypes used in EA
    /// </summary>
    public static class StereotypeExtensions
    {
        /// <summary>
        /// Gets the value of the given ValueProperty
        /// </summary>
        /// <param name="valueProperty">The value property</param>
        /// <returns>The value</returns>
        public static string GetValueOfPropertyValue(this Element valueProperty)
        {
            return valueProperty.CustomProperties.OfType<CustomProperty>().FirstOrDefault(x => x.Name == "default")?.Value;
        }

        /// <summary>
        /// Sets the value of the given ValueProperty
        /// </summary>
        /// <param name="valueProperty">The value property</param>
        /// <param name="newValue">The new value</param>
        public static void SetValueOfPropertyValue(this Element valueProperty, string newValue)
        {
            if (valueProperty.CustomProperties.OfType<CustomProperty>().FirstOrDefault(x => x.Name == "default") is { } customProperty)
            {
                customProperty.Value = newValue;
            }
        }

        /// <summary>
        /// Get the <see cref="TaggedValue" /> corresponding to the unit of the given ValueProperty
        /// </summary>
        /// <param name="valueProperty">The ValueProperty</param>
        /// <returns>The <see cref="TaggedValue" /></returns>
        public static TaggedValue GetUnitOfValueProperty(this Element valueProperty)
        {
            return valueProperty?.TaggedValuesEx.OfType<TaggedValue>().FirstOrDefault(x => x.Name.AreEquals(StereotypeKind.Unit));
        }

        /// <summary>
        /// Gets all Blocks that defines Ports of an <see cref="Element"/>
        /// </summary>
        /// <param name="element">The <see cref="Element"/></param>
        /// <returns>A collection of <see cref="Element"/></returns>
        public static IEnumerable<Element> GetAllPortsDefinitionOfElement(this Element element)
        {
            return element.Elements.OfType<Element>().Where(x => x.HasStereotype(StereotypeKind.Block));
        }

        /// <summary>
        /// Gets the text of the Requirement
        /// </summary>
        /// <param name="requirement">The <see cref="Element"/> representing Requirement</param>
        /// <returns>The text of the Requirement</returns>
        public static string GetRequirementText(this Element requirement)
        {
            return (requirement.TaggedValuesEx.GetByName("text") as TaggedValue)?.Notes;
        }

        /// <summary>
        /// Sets the text of the Requirement
        /// </summary>
        /// <param name="requirement">The <see cref="Element"/> representing a Requirement</param>
        /// <param name="text">The new text</param>
        public static void SetRequirementText(this Element requirement, string text)
        {
            if (requirement.TaggedValuesEx.GetByName("text") is TaggedValue taggedValue)
            {
                taggedValue.Notes = text;
                taggedValue.Update();
            }
        }

        /// <summary>
        /// Gets all connectors contained in an <see cref="Element"/>
        /// </summary>
        /// <param name="element">The <see cref="Element"/></param>
        /// <returns>A collection of <see cref="Connector" /></returns>
        public static IEnumerable<Connector> GetAllConnectorsOfElement(this Element element)
        {
            return element.Connectors.OfType<Connector>();
        }

        /// <summary>
        /// Gets the id of the Requirement
        /// </summary>
        /// <param name="requirement">The <see cref="Element"/> representing Requirement</param>
        /// <returns>The Id of the Requirement</returns>
        public static string GetRequirementId(this Element requirement)
        {
            var requirementId = (requirement.TaggedValuesEx.GetByName("id") as TaggedValue)?.Value;

            return string.IsNullOrEmpty(requirementId) ? "ID NOT SET" : requirementId;
        }

        /// <summary>
        /// Sets the id of the Requirement
        /// </summary>
        /// <param name="requirement">The <see cref="Element"/> representing a Requirement</param>
        /// <param name="id">The new id</param>
        public static void SetRequirementId(this Element requirement, string id)
        {
            if (requirement.TaggedValuesEx.GetByName("id") is TaggedValue taggedValue)
            {
                taggedValue.Value = id;
                taggedValue.Update();
            }
        }

        /// <summary>
        /// Gets all connectors representing a relationship between 2 requirements
        /// </summary>
        /// <param name="requirement">The <see cref="Element"/> representing a Requirement</param>
        /// <returns>A collection of <see cref="Connector" /></returns>
        public static IEnumerable<Connector> GetRequirementsRelationShipConnectors(this Element requirement)
        {
            return requirement.GetAllConnectorsOfElement().Where(x => x.Stereotype.AreEquals(StereotypeKind.DeriveReqt));
        }

        /// <summary>
        /// Gets all <see cref="Element"/> of a certain Stereotype inside a <see cref="Package"/>
        /// </summary>
        /// <param name="package">The <see cref="IDualPackage"/></param>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>A collection of <see cref="Element"/></returns>
        public static IEnumerable<Element> GetElementsOfStereotypeInPackage(this IDualPackage package, StereotypeKind stereotype)
        {
            return package.Elements.OfType<Element>().Where(x => x.HasStereotype(stereotype));
        }

        /// <summary>
        /// Gets all <see cref="Element"/> of a certain Type inside a <see cref="Package"/>
        /// </summary>
        /// <param name="package">The <see cref="IDualPackage"/></param>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>A collection of <see cref="Element"/></returns>
        public static IEnumerable<Element> GetElementsOfTypeInPackage(this IDualPackage package, StereotypeKind stereotype)
        {
            return package.Elements.OfType<Element>().Where(x => x.MetaType.AreEquals(stereotype));
        }

        /// <summary>
        /// Asserts if the <see cref="Element"/> has the <see cref="StereotypeKind"/> applied
        /// </summary>
        /// <param name="element">The <see cref="Element"/></param>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>A value indicating if the <see cref="StereotypeKind"/> is applied</returns>
        public static bool HasStereotype(this Element element, StereotypeKind stereotype)
        {
            return element.HasStereotype(stereotype.GetStereotypeName());
        }

        /// <summary>
        /// Get the fully qualified stereotype of the provided <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>The Fully Qualified stereotype</returns>
        public static string GetFQStereotype(this StereotypeKind stereotype)
        {
            switch (stereotype)
            {
                case StereotypeKind.Unit:
                    return "SysML1.3::unit";
                case StereotypeKind.Block:
                    return "SysML1.4::block";
                case StereotypeKind.Requirement:
                    return "SysML1.4::requirement";
                case StereotypeKind.Port:
                case StereotypeKind.Interface:
                case StereotypeKind.RequiredInterface:
                case StereotypeKind.ProvidedInterface:
                case StereotypeKind.State:
                    return string.Empty;
                default: return stereotype.ToString();
            }
        }

        /// <summary>
        /// Get the name of the provided <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="stereotype">The <see cref="StereotypeKind"/></param>
        /// <returns>The stereotype name</returns>
        public static string GetStereotypeName(this StereotypeKind stereotype)
        {
            switch (stereotype)
            {
                case StereotypeKind.Unit:
                case StereotypeKind.Block:
                case StereotypeKind.Requirement:
                    return stereotype.ToString().ToLower();
                default: return stereotype.ToString();
            }
        }

        /// <summary>
        /// Verifies that the <paramref name="stereotypeName"/> correspond to the <see cref="StereotypeKind"/>
        /// </summary>
        /// <param name="stereotypeName">The name of the Stereotype</param>
        /// <param name="stereotypeKind">The <see cref="StereotypeKind"/></param>
        /// <returns>Asserts the equality of the values</returns>
        public static bool AreEquals(this string stereotypeName, StereotypeKind stereotypeKind)
        {
            return string.Equals(stereotypeName, stereotypeKind.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Gets a 10-25 compliant short Name from the provided stereotype name
        /// </summary>
        /// <param name="element">The element to base the short name</param>
        /// <returns>The shortname</returns>
        public static string GetShortName(this Element element)
        {
            return element.Name.GetShortName();
        }

        /// <summary>
        /// Gets a 10-25 compliant short Name from the provided stereotype name
        /// </summary>
        /// <param name="elementName">The element to base the short name</param>
        /// <returns>The shortname</returns>
        public static string GetShortName(this string elementName)
        {
            var regex = new Regex("[^a-zA-Z0-9]");
            return regex.Replace(elementName, "");
        }

        /// <summary>
        /// Asserts that an <see cref="Element"/> is a Part or not
        /// </summary>
        /// <param name="element">The <see cref="Element"/> to check</param>
        /// <returns>The assert</returns>
        public static bool IsPart(this Element element)
        {
            return element.ParentID != 0 && element.MetaType == "Part";
        }
    }
}
