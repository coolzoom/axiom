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

namespace Axiom.Graphics {
	/// <summary>
	///     Records the use of temporary blend buffers.
	/// </summary>
	public class TempBlendedBufferInfo : IHardwareBufferLicensee {
        #region Fields

        /// <summary>
        ///     Pre-blended position buffer.
        /// </summary>
        public HardwareVertexBuffer srcPositionBuffer;
        /// <summary>
        ///     Pre-blended normal buffer.
        /// </summary>
        public HardwareVertexBuffer srcNormalBuffer;
        /// <summary>
        ///     Post-blended position buffer.
        /// </summary>
        public HardwareVertexBuffer destPositionBuffer;
        /// <summary>
        ///     Post-blended normal buffer.
        /// </summary>
        public HardwareVertexBuffer destNormalBuffer;
        /// <summary>
        ///    Both positions and normals are contained in the same buffer. 
        /// </summary>
        public bool posNormalShareBuffer;
        /// <summary>
        ///     Index at which the positions are bound in the buffer.
        /// </summary>
        public short posBindIndex;
        /// <summary>
        ///     Index at which the normals are bound in the buffer.
        /// </summary>
        public short normBindIndex;

        #endregion Fields

        #region Methods

        /// <summary>
        ///     Utility method, checks out temporary copies of src into dest.
        /// </summary>
        public void CheckoutTempCopies() {
            // TODO: Implement CheckoutTempCopies
        }

        /// <summary>
        ///     Utility method, binds dest copies into a given VertexData.
        /// </summary>
        /// <param name="targetData">VertexData object to bind the temp buffers into.</param>
        /// <param name="suppressHardwareUpload"></param>
        public void BindTempCopies(VertexData targetData, bool suppressHardwareUpload) {
            // TODO: Implement BindTempCopies
        }

        #endregion Methods

        #region IHardwareBufferLicensee Members

        /// <summary>
        ///     Implementation of LicenseExpired.
        /// </summary>
        /// <param name="buffer"></param>
        public void LicenseExpired(HardwareBuffer buffer) {
            // TODO:  Add TempBlendedBufferInfo.LicenseExpired implementation
        }

        #endregion
    }
}
