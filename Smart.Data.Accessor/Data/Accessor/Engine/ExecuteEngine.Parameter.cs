namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Smart.Data.Accessor.Attributes;

    public sealed partial class ExecuteEngine
    {
        //--------------------------------------------------------------------------------
        // In
        //--------------------------------------------------------------------------------

        public Action<DbCommand, string, T> CreateInParameterSetup<T>(ICustomAttributeProvider provider)
        {
            var type = typeof(T);

            // ParameterBuilderAttribute
            var attribute = provider.GetCustomAttributes(true).OfType<ParameterBuilderAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return CreateInParameterSetupByDbType<T>(attribute.DbType, attribute.Size);
            }

            // ITypeHandler
            if (LookupTypeHandler(type, out var handler))
            {
                return CreateInParameterSetupByHandler<T>(handler.SetValue);
            }

            // Type
            if (LookupDbType(type, out var dbType))
            {
                return CreateInParameterSetupByDbType<T>(dbType, null);
            }

            throw new AccessorRuntimeException($"Parameter type is not supported. type=[{type.FullName}]");
        }

        private static Action<DbCommand, string, T> CreateInParameterSetupByHandler<T>(Action<DbParameter, object> action)
        {
            return (cmd, name, value) =>
            {
                var parameter = cmd.CreateParameter();
                cmd.Parameters.Add(parameter);
                if (value == null)
                {
                    parameter.Value = DBNull.Value;
                }
                else
                {
                    action(parameter, value);
                }
                parameter.ParameterName = name;
            };
        }

        private static Action<DbCommand, string, T> CreateInParameterSetupByDbType<T>(DbType dbType, int? size)
        {
            return (cmd, name, value) =>
            {
                var parameter = cmd.CreateParameter();
                cmd.Parameters.Add(parameter);
                if (value == null)
                {
                    parameter.Value = DBNull.Value;
                }
                else
                {
                    parameter.Value = value;
                    parameter.DbType = dbType;
                    if (size.HasValue)
                    {
                        parameter.Size = size.Value;
                    }
                }
                parameter.ParameterName = name;
            };
        }

        //--------------------------------------------------------------------------------
        // In/Out
        //--------------------------------------------------------------------------------

        public Func<DbCommand, string, T, DbParameter> CreateInOutParameterSetup<T>(ICustomAttributeProvider provider)
        {
            var type = typeof(T);

            // ParameterBuilderAttribute
            var attribute = provider.GetCustomAttributes(true).OfType<ParameterBuilderAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return CreateInOutParameterSetupByDbType<T>(attribute.DbType, attribute.Size);
            }

            // ITypeHandler
            if (LookupTypeHandler(type, out var handler))
            {
                return CreateInOutParameterSetupByHandler<T>(handler.SetValue);
            }

            // Type
            if (LookupDbType(type, out var dbType))
            {
                return CreateInOutParameterSetupByDbType<T>(dbType, null);
            }

            throw new AccessorRuntimeException($"Parameter type is not supported. type=[{type.FullName}]");
        }

        private static Func<DbCommand, string, T, DbParameter> CreateInOutParameterSetupByHandler<T>(Action<DbParameter, object> action)
        {
            return (cmd, name, value) =>
            {
                var parameter = cmd.CreateParameter();
                cmd.Parameters.Add(parameter);
                if (value == null)
                {
                    parameter.Value = DBNull.Value;
                }
                else
                {
                    action(parameter, value);
                }
                parameter.ParameterName = name;
                parameter.Direction = ParameterDirection.InputOutput;
                return parameter;
            };
        }

        private static Func<DbCommand, string, T, DbParameter> CreateInOutParameterSetupByDbType<T>(DbType dbType, int? size)
        {
            return (cmd, name, value) =>
            {
                var parameter = cmd.CreateParameter();
                cmd.Parameters.Add(parameter);
                if (value == null)
                {
                    parameter.Value = DBNull.Value;
                }
                else
                {
                    parameter.Value = value;
                    parameter.DbType = dbType;
                    if (size.HasValue)
                    {
                        parameter.Size = size.Value;
                    }
                }
                parameter.ParameterName = name;
                parameter.Direction = ParameterDirection.InputOutput;
                return parameter;
            };
        }

        //--------------------------------------------------------------------------------
        // Out
        //--------------------------------------------------------------------------------

        public Func<DbCommand, string, DbParameter> CreateOutParameterSetup<T>(ICustomAttributeProvider provider)
        {
            var type = typeof(T);

            // ParameterBuilderAttribute
            var attribute = provider.GetCustomAttributes(true).OfType<ParameterBuilderAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return CreateOutParameterSetupByDbType(attribute.DbType, attribute.Size);
            }

            // Type
            if (LookupDbType(type, out var dbType))
            {
                return CreateOutParameterSetupByDbType(dbType, null);
            }

            throw new AccessorRuntimeException($"Parameter type is not supported. type=[{type.FullName}]");
        }

        private static Func<DbCommand, string, DbParameter> CreateOutParameterSetupByDbType(DbType dbType, int? size)
        {
            return (cmd, name) =>
            {
                var parameter = cmd.CreateParameter();
                cmd.Parameters.Add(parameter);
                parameter.DbType = dbType;
                if (size.HasValue)
                {
                    parameter.Size = size.Value;
                }
                parameter.ParameterName = name;
                parameter.Direction = ParameterDirection.Output;
                return parameter;
            };
        }

        //--------------------------------------------------------------------------------
        // Return
        //--------------------------------------------------------------------------------

        public Func<DbCommand, DbParameter> CreateReturnParameterSetup()
        {
            return cmd =>
            {
                var parameter = cmd.CreateParameter();
                cmd.Parameters.Add(parameter);
                parameter.Direction = ParameterDirection.ReturnValue;
                return parameter;
            };
        }

        //--------------------------------------------------------------------------------
        // Array
        //--------------------------------------------------------------------------------

        public Action<string, StringBuilder, T[]> CreateArraySqlSetup<T>()
        {
            return (name, sql, values) =>
            {
                sql.Append("(");

                if ((values is null) || (values.Length == 0))
                {
                    sql.Append(emptyDialect.GetSql());
                }
                else
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        sql.Append(name);
                        sql.Append(GetParameterSubName(i));
                        sql.Append(", ");
                    }

                    sql.Length -= 2;
                }

                sql.Append(") ");
            };
        }

        public Action<DbCommand, string, T[]> CreateArrayParameterSetup<T>(ICustomAttributeProvider provider)
        {
            var type = typeof(T);

            // ParameterBuilderAttribute
            var attribute = provider.GetCustomAttributes(true).OfType<ParameterBuilderAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return CreateArrayParameterSetupByDbType<T>(attribute.DbType, attribute.Size);
            }

            // ITypeHandler
            if (LookupTypeHandler(type, out var handler))
            {
                return CreateArrayParameterSetupByHandler<T>(handler.SetValue);
            }

            // Type
            if (LookupDbType(type, out var dbType))
            {
                return CreateArrayParameterSetupByDbType<T>(dbType, null);
            }

            throw new AccessorRuntimeException($"Parameter type is not supported. type=[{type.FullName}]");
        }

        private Action<DbCommand, string, T[]> CreateArrayParameterSetupByHandler<T>(Action<DbParameter, object> action)
        {
            return (cmd, name, values) =>
            {
                if (values == null)
                {
                    return;
                }

                for (var i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    var parameter = cmd.CreateParameter();
                    cmd.Parameters.Add(parameter);
                    if (value == null)
                    {
                        parameter.Value = DBNull.Value;
                    }
                    else
                    {
                        action(parameter, value);
                    }
                    parameter.ParameterName = name + GetParameterSubName(i);
                }
            };
        }

        private Action<DbCommand, string, T[]> CreateArrayParameterSetupByDbType<T>(DbType dbType, int? size)
        {
            return (cmd, name, values) =>
            {
                if (values == null)
                {
                    return;
                }

                for (var i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    var parameter = cmd.CreateParameter();
                    cmd.Parameters.Add(parameter);
                    if (value == null)
                    {
                        parameter.Value = DBNull.Value;
                    }
                    else
                    {
                        parameter.Value = value;
                        parameter.DbType = dbType;
                        if (size.HasValue)
                        {
                            parameter.Size = size.Value;
                        }
                    }
                    parameter.ParameterName = name + GetParameterSubName(i);
                }
            };
        }

        //--------------------------------------------------------------------------------
        // IList
        //--------------------------------------------------------------------------------

        public Action<string, StringBuilder, IList<T>> CreateListSqlSetup<T>()
        {
            return (name, sql, values) =>
            {
                sql.Append("(");

                if ((values is null) || (values.Count == 0))
                {
                    sql.Append(emptyDialect.GetSql());
                }
                else
                {
                    for (var i = 0; i < values.Count; i++)
                    {
                        sql.Append(name);
                        sql.Append(GetParameterSubName(i));
                        sql.Append(", ");
                    }

                    sql.Length -= 2;
                }

                sql.Append(") ");
            };
        }

        public Action<DbCommand, string, IList<T>> CreateListParameterSetup<T>(ICustomAttributeProvider provider)
        {
            var type = typeof(T);

            // ParameterBuilderAttribute
            var attribute = provider.GetCustomAttributes(true).OfType<ParameterBuilderAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return CreateListParameterSetupByDbType<T>(attribute.DbType, attribute.Size);
            }

            // ITypeHandler
            if (LookupTypeHandler(type, out var handler))
            {
                return CreateListParameterSetupByHandler<T>(handler.SetValue);
            }

            // Type
            if (LookupDbType(type, out var dbType))
            {
                return CreateListParameterSetupByDbType<T>(dbType, null);
            }

            throw new AccessorRuntimeException($"Parameter type is not supported. type=[{type.FullName}]");
        }

        private Action<DbCommand, string, IList<T>> CreateListParameterSetupByHandler<T>(Action<DbParameter, object> action)
        {
            return (cmd, name, values) =>
            {
                if (values is null)
                {
                    return;
                }

                for (var i = 0; i < values.Count; i++)
                {
                    var value = values[i];
                    var parameter = cmd.CreateParameter();
                    cmd.Parameters.Add(parameter);
                    if (value == null)
                    {
                        parameter.Value = DBNull.Value;
                    }
                    else
                    {
                        action(parameter, value);
                    }
                    parameter.ParameterName = name + GetParameterSubName(i);
                }
            };
        }

        private Action<DbCommand, string, IList<T>> CreateListParameterSetupByDbType<T>(DbType dbType, int? size)
        {
            return (cmd, name, values) =>
            {
                if (values is null)
                {
                    return;
                }

                for (var i = 0; i < values.Count; i++)
                {
                    var value = values[i];
                    var parameter = cmd.CreateParameter();
                    cmd.Parameters.Add(parameter);
                    if (value == null)
                    {
                        parameter.Value = DBNull.Value;
                    }
                    else
                    {
                        parameter.Value = value;
                        parameter.DbType = dbType;
                        if (size.HasValue)
                        {
                            parameter.Size = size.Value;
                        }
                    }
                    parameter.ParameterName = name + GetParameterSubName(i);
                }
            };
        }
    }
}
