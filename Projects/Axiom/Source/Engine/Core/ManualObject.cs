#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2007 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: ManualObject.cs 1085 2007-08-13 20:37:24Z jprice $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Axiom.Collections;
using Axiom.Math;
using Axiom.Graphics;

#endregion

namespace Axiom.Core
{
    /// <summary>
    ///    Class providing a much simplified interface to generating manual
    ///    objects with custom geometry.
    ///
    ///    Building one-off geometry objects manually usually requires getting
    ///    down and dirty with the vertex buffer and vertex declaration API, 
    ///    which some people find a steep learning curve. This class gives you 
    ///    a simpler interface specifically for the purpose of building a 
    ///    3D object simply and quickly. Note that if you intend to instance your
    ///    object you will still need to become familiar with the Mesh class. 
    ///
    ///    This class draws heavily on the interface for OpenGL 
    ///    immediate-mode (glBegin, glVertex, glNormal etc), since this
    ///    is generally well-liked by people. There are a couple of differences
    ///    in the results though - internally this class still builds hardware 
    ///    buffers which can be re-used, so you can render the resulting object
    ///    multiple times without re-issuing all the same commands again. 
    ///    Secondly, the rendering is not immediate, it is still queued just like
    ///    all OGRE/Axiom objects. This makes this object more efficient than the 
    ///    equivalent GL immediate-mode commands, so it's feasible to use it for
    ///    large objects if you really want to.
    ///
    ///    To construct some geometry with this object:
    ///      -# If you know roughly how many vertices (and indices, if you use them)
    ///         you're going to submit, call <see cref="EstimateVertexCount"/> and <see cref="EstimateIndexCount"/>.
    ///         This is not essential but will make the process more efficient by saving
    ///         memory reallocations.
    ///      -# Call <see cref="Begin"/> to begin entering data
    ///      -# For each vertex, call <see cref="Position"/>, <see cref="Normal"/>, <see cref="TextureCoord"/>, <see cref="Color"/>
    ///         to define your vertex data. Note that each time you call Position()
    ///         you start a new vertex. Note that the first vertex defines the 
    ///         components of the vertex - you can't add more after that. For example
    ///         if you didn't call Normal() in the first vertex, you cannot call it
    ///         in any others. You ought to call the same combination of methods per
    ///         vertex.
    ///      -# If you want to define triangles (or lines/points) by indexing into the vertex list, 
    ///         you can call <see cref="Index"/> as many times as you need to define them.
    ///         If you don't do this, the class will assume you want triangles drawn
    ///         directly as defined by the vertex list, ie non-indexed geometry. Note
    ///         that stencil shadows are only supported on indexed geometry, and that
    ///         indexed geometry is a little faster; so you should try to use it.
    ///      -# Call <see cref="End"/> to finish entering data.
    ///      -# Optionally repeat the begin-end cycle if you want more geometry 
    ///        using different rendering operation types, or different materials
    ///    After calling End(), the class will organise the data for that section
    ///    internally and make it ready to render with. Like any other 
    ///    MovableObject you should attach the object to a SceneNode to make it 
    ///    visible. Other aspects like the relative render order can be controlled
    ///    using standard MovableObject methods like SetRenderQueueGroup.
    ///
    ///    You can also use <see cref="BeginUpdate"/> to alter the geometry later on if you wish.
    ///    If you do this, you should set the <see cref="Dynamic"/> property to true before your first call 
    ///    to Begin(), and also consider using EstimateVertexCount()/EstimateIndexCount()
    ///    if your geometry is going to be growing, to avoid buffer recreation during
    ///    growth.
    ///
    ///    Note that like all OGRE/Axiom geometry, triangles should be specified in 
    ///    anti-clockwise winding order (whether you're doing it with just
    ///    vertices, or using indexes too). That is to say that the front of the
    ///    face is the one where the vertices are listed in anti-clockwise order.
    /// </summary>
    public class ManualObject : MovableObject
    {
        #region Constructor

        public ManualObject( string name )
        {
            base.Name = name;
            dynamic = false;
            currentSection = null;
            firstVertex = true;
            tempVertexPending = false;
            tempVertexBuffer = null;
            tempVertexSize = TEMP_INITIAL_VERTEX_SIZE;
            tempIndexBuffer = null;
            tempIndexSize = TEMP_INITIAL_INDEX_SIZE;
            declSize = 0;
            estVertexCount = 0;
            estIndexCount = 0;
            texCoordIndex = 0;
            radius = 0;
            anyIndexed = false;
            edgeList = null;
            useIdentityProjection = false;
            useIdentityView = false;

        }

        #endregion Constructor

        #region Const

        const int TEMP_INITIAL_SIZE = 50;
        const int TEMP_VERTEXSIZE_GUESS = sizeof( float ) * 12;
        const int TEMP_INITIAL_VERTEX_SIZE = TEMP_VERTEXSIZE_GUESS * TEMP_INITIAL_SIZE;
        const int TEMP_INITIAL_INDEX_SIZE = sizeof( UInt16 ) * TEMP_INITIAL_SIZE;

        #endregion Const

        #region Protected

        #region Fields

        /// List of subsections
        protected SectionList sectionList = new SectionList();
        /// Current section
        protected ManualObjectSection currentSection;
        /// Are we updating?
        protected bool currentUpdating;
        /// Temp storage
        protected TempVertex tempVertex = new TempVertex();
        /// First vertex indicator
        protected bool firstVertex;
        /// Temp vertex data to copy?
        protected bool tempVertexPending;
        /// System-memory buffer whilst we establish the size required
        protected byte[] tempVertexBuffer;
        /// System memory allocation size, in bytes
        protected int tempVertexSize;
        /// System-memory buffer whilst we establish the size required
        protected UInt16[] tempIndexBuffer;
        /// System memory allocation size, in bytes
        protected int tempIndexSize;
        /// Current declaration vertex size
        protected int declSize;
        /// Estimated vertex count
        protected int estVertexCount;
        /// Estimated index count
        protected int estIndexCount;
        /// Current texture coordinate
        protected ushort texCoordIndex;
        /// Bounding box
        protected AxisAlignedBox AABB = new AxisAlignedBox();
        /// Bounding sphere
        protected float radius;
        /// Any indexed geoemtry on any sections?
        protected bool anyIndexed;
        /// Edge list, used if stencil shadow casting is enabled 
        protected EdgeData edgeList;
        /// List of shadow renderables
        protected ShadowRenderableList shadowRenderables;
        /// Whether geometry will be updated
        protected bool dynamic;
        /// Whether to use identity projection for sections
        protected bool useIdentityProjection;
        /// Whether to use identity view for sections
        protected bool useIdentityView;

