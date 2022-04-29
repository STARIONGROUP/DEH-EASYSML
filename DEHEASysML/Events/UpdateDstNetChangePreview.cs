﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdateDstNetChangePreview.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Events
{
    using CDP4Dal;

    using DEHPCommon.Events;

    /// <summary>
    /// An event for the <see cref="CDPMessageBus"/>
    /// </summary>
    public class UpdateDstNetChangePreview : UpdateTreeBaseEvent
    {
        /// <summary>
        /// Initializes a new <see cref="UpdateDstNetChangePreview" />
        /// </summary>
        /// <param name="reset">a value indicating whether the listener should reset its tree</param>
        public UpdateDstNetChangePreview(bool reset = false) : base(reset)
        {
        }
    }
}