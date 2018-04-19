namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// Implementation of the repository pattern, providing a familiar set of simple data methods.
    /// </summary>
    /// <remarks>
    /// Due to how Entity Framework works with object graphs, exposing granular CRUD methods may not produce the desired result.
    /// When an entity is added or attached, it may add/attach numerous related entities present in the graph, perhaps more than would
    /// be expected for the current unit of work.
    /// 
    /// To ensure that the expected number of entities are added/attached, it may be necessary to work with detached entities 
    /// to excercise more control over when and how objects are included in the graph.
    /// </remarks>
    public class Repository<TEntity, TContext> : IRepository<TEntity, TContext>, IUnitOfWork, IDisposable
        where TEntity : class, IObjectWithState
        where TContext : ICountriesEntityManager {

        #region Members
        private readonly TContext context;
        #endregion

        #region Constructors
        public Repository(TContext context) {
            if (context == null) {
                throw new ArgumentNullException("context");
            }
            this.context = context;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates/adds the entity of the specified type
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>The created entity</returns>
        public virtual void Create(TEntity entity) {
            entity.DetachedState = DetachedState.Added;
            this.context.AddEntity<TEntity>(entity);
        }

        /// <summary>
        /// Deletes the specified entity
        /// </summary>
        /// <param name="entity">The entity</param>
        public virtual void Delete(TEntity entity) {
            entity.DetachedState = DetachedState.Deleted;
            this.context.Set<TEntity>().Remove(entity);
        }

        /// <summary>
        /// Finds and entity by the specified type and predicate
        /// </summary>
        /// <param name="predicate">The predicate</param>
        /// <returns>An IQueryable set of the specified entity type</returns>
        /// <remarks>DbSet.Find will check for the object in-memory and return that, if available</remarks>
        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate) {
            return this.context.Set<TEntity>().Where(predicate).AsQueryable();
        }

        public virtual IQueryable<TEntity> Get() {
            return this.context.Set<TEntity>().AsQueryable();
        }

        /// <summary>
        /// Called with IObjectWithState.EntityKey as the entityKeyId
        /// </summary>
        public virtual TEntity Get(object entityKeyId) {
            return this.context.Set<TEntity>().Find(entityKeyId);
        }

        /// <summary>
        /// Returns an IQueryable of TEntity with optional list of .Includes for the DbSet eager loading
        /// </summary>
        /// <param name="includes">The optional list of related entities to include</param>
        /// <returns>An IQueryable set of the specified entity type</returns>
        public IQueryable<TEntity> Get(params Expression<Func<TEntity, object>>[] includes) {
            var result = this.Get();
            if (includes.Any()) {
                foreach (var include in includes) {
                    result = result.Include(include);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns an IQueryable of TEntity based on the specified predicate, with optional list of .Includes for the DbSet eager loading
        /// </summary>
        /// <param name="predicate">The predicate that filters the query</param>
        /// <param name="includes">The optional list of related entities to include</param>
        /// <returns>An IQueryable set of the specified entity type</returns>
        public IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includes) {
            var result = this.context.Set<TEntity>().Where(predicate);

            if (includes.Any()) {
                foreach (var include in includes) {
                    result = result.Include(include);
                }
            }
            return result;
        }

        /// <summary>
        /// Consider disposing of the context after saving changes.
        /// Create a new context for other units of work.
        /// </summary>
        /// <returns>The number of changes saved.</returns>
        public virtual int Save() {
            return this.context.SaveChanges();
        }

        /// <summary>
        /// Add() is used here due to issues that may occur with store-generated keys and multiple entities with the same key value
        /// if an object graph is attached.
        /// The correct state will be assigned to the entity during inspection of StartingOriginalValues in SaveChanges().
        /// </summary>
        public virtual TEntity Update(TEntity entity) {
            entity.DetachedState = DetachedState.Unchanged;
            return this.context.Set<TEntity>().Add(entity);
        }

        #region IDisposable methods
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                var disposable = this.context as IDisposable;
                if (disposable != null) {
                    disposable.Dispose();
                }
            }
        }
        #endregion 
        #endregion
    }
}