        #endregion Fields

        #region Methods

        /// <summary>
        /// Delete temp buffers and reset init counts
        /// </summary>
        protected virtual void ResetTempAreas()
        {
            tempVertexBuffer = null;
            tempIndexBuffer = null;
            tempVertexSize = TEMP_INITIAL_VERTEX_SIZE;
            tempIndexSize = TEMP_INITIAL_INDEX_SIZE;
        }

        /// <summary>
        /// Resize the temp vertex buffer
        /// </summary>
        /// <param name="numVerts">Number of vertices</param>
        protected virtual void ResizeTempVertexBufferIfNeeded( int numVerts )
        {
            // Calculate byte size
            // Use decl if we know it by now, otherwise default size to pos/norm/texcoord*2
            int newSize;
            if ( !firstVertex )
            {
                newSize = declSize * numVerts;
            }
            else
            {
                // estimate - size checks will deal for subsequent verts
                newSize = TEMP_VERTEXSIZE_GUESS * numVerts;
            }
            if ( newSize > tempVertexSize || tempVertexBuffer == null )
            {
                if ( tempVertexBuffer == null )
                {
                    // init
                    newSize = tempVertexSize;
                }
                else
                {
                    // increase to at least double current
                    newSize = (int)Math.Utility.Max( (float)newSize, (float)tempVertexSize * 2.0f );
                }
                // copy old data
                byte[] tmp = tempVertexBuffer;
                tempVertexBuffer = new byte[ newSize ];
                if ( tmp != null )
                {
                    tmp.CopyTo( tempVertexBuffer, 0 );
                    tmp = null;
                }
                tempVertexSize = newSize;
            }
        }

        /// <summary>
        /// Resize the index buffer
        /// </summary>
        /// <param name="numInds">Number of indices</param>
        protected virtual void ResizeTempIndexBufferIfNeeded( int numInds )
        {
            int newSize = numInds * sizeof( UInt16 );
            if ( newSize > tempIndexSize || tempIndexBuffer == null )
            {
                if ( tempIndexBuffer == null )
                {
                    // init
                    newSize = tempIndexSize;
                }
                else
                {
                    // increase to at least double current
                    newSize = (int)Math.Utility.Max( (float)newSize, (float)tempIndexSize * 2 );
                }
                numInds = newSize / sizeof( UInt16 );
                UInt16[] tmp = tempIndexBuffer;
                tempIndexBuffer = new UInt16[ numInds ];
                if ( tmp != null )
                {
                    tmp.CopyTo( tempIndexBuffer, 0 );
                    tmp = null;
                }
                tempIndexSize = newSize;
            }
        }

        /// <summary>
        /// Copies temporary vertex buffer to hardware buffer
        /// </summary>
        protected virtual void CopyTempVertexToBuffer()
        {
            tempVertexPending = false;
            RenderOperation rop = currentSection.RenderOperation;

            if ( rop.vertexData.vertexCount == 0 && !currentUpdating )
            {
                // first vertex, autoorganise decl
                VertexDeclaration oldDcl = rop.vertexData.vertexDeclaration;
                rop.vertexData.vertexDeclaration =
                    oldDcl.GetAutoOrganizedDeclaration( false, false );

                HardwareBufferManager.Instance.DestroyVertexDeclaration( oldDcl );
            }

            ResizeTempVertexBufferIfNeeded( ++rop.vertexData.vertexCount );

            List<VertexElement> elemList = rop.vertexData.vertexDeclaration.Elements;

            unsafe
            {
                // get base pointer
                fixed ( byte* pBase = &tempVertexBuffer[ declSize * ( rop.vertexData.vertexCount - 1 ) ] )
                {
                    foreach ( VertexElement elem in elemList )
                    {
                        float* pFloat = null;
                        UInt32* pRGBA = null;

                        switch ( elem.Type )
                        {
                            case VertexElementType.Float1:
                            case VertexElementType.Float2:
                            case VertexElementType.Float3:
                                pFloat = (float*)( (byte*)pBase + elem.Offset );
                                break;

                            case VertexElementType.Color:
                            case VertexElementType.Color_ABGR:
                            case VertexElementType.Color_ARGB:
                                pRGBA = (uint*)( (byte*)pBase + elem.Offset );
                                break;
                            default:
                                // nop ?
                                break;
                        }

                        RenderSystem rs;
                        int dims;
                        switch ( elem.Semantic )
                        {
                            case VertexElementSemantic.Position:
                                *pFloat++ = tempVertex.position.x;
                                *pFloat++ = tempVertex.position.y;
                                *pFloat++ = tempVertex.position.z;
                                break;
                            case VertexElementSemantic.Normal:
                                *pFloat++ = tempVertex.normal.x;
                                *pFloat++ = tempVertex.normal.y;
                                *pFloat++ = tempVertex.normal.z;
                                break;
                            case VertexElementSemantic.TexCoords:
                                dims = VertexElement.GetTypeCount( elem.Type );
                                for ( int t = 0; t < dims; ++t )
                                    *pFloat++ = tempVertex.texCoord[ elem.Index ][ t ];
                                break;
                            case VertexElementSemantic.Diffuse:
                                rs = Root.Instance.RenderSystem;
                                if ( rs != null )
                                    *pRGBA++ = (uint)rs.ConvertColor( tempVertex.color );
                                else
                                    *pRGBA++ = (uint)tempVertex.color.ToRGBA(); // pick one!
                                break;
                            default:
                                // nop ?
                                break;
                        }
                    }

                }
            }

        }

        #endregion Methods

        #endregion Protected

        #region Public

        #region Properties


