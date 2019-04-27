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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
    /// This enum defines the integer ids for keywords this compiler handles
    public enum Keywords : uint
    {
        ID_MATERIAL = 3,
        ID_VERTEX_PROGRAM,
        ID_GEOMETRY_PROGRAM,
        ID_FRAGMENT_PROGRAM,
        ID_TECHNIQUE,
        ID_PASS,
        ID_TEXTURE_UNIT,
        ID_VERTEX_PROGRAM_REF,
        ID_GEOMETRY_PROGRAM_REF,
        ID_FRAGMENT_PROGRAM_REF,
        ID_SHADOW_CASTER_VERTEX_PROGRAM_REF,
        ID_SHADOW_RECEIVER_VERTEX_PROGRAM_REF,
        ID_SHADOW_RECEIVER_FRAGMENT_PROGRAM_REF,
        ID_SHADOW_CASTER_MATERIAL,
        ID_SHADOW_RECEIVER_MATERIAL,

        ID_LOD_VALUES,
        ID_LOD_STRATEGY,
        ID_LOD_DISTANCES,
        ID_RECEIVE_SHADOWS,
        ID_TRANSPARENCY_CASTS_SHADOWS,
        ID_SET_TEXTURE_ALIAS,

        ID_SOURCE,
        ID_SYNTAX,
        ID_DEFAULT_PARAMS,
        ID_PARAM_INDEXED,
        ID_PARAM_NAMED,
        ID_PARAM_INDEXED_AUTO,
        ID_PARAM_NAMED_AUTO,

        ID_SCHEME,
        ID_LOD_INDEX,
        ID_GPU_VENDOR_RULE,
        ID_GPU_DEVICE_RULE,
        ID_INCLUDE,
        ID_EXCLUDE,

        ID_AMBIENT,
        ID_DIFFUSE,
        ID_SPECULAR,
        ID_EMISSIVE,
        ID_VERTEX_COLOUR,
        ID_SCENE_BLEND,
        ID_COLOUR_BLEND,
        ID_ONE,
        ID_ZERO,
        ID_DEST_COLOUR,
        ID_SRC_COLOUR,
        ID_ONE_MINUS_DEST_COLOUR,
        ID_ONE_MINUS_SRC_COLOUR,
        ID_DEST_ALPHA,
        ID_SRC_ALPHA,
        ID_ONE_MINUS_DEST_ALPHA,
        ID_ONE_MINUS_SRC_ALPHA,
        ID_SEPARATE_SCENE_BLEND,
        ID_SCENE_BLEND_OP,
        ID_REVERSE_SUBTRACT,
        ID_MIN,
        ID_MAX,
        ID_SEPARATE_SCENE_BLEND_OP,
        ID_DEPTH_CHECK,
        ID_DEPTH_WRITE,
        ID_DEPTH_FUNC,
        ID_DEPTH_BIAS,
        ID_ITERATION_DEPTH_BIAS,
        ID_ALWAYS_FAIL,
        ID_ALWAYS_PASS,
        ID_LESS_EQUAL,
        ID_LESS,
        ID_EQUAL,
        ID_NOT_EQUAL,
        ID_GREATER_EQUAL,
        ID_GREATER,
        ID_ALPHA_REJECTION,
        ID_ALPHA_TO_COVERAGE,
        ID_LIGHT_SCISSOR,
        ID_LIGHT_CLIP_PLANES,
        ID_TRANSPARENT_SORTING,
        ID_ILLUMINATION_STAGE,
        ID_DECAL,
        ID_CULL_HARDWARE,
        ID_CLOCKWISE,
        ID_ANTICLOCKWISE,
        ID_CULL_SOFTWARE,
        ID_BACK,
        ID_FRONT,
        ID_NORMALISE_NORMALS,
        ID_LIGHTING,
        ID_SHADING,
        ID_FLAT,
        ID_GOURAUD,
        ID_PHONG,
        ID_POLYGON_MODE,
        ID_SOLID,
        ID_WIREFRAME,
        ID_POINTS,
        ID_POLYGON_MODE_OVERRIDEABLE,
        ID_FOG_OVERRIDE,
        ID_NONE,
        ID_LINEAR,
        ID_EXP,
        ID_EXP2,
        ID_COLOUR_WRITE,
        ID_MAX_LIGHTS,
        ID_START_LIGHT,
        ID_ITERATION,
        ID_ONCE,
        ID_ONCE_PER_LIGHT,
        ID_PER_LIGHT,
        ID_PER_N_LIGHTS,
        ID_POINT,
        ID_SPOT,
        ID_DIRECTIONAL,
        ID_LIGHT_MASK,
        ID_POINT_SIZE,
        ID_POINT_SPRITES,
        ID_POINT_SIZE_ATTENUATION,
        ID_POINT_SIZE_MIN,
        ID_POINT_SIZE_MAX,

        ID_TEXTURE_ALIAS,
        ID_TEXTURE,
        ID_1D,
        ID_2D,
        ID_3D,
        ID_CUBIC,
        ID_UNLIMITED,
        ID_ALPHA,
        ID_GAMMA,
        ID_ANIM_TEXTURE,
        ID_CUBIC_TEXTURE,
        ID_SEPARATE_UV,
        ID_COMBINED_UVW,
        ID_TEX_COORD_SET,
        ID_TEX_ADDRESS_MODE,
        ID_WRAP,
        ID_CLAMP,
        ID_BORDER,
        ID_MIRROR,
        ID_TEX_BORDER_COLOUR,
        ID_FILTERING,
        ID_BILINEAR,
        ID_TRILINEAR,
        ID_ANISOTROPIC,
        ID_MAX_ANISOTROPY,
        ID_MIPMAP_BIAS,
        ID_COLOR_OP,
        ID_REPLACE,
        ID_ADD,
        ID_MODULATE,
        ID_ALPHA_BLEND,
        ID_COLOR_OP_EX,
        ID_SOURCE1,
        ID_SOURCE2,
        ID_MODULATE_X2,
        ID_MODULATE_X4,
        ID_ADD_SIGNED,
        ID_ADD_SMOOTH,
        ID_SUBTRACT,
        ID_BLEND_DIFFUSE_COLOUR,
        ID_BLEND_DIFFUSE_ALPHA,
        ID_BLEND_TEXTURE_ALPHA,
        ID_BLEND_CURRENT_ALPHA,
        ID_BLEND_MANUAL,
        ID_DOT_PRODUCT,
        ID_SRC_CURRENT,
        ID_SRC_TEXTURE,
        ID_SRC_DIFFUSE,
        ID_SRC_SPECULAR,
        ID_SRC_MANUAL,
        ID_COLOR_OP_MULTIPASS_FALLBACK,
        ID_ALPHA_OP_EX,
        ID_ENV_MAP,
        ID_SPHERICAL,
        ID_PLANAR,
        ID_CUBIC_REFLECTION,
        ID_CUBIC_NORMAL,
        ID_SCROLL,
        ID_SCROLL_ANIM,
        ID_ROTATE,
        ID_ROTATE_ANIM,
        ID_SCALE,
        ID_WAVE_XFORM,
        ID_SCROLL_X,
        ID_SCROLL_Y,
        ID_SCALE_X,
        ID_SCALE_Y,
        ID_SINE,
        ID_TRIANGLE,
        ID_SQUARE,
        ID_SAWTOOTH,
        ID_INVERSE_SAWTOOTH,
        ID_PULSE_WIDTH_MODULATION,
        ID_TRANSFORM,
        ID_BINDING_TYPE,
        ID_VERTEX,
        ID_FRAGMENT,
        ID_CONTENT_TYPE,
        ID_NAMED,
        ID_SHADOW,
        ID_TEXTURE_SOURCE,
        ID_SHARED_PARAMS,
        ID_SHARED_PARAM_NAMED,
        ID_SHARED_PARAMS_REF,

        ID_PARTICLE_SYSTEM,
        ID_EMITTER,
        ID_AFFECTOR,

        ID_COMPOSITOR,
        ID_TARGET,
        ID_TARGET_OUTPUT,

        ID_INPUT,
        ID_PREVIOUS,
        ID_TARGET_WIDTH,
        ID_TARGET_HEIGHT,
        ID_TARGET_WIDTH_SCALED,
        ID_TARGET_HEIGHT_SCALED,
        ID_COMPOSITOR_LOGIC,
        ID_TEXTURE_REF,
        ID_SCOPE_LOCAL,
        ID_SCOPE_CHAIN,
        ID_SCOPE_GLOBAL,
        ID_POOLED,
        //ID_GAMMA, - already registered for material
        ID_NO_FSAA,
        ID_ONLY_INITIAL,
        ID_VISIBILITY_MASK,
        ID_LOD_BIAS,
        ID_MATERIAL_SCHEME,
        ID_SHADOWS_ENABLED,

        ID_CLEAR,
        ID_STENCIL,
        ID_RENDER_SCENE,
        ID_RENDER_QUAD,
        ID_IDENTIFIER,
        ID_FIRST_RENDER_QUEUE,
        ID_LAST_RENDER_QUEUE,
        ID_QUAD_NORMALS,
        ID_CAMERA_FAR_CORNERS_VIEW_SPACE,
        ID_CAMERA_FAR_CORNERS_WORLD_SPACE,

        ID_BUFFERS,
        ID_COLOUR,
        ID_DEPTH,
        ID_COLOUR_VALUE,
        ID_DEPTH_VALUE,
        ID_STENCIL_VALUE,

        ID_CHECK,
        ID_COMP_FUNC,
        ID_REF_VALUE,
        ID_MASK,
        ID_FAIL_OP,
        ID_KEEP,
        ID_INCREMENT,
        ID_DECREMENT,
        ID_INCREMENT_WRAP,
        ID_DECREMENT_WRAP,
        ID_INVERT,
        ID_DEPTH_FAIL_OP,
        ID_PASS_OP,
        ID_TWO_SIDED,
#if RTSHADER_SYSTEM_BUILD_CORE_SHADERS
		ID_RT_SHADER_SYSTEM,
#endif
        ID_END_BUILTIN_IDS
    }
}