using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Dynamic;
using PlanetoidGen.DataAccess.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace PlanetoidGen.DataAccess.Repositories.Dynamic
{
    public class EmitTypeRowSerializer<TData> : IRowSerializer<TData>
    {
        private readonly TableSchema _schema;

        private readonly Func<IDataReader, TData>? _deserializer;

        private readonly Func<TData, object> _create;
        private readonly Func<TData, object> _createMultiple;
        private readonly Func<TData, object> _delete;
        private readonly Func<TData, object> _deleteMultiple;
        private readonly Func<TData, object> _read;
        private readonly Func<TData, object> _update;
        private readonly Func<TData, object> _updateMultiple;

        public EmitTypeRowSerializer(TableSchema schema, Func<IDataReader, TData>? deserializer = null)
        {
            _schema = schema;

            _deserializer = deserializer;

            _create = CompileLambda("d", _schema.Columns.Where(x => x.UsedInCreate));
            _createMultiple = CompileLambda("", _schema.Columns.Where(x => x.UsedInCreate));
            _read = CompileLambda("d", _schema.Columns.Where(x => x.UsedInRead));
            _update = CompileLambda("d", _schema.Columns);
            _updateMultiple = CompileLambda("", _schema.Columns);
            _delete = CompileLambda("d", _schema.Columns.Where(x => x.UsedInDelete));
            _deleteMultiple = CompileLambda("", _schema.Columns);
        }

        public Func<IDataReader, TData>? Deserializer => _deserializer;

        public object SerializeCreate(TData data)
        {
            return _create(data);
        }

        public object SerializeCreateMultiple(TData data)
        {
            return _createMultiple(data);
        }

        public object SerializeDelete(TData data)
        {
            return _delete(data);
        }

        public object SerializeRead(TData data)
        {
            return _read(data);
        }

        public object SerializeUpdate(TData data)
        {
            return _update(data);
        }

        private Func<TData, object> CompileLambda(string prefix, IEnumerable<ColumnSchema> columns)
        {
            var constructor = GetAnonymousTypeConstructor(prefix, columns);

            var lambdaParameter = Expression.Parameter(typeof(TData), "value");
            var arguments = columns.Select(x => Expression.PropertyOrField(lambdaParameter, x.Title)).ToArray();

            var lambdaBody = Expression.New(constructor, arguments);
            return Expression.Lambda<Func<TData, object>>(lambdaBody, lambdaParameter).Compile();
        }

        private ConstructorInfo GetAnonymousTypeConstructor(string prefix, IEnumerable<ColumnSchema> columns)
        {
            var keys = columns.Select(x => $"{prefix}{x.Title}");
            var types = columns.Select(x => x.ToType()).ToArray();
            var type = GetAnonymousType(keys, types);
            return type.GetConstructor(types);
        }

        private Type GetAnonymousType(IEnumerable<string> keys, IEnumerable<Type> values)
        {
            var names = keys.ToArray();
            var types = values.ToArray();
            var type = EmitType.CreateType(_schema.Title, types, names);

            return type;
        }

        public object SerializeUpdateMultiple(TData data)
        {
            return _updateMultiple(data);
        }

        public object SerializeDeleteMultiple(TData data)
        {
            return _deleteMultiple(data);
        }
    }
}
