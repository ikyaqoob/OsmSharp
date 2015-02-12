﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2015 Abelshausen Ben
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

using OsmSharp.IO.MemoryMappedFiles;
using System;
using System.Collections.Generic;

namespace OsmSharp.Collections.Arrays.MemoryMapped
{
    /// <summary>
    /// Represents a memory mapped huge array.
    /// </summary>
    public abstract class MemoryMappedHugeArray<T> : IHugeArray<T>
        where T : struct
    {
        /// <summary>
        /// Holds the length of this array.
        /// </summary>
        private long _length;

        /// <summary>
        /// Holds the buffer size.
        /// </summary>
        private int _bufferSize;

        /// <summary>
        /// Holds the buffer position.
        /// </summary>
        private long _bufferPosition;

        /// <summary>
        /// Holds the buffer.
        /// </summary>
        private T[] _buffer;

        /// <summary>
        /// Holds the buffer dirty flag.
        /// </summary>
        private bool _bufferDirty;

        /// <summary>
        /// Holds the file to create the memory mapped accessors.
        /// </summary>
        private MemoryMappedFile _file;

        /// <summary>
        /// Holds the list of accessors, one for each range.
        /// </summary>
        private List<MemoryMappedAccessor<T>> _accessors;

        /// <summary>
        /// Holds the default file element size.
        /// </summary>
        public static long DefaultFileElementSize = (long)1024 * (long)1024;

        /// <summary>
        /// Holds the file element size.
        /// </summary>
        private long _fileElementSize = DefaultFileElementSize;

        /// <summary>
        /// Holds the element size.
        /// </summary>
        private int _elementSize;

        /// <summary>
        /// Holds the maximum array size in bytes.
        /// </summary>
        private long _fileSizeBytes;

        /// <summary>
        /// Creates a memory mapped huge array.
        /// </summary>
        /// <param name="file">The the memory mapped file.</param>
        /// <param name="elementSize">The element size.</param>
        /// <param name="size">The initial size of the array.</param>
        /// <param name="arraySize">The size of an indivdual array block.</param>
        public MemoryMappedHugeArray(MemoryMappedFile file, int elementSize, long size, long arraySize)
        {
            if (file == null) { throw new ArgumentNullException(); }
            if (elementSize < 0) { throw new ArgumentOutOfRangeException("elementSize"); }
            if (arraySize < 0) { throw new ArgumentOutOfRangeException("arraySize"); }
            if (size < 0) { throw new ArgumentOutOfRangeException("size"); }
            if ((arraySize & (arraySize - 1)) != 0) { throw new ArgumentException("arraySize needs to be a power of 2."); }

            _file = file;
            _length = size;
            _fileElementSize = arraySize;
            _elementSize = elementSize;
            _fileSizeBytes = arraySize * _elementSize;

            _bufferSize = (int)arraySize / 64;
            _buffer = new T[_bufferSize];
            _bufferPosition = -1;
            _bufferDirty = false;

            var arrayCount = (int)System.Math.Ceiling((double)size / _fileElementSize);
            _accessors = new List<MemoryMappedAccessor<T>>(arrayCount);
            for (int arrayIdx = 0; arrayIdx < arrayCount; arrayIdx++)
            {
                _accessors.Add(this.CreateAccessor(_file, _fileSizeBytes));
            }
        }

        /// <summary>
        /// Creates a new memory mapped accessor.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="sizeInBytes"></param>
        /// <returns></returns>
        protected abstract MemoryMappedAccessor<T> CreateAccessor(MemoryMappedFile file, long sizeInBytes);

        /// <summary>
        /// Returns the length of this array.
        /// </summary>
        public long Length
        {
            get { return _length; }
        }

        /// <summary>
        /// Resizes this array.
        /// </summary>
        /// <param name="size"></param>
        public void Resize(long size)
        {
            // clear buffer.
            if(_bufferDirty)
            { // flush buffer.
                this.FlushBuffer();
            }
            _bufferPosition = -1;
            _buffer = new T[_bufferSize];


            var oldSize = _length;
            _length = size;

            var arrayCount = (int)System.Math.Ceiling((double)size / _fileElementSize);
            // _accessors = new List<MemoryMappedAccessor<T>>(arrayCount);
            if (arrayCount < _accessors.Count)
            { // decrease files/accessors.
                for (int arrayIdx = (int)arrayCount; arrayIdx < _accessors.Count; arrayIdx++)
                {
                    _accessors[arrayIdx].Dispose();
                    _accessors[arrayIdx] = null;
                }
                _accessors.RemoveRange((int)arrayCount, (int)(_accessors.Count - arrayCount));
            }
            else
            { // increase files/accessors.
                for (int arrayIdx = _accessors.Count; arrayIdx < arrayCount; arrayIdx++)
                {
                    _accessors.Add(this.CreateAccessor(_file, _fileSizeBytes));
                }
            }

            if (oldSize < _length)
            {
                for(var i = oldSize; i < _length; i++)
                {
                    this[i] = default(T);
                }
            }
        }

        /// <summary>
        /// Returns the element at the given index.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public T this[long idx]
        {
            get
            {
                // sync buffer.
                this.SyncBuffer(idx);

                return _buffer[idx - _bufferPosition];
            }
            set
            {
                // sync buffer.
                this.SyncBuffer(idx);

                _buffer[idx - _bufferPosition] = value;
                _bufferDirty = true;
            }
        }

        #region Buffering

        /// <summary>
        /// Syncs buffer.
        /// </summary>
        private void SyncBuffer(long idx)
        {
            // check buffer.
            if (_bufferPosition >= 0 &&
                idx >= _bufferPosition &&
                idx - _bufferPosition < _bufferSize)
            { // in buffer.

            }
            else
            { // load buffer.
                if (_bufferDirty)
                { // flush buffer.
                    this.FlushBuffer();
                }

                // load buffer.
                _bufferPosition = idx - (idx % _bufferSize);

                long arrayIdx = (long)System.Math.Floor(_bufferPosition / _fileElementSize);
                long localIdx = _bufferPosition % _fileElementSize;
                long localPosition = localIdx * _elementSize;

                _accessors[(int)arrayIdx].ReadArray(localPosition, _buffer, 0, _bufferSize);
            }
        }

        /// <summary>
        /// Flushes the current buffer to the file.
        /// </summary>
        private void FlushBuffer()
        {
            long arrayIdx = (long)System.Math.Floor(_bufferPosition / _fileElementSize);
            long localIdx = _bufferPosition % _fileElementSize;
            long localPosition = localIdx * _elementSize;

            _accessors[(int)arrayIdx].WriteArray(localPosition, _buffer, 0, _bufferSize);
            _bufferDirty = false;
        }

        #endregion

        /// <summary>
        /// Diposes of all native resource associated with this array.
        /// </summary>
        public void Dispose()
        {
            if (_bufferDirty)
            { // flush buffer.
                this.FlushBuffer();
            }

            // disposing the file will also dispose of all undisposed accessors, and accessor cannot exist without a file.
            _file.Dispose();
        }
    }
}