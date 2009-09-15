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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

using Axiom.Animating;
using Axiom.Collections;
using Axiom.Controllers;
using Axiom.FileSystem;
using Axiom.Fonts;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Axiom.Overlays;
using Axiom.ParticleSystems;
using Axiom.Serialization;

#endregion

namespace Axiom.Core
{
    /// <summary>
    ///		The Engine class is the main container of all the subsystems.  This includes the RenderSystem, various ResourceManagers, etc.
    /// </summary>
    public sealed class Root : IDisposable
    {
        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of Root.
        /// </summary>
        private static Root instance;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <remarks>
        ///     This public contructor is intended for the user to decide when the Root object gets instantiated.
        ///     This is a critical step in preparing the engine for use.
        /// </remarks>
        /// <param name="logFileName">Name of the default log file.</param>
        public Root( string logFileName )
        {
            if ( instance == null )
            {
                instance = this;

                this.meterFrameCount = 0;
                this.pendingMeterFrameCount = 0;

                StringBuilder info = new StringBuilder();

                // write the initial info at the top of the log
                info.AppendFormat( "*********Axiom 3D Engine Log *************\n" );
                info.AppendFormat( "Copyright {0}\n", this.Copyright );
                info.AppendFormat( "Version: {0}\n", this.Version );
                info.AppendFormat( "Operating System: {0}\n", Environment.OSVersion.ToString() );
                info.AppendFormat( ".Net Framework: {0}\n", Environment.Version.ToString() );

                // Initializes the Log Manager singleton
                this.logMgr = new LogManager();
				
                //if logFileName is null, then just the Diagnostics (debug) writes will be made
                // create a new default log
                this.logMgr.CreateLog( logFileName, true, true );

                this.logMgr.Write( info.ToString() );
                this.logMgr.Write( "*-*-* Axiom Intializing" );

                ArchiveManager.Instance.Initialize();

                ResourceGroupManager.Instance.Initialize();

                this.sceneManagerEnumerator = SceneManagerEnumerator.Instance;

                MaterialManager mat = MaterialManager.Instance;
                MeshManager mesh = MeshManager.Instance;
                SkeletonManager.Instance.Initialize();
                new ParticleSystemManager();
#if !(XBOX || XBOX360 || SILVERLIGHT)
                new PlatformManager();
#endif

                // create a new timer
                this.timer = new Timer();

                FontManager.Instance.Initialize();

                OverlayManager.Instance.Initialize();
                new OverlayElementManager();

#if !(XBOX || XBOX360 || SILVERLIGHT)
                ArchiveManager.Instance.AddArchiveFactory( new ZipArchiveFactory() );
                ArchiveManager.Instance.AddArchiveFactory( new FileSystemArchiveFactory() );
#endif
                new CodecManager();

                new HighLevelGpuProgramManager();
                CompositorManager.Instance.Initialize();

                LodStrategyManager.Instance.Initialize();

                new PluginManager();
                PluginManager.Instance.LoadAll();

                // instantiate and register base movable factories
                this.entityFactory = new EntityFactory();
                this.AddMovableObjectFactory( this.entityFactory, true );
                this.lightFactory = new LightFactory();
                this.AddMovableObjectFactory( this.lightFactory, true );
                this.billboardSetFactory = new BillboardSetFactory();
                this.AddMovableObjectFactory( this.billboardSetFactory, true );
                this.manualObjectFactory = new ManualObjectFactory();
                this.AddMovableObjectFactory( this.manualObjectFactory, true );
                this.billboardChainFactory = new BillboardChainFactory();
                this.AddMovableObjectFactory( this.billboardChainFactory, true );
                this.ribbonTrailFactory = new RibbonTrailFactory();
                this.AddMovableObjectFactory( this.ribbonTrailFactory, true );
            }
        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        /// <value></value>
        public static Root Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        ///     Current active render system.
        /// </summary>
        private RenderSystem activeRenderSystem;

        /// <summary>
        ///     Auto created window (if one was created).
        /// </summary>
        private RenderWindow autoWindow;

        /// <summary>
        ///     Average frames per second.
        /// </summary>
        private float averageFPS;

        /// <summary>
        ///     Current frames per second.
        /// </summary>
        private float currentFPS;

        /// <summary>
        ///		Global frame count since startup.
        /// </summary>
        private ulong currentFrameCount;

        /// <summary>
        ///    In case multiple render windows are created, only once are the resources loaded.
        /// </summary>
        private bool firstTimePostWindowInit = true;

        /// <summary>
        ///     Frames drawn counter for FPS calculations.
        /// </summary>
        private long frameCount;

        /// <summary>
        ///     Highest recorded frames per second.
        /// </summary>
        private float highestFPS;

        /// <summary>
        ///     The last time we calculated the framerate.
        /// </summary>
        private long lastCalculationTime;

        /// <summary>
        ///     End time of last frame.
        /// </summary>
        private long lastEndTime;

        /// <summary>
        ///     Start time of last frame.
        /// </summary>
        private long lastStartTime;

        /// <summary>
        /// Holds instance of LogManager
        /// </summary>
        private LogManager logMgr;

        /// <summary>
        ///     Lowest recorded frames per second.
        /// </summary>
        private float lowestFPS = 9999;

        /// <summary>
        ///	    These variables control per-frame metering
        /// </summary>
        protected int meterFrameCount;

        protected int pendingMeterFrameCount;

        /// <summary>
        ///		True if a request has been made to shutdown the rendering engine.
        /// </summary>
        private bool queuedEnd;

        /// <summary>
        ///     List of available render systems.
        /// </summary>
        private RenderSystemCollection renderSystemList = new RenderSystemCollection();

        /// <summary>
        ///     Current active scene manager.
        /// </summary>
        private SceneManager sceneManager;

        /// <summary>
        ///     List of available scene managers.
        /// </summary>
        private SceneManagerEnumerator sceneManagerEnumerator;

        /// <summary>
        ///     How often we determine the FPS average, in seconds
        /// </summary>
        private float secondsBetweenFPSAverages = 1f;

        /// <summary>
        ///		True if a request has been made to suspend rendering, typically because the 
        ///	    form has been minimized
        /// </summary>
        private bool suspendRendering = false;

        /// <summary>
        ///     Current active timer.
        /// </summary>
        private ITimer timer;

        #region MovableObjectFactory fields

        public static ulong USER_TYPE_MASK_LIMIT = 0x04000000;

        protected readonly MovableObjectFactoryMap movableObjectFactoryMap = new MovableObjectFactoryMap();

        protected EntityFactory entityFactory;
        protected LightFactory lightFactory;
        protected BillboardSetFactory billboardSetFactory;
        protected BillboardChainFactory billboardChainFactory;
        protected ManualObjectFactory manualObjectFactory;
        protected ulong nextMovableObjectTypeFlag;
        protected RibbonTrailFactory ribbonTrailFactory;

        #endregion MovableObjectFactory fields

        #endregion Fields

        #region Events

        /// <summary>
        ///    The time when the meter manager was started
        /// </summary>
        protected long lastFrameStartTime = 0;

        /// <summary>
        ///    The number of microseconds per frame when we're
        ///    limiting frame rates.  By default, we don't limit frame
        ///    rates, and in that case, the number is 0.
        /// </summary>
        private float microsecondsPerFrame = 0;

        /// <summary>
        ///    The number of microseconds per tick; obviously a fraction
        /// </summary>
        private float microsecondsPerTick;

        /// <summary>
        /// Fired as a frame is about to be rendered.
        /// </summary>
        public event FrameEvent FrameStarted;

        /// <summary>
        /// Fired after a frame has completed rendering.
        /// </summary>
        public event FrameEvent FrameEnded;

        #endregion

        #region Properties

        /// <summary>
        /// Specifies the name of the engine that will be used where needed (i.e. log files, etc).  
        /// </summary>
        public string Copyright
        {
            get
            {
                AssemblyCopyrightAttribute attribute =
                        ( AssemblyCopyrightAttribute )
                        Attribute.GetCustomAttribute( Assembly.GetExecutingAssembly(),
                                                      typeof( AssemblyCopyrightAttribute ),
                                                      false );

                if ( attribute != null )
                {
                    return attribute.Copyright;
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Returns the current version of the Engine assembly.
        /// </summary>
        public string Version
        {
            get
            {
                // returns the file version of this assembly
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        /// <summary>
        ///		Gets the scene manager currently being used to render a frame.
        /// </summary>
        /// <remarks>
        ///		This is only intended for internal use; it is only valid during the
        ///		rendering of a frame.		
        ///</remarks>
        public SceneManager SceneManager
        {
            get
            {
                return this.sceneManager;
            }
            set
            {
                this.sceneManager = value;
            }
        }

        /// <summary>
        ///		Gets a list over all the existing SceneManager instances.
        /// </summary>
        public SceneManagerCollection SceneManagerList
        {
            get
            {
                return this.sceneManagerEnumerator.SceneManagerList;
            }
        }

        /// <summary>
        ///		Gets a list of all types of SceneManager available for construction, 
        ///		providing some information about each one.
        /// </summary>
        public List<SceneManagerMetaData> MetaDataList
        {
            get
            {
                return this.sceneManagerEnumerator.MetaDataList;
            }
        }

        /// <summary>
        /// Gets/Sets the current active RenderSystem that the engine is using.
        /// </summary>
        public RenderSystem RenderSystem
        {
            get
            {
                return this.activeRenderSystem;
            }
            set
            {
                // Sets the active rendering system
                // Can be called direct or will be called by
                // standard config dialog

                // Is there already an active renderer?
                // If so, disable it and initialize the new one
                if ( this.activeRenderSystem != null && this.activeRenderSystem != value )
                {
                    this.activeRenderSystem.Shutdown();
                }

                this.activeRenderSystem = value;

                // Tell scene managers
                SceneManagerEnumerator.Instance.RenderSytem = this.activeRenderSystem;
            }
        }

        /// <summary>
        /// The list of available render systems for the engine to use (made available via plugins).
        /// </summary>
        public RenderSystemCollection RenderSystems
        {
            get
            {
                return this.renderSystemList;
            }
        }

        /// <summary>
        ///    Gets a reference to the timer being used for timing throughout the engine.
        /// </summary>
        public ITimer Timer
        {
            get
            {
                return this.timer;
            }
        }

        /// <summary>
        ///    Gets or sets the maximum frame rate, in frames per second
        /// </summary>
        public int MaxFramesPerSecond
        {
            get
            {
                return
                        ( int )
                        ( ( this.microsecondsPerFrame == 0 )
                                  ? this.microsecondsPerFrame
                                  : ( 1000000.0f / this.microsecondsPerFrame ) );
            }
            set
            {
                if ( value != 0 )
                {
                    this.microsecondsPerTick = 1000000.0f / ( float ) Stopwatch.Frequency;
                    this.microsecondsPerFrame = 1000000.0f / ( float ) value;
                }
                else // Disable MaxFPS
                {
                    this.microsecondsPerFrame = 0;
                }
            }
        }

        /// <summary>
        ///    Access to the float that determines how often we compute the FPS average
        /// </summary>
        public float SecondsBetweenFPSAverages
        {
            get
            {
                return this.secondsBetweenFPSAverages;
            }
            set
            {
                this.secondsBetweenFPSAverages = value;
            }
        }

        #endregion

        private static TimingMeter eventMeter = MeterManager.GetMeter( "Engine OS Events", "Engine" );
        private static TimingMeter frameMeter = MeterManager.GetMeter( "Engine Frame", "Engine" );

        private static TimingMeter oneFrameEndedMeter = MeterManager.GetMeter( "Engine One Frame Ended",
                                                                               "Engine One Frame" );

        private static TimingMeter oneFrameMeter = MeterManager.GetMeter( "Engine One Frame", "Engine One Frame" );

        private static TimingMeter oneFrameStartedMeter = MeterManager.GetMeter( "Engine One Frame Started",
                                                                                 "Engine One Frame" );

        private static TimingMeter renderMeter = MeterManager.GetMeter( "Engine Render", "Engine" );

        private static TimingMeter updateRenderTargetsMeter = MeterManager.GetMeter( "Engine One Frame Update",
                                                                                     "Engine One Frame" );

        /// <summary>
        ///		Gets the number of frames drawn since startup.
        /// </summary>
        public ulong CurrentFrameCount
        {
            get
            {
                return this.currentFrameCount;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public float CurrentFPS
        {
            get
            {
                return this.currentFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public float BestFPS
        {
            get
            {
                return this.highestFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public float WorstFPS
        {
            get
            {
                return this.lowestFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public float AverageFPS
        {
            get
            {
                return this.averageFPS;
            }
        }

        /// <summary>
        ///	    Exposes the mechanism to suspend rendering
        /// </summary>
        public bool SuspendRendering
        {
            get
            {
                return this.suspendRendering;
            }
            set
            {
                this.suspendRendering = value;
            }
        }

        /// <summary>
        ///		Registers a new SceneManagerFactory, a factory object for creating instances
        ///		of specific SceneManagers. 
        /// </summary>
        /// <remarks>
        ///		Plugins should call this to register as new SceneManager providers.
        /// </remarks>
        /// <param name="factory"></param>
        public void AddSceneManagerFactory( SceneManagerFactory factory )
        {
            this.sceneManagerEnumerator.AddFactory( factory );
        }

        /// <summary>
        ///		Unregisters a SceneManagerFactory.
        /// </summary>
        /// <param name="factory"></param>
        public void RemoveSceneManagerFactory( SceneManagerFactory factory )
        {
            this.sceneManagerEnumerator.RemoveFactory( factory );
        }

        /// <summary>
        ///		Gets more information about a given type of SceneManager.
        /// </summary>
        /// <remarks>
        ///		The metadata returned tells you a few things about a given type 
        ///		of SceneManager, which can be created using a factory that has been
        ///		registered already.
        /// </remarks>
        /// <param name="typeName">
        ///		The type name of the SceneManager you want to enquire on.
        /// 	If you don't know the typeName already, you can iterate over the 
        ///		metadata for all types using getMetaDataIterator.
        /// </param>
        public SceneManagerMetaData GetSceneManagerMetaData( string typeName )
        {
            return this.sceneManagerEnumerator.GetMetaData( typeName );
        }

        /// <summary>
        ///		Creates a <see cref="SceneManager"/> instance of a given type.
        /// </summary>
        /// <remarks>
        ///		You can use this method to create a SceneManager instance of a 
        ///		given specific type. You may know this type already, or you may
        ///		have discovered it by looking at the results from <see cref="Root.GetSceneManagerMetaData"/>.
        /// </remarks>
        /// <param name="typeName">String identifying a unique SceneManager type.</param>
        /// <param name="instanceName">
        ///		Optional name to given the new instance that is created.
        ///		If you leave this blank, an auto name will be assigned.
        /// </param>
        /// <returns></returns>
        public SceneManager CreateSceneManager( string typeName, string instanceName )
        {
            return this.sceneManagerEnumerator.CreateSceneManager( typeName, instanceName );
        }

        /// <summary>
        ///		Creates a <see cref="SceneManager"/> instance based on scene type support.
        /// </summary>
        /// <remarks>
        ///		Creates an instance of a <see cref="SceneManager"/> which supports the scene types
        ///		identified in the parameter. If more than one type of SceneManager 
        ///		has been registered as handling that combination of scene types, 
        ///		in instance of the last one registered is returned.	
        /// </remarks>
        /// <param name="sceneType"> A mask containing one or more <see cref="SceneType"/> flags.</param>
        /// <returns></returns>
        public SceneManager CreateSceneManager( SceneType sceneType )
        {
            string instanceName = ( new NameGenerator<SceneManager>() ).GetNextUniqueName(sceneType.ToString());
            return this.sceneManagerEnumerator.CreateSceneManager( sceneType, instanceName );
        }
        /// <summary>
        ///		Creates a <see cref="SceneManager"/> instance based on scene type support.
        /// </summary>
        /// <remarks>
        ///		Creates an instance of a <see cref="SceneManager"/> which supports the scene types
        ///		identified in the parameter. If more than one type of SceneManager 
        ///		has been registered as handling that combination of scene types, 
        ///		in instance of the last one registered is returned.	
        /// </remarks>
        /// <param name="sceneType"> A mask containing one or more <see cref="SceneType"/> flags.</param>
        /// <param name="instanceName">
        ///		Optional name to given the new instance that is
        ///		created. If you leave this blank, an auto name will be assigned.
        /// </param>
        /// <returns></returns>
        public SceneManager CreateSceneManager( SceneType sceneType, string instanceName )
        {
            if (String.IsNullOrEmpty( instanceName ) )
            {
                return CreateSceneManager( sceneType );
            }
            return this.sceneManagerEnumerator.CreateSceneManager( sceneType, instanceName );
        }

        /// <summary>
        ///		Destroys an instance of a SceneManager.
        /// </summary>
        /// <param name="instance"></param>
        public void DestroySceneManager( SceneManager instance )
        {
            this.sceneManagerEnumerator.DestroySceneManager( instance );
        }

        /// <summary>
        ///		Gets an existing SceneManager instance that has already been created,
        ///		identified by the instance name.
        /// </summary>
        /// <param name="instanceName">The name of the instance to retrieve.</param>
        /// <returns></returns>
        public SceneManager GetSceneManager( string instanceName )
        {
            return this.sceneManagerEnumerator.GetSceneManager( instanceName );
        }

        /// <summary>
        ///    Initializes the renderer.
        /// </summary>
        /// <remarks>
        ///     This method can only be called after a renderer has been
        ///     selected with <see cref="Root.RenderSystem"/>, and it will initialize
        ///     the selected rendering system ready for use.
        /// </remarks>
        /// <param name="autoCreateWindow">
        ///     If true, a rendering window will automatically be created (saving a call to
        ///     <see cref="RenderSystem.CreateRenderWindow"/>). The window will be
        ///     created based on the options currently set on the render system.
        /// </param>
        /// <returns>A reference to the automatically created window (if requested), or null otherwise.</returns>
        public RenderWindow Initialize( bool autoCreateWindow )
        {
            return this.Initialize( autoCreateWindow, "Axiom Render Window" );
        }

        /// <summary>
        ///    Initializes the renderer.
        /// </summary>
        /// <remarks>
        ///     This method can only be called after a renderer has been
        ///     selected with <see cref="Root.RenderSystem"/>, and it will initialize
        ///     the selected rendering system ready for use.
        /// </remarks>
        /// <param name="autoCreateWindow">
        ///     If true, a rendering window will automatically be created (saving a call to
        ///     <see cref="RenderSystem.CreateRenderWindow"/>). The window will be
        ///     created based on the options currently set on the render system.
        /// </param>
        /// <param name="windowTitle">Title to use by the window.</param>
        /// <returns>A reference to the automatically created window (if requested), or null otherwise.</returns>
        public RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
        {
            if ( this.activeRenderSystem == null )
            {
                throw new AxiomException( "Cannot initialize - no render system has been selected." );
            }

            new ControllerManager();

 #if !(XBOX || XBOX360 || SILVERLIGHT)
            PlatformInformation.Log( LogManager.Instance.DefaultLog );
#endif
            // initialize the current render system
            this.autoWindow = this.activeRenderSystem.Initialize( autoCreateWindow, windowTitle );

            // if they chose to auto create a window, also initialize several subsystems
            if ( autoCreateWindow )
            {
                this.OneTimePostWindowInit();
            }

            // initialize timer
            this.timer.Reset();

            return this.autoWindow;
        }

        /// <summary>
        ///    Internal method for one-time tasks after first window creation.
        /// </summary>
        private void OneTimePostWindowInit()
        {
            if ( this.firstTimePostWindowInit )
            {
                // init material manager singleton, which parse sources for materials
                MaterialManager.Instance.Initialize();

                // init the particle system manager singleton
				//clarabie - temporarily disabled because something's wrong here on the 360 with ParticleSystemManager.RegisterParsers
#if !(XBOX || XBOX360 || SILVERLIGHT)
                ParticleSystemManager.Instance.Initialize();
#endif

                // init mesh manager
                MeshManager.Instance.Initialize();

                this.firstTimePostWindowInit = false;
            }
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="isFullscreen"></param>
        /// <returns></returns>
        public RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen )
        {
            return this.CreateRenderWindow( name, width, height, isFullScreen, null );
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="name"></param>
        /// <param name="target"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="isFullscreen"></param>
        /// <param name="miscParams">
        ///		A collection of addition render system specific options.
        ///	</param>
        public RenderWindow CreateRenderWindow( string name,
                                                int width,
                                                int height,
                                                bool isFullscreen,
                                                NamedParameterList miscParams )
        {
            Debug.Assert( this.activeRenderSystem != null,
                          "Cannot create a RenderWindow without an active RenderSystem." );

            // create a new render window via the current render system
            RenderWindow window = this.activeRenderSystem.CreateRenderWindow( name,
                                                                              width,
                                                                              height,
                                                                              isFullscreen,
                                                                              miscParams );

            // do any required initialization
            if ( this.firstTimePostWindowInit )
            {
                this.OneTimePostWindowInit();
                // window.Primary = true;
            }

            return window;
        }

        /// <summary>
        ///		Asks the current API to convert an instance of ColorEx to a 4 byte packed
        ///		int value the way it would expect it. 		
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public int ConvertColor( ColorEx color )
        {
            Debug.Assert( this.activeRenderSystem != null, "Cannot covert color value without an active renderer." );

            return this.activeRenderSystem.ConvertColor( color );
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="target"></param>
        public void DetachRenderTarget( RenderTarget target )
        {
            if ( this.activeRenderSystem == null )
            {
                throw new AxiomException( "Cannot detach render target - no render system has been selected." );
            }

            this.activeRenderSystem.DetachRenderTarget( target );
        }

        protected long CaptureCurrentTime()
        {
            return Stopwatch.GetTimestamp();
        }

        /// <summary>
        ///		Renders one frame.
        /// </summary>
        /// <remarks>
        ///		Updates all the render targets automatically and then returns, raising frame events before and after.
        /// </remarks>
        /// <returns>True if execution should continue, false if a quit was requested.</returns>
        public void RenderOneFrame()
        {
            // If we're capping the maximum frame rate, check to see
            // if we should sleep
            if ( this.microsecondsPerFrame != 0 )
            {
                long current = this.CaptureCurrentTime();
                long diff = ( long ) Utility.Abs( current - this.lastFrameStartTime );
                float microsecondsSinceLastFrame = diff * this.microsecondsPerTick;
                float mdiff = this.microsecondsPerFrame - microsecondsSinceLastFrame;
                // If the difference is greater than 500usec and less
                // than 1000 ms, sleep
                if ( mdiff > 500f && mdiff < 1000000f )
                {
                    Thread.Sleep( ( int ) ( Utility.Min( mdiff / 1000f, 200f ) ) );
                    this.lastFrameStartTime = this.CaptureCurrentTime();
                }
                else
                {
                    this.lastFrameStartTime = current;
                }
            }
            // Stop rendering if frame callback says so
            oneFrameMeter.Enter();
            oneFrameStartedMeter.Enter();
            this.OnFrameStarted();
            oneFrameStartedMeter.Exit();

            // bail out before continuing
            if ( this.queuedEnd )
            {
                oneFrameMeter.Exit();
                return;
            }

            // update all current render targets
            updateRenderTargetsMeter.Enter();
            this.UpdateAllRenderTargets();
            updateRenderTargetsMeter.Exit();

            // Stop rendering if frame callback says so

            oneFrameEndedMeter.Enter();
            this.OnFrameEnded();
            oneFrameEndedMeter.Exit();

            oneFrameMeter.Exit();
        }

        /// <summary>
        ///		Starts the default rendering loop.
        /// </summary>
        public void StartRendering()
        {
            Debug.Assert( this.activeRenderSystem != null,
                          "Engine cannot start rendering without an active RenderSystem." );

            this.activeRenderSystem.InitRenderTargets();

            // initialize the vars
            this.lastStartTime = this.lastEndTime = this.timer.Milliseconds;

            // reset to false so that rendering can begin
            this.queuedEnd = false;

            while ( !this.queuedEnd )
            {
                // Make sure we're collecting if it's called for
                if ( this.meterFrameCount > 0 )
                {
                    MeterManager.Collecting = true;
                }

                // allow OS events to process (if the platform requires it
                frameMeter.Enter();
                eventMeter.Enter();
                if ( WindowEventMonitor.Instance.MessagePump != null )
                {
                    WindowEventMonitor.Instance.MessagePump();
                }
                eventMeter.Exit();

                if ( this.suspendRendering )
                {
                    Thread.Sleep( 100 );
                    frameMeter.Exit();
                    continue;
                }

                renderMeter.Enter();
                this.RenderOneFrame();
                renderMeter.Exit();

                if ( this.activeRenderSystem.RenderTargetCount == 0 )
                {
                    this.QueueEndRendering();
                }
                frameMeter.Exit();

                // Turn metering on or off, and generate the report if
                // we're done
                if ( this.meterFrameCount > 0 )
                {
                    this.meterFrameCount--;
                    if ( this.meterFrameCount == 0 )
                    {
                        MeterManager.Collecting = false;
                        MeterManager.Report( "Frame Processing" );
                    }
                }
                else if ( this.pendingMeterFrameCount > 0 )
                {
                    // We'll start metering next frame
                    this.meterFrameCount = this.pendingMeterFrameCount;
                    this.pendingMeterFrameCount = 0;
                }
            }
        }

        /// <summary>
        ///		Shuts down the engine and unloads plugins.
        /// </summary>
        public void Shutdown()
        {
            //_isIntialized = false;
            LogManager.Instance.Write( "*-*-* Axiom Shutdown Initiated." );

            SceneManagerEnumerator.Instance.ShutdownAll();

            // destroy all auto created GPU programs
            ShadowVolumeExtrudeProgram.Shutdown();

            // ResourceBackGroundPool.Instance.Shutdown();
            ResourceGroupManager.Instance.ShutdownAll();

            PluginManager.Instance.UnloadAll();
        }

        /// <summary>
        ///		Requests that the rendering engine shutdown at the beginning of the next frame.
        /// </summary>
        public void QueueEndRendering()
        {
            this.queuedEnd = true;
        }

        /// <summary>
        ///     Internal method used for updating all <see cref="RenderTarget"/> objects (windows, 
        ///     renderable textures etc) which are set to auto-update.
        /// </summary>
        /// <remarks>
        ///     You don't need to use this method if you're using Axiom's own internal
        ///     rendering loop (<see cref="Root.StartRendering"/>). If you're running your own loop
        ///     you may wish to call it to update all the render targets which are
        ///     set to auto update (<see cref="RenderTarget.AutoUpdated"/>). You can also update
        ///     individual <see cref="RenderTarget"/> instances using their own Update() method.
        /// </remarks>
        public void UpdateAllRenderTargets()
        {
            this.activeRenderSystem.UpdateAllRenderTargets();
        }

        public void ToggleMetering( int frameCount )
        {
            if ( this.meterFrameCount == 0 )
            {
                MeterManager.ClearEvents();
                this.pendingMeterFrameCount = frameCount;
            }
            else
            {
                // Set it to 1 so we'll stop metering at the end of the next frame
                this.meterFrameCount = 1;
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        ///		Called to shutdown the engine and dispose of all it's resources.
        /// </summary>
        public void Dispose()
        {
            // force the engine to shutdown
            this.Shutdown();

            if ( CompositorManager.Instance != null )
            {
                CompositorManager.Instance.Dispose();
            }

            if ( OverlayManager.Instance != null )
            {
                OverlayManager.Instance.Dispose();
            }

            if ( OverlayElementManager.Instance != null )
            {
                OverlayElementManager.Instance.Dispose();
            }

            if ( FontManager.Instance != null )
            {
                FontManager.Instance.Dispose();
            }

            if ( ArchiveManager.Instance != null )
            {
                ArchiveManager.Instance.Dispose();
            }

            if ( SkeletonManager.Instance != null )
            {
                SkeletonManager.Instance.Dispose();
            }

            if ( MeshManager.Instance != null )
            {
                MeshManager.Instance.Dispose();
            }

            if ( MaterialManager.Instance != null )
            {
                MaterialManager.Instance.Dispose();
            }
            MaterialSerializer.materialSourceFiles.Clear();

            if ( ParticleSystemManager.Instance != null )
            {
                ParticleSystemManager.Instance.Dispose();
            }

            if ( ControllerManager.Instance != null )
            {
                ControllerManager.Instance.Dispose();
            }

            if ( HighLevelGpuProgramManager.Instance != null )
            {
                HighLevelGpuProgramManager.Instance.Dispose();
            }

            if ( PluginManager.Instance != null )
            {
                PluginManager.Instance.Dispose();
            }

            Pass.ProcessPendingUpdates();

            if ( ResourceGroupManager.Instance != null )
            {
                ResourceGroupManager.Instance.Dispose();
            }

#if !XBOX360
            if ( PlatformManager.Instance != null )
            {
                PlatformManager.Instance.Dispose();
            }
#endif
            if ( LogManager.Instance != null )
            {
                LogManager.Instance.Dispose();
            }

            instance = null;
        }

        #endregion

        #region Internal Engine Methods

        /// <summary>
        ///    Internal method for calculating the average time between recently fired events.
        /// </summary>
        /// <param name="time">The current time in milliseconds.</param>
        /// <param name="type">The type event to calculate.</param>
        /// <returns>Average time since last event of the same type.</returns>
        private float CalculateEventTime( long time, FrameEventType type )
        {
            float result = 0;

            if ( type == FrameEventType.Start )
            {
                result = ( float ) ( time - this.lastStartTime ) / 1000;

                // update the last start time before the render targets are rendered
                this.lastStartTime = time;
            }
            else
            {
                // increment frameCount
                this.frameCount++;

                // collect performance stats
                if ( ( time - this.lastCalculationTime ) > this.secondsBetweenFPSAverages * 1000f )
                {
                    // Is It Time To Update Our Calculations?
                    // Calculate New Framerate
                    this.currentFPS = ( float ) this.frameCount / ( float ) ( time - this.lastCalculationTime ) * 1000f;

                    // calculate the averge framerate
                    if ( this.averageFPS == 0 )
                    {
                        this.averageFPS = this.currentFPS;
                    }
                    else
                    {
                        this.averageFPS = ( this.averageFPS + this.currentFPS ) / 2.0f;
                    }

                    // Is The New Framerate A New Low?
                    if ( this.currentFPS < this.lowestFPS || ( int ) this.lowestFPS == 0 )
                    {
                        // Set It To The New Low
                        this.lowestFPS = this.currentFPS;
                    }

                    // Is The New Framerate A New High?
                    if ( this.currentFPS > this.highestFPS )
                    {
                        // Set It To The New High
                        this.highestFPS = this.currentFPS;
                    }

                    // Update Our Last Frame Time To Now
                    this.lastCalculationTime = time;

                    // Reset Our Frame Count
                    this.frameCount = 0;
                }

                result = ( float ) ( time - this.lastEndTime ) / 1000;

                this.lastEndTime = time;
            }

            return result;
        }

        /// <summary>
        ///    Method for raising frame started events.
        /// </summary>
        /// <remarks>
        ///    This method is only for internal use when you use the built-in rendering
        ///    loop (Root.StartRendering). However, if you run your own rendering loop then
        ///    you should call this method to ensure that FrameEvent handlers are notified
        ///    of frame events; processes like texture animation and particle systems rely on 
        ///    this.
        ///    <p/>
        ///    This method calculates the frame timing information for you based on the elapsed
        ///    time. If you want to specify elapsed times yourself you should call the other 
        ///    version of this method which takes event details as a parameter.
        /// </remarks>
        public void OnFrameStarted()
        {
            FrameEventArgs e = new FrameEventArgs();
            long now = this.timer.Milliseconds;
            e.TimeSinceLastFrame = this.CalculateEventTime( now, FrameEventType.Start );

            // if any event handler set this to true, that will signal the engine to shutdown
            this.OnFrameStarted( e );
        }

        /// <summary>
        ///    Method for raising frame ended events.
        /// </summary>
        /// <remarks>
        ///    This method is only for internal use when you use the built-in rendering
        ///    loop (Root.StartRendering). However, if you run your own rendering loop then
        ///    you should call this method to ensure that FrameEvent handlers are notified
        ///    of frame events; processes like texture animation and particle systems rely on 
        ///    this.
        ///    <p/>
        ///    This method calculates the frame timing information for you based on the elapsed
        ///    time. If you want to specify elapsed times yourself you should call the other 
        ///    version of this method which takes event details as a parameter.
        /// </remarks>
        public void OnFrameEnded()
        {
            FrameEventArgs e = new FrameEventArgs();
            long now = this.timer.Milliseconds;
            e.TimeSinceLastFrame = this.CalculateEventTime( now, FrameEventType.End );

            // if any event handler set this to true, that will signal the engine to shutdown
            this.OnFrameEnded( e );
        }

        /// <summary>
        ///    Method for raising frame started events.
        /// </summary>
        /// <remarks>
        ///    This method is only for internal use when you use the built-in rendering
        ///    loop (Root.StartRendering). However, if you run your own rendering loop then
        ///    you should call this method to ensure that FrameEvent handlers are notified
        ///    of frame events; processes like texture animation and particle systems rely on 
        ///    this.
        ///    <p/>
        ///    This method takes an event object as a parameter, so you can specify the times
        ///    yourself. If you are happy for the engine to automatically calculate the frame time
        ///    for you, then call the other version of this method with no parameters.
        /// </remarks>
        /// <param name="e">
        ///    Event object which includes all the timing information which must already be 
        ///    calculated.  RequestShutdown should be checked after each call, because that means
        ///    an event handler is requesting that shudown begin for one reason or another.
        /// </param>
        public void OnFrameStarted( FrameEventArgs e )
        {
            // increment the current frame count
            this.currentFrameCount++;

            // call the event, which automatically fires all registered handlers
            if ( this.FrameStarted != null )
            {
                this.FrameStarted( this, e );
            }
        }

        /// <summary>
        ///    Method for raising frame ended events.
        /// </summary>
        /// <remarks>
        ///    This method is only for internal use when you use the built-in rendering
        ///    loop (Root.StartRendering). However, if you run your own rendering loop then
        ///    you should call this method to ensure that FrameEvent handlers are notified
        ///    of frame events; processes like texture animation and particle systems rely on 
        ///    this.
        ///    <p/>
        ///    This method takes an event object as a parameter, so you can specify the times
        ///    yourself. If you are happy for the engine to automatically calculate the frame time
        ///    for you, then call the other version of this method with no parameters.
        /// </remarks>
        /// <param name="e">
        ///    Event object which includes all the timing information which must already be 
        ///    calculated.  RequestShutdown should be checked after each call, because that means
        ///    an event handler is requesting that shudown begin for one reason or another.
        /// </param>
        public void OnFrameEnded( FrameEventArgs e )
        {
            // call the event, which automatically fires all registered handlers
            if ( this.FrameEnded != null )
            {
                this.FrameEnded( this, e );
            }

            // Tell buffer manager to free temp buffers used this frame
            if ( HardwareBufferManager.Instance != null )
            {
                HardwareBufferManager.Instance.ReleaseBufferCopies( false );
            }
        }

        #endregion

        #region MovableObjectFactory methods

        /// <summary>
        ///     Allocate and retrieve the next MovableObject type flag.
        /// </summary>
        /// <remarks>
        ///     This is done automatically if MovableObjectFactory.RequestTypeFlags
        ///	    returns true; don't call this manually unless you're sure you need to.
        /// </remarks>
        public ulong NextMovableObjectTypeFlag()
        {
            if ( this.nextMovableObjectTypeFlag == USER_TYPE_MASK_LIMIT )
            {
                throw new AxiomException(
                        "Cannot allocate a type flag since all the available flags have been used." );
            }

            ulong ret = this.nextMovableObjectTypeFlag;
            this.nextMovableObjectTypeFlag <<= 1;
            return ret;
        }

        /// <summary>
        ///     Checks whether a factory is registered for a given MovableObject type
        /// </summary>
        /// <param name="typeName">
        ///     The factory type to check for.
        /// </param>
        /// <returns>True if the factory type is registered.</returns>
        public bool HasMovableObjectFactory( string typeName )
        {
            return this.movableObjectFactoryMap.ContainsKey( typeName );
        }

        /// <summary>
        ///     Get a MovableObjectFactory for the given type.
        /// </summary>
        /// <param name="typeName">
        ///     The factory type to obtain.
        /// </param>
        /// <returns>
        ///     A factory for the given type of MovableObject.
        /// </returns>
        public MovableObjectFactory GetMovableObjectFactory( string typeName )
        {
            if ( !this.movableObjectFactoryMap.ContainsKey( typeName ) )
            {
                throw new AxiomException( "MovableObjectFactory for type " + typeName + " does not exist." );
            }

            return this.movableObjectFactoryMap[ typeName ];
        }

        /// <summary>
        ///     Removes a previously registered MovableObjectFactory.
        /// </summary>
        /// <remarks>
        ///	    All instances of objects created by this factory will be destroyed
        ///	    before removing the factory (by calling back the factories 
        ///	    'DestroyInstance' method). The plugin writer is responsible for actually
        ///	    destroying the factory.
        /// </remarks>
        /// <param name="fact">The instance to remove.</param>
        public void RemoveMovableObjectFactory( MovableObjectFactory fact )
        {
            if ( this.movableObjectFactoryMap.ContainsValue( fact ) )
            {
                this.movableObjectFactoryMap.Remove( fact.Type );
            }
        }

        /** Allocate the next MovableObject type flag.
        @remarks
            This is done automatically if MovableObjectFactory::requestTypeFlags
            returns true; don't call this manually unless you're sure you need to.
        */

        /// <summary>
        ///     Register a new MovableObjectFactory which will create new MovableObject
        ///	    instances of a particular type, as identified by the Type property.
        /// </summary>
        /// <remarks>
        ///     Plugin creators can create subclasses of MovableObjectFactory which 
        ///	    construct custom subclasses of MovableObject for insertion in the 
        ///	    scene. This is the primary way that plugins can make custom objects
        ///	    available.
        /// </remarks>
        /// <param name="fact">
        ///     The factory instance.
        /// </param>
        /// <param name="overrideExisting">
        ///     Set this to true to override any existing 
        ///	    factories which are registered for the same type. You should only
        ///	    change this if you are very sure you know what you're doing.
        /// </param>
        public void AddMovableObjectFactory( MovableObjectFactory fact, bool overrideExisting )
        {
            if ( this.movableObjectFactoryMap.ContainsKey( fact.Type ) && !overrideExisting )
            {
                throw new AxiomException( "A factory of type '" + fact.Type + "' already exists." );
            }

            if ( fact.RequestTypeFlags )
            {
                if ( this.movableObjectFactoryMap.ContainsValue( fact ) )
                {
                    // Copy type flags from the factory we're replacing
                    fact.TypeFlag = ( this.movableObjectFactoryMap[ fact.Type ] ).TypeFlag;
                }
                else
                {
                    // Allocate new
                    fact.TypeFlag = this.NextMovableObjectTypeFlag();
                }
            }

            // Save
            this.movableObjectFactoryMap.Add( fact.Type, fact );

            LogManager.Instance.Write( "Factory " + fact.GetType().Name + " registered for MovableObjectType '" + fact.Type + "'." );
        }

        public MovableObjectFactoryMap MovableObjectFactories
        {
            get
            {
                return movableObjectFactoryMap;
            }
        }

        #endregion MovableObjectFactory methods
    }

    #region Frame Events

    /// <summary>
    ///		A delegate for defining frame events.
    /// </summary>
    public delegate bool FrameEvent( object source, FrameEventArgs e );

    /// <summary>
    ///		Used to supply info to the FrameStarted and FrameEnded events.
    /// </summary>
    public class FrameEventArgs : EventArgs
    {
        /// <summary>
        ///    Time elapsed (in milliseconds) since the last frame event.
        /// </summary>
        public float TimeSinceLastEvent;

        /// <summary>
        ///    Time elapsed (in milliseconds) since the last frame.
        /// </summary>
        public float TimeSinceLastFrame;
    }

    public enum FrameEventType
    {
        Start,
        End
    }

    #endregion Frame Events
}