        /// <summary>
        ///    Movable type
        /// </summary>
        public string MovableType
        {
            get
            {
                return "ManualObject";
            }
        }

        /// <summary>
        /// Usually ManualObjects will use a projection matrix as determined
        ///	by the active camera. However, if they want they can cancel this out
        ///	and use an identity projection, which effectively projects in 2D using
        ///	a {-1, 1} view space. Useful for overlay rendering. Normally you don't
        //	need to change this. The default is false.
        /// </summary>
        public bool UseIdentityProjection
        {
            get
            {
                return useIdentityProjection;
            }
            set
            {

                // Set existing
                foreach ( ManualObjectSection sec in sectionList )
                {
                    sec.UseIdentityProjection = value;
                }

                // Save setting for future sections
                useIdentityProjection = value;
            }
        }

        /// <summary>
        /// Usually ManualObjects will use a view matrix as determined
        ///	by the active camera. However, if they want they can cancel this out
        ///	and use an identity matrix, which means all geometry is assumed
        ///	to be relative to camera space already. Useful for overlay rendering. 
        ///	Normally you don't need to change this. The default is false.
        /// </summary>
        public bool UseIdentityView
        {
            get
            {
                return useIdentityView;
            }
            set
            {

                // Set existing
                foreach ( ManualObjectSection sec in sectionList )
                {
                    sec.UseIdentityView = value;
                }

                // Save setting for future sections
                useIdentityView = value;
            }
        }

        /// <summary>
        /// Retrieves the number of <see cref="ManualObjectSection"/> objects making up this ManualObject.
        /// </summary>
        public int NumSections
        {
            get
            {
                return sectionList.Count;
            }
        }

        /// <summary>
        /// Use before defining geometry to indicate that you intend to update the
        /// geometry regularly and want the internal structure to reflect that.
        /// </summary>
        public bool Dynamic
        {
            get
            {
                return dynamic;
            }
            set
            {
                dynamic = value;
            }
        }

        #endregion Properties

        #region Methods

        ///<summary>
        /// Clearing the contents of this object and rebuilding from scratch
        /// is not the optimal way to manage dynamic vertex data, since the 
        /// buffers are recreated. If you want to keep the same structure but
        /// update the content within that structure, use <see cref="BeginUpdate"/> instead 
        /// of <see cref="Clear"/> <see cref="Begin"/>. However if you do want to modify the structure 
        /// from time to time you can do so by clearing and re-specifying the data.
        ///</summary>
        public virtual void Clear()
        {
            ResetTempAreas();
            sectionList.Clear();
            radius = 0;
            AABB = null;
            edgeList = null;
            anyIndexed = false;
            shadowRenderables.Clear();
        }


        ///<summary>
        /// Calling this helps to avoid memory reallocations when you define
        /// vertices. 
        ///</summary>
        public virtual void EstimateVertexCount( int vcount )
        {
            ResizeTempVertexBufferIfNeeded( vcount );
            estVertexCount = vcount;
        }

        ///<summary>
        /// Calling this helps to avoid memory reallocations when you define
        /// indices. 
        ///</summary>
        public virtual void EstimateIndexCount( int icount )
        {
            ResizeTempIndexBufferIfNeeded( icount );
            estIndexCount = icount;
        }

        ///<summary>
        /// Each time you call this method, you start a new section of the
        /// object with its own material and potentially its own type of
        /// rendering operation (triangles, points or lines for example).
        ///</summary>
        ///<param name="materialName">The name of the material to render this part of the object with.</param>
        ///<param name="opType">The type of operation to use to render.</param>
        public virtual void Begin( string materialName, OperationType opType )
        {
            if ( currentSection != null )
            {
                throw new AxiomException( "ManualObject:Begin - You cannot call Begin() again until after you call End()" );
            }

            currentSection = new ManualObjectSection( this, materialName, opType );
            currentUpdating = false;
            currentSection.UseIdentityProjection = useIdentityProjection;
            currentSection.UseIdentityView = useIdentityView;
            sectionList.Add( currentSection );
            firstVertex = true;
            declSize = 0;
            texCoordIndex = 0;
        }

        ///<summary>
        /// Using this method, you can update an existing section of the object
        /// efficiently. You do not have the option of changing the operation type
        /// obviously, since it must match the one that was used before. 
        /// </summary>
        /// <remarks>
        /// If your sections are changing size, particularly growing, use
        ///	<see cref="EstimateVertexCount"/> and <see cref="EstimateIndexCount"/> to pre-size the buffers a little
        ///	larger than the initial needs to avoid buffer reconstruction.
        /// </remarks>
        /// <param name="sectionIndex">The index of the section you want to update. The first
        ///	call to <see cref="Begin"/> would have created section 0, the second section 1, etc.
        ///	</param>
        public virtual void BeginUpdate( int sectionIndex )
        {
            if ( currentSection != null )
            {
                throw new AxiomException( "ManualObject.BeginUpdate - You cannot call Begin() again until after you call End()" );
            }

            if ( sectionIndex >= sectionList.Count )
            {
                throw new AxiomException( "ManualObject.BeginUpdate - Invalid section index - out of range." );
            }
            currentSection = sectionList[ sectionIndex ];
            currentUpdating = true;
            firstVertex = true;
            texCoordIndex = 0;
            // reset vertex & index count
            RenderOperation rop = currentSection.RenderOperation;
            rop.vertexData.vertexCount = 0;
            if ( rop.indexData != null )
                rop.indexData.indexCount = 0;
            rop.useIndices = false;
            declSize = rop.vertexData.vertexDeclaration.GetVertexSize( 0 );
        }

        ///<summary>
        /// A vertex position is slightly special among the other vertex data
        /// methods like <see cref="Normal"/> and <see cref="TextureCoord"/>, since calling it indicates
        /// the start of a new vertex. All other vertex data methods you call 
        /// after this are assumed to be adding more information (like normals or
        /// texture coordinates) to the last vertex started with <see cref="Position"/>.
        /// </summary>
        /// <param name="pos">Position as a <see cref="Vector3"/></param>
        public virtual void Position( Vector3 pos )
        {
            Position( pos.x, pos.y, pos.z );
        }

