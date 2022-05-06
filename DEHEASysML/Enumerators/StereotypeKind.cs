// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StereotypeKind.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Enumerators
{
    /// <summary>
    /// An enumeration data type used to identify the kind of used Enterprise Architect Stereotype
    /// </summary>
    public enum StereotypeKind
    {
        /// <summary>
        /// Used to represent a block Stereotype
        /// </summary>
        Block,

        /// <summary>
        /// Used to represent a RelationShip between requirement
        /// </summary>
        DeriveReqt,

        /// <summary>
        /// Used to represent a PartProperty Stereotype
        /// </summary>
        PartProperty,

        /// <summary>
        /// Used to represent a ValueProperty Stereotype
        /// </summary>
        ValueProperty,

        /// <summary>
        /// Used to represent a ValueType Stereotype
        /// </summary>
        ValueType,

        /// <summary>
        /// Used to represent a Requirement Stereotype
        /// </summary>
        Requirement,

        /// <summary>
        /// Used to represent the Port MetaType
        /// </summary>
        Port,

        /// <summary>
        /// Used to represent the Unit Stereotype
        /// </summary>
        Unit,

        /// <summary>
        /// Used to represent the TaggedValue Stereotype
        /// </summary>
        TaggedValue,

        /// <summary>
        /// Used to represent the RequiredInterface MetaType
        /// </summary>
        RequiredInterface,

        /// <summary>
        /// Used to represent the ProvidedInterface MetaType
        /// </summary>
        ProvidedInterface,

        /// <summary>
        /// Used to represent the Interface MetaType
        /// </summary>
        Interface, 

        /// <summary>
        /// Used to represent the Usage MetaType
        /// </summary>
        Usage
    }
}
