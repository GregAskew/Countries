namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// Creates a DataTable from a collection of entities
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ObjectShredder<T> {

        #region Members

        /// <summary>
        /// Key: Column name Value: Column ordinal
        /// </summary>
        private readonly Dictionary<string, int> ordinalMap;

        /// <summary>
        /// The type of the object
        /// </summary>
        private readonly Type thisType;

        #region Collections
        /// <summary>
        /// The collection of PropertyDescriptors from this Type
        /// </summary>
        private readonly PropertyDescriptorCollection propertyDescriptors;
        #endregion
        #endregion

        #region Constructor
        public ObjectShredder() {
            thisType = typeof(T);
            propertyDescriptors = TypeDescriptor.GetProperties(thisType);
            ordinalMap = new Dictionary<string, int>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Loads a DataTable from a sequence of objects.
        /// </summary>
        /// <param name="source">The sequence of objects to load into the DataTable.</param>
        /// <param name="table">The input table. The schema of the table must match that 
        /// the type T.  If the table is null, a new table is created with a schema 
        /// created from the public properties and fields of the type T.</param>
        /// <param name="options">Specifies how values from the source sequence will be applied to 
        /// existing rows in the table.</param>
        /// <returns>A DataTable created from the source sequence.</returns>
        internal DataTable Shred(IEnumerable<T> source, DataTable table, LoadOption? options) {
            // Load the table from the scalar sequence if T is a primitive type.
            if (typeof(T).IsPrimitive) {
                return ShredPrimitive(source, table, options);
            }

            // Create a new table if the input table is null.
            if (table == null) {
                string tableName = GetTableName();
                table = new DataTable(tableName);
            }

            // Initialize the ordinal map and extend the table schema based on type T.
            table = ExtendTable(table, typeof(T));

            // Enumerate the source sequence and load the object values into rows.
            table.BeginLoadData();
            using (IEnumerator<T> e = source.GetEnumerator()) {
                while (e.MoveNext()) {
                    if (options != null) {
                        table.LoadDataRow(ShredObject(table, e.Current), (LoadOption)options);
                    }
                    else {
                        table.LoadDataRow(ShredObject(table, e.Current), fAcceptChanges: true);
                    }
                }
            }
            table.EndLoadData();

            // Return the table.
            return table;
        }

        internal DataTable ShredPrimitive(IEnumerable<T> source, DataTable table, LoadOption? options) {
            // Create a new table if the input table is null.
            string tableName = GetTableName();
            if (table == null) {
                table = new DataTable(tableName);
            }

            if (!table.Columns.Contains("Value")) {
                table.Columns.Add("Value", typeof(T));
            }

            // Enumerate the source sequence and load the scalar values into rows.
            table.BeginLoadData();
            using (IEnumerator<T> e = source.GetEnumerator()) {
                object[] values = new object[table.Columns.Count];
                while (e.MoveNext()) {
                    values[table.Columns["Value"].Ordinal] = e.Current;

                    if (options != null) {
                        table.LoadDataRow(values, (LoadOption)options);
                    }
                    else {
                        table.LoadDataRow(values, fAcceptChanges: true);
                    }
                }
            }
            table.EndLoadData();

            // Return the table.
            return table;
        }

        private object[] ShredObject(DataTable table, T instance) {
            PropertyDescriptorCollection pdc = propertyDescriptors;

            if (instance.GetType() != typeof(T)) {
                // If the instance is derived from T, extend the table schema and get the properties.
                ExtendTable(table, instance.GetType());
                pdc = TypeDescriptor.GetProperties(typeof(T));
            }

            // Add the property and field values of the instance to an array.
            object[] values = new object[table.Columns.Count];

            foreach (PropertyDescriptor propertyDescriptor in pdc) {

                string columnName = GetColumnName(propertyDescriptor);
                if (!ordinalMap.ContainsKey(columnName)) {
                    continue;
                }
                values[ordinalMap[columnName]] = propertyDescriptor.GetValue(instance) ?? DBNull.Value;
            }

            // Return the property and field values of the instance.
            return values;
        }

        private DataTable ExtendTable(DataTable table, Type type) {
            // Extend the table schema if the input table was null or if the value 
            // in the sequence is derived from type T.            

            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(type)) {
                var skip = false;
                foreach (var attribute in propertyDescriptor.Attributes) {

                    // check for and skip property if it is NotMapped to a column
                    var notMappedAttribute = attribute as NotMappedAttribute;
                    if (notMappedAttribute != null) {
                        skip = true;
                        break;
                    }
                    // check for and skip property if column is DatabaseGenerated
                    var databaseGeneratedAttribute = attribute as DatabaseGeneratedAttribute;
                    if (databaseGeneratedAttribute != null) {
                        if (databaseGeneratedAttribute.DatabaseGeneratedOption != DatabaseGeneratedOption.None) {
                            skip = true;
                            break;
                        }
                    }
                }
                if (skip) continue;

                if (string.IsNullOrWhiteSpace(propertyDescriptor.PropertyType.Name)) continue;
                if (propertyDescriptor.PropertyType.Name.StartsWith("ICollection", StringComparison.OrdinalIgnoreCase)) continue;
                if ((propertyDescriptor.PropertyType.BaseType == null) || string.IsNullOrWhiteSpace(propertyDescriptor.PropertyType.BaseType.FullName)) continue;
                if (propertyDescriptor.PropertyType.BaseType.Assembly == this.GetType().Assembly) continue;

                string columnName = GetColumnName(propertyDescriptor);

                if (!ordinalMap.ContainsKey(columnName)) {
                    // Add the property as a column in the table if it doesn't exist
                    DataColumn dc = table.Columns.Contains(columnName)
                        ? table.Columns[columnName]
                        : table.Columns.Add(columnName, Nullable.GetUnderlyingType(propertyDescriptor.PropertyType) ?? propertyDescriptor.PropertyType);

                    // Add the property to the ordinal map.
                    ordinalMap.Add(columnName, dc.Ordinal);
                }
            }

            // Return the table.
            return table;
        }

        /// <summary>
        /// Gets the name of the column, checking for a ColumnAttribute.
        /// </summary>
        /// <param name="propertyDescriptor">The PropertyDescriptor</param>
        /// <returns>The column name.</returns>
        private string GetColumnName(PropertyDescriptor propertyDescriptor) {
            string columnName = string.Empty;
            // Check for [Column] attribute, and use that name if present
            foreach (var attribute in propertyDescriptor.Attributes) {
                var columnAttribute = attribute as ColumnAttribute;
                if (columnAttribute != null) {
                    columnName = columnAttribute.Name;
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(columnName) && !string.IsNullOrWhiteSpace(propertyDescriptor.Name)) {
                columnName = propertyDescriptor.Name;
            }

            return columnName;
        }

        /// <summary>
        /// Gets the table name of the specified type, checking for a TableAttribute.
        /// </summary>
        /// <returns>The table name.</returns>
        private string GetTableName() {
            string tableName = string.Empty;

            // Check for [Table] attribute, and use that name if present
            foreach (var attribute in TypeDescriptor.GetAttributes(typeof(T))) {
                var tableAttribute = attribute as TableAttribute;
                if (tableAttribute != null) {
                    tableName = tableAttribute.Name ?? string.Empty;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(tableName)) {
                tableName = typeof(T).Name;
            }

            return tableName;
        } 
        #endregion
    }
}
