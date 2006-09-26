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

#region Namespace Declarations

using System;

using DX = Microsoft.Xna.Framework;
using D3D = Microsoft.Xna.Framework;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    ///		Helper class for dealing with D3D Drivers.
    /// </summary>
    public class Driver
    {
        #region Member variables

        private int adapterNum;
        private D3D.Graphics.DisplayMode desktopMode;
        private VideoModeCollection videoModeList;
        private string name;
        private string description;

        #endregion

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Driver( D3D.Graphics.GraphicsAdapter adapterDetails )
        {
            this.desktopMode = adapterDetails.CurrentDisplayMode;
            this.name = adapterDetails.DeviceName;
            this.description = adapterDetails.Description;
            this.adapterNum = adapterDetails.DeviceId;

            videoModeList = new VideoModeCollection();
        }

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int AdapterNumber
        {
            get
            {
                return adapterNum;
            }
        }

        /// <summary>
        ///		
        /// </summary>
        public D3D.Graphics.DisplayMode DesktopMode
        {
            get
            {
                return desktopMode;
            }
        }

        /// <summary>
        ///		
        /// </summary>
        public VideoModeCollection VideoModes
        {
            get
            {
                return videoModeList;
            }
        }

        #endregion
    }
}
