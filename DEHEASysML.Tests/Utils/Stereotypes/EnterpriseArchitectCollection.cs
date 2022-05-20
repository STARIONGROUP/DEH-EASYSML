// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectCollection.cs" company="RHEA System S.A.">
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

namespace DEHEASysML.Tests.Utils.Stereotypes
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using DEHEASysML.Enumerators;
    using DEHEASysML.Extensions;

    using EA;

    using Moq;

    /// <summary>
    /// The <see cref="EnterpriseArchitectCollection" /> is used for test purposed. As all constructors of all class used by
    /// the Enterprise Architect
    /// Assembly are not accessible outside of this Assembly, this class will helps for the Unit Test
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class EnterpriseArchitectCollection : Collection
    {
        /// <summary>
        /// A containedObjects of the contained object
        /// </summary>
        private readonly List<object> containedObjects;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnterpriseArchitectCollection" /> class.
        /// </summary>
        public EnterpriseArchitectCollection()
        {
            this.containedObjects = new List<object>();
        }

        /// <summary>
        /// Gets the number of contained objects
        /// </summary>
        public short Count => (short)this.containedObjects.Count;

        /// <summary>
        /// Gets the <see cref="ObjectType" /> of contained objects
        /// </summary>
        public ObjectType ObjectType => 0;

        /// <summary>
        /// Retrieves a contained object at the given index
        /// </summary>
        /// <param name="index">The index of the object</param>
        /// <returns>The contained object</returns>
        public object GetAt(short index)
        {
            return this.IsIndexInRange(index) ? this.containedObjects[index] : null;
        }

        /// <summary>
        /// Removes a contained object
        /// </summary>
        /// <param name="index">The index of the object</param>
        /// <param name="Refresh">Not used</param>
        public void DeleteAt(short index, bool Refresh)
        {
            if (!this.IsIndexInRange(index))
            {
                return;
            }

            this.containedObjects.RemoveAt(index);
        }

        /// <summary>
        /// Gets the last occured error (Not used)
        /// </summary>
        /// <returns>null</returns>
        public string GetLastError()
        {
            return null;
        }

        /// <summary>
        /// Gets a object by his name (Not used)
        /// </summary>
        /// <param name="Name">The name of the object</param>
        /// <returns>null</returns>
        public object GetByName(string Name)
        {
            return null;
        }

        /// <summary>
        /// Refresh the collection (Not used)
        /// </summary>
        public void Refresh()
        {
        }

        /// <summary>
        /// Adds a new object to the contained objects
        /// </summary>
        /// <param name="Name">The name of the object</param>
        /// <param name="Type">The Type if the object</param>
        /// <returns>The created object</returns>
        public object AddNew(string Name, string Type)
        {
            if (Type.AreEquals(StereotypeKind.Dependency) || Type.AreEquals(StereotypeKind.Abstraction))
            {
                return new Mock<Connector>().Object;
            }

            if (Type.AreEquals(StereotypeKind.Package))
            {
                return new Mock<Package>().Object;
            }

            return null;
        }

        /// <summary>
        /// Removes a contained object
        /// </summary>
        /// <param name="index">The index of the objects to removed</param>
        public void Delete(short index)
        {
            this.DeleteAt(index, false);
        }

        /// <summary>
        /// Gets the <see cref="IEnumerator" /> to iterate through the collection
        /// </summary>
        /// <returns>The <see cref="IEnumerator" /></returns>
        public IEnumerator GetEnumerator()
        {
            return this.containedObjects.GetEnumerator();
        }

        /// <summary>
        /// Adds an object to the contained Object
        /// </summary>
        /// <param name="newObject">The new object</param>
        public void Add(object newObject)
        {
            this.containedObjects.Add(newObject);
        }

        /// <summary>
        /// Adds a collection of object
        /// </summary>
        /// <param name="objects">The collection to add</param>
        public void AddRange(IEnumerable<object> objects)
        {
            this.containedObjects.AddRange(objects);
        }

        /// <summary>
        /// Asserts if the given index is in range of the <see cref="containedObjects" /> collection
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>Asserts that the index is in range</returns>
        private bool IsIndexInRange(short index)
        {
            return index >= 0 && index < this.containedObjects.Count;
        }
    }
}
