using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace NexusForever.SpellWorks.Core.Services
{
    /// <summary>
    /// Builds a browsable catalog by reflecting over <see cref="IGameTableService"/>'s
    /// <c>GameTable&lt;T&gt;</c> properties once per load.
    /// </summary>
    /// <remarks>
    /// Column accessors are compiled to a single <see cref="Func{T, TResult}"/> per entry type, so rendering a
    /// cell never touches reflection.
    /// </remarks>
    public class TableCatalog : ITableCatalog
    {
        public IReadOnlyList<TableDescriptor> Tables => _tables;

        private readonly Dictionary<Type, (string[] Columns, Func<object, string[]> Values)> _schemas = [];
        private readonly Dictionary<string, TableDescriptor> _byName = new(StringComparer.OrdinalIgnoreCase);

        private List<TableDescriptor> _tables = [];

        #region Dependency Injection

        private readonly IGameTableService _gameTableService;

        public TableCatalog(
            IGameTableService gameTableService)
        {
            _gameTableService = gameTableService;
        }

        #endregion

        public TableDescriptor Get(string name)
        {
            return name != null && _byName.TryGetValue(name, out TableDescriptor descriptor) ? descriptor : null;
        }

        public void Clear()
        {
            _tables = [];
            _byName.Clear();
        }

        public void Rebuild()
        {
            var tables = new List<TableDescriptor>();

            foreach (PropertyInfo property in typeof(IGameTableService)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType
                    && p.PropertyType.GetGenericTypeDefinition().Name.StartsWith("GameTable")))
            {
                object gameTable = property.GetValue(_gameTableService);
                if (gameTable == null)
                    continue;

                Type entryType = property.PropertyType.GetGenericArguments()[0];
                (string[] columns, Func<object, string[]> values) = GetSchema(entryType);

                // GameTable<T>.Entries is a T[]; grab it once per rebuild rather than per access.
                var entries = (IReadOnlyList<object>)((IEnumerable)gameTable
                    .GetType()
                    .GetProperty("Entries")
                    .GetValue(gameTable))
                    .Cast<object>()
                    .ToArray();

                tables.Add(new TableDescriptor(
                    property.Name,
                    entryType,
                    entries.Count,
                    columns,
                    () => entries,
                    values));
            }

            _tables = tables.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();

            _byName.Clear();
            foreach (TableDescriptor descriptor in _tables)
                _byName[descriptor.Name] = descriptor;
        }

        private (string[] Columns, Func<object, string[]> Values) GetSchema(Type entryType)
        {
            if (_schemas.TryGetValue(entryType, out (string[], Func<object, string[]>) cached))
                return cached;

            FieldInfo[] fields = entryType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            string[] columns = fields.Select(f => f.Name).ToArray();

            ParameterExpression parameter = Expression.Parameter(typeof(object), "entry");
            UnaryExpression typed = Expression.Convert(parameter, entryType);

            var cells = new Expression[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                cells[i] = Format(Expression.Field(typed, fields[i]), fields[i].FieldType);

            var accessor = Expression
                .Lambda<Func<object, string[]>>(Expression.NewArrayInit(typeof(string), cells), parameter)
                .Compile();

            (string[], Func<object, string[]>) schema = (columns, accessor);
            _schemas.Add(entryType, schema);
            return schema;
        }

        /// <summary>
        /// Render one field as display text. Arrays join with a space, everything else uses <c>ToString</c>.
        /// </summary>
        private static Expression Format(Expression value, Type type)
        {
            if (type.IsArray)
            {
                MethodInfo join = typeof(TableCatalog).GetMethod(nameof(JoinArray), BindingFlags.NonPublic | BindingFlags.Static);
                return Expression.Call(join, Expression.Convert(value, typeof(Array)));
            }

            if (type == typeof(string))
                return Expression.Coalesce(value, Expression.Constant(string.Empty));

            MethodInfo toString = type.GetMethod("ToString", Type.EmptyTypes);
            return Expression.Call(value, toString);
        }

        private static string JoinArray(Array array)
        {
            if (array == null)
                return string.Empty;

            return string.Join(' ', array.Cast<object>());
        }
    }
}
