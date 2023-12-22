namespace Smart.Data.Accessor.Mappers;

using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Smart.Data.Accessor.Engine;
using Smart.Data.Accessor.Selectors;
using Smart.Reflection.Emit;

public sealed class ObjectResultMapperFactory : IResultMapperFactory
{
    public static ObjectResultMapperFactory Instance { get; } = new();

    private readonly HashSet<string> targetAssemblies = [];

    private int typeNo;

    private AssemblyBuilder? assemblyBuilder;

    private ModuleBuilder moduleBuilder = default!;

    public bool IsMatch(Type type, MethodInfo mi) => true;

    private void PrepareAssembly(Type type)
    {
        if (assemblyBuilder is null)
        {
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("ObjectResultMapperFactoryAssembly"),
                AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(
                "ObjectResultMapperFactoryModule");
        }

        var assemblyName = type.Assembly.GetName().Name;
        if ((assemblyName is not null) && !targetAssemblies.Contains(assemblyName))
        {
            assemblyBuilder!.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(IgnoresAccessChecksToAttribute).GetConstructor([typeof(string)])!,
                [assemblyName]));

            targetAssemblies.Add(assemblyName);
        }
    }

    public ResultMapper<T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns)
    {
        var type = typeof(T);
        var isNullableType = type.IsValueType && type.IsNullableType();
        var targetType = isNullableType ? Nullable.GetUnderlyingType(type)! : type;
        var selector = (IMappingSelector)context.ServiceProvider.GetService(typeof(IMappingSelector))!;
        var typeMap = selector.Select(mi, targetType, columns);
        if (typeMap is null)
        {
            throw new InvalidOperationException($"Type is not supported for mapper. type=[{type}]");
        }

        PrepareAssembly(type);

        var converters = new Dictionary<int, Func<object, object>>();
        TypeMapInfoHelper.BuildConverterMap(typeMap, context, columns, converters);

        // Define type
        var typeBuilder = moduleBuilder.DefineType(
            $"Holder_{typeNo}",
            TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
        typeNo++;

        // Set base type
        var baseType = typeof(ResultMapper<>).MakeGenericType(type);
        typeBuilder.SetParent(baseType);

        // Define fields
        var fields = converters.ToDictionary(
            static x => x.Key,
            x => typeBuilder.DefineField($"parser{x.Key}", typeof(Func<object, object>), FieldAttributes.Public));

        // Define method
        var methodBuilder = typeBuilder.DefineMethod(
            nameof(ResultMapper<T>.Map),
            MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            type,
            [typeof(IDataRecord)]);

        var ilGenerator = methodBuilder.GetILGenerator();

        // Variables
        var objectLocal = converters.Count > 0 ? ilGenerator.DeclareLocal(typeof(object)) : null;
        var ctorLocal = typeMap.Constructor is null ? ilGenerator.DeclareLocal(targetType) : null;
        var valueTypeLocals = ilGenerator.DeclareValueTypeLocals(TypeMapInfoHelper.EnumerateTypes(typeMap));

        // --------------------------------------------------------------------------------
        // Constructor
        // --------------------------------------------------------------------------------

        if (typeMap.Constructor is not null)
        {
            foreach (var parameterMap in typeMap.Constructor.Parameters)
            {
                // Stack value
                EmitStackColumnValue(ilGenerator, parameterMap.Index, parameterMap.Info.ParameterType, objectLocal!, valueTypeLocals, fields.GetValueOrDefault(parameterMap.Index));
            }

            // Class new
            ilGenerator.Emit(OpCodes.Newobj, typeMap.Constructor.Info);
        }
        else
        {
            // Struct init
            ilGenerator.EmitInitStruct(targetType, ctorLocal!);
        }

        // --------------------------------------------------------------------------------
        // Property
        // --------------------------------------------------------------------------------

        foreach (var propertyMap in typeMap.Properties)
        {
            if (ctorLocal is not null)
            {
                ilGenerator.EmitLdloca(ctorLocal);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Dup);
            }

            // Stack value
            EmitStackColumnValue(ilGenerator, propertyMap.Index, propertyMap.Info.PropertyType, objectLocal!, valueTypeLocals, fields.GetValueOrDefault(propertyMap.Index));

            // Set
            ilGenerator.EmitCallMethod(propertyMap.Info.SetMethod!);
        }

        if (ctorLocal is not null)
        {
            ilGenerator.EmitLdloc(ctorLocal);
        }

        if (isNullableType)
        {
            ilGenerator.EmitValueToNullableType(type);
        }

        ilGenerator.Emit(OpCodes.Ret);

        // Create instance
        var typeInfo = typeBuilder.CreateTypeInfo();
        var holderType = typeInfo.AsType();
        var holder = (ResultMapper<T>)Activator.CreateInstance(holderType)!;

        // Set field
        foreach (var entry in converters)
        {
            var field = holderType.GetField($"parser{entry.Key}")!;
            field.SetValue(holder, entry.Value);
        }

        return holder;
    }

    private static void EmitStackColumnValue(
        ILGenerator ilGenerator,
        int index,
        Type type,
        LocalBuilder objectLocal,
        Dictionary<Type, LocalBuilder> valueTypeLocals,
        FieldBuilder? field)
    {
        var hasValueLabel = ilGenerator.DefineLabel();
        var next = ilGenerator.DefineLabel();

        // Stack
        ilGenerator.EmitGetColumnValue(index);

        ilGenerator.EmitCheckDbNull();
        ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

        // Null
        ilGenerator.Emit(OpCodes.Pop);

        ilGenerator.EmitStackDefaultValue(type, valueTypeLocals);

        ilGenerator.Emit(OpCodes.Br_S, next);

        // Value:
        ilGenerator.MarkLabel(hasValueLabel);

        if (field is not null)
        {
            ilGenerator.EmitValueConvertByField(field, objectLocal);
        }

        ilGenerator.EmitTypeConversionForType(type);

        // Next:
        ilGenerator.MarkLabel(next);
    }
}
