﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2013 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using System;

namespace OsmSharp.Collections.Arrays
{
    /// <summary>
    /// An abstract representation of a huge array.
    /// </summary>
    public abstract class HugeArrayBase<T> : IDisposable
    {
        /// <summary>
        /// Returns the length of this array.
        /// </summary>
        public abstract long Length { get; }
        
        /// <summary>
        /// Resizes this array.
        /// </summary>
        /// <param name="size"></param>
        public abstract void Resize(long size);

        /// <summary>
        /// Returns the element at the given index.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public abstract T this[long idx] { get; set; }

        /// <summary>
        /// Disposes of all resources associated with this array.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Copies all the data over from the given array to this array.
        /// </summary>
        /// <param name="array"></param>
        public virtual void CopyFrom(HugeArrayBase<T> array)
        {
            for(int idx = 0; idx < array.Length; idx++)
            {
                this[idx] = array[idx];
            }
        }
    }
}