using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Phoenix.WorkshopTool
{
    // This was taken from SESN, and butchered: https://github.com/Jimmacle/SESN/blob/master/SEServerNetwork/MethodUtil.cs
    public static class MethodUtil
    {
        /// <summary>
        /// Replaces the method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="dest">The dest.</param>
        public static void ReplaceMethod(MethodBase source, MethodBase dest)
        {
            if (!MethodSignaturesEqual(source, dest))
            {
                throw new ArgumentException("The method signatures are not the same.", nameof(source));
            }
            RuntimeHelpers.PrepareMethod(source.MethodHandle);
            RuntimeHelpers.PrepareMethod(dest.MethodHandle);

            unsafe
            {
                if (IntPtr.Size == 8)
                {
                    long* inj = (long*)dest.MethodHandle.Value.ToPointer() + 1;
                    long* tar = (long*)source.MethodHandle.Value.ToPointer() + 1;
        #if false   // dunno why this doesn't work
                    byte* injInst = (byte*)*inj;
                    byte* tarInst = (byte*)*tar;


                    int* injSrc = (int*)(injInst + 1);
                    int* tarSrc = (int*)(tarInst + 1);

                    *tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
        #else
                    *tar = *inj;
        #endif
                }
                else
                {
                    int* inj = (int*)dest.MethodHandle.Value.ToPointer() + 2;
                    int* tar = (int*)source.MethodHandle.Value.ToPointer() + 2;
        #if DEBUG
                    byte* injInst = (byte*)*inj;
                    byte* tarInst = (byte*)*tar;

                    int* injSrc = (int*)(injInst + 1);
                    int* tarSrc = (int*)(tarInst + 1);

                    *tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
        #else
                    *tar = *inj;
        #endif
                }
            }
        }

        /// <summary>
        /// Gets the address of the method stub
        /// </summary>
        /// <param name="method">The method handle.</param>
        /// <returns></returns>
        public static IntPtr GetMethodAddress(MethodBase method)
        {
            if ((method is DynamicMethod))
            {
                return GetDynamicMethodAddress(method);
            }

            // Prepare the method so it gets jited
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            unsafe
            {
                return new IntPtr((int*)method.MethodHandle.Value.ToPointer() + 2);
            }

        }

        private static IntPtr GetDynamicMethodAddress(MethodBase method)
        {
            unsafe
            {
                RuntimeMethodHandle handle = GetDynamicMethodRuntimeHandle(method);
                byte* ptr = (byte*)handle.Value.ToPointer();
                if (IntPtr.Size == 8)
                {
                    ulong* address = (ulong*)ptr;
                    address += 6;
                    return new IntPtr(address);
                }
                else
                {
                    uint* address = (uint*)ptr;
                    address += 6;
                    return new IntPtr(address);
                }


            }
        }
        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(MethodBase method)
        {
            if (method is DynamicMethod)
            {
                FieldInfo fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    RuntimeMethodHandle handle = ((RuntimeMethodHandle)fieldInfo.GetValue(method));

                    return handle;
                }
            }
            return method.MethodHandle;
        }

        private static bool MethodSignaturesEqual(MethodBase x, MethodBase y)
        {
            if (x.CallingConvention != y.CallingConvention)
            {
                return false;
            }
            Type returnX = GetMethodReturnType(x), returnY = GetMethodReturnType(y);
            // Handle case of return type being private, so 'object' has to be a substitute
            if (returnX != returnY && !returnX.IsSubclassOf(returnY))
            {
                return false;
            }
            ParameterInfo[] xParams = x.GetParameters(), yParams = y.GetParameters();
            if (xParams.Length != yParams.Length)
            {
                return false;
            }
            for (int i = 0; i < xParams.Length; i++)
            {
                if (xParams[i].ParameterType != yParams[i].ParameterType)
                {
                    return false;
                }
            }
            return true;
        }
        private static Type GetMethodReturnType(MethodBase method)
        {
            MethodInfo methodInfo = method as MethodInfo;
            if (methodInfo == null)
            {
                // Constructor info.
                throw new ArgumentException("Unsupported MethodBase : " + method.GetType().Name, nameof(method));
            }
            return methodInfo.ReturnType;
        }

        // Ref field getter, from: https://stackoverflow.com/questions/16073091/is-there-a-way-to-create-a-delegate-to-get-and-set-values-for-a-fieldinfo
        public delegate ref U RefGetter<T, U>(T obj);
        public static RefGetter<T, U> create_refgetter<T, U>(string s_field, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        {
            const BindingFlags bf = BindingFlags.NonPublic |
                                    BindingFlags.Instance |
                                    BindingFlags.DeclaredOnly;

            var fi = typeof(T).GetField(s_field, bindingFlags);
            if (fi == null)
                throw new MissingFieldException(typeof(T).Name, s_field);

            var s_name = "__refget_" + typeof(T).Name + "_fi_" + fi.Name;

            // workaround for using ref-return with DynamicMethod:
            //   a.) initialize with dummy return value
            var dm = new DynamicMethod(s_name, typeof(U), new[] { typeof(T) }, typeof(T), true);

            //   b.) replace with desired 'ByRef' return value
            dm.GetType().GetField("m_returnType", bf).SetValue(dm, typeof(U).MakeByRefType());

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, fi);
            il.Emit(OpCodes.Ret);

            return (RefGetter<T, U>)dm.CreateDelegate(typeof(RefGetter<T, U>));
        }
    }
}
