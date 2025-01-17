﻿#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Serialization
{
    /// <summary>
    /// The endianness of files
    /// </summary>
    public enum Endian
    {
        /// <summary>
        /// The endianness of files
        /// </summary>
        Auto,

        /// <summary>
        /// Use big endian (0x1000 is serialised as 0x10 0x00)
        /// </summary>
        Big,

        /// <summary>
        /// Use little endian (0x1000 is serialised as 0x00 0x10)
        /// </summary>
        Little,
        Native
    }

    /// <summary>
    /// The storage format of Real values
    /// </summary>
    public enum RealStorageFormat
    {
        /// <summary>
        /// Real is stored as float, reducing precision if you're using OGRE_DOUBLE_PRECISION
        /// </summary>
        Float,

        /// <summary>
        /// Real as stored as double, not useful unless you're using OGRE_DOUBLE_PRECISION
        /// </summary>
        Double
    }

    /// <summary>
    /// Definition of a chunk of data in a file
    /// </summary>
    public class Chunk
    {
        /// <summary>
        /// Identifier of the chunk (for example from makeIdentifier)  (stored)
        /// </summary>
        public uint id;

        /// <summary>
        /// Version of the chunk (stored)
        /// </summary>
        public ushort version;

        /// <summary>
        /// Length of the chunk data in bytes, excluding the header of this chunk (stored)
        /// </summary>
        public uint length;

        /// <summary>
        /// Location of the chunk (header) in bytes from the start of a stream (derived)
        /// </summary>
        public uint offset;

        public Chunk()
        {
            this.version = 1;
        }

        public override int GetHashCode()
        {
            return (int)(this.id ^ this.version ^ this.length);
        }
    }

    /// <summary>
    /// Utility class providing helper methods for reading / writing structured data held in a Stream.
    /// </summary>
    /// <remarks>
    /// The structure of a file read / written by this class is a series of 
    /// 'chunks'. A chunk-based format has the advantage of being extensible later, 
    /// and it's robust, in that a reader can skip chunks that they are not 
    /// able (or willing) to process.
    /// <para />
    /// Chunks are contained serially in the file, but they can also be 
    /// nested in order both to provide context, and to group chunks together for 
    /// potential skipping. 
    /// <para />
    /// The data format of a chunk is as follows:
    /// -# Chunk ID (32-bit uint). This can be any number unique in a context, except the numbers 0x0000, 0x0001 and 0x1000, which are reserved for Ogre's use
    /// -# Chunk version (16-bit uint). Chunks can change over time so this version number reflects that
    /// -# Length (32-bit uint). The length of the chunk data section, including nested chunks. Note that
    ///    this length excludes this header, but includes the header of any nested chunks. 
    /// -# Checksum (32-bit uint). Checksum value generated from the above - basically lets us check this is a valid chunk.
    /// -# Chunk data
    /// The 'Chunk data' section will contain chunk-specific data, which may include
    /// other nested chunks.
    /// </remarks>
    public class StreamSerializer : DisposableObject
    {
        #region Constants and Enumerations

        public const uint HEADER_ID = 0x0001;
        public const uint REVERSE_HEADER_ID = 0x1000;

        public const uint CHUNK_HEADER_SIZE = sizeof(uint) + //id
                                              sizeof(ushort) + //version
                                              sizeof(uint) + //length
                                              sizeof(int); //checksum

        #endregion Constants and Enumerations

        #region Fields and Properties

        protected Stream mStream;
        protected bool mFlipEndian;
        protected bool mReadWriteHeader;
        protected RealStorageFormat mRealFormat = RealStorageFormat.Float;
        protected Deque<Chunk> mChunkStack = new Deque<Chunk>();
        protected int mCurrentOffset = 0;

        #region Endian Property

        protected Endian mEndian;

        /// <summary>
        /// Get the endian mode.
        /// </summary>
        /// <remarks>
        /// If the result is Endian.Auto, this mode will change when the first piece of data is read / written.</remarks>
        public Endian Endian
        {
            get
            {
                return this.mEndian;
            }
        }

        #endregion Endian Property

        /// <summary>
        /// Reports whether the stream is at the end of file
        /// </summary>
        public virtual bool Eof
        {
            [OgreVersion(1, 7, 2)]
            get
            {
                CheckStream();
                return this.mStream.Position == this.mStream.Length;
            }
        }

        /// <summary>
        /// Get the definition of the current chunk being read (if any).
        /// </summary>
        public Chunk CurrentChunk
        {
            get
            {
                if (this.mChunkStack.Count == 0)
                {
                    return null;
                }
                else
                {
                    return this.mChunkStack.PeekTail();
                }
            }
        }

        /// <summary>
        /// Get the ID of the chunk that's currently being read/written, if any.
        /// </summary>
        /// <remarks>The id of the current chunk being read / written (at the tightest
        /// level of nesting), or zero if no chunk is being processed.</remarks>
        public uint CurrentChunkID
        {
            get
            {
                if (this.mChunkStack.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return this.mChunkStack.PeekTail().id;
                }
            }
        }

        /// <summary>
        /// Get the current byte position relative to the start of the data section
        /// of the last chunk that was read or written.
        /// </summary>
        /// <remarks>Returns the offset. Note that a return value of 0 means that either the
        /// position is at the start of the chunk data section (ie right after the
        /// header), or that no chunk is currently active. Use getCurrentChunkID
        /// or getCurrentChunkDepth to determine if a chunk is active.</remarks>
        public int OffsetFromChunkStart
        {
            [OgreVersion(1, 7, 2)]
            get
            {
                CheckStream(false, false, false);

                if (this.mChunkStack.Count == 0)
                {
                    return 0;
                }
                else
                {
                    var curPos = this.mStream.Position;
                    var diff = curPos - this.mChunkStack.PeekTail().offset;
                    if (diff >= CHUNK_HEADER_SIZE)
                    {
                        return (int)(diff - CHUNK_HEADER_SIZE);
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        /// <summary>
        /// Call this to 'peek' at the next chunk ID without permanently moving the stream pointer.
        /// </summary>
        public uint NextChunkId
        {
            get
            {
                CheckStream();
                if (Eof)
                {
                    return 0;
                }

                if (this.mReadWriteHeader)
                {
                    ReadHeader();
                }

                if (this.mEndian == Endian.Auto)
                {
                    throw new Exception("Endian mode has not been determined, did you disable header without setting?");
                }

                var curPos = this.mStream.Position;
                var chunkId = Read<uint>();
                this.mStream.Position = curPos;
                return chunkId;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stream">The stream on which you will read / write data.</param>
        public StreamSerializer(Stream stream)
            : this(stream, Endian.Auto, true)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stream">The stream on which you will read / write data.</param>
        /// <param name="endianMode">If true, the first write or read to this stream will 
        /// automatically read / write the header too. This is required if you
        /// set endianMode to Endian.Auto, but if you manually set the endian mode, 
        /// then you can skip writing / reading the header if you wish, if for example
        /// this stream is midway through a file which has already included header
        /// information.</param>
        public StreamSerializer(Stream stream, Endian endianMode)
            : this(stream, endianMode, true)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stream">The stream on which you will read / write data.</param>
        /// <param name="endianMode">If true, the first write or read to this stream will 
        /// automatically read / write the header too. This is required if you
        /// set endianMode to Endian.Auto, but if you manually set the endian mode, 
        /// then you can skip writing / reading the header if you wish, if for example
        /// this stream is midway through a file which has already included header
        /// information.</param>
        /// <param name="autoHeader">If true, the first write or read to this stream will 
        /// automatically read / write the header too. This is required if you
        /// set endianMode to Endian.Auto, but if you manually set the endian mode, 
        /// then you can skip writing / reading the header if you wish, if for example
        /// this stream is midway through a file which has already included header
        /// information.</param>
        public StreamSerializer(Stream stream, Endian endianMode, bool autoHeader)
#if !AXIOM_DOUBLE_PRECISION
            : this(stream, endianMode, autoHeader, RealStorageFormat.Float)
#else
			: this( stream, endianMode, autoHeader, RealStorageFormat.Double )
#endif
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stream">The stream on which you will read / write data.</param>
        /// <param name="endianMode">If true, the first write or read to this stream will 
        /// automatically read / write the header too. This is required if you
        /// set endianMode to Endian.Auto, but if you manually set the endian mode, 
        /// then you can skip writing / reading the header if you wish, if for example
        /// this stream is midway through a file which has already included header
        /// information.</param>
        /// <param name="autoHeader">If true, the first write or read to this stream will 
        /// automatically read / write the header too. This is required if you
        /// set endianMode to Endian.Auto, but if you manually set the endian mode, 
        /// then you can skip writing / reading the header if you wish, if for example
        /// this stream is midway through a file which has already included header
        /// information.</param>
        /// <param name="realFormat">Set the format you want to write reals in. Only useful for files that 
        /// you're writing (since when reading this is picked up from the file), 
        /// and can only be changed if autoHeader is true, since real format is stored in the header. 
        /// Defaults to float unless you're using AXIOM_DOUBLE_PRECISION.</param>
        [OgreVersion(1, 7, 2)]
        public StreamSerializer(Stream stream, Endian endianMode, bool autoHeader, RealStorageFormat realFormat)
            : base()
        {
            this.mStream = stream;
            this.mEndian = endianMode;
            this.mReadWriteHeader = autoHeader;
            this.mRealFormat = realFormat;

            if (this.mEndian != Endian.Auto)
            {
#if AXIOM_BIG_ENDIAN
				if ( mEndian == Endian.Little )
				{
					mFlipEndian = true;
				}
#else
                if (this.mEndian == Endian.Big)
                {
                    this.mFlipEndian = true;
                }
#endif
            }

            CheckStream();
        }

        #endregion Construction and Destruction

        /// <summary>
        /// Pack a 4-character code into a 32-bit identifier.
        /// </summary>
        /// <param name="code"> Pack a 4-character code into a 32-bit identifier.</param>
        /// <remarks>You can use this to generate id's for your chunks based on friendlier 
        /// 4-character codes rather than assigning numerical IDs, if you like.</remarks>
        /// <returns></returns>
        public static uint MakeIdentifier(string code)
        {
            Contract.Requires(code.Length <= 4, "code", "Only four (4) characters allowed in code.");

            uint ret = 0;
            var c = System.Math.Min(4, code.Length);
            for (var i = 0; i < c; ++i)
            {
                ret += (uint)(code[i] << (i * 8));
            }
            return ret;
        }

        /// <summary>
        /// Report the current depth of the chunk nesting, whether reading or writing.
        /// </summary>
        /// <returns>Returns how many levels of nested chunks are currently being processed, 
        /// either writing or reading. In order to tidily finish, you must call
        /// read/writeChunkEnd this many times.</returns>
        public int GetCurrentChunkDepth()
        {
            return this.mChunkStack.Count;
        }

        /// <summary>
        /// Reads the start of the next chunk in the file.
        /// </summary>
        /// <remarks>
        /// Files are serialised in a chunk-based manner, meaning that each section
        /// of data is prepended by a chunk header. After reading this chunk header, 
        /// the next set of data is available directly afterwards.
        /// <para />
        /// When you have finished with this chunk, you should call readChunkEnd.
        /// This will perform a bit of validation and clear the chunk from 
        /// the stack.
        /// </remarks>
        /// <returns>The Chunk that comes next</returns>
        [OgreVersion(1, 7, 2)]
        public virtual Chunk ReadChunkBegin()
        {
            // Have we figured out the endian mode yet?
            if (this.mReadWriteHeader)
            {
                ReadHeader();
            }

            if (this.mEndian == Endian.Auto)
            {
                throw new AxiomException("Endian mode has not been determined, did you disable header without setting?");
            }

            var chunk = ReadChunk();
            this.mChunkStack.Add(chunk);

            return chunk;
        }

        /// <summary>
        /// Reads the start of the next chunk so long as it's of a given ID and version.
        /// </summary>
        /// <remarks>
        /// Files are serialised in a chunk-based manner, meaning that each section
        /// of data is prepended by a chunk header. After reading this chunk header, 
        /// the next set of data is available directly afterwards.
        /// </remarks>
        /// <param name="id">The ID you're expecting. If the next chunk isn't of this ID, then
        /// the chunk read is undone and the method returns null.</param>
        /// <param name="maxVersion">The maximum version you're able to process. If the ID is correct
        /// but the version	exceeds what is passed in here, the chunk is skipped over,
        /// the problem logged and null is returned.</param>
        /// <returns>The chunk if it passes the validation</returns>
        public Chunk ReadChunkBegin(uint id, UInt16 maxVersion)
        {
            return ReadChunkBegin(id, maxVersion, String.Empty);
        }

        /// <summary>
        /// Reads the start of the next chunk so long as it's of a given ID and version.
        /// </summary>
        /// <remarks>
        /// Files are serialised in a chunk-based manner, meaning that each section
        /// of data is prepended by a chunk header. After reading this chunk header, 
        /// the next set of data is available directly afterwards.
        /// </remarks>
        /// <param name="id">The ID you're expecting. If the next chunk isn't of this ID, then
        /// the chunk read is undone and the method returns null.</param>
        /// <param name="maxVersion">The maximum version you're able to process. If the ID is correct
        /// but the version	exceeds what is passed in here, the chunk is skipped over,
        /// the problem logged and null is returned.</param>
        /// <param name="msg">Descriptive text added to the log if versions are not compatible</param>
        /// <returns>The chunk if it passes the validation</returns>
        public Chunk ReadChunkBegin(uint id, UInt16 maxVersion, string msg)
        {
            var c = ReadChunkBegin();
            if (c.id != id)
            {
                // rewind
                UndoReadChunk(c.id);
                return null;
            }
            else if (c.version > maxVersion)
            {
                LogManager.Instance.Write("Error: " + msg + " : Data version is " + c.version +
                                           " but this software can only read up to version " + maxVersion);
                // skip
                ReadChunkEnd(c.id);
                return null;
            }

            return c;
        }

        /// <summary>
        /// Call this to 'rewind' the stream to just before the start of the current chunk. 
        /// </summary>
        /// <remarks>The most common case of wanting to use this is if you'd called <see cref="ReadChunkBegin()"/>, 
        /// but the chunk you read wasn't one you wanted to process, and rather than
        /// skipping over it (which <see cref="ReadChunkEnd"/> would do), you want to backtrack
        /// and give something else an opportunity to read it. </remarks>
        /// <param name="id">The id of the chunk that you were reading (for validation purposes)</param>
        [OgreVersion(1, 7, 2)]
        public virtual void UndoReadChunk(uint id)
        {
            var c = PopChunk(id);

            CheckStream();

            this.mStream.Position = c.offset;
        }

        /// <summary>
        /// Finish the reading of a chunk.
        /// </summary>
        /// <remarks>You can call this method at any point after calling <see cref="ReadChunkBegin()" />, even
        /// if you didn't read all the rest of the data in the chunk. If you did 
        /// not read to the end of a chunk, this method will automatically skip 
        /// over the remainder of the chunk and position the stream just after it.</remarks>
        /// <param name="id">The id of the chunk that you were reading (for validation purposes)</param>
        [OgreVersion(1, 7, 2)]
        public virtual void ReadChunkEnd(uint id)
        {
            var c = PopChunk(id);

            CheckStream();

            // skip to the end of the chunk if we were not there already
            // this lets us quite reading a chunk anywhere and have the read marker
            // automatically skip to the next one
            if (this.mStream.Position < (c.offset + CHUNK_HEADER_SIZE + c.length))
            {
                this.mStream.Position = c.offset + CHUNK_HEADER_SIZE + c.length;
            }
        }

        /// <summary>
        /// Return whether the current data pointer is at the end of the current chunk.
        /// </summary>
        /// <param name="id">The id of the chunk that you were reading (for validation purposes)</param>
        /// <returns>Return whether the current data pointer is at the end of the current chunk.</returns>
        public bool IsEndOfChunk(uint id)
        {
            var c = CurrentChunk;
            Contract.Requires(c.id == id);
            return this.mStream.Position == (c.offset + CHUNK_HEADER_SIZE + c.length);
        }

        /// <summary>
        /// Begin writing a new chunk.
        /// </summary>
        /// <remarks>
        /// This starts the process of writing a new chunk to the stream. This will 
        /// write the chunk header for you, and store a pointer so that the
        /// class can automatically go back and fill in the size for you later
        /// should you need it to. If you have already begun a chunk without ending
        /// it, then this method will start a nested chunk within it. Once written, 
        /// you can then start writing chunk-specific data into your stream.</remarks>
        /// <param name="id">The identifier of the new chunk. Any value that's unique in the
        /// file context is valid, except for the numbers 0x0001 and 0x1000 which are reserved
        /// for internal header identification use.</param>
        /// <param name="version">The version of the chunk you're writing</param>
        [OgreVersion(1, 7, 2)]
#if NET_40
        public virtual void WriteChunkBegin( uint id, UInt16 version = 1 )
#else
        public virtual void WriteChunkBegin(uint id, UInt16 version)
#endif
        {
            CheckStream(false, false, true);

            if (this.mReadWriteHeader)
            {
                WriteHeader();
            }

            if (this.mEndian == Endian.Auto)
            {
                throw new AxiomException("Endian mode has not been determined, did you disable header without setting?");
            }

            WriteChunk(id, version);
        }

#if !NET_40
        /// <see cref="StreamSerializer.WriteChunkBegin(uint, UInt16)"/>
        public void WriteChunkBegin(uint id)
        {
            WriteChunkBegin(id, 1);
        }
#endif

        /// <summary>
        /// End writing a chunk.
        /// </summary>
        /// <param name="id">The identifier of the chunk - this is really just a safety check, 
        /// since you can only end the chunk you most recently started.</param>
        [OgreVersion(1, 7, 2)]
        public virtual void WriteChunkEnd(uint id)
        {
            CheckStream(false, false, true);

            var c = PopChunk(id);

            // update the sizes
            var currPos = this.mStream.Position;
            c.length = (uint)(currPos - c.offset - CHUNK_HEADER_SIZE);

            // seek to 'length' position in stream for this chunk
            // skip id (32) and version (16)
            this.mStream.Position = (c.offset + sizeof(uint) + sizeof(ushort));
            Write(c.length);
            // write updated checksum
            Write(c.GetHashCode());

            // seek back to previous position
            this.mStream.Position = currPos;
        }

        /// <summary>
        /// Reads an item from the stream
        /// </summary>
        /// <typeparam name="T">Type to read</typeparam>
        /// <returns>new instance of type T</returns>
        public T Read<T>()
        {
            var buffer = new byte[Memory.SizeOf(typeof(T))];
            ReadData(buffer, buffer.Length, 1);
            return BitConverterEx.SetBytes<T>(buffer);
        }

        /// <summary>
        /// Reads an item from the stream
        /// </summary>
        /// <typeparam name="T">Type to read</typeparam>
        /// <param name="data">new instance of type T</param>
        public void Read<T>(out T data)
        {
            var buffer = new byte[Memory.SizeOf(typeof(T))];
            ReadData(buffer, buffer.Length, 1);
            data = BitConverterEx.SetBytes<T>(buffer);
        }

        /// <summary>
        /// Reads an item from the stream
        /// </summary>
        /// <typeparam name="T">Type to read</typeparam>
        /// <param name="data">new instance of type T</param>
        public void Read<T>(out T[] data)
        {
            int length;
            var buffer = new byte[Memory.SizeOf(typeof(int))];
            ReadData(buffer, buffer.Length, 1);
            length = BitConverterEx.SetBytes<int>(buffer);
            buffer = new byte[Memory.SizeOf(typeof(T)) * length];
            ReadData(buffer, buffer.Length, 1);
            BitConverterEx.SetBytes<T>(buffer, out data);
        }

        /// <summary>
        /// Reads a string from the stream
        /// </summary>
        /// <param name="data"></param>
        public void Read(out string data)
        {
            var length = Read<int>();
            var encoding = new System.Text.UTF8Encoding();
            var buffer = new byte[length];
            ReadData(buffer, buffer.Length, 1);
            data = encoding.GetString(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads a node from the stream
        /// </summary>
        /// <param name="data"></param>
        public void Read(ref Node data)
        {
            data.Position = Read<Vector3>();
            data.Orientation = Read<Quaternion>();
            data.Scale = Read<Vector3>();
        }

        /// <summary>
        /// Write an item to the stream
        /// </summary>
        /// <typeparam name="T">Type to write</typeparam>
        /// <param name="data">instance of T to write</param>
        public void Write<T>(T data)
        {
            var buffer = BitConverterEx.GetBytes(data);
            WriteData(buffer, buffer.Length, 1);
        }

        /// <summary>
        /// Write an item to the stream
        /// </summary>
        /// <typeparam name="T">Type to write</typeparam>
        /// <param name="data">instance of T to write</param>
        public void Write<T>(T[] data)
        {
            var buffer = BitConverterEx.GetBytes(data.Length);
            WriteData(buffer, buffer.Length, 1);
            buffer = BitConverterEx.GetBytes(data);
            WriteData(buffer, buffer.Length, 1);
        }

        /// <summary>
        /// Write a string to the stream
        /// </summary>
        /// <param name="data"></param>
        public void Write(string data)
        {
            var encoding = new System.Text.UTF8Encoding();
            var buffer = encoding.GetBytes(data);
            Write(buffer.Length);
            WriteData(buffer, buffer.Length, 1);
        }

        /// <summary>
        /// Write a node to the stream
        /// </summary>
        /// <param name="data"></param>
        public void Write(Node data)
        {
            Write(data.Position);
            Write(data.Orientation);
            Write(data.Scale);
        }

        #region Protected Methods

        [OgreVersion(1, 7, 2)]
        protected virtual Chunk PopChunk(uint id)
        {
            if (this.mChunkStack.Count == 0)
            {
                throw new AxiomException("No active chunk!");
            }

            var chunk = this.mChunkStack.PeekTail();
            if (chunk.id != id)
            {
                throw new AxiomException("Incorrect chunk id!");
            }

            var c = this.mChunkStack.RemoveFromTail();
            return c;
        }

        /// <summary>
        ///
        /// </summary>
        protected void ReadHeader()
        {
            uint headerid;
            var mtp = new byte[4];
            var actually_read = this.mStream.Read(mtp, 0, sizeof(uint));
            this.mStream.Position -= actually_read;

            headerid = BitConverter.ToUInt32(mtp, 0);
            if (headerid == REVERSE_HEADER_ID)
            {
                this.mFlipEndian = true;
            }
            else if (headerid == HEADER_ID)
            {
                this.mFlipEndian = false;
            }
            else
            {
                throw new Exception("Cannot determine endian mode because header is missing");
            }
            DetermineEndianness();
            this.mReadWriteHeader = false;

            var chunk = ReadChunkBegin();
            // endian should be flipped now
            Debug.Assert(chunk.id == HEADER_ID);

            // read real storage format
            bool realIsDouble;
            Read(out realIsDouble);
            this.mRealFormat = realIsDouble ? RealStorageFormat.Double : RealStorageFormat.Float;

            ReadChunkEnd(HEADER_ID);
        }

        /// <summary>
        ///
        /// </summary>
        protected void WriteHeader()
        {
            if (this.mEndian == Endian.Auto)
            {
                DetermineEndianness();
            }
            // Header chunk has zero data size
            WriteChunk(HEADER_ID, 1);

            // real format
            var realIsDouble = (this.mRealFormat == RealStorageFormat.Double);
            Write(realIsDouble);

            WriteChunkEnd(HEADER_ID);

            this.mReadWriteHeader = false;
        }

        [OgreVersion(1, 7, 2)]
        protected Chunk ReadChunk()
        {
            var chunk = new Chunk();
            chunk.offset = (uint)(this.mStream.Position);
            Read(out chunk.id);
            Read(out chunk.version);
            Read(out chunk.length);

            uint checksum;
            Read(out checksum);

            if (checksum != chunk.GetHashCode())
            {
                // no good, this is an invalid chunk
                var off = chunk.offset;
                throw new AxiomException("Corrupt chunk detected in stream at byte {0}", off);
            }
            else
            {
                return chunk;
            }
        }

        [OgreVersion(1, 7, 2)]
        protected void WriteChunk(uint id, ushort version)
        {
            var c = new Chunk();
            c.id = id;
            c.version = version;
            c.offset = (uint)this.mStream.Position;
            c.length = 0;

            this.mChunkStack.Add(c);

            Write(c.id);
            Write(c.version);
            Write(c.length);
            // write length again, this is just a placeholder for the checksum (to come later)
            Write(c.length);
        }

        [OgreVersion(1, 7, 2)]
#if NET_40
        protected virtual void CheckStream( bool failOnEof = false, bool validateReadable = false, bool validateWriteable = false )
#else
        protected virtual void CheckStream(bool failOnEof, bool validateReadable, bool validateWriteable)
#endif
        {
            if (this.mStream == null)
            {
                throw new AxiomException("Invalid operation, stream is null");
            }

            if (failOnEof && this.mStream.Position == this.mStream.Length)
            {
                throw new AxiomException("Invalid operation, end of file on stream");
            }

            if (validateReadable && !this.mStream.CanRead)
            {
                throw new AxiomException("Invalid operation, file is not readable");
            }

            if (validateWriteable && !this.mStream.CanWrite)
            {
                throw new AxiomException("Invalid operation, file is not writeable");
            }
        }

#if !NET_40
        protected void CheckStream()
        {
            CheckStream(false, false, false);
        }

        protected void CheckStream(bool failOnEof)
        {
            CheckStream(failOnEof, false, false);
        }

        protected void CheckStream(bool failOnEof, bool validateReadable)
        {
            CheckStream(failOnEof, validateReadable, false);
        }
#endif

        protected void DetermineEndianness()
        {
#if AXIOM_BIG_ENDIAN
			if (mFlipEndian)
				mEndian = Endian.Little;
			else
				mEndian = Endian.Big;
#else
            if (this.mFlipEndian)
            {
                this.mEndian = Endian.Big;
            }
            else
            {
                this.mEndian = Endian.Little;
            }
#endif
        }

        /// <summary>
        /// Read arbitrary data to a stream.
        /// </summary>
        /// <param name="buf">Array of bytes to read into</param>
        /// <param name="size">The size of each element to read; each will be endian-flipped if necessary</param>
        /// <param name="count">The number of elements to read</param>
        [OgreVersion(1, 7, 2)]
        public virtual void ReadData(byte[] buf, int size, int count)
        {
            CheckStream(true, true, false);

            var totSize = size * count;
            this.mStream.Read(buf, 0, totSize);

            if (this.mFlipEndian)
            {
                FlipEndian(buf, size, count);
            }
        }

        /// <summary>
        /// Write arbitrary data to a stream.
        /// </summary>
        /// <param name="buf">Array of bytes to write</param>
        /// <param name="size">The size of each element to write; each will be endian-flipped if necessary</param>
        /// <param name="count">The number of elements to write</param>
        [OgreVersion(1, 7, 2)]
        public virtual void WriteData(byte[] buf, int size, int count)
        {
            CheckStream(false, false, true);

            var totSize = size * count;

            if (this.mFlipEndian)
            {
                FlipEndian(buf, size, count);
            }

            this.mStream.Write(buf, 0, totSize);
        }

        protected void FlipEndian(byte[] pBase, int size, int count)
        {
            for (var c = 0; c < count; ++c)
            {
                {
                    var pData = c * size;
                    for (var byteIndex = 0; byteIndex < size / 2; byteIndex++)
                    {
                        var swapByte = pBase[pData + byteIndex];
                        pBase[pData + byteIndex] = pBase[pData + size - byteIndex - 1];
                        pBase[pData + size - byteIndex - 1] = swapByte;
                    }
                }
            }
        }

        #endregion Protected Methods

        #region IDisposable Implementation

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    lock (this)
                    {
                        if (this.mChunkStack.Count != 0)
                        {
                            Debug.WriteLine("Warning: stream was not fully read / written; " + this.mChunkStack.Count +
                                             " chunks remain unterminated.");
                        }
                        this.mChunkStack.Clear();
                        this.mStream.Dispose();
                        this.mStream = null;
                    }
                }
            }

            base.dispose(disposeManagedResources);
        }

        #endregion IDisposable Implementation
    }
}