        ///<summary>Vertex Position</summary>
        ///<param name="x">x value of position as a float</param>
        ///<param name="y">y value of position as a float</param>
        ///<param name="z">z value of position as a float</param>
        public virtual void Position( float x, float y, float z )
        {

            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.Position - You must call Begin() before this method" );
            }

            if ( tempVertexPending )
            {
                // bake current vertex
                CopyTempVertexToBuffer();
                firstVertex = false;
            }

            if ( firstVertex && !currentUpdating )
            {
                // defining declaration
                currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, declSize, VertexElementType.Float3, VertexElementSemantic.Position );
                declSize += VertexElement.GetTypeSize( VertexElementType.Float3 );
            }

            tempVertex.position.x = x;
            tempVertex.position.y = y;
            tempVertex.position.z = z;

            // update bounds
            AABB.Merge( tempVertex.position );
            radius = Math.Utility.Max( radius, tempVertex.position.Length );

            // reset current texture coord
            texCoordIndex = 0;

            tempVertexPending = true;
        }

        ///<summary>
        /// Vertex normals are most often used for dynamic lighting, and 
        /// their components should be normalised.
        /// </summary>
        /// <param name="norm">Normal as Vector3</param>
        public virtual void Normal( Vector3 norm )
        {
            Normal( norm.x, norm.y, norm.z );
        }

        /// <summary>
        /// Normal value
        /// </summary>
        /// <param name="x">x value of vector as float</param>
        /// <param name="y">y value of vector as float</param>
        /// <param name="z">z value of vector as float</param>
        public virtual void Normal( float x, float y, float z )
        {
            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.Normal - You must call Begin() before this method" );
            }

            if ( firstVertex && !currentUpdating )
            {
                // defining declaration
                currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, declSize, VertexElementType.Float3, VertexElementSemantic.Normal );

                declSize += VertexElement.GetTypeSize( VertexElementType.Float3 );
            }

            tempVertex.normal.x = x;
            tempVertex.normal.y = y;
            tempVertex.normal.z = z;

        }

        ///<summary>
        /// You can call this method multiple times between <see cref="Position"/> calls
        /// to add multiple texture coordinates to a vertex. Each one can have
        /// between 1 and 3 dimensions, depending on your needs, although 2 is
        /// most common. There are several versions of this method for the 
        /// variations in number of dimensions.
        ///</summary>
        ///<param name="u">u coordinate as float</param>
        public virtual void TextureCoord( float u )
        {
            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.TextureCoord - You must call Begin() before this method" );
            }

            if ( firstVertex && !currentUpdating )
            {
                // defining declaration
                currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, declSize, VertexElementType.Float1, VertexElementSemantic.TexCoords, texCoordIndex );
                declSize += VertexElement.GetTypeSize( VertexElementType.Float1 );
            }

            tempVertex.texCoordDims[ texCoordIndex ] = 1;
            tempVertex.texCoord[ texCoordIndex ].x = u;

            ++texCoordIndex;

        }

        /// <summary>
        /// Texture coordinate
        /// </summary>
        /// <param name="u">u coordinate as float</param>
        /// <param name="v">v coordinate as float</param>
        public virtual void TextureCoord( float u, float v )
        {
            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.TextureCoord - You must call Begin() before this method" );
            }

            if ( firstVertex && !currentUpdating )
            {
                // defining declaration
                currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, declSize, VertexElementType.Float2, VertexElementSemantic.TexCoords, texCoordIndex );
                declSize += VertexElement.GetTypeSize( VertexElementType.Float2 );
            }

            tempVertex.texCoordDims[ texCoordIndex ] = 2;
            tempVertex.texCoord[ texCoordIndex ].x = u;
            tempVertex.texCoord[ texCoordIndex ].y = v;

            ++texCoordIndex;

        }

        /// <summary>
        /// Texture Coordinate
        /// </summary>
        /// <param name="u">u coordinate as float</param>
        /// <param name="v">v coordinate as float</param>
        /// <param name="w">w coordinate as float</param>
        public virtual void TextureCoord( float u, float v, float w )
        {
            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.TextureCoord - You must call Begin() before this method" );
            }

            if ( firstVertex && !currentUpdating )
            {
                // defining declaration
                currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, declSize, VertexElementType.Float3, VertexElementSemantic.TexCoords, texCoordIndex );
                declSize += VertexElement.GetTypeSize( VertexElementType.Float3 );
            }

            tempVertex.texCoordDims[ texCoordIndex ] = 3;
            tempVertex.texCoord[ texCoordIndex ].x = u;
            tempVertex.texCoord[ texCoordIndex ].y = v;
            tempVertex.texCoord[ texCoordIndex ].z = w;

            ++texCoordIndex;

        }

        /// <summary>
        /// Texture coordinate
        /// </summary>
        /// <param name="uv">uv coordinate as Vector2</param>
        public virtual void TextureCoord( Vector2 uv )
        {
            TextureCoord( uv.x, uv.y );
        }

        /// <summary>
        /// Texture Coordinate
        /// </summary>
        /// <param name="uvw">uvw coordinate as Vector3</param>
        public virtual void TextureCoord( Vector3 uvw )
        {
            TextureCoord( uvw.x, uvw.y, uvw.z );
        }

        /// <summary>Add a vertex color to a vertex</summary>
        /// <param name="col">col as ColorEx object</param>
        public virtual void Color( ColorEx col )
        {
            Color( col.r, col.g, col.b, col.a );
        }

        ///<summary>Add a vertex color to a vertex</summary>
        ///<param name="r">r color component as float</param>
        ///<param name="g">g color component as float</param>
        ///<param name="b">b color component as float</param>
        ///<param name="a">a color component as float</param>
        public virtual void Color( float r, float g, float b, float a )
        {
            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.Color - You must call Begin() before this method" );
            }

            if ( firstVertex && !currentUpdating )
            {
                // defining declaration
                currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, declSize, VertexElementType.Color, VertexElementSemantic.Diffuse );
                declSize += VertexElement.GetTypeSize( VertexElementType.Color );
            }

            tempVertex.color.r = r;
            tempVertex.color.g = g;
            tempVertex.color.b = b;
            tempVertex.color.a = a;

        }

        ///<summary>
        ///Add a vertex index to construct faces / lines / points via indexing
        /// rather than just by a simple list of vertices.
        /// <remarks>
        /// You will have to call this 3 times for each face for a triangle list, 
        /// or use the alternative 3-parameter version. Other operation types
        /// require different numbers of indexes, <see cref="RenderOperation.OperationType"/>.
        /// 32-bit indexes are not supported on all cards which is why this 
        /// class only allows 16-bit indexes, for simplicity and ease of use.
        /// </remarks>
        /// <param name="idx">A vertex index from 0 to 65535.</param>
        public virtual void Index( UInt16 idx )
        {
            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.Index - You must call Begin() before this method" );
            }

            anyIndexed = true;
            // make sure we have index data
            RenderOperation rop = currentSection.RenderOperation;
            if ( rop.indexData == null )
            {
                rop.indexData = new IndexData();
                rop.indexData.indexCount = 0;
            }

            rop.useIndices = true;
            ResizeTempIndexBufferIfNeeded( ++rop.indexData.indexCount );

            tempIndexBuffer[ rop.indexData.indexCount - 1 ] = idx;

        }

        /** 
        @note
            32-bit indexes are not supported on all cards which is why this 
            class only allows 16-bit indexes, for simplicity and ease of use.
        @param i1, i2, i3 3 vertex indices from 0 to 65535 defining a face. 
        */

        ///<summary>
        /// Add a set of 3 vertex indices to construct a triangle; this is a
        /// shortcut to calling <see cref="Index"/> 3 times. It is only valid for triangle 
        /// lists.
        ///</summary>
        public virtual void Triangle( UInt16 i1, UInt16 i2, UInt16 i3 )
        {
            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.Triangle - You must call Begin() before this method" );
            }

            if ( currentSection.RenderOperation.operationType != OperationType.TriangleList )
            {
                throw new AxiomException( "ManualObject.Triangle - This method is only valid on triangle lists" );
            }

            Index( i1 );
            Index( i2 );
            Index( i3 );

        }

        ///<summary>
        /// Add a set of 4 vertex indices to construct a quad (out of 2 
        /// triangles); this is a shortcut to calling <see cref="Index"/> 6 times, 
        /// or <see cref="Triangle"/> twice. It's only valid for triangle list operations.
        ///</summary>
        ///<param name="i1">vertex index from 0 to 65535 defining a face</param>
        ///<param name="i2">vertex index from 0 to 65535 defining a face</param>
        ///<param name="i3">vertex index from 0 to 65535 defining a face</param>
        ///<param name="i4">vertex index from 0 to 65535 defining a face</param>
        public virtual void Quad( UInt16 i1, UInt16 i2, UInt16 i3, UInt16 i4 )
        {
            // first tri
            Triangle( i1, i2, i3 );
            // second tri
            Triangle( i3, i4, i1 );
        }

        ///<summary>
        /// Finish defining the object and compile the final renderable version.
        ///</summary>
        public virtual ManualObjectSection End()
        {
            if ( currentSection == null )
            {
                throw new AxiomException( "ManualObject.End - You cannot call End() until after you call Begin()" );
            }

            if ( tempVertexPending )
            {
                // bake current vertex
                CopyTempVertexToBuffer();
            }

            // pointer that will be returned
            ManualObjectSection result = null;

            RenderOperation rop = currentSection.RenderOperation;

            // Check for empty content
            if ( rop.vertexData.vertexCount == 0 || ( rop.useIndices && rop.indexData.indexCount == 0 ) )
            {
                // You're wasting my time sonny
                if ( currentUpdating )
                {
                    // Can't just undo / remove since may be in the middle
                    // Just allow counts to be 0, will not be issued to renderer

                    // return the finished section (though it has zero vertices)
                    result = currentSection;
                }
                else
                {
                    // First creation, can really undo
                    // Has already been added to section list end, so remove
                    if ( sectionList.Count > 0 )
                        sectionList.RemoveAt( sectionList.Count - 1 );
                }
            }
            else // not an empty section
            {
                // Bake the real buffers
                HardwareVertexBuffer vbuf = null;
                // Check buffer sizes
                bool vbufNeedsCreating = true;
                bool ibufNeedsCreating = rop.useIndices;

                if ( currentUpdating )
                {
                    // May be able to reuse buffers, check sizes
                    vbuf = rop.vertexData.vertexBufferBinding.GetBuffer( 0 );
                    if ( vbuf.VertexCount >= rop.vertexData.vertexCount )
                        vbufNeedsCreating = false;

                    if ( rop.useIndices )
                    {
                        if ( rop.indexData.indexBuffer.IndexCount >= rop.indexData.indexCount )
                            ibufNeedsCreating = false;
                    }

                }

                if ( vbufNeedsCreating )
                {
                    // Make the vertex buffer larger if estimated vertex count higher
                    // to allow for user-configured growth area
                    int vertexCount = (int)Math.Utility.Max( rop.vertexData.vertexCount, estVertexCount );

                    vbuf = HardwareBufferManager.Instance.CreateVertexBuffer(
                            declSize,
                            vertexCount,
                            dynamic ? BufferUsage.DynamicWriteOnly :
                                BufferUsage.StaticWriteOnly );

                    rop.vertexData.vertexBufferBinding.SetBinding( 0, vbuf );
                }

                if ( ibufNeedsCreating )
                {
                    // Make the index buffer larger if estimated index count higher
                    // to allow for user-configured growth area
                    int indexCount = (int)Utility.Max( rop.indexData.indexCount, estIndexCount );
                    rop.indexData.indexBuffer =
                        HardwareBufferManager.Instance.CreateIndexBuffer(
                            IndexType.Size16, indexCount, dynamic ?
                            BufferUsage.DynamicWriteOnly : BufferUsage.StaticWriteOnly );
                }

                // Write vertex data
                if ( vbuf != null )
                    vbuf.WriteData( 0, rop.vertexData.vertexCount * vbuf.VertexSize, tempVertexBuffer, true );

                // Write index data
                if ( rop.useIndices )
                {
                    rop.indexData.indexBuffer.WriteData( 0, rop.indexData.indexCount * rop.indexData.indexBuffer.IndexSize,
                        tempIndexBuffer, true );
                }

                // return the finished section
                result = currentSection;

            } // empty section check

            currentSection = null;
            ResetTempAreas();

            // Tell parent if present
            if ( ParentNode != null )
            {
                ParentNode.NeedUpdate();
            }

            // will return the finished section or NULL if
            // the section was empty (i.e. zero vertices/indices)
            return result;

        }

        /// <summary>
        /// Alter the material for a subsection of this object after it has been
        ///	specified.
        ///	You specify the material to use on a section of this object during the
        ///	call to <see cref="Begin"/>, however if you want to change the material afterwards
        ///	you can do so by calling this method.
        /// </summary>
        /// <param name="idx">The index of the subsection to alter</param>
        /// <param name="name">The name of the new material to use</param>
        public virtual void SetMaterialName( int idx, string name )
        {
            if ( idx >= sectionList.Count )
            {
                throw new AxiomException( "ManualObject.SetMaterialName - Index out of bounds!" );
            }

            sectionList[ idx ].MaterialName = name;

        }

        ///<summary>
        /// After you've finished building this object, you may convert it to 
        /// a <see cref="Mesh"/> if you want in order to be able to create many instances of
        /// it in the world (via <see cref="Entity"/>). This is optional, since this instance
        /// can be directly attached to a <see cref="SceneNode"/> itself, but of course only
        /// one instance of it can exist that way. 
        ///</summary>
        ///<remarks>Only objects which use indexed geometry may be converted to a mesh.</remarks>
        ///<param name="meshName">The name to give the mesh</param>
        ///<param name="groupName">The resource group to create the mesh in</param>

        public virtual Mesh ConvertToMesh( string meshName, string groupName )
        {
            if ( currentSection != null )
            {
                throw new AxiomException( "ManualObject.ConvertToMesh - You cannot call ConvertToMesh() whilst you are in the middle of defining the object; call End() first." );
            }

            if ( sectionList.Count == 0 )
            {
                throw new AxiomException( "ManualObject.ConvertToMesh - No data defined to convert to a mesh." );
            }

            foreach ( ManualObjectSection sec in sectionList )
            {
                if ( !sec.RenderOperation.useIndices )
                {
                    throw new AxiomException( "ManualObject.ConvertToMesh - Only indexed geometry may be converted to a mesh." );
                }
            }

            Mesh m = MeshManager.Instance.CreateManual( meshName, groupName, null );

            foreach ( ManualObjectSection sec in sectionList )
            {
                RenderOperation rop = sec.RenderOperation;
                SubMesh sm = m.CreateSubMesh();
                sm.useSharedVertices = false;
                sm.operationType = rop.operationType;
                sm.MaterialName = sec.MaterialName;
                // Copy vertex data; replicate buffers too
                sm.vertexData = rop.vertexData.Clone( true );
                // Copy index data; replicate buffers too
                sm.indexData = rop.indexData.Clone( true );
            }
            // update bounds
            m.BoundingBox = AABB;
            m.BoundingSphereRadius = radius;

            m.Load();

            return m;

        }

        /// <summary>
        /// Gets a reference to a <see cref="ManualObjectSection"/>, ie a part of a ManualObject.
        /// </summary>
        /// <param name="index">Index of section to get</param>
        /// <returns></returns>
        public ManualObjectSection GetSection( int index )
        {
            if ( index >= sectionList.Count )
                throw new AxiomException( "ManualObject.GetSection - Index out of bounds." );

            return sectionList[ index ];
        }

        /// <summary>
        /// Implement this method to enable stencil shadows.
        /// </summary>
        public EdgeData GetEdgeList()
        {
            // Build on demand
            if ( edgeList == null && anyIndexed )
            {
                EdgeListBuilder eb = new EdgeListBuilder();
                int vertexSet = 0;
                bool anyBuilt = false;
                foreach ( ManualObjectSection sec in sectionList )
                {
                    RenderOperation rop = sec.RenderOperation;
                    // Only indexed triangle geometry supported for stencil shadows
                    if ( rop.useIndices && rop.indexData.indexCount != 0 &&
                        ( rop.operationType == OperationType.TriangleFan ||
                         rop.operationType == OperationType.TriangleList ||
                         rop.operationType == OperationType.TriangleStrip ) )
                    {
                        eb.AddVertexData( rop.vertexData );
                        eb.AddIndexData( rop.indexData, vertexSet++ );
                        anyBuilt = true;
                    }
                }

                if ( anyBuilt )
                    edgeList = eb.Build();

            }

            return edgeList;

        }

        /// <summary>
        /// Does the edge list exist? Attempts to build one if not.
        /// </summary>
        /// <returns>true if list exists</returns>
        public bool HasEdgeList()
        {
            return GetEdgeList() != null;
        }

        #endregion Methods

        #endregion Public

        #region MovableObject

        /// <summary>
        ///    Get bounding box for this object
        /// </summary>
        public override AxisAlignedBox BoundingBox
        {
            get
            {
                return (AxisAlignedBox)AABB.Clone();
            }
        }

        /// <summary>
        ///    Local bounding radius of this object.
        /// </summary>
        public override float BoundingRadius
        {
            get
            {
                return radius;
            }
        }

        public override void NotifyCurrentCamera( Camera camera )
        {
        }

        /// <summary>
        /// Add sections that make up this ManualObject to a rendering queue.
        /// This is called by the engine automatically if the object is attached to a <see cref="SceneNode"/>.
        /// </summary>
        /// <param name="queue">Rendering queue to add this object</param>
        public override void UpdateRenderQueue( RenderQueue queue )
        {
            foreach ( ManualObjectSection sec in sectionList )
            {
                // Skip empty sections (only happens if non-empty first, then updated)
                RenderOperation rop = sec.RenderOperation;
                if ( rop.vertexData.vertexCount == 0 ||
                    ( rop.useIndices && rop.indexData.indexCount == 0 ) )
                    continue;

                if ( this.renderQueueIDSet )
                    queue.AddRenderable( sec, this.renderQueueID );
                else
                    queue.AddRenderable( sec );
            }

        }

        /// <summary>
        /// Implement this method to enable stencil shadows.
        /// </summary>
        /// <param name="technique">Render technique</param>
        /// <param name="light">Light source</param>
        /// <param name="indexBuffer">Index buffer</param>
        /// <param name="extrudeVertices">Extrude (true or false)</param>
        /// <param name="extrusionDistance">Extrusion distance</param>
        /// <param name="flags">Flag parameters</param>
        /// <returns></returns>
        public override IEnumerator GetShadowVolumeRenderableEnumerator( ShadowTechnique technique, Light light,
            HardwareIndexBuffer indexBuffer, bool extrudeVertices, float extrusionDistance, int flags )
        {
            Debug.Assert( indexBuffer != null, "Only external index buffers are supported right now" );
            Debug.Assert( indexBuffer.Type == IndexType.Size16, "Only 16-bit indexes supported for now" );

            EdgeData edgeList = GetEdgeList();

            if ( edgeList == null )
            {
                return shadowRenderables.GetEnumerator();
            }

            // Calculate the object space light details
            Vector4 lightPos = light.GetAs4DVector();
            Matrix4 world2Obj = this.ParentNode.FullTransform.Inverse();
            lightPos = world2Obj.TransformAffine( lightPos );

            // Init shadow renderable list if required (only allow indexed)
            bool init = ( shadowRenderables.Count == 0 && anyIndexed );

            ManualObjectSectionShadowRenderable esr = null;
            ManualObjectSection seci = null;

            if ( init )
            {
                shadowRenderables.Capacity = edgeList.edgeGroups.Count;
            }

            EdgeData.EdgeGroup egi;

            for ( int i = 0; i < shadowRenderables.Capacity; i++ )
            {
                // Skip non-indexed geometry
                egi = (EdgeData.EdgeGroup)edgeList.edgeGroups[ i ];
                seci = sectionList[ i ];

                if ( seci.RenderOperation.useIndices )
                {
                    continue;
                }

                if ( init )
                {
                    // Create a new renderable, create a separate light cap if
                    // we're using a vertex program (either for this model, or
                    // for extruding the shadow volume) since otherwise we can
                    // get depth-fighting on the light cap
                    Material mat = seci.Material;
                    mat.Load();
                    bool vertexProgram = false;
                    Technique t = mat.GetBestTechnique();
                    for ( int p = 0; p < t.PassCount; ++p )
                    {
                        Pass pass = t.GetPass( p );
                        if ( pass.HasVertexProgram )
                        {
                            vertexProgram = true;
                            break;
                        }
                    }

                    esr = new ManualObjectSectionShadowRenderable( this, indexBuffer, egi.vertexData, vertexProgram || !extrudeVertices, false );
                    shadowRenderables.Add( esr );
                }
                // Get shadow renderable
                esr = (ManualObjectSectionShadowRenderable)shadowRenderables[ i ];

                // Extrude vertices in software if required
                if ( extrudeVertices )
                {
                    ExtrudeVertices( esr.PositionBuffer, egi.vertexData.vertexCount, lightPos, extrusionDistance );
                }

            }

            // Calc triangle light facing
            UpdateEdgeListLightFacing( edgeList, lightPos );

            // Generate indexes and update renderables
            GenerateShadowVolume( edgeList, indexBuffer, light, shadowRenderables, flags );

            return shadowRenderables.GetEnumerator();
        }

        #endregion MovableObject

        #region Nested types

        #region TempVertex

        /// <summary>
        /// Temporary vertex structure
        /// </summary>
        protected class TempVertex
        {
            public Vector3 position = Vector3.Zero;
            public Vector3 normal = Vector3.Zero;
            public Vector3[] texCoord = new Vector3[ Axiom.Configuration.Config.MaxTextureCoordSets ];
            public ushort[] texCoordDims = new ushort[ Axiom.Configuration.Config.MaxTextureCoordSets ];
            public ColorEx color = ColorEx.White;
        }

        #endregion TempVertex

        #region ManualObjectSection

        ///<summary>
        /// Built, renderable section of geometry
        ///</summary>
        public class ManualObjectSection : IRenderable
        {
            #region Protected fields

            protected ManualObject parent = null;
            protected string materialName;
            protected RenderOperation renderOperation = new RenderOperation();
            protected Hashtable customParams = new Hashtable( 20 );

            #endregion Protected fields

            #region Constructor

            public ManualObjectSection( ManualObject parent, string materialName,
                OperationType opType )
            {
                this.parent = parent;
                this.materialName = materialName;
                renderOperation.operationType = opType;
                // default to no indexes unless we're told
                renderOperation.useIndices = false;
                renderOperation.vertexData = new VertexData();
                renderOperation.vertexData.vertexCount = 0;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Get the material name in use
            /// </summary>
            public string MaterialName
            {
                get
                {
                    return materialName;
                }

                set
                {
                    if ( materialName != value )
                    {
                        materialName = value;
                        _material = null;
                    }
                }
            }

            /// <summary>
            /// Get render operation for manipulation
            /// </summary>
            public RenderOperation RenderOperation
            {
                get
                {
                    return renderOperation;
                }
            }

            #endregion

            #region IRenderable Members

            #region Methods

            public void GetRenderOperation( RenderOperation op )
            {
                op.useIndices = this.renderOperation.useIndices;
                op.operationType = this.renderOperation.operationType;
                op.vertexData = this.renderOperation.vertexData;
                op.indexData = this.renderOperation.indexData;
            }

            public void GetWorldTransforms( Matrix4[] matrices )
            {
                matrices[ 0 ] = parent.ParentNode.FullTransform;
            }

            public float GetSquaredViewDepth( Camera camera )
            {
                if ( parent.ParentNode != null )
                {
                    return parent.ParentNode.GetSquaredViewDepth( camera );
                }
                else
                    return 0.0f;
            }

            public Vector4 GetCustomParameter( int index )
            {
                if ( customParams[ index ] == null )
                {
                    throw new Exception( "A parameter was not found at the given index" );
                }
                else
                {
                    return (Vector4)customParams[ index ];
                }
            }

            public void SetCustomParameter( int index, Vector4 val )
            {
                customParams[ index ] = val;
            }

            public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
            {
                if ( customParams[ entry.data ] != null )
                {
                    gpuParams.SetConstant( entry.index, (Vector4)customParams[ entry.data ] );
                }
            }

            #endregion Methods

            #region Properties

            public bool CastsShadows
            {
                get
                {
                    return false;
                }
            }

            public Material Material
            {
                get
                {
                    if ( _material == null )
                    {
                        // Load from default group. If user wants to use alternate groups,
                        // they can define it and preload
                        _material = (Material)MaterialManager.Instance.Load( materialName, ResourceGroupManager.DefaultResourceGroupName );
                    }

                    return _material;
                }
            }
            private Material _material = null;

            public Technique Technique
            {
                get
                {
                    Material retMat = this.Material;
                    if ( retMat != null )
                        return retMat.GetBestTechnique();
                    else
                        throw new AxiomException( "ManualObject.Technique - couldn't get object material." );

                    return null;
                }
            }

            public Axiom.Collections.LightList Lights
            {
                get
                {
                    return parent.ParentNode.Lights;
                }
            }

            public bool NormalizeNormals
            {
                get
                {
                    return false;
                }
            }

            public ushort NumWorldTransforms
            {
                get
                {
                    return 1;
                }
            }

            public bool UseIdentityProjection
            {
                get
                {
                    return _useIdentityProjection;
                }
                set
                {
                    _useIdentityProjection = value;
                }
            }
            private bool _useIdentityProjection = false;


            public bool UseIdentityView
            {
                get
                {
                    return _useIdentityView;
                }
                set
                {
                    _useIdentityView = value;
                }
            }
            private bool _useIdentityView = false;


            public bool PolygonModeOverrideable
            {
                get
                {
                    return true;
                }
            }

            public Quaternion WorldOrientation
            {
                get
                {
                    return parent.ParentNode.DerivedOrientation;
                }
            }

            public Vector3 WorldPosition
            {
                get
                {
                    return parent.ParentNode.DerivedPosition;
                }
            }

            #endregion Properties

            #endregion IRenderable members
        }

        #endregion ManualObjectSection

        #region ManualObjectSectionShadowRenderable

        /// <summary>
        /// Nested class to allow shadows.
        /// </summary>
        public class ManualObjectSectionShadowRenderable : ShadowRenderable
        {
            #region Protected fields

            protected ManualObject parent;
            // Shared link to position buffer
            protected HardwareVertexBuffer positionBuffer;
            // Shared link to w-coord buffer (optional)
            protected HardwareVertexBuffer wBuffer;

            #endregion

            #region Constructor

            public ManualObjectSectionShadowRenderable( ManualObject parent,
                HardwareIndexBuffer indexBuffer, VertexData vertexData,
                bool createSeparateLightCap, bool isLightCap )
            {
                this.parent = parent;
                // Initialise render op
                renderOp.indexData = new IndexData();
                renderOp.indexData.indexBuffer = indexBuffer;
                renderOp.indexData.indexStart = 0;
                // index start and count are sorted out later

                // Create vertex data which just references position component (and 2 component)
                renderOp.vertexData = new VertexData();
                // Map in position data
                renderOp.vertexData.vertexDeclaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
                short origPosBind =
                vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position ).Source;

                positionBuffer = vertexData.vertexBufferBinding.GetBuffer( origPosBind );

                renderOp.vertexData.vertexBufferBinding.SetBinding( 0, positionBuffer );
                // Map in w-coord buffer (if present)
                if ( vertexData.hardwareShadowVolWBuffer != null )
                {
                    renderOp.vertexData.vertexDeclaration.AddElement( 1, 0, VertexElementType.Float1, VertexElementSemantic.TexCoords, 0 );
                    wBuffer = vertexData.hardwareShadowVolWBuffer;
                    renderOp.vertexData.vertexBufferBinding.SetBinding( 1, wBuffer );
                }

                // Use same vertex start as input
                renderOp.vertexData.vertexStart = vertexData.vertexStart;

                if ( isLightCap )
                {
                    // Use original vertex count, no extrusion
                    renderOp.vertexData.vertexCount = vertexData.vertexCount;
                }
                else
                {
                    // Vertex count must take into account the doubling of the buffer,
                    // because second half of the buffer is the extruded copy
                    renderOp.vertexData.vertexCount = vertexData.vertexCount * 2;
                    if ( createSeparateLightCap )
                    {
                        // Create child light cap
                        this.lightCap = new ManualObjectSectionShadowRenderable( parent,
                        indexBuffer, vertexData, false, true );
                    }
                }
            }

            #endregion Constructor

            #region Properties

            public HardwareVertexBuffer PositionBuffer
            {
                get
                {
                    return positionBuffer;
                }
            }

            public HardwareVertexBuffer WBuffer
            {
                get
                {
                    return wBuffer;
                }
            }

            #endregion Properties

            #region ShadowRenderable

            public override void GetWorldTransforms( Matrix4[] matrices )
            {
                matrices[ 0 ] = parent.ParentNode.FullTransform;
            }

            public override Quaternion WorldOrientation
            {
                get
                {
                    return parent.ParentNode.DerivedOrientation;
                }
            }

            public override Vector3 WorldPosition
            {
                get
                {
                    return parent.ParentNode.DerivedPosition;
                }
            }

            #endregion

        } // end class

        #endregion ManualObjectSectionShadowRenderable

        #region SectionList

        public class SectionList : List<ManualObjectSection>
        {
        }

        #endregion SectionList

        #endregion Nested types
    }

    #region MovableObjectFactory implementation

    public class ManualObjectFactory : MovableObjectFactory
    {
        private const string Factory_Type_Name = "ManualObject";

        public ManualObjectFactory()
        {
            this.Type = ManualObjectFactory.Factory_Type_Name;
        }

        protected override MovableObject _createInstance( string name, NameValuePairList param )
        {
            return new ManualObject( name );
        }

        public override void DestroyInstance( MovableObject obj )
        {
            obj = null;
        }
    }

    #endregion MovableObjectFactory implementation

}