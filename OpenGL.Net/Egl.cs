﻿
// Copyright (C) 2015-2016 Luca Piccioni
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
// USA

#pragma warning disable 618

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenGL
{
	/// <summary>
	/// Modern OpenGL bindings: EGL, Native Platform Interface.
	/// </summary>
	public partial class Egl : KhronosApi
	{
		#region Constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static Egl()
		{
			Initialize();
		}

		/// <summary>
		/// Initialize OpenGL namespace static environment. This method shall be called before any other classes methods.
		/// </summary>
		public static void Initialize()
		{
			if (_Initialized == true)
				return; // Already initialized
			_Initialized = true;

			// Before linking procedures, append ANGLE directory in path
			string assemblyPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Egl)).Location);
			string anglePath = null;

			switch (Platform.CurrentPlatformId) {
				case Platform.Id.WindowsNT:
#if DEBUG
					if (IntPtr.Size == 8)
						anglePath = Path.Combine(assemblyPath, @"ANGLE\winrt10d\x64");
					else
						anglePath = Path.Combine(assemblyPath, @"ANGLE\winrt10d\x86");
#else
					if (IntPtr.Size == 8)
						anglePath = Path.Combine(assemblyPath, @"ANGLE\winrt10\x64");
					else
						anglePath = Path.Combine(assemblyPath, @"ANGLE\winrt10\x86");
#endif
					break;
			}

			// Include ANGLE path, if any
			if (anglePath != null && Directory.Exists(anglePath))
				OpenGL.GetProcAddress.AddLibraryDirectory(Path.Combine(assemblyPath, anglePath));

			// Cache imports & delegates
			_Delegates = GetDelegateList(typeof(Egl));
			_ImportMap = GetImportMap(typeof(Egl));
			
			try {
				BindAPI();
			} catch { /* Fail-safe (it may fail due Egl access) */ }
		}

		/// <summary>
		/// Flag indicating whether <see cref="Gl"/> has been initialized.
		/// </summary>
		private static bool _Initialized;

		/// <summary>
		/// OpenGL extension support.
		/// </summary>
		public static Extensions CurrentExtensions { get { return (_CurrentExtensions); } }

		/// <summary>
		/// OpenGL extension support.
		/// </summary>
		internal static Extensions _CurrentExtensions;

		#endregion

		#region API Binding

		/// <summary>
		/// Bind Windows GL delegates.
		/// </summary>
		private static void BindAPI()
		{
			// Using eglGetProcAddress
			BindDelegatesOS(Library, _ImportMap, _Delegates);
		}

		/// <summary>
		/// Default import library.
		/// </summary>
		internal const string Library = "libEGL.dll";

		/// <summary>
		/// Imported functions delegates.
		/// </summary>
		private static List<FieldInfo> _Delegates;

		/// <summary>
		/// Build a string->MethodInfo map to speed up extension loading.
		/// </summary>
		private static SortedList<string, MethodInfo> _ImportMap;

		#endregion

		#region EGL Availability

		/// <summary>
		/// Get whether <see cref="DeviceContextFactory"/> must create an EGL device context.
		/// </summary>
		public static bool IsMandatory
		{
			get
			{
				switch (Platform.CurrentPlatformId) {
					case Platform.Id.Android:
						return (true);
					default:
						return (false);
				}
			}
		}

		/// <summary>
		/// Get whether EGL layer is avaialable.
		/// </summary>
		public static bool IsAvailable { get { return (Delegates.peglInitialize != null); } }

		/// <summary>
		/// Get or set whether <see cref="DeviceContextFactory"/> should create an EGL device context, if available.
		/// </summary>
		public static bool IsRequired
		{
			get { return ((_IsRequired && IsAvailable) || IsMandatory); }
			set { _IsRequired = value; }
		}

		/// <summary>
		/// Flag for requesting an EGL device context, if available.
		/// </summary>
		private static bool _IsRequired;

		#endregion

		#region Error Handling

		/// <summary>
		/// OpenGL error checking.
		/// </summary>
		[Conditional("DEBUG")]
		private static void DebugCheckErrors(object returnValue)
		{
			int error = GetError();

			if (error != SUCCESS)
				throw new EglException(error);
		}

		#endregion

		#region Required External Declarations

		/// <summary>
		/// Structure corresponding to EGLClientPixmapHI.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct ClientPixmap
		{
			public IntPtr Data;

			public Int32 Width;

			public Int32 Height;

			public Int32 Stride;
		}

		/// <summary>
		/// Delegate corresponding to EGLSetBlobFuncANDROID.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="keySize"></param>
		/// <param name="value"></param>
		/// <param name="valueSize"></param>
		public delegate void SetBlobFuncDelegate(IntPtr key, UInt32 keySize, IntPtr value, UInt32 valueSize);

		/// <summary>
		/// Delegate corresponding to EGLGetBlobFuncANDROID.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="keySize"></param>
		/// <param name="value"></param>
		/// <param name="valueSize"></param>
		public delegate void GetBlobFuncDelegate(IntPtr key, UInt32 keySize, [Out] IntPtr value, UInt32 valueSize);

		/// <summary>
		/// Delegate corresponding to EGLDEBUGPROCKHR.
		/// </summary>
		/// <param name="error"></param>
		/// <param name="command"></param>
		/// <param name="messageType"></param>
		/// <param name="threadLabel"></param>
		/// <param name="objectLabel"></param>
		/// <param name="message"></param>
		public delegate void DebugProcKHR(uint error, string command, int messageType, IntPtr threadLabel, IntPtr objectLabel, string message);

		#endregion
	}
}
