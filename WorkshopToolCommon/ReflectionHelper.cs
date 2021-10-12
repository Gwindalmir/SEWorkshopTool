using Phoenix.WorkshopTool.Extensions;
using Sandbox;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using VRage.Utils;
using System.Linq;
#if SE
using ParallelTasks;
using MyDebug = Phoenix.WorkshopTool.Extensions.MyDebug;
#else
using VRage.Engine;
#endif

namespace Phoenix.WorkshopTool
{
    public static class ReflectionHelper
    {
#if SE
        public static MethodInfo ReflectInitModAPI()
        {
            ParameterInfo[] parameters;

            // Init ModAPI
            var initmethod = typeof(MySandboxGame).GetMethod("InitModAPI", BindingFlags.Instance | BindingFlags.NonPublic);
            MyDebug.AssertRelease(initmethod != null);

            if (initmethod != null)
            {
                parameters = initmethod.GetParameters();
                MyDebug.AssertRelease(parameters.Count() == 0);

                if (!(parameters.Count() == 0))
                    initmethod = null;
            }
            return initmethod;
        }
#else
        public static MethodInfo ReflectVRageCoreMethod(string method)
        {
            return typeof(VRageCore).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static FieldInfo ReflectVRageCoreField(string field)
        {
            return typeof(VRageCore).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        }
#endif

        public static MethodInfo ReflectSteamRestartApp()
        {
            return GetMethod(typeof(SteamAPI), nameof(SteamAPI.RestartAppIfNecessary), BindingFlags.Static | BindingFlags.Public);
        }

        public static MethodInfo ReflectFileCopy(Type[] arguments)
        {
            return GetMethod(typeof(VRage.FileSystem.MyFileSystem), nameof(VRage.FileSystem.MyFileSystem.CopyAll), BindingFlags.Static | BindingFlags.Public, arguments);
        }

        public static MethodInfo GetMethod(Type type, string method, BindingFlags binding, Type[] types = null)
        {
            if (types == null)
                return type.GetMethod(method, binding);
            else
                return type.GetMethod(method, types);
        }

        /// <summary>
        /// Replaces a method with another one
        /// </summary>
        /// <param name="sourceType">Original type</param>
        /// <param name="sourceMethod">Original method name</param>
        /// <param name="destinationType">New type</param>
        /// <param name="destinationMethod">New method name</param>
        public static bool ReplaceMethod(Type sourceType, string sourceMethod, BindingFlags sourceBinding, Type destinationType, string destinationMethod, BindingFlags? destinationBinding = null, Type[] types = null)
        {
            var methodtoreplace = GetMethod(sourceType, sourceMethod, sourceBinding, types);
            var methodtoinject = GetMethod(destinationType, destinationMethod, destinationBinding ?? sourceBinding);
            var result = ReplaceMethod(methodtoreplace, methodtoinject, types);

            if (!result)
                MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, sourceMethod));

            return result;
        }

        public static bool ReplaceMethod(MethodInfo methodtoreplace, Type destinationType, string destinationMethod, BindingFlags destinationBinding, Type[] types = null)
        {
            var methodtoinject = GetMethod(destinationType, destinationMethod, destinationBinding, types);
            var result = ReplaceMethod(methodtoreplace, methodtoinject, types);

            if (!result)
                MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, destinationMethod));

            return result;
        }

        public static bool ReplaceMethod(MethodInfo methodtoreplace, MethodInfo methodtoinject, Type[] types = null)
        {
            ParameterInfo[] sourceParameters;
            ParameterInfo[] destinationParameters;

            MyDebug.AssertRelease(methodtoreplace != null);
            if (methodtoreplace != null && methodtoinject != null)
            {
                sourceParameters = methodtoreplace.GetParameters();
                destinationParameters = methodtoinject.GetParameters();
                MyDebug.AssertDebug(sourceParameters.Length == destinationParameters.Length);
                bool valid = true;

                // Verify signatures
                for (var x = 0; x < Math.Min(destinationParameters.Length, sourceParameters.Length); x++)
                {
                    MyDebug.AssertDebug(destinationParameters[x].ParameterType == sourceParameters[x].ParameterType);
                    if (destinationParameters[x].ParameterType != sourceParameters[x].ParameterType)
                        valid = false;
                }

                if (sourceParameters.Length != destinationParameters.Length || !valid)
                    methodtoreplace = null;
            }

            if (methodtoreplace != null && methodtoinject != null)
            {
                MethodUtil.ReplaceMethod(methodtoreplace, methodtoinject);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
