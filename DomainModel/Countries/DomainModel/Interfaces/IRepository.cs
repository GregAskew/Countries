namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    #endregion

    public interface IRepository<TEntity, TContext> : IUnitOfWork where TEntity : class, IObjectWithState {

        void Create(TEntity entity);
        void Delete(TEntity entity);
        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        IQueryable<TEntity> Get();
        TEntity Get(object objectWithStateEntityKey);
        IQueryable<TEntity> Get(params Expression<Func<TEntity, object>>[] includes);
        IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includes);
        TEntity Update(TEntity entity);

        // Update() is handled by the EntityManager during inspection of the .StartingOriginalValues collection when SaveChanges() is called.
        // SaveChanges is implemented on the client so that they can handle recovery/rollback as they see fit.
        // Save is defined separately as IUnitOfWork, where multiple adds/updates/deletes are performed together.
    }
}
