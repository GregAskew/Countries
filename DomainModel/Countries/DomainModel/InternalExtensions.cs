namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    #endregion

    internal static class InternalExtensions {

        internal static DataTable CopyToDataTable<T>(this IEnumerable<T> source) {
            return new ObjectShredder<T>().Shred(source, null, null);
        }

        internal static DataTable CopyToDataTable<T>(this IEnumerable<T> source, DataTable table, LoadOption? options) {
            return new ObjectShredder<T>().Shred(source, table, options);
        }
    }
}
