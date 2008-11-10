﻿#region LGPL License
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

using Axiom.Graphics;
using Axiom.RenderSystems.Xna.HLSL;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
	class HLSLShaderGenerator : ShaderGenerator
	{
        internal class TextureCoordType : Dictionary<int, VertexElementType>
        {}
        protected TextureCoordType texCordVecType = new TextureCoordType();
        #region Construction and Destruction

		public HLSLShaderGenerator()
		{
			name = "hlsl";
			languageName = "hlsl";
			vpTarget = "vs_3_0";
			fpTarget = "ps_3_0";
		}
        

		#endregion Construction and Destruction

		#region ShaderGenerator Implentation

		public override string GetShaderSource( string vertexProgramName, string fragmentProgramName, VertexBufferDeclaration vertexBufferDeclaration, FixedFunctionState fixedFunctionState )
        {
            bool bHasColor = vertexBufferDeclaration.HasColor;
            bool bHasNormal = false;
			bool bHasTexcoord = vertexBufferDeclaration.HasTexCoord;
            uint texcoordCount = vertexBufferDeclaration.TexCoordCount;
            
            
			String shaderSource = "";

			shaderSource += "struct VS_INPUT\n{\n";
            
			ushort[] semanticCount = new ushort[ 100 ];

			IEnumerable<VertexBufferElement> vertexBufferElements = vertexBufferDeclaration.VertexBufferElements;
			foreach ( VertexBufferElement element in vertexBufferElements )
			{
				VertexElementSemantic semantic = element.VertexElementSemantic;
				VertexElementType type = element.VertexElementType;

				String thisElementSemanticCount = semanticCount[ (int)semantic ].ToString();
                semanticCount[(int)semantic]++;
				String parameterType = "";
				String parameterName = "";
				String parameterShaderTypeName = "";

				switch ( type )
				{
					case VertexElementType.Float1:
						parameterType = "float";
						break;
					case VertexElementType.Float2:
						parameterType = "float2";
						break;
					case VertexElementType.Float3:
						parameterType = "float3";
						break;
					case VertexElementType.Float4:
						parameterType = "float4";
						break;
					case VertexElementType.Color:
					case VertexElementType.Color_ABGR:
					case VertexElementType.Color_ARGB:
                        parameterType = "float4";
						break;
					case VertexElementType.Short1:
						parameterType = "short";
						break;
					case VertexElementType.Short2:
						parameterType = "short2";
						break;
					case VertexElementType.Short3:
						parameterType = "short3";
						break;
					case VertexElementType.Short4:
						parameterType = "short4";
						break;
					case VertexElementType.UByte4:
                        parameterType = "int";
						break;

				}
				switch ( semantic )
				{
					case VertexElementSemantic.Position:
						parameterName = "Position";
						parameterShaderTypeName = "POSITION";
						//parameterType = "float4"; // position must be float4 (and not float3 like in the buffer)
						break;
					case VertexElementSemantic.BlendWeights:
						parameterName = "BlendWeight";
						parameterShaderTypeName = "BLENDWEIGHT";
						break;
					case VertexElementSemantic.BlendIndices:
						parameterName = "BlendIndices";
						parameterShaderTypeName = "BLENDINDICES";
						break;
					case VertexElementSemantic.Normal:
						parameterName = "Normal";
						parameterShaderTypeName = "NORMAL";
                        bHasNormal = true;
						break;
					case VertexElementSemantic.Diffuse:
						parameterName = "DiffuseColor";
						parameterShaderTypeName = "COLOR";
						break;
					case VertexElementSemantic.Specular:
						parameterName = "SpecularColor";
						parameterShaderTypeName = "COLOR";
						thisElementSemanticCount = semanticCount[ (int)VertexElementSemantic.Diffuse ].ToString(); // Diffuse is the "COLOR" count...
						semanticCount[ (int)VertexElementSemantic.Diffuse ]++;
						break;
					case VertexElementSemantic.TexCoords:
						parameterName = "Texcoord";
                        texCordVecType[(int)semanticCount[(int)semantic]-1] = type;
						parameterShaderTypeName = "TEXCOORD";
						break;
					case VertexElementSemantic.Binormal:
						parameterName = "Binormal";
						parameterShaderTypeName = "BINORMAL";
						break;
					case VertexElementSemantic.Tangent:
						parameterName = "Tangent";
						parameterShaderTypeName = "TANGENT";
						break;
				}

                shaderSource += "\t"+ parameterType + " " + parameterName + thisElementSemanticCount + " : " + parameterShaderTypeName + thisElementSemanticCount + ";\n";
			}

			shaderSource += "};\n\n";

            shaderSource += "float4x4  World;\n";
            shaderSource += "float4x4  View;\n";
            shaderSource += "float4x4  Projection;\n";
            shaderSource += "float4x4  ViewIT;\n";
            shaderSource += "float4x4  WorldViewIT;\n";

            for(uint i = 0 ; i < fixedFunctionState.TextureLayerStates.Count; i++)
    		{
	    		String layerCounter = Convert.ToString(i);
                shaderSource += "float4x4  TextureMatrix" + layerCounter + ";\n";
		    }

			switch ( fixedFunctionState.GeneralFixedFunctionState.FogMode )
			{
				case FogMode.None:
					break;
				case FogMode.Exp:
				case FogMode.Exp2:
                    shaderSource += "float4 FogDensity;\n";
                    shaderSource += "float4  FogColor;\n";
					break;
				case FogMode.Linear:
                    shaderSource += "float4 FogStart;\n";
                    shaderSource += "float4 FogEnd;\n";
                    shaderSource += "float4  FogColor;\n";
					break;
			}

			if ( fixedFunctionState.GeneralFixedFunctionState.EnableLighting )
			{
                shaderSource += "float4 BaseLightAmbient;\n";

				for ( int i = 0; i < fixedFunctionState.Lights.Count; i++ )
				{
					String prefix = "Light" + i.ToString() + "_";
                    
                    
					switch ( fixedFunctionState.Lights[ i ] )
					{
                        case LightType.Point:
                            shaderSource += "float4 " + prefix + "Ambient;\n";
                            shaderSource += "float4 " + prefix + "Diffuse;\n";
                            shaderSource += "float4 " + prefix + "Specular;\n";

                            shaderSource += "float3 " + prefix + "Position;\n";
                            shaderSource += "float3 " + prefix + "Range;\n";
                            shaderSource += "float3 " + prefix + "Attenuation;\n";
                            break;
                        case LightType.Directional:
                            shaderSource += "float4 " + prefix + "Ambient;\n";
                            shaderSource += "float4 " + prefix + "Diffuse;\n";
                            shaderSource += "float4 " + prefix + "Specular;\n";

                            shaderSource += "float3 " + prefix + "Direction;\n";
                            break;
                        case LightType.Spotlight:
                            shaderSource += "float3 " + prefix + "Position;\n";
                            shaderSource += "float3 " + prefix + "Direction;\n";

                            shaderSource += "float4 " + prefix + "Ambient;\n";
                            shaderSource += "float4 " + prefix + "Diffuse;\n";
                            shaderSource += "float4 " + prefix + "Specular;\n";

                            shaderSource += "float3 " + prefix + "Attenuation;\n";
                            shaderSource += "float3 " + prefix + "Spot;\n";
                            break;                 
					}
				}
			}

			shaderSource += "\nstruct VS_OUTPUT\n";
			shaderSource += "{\n";
            shaderSource += "\tfloat4 Pos : POSITION;\n";
           
            for (int i = 0; i < fixedFunctionState.TextureLayerStates.Count; i++)
		    {
			    String layerCounter = Convert.ToString(i);

                if (texCordVecType.ContainsKey(i))
                    switch (texCordVecType[i])
                    {
                        case VertexElementType.Float1:
                            shaderSource += "\tfloat1 Texcoord" + layerCounter + " : TEXCOORD" + layerCounter + ";\n";
                            break;
                        case VertexElementType.Float2:
                            shaderSource += "\tfloat2 Texcoord" + layerCounter + " : TEXCOORD" + layerCounter + ";\n";
                            break;
                        case VertexElementType.Float3:
                            shaderSource += "\tfloat3 Texcoord" + layerCounter + " : TEXCOORD" + layerCounter + ";\n";
                            break;
                    }
                else
                {
                    //fix sometimes there are more texture layer states than textures ?? (water demo)
                    shaderSource += "\tfloat2 Texcoord" + layerCounter + " : TEXCOORD" + layerCounter + ";\n";
                }

		    }

            shaderSource += "\tfloat4 Color : COLOR0;\n";
            shaderSource += "\tfloat4 ColorSpec : COLOR1;\n";

			if ( fixedFunctionState.GeneralFixedFunctionState.FogMode != FogMode.None )
			{
                shaderSource += "\tfloat fogDist :COLOR2;\n"; //"float fogDist : FOGDISTANCE;\n";
			}

			shaderSource += "};\n";
			shaderSource += "\nVS_OUTPUT " + vertexProgramName + "( VS_INPUT input )\n";
			shaderSource += "{\n";
            shaderSource += "\tVS_OUTPUT output = (VS_OUTPUT)0;\n";
            shaderSource += "\tfloat4 worldPos = mul( World, float4( input.Position0 , 1 ));\n";
            shaderSource += "\tfloat4 cameraPos = mul( View, worldPos );\n";
            shaderSource += "\toutput.Pos = mul( Projection, cameraPos );\n";

            if (bHasNormal)
            {
                shaderSource += "\tfloat3 Normal = input.Normal0;\n";
            }
            else
            {
                shaderSource += "\tfloat3 Normal = float3(0.0, 0.0, 0.0);\n";
            }
		
           for(int i = 0 ; i < fixedFunctionState.TextureLayerStates.Count; i++)
           {
                TextureLayerState curTextureLayerState = fixedFunctionState.TextureLayerStates[i];
			    String layerCounter = Convert.ToString(i);
			    String coordIdx = Convert.ToString(curTextureLayerState.CoordIndex);

			    shaderSource += "\t{\n";

                switch (curTextureLayerState.TexCoordCalcMethod)
                {
                    case TexCoordCalcMethod.None:
                        if (curTextureLayerState.CoordIndex < texcoordCount)
                        {
                            //shaderSource += "TextureMatrix" + layerCounter + "=float4x4(1.0,0.0,0.0,0.0, 0.0,1.0,0.0,0.0, 0.0,0.0,1.0,0.0, 0.0,0.0,0.0,1.0);\n";
                            if (texCordVecType.ContainsKey(i))
                                switch (texCordVecType[i])
                                {
                                    case VertexElementType.Float1:
                                        shaderSource += "\t\toutput.Texcoord" + layerCounter + " = input.Texcoord" + coordIdx + ";\n";
                                        break;
                                    case VertexElementType.Float2:
                                        shaderSource += "\t\tfloat4 texCordWithMatrix = float4(input.Texcoord" + coordIdx + ".x, input.Texcoord" + coordIdx + ".y, 0, 1);\n";
                                        shaderSource += "\t\ttexCordWithMatrix = mul(texCordWithMatrix, TextureMatrix" + layerCounter + " );\n";
                                        shaderSource += "\t\toutput.Texcoord" + layerCounter + " = texCordWithMatrix.xy;\n";
                                        break;
                                    case VertexElementType.Float3:
                                        shaderSource += "\t\toutput.Texcoord" + layerCounter + " = input.Texcoord" + coordIdx + ";\n";
                                        break;
                                }
                            else
                                shaderSource += "\t\toutput.Texcoord" + layerCounter + " = input.Texcoord" + coordIdx + ";\n";
                        }
                        else
                        {
                            if (texCordVecType.ContainsKey(i))
                                switch (texCordVecType[i])
                                {
                                    case VertexElementType.Float1:
                                        shaderSource += "\t\toutput.Texcoord" + layerCounter + " = 0.0;\n"; // so no error
                                        break;
                                    case VertexElementType.Float2:
                                        shaderSource += "\t\toutput.Texcoord" + layerCounter + " = float2(0.0, 0.0);\n"; // so no error
                                        break;
                                    case VertexElementType.Float3:
                                        shaderSource += "\t\toutput.Texcoord" + layerCounter + " = float3(0.0, 0.0, 0.0);\n"; // so no error
                                        break;
                                }
                            else
                                shaderSource += "\t\toutput.Texcoord" + layerCounter + " = float3(0.0, 0.0, 0.0);\n"; // so no error
                        }
                        break;

                    case TexCoordCalcMethod.EnvironmentMap:
                        //shaderSource += "float3 ecPosition3 = cameraPos.xyz/cameraPos.w;\n";
                        shaderSource += "\t\tfloat3 u = normalize(cameraPos.xyz);\n";
                        shaderSource += "\t\tfloat3 r = reflect(u, Normal);\n";
                        shaderSource += "\t\tfloat  m = 2.0 * sqrt(r.x * r.x + r.y * r.y + (r.z + 1.0) * (r.z + 1.0));\n";
                        shaderSource += "\t\toutput.Texcoord" + layerCounter + " = float2 (r.x / m + 0.5, r.y / m + 0.5);\n";
                        break;
                    case TexCoordCalcMethod.EnvironmentMapPlanar:
                        break;
                    case TexCoordCalcMethod.EnvironmentMapReflection:
                        //assert(curTextureLayerState.getTextureType() == TEX_TYPE_CUBE_MAP);
                        shaderSource += "\t{\n";
                        shaderSource += "\t\tfloat4 worldNorm = mul(float4(Normal, 0), World);\n";
                        shaderSource += "\t\tfloat4 viewNorm = mul(worldNorm, View);\n";
                        shaderSource += "\t\tviewNorm = normalize(viewNorm);\n";
                        shaderSource += "\t\toutput.Texcoord" + layerCounter + " = reflect(viewNorm.xyz, float3(0.0,0.0,-1.0));\n";
                        shaderSource += "\t}\n";
                        break;
                    case TexCoordCalcMethod.EnvironmentMapNormal:
                        break;
                    case TexCoordCalcMethod.ProjectiveTexture:
                        if (texCordVecType.ContainsKey(i))
                            switch (texCordVecType[i])
                            {
                                case VertexElementType.Float1:
                                    shaderSource += "\t{\n";
                                    shaderSource += "\t\tfloat4 cameraPosNorm = normalize(cameraPos);\n";
                                    shaderSource += "\t\toutput.Texcoord" + layerCounter + ".x = 0.5 + cameraPosNorm.x;\n";
                                    shaderSource += "\t}\n";
                                    break;
                                case VertexElementType.Float2:
                                case VertexElementType.Float3:
                                    shaderSource += "\t{\n";
                                    shaderSource += "\t\tfloat4 cameraPosNorm = normalize(cameraPos);\n";
                                    shaderSource += "\t\toutput.Texcoord" + layerCounter + ".x = 0.5 + cameraPosNorm.x;\n";
                                    shaderSource += "\t\toutput.Texcoord" + layerCounter + ".y = 0.5 - cameraPosNorm.y;\n";
                                    shaderSource += "\t}\n";
                                    break;
                            }
                        else
                        {
                            shaderSource += "\t{\n";
                            shaderSource += "\t\tfloat4 cameraPosNorm = normalize(cameraPos);\n";
                            shaderSource += "\t\toutput.Texcoord" + layerCounter + ".x = 0.5 + cameraPosNorm.x;\n";
                            shaderSource += "\t\toutput.Texcoord" + layerCounter + ".y = 0.5 - cameraPosNorm.y;\n";
                            shaderSource += "\t}\n";
                        }
                        break;
                }
                shaderSource += "\t}\n";
            }


			
            shaderSource += "\toutput.ColorSpec = float4(0.0, 0.0, 0.0, 0.0);\n";
           

			if ( fixedFunctionState.GeneralFixedFunctionState.EnableLighting && fixedFunctionState.Lights.Count > 0 )
			{
                shaderSource += "\toutput.Color =BaseLightAmbient;\n";
				if ( bHasColor )
				{
                    shaderSource += "\toutput.Color += input.DiffuseColor0;\n";
					
				}
                shaderSource += "\tfloat3 N = mul((float3x3)WorldViewIT, Normal);\n";
                shaderSource += "\tfloat3 V = -normalize( cameraPos);\n";
               
                shaderSource += "\t#define fMaterialPower 16.f\n";

                for (int i = 0; i <fixedFunctionState.Lights.Count; i++)
                {
                    String prefix = "Light" + i.ToString() + "_";
                    switch (fixedFunctionState.Lights[i])
                    {
                        case LightType.Point:
                            shaderSource += "{\n";
                            shaderSource += "  float3 PosDiff = " + prefix + "Position-(float3)mul(World,input.Position0);\n";
                            shaderSource += "  float3 L = mul((float3x3)ViewIT, normalize((PosDiff)));\n";
                            shaderSource += "  float NdotL = dot(N, L);\n";
                            shaderSource += "  float4 Color = " + prefix + "Ambient;\n";
                            shaderSource += "  float4 ColorSpec = 0;\n";
                            shaderSource += "  float fAtten = 1.f;\n";
                            shaderSource += "  if(NdotL >= 0.f)\n";
                            shaderSource += "  {\n";
                            shaderSource += "    //compute diffuse color\n";
                            shaderSource += "    Color += NdotL * " + prefix + "Diffuse;\n";
                            shaderSource += "    //add specular component\n";
                            shaderSource += "    float3 H = normalize(L + V);   //half vector\n";
                            shaderSource += "    ColorSpec = pow(max(0, dot(H, N)), fMaterialPower) * " + prefix + "Specular;\n";
                            shaderSource += "    float LD = length(PosDiff);\n";
                            shaderSource += "    if(LD > " + prefix + "Range.x)\n";
                            shaderSource += "    {\n";
                            shaderSource += "      fAtten = 0.f;\n";
                            shaderSource += "    }\n";
                            shaderSource += "    else\n";
                            shaderSource += "    {\n";
                            shaderSource += "      fAtten *= 1.f/(" + prefix + "Attenuation.x + " + prefix + "Attenuation.y*LD + " + prefix + "Attenuation.z*LD*LD);\n";
                            shaderSource += "    }\n";
                            shaderSource += "    Color *= fAtten;\n";
                            shaderSource += "    ColorSpec *= fAtten;\n";
                            shaderSource += "    output.Color += Color;\n";
                            shaderSource += "    output.ColorSpec += ColorSpec;\n";
                            shaderSource += "  }\n";
                            shaderSource += "}\n";
                            break;
                        case LightType.Directional:
                            shaderSource += "{\n";
                            shaderSource += "  float3 L = mul((float3x3)ViewIT, -normalize(" + prefix + "Direction));\n";
                            shaderSource += "  float NdotL = dot(N, L);\n";
                            shaderSource += "  float4 Color = " + prefix + "Ambient;\n";
                            shaderSource += "  float4 ColorSpec = 0;\n";
                            shaderSource += "  if(NdotL > 0.f)\n";
                            shaderSource += "  {\n";
                            shaderSource += "    //compute diffuse color\n";
                            shaderSource += "    Color += NdotL * " + prefix + "Diffuse;\n";
                            shaderSource += "    //add specular component\n";
                            shaderSource += "    float3 H = normalize(L + V);   //half vector\n";
                            shaderSource += "    ColorSpec = pow(max(0, dot(H, N)), fMaterialPower) * " + prefix + "Specular;\n";
                            shaderSource += "    output.Color += Color;\n";
                            shaderSource += "    output.ColorSpec += ColorSpec;\n";
                            shaderSource += "  }\n";
                            shaderSource += "}\n";
                            break;
                        case LightType.Spotlight:
                            shaderSource += "{\n";
                            shaderSource += "  float3 PosDiff = " + prefix + "Position-(float3)mul(World,input.Position0);\n";
                            shaderSource += "   float3 L = mul((float3x3)ViewIT, normalize((PosDiff)));\n";
                            shaderSource += "   float NdotL = dot(N, L);\n";
                            shaderSource += "   output.Color = " + prefix + "Ambient;\n";
                            shaderSource += "   output.ColorSpec = 0;\n";
                            shaderSource += "   float fAttenSpot = 1.f;\n";
                            shaderSource += "   if(NdotL >= 0.f)\n";
                            shaderSource += "   {\n";
                            shaderSource += "      //compute diffuse color\n";
                            shaderSource += "      output.Color += NdotL * " + prefix + "Diffuse;\n";
                            shaderSource += "      //add specular component\n";
                            shaderSource += "       float3 H = normalize(L + V);   //half vector\n";
                            shaderSource += "       output.ColorSpec = pow(max(0, dot(H, N)), fMaterialPower) * " + prefix + "Specular;\n";
                            shaderSource += "      float LD = length(PosDiff);\n";

                            /*shaderSource += "      if(LD >" + prefix + "Attenuation.x)\n";
                            shaderSource += "      {\n";
                            shaderSource += "         fAttenSpot = 0.f;\n";
                            shaderSource += "      }\n";
                            shaderSource += "      else\n";
                            shaderSource += "      {\n";
                            shaderSource += "         fAttenSpot *= 1.f/(" + prefix + "Attenuation.x + " + prefix + "Attenuation.y*LD + " + prefix + "Attenuation.z*LD*LD);\n";
                            shaderSource += "      }\n";*/

                            shaderSource += "      //spot cone computation\n";
                            shaderSource += "      float3 L2 = mul((float3x3)ViewIT, -normalize(" + prefix + "Direction));\n";
                            shaderSource += "      float rho = dot(L, L2);\n";
                            shaderSource += "      fAttenSpot *= pow(saturate((rho - " + prefix + "Spot.y)/(" + prefix + "Spot.x - " + prefix + "Spot.y)), " + prefix + "Spot.z);\n";
                            shaderSource += "		output.Color *= fAttenSpot;\n";
                            shaderSource += "		output.ColorSpec *= fAttenSpot;\n";
                            shaderSource += "    output.Color += output.Color;\n";
                            shaderSource += "    output.ColorSpec += output.ColorSpec;\n";
                            shaderSource += "   }\n";
                            shaderSource += "}\n";
                            break;
                    }
                }
			}
			else
			{
				if ( bHasColor )
				{
                    shaderSource += "\toutput.Color = input.DiffuseColor0;\n";
				}
				else
				{
					shaderSource += "\toutput.Color = float4(1.0, 1.0, 1.0, 1.0);\n";
				}
			}

            switch (fixedFunctionState.GeneralFixedFunctionState.FogMode)
            {
                case FogMode.None:
                    break;
                case FogMode.Exp:
                    shaderSource += "\t#define E 2.71828\n";
                    shaderSource += "\toutput.fogDist = 1.0 / pow( E, output.fogDist*FogDensity );\n";
                    shaderSource += "\toutput.fogDist = clamp( output.fogDist, 0, 1 );\n";
                    break;
                case FogMode.Exp2:
                    shaderSource += "\t#define E 2.71828\n";
                    shaderSource += "\toutput.fogDist = 1.0 / pow( E, output.fogDist*output.fogDist*FogDensity*FogDensity );\n";
                    shaderSource += "\toutput.fogDist = clamp( output.fogDist, 0, 1 );\n";
                    break;
                case FogMode.Linear:
                    shaderSource += "\toutput.fogDist = (FogEnd - output.fogDist)/(FogEnd - FogStart);\n";
                    shaderSource += "\toutput.fogDist = clamp( output.fogDist, 0, 1 );\n";
                    break;
            }
			shaderSource += "\treturn output;\n}\n\n";






            /////////////////////////////////////
			// here starts the fragment shader //
            /////////////////////////////////////
            for(int i = 0 ; i < fixedFunctionState.TextureLayerStates.Count; i++)
		    {
                String layerCounter = Convert.ToString(i);
			    shaderSource += "sampler Texture" + layerCounter + " : register(s" + layerCounter + ");\n";
		    }
            //shaderSource += "float4  FogColor;\n";

            shaderSource += "\nfloat4 " + fragmentProgramName + "( VS_OUTPUT input ) : COLOR\n";
		    shaderSource += "{\n";

		    shaderSource += "\tfloat4 finalColor= input.Color + input.ColorSpec;\n";

            for (int i = 0; i < fixedFunctionState.TextureLayerStates.Count; i++)
            {
                shaderSource += "\t{\n\tfloat4 texColor=float4(1.0,1.0,1.0,1.0);\n";
                TextureLayerState curTextureLayerState = fixedFunctionState.TextureLayerStates[i];
                String layerCounter = Convert.ToString(i);
               
                switch (curTextureLayerState.TextureType)
                {
                    case TextureType.OneD:
                        {
                            if (texCordVecType.ContainsKey(i))
                                switch (texCordVecType[i])
                                {
                                    case VertexElementType.Float1:
                                        shaderSource += "\t\texColor = tex1D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                                        break;
                                    case VertexElementType.Float2:
                                        shaderSource += "\t\ttexColor = tex1D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ".x);\n";
                                        break;
                                }
                            else shaderSource += "\t\ttexColor = tex1D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                        }
                        break;
                    case TextureType.TwoD:
                        {
                            if (texCordVecType.ContainsKey(i))
                                switch (texCordVecType[i])
                                {
                                    case VertexElementType.Float1:
                                        shaderSource += "\t\ttexColor  = tex2D(Texture" + layerCounter + ", float2(input.Texcoord" + layerCounter + ", 0.0));\n";
                                        break;
                                    case VertexElementType.Float2:
                                        shaderSource += "\t\ttexColor  = tex2D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                                        break;
                                }
                            else//envmap works now and correct the color of the overlay with the demo cellshading (was completly white before)
                                shaderSource += "\t\ttexColor  = tex2D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                        }

                        break;
                    case TextureType.CubeMap:
                        shaderSource += "\t\ttexColor  = texCUBE(Texture" + layerCounter + ", float3(input.Texcoord" + layerCounter + ".x, input.Texcoord" + layerCounter + ".y, 0.0));\n";
                        break;
                    case TextureType.ThreeD:
                        if (texCordVecType.ContainsKey(i))
                            switch (texCordVecType[i])
                            {
                                case VertexElementType.Float1:
                                    shaderSource += "\t\ttexColor  = tex3D(Texture" + layerCounter + ", float3(input.Texcoord" + layerCounter + ", 0.0, 0.0));\n";
                                    break;
                                case VertexElementType.Float2:
                                    shaderSource += "\t\ttexColor  = tex3D(Texture" + layerCounter + ", float3(input.Texcoord" + layerCounter + ".x, input.Texcoord" + layerCounter + ".y, 0.0));\n";
                                    break;
                                case VertexElementType.Float3:
                                    shaderSource += "\t\ttexColor  = tex3D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                                    break;
                            }
                        else
                            shaderSource += "\t\t texColor  = tex3D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                        break;
                }


               
                LayerBlendModeEx blend = curTextureLayerState.LayerBlendModeEx;
                switch (blend.source1)
                {
                    case LayerBlendSource.Current:
                        shaderSource += "\t\tfloat4 source1 = finalColor;\n";
                        break;
                    case LayerBlendSource.Texture:
                        shaderSource += "\t\tfloat4 source1 = texColor;\n";
                        break;
                    case LayerBlendSource.Diffuse:
                        shaderSource += "\t\tfloat4 source1 = input.Color;\n";
                        break;
                    case LayerBlendSource.Specular:
                        shaderSource += "\t\tfloat4 source1 = input.ColorSpec;\n";
                        break;
                    case LayerBlendSource.Manual:
                        shaderSource += "\t\tfloat4 source1 = Texture" + layerCounter + "_colourArg1;\n";
                        break;
                }
                switch (blend.source2)
                {
                    case LayerBlendSource.Current:
                        shaderSource += "\t\tfloat4 source2 = finalColor;\n";
                        break;
                    case LayerBlendSource.Texture:
                        shaderSource += "\t\tfloat4 source2 = texColor;\n";
                        break;
                    case LayerBlendSource.Diffuse:
                        shaderSource += "\t\tfloat4 source2 = input.Color;\n";
                        break;
                    case LayerBlendSource.Specular:
                        shaderSource += "\t\tfloat4 source2 = input.ColorSpec;\n";
                        break;
                    case LayerBlendSource.Manual:
                        shaderSource += "\t\tfloat4 source2 = Texture" + layerCounter + "_colourArg2;\n";
                        break;
                }

                switch (blend.operation)
                {
                    case LayerBlendOperationEx.Source1:
                        shaderSource += "\t\tfinalColor = source1;\n";
                        break;
                    case LayerBlendOperationEx.Source2:
                        shaderSource += "\t\tfinalColor = source2;\n";
                        break;
                    case LayerBlendOperationEx.Modulate:
                        shaderSource += "\t\tfinalColor = source1 * source2;\n";
                        break;
                    case LayerBlendOperationEx.ModulateX2:
                        shaderSource += "\t\tfinalColor = source1 * source2 * 2.0;\n";
                        break;
                    case LayerBlendOperationEx.ModulateX4:
                        shaderSource += "\t\tfinalColor = source1 * source2 * 4.0;\n";
                        break;
                    case LayerBlendOperationEx.Add:
                        shaderSource += "\t\tfinalColor = source1 + source2;\n";
                        break;
                    case LayerBlendOperationEx.AddSigned:
                        shaderSource += "\t\tfinalColor = source1 + source2 - 0.5;\n";
                        break;
                    case LayerBlendOperationEx.AddSmooth:
                        shaderSource += "\t\tfinalColor = source1 + source2 - (source1 * source2);\n";
                        break;
                    case LayerBlendOperationEx.Subtract:
                        shaderSource += "\t\tfinalColor = source1 - source2;\n";
                        break;
                    case LayerBlendOperationEx.BlendDiffuseAlpha:
                        shaderSource += "\t\tfinalColor = source1 * input.Color.w + source2 * (1.0 - input.Color.w);\n";
                        break;
                    case LayerBlendOperationEx.BlendTextureAlpha:
                        shaderSource += "\t\tfinalColor = source1 * texColor.w + source2 * (1.0 - texColor.w);\n";
                        break;
                    case LayerBlendOperationEx.BlendCurrentAlpha:
                        shaderSource += "\t\tfinalColor = source1 * finalColor.w + source2 * (1.0 - finalColor.w);\n";
                        break;
                    case LayerBlendOperationEx.BlendManual:
                        shaderSource += "\t\tfinalColor = source1 * " + Convert.ToString(blend.blendFactor) +
                                                        " + source2 * (1.0 - " + Convert.ToString(blend.blendFactor) + ");\n";
                        break;
                    case LayerBlendOperationEx.DotProduct:
                        shaderSource += "\t\tfinalColor = product(source1,source2);\n";
                        break;
                }
                shaderSource += "\t}\n";
            }
       
            if ( fixedFunctionState.GeneralFixedFunctionState.FogMode != FogMode.None )
			{
                //just to test for now...
                //shaderSource += "\tinput.fogDist=0.5;\n";
                //shaderSource += "\tFogColor=float4(1.0,1.0,1.0,1.0);\n";
                
                shaderSource += "\tfinalColor = input.fogDist * finalColor + (1.0 - input.fogDist)*FogColor;\n";
			}

			shaderSource += "\treturn finalColor;\n}\n";

            
            
            
            return shaderSource;
		}

        public override FixedFunctionPrograms CreateFixedFunctionPrograms()
        {
            return new HLSLFixedFunctionProgram();
        }

		#endregion ShaderGenerator Implementation
    }
}