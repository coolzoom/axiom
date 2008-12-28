#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.Math;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleFX
{
    /// <summary>
    /// 	Summary description for DrawEmitter.
    /// </summary>
    public class DrawEmitter : ParticleEmitter
    {
        protected float distance;

        #region Constructors

        public DrawEmitter()
        {
            this.Type = "Draw";
        }

        #endregion

        #region Methods

        public override ushort GetEmissionCount( float timeElapsed )
        {
            // use basic constant emission
            return GenerateConstantEmissionCount( timeElapsed );
        }

        public override void InitParticle( Particle particle )
        {
            base.InitParticle( particle );

            Vector3 pos = new Vector3();

            pos.x = Utility.SymmetricRandom() * distance;
            pos.y = Utility.SymmetricRandom() * distance;
            pos.z = Utility.SymmetricRandom() * distance;

            // point emitter emits starting from its own position
            particle.Position = pos + particle.ParentSet.WorldPosition;

            GenerateEmissionColor( ref particle.Color );
            particle.Direction = particle.ParentSet.WorldPosition - particle.Position;
            GenerateEmissionVelocity( ref particle.Direction );

            // generate time to live
            particle.timeToLive = GenerateEmissionTTL();
        }

        #endregion

        #region Properties

        public float Distance
        {
            get
            {
                return distance;
            }
            set
            {
                distance = value;
            }
        }

        #endregion Properties

        #region Command definition classes

        [Command( "distance", "Distance from the center of the emitter where the particles spawn.", typeof( ParticleEmitter ) )]
        class DistanceCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                DrawEmitter emitter = target as DrawEmitter;
                return StringConverter.ToString( emitter.Distance );
            }
            public void Set( object target, string val )
            {
                DrawEmitter emitter = target as DrawEmitter;
                emitter.Distance = StringConverter.ParseFloat( val );
            }

            #endregion
        }

        #endregion Command definition classes
    }
}
