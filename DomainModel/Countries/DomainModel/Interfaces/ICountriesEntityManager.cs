namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Validation;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    using System.Data.SqlClient;
    #endregion

    /// <summary>
    /// Interface methods for CountriesEntityManager implementation.  
    /// </summary>
    public interface ICountriesEntityManager : IDisposable {

        DbContextConfiguration Configuration { get; }
        Exception LastError { get; }
        ObjectContext ObjectContext { get; }
        IReadOnlyList<DbEntityValidationResult> ValidationSummary { get; }

        void AddEntity<TEntity>(TEntity entity) where TEntity : class, IObjectWithState;
        DbEntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class, IObjectWithState;
        void Detach(IObjectWithState entity);
        void Detach(IObjectWithState entity, bool detachChildren);
        DbEntityValidationResult GetValidationResult<TEntity>(TEntity entity) where TEntity : class, IObjectWithState;
        bool RemoveEntity<TEntity>(TEntity entity) where TEntity : class, IObjectWithState;
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        int SaveChanges();
        int SaveChanges(bool bulkUpdate);

        #region Native SQL methods
        void BulkCopyDataTable(DataTable dataTable, SqlBulkCopyOptions sqlBulkCopyOptions, SqlTransaction sqlTransaction, int batchSize);
        int BulkDeleteRows(string tableName, List<string> rowIds, string idName, int batchDeleteCount);
        void BulkInsertRows<TEntity>(IEnumerable<TEntity> entities, SqlBulkCopyOptions sqlBulkCopyOptions) where TEntity : class;
        string GetColumnName(Type type, string propertyName);
        DataTable GetDataTable<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
        DataTable ExecuteSql(SqlCommand sqlCommand);
        int ExecuteNonQuerySql(SqlCommand sqlCommand);
        object ExecuteScalarSql(SqlCommand sqlCommand);

        ObjectResult<TEntity> ExecuteSql<TEntity>(string sql, params object[] parameters) where TEntity : class;
        ObjectResult<TEntity> ExecuteSql<TEntity>(string sql, string entitySetName, MergeOption mergeOption, params object[] parameters) where TEntity : class;
        ObjectResult<TEntity> ExecuteStoredProcedure<TEntity>(string functionName, params ObjectParameter[] parameters) where TEntity : class;
        ObjectResult<TEntity> ExecuteStoredProcedure<TEntity>(string functionName, MergeOption mergeOption, params ObjectParameter[] parameters) where TEntity : class;
        #endregion
    }
}
