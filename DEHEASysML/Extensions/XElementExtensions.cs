// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XElementExtensions.cs" company="RHEA System S.A.">
// Copyright (c) 2020-2024 RHEA System S.A.
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
    using System.Xml.Linq;

    /// <summary>
    /// Extensions class for <see cref="XElement"/>
    /// </summary>
    public static class XElementExtensions
    {
        /// <summary>
        /// Function that verifies that an <see cref="XElement" /> matches a name
        /// </summary>
        /// <param name="matchingName">The name that have to match</param>
        /// <returns>A <see cref="Func{TResult}" /></returns>
        public static Func<XElement, bool> MatchElementByName(string matchingName)
        {
            return x => string.Equals(x.Name.LocalName, matchingName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
