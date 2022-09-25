﻿/*
 * Copyright (c) 2009 Olav Kalgraf(olav.kalgraf@gmail.com)
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenCLNet
{

    unsafe public class Platform : InteropTools.IPropertyContainer
    {
        #region Properties

        public IntPtr PlatformID { get; protected set; }
        /// <summary>
        /// Equal to "FULL_PROFILE" if the implementation supports the OpenCL specification or
        /// "EMBEDDED_PROFILE" if the implementation supports the OpenCL embedded profile.
        /// </summary>
        public string Profile { get { return InteropTools.ReadString( this, (uint)PlatformInfo.PROFILE ); } }
        /// <summary>
        /// OpenCL version string. Returns the OpenCL version supported by the implementation. This version string
        /// has the following format: OpenCL&lt;space&gt;&lt;major_version.minor_version&gt;&lt;space&gt;&lt;platform specific information&gt;
        /// </summary>
        public string Version { get { return InteropTools.ReadString( this, (uint)PlatformInfo.VERSION ); } }
        /// <summary>
        /// Platform name string
        /// </summary>
        public string Name { get { return InteropTools.ReadString( this, (uint)PlatformInfo.NAME ); } }
        /// <summary>
        /// Platform Vendor string
        /// </summary>
        public string Vendor { get { return InteropTools.ReadString( this, (uint)PlatformInfo.VENDOR ); } }
        /// <summary>
        /// Space separated string of extension names.
        /// Note that this class has some support functions to help query extension capbilities.
        /// This property is only present for completeness.
        /// </summary>
        public string Extensions { get { return InteropTools.ReadString( this, (uint)PlatformInfo.EXTENSIONS ); } }
        /// <summary>
        /// Convenience method to get at the major_version field in the Version string
        /// </summary>
        public int OpenCLMajorVersion { get; protected set; }
        /// <summary>
        /// Convenience method to get at the minor_version field in the Version string
        /// </summary>
        public int OpenCLMinorVersion { get; protected set; }

        #endregion

        #region Private variables

        Regex VersionStringRegex = new Regex("OpenCL (?<Major>[0-9]+)\\.(?<Minor>[0-9]+)");
        protected Dictionary<IntPtr, Device> _Devices = new Dictionary<IntPtr, Device>();
        Device[] DeviceList;
        IntPtr[] DeviceIDs;

        protected HashSet<string> ExtensionHashSet = new HashSet<string>();
        protected Dictionary<string, Extension> extensionSupport = new Dictionary<string, Extension>();
        DirectX9Extension DirectX9Extension;
        DirectX10Extension DirectX10Extension;
        DirectX11Extension DirectX11Extension;

        #endregion

        #region Constructors

        public Platform( IntPtr platformID )
        {
            PlatformID = platformID;

            // Create a local representation of all devices
            DeviceIDs = QueryDeviceIntPtr( DeviceType.ALL );
            for( int i=0; i<DeviceIDs.Length; i++ )
                _Devices[DeviceIDs[i]] = new Device( this, DeviceIDs[i] );
            DeviceList = InteropTools.ConvertDeviceIDsToDevices( this, DeviceIDs );

            InitializeExtensionHashSet();

            Match m = VersionStringRegex.Match(Version);
            if (m.Success)
            {
                OpenCLMajorVersion = int.Parse(m.Groups["Major"].Value);
                OpenCLMinorVersion = int.Parse(m.Groups["Minor"].Value);
            }
            else
            {
                OpenCLMajorVersion = 1;
                OpenCLMinorVersion = 0;
            }

            if ( VersionCheck(1,2) )
            {
                AddExtensionSupport(new DirectX9Extension(this));
                AddExtensionSupport(new DirectX10Extension(this));
                AddExtensionSupport(new DirectX11Extension(this));
            }
        }

        #endregion

        public Context CreateDefaultContext()
        {
            return CreateDefaultContext(null, IntPtr.Zero);
        }

        public Context CreateDefaultContext( ContextNotify notify, IntPtr userData )
        {
            IntPtr[] properties = new IntPtr[]
            {
                new IntPtr((long)ContextProperties.PLATFORM), PlatformID,
                IntPtr.Zero,
            };

            IntPtr contextID;
            ErrorCode result;

            contextID = (IntPtr)OpenCL.CreateContext( properties,
                (uint)DeviceIDs.Length,
                DeviceIDs,
                notify,
                userData,
                out result );
            if( result!=ErrorCode.SUCCESS )
                throw new OpenCLException( "CreateContext failed with error code: "+result, result);
            return new Context( this, contextID );
        }

        public Context CreateContext(IntPtr[] contextProperties, Device[] devices, ContextNotify notify, IntPtr userData)
        {
            IntPtr contextID;
            ErrorCode result;

            IntPtr[] deviceIDs = InteropTools.ConvertDevicesToDeviceIDs(devices);
            contextID = (IntPtr)OpenCL.CreateContext(contextProperties,
                (uint)deviceIDs.Length,
                deviceIDs,
                notify,
                userData,
                out result);
            if (result != ErrorCode.SUCCESS)
                throw new OpenCLException("CreateContext failed with error code: " + result, result);
            return new Context(this, contextID);
        }

        public Context CreateContextFromType(IntPtr[] contextProperties, DeviceType deviceType, ContextNotify notify, IntPtr userData)
        {
            IntPtr contextID;
            ErrorCode result;

            contextID = (IntPtr)OpenCL.CreateContextFromType(contextProperties,
                deviceType,
                notify,
                userData,
                out result);
            if (result != ErrorCode.SUCCESS)
                throw new OpenCLException("CreateContextFromType failed with error code: " + result, result);
            return new Context(this, contextID);
        }

        /// <summary>
        /// Get a Device structure, given an OpenCL device handle
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Device GetDevice(IntPtr index)
        {
            if (_Devices.ContainsKey(index))
                return _Devices[index];
            else
                return null;
        }

        protected IntPtr[] QueryDeviceIntPtr( DeviceType deviceType )
        {
            ErrorCode result;
            uint numberOfDevices;
            IntPtr[] deviceIDs;

            result = (ErrorCode)OpenCL.GetDeviceIDs( PlatformID, deviceType, 0, null, out numberOfDevices );
            if (result == ErrorCode.DEVICE_NOT_FOUND || (result == ErrorCode.SUCCESS && numberOfDevices==0))
                return new IntPtr[0];

            if( result!=ErrorCode.SUCCESS )
                throw new OpenCLException( "GetDeviceIDs failed: "+((ErrorCode)result).ToString(), result);

            deviceIDs = new IntPtr[numberOfDevices];
            result = (ErrorCode)OpenCL.GetDeviceIDs(PlatformID, deviceType, numberOfDevices, deviceIDs, out numberOfDevices);
            if (result != ErrorCode.SUCCESS)
                throw new OpenCLException("GetDeviceIDs failed: " + ((ErrorCode)result).ToString(), result);
            return deviceIDs;
        }

        /// <summary>
        /// Find all devices of a specififc type
        /// </summary>
        /// <param name="deviceType"></param>
        /// <returns>Array containing the devices</returns>
        public Device[] QueryDevices( DeviceType deviceType )
        {
            IntPtr[] deviceIDs;

            deviceIDs = QueryDeviceIntPtr( deviceType );
            return InteropTools.ConvertDeviceIDsToDevices( this, deviceIDs );
        }

        /// <summary>
        /// Returns true if this OpenCL platform has a version number greater
        /// than or equal to the version specified in the parameters
        /// </summary>
        /// <param name="majorVersion"></param>
        /// <param name="minorVersion"></param>
        /// <returns></returns>
        public bool VersionCheck(int majorVersion, int minorVersion)
        {
            return OpenCLMajorVersion>majorVersion || (OpenCLMajorVersion==majorVersion && OpenCLMinorVersion>=minorVersion);
        }

        #region Extension management

        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        /// <param name="func_name"></param>
        /// <returns></returns>
        public IntPtr GetExtensionFunctionAddress(string func_name)
        {
            return OpenCL.GetExtensionFunctionAddressForPlatform(this,func_name);
        }

        protected void InitializeExtensionHashSet()
        {
            string[] ext = Extensions.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in ext)
                ExtensionHashSet.Add(s);
        }

        /// <summary>
        /// Test if this platform flags support for a specific extension
        /// </summary>
        /// <param name="extension"></param>
        /// <returns>Returns true if the extension is supported</returns>
        public bool HasExtension(string extension)
        {
            return ExtensionHashSet.Contains(extension);
        }

        /// <summary>
        /// Test if this Platform flags support for a set of exentions.
        /// </summary>
        /// <param name="extensions"></param>
        /// <returns>Returns true if all the extensions are supported</returns>
        public bool HasExtensions( IEnumerable<string> extensions)
        {
            foreach (string s in extensions)
            {
                if (!ExtensionHashSet.Contains(s))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// <para>
        /// Add user created extension support to this platform.
        /// This lets you handle your own extensions exactly like built-in extensions.
        /// That is, they'll be reachable by GetExtension and will be subject to the
        /// same regime as built-in extensions in GetExtension().
        /// </para>
        /// 
        /// <remarks>Note that this mechanism can also be used to override built-in extension support.
        /// Such overriding is accomplished by subclassing the built-in extension, overriding
        /// its entrypoints, and then adding it with this function.</remarks>
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If extension is null</exception>
        public void AddExtensionSupport(Extension extension)
        {
            if (extension == null)
                throw new ArgumentNullException();

            if (!extension.IsAvailable || !HasExtension(extension.GetName()))
                return;

            if (extensionSupport.ContainsKey(extension.GetName()))
            {
                extensionSupport.Remove(extension.GetName());
                extensionSupport.Add(extension.GetName(), extension);
                InitializeBuiltInExtensions();
            }
            else
            {
                extensionSupport.Add(extension.GetName(), extension);
            }
        }

        /// <summary>
        /// Get a named extension, if this Platform has support for it
        /// </summary>
        /// <param name="ExtensionName"></param>
        /// <returns> An Extension object, or null if the extension is not supported by the Platform</returns>
        public Extension GetExtension(string extensionName)
        {
            Extension e;

            // Always return null if the underlying Platform doesn't support the extension
            if (!HasExtension(extensionName))
                return null;

            if (extensionSupport.TryGetValue(extensionName, out e))
                return e;
            else
                return null;
        }


        internal void InitializeBuiltInExtensions()
        {
            DirectX9Extension = (DirectX9Extension)GetExtension(DirectX9Extension.ExtensionName);
            DirectX10Extension = (DirectX10Extension)GetExtension(DirectX10Extension.ExtensionName);
            DirectX11Extension = (DirectX11Extension)GetExtension(DirectX11Extension.ExtensionName);
        }

        #endregion

        public static implicit operator IntPtr( Platform p )
        {
            return p.PlatformID;
        }

        #region IPropertyContainer Members

        public IntPtr GetPropertySize( uint key )
        {
            IntPtr propertySize;
            ErrorCode result;

            result = (ErrorCode)OpenCL.GetPlatformInfo( PlatformID, key, IntPtr.Zero, null, out propertySize );
            if( result!=ErrorCode.SUCCESS )
                throw new OpenCLException( "Unable to get platform info for platform "+PlatformID+": "+result, result);
            return propertySize;

        }

        public void ReadProperty( uint key, IntPtr keyLength, void* pBuffer )
        {
            IntPtr propertySize;
            ErrorCode result;

            result = (ErrorCode)OpenCL.GetPlatformInfo( PlatformID, key, keyLength, (void*)pBuffer, out propertySize );
            if( result!=ErrorCode.SUCCESS )
                throw new OpenCLException( "Unable to get platform info for platform "+PlatformID+": "+result, result);
        }

        #endregion
    }
}
