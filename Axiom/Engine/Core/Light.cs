#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;

using Axiom.MathLib;
using Axiom.Graphics;

namespace Axiom.Core {
    /// <summary>
    ///    Representation of a dynamic light source in the scene.
    /// </summary>
    /// <remarks>
    ///    Lights are added to the scene like any other object. They contain various
    ///    parameters like type, position, attenuation (how light intensity fades with
    ///    distance), color etc.
    ///    <p/>
    ///    The defaults when a light is created is pure white diffues light, with no
    ///    attenuation (does not decrease with distance) and a range of 1000 world units.
    ///    <p/>
    ///    Lights are created by using the SceneManager.CreateLight method. They can subsequently be
    ///    added to a SceneNode if required to allow them to move relative to a node in the scene. A light attached
    ///    to a SceneNode is assumed to have a base position of (0,0,0) and a direction of (0,0,1) before modification
    ///    by the SceneNode's own orientation. If not attached to a SceneNode,
    ///    the light's position and direction is as set using Position and Direction.
    ///    <p/>
    ///    Remember also that dynamic lights rely on modifying the color of vertices based on the position of
    ///    the light compared to an object's vertex normals. Dynamic lighting will only look good if the
    ///    object being lit has a fair level of tesselation and the normals are properly set. This is particularly
    ///    true for the spotlight which will only look right on highly tesselated models.
    /// </remarks>
    public class Light : SceneObject, IComparable {
        #region Member variables

        /// <summary>
        ///    Type of light.
        /// </summary>
        protected LightType type;
        /// <summary>
        ///    Position of this light.
        /// </summary>
        protected Vector3 position = Vector3.Zero;
        /// <summary>
        ///    Direction of this light.
        /// </summary>
        protected Vector3 direction = Vector3.UnitZ;
        /// <summary>Dervied position of this light.</summary>
        protected Vector3 derivedPosition = Vector3.Zero;
        /// <summary>Dervied direction of this light.</summary>
        protected Vector3 derivedDirection = Vector3.Zero;
        /// <summary>Stored version of parent orientation.</summary>
        protected Quaternion lastParentOrientation = Quaternion.Identity;
        /// <summary>Stored version of parent position.</summary>
        protected Vector3 lastParentPosition = Vector3.Zero;
        /// <summary>Diffuse color.</summary>
        protected ColorEx diffuse;
        /// <summary>Specular color.</summary>
        protected ColorEx specular;
        /// <summary></summary>
        protected float spotOuter;
        /// <summary></summary>
        protected float spotInner;
        /// <summary></summary>
        protected float spotFalloff;
        /// <summary></summary>
        protected float range;
        /// <summary></summary>
        protected float attenuationConst;
        /// <summary></summary>
        protected float attenuationLinear;
        /// <summary></summary>
        protected float attenuationQuad;
        /// <summary></summary>
        protected bool isModified;
        /// <summary>
        ///    Used for sorting.  Internal for "friend" access to SceneManager.
        /// </summary>
        internal float tempSquaredDist;

        #endregion
		
        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Light() {
            Construct("");
        }

        /// <summary>
        ///		Normal constructor. Should not be called directly, but rather the SceneManager.CreateLight method should be used.
        /// </summary>
        /// <param name="name"></param>
        public Light(string name) {
            Construct(name);
        }

