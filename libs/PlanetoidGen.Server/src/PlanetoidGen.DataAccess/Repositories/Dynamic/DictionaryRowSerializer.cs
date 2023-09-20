using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Dynamic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace PlanetoidGen.DataAccess.Repositories.Dynamic
{
    public class DictionaryRowSerializer<TData> : IRowSerializer<TData>
    {
        private readonly TableSchema _schema;

        private readonly Type _dictType;
        private readonly MethodInfo _dictAddMethod;

        private readonly Func<IDataReader, TData>? _deserializer;

        private readonly Func<TData, object> _create;
        private readonly Func<TData, object> _createMultiple;
        private readonly Func<TData, object> _delete;
        private readonly Func<TData, object> _deleteMultiple;
        private readonly Func<TData, object> _read;
        private readonly Func<TData, object> _update;
        private readonly Func<TData, object> _updateMultiple;

        public DictionaryRowSerializer(TableSchema schema, Func<IDataReader, TData>? deserializer = null)
        {
            _schema = schema;

            _dictType = typeof(Dictionary<string, object>);
            _dictAddMethod = _dictType.GetMethod("Add");

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

        public object SerializeUpdateMultiple(TData data)
        {
            return _updateMultiple(data);
        }

        public object SerializeDeleteMultiple(TData data)
        {
            return _deleteMultiple(data);
        }

        private Func<TData, object> CompileLambda(string prefix, IEnumerable<ColumnSchema> columns)
        {
            var lambdaParameter = Expression.Parameter(typeof(TData), "value");

            var lambdaBody = GetLambdaBody(prefix, columns, lambdaParameter);

            return Expression.Lambda<Func<TData, object>>(lambdaBody, lambdaParameter).Compile();
        }

        private Expression GetLambdaBody(string prefix, IEnumerable<ColumnSchema> columns, Expression lambdaParameter)
        {
            var initEx = columns.Select(x => Expression.ElementInit(
                _dictAddMethod,
                Expression.Constant($"{prefix}{x.Title/*.ToLowerInvariant()*/}", typeof(string)),
                Expression.Convert(Expression.PropertyOrField(lambdaParameter, x.Title), typeof(object))));

            var newDictionaryExpression = Expression.New(_dictType);

            var listInitExpression = Expression.ListInit(
                newDictionaryExpression,
                initEx);

            return listInitExpression;
        }
    }
}
