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
using System.Drawing;
using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.MathLib;
using Axiom.Scripting;

namespace ParticleFX
{
	/// <summary>
	/// Summary description for BoxEmitter.
	/// </summary>
	public class BoxEmitter : AreaEmitter
	{
		public BoxEmitter() : base()
		{
			InitDefaults("Box");
		}

		public override void InitParticle(Particle particle)
		{
			Vector3 xOff, yOff, zOff;

			xOff = MathUtil.SymmetricRandom() * xRange;
			yOff = MathUtil.SymmetricRandom() * yRange;
			zOff = MathUtil.SymmetricRandom() * zRange;

			particle.Position = position + xOff + yOff + zOff;
	        
			// Generate complex data by reference
			GenerateEmissionColor(particle.Color);
			GenerateEmissionDirection(ref particle.Direction);
			GenerateEmissionVelocity(ref particle.Direction);

			// Generate simpler data
			particle.timeToLive = GenerateEmissionTTL();
		}

		#region Script parser methods

		[AttributeParser("width", EMITTER)]
		public static void ParseWidth(string[] values, params object[] objects) {
			BoxEmitter emitter = objects[0] as BoxEmitter;
			emitter.Width = float.Parse(values[0]);
		}

		[AttributeParser("height", EMITTER)]
		public static void ParseHeight(string[] values, params object[] objects) 
		{
			BoxEmitter emitter = objects[0] as BoxEmitter;
			emitter.Height = float.Parse(values[0]);
		}

		[AttributeParser("depth", EMITTER)]
		public static void ParseDepth(string[] values, params object[] objects) 
		{
			BoxEmitter emitter = objects[0] as BoxEmitter;
			emitter.Depth = float.Parse(values[0]);
		}

		#endregion Script parser methods

	}
}
