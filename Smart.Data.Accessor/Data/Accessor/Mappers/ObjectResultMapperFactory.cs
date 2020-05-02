namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Selectors;
    using Smart.Reflection.Emit;

    public sealed class ObjectResultMapperFactory : IResultMapperFactory
    {
        public static ObjectResultMapperFactory Instance { get; } = new ObjectResultMapperFactory();

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

        public bool IsMatch(Type type, MethodInfo mi) => true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns)
        {
            var type = typeof(T);
            var selector = (IMappingSelector)context.ServiceProvider.GetService(typeof(IMappingSelector));
            var typeMap = selector.Select(mi, type, columns);
            if (typeMap is null)
            {
                throw new InvalidOperationException($"Type is not supported for mapper. type=[{type}]");
            }

            var converters = new Dictionary<int, Func<object, object>>();
            TypeMapInfoHelper.BuildConverterMap(typeMap, context, columns, converters);

            var holder = CreateHolder(converters);
            var holderType = holder.GetType();

            var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { holderType, typeof(IDataRecord) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Variables
            var objectLocal = converters.Count > 0 ? ilGenerator.DeclareLocal(typeof(object)) : null;
            var ctorLocal = typeMap.Constructor is null ? ilGenerator.DeclareLocal(type) : null;
            var valueTypeLocals = ilGenerator.DeclareValueTypeLocals(TypeMapInfoHelper.EnumerateTypes(typeMap));
            var isShort = valueTypeLocals.Count + (objectLocal is null ? 0 : 1) + (ctorLocal is null ? 0 : 1) <= 256;

            // --------------------------------------------------------------------------------
            // Constructor
            // --------------------------------------------------------------------------------

            foreach (var parameterMap in typeMap.Constructor.Parameters)
            {
                var hasValueLabel = ilGenerator.DefineLabel();
                var next = ilGenerator.DefineLabel();

                // Stack
                ilGenerator.EmitStackColumnValue(parameterMap.Index);

                ilGenerator.EmitCheckDbNull();
                ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                // Null
                ilGenerator.Emit(OpCodes.Pop);

                ilGenerator.EmitStackDefault(parameterMap.Info.ParameterType, valueTypeLocals, isShort);

                ilGenerator.Emit(OpCodes.Br_S, next);

                // Value
                ilGenerator.MarkLabel(hasValueLabel);

                if (converters.ContainsKey(parameterMap.Index))
                {
                    ilGenerator.EmitConvertByField(holderType.GetField($"parser{parameterMap.Index}"), objectLocal, isShort);
                }

                ilGenerator.EmitTypeConversion(parameterMap.Info.ParameterType);

                // Next
                ilGenerator.MarkLabel(next);
            }

            // New
            ilGenerator.Emit(OpCodes.Newobj, typeMap.Constructor.Info);

            // --------------------------------------------------------------------------------
            // Property
            // --------------------------------------------------------------------------------

            foreach (var propertyMap in typeMap.Properties)
            {
                var hasValueLabel = ilGenerator.DefineLabel();
                var next = ilGenerator.DefineLabel();

                ilGenerator.Emit(OpCodes.Dup);

                // Stack
                ilGenerator.EmitStackColumnValue(propertyMap.Index);

                ilGenerator.EmitCheckDbNull();
                ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                // Null
                ilGenerator.Emit(OpCodes.Pop);

                ilGenerator.EmitStackDefault(propertyMap.Info.PropertyType, valueTypeLocals, isShort);

                ilGenerator.Emit(OpCodes.Br_S, next);

                // Value
                ilGenerator.MarkLabel(hasValueLabel);

                if (converters.ContainsKey(propertyMap.Index))
                {
                    ilGenerator.EmitConvertByField(holderType.GetField($"parser{propertyMap.Index}"), objectLocal, isShort);
                }

                ilGenerator.EmitTypeConversion(propertyMap.Info.PropertyType);

                // Next
                ilGenerator.MarkLabel(next);

                // Set
                ilGenerator.EmitSetter(propertyMap.Info);
            }

            ilGenerator.Emit(OpCodes.Ret);

            return (Func<IDataRecord, T>)dynamicMethod.CreateDelegate(typeof(Func<IDataRecord, T>), holder);
        }

        private object CreateHolder(Dictionary<int, Func<object, object>> converters)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            var indexes = converters.Select(x => x.Key).OrderBy(x => x).ToList();

            foreach (var index in indexes)
            {
                if (converters.ContainsKey(index))
                {
                    typeBuilder.DefineField(
                        $"parser{index}",
                        typeof(Func<object, object>),
                        FieldAttributes.Public);
                }
            }

            var typeInfo = typeBuilder.CreateTypeInfo();
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType);

            foreach (var index in indexes)
            {
                if (converters.TryGetValue(index, out var converter))
                {
                    var field = holderType.GetField($"parser{index}");
                    field.SetValue(holder, converter);
                }
            }

            return holder;
        }
    }
}
