namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    #endregion

    public interface IObjectWithState {

        DetachedState DetachedState { get; set; }
        /// <summary>
        /// Maintains the OriginalValues change-tracking collection for the life of the entity.
        /// Used to restore the OriginalValues when the entity is attached to a context and updated.
        /// </summary>
        Dictionary<string, object> StartingOriginalValues { get; set; }
        /// <summary>
        /// Returns the PK property for use with DbSet().Find()
        /// </summary>
        Object EntityKey { get; }
    }
}
