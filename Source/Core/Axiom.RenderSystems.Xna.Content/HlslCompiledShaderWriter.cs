#region LGPL License
/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

using TWrite = Axiom.RenderSystems.Xna.Content.HlslCompiledShaders;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.Content
{
	/// <summary>
	/// This class will be instantiated by the XNA Framework Content Pipeline
	/// to write the specified data type into binary .xnb format.
	///
	/// This should be part of a Content Pipeline Extension Library project.
	/// </summary>
	[ContentTypeWriter]
	public class HlslCompiledShaderWriter : ContentTypeWriter<TWrite>
	{
		protected override void Write( ContentWriter output, TWrite value )
		{
			//number of compiled shaders
			output.Write( value.Count );
			for ( int i = 0; i < value.Count; ++i )
			{
				//write out compiled shader info - entry point and shader code
				output.Write( value[ i ].EntryPoint );
				output.Write( value[ i ].ShaderCode.Length );
				output.Write( value[ i ].ShaderCode );
			}
		}

		public override string GetRuntimeReader( TargetPlatform targetPlatform )
		{
			return "Axiom.RenderSystems.Xna.Content.HlslCompiledShaderReader, Axiom.RenderSystems.Xna.Content";
		}
	}
}