        /// <summary>
        ///    Helper method to allow for consistent object construction.
        /// </summary>
        /// <param name="name"></param>
        protected void Construct(string name) {
            this.name = name;

            type = LightType.Point;
            diffuse = ColorEx.White;
            specular = ColorEx.Black;
            range = 1000;
            attenuationConst = 1.0f;
            attenuationLinear = 0.0f;
            attenuationQuad = 0.0f;

            position = Vector3.Zero;
            direction = Vector3.UnitZ;

            spotInner = 30.0f;
            spotOuter = 40.0f;
            spotFalloff = 1.0f;
			
            isModified = true;
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets/Sets the type of light this is.
        /// </summary>
        public LightType Type {
            get { 
                return type; 
            }
            set { 
                type = value; 
                isModified = true; 
            }
        }

        /// <summary>
        ///		Gets/Sets the position of the light.
        /// </summary>
        public Vector3 Position {
            get { 
                return position; 
            }
            set { 
                position = value; 
                isModified = true; 
            }
        }

        /// <summary>
        ///		Gets/Sets the direction of the light.
        /// </summary>
        public Vector3 Direction {
            get { 
                return direction; 
            }
            set { 
                direction = value; 
                isModified = true; 
            }
        }

        /// <summary>
        ///		Gets the inner angle of the spotlight.
        /// </summary>
        public float SpotlightInnerAngle {
            get { 
                return spotInner; 
            }
        }

        /// <summary>
        ///		Gets the outer angle of the spotlight.
        /// </summary>
        public float SpotlightOuterAngle {
            get { 
                return spotInner; 
            }
        }

        /// <summary>
        ///		Gets the spotlight falloff.
        /// </summary>
        public float SpotlightFalloff {
            get { 
                return spotInner; 
            }
        }

        /// <summary>
        ///		Gets/Sets the diffuse color of the light.
        /// </summary>
        public ColorEx Diffuse {
            get { 
                return diffuse; 
            }
            set { 
                diffuse = value; 
                isModified = true; 
            }
        }

        /// <summary>
        ///		Gets/Sets the diffuse color of the light.
        /// </summary>
        public ColorEx Specular {
            get { 
                return specular; 
            }
            set { 
                specular = value; 
                isModified = true; 
            }
        }

        /// <summary>
        ///		Gets the attenuation range value.
        /// </summary>
        public float AttenuationRange {
            get { 
                return range; 
            }
        }

        /// <summary>
        ///		Gets the constant attenuation value.
        /// </summary>
        public float AttenuationConstant {
            get { 
                return attenuationConst; 
            }
        }

        /// <summary>
        ///		Gets the linear attenuation value.
        /// </summary>
        public float AttenuationLinear {
            get { 
                return attenuationLinear; 
            }
        }

        /// <summary>
        ///		Gets the quadratic attenuation value.
        /// </summary>
        public float AttenuationQuadratic {
            get { 
                return attenuationQuad; 
            }
        }

        /// <summary>
        ///		Updates this lights position.
        /// </summary>
        public void Update() {
            if(parentNode != null) {
                if(!isModified
                    && parentNode.DerivedOrientation == lastParentOrientation
                    && parentNode.DerivedPosition == lastParentPosition) {
                }
                else {
                    // we are out of date with the scene node we are attached to
                    lastParentOrientation = parentNode.DerivedOrientation;
                    lastParentPosition = parentNode.DerivedPosition;
                    derivedDirection = lastParentOrientation * direction;
                    derivedPosition = (lastParentOrientation * position) + lastParentPosition;
                }
            }
            else {
                derivedPosition = position;
                derivedDirection = direction;
            }
        }

        /// <summary>
        ///		Gets the derived position of this light.
        /// </summary>
        public Vector3 DerivedPosition {
            get {
                // this is called to force an update
                Update();

                return derivedPosition;
            }
        }

        /// <summary>
        ///		Gets the derived position of this light.
        /// </summary>
        public Vector3 DerivedDirection {
            get {
                // this is called to force an update
                Update();

                return derivedDirection;
            }
        }

        /// <summary>
        ///		Override IsVisible to ensure we are updated when this changes.
        /// </summary>
        public override bool IsVisible {
            get {
                return base.IsVisible;
            }
            set {
                base.IsVisible = value;
            }
        }

        /// <summary>
        ///    Local bounding radius of this light.
        /// </summary>
        public override float BoundingRadius {
            get {
                // not visible
                return 0;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///		Sets the spotlight parameters in a single call.
        /// </summary>
        /// <param name="innerAngle"></param>
        /// <param name="outerAngle"></param>
        /// <param name="falloff"></param>
        public void SetSpotlightRange(float innerAngle, float outerAngle, float falloff) {
            if(type != LightType.Spotlight)
                throw new Exception("Setting the spotlight range is only valid for spotlights.");

            spotInner = innerAngle;
            spotOuter = outerAngle;
            spotFalloff = falloff;
        }

        /// <summary>
        ///		Sets the attenuation parameters of the light in a single call.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="constant"></param>
        /// <param name="linear"></param>
        /// <param name="quadratic"></param>
        public void SetAttenuation(float range, float constant, float linear, float quadratic) {
            this.range = range;
            attenuationConst = constant;
            attenuationLinear = linear;
            attenuationQuad = quadratic;
        }

        #endregion

        #region SceneObject Implementation

        public override void NotifyCurrentCamera(Camera camera) {
            // Do nothing
        }

        public override void UpdateRenderQueue(RenderQueue queue) {
            // Do Nothing	
        }

        /// <summary>
        /// 
        /// </summary>
        public override AxisAlignedBox BoundingBox {
            get {	
                return AxisAlignedBox.Null; 
            }
        }

        #endregion

        #region IComparable Members

        /// <summary>
        ///    Used to compare this light to another light for sorting.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj) {
            Light other = obj as Light;

            if(other.tempSquaredDist > this.tempSquaredDist) {
                return 1;
            }
            else {
                return -1;
            }
        }

        #endregion
    }
}
