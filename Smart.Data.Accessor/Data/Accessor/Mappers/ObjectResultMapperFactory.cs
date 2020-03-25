namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Selectors;
    using Smart.Reflection.Emit;

    public sealed class ObjectResultMapperFactory : IResultMapperFactory
    {
        public static ObjectResultMapperFactory Instance { get; } = new ObjectResultMapperFactory();

        private readonly MethodInfo getValueMethod;

        private readonly MethodInfo getValueWithConvertMethod;

        private int typeNo;

        private AssemblyBuilder assemblyBuilder;

        private ModuleBuilder moduleBuilder;

        private ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder == null)
                {
                    assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("ObjectResultMapperFactoryAssembly"),
                        AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(
                        "ObjectResultMapperFactoryModule");
                }

                return moduleBuilder;
            }
        }

        private ObjectResultMapperFactory()
        {
            getValueMethod = GetType().GetMethod(nameof(GetValue), BindingFlags.Static | BindingFlags.NonPublic);
            getValueWithConvertMethod = GetType().GetMethod(nameof(GetValueWithConvert), BindingFlags.Static | BindingFlags.NonPublic);
        }

        public bool IsMatch(Type type, MethodInfo mi) => true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns)
        {
            var type = typeof(T);
            var entries = CreateMapEntries(context, type, columns);
            var holder = CreateHolder(entries);
            var holderType = holder.GetType();

            var ci = type.GetConstructor(Type.EmptyTypes);
            if (ci is null)
            {
                throw new ArgumentException($"Default constructor not found. type=[{type.FullName}]", nameof(type));
            }

            var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { holderType, typeof(IDataRecord) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Newobj, ci);

            foreach (var entry in entries)
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.EmitLdcI4(entry.Index);
                if (entry.Converter == null)
                {
                    var method = getValueMethod.MakeGenericMethod(entry.Property.PropertyType);
                    ilGenerator.Emit(OpCodes.Call, method);
                }
                else
                {
                    var field = holderType.GetField($"parser{entry.Index}");
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, field);
                    var method = getValueWithConvertMethod.MakeGenericMethod(entry.Property.PropertyType);
                    ilGenerator.Emit(OpCodes.Call, method);
                }
                ilGenerator.Emit(OpCodes.Callvirt, entry.Property.SetMethod);
            }

            ilGenerator.Emit(OpCodes.Ret);

            var funcType = typeof(Func<,>).MakeGenericType(typeof(IDataRecord), type);
            return (Func<IDataRecord, T>)dynamicMethod.CreateDelegate(funcType, holder);
        }

        private static MapEntry[] CreateMapEntries(IResultMapperCreateContext context, Type type, ColumnInfo[] columns)
        {
            var selector = (IPropertySelector)context.ServiceProvider.GetService(typeof(IPropertySelector));
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(IsTargetProperty)
                .ToArray();

            var list = new List<MapEntry>();
            for (var i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var pi = selector.SelectProperty(properties, column.Name);
                if (pi == null)
                {
                    continue;
                }

                list.Add(new MapEntry(i, pi, context.GetConverter(column.Type, pi.PropertyType, pi)));
            }

            return list.ToArray();
        }

        private static bool IsTargetProperty(PropertyInfo pi)
        {
            return pi.CanWrite && (pi.GetCustomAttribute<IgnoreAttribute>() == null);
        }

        private object CreateHolder(MapEntry[] entries)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            // Define setter fields
            foreach (var entry in entries)
            {
                if (entry.Converter != null)
                {
                    typeBuilder.DefineField(
                        $"parser{entry.Index}",
                        typeof(Func<object, object>),
                        FieldAttributes.Public);
                }
            }

            var typeInfo = typeBuilder.CreateTypeInfo();
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType);

            foreach (var entry in entries)
            {
                if (entry.Converter != null)
                {
                    var field = holderType.GetField($"parser{entry.Index}");
                    field.SetValue(holder, entry.Converter);
                }
            }

            return holder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetValue<T>(IDataRecord reader, int index)
        {
            var value = reader.GetValue(index);
            return value is DBNull ? default : (T)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetValueWithConvert<T>(IDataRecord reader, int index, Func<object, object> parser)
        {
            var value = reader.GetValue(index);
            return value is DBNull ? default : (T)parser(value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Performance")]
        private sealed class MapEntry
        {
            public readonly int Index;

            public readonly PropertyInfo Property;

            public readonly Func<object, object> Converter;

            public MapEntry(int index, PropertyInfo property, Func<object, object> converter)
            {
                Index = index;
                Property = property;
                Converter = converter;
            }
        }
    }
}
