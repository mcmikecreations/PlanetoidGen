using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace PlanetoidGen.Contracts.Models
{
    public static class EmitType
    {
        public static Type CreateType(string baseName, Type[] types, string[] names)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            if (types.Length != names.Length)
            {
                throw new ArgumentException(nameof(names));
            }

            if (string.IsNullOrWhiteSpace(baseName) || (baseName = baseName.Trim()).Length == 0)
            {
                throw new ArgumentException(nameof(baseName));
            }

            types.Where(x => x.IsNotPublic).Select<Type, Type>(x => throw new ArgumentException(x.Name));

            // Since multiple types can have the same name in terms of dynamic tables, we use the
            // base name and param list to distinguish between them.
            string fullName;

            {
                var fullNameBuilder = new StringBuilder();

                fullNameBuilder.Append(Escape(baseName)).Append("|");

                for (int i = 0; i < names.Length; ++i)
                {
                    if (i > 0) fullNameBuilder.Append("|");
                    fullNameBuilder.Append(Escape(types[i].Name)).Append("|").Append(Escape(names[i]));
                }

                fullName = fullNameBuilder.ToString();
            }

            if (!GeneratedTypes.TryGetValue(fullName, out var type))
            {
                // We create only a single class at a time, through this lock
                // Note that this is a variant of the double-checked locking.
                // It is safe because we are using a thread safe class.
                lock (GeneratedTypes)
                {
                    if (!GeneratedTypes.TryGetValue(fullName, out type))
                    {
                        var index = Interlocked.Increment(ref Index);

                        var name = names.Length != 0 ? string.Format("__EmitType{0}__{1}", index, names.Length) : string.Format("__EmitType{0}", index);
                        var tb = ModuleBuilder.DefineType(name, TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, typeof(object));
                        // Optionally specify this was generated during runtime.
                        tb.SetCustomAttribute(CompilerGeneratedAttributeBuilder);

                        var fields = new FieldBuilder[names.Length];
                        var getters = new MethodBuilder[names.Length];

                        for (int i = 0; i < names.Length; ++i)
                        {
                            fields[i] = GenerateField(tb, types[i], names[i]);
                        }

                        for (int i = 0; i < names.Length; ++i)
                        {
                            getters[i] = GenerateGetterMethod(tb, fields[i], types[i], names[i]);
                        }

                        var constructor = GenerateConstructor(tb, types, names, fields);

                        var iEquatableEquals = GenerateEquals(tb, fields, types);

                        var getHashCode = GenerateGetHashCode(tb, types, names, fields);

                        var toString = GenerateToString(tb, types, names, fields);

                        for (int i = 0; i < names.Length; ++i)
                        {
                            GenerateProperty(tb, getters[i], types[i], names[i]);
                        }

                        type = tb.CreateTypeInfo();

                        type = GeneratedTypes.GetOrAdd(fullName, type);
                    }
                }
            }

            return type;
        }

        private static MethodBuilder GenerateEquals(TypeBuilder tb, FieldBuilder[] fields, Type[] types)
        {
            var builder = tb.DefineMethod("Equals", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(bool), new[] { typeof(object) });
            builder.DefineParameter(1, ParameterAttributes.None, "obj");
            var cil = builder.GetILGenerator();

            var other = cil.DeclareLocal(tb);

            if (types.Length == 0)
            {
                var label = cil.DefineLabel();

                cil.Emit(OpCodes.Ldarg_1); // obj != null
                cil.Emit(OpCodes.Brfalse, label);

                cil.Emit(OpCodes.Ldarg_1); // obj is EmitType
                cil.Emit(OpCodes.Isinst, tb);
                cil.Emit(OpCodes.Brfalse, label);

                // return false
                cil.MarkLabel(label);
                cil.Emit(OpCodes.Ldc_I4_0);
            }
            else
            {
                // If obj != null
                var label = cil.DefineLabel();

                cil.Emit(OpCodes.Ldarg_1); // obj != null
                cil.Emit(OpCodes.Brfalse, label);

                cil.Emit(OpCodes.Ldarg_1); // obj is EmitType
                cil.Emit(OpCodes.Isinst, tb);
                cil.Emit(OpCodes.Brfalse, label);

                cil.Emit(OpCodes.Ldarg_1); // EmitType other = obj as EmitType
                cil.Emit(OpCodes.Isinst, tb);
                cil.Emit(OpCodes.Stloc_0);

                // all but one properties match
                for (int i = 0; i < types.Length - 1; ++i)
                {
                    var equalityComparerT = EqualityComparer.MakeGenericType(types[i]);
                    var equalityComparerTDefault = equalityComparerT.GetMethod("get_Default", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
                    var equalityComparerTEquals = equalityComparerT.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, null, new[] { types[i], types[i] }, null);

                    cil.Emit(OpCodes.Call, equalityComparerTDefault); // Put default comparer onto stack
                    cil.Emit(OpCodes.Ldarg_0);
                    cil.Emit(OpCodes.Ldfld, fields[i]); // Put this.field onto stack
                    cil.Emit(OpCodes.Ldloc_0);
                    cil.Emit(OpCodes.Ldfld, fields[i]); // Put other.field onto stack
                    cil.Emit(OpCodes.Callvirt, equalityComparerTEquals);
                    cil.Emit(OpCodes.Brfalse, label);
                }

                // return last property match
                {
                    int i = types.Length - 1;

                    var equalityComparerT = EqualityComparer.MakeGenericType(types[i]);
                    var equalityComparerTDefault = equalityComparerT.GetMethod("get_Default", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
                    var equalityComparerTEquals = equalityComparerT.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, null, new[] { types[i], types[i] }, null);

                    cil.Emit(OpCodes.Call, equalityComparerTDefault); // Put default comparer onto stack
                    cil.Emit(OpCodes.Ldarg_0);
                    cil.Emit(OpCodes.Ldfld, fields[i]); // Put this.field onto stack
                    cil.Emit(OpCodes.Ldloc_0);
                    cil.Emit(OpCodes.Ldfld, fields[i]); // Put other.field onto stack
                    cil.Emit(OpCodes.Callvirt, equalityComparerTEquals);
                    cil.Emit(OpCodes.Ret);
                }

                // return false
                cil.MarkLabel(label);
                cil.Emit(OpCodes.Ldc_I4_0);
            }

            cil.Emit(OpCodes.Ret);

            return builder;
        }

        private static FieldBuilder GenerateField(TypeBuilder tb, Type type, string name)
        {
            var builder = tb.DefineField(string.Format("<{0}>k__BackingField", name), type, FieldAttributes.Private | FieldAttributes.InitOnly);
            builder.SetCustomAttribute(DebuggerBrowsableAttributeBuilder);
            builder.SetCustomAttribute(CompilerGeneratedAttributeBuilder);

            return builder;
        }

        private static MethodBuilder GenerateGetterMethod(TypeBuilder tb, FieldBuilder fb, Type type, string name)
        {
            var builder = tb.DefineMethod(string.Format("get_{0}", name), MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, CallingConventions.HasThis, type, Type.EmptyTypes);

            var cil = builder.GetILGenerator();
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Ldfld, fb);
            cil.Emit(OpCodes.Ret);

            return builder;
        }

        private static PropertyBuilder GenerateProperty(TypeBuilder tb, MethodBuilder getter, Type type, string name)
        {
            var builder = tb.DefineProperty(name, PropertyAttributes.None, CallingConventions.HasThis, type, Type.EmptyTypes);
            builder.SetGetMethod(getter);

            return builder;
        }

        private static ConstructorBuilder GenerateConstructor(TypeBuilder tb, Type[] types, string[] names, FieldBuilder[] fields)
        {
            ConstructorBuilder builder = tb.DefineConstructor(MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.HasThis,
                types);
            for (int i = 0; i < names.Length; ++i)
                builder.DefineParameter(i + 1, ParameterAttributes.None, names[i]);

            ILGenerator cil = builder.GetILGenerator();
            cil.Emit(OpCodes.Ldarg_0); // {
            cil.Emit(OpCodes.Call, ObjectCtor); // : object()

            // Assign parameters to fields
            for (int i = 0; i < names.Length; ++i)
            {
                cil.Emit(OpCodes.Ldarg_0);

                if (i == 0)
                {
                    cil.Emit(OpCodes.Ldarg_1);
                }
                else if (i == 1)
                {
                    cil.Emit(OpCodes.Ldarg_2);
                }
                else if (i == 2)
                {
                    cil.Emit(OpCodes.Ldarg_3);
                }
                else if (i < 255)
                {
                    cil.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                }
                else
                {
                    // Ldarg uses a ushort, but the Emit only
                    // accepts short, so we use a unchecked(...),
                    // cast to short and let the CLR interpret it
                    // as ushort
                    cil.Emit(OpCodes.Ldarg, unchecked((short)(i + 1)));
                }

                cil.Emit(OpCodes.Stfld, fields[i]);
            }

            cil.Emit(OpCodes.Ret); // }

            return builder;
        }

        private static MethodBuilder GenerateToString(TypeBuilder tb, Type[] types, string[] names, FieldBuilder[] fields)
        {
            var builder = tb.DefineMethod("ToString", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, typeof(string), Type.EmptyTypes);
            //builder.SetCustomAttribute(DebuggerHiddenAttributeBuilder);
            var cil = builder.GetILGenerator();

            cil.Emit(OpCodes.Newobj, StringBuilderCtor); // Hold a stringbuilder on the stack

            cil.Emit(OpCodes.Dup); // sb.Append("{");
            cil.Emit(OpCodes.Ldstr, "{");
            cil.Emit(OpCodes.Callvirt, StringBuilderAppendString);
            cil.Emit(OpCodes.Pop);

            for (int i = 0; i < names.Length; ++i)
            {
                cil.Emit(OpCodes.Dup); // sb.Append(", MyProperty = ");
                cil.Emit(OpCodes.Ldstr, (i == 0 ? " " : ", ") + names[i] + " = ");
                cil.Emit(OpCodes.Callvirt, StringBuilderAppendString);
                cil.Emit(OpCodes.Pop);

                cil.Emit(OpCodes.Dup); // sb.Append((object)MyPropertyBackingField);
                cil.Emit(OpCodes.Ldarg_0);
                cil.Emit(OpCodes.Ldfld, fields[i]);
                if (types[i].IsValueType)
                {
                    cil.Emit(OpCodes.Box, types[i]);
                }
                cil.Emit(OpCodes.Callvirt, StringBuilderAppendObject);
                cil.Emit(OpCodes.Pop);
            }

            cil.Emit(OpCodes.Dup);
            cil.Emit(OpCodes.Ldstr, " }");
            cil.Emit(OpCodes.Callvirt, StringBuilderAppendString);
            cil.Emit(OpCodes.Pop);

            cil.Emit(OpCodes.Callvirt, ObjectToString);
            cil.Emit(OpCodes.Ret);

            return builder;
        }

        private static MethodBuilder GenerateGetHashCode(TypeBuilder tb, Type[] types, string[] names, FieldBuilder[] fields)
        {
            var builder = tb.DefineMethod("GetHashCode", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, typeof(int), Type.EmptyTypes);
            //getHashCode.SetCustomAttribute(DebuggerHiddenAttributeBuilder);
            var cil = builder.GetILGenerator();

            cil.DeclareLocal(HashCodeType);

            cil.Emit(OpCodes.Ldloca_S, (byte)0); // HashCode hashCode = default(HashCode);
            cil.Emit(OpCodes.Initobj, HashCodeType);

            for (int i = 0; i < names.Length; ++i)
            {
                cil.Emit(OpCodes.Ldloca_S, (byte)0); // hashCode.Add((object)_field);
                cil.Emit(OpCodes.Ldarg_0);
                cil.Emit(OpCodes.Ldfld, fields[i]);
                if (types[i].IsValueType)
                {
                    cil.Emit(OpCodes.Box, types[i]);
                }
                cil.Emit(OpCodes.Call, HashCodeAddObject);
            }

            cil.Emit(OpCodes.Ldloca_S, (byte)0); // return hashCode.ToHashCode();
            cil.Emit(OpCodes.Call, HashCodeToHashCode);
            cil.Emit(OpCodes.Ret);

            return builder;
        }

        private static string Escape(string str)
        {
            // We escape the \ with \\, so that we can safely escape the
            // "|" (that we use as a separator) with "\|"
            str = str.Replace(@"\", @"\\");
            str = str.Replace(@"|", @"\|");
            return str;
        }

        #region Cache

        static EmitType()
        {
            var assemblyName = new AssemblyName("__EmitTypes");

            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(assemblyName.Name);
        }

        /// <summary>
        /// Types created before, so we don't have collisions on the same type.
        /// Adds a constraint on type names, but since type names map to tables,
        /// they have to be unique anyway.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Type> GeneratedTypes = new ConcurrentDictionary<string, Type>();

        private static readonly AssemblyBuilder AssemblyBuilder;
        private static readonly ModuleBuilder ModuleBuilder;

        /// <summary>
        /// This class was generated directly by the compiler.
        /// </summary>
        private static readonly CustomAttributeBuilder CompilerGeneratedAttributeBuilder = new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
        /// <summary>
        /// This class shouldn't be browseable by the debugger.
        /// </summary>
        private static readonly CustomAttributeBuilder DebuggerBrowsableAttributeBuilder = new CustomAttributeBuilder(typeof(DebuggerBrowsableAttribute).GetConstructor(new[] { typeof(DebuggerBrowsableState) }), new object[] { DebuggerBrowsableState.Never });
        /// <summary>
        /// Some methods should be hidden from the debugger.
        /// </summary>
        private static readonly CustomAttributeBuilder DebuggerHiddenAttributeBuilder = new CustomAttributeBuilder(typeof(DebuggerHiddenAttribute).GetConstructor(Type.EmptyTypes), new object[0]);

        /// <summary>
        /// Needs to be called before the constructor body.
        /// </summary>
        private static readonly ConstructorInfo ObjectCtor = typeof(object).GetConstructor(Type.EmptyTypes);
        /// <summary>
        /// For StringBuilder.ToString() inside generated ToString method.
        /// </summary>
        private static readonly MethodInfo ObjectToString = typeof(object).GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);

        private static readonly ConstructorInfo StringBuilderCtor = typeof(StringBuilder).GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo StringBuilderAppendString = typeof(StringBuilder).GetMethod("Append", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(string) }, null);
        private static readonly MethodInfo StringBuilderAppendObject = typeof(StringBuilder).GetMethod("Append", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(object) }, null);

        private static readonly Type EqualityComparer = typeof(EqualityComparer<>);

        private static readonly Type HashCodeType = typeof(System.HashCode);
        private static readonly MethodInfo HashCodeAddObject = HashCodeType.GetMethods().Where(x => x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Length == 1).Single().MakeGenericMethod(typeof(object));
        private static readonly MethodInfo HashCodeToHashCode = HashCodeType.GetMethod("ToHashCode", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);

        /// <summary>
        /// Index for unique naming.
        /// </summary>
        private static int Index = -1;

        #endregion
    